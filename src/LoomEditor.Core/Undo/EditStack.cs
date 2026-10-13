// Ported from microsoft/vscode: src/vs/editor/common/model/editStack.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Undo;

/// <summary>Records the edits made through <c>PushEditOperations</c> and <c>PushEol</c> as undo steps.</summary>
/// <remarks>
/// <para>Monaco: <c>EditStack</c>, on top of <see cref="UndoRedoStack"/> instead of <c>IUndoRedoService</c>.</para>
/// <para>
/// Behavior difference: an exception thrown by the cursor state computer is not caught (Monaco reports it and uses
/// a null cursor state).
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class EditStack
{
    private readonly TextModel _model;
    private readonly UndoRedoStack _stack;

    public EditStack(TextModel model, UndoRedoStack stack)
    {
        _model = model;
        _stack = stack;
    }

    public void PushStackElement() => _stack.GetLastElement()?.Close();

    public void PopStackElement() => _stack.GetLastElement()?.Open();

    public void Clear() => _stack.Clear();

    public void PushEol(EndOfLineSequence eol)
    {
        SingleModelEditStackElement element = GetOrCreateEditStackElement(null);
        _model.SetEol(eol);
        element.Append([], _model.EndOfLineSequence, _model.AlternativeVersionId, null);
    }

    public IReadOnlyList<Selection>? PushEditOperation(
        IReadOnlyList<Selection>? beforeCursorState,
        IReadOnlyList<TextEdit> edits,
        Func<IReadOnlyList<TextEdit>, IReadOnlyList<Selection>?>? cursorStateComputer)
    {
        SingleModelEditStackElement element = GetOrCreateEditStackElement(beforeCursorState);
        IReadOnlyList<ValidEditOperation> inverse = _model.ApplyEditsForUndo(edits);
        IReadOnlyList<Selection>? afterCursorState = ComputeCursorState(cursorStateComputer, inverse);

        var textChanges = new (int Index, TextChange Change)[inverse.Count];
        for (int i = 0; i < textChanges.Length; i++)
        {
            textChanges[i] = (i, inverse[i].TextChange);
        }

        Array.Sort(textChanges, static (a, b) =>
            a.Change.OldPosition == b.Change.OldPosition ? a.Index - b.Index : a.Change.OldPosition - b.Change.OldPosition);

        var sorted = new TextChange[textChanges.Length];
        for (int i = 0; i < sorted.Length; i++)
        {
            sorted[i] = textChanges[i].Change;
        }

        element.Append(sorted, _model.EndOfLineSequence, _model.AlternativeVersionId, afterCursorState);
        return afterCursorState;
    }

    private SingleModelEditStackElement GetOrCreateEditStackElement(IReadOnlyList<Selection>? beforeCursorState)
    {
        SingleModelEditStackElement? lastElement = _stack.GetLastElement();
        if (lastElement is not null && lastElement.CanAppend(_model))
        {
            return lastElement;
        }

        var newElement = new SingleModelEditStackElement(_model, beforeCursorState);
        _stack.Push(newElement);
        return newElement;
    }

    // Monaco: _computeCursorState
    private static IReadOnlyList<Selection>? ComputeCursorState(
        Func<IReadOnlyList<TextEdit>, IReadOnlyList<Selection>?>? cursorStateComputer,
        IReadOnlyList<ValidEditOperation> inverse)
    {
        if (cursorStateComputer is null)
        {
            return null;
        }

        var inverseEdits = new TextEdit[inverse.Count];
        for (int i = 0; i < inverseEdits.Length; i++)
        {
            inverseEdits[i] = new TextEdit(inverse[i].Range, inverse[i].Text);
        }

        return cursorStateComputer(inverseEdits);
    }
}
