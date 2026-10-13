// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model;

public sealed partial class TextModel
{
    /// <summary>Raised after the text changed, once the model is fully updated (Monaco: <c>onDidChangeContent</c>).</summary>
    /// <remarks>Raised once per <c>ApplyEdits</c> call that changes something, once per <c>SetValue</c> and once per effective <c>SetEol</c>. An edit made by a handler is delivered after the current event has reached every handler.</remarks>
    public event EventHandler<ModelContentChangedEventArgs>? ContentChanged;

    /// <summary>Raised just before <see cref="ContentChanged"/>, describing the change per line (Monaco: the raw content changed event).</summary>
    /// <remarks>Raised under the same conditions as <see cref="ContentChanged"/>.</remarks>
    public event EventHandler<ModelRawContentChangedEventArgs>? RawContentChanged;

    /// <summary>Applies a batch of edits, all expressed against the text before the batch (Monaco: <c>applyEdits</c>).</summary>
    /// <param name="edits">The edits; their ranges are validated first and they must not overlap.</param>
    /// <exception cref="ArgumentNullException"><paramref name="edits"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Two edits overlap.</exception>
    public void ApplyEdits(IReadOnlyList<TextEdit> edits) => DoApplyEdits(edits, computeUndoEdits: false);

    /// <summary>Applies a batch of edits and optionally returns the edits that undo them (Monaco: <c>applyEdits</c>).</summary>
    /// <param name="edits">The edits; their ranges are validated first and they must not overlap.</param>
    /// <param name="computeUndoEdits">True to compute the inverse edits.</param>
    /// <returns>The inverse edits when <paramref name="computeUndoEdits"/> is true; an empty list otherwise.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="edits"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Two edits overlap.</exception>
    public IReadOnlyList<TextEdit> ApplyEdits(IReadOnlyList<TextEdit> edits, bool computeUndoEdits) =>
        DoApplyEdits(edits, computeUndoEdits);

    /// <summary>Replaces the whole text and resets the model state (Monaco: <c>setValue</c>).</summary>
    /// <remarks>Raises one event with <see cref="ModelContentChangedEventArgs.IsFlush"/> set, even if the text is the same.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public void SetValue(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        PieceTreeTextBuffer newBuffer = CreateBuffer(text, _defaultEol);
        Range oldFullModelRange = FullModelRange;
        int oldValueLength = GetValueLengthInRange(oldFullModelRange);

        _buffer = newBuffer;
        IncreaseVersionId();

        RaiseContentChanged(new ModelRawFlush(), oldFullModelRange, oldValueLength, GetValue(), isFlush: true, isEolChange: false);
    }

    /// <summary>Changes the end of line of the text, rewriting its line breaks (Monaco: <c>setEOL</c>).</summary>
    /// <remarks>Does nothing when the text already uses that EOL; otherwise raises one event with <see cref="ModelContentChangedEventArgs.IsEolChange"/> set.</remarks>
    public void SetEol(EndOfLineSequence eol)
    {
        string newEol = eol == EndOfLineSequence.CRLF ? "\r\n" : "\n";
        if (_buffer.GetEol() == newEol)
        {
            return;
        }

        Range oldFullModelRange = FullModelRange;
        int oldValueLength = GetValueLengthInRange(oldFullModelRange);

        _buffer.SetEol(newEol);
        IncreaseVersionId();

        RaiseContentChanged(new ModelRawEolChanged(), oldFullModelRange, oldValueLength, GetValue(), isFlush: false, isEolChange: true);
    }

    // Monaco: _increaseVersionId
    private void IncreaseVersionId()
    {
        _versionId++;
        _alternativeVersionId = _versionId;
    }

    // Monaco: _createContentChanged2 + _emitContentChangedEvent (public part)
    private void RaiseContentChanged(ModelRawChange rawChange, Range oldRange, int oldLength, string newText, bool isFlush, bool isEolChange)
    {
        var change = new ModelContentChange(oldRange, 0, oldLength, newText);
        Deliver(
            new ModelRawContentChangedEventArgs([rawChange], _versionId, isUndoing: false, isRedoing: false),
            new ModelContentChangedEventArgs(
                [change], _buffer.GetEol(), _versionId, isUndoing: false, isRedoing: false, isFlush, isEolChange));
    }

    // Monaco: Emitter.fire with its delivery queue. An edit made by a handler raises its events only once the event
    // being delivered has reached every handler, so all handlers see the events in version order.
    private void Deliver(ModelRawContentChangedEventArgs raw, ModelContentChangedEventArgs content)
    {
        _deliveryQueue.Enqueue((raw, content));
        if (_isDelivering)
        {
            return;
        }

        _isDelivering = true;
        try
        {
            while (_deliveryQueue.TryDequeue(out (ModelRawContentChangedEventArgs Raw, ModelContentChangedEventArgs Content) item))
            {
                RawContentChanged?.Invoke(this, item.Raw);
                ContentChanged?.Invoke(this, item.Content);
            }
        }
        finally
        {
            _isDelivering = false;
            _deliveryQueue.Clear();
        }
    }

    // Monaco: applyEdits + _doApplyEdits (without decorations, raw events and edit sources)
    private TextEdit[] DoApplyEdits(IReadOnlyList<TextEdit> edits, bool computeUndoEdits)
    {
        ArgumentNullException.ThrowIfNull(edits);

        var operations = new ValidAnnotatedEditOperation[edits.Count];
        for (int i = 0; i < operations.Length; i++)
        {
            operations[i] = ValidateEditOperation(edits[i]);
        }

        int oldLineCount = _buffer.GetLineCount();
        ApplyEditsResult result = _buffer.ApplyEdits(operations, recordTrimAutoWhitespace: false, computeUndoEdits);
        int newLineCount = _buffer.GetLineCount();

        if (result.Changes.Count != 0)
        {
            IncreaseVersionId();

            var changes = new ModelContentChange[result.Changes.Count];
            for (int i = 0; i < changes.Length; i++)
            {
                changes[i] = result.Changes[i].Change;
            }

            Deliver(
                new ModelRawContentChangedEventArgs(
                    ComputeRawChanges(changes, oldLineCount, newLineCount), _versionId, isUndoing: false, isRedoing: false),
                new ModelContentChangedEventArgs(
                    changes, _buffer.GetEol(), _versionId, isUndoing: false, isRedoing: false, isFlush: false, isEolChange: false));
        }

        if (result.ReverseEdits is null)
        {
            return [];
        }

        var reverse = new TextEdit[result.ReverseEdits.Count];
        for (int i = 0; i < reverse.Length; i++)
        {
            reverse[i] = new TextEdit(result.ReverseEdits[i].Range, result.ReverseEdits[i].Text);
        }

        return reverse;
    }

    // Monaco: _doApplyEdits (the raw content changes)
    private static List<ModelRawChange> ComputeRawChanges(ModelContentChange[] contentChanges, int oldLineCount, int newLineCount)
    {
        var rawChanges = new List<ModelRawChange>();
        int lineCount = oldLineCount;
        foreach (ModelContentChange change in contentChanges)
        {
            int eolCount = EolCounter.Count(change.Text).Count;

            int startLineNumber = change.Range.StartLineNumber;
            int endLineNumber = change.Range.EndLineNumber;

            int deletingLinesCnt = endLineNumber - startLineNumber;
            int insertingLinesCnt = eolCount;
            int editingLinesCnt = Math.Min(deletingLinesCnt, insertingLinesCnt);

            int changeLineCountDelta = insertingLinesCnt - deletingLinesCnt;

            int currentEditStartLineNumber = newLineCount - lineCount - changeLineCountDelta + startLineNumber;

            for (int j = editingLinesCnt; j >= 0; j--)
            {
                rawChanges.Add(new ModelRawLineChanged(startLineNumber + j, currentEditStartLineNumber + j));
            }

            if (editingLinesCnt < deletingLinesCnt)
            {
                // Must delete some lines
                int spliceStartLineNumber = startLineNumber + editingLinesCnt;
                int cnt = insertingLinesCnt - deletingLinesCnt;
                int lastUntouchedLinePostEdit = newLineCount - lineCount - cnt + spliceStartLineNumber;
                rawChanges.Add(new ModelRawLinesDeleted(spliceStartLineNumber + 1, endLineNumber, lastUntouchedLinePostEdit));
            }

            if (editingLinesCnt < insertingLinesCnt)
            {
                // Must insert some lines
                int spliceLineNumber = startLineNumber + editingLinesCnt;
                int cnt = insertingLinesCnt - editingLinesCnt;
                int fromLineNumber = newLineCount - lineCount - cnt + spliceLineNumber + 1;
                rawChanges.Add(new ModelRawLinesInserted(spliceLineNumber + 1, fromLineNumber, cnt));
            }

            lineCount += changeLineCountDelta;
        }

        return rawChanges;
    }

    // Monaco: _validateEditOperation
    private ValidAnnotatedEditOperation ValidateEditOperation(TextEdit edit)
    {
        Range validatedRange = ValidateRange(edit.Range);

        // Normalize edit when replacement text ends with lone CR and the range ends right before a CRLF in the buffer:
        // strip the trailing CR from the replacement text.
        string text = edit.Text;
        if (text.Length > 0
            && text[^1] == '\r'
            && _buffer.GetEol() == "\r\n"
            && validatedRange.EndColumn == GetLineMaxColumn(validatedRange.EndLineNumber))
        {
            text = text[..^1];
        }

        return new ValidAnnotatedEditOperation(
            Identifier: null,
            validatedRange,
            text,
            ForceMoveMarkers: false,
            IsAutoWhitespaceEdit: false,
            IsTracked: false);
    }
}
