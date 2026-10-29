// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestController.ts (_insertSuggestion, getOverwriteInfo)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Features.Snippet;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Suggest;

// Accepting an item: a port of _insertSuggestion. Behavior differences from Monaco: a plain-text item edits only the primary cursor and is not
// re-indented; an item with InsertTextRules.InsertAsSnippet goes through the snippet controller, which edits every cursor and re-indents unless
// KeepWhitespace is set. Not ported: item commands, memory, OvertypingCapturer, telemetry, onWillInsertSuggestItem, alternatives, LineSuffix.
internal sealed partial class SuggestController
{
    /// <summary>
    /// Replaces the typed prefix with the text of <paramref name="item"/> in one undo step; false (nothing changed) when it cannot.
    /// <paramref name="noAfterUndoStop"/> skips the undo stop after the edit (a commit character is typed right after). An unresolved item without
    /// additional edits gets them when it is resolved, unless the user edited before an edit start meanwhile; the edit ranges are those of the
    /// document as it was when the item was built, and that check is the only protection.
    /// </summary>
    public bool Accept(SuggestItem item, bool noAfterUndoStop = false)
    {
        ArgumentNullException.ThrowIfNull(item);
        ViewModel? vm = _viewModel;
        TextModel model = _context.Model;
        Position cursor = _context.Selection.Position;
        if (vm is null || vm.IsReadOnly || cursor.LineNumber != item.Position.LineNumber)
        {
            Model.Cancel();
            return false;
        }

        model.PushStackElement();

        bool isResolved = item.IsResolved;
        bool hasSyncEdits = item.Completion.AdditionalTextEdits is not null;

        // overwrite info (insertMode = insert), computed before the additional edits
        int columnDelta = cursor.Column - item.Position.Column;
        int overwriteBefore = item.Position.Column - item.EditStart.Column + columnDelta;
        int overwriteAfter = item.EditInsertEnd.Column - item.Position.Column;

        Model.Cancel();

        if (item.Completion.AdditionalTextEdits is { Count: > 0 } edits)
        {
            var ops = new List<IdentifiedEditOperation>(edits.Count);
            foreach (TextEdit edit in edits)
            {
                Range range = edit.Range;
                if (range.StartLineNumber == item.Position.LineNumber && range.StartColumn > item.Position.Column)
                {
                    // shift the additional edit when it is "after" the completion insertion position
                    int endDelta = range.StartLineNumber == range.EndLineNumber ? columnDelta : 0;
                    range = new Range(range.StartLineNumber, range.StartColumn + columnDelta, range.EndLineNumber, range.EndColumn + endDelta);
                }

                ops.Add(new IdentifiedEditOperation(range, edit.Text, ForceMoveMarkers: true));
            }

            vm.ExecuteEdits("suggest.additionalTextEdits", ops, _ => null);
            _context.GetAnnouncer().Announce(AccessibilityMessages.SuggestAdditionalEdits(item.TextLabel, edits.Count));
        }

        bool asyncEdits = !hasSyncEdits && !isResolved;
        bool oldNoAfterUndoStop = noAfterUndoStop;
        if (asyncEdits)
        {
            StartAsyncEdits(item, vm, oldNoAfterUndoStop);
            noAfterUndoStop = true;
        }

        Position cursor2 = _context.Selection.Position;
        int startColumn = Math.Max(1, cursor2.Column - overwriteBefore);
        int endColumn = Math.Min(model.GetLineMaxColumn(cursor2.LineNumber), cursor2.Column + overwriteAfter);
        if (item.Completion.InsertTextRules.HasFlag(InsertTextRules.InsertAsSnippet) && _context.GetSnippetController() is { } snippets)
        {
            // Monaco: SnippetController2.insert; the undo stops are pushed by this method, and the snippet session edits every cursor.
            snippets.Insert(
                item.Completion.InsertText,
                new SnippetInsertOptions
                {
                    OverwriteBefore = overwriteBefore,
                    OverwriteAfter = overwriteAfter,
                    AdjustWhitespace = !item.Completion.InsertTextRules.HasFlag(InsertTextRules.KeepWhitespace),
                    UndoStopBefore = false,
                    UndoStopAfter = false,
                });
        }
        else
        {
            var mainRange = new Range(cursor2.LineNumber, startColumn, cursor2.LineNumber, endColumn);
            vm.ExecuteEdits(
                "suggest",
                [new IdentifiedEditOperation(mainRange, item.Completion.InsertText)],
                undoEdits => [Selection.Collapsed(undoEdits[0].Range.End)]);
        }

        if (!noAfterUndoStop)
        {
            model.PushStackElement();
        }

        return true;
    }

    // Monaco: the isResolved == false branch of _insertSuggestion
    private void StartAsyncEdits(SuggestItem item, ViewModel vm, bool noAfterUndoStop)
    {
        TextModel model = _context.Model;
        var cts = new CancellationTokenSource();
        Position? position = null;
        var didType = false;
        var done = false;

        void OnContentChanged(object? sender, ModelContentChangedEventArgs e)
        {
            if (e.IsFlush)
            {
                Cleanup(cancel: true);
                return;
            }

            foreach (ModelContentChange change in e.Changes)
            {
                Position thisPosition = change.Range.End;
                if (position is not { } p || thisPosition.IsBefore(p))
                {
                    position = thisPosition;
                }
            }
        }

        void OnWillType(object? sender, string text)
        {
            vm.WillType -= OnWillType;
            didType = true;
            if (!noAfterUndoStop)
            {
                model.PushStackElement();
            }
        }

        void Cleanup(bool cancel)
        {
            if (done)
            {
                return;
            }

            done = true;
            model.ContentChanged -= OnContentChanged;
            vm.WillType -= OnWillType;
            _pendingAsyncEdits.Remove(Cancel);
            if (cancel)
            {
                cts.Cancel();
            }

            cts.Dispose();
        }

        void Cancel() => Cleanup(cancel: true);

        model.ContentChanged += OnContentChanged;
        vm.WillType += OnWillType;
        _pendingAsyncEdits.Add(Cancel);

        Task<CompletionItem?>? task = item.BeginResolve(cts.Token);
        if (task is null)
        {
            Cleanup(cancel: false);
            return;
        }

        UiContinuation.Then(
            task,
            _context.GetUiDispatcher(),
            resolved =>
            {
                item.CompleteResolve(resolved);
                if (item.Completion.AdditionalTextEdits is { Count: > 0 } edits
                    && !(position is { } p && edits.Any(edit => p.IsBefore(edit.Range.Start))))
                {
                    if (didType)
                    {
                        model.PushStackElement();
                    }

                    vm.ExecuteEdits(
                        "suggest.additionalTextEdits.async",
                        [.. edits.Select(e => new IdentifiedEditOperation(e.Range, e.Text, ForceMoveMarkers: true))],
                        _ => null);
                    if (didType || !noAfterUndoStop)
                    {
                        model.PushStackElement();
                    }

                    _context.GetAnnouncer().Announce(AccessibilityMessages.SuggestAdditionalEdits(item.TextLabel, edits.Count));
                }

                Cleanup(cancel: false);
            },
            _ => Cleanup(cancel: false),
            cts.Token);
    }
}
