// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.Core.Undo;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model;

public sealed partial class TextModel
{
    /// <summary>True when there is something to undo (Monaco: <c>canUndo</c>).</summary>
    public bool CanUndo => _undoRedoStack.CanUndo;

    /// <summary>True when there is something to redo (Monaco: <c>canRedo</c>).</summary>
    public bool CanRedo => _undoRedoStack.CanRedo;

    /// <summary>Closes the current undo step: the next edit pushed starts a new one (Monaco: <c>pushStackElement</c>).</summary>
    public void PushStackElement() => _editStack.PushStackElement();

    /// <summary>Reopens the current undo step, so the next edit pushed is appended to it (Monaco: <c>popStackElement</c>).</summary>
    public void PopStackElement() => _editStack.PopStackElement();

    /// <summary>Changes the EOL as an undoable edit; does nothing when the text already uses it (Monaco: <c>pushEOL</c>).</summary>
    public void PushEol(EndOfLineSequence eol)
    {
        if (EndOfLineSequence == eol)
        {
            return;
        }

        _editStack.PushEol(eol);
    }

    /// <summary>
    /// Applies a batch of edits and records them in the undo stack. Edits pushed with no <see cref="PushStackElement"/>
    /// in between form one undo step (Monaco: <c>pushEditOperations</c>).
    /// </summary>
    /// <param name="beforeCursorState">The selections before the edit; restored by <see cref="Undo"/>. Only used when the edit starts a new undo step.</param>
    /// <param name="edits">The edits; their ranges are validated first and they must not overlap.</param>
    /// <param name="cursorStateComputer">Computes the selections after the edit from the inverse edits; restored by <see cref="Redo"/>. May be <see langword="null"/>.</param>
    /// <returns>The selections computed by <paramref name="cursorStateComputer"/>, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="edits"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Two edits overlap.</exception>
    public IReadOnlyList<Selection>? PushEditOperations(
        IReadOnlyList<Selection>? beforeCursorState,
        IReadOnlyList<TextEdit> edits,
        Func<IReadOnlyList<TextEdit>, IReadOnlyList<Selection>?>? cursorStateComputer)
    {
        ArgumentNullException.ThrowIfNull(edits);
        return PushEditOperations(
            beforeCursorState,
            ToIdentified(edits),
            cursorStateComputer is null ? null : inverse => cursorStateComputer(ToTextEdits(inverse)));
    }

    /// <summary>
    /// Like the public overload, with edits that carry an identifier and flags; the cursor state computer receives the
    /// inverse operations with the identifiers of the forward operations (Monaco: <c>pushEditOperations</c>).
    /// </summary>
    /// <remarks>
    /// The content change events are raised immediately, not deferred: Monaco only defers the public
    /// <c>onDidChangeContent</c>, and the view models are notified at once.
    /// </remarks>
    internal IReadOnlyList<Selection>? PushEditOperations(
        IReadOnlyList<Selection>? beforeCursorState,
        IReadOnlyList<IdentifiedEditOperation> edits,
        Func<IReadOnlyList<ValidEditOperation>, IReadOnlyList<Selection>?>? cursorStateComputer)
    {
        ArgumentNullException.ThrowIfNull(edits);
        return _editStack.PushEditOperation(beforeCursorState, edits, cursorStateComputer);
    }

    private static TextEdit[] ToTextEdits(IReadOnlyList<ValidEditOperation> operations)
    {
        var edits = new TextEdit[operations.Count];
        for (int i = 0; i < edits.Length; i++)
        {
            edits[i] = new TextEdit(operations[i].Range, operations[i].Text);
        }

        return edits;
    }

    /// <summary>Undoes the last undo step (Monaco: <c>undo</c>).</summary>
    /// <returns>The selections to restore, or <see langword="null"/> when nothing was undone or the step has none.</returns>
    /// <remarks>Raises <see cref="ContentChanged"/> with <see cref="ModelContentChangedEventArgs.IsUndoing"/> set.</remarks>
    public IReadOnlyList<Selection>? Undo()
    {
        SingleModelEditStackElement? element = _undoRedoStack.Undo();
        if (element is null)
        {
            return null;
        }

        SingleModelEditStackData data = element.Data;
        ApplyUndo(data.Changes, data.BeforeEol, data.BeforeVersionId, data.BeforeCursorState);
        return data.BeforeCursorState;
    }

    /// <summary>Redoes the last undone step (Monaco: <c>redo</c>).</summary>
    /// <returns>The selections to restore, or <see langword="null"/> when nothing was redone or the step has none.</returns>
    /// <remarks>Raises <see cref="ContentChanged"/> with <see cref="ModelContentChangedEventArgs.IsRedoing"/> set.</remarks>
    public IReadOnlyList<Selection>? Redo()
    {
        SingleModelEditStackElement? element = _undoRedoStack.Redo();
        if (element is null)
        {
            return null;
        }

        SingleModelEditStackData data = element.Data;
        ApplyRedo(data.Changes, data.AfterEol, data.AfterVersionId, data.AfterCursorState);
        return data.AfterCursorState;
    }

    // Monaco: _applyUndo (the changes are expressed on the text after the step: put the old text back)
    private void ApplyUndo(
        IReadOnlyList<TextChange> changes,
        EndOfLineSequence eol,
        int resultingAlternativeVersionId,
        IReadOnlyList<Selection>? resultingSelection)
    {
        var edits = new IdentifiedEditOperation[changes.Count];
        for (int i = 0; i < edits.Length; i++)
        {
            TextChange change = changes[i];
            Position start = GetPositionAt(change.NewPosition);
            Position end = GetPositionAt(change.NewEnd);
            edits[i] = new IdentifiedEditOperation(new Range(start.LineNumber, start.Column, end.LineNumber, end.Column), change.OldText);
        }

        ApplyUndoRedoEdits(edits, eol, isUndoing: true, isRedoing: false, resultingAlternativeVersionId, resultingSelection);
    }

    // Monaco: _applyRedo
    private void ApplyRedo(
        IReadOnlyList<TextChange> changes,
        EndOfLineSequence eol,
        int resultingAlternativeVersionId,
        IReadOnlyList<Selection>? resultingSelection)
    {
        var edits = new IdentifiedEditOperation[changes.Count];
        for (int i = 0; i < edits.Length; i++)
        {
            TextChange change = changes[i];
            Position start = GetPositionAt(change.OldPosition);
            Position end = GetPositionAt(change.OldEnd);
            edits[i] = new IdentifiedEditOperation(new Range(start.LineNumber, start.Column, end.LineNumber, end.Column), change.NewText);
        }

        ApplyUndoRedoEdits(edits, eol, isUndoing: false, isRedoing: true, resultingAlternativeVersionId, resultingSelection);
    }

    // Monaco: _applyUndoRedoEdits
    private void ApplyUndoRedoEdits(
        IReadOnlyList<IdentifiedEditOperation> edits,
        EndOfLineSequence eol,
        bool isUndoing,
        bool isRedoing,
        int resultingAlternativeVersionId,
        IReadOnlyList<Selection>? resultingSelection)
    {
        try
        {
            _isUndoing = isUndoing;
            _isRedoing = isRedoing;
            DoApplyEdits(edits, computeUndoEdits: false, resultingSelection);
            SetEol(eol);
            _alternativeVersionId = resultingAlternativeVersionId;
        }
        finally
        {
            _isUndoing = false;
            _isRedoing = false;
        }
    }
}
