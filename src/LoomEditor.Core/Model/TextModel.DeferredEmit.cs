// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts (DidChangeContentEmitter)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model;

public sealed partial class TextModel
{
    private int _deferredCount;
    private (ModelRawContentChangedEventArgs Raw, ModelContentChangedEventArgs Content)? _deferredEvent;

    // Monaco: DidChangeContentEmitter.beginDeferredEmit
    private void BeginDeferredEmit() => _deferredCount++;

    // Monaco: DidChangeContentEmitter.endDeferredEmit. The events raised since the matching begin were merged into one;
    // it is delivered now, with the selections to restore (undo, redo) or without.
    private void EndDeferredEmit(IReadOnlyList<Selection>? resultingSelection = null)
    {
        _deferredCount--;
        if (_deferredCount == 0 && _deferredEvent is { } deferred)
        {
            _deferredEvent = null;
            var raw = new ModelRawContentChangedEventArgs(
                deferred.Raw.Changes, deferred.Raw.VersionId, deferred.Raw.IsUndoing, deferred.Raw.IsRedoing, resultingSelection);
            Dispatch(raw, deferred.Content);
        }
    }

    // Monaco: DidChangeContentEmitter.fire
    private void Deliver(ModelRawContentChangedEventArgs raw, ModelContentChangedEventArgs content)
    {
        if (_deferredCount > 0)
        {
            _deferredEvent = _deferredEvent is { } previous ? Merge(previous.Raw, previous.Content, raw, content) : (raw, content);
            return;
        }

        Dispatch(raw, content);
    }

    // Monaco: InternalModelContentChangeEvent.merge
    private static (ModelRawContentChangedEventArgs, ModelContentChangedEventArgs) Merge(
        ModelRawContentChangedEventArgs rawA,
        ModelContentChangedEventArgs contentA,
        ModelRawContentChangedEventArgs rawB,
        ModelContentChangedEventArgs contentB)
    {
        var raw = new ModelRawContentChangedEventArgs(
            [.. rawA.Changes, .. rawB.Changes],
            rawB.VersionId,
            rawA.IsUndoing || rawB.IsUndoing,
            rawA.IsRedoing || rawB.IsRedoing);
        var content = new ModelContentChangedEventArgs(
            [.. contentA.Changes, .. contentB.Changes],
            contentB.Eol,
            contentB.VersionId,
            contentA.IsUndoing || contentB.IsUndoing,
            contentA.IsRedoing || contentB.IsRedoing,
            contentA.IsFlush || contentB.IsFlush,
            contentA.IsEolChange && contentB.IsEolChange); // both must be true to not confuse listeners who skip such edits
        return (raw, content);
    }
}
