// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestController.ts (_insertSuggestion, getOverwriteInfo)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Features;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Suggest;

// Accepting an item: a plain-text port of _insertSuggestion. Behavior differences from Monaco: only the primary cursor is edited (Monaco snippet
// insertion edits every cursor) and the inserted text is not re-indented (adjustWhitespace belongs to snippets). Not ported: item commands,
// memory, OvertypingCapturer, SnippetController2, telemetry, onWillInsertSuggestItem, alternatives, LineSuffix.
internal sealed partial class SuggestController
{
    /// <summary>Replaces the typed prefix with the text of <paramref name="item"/> in one undo step; false (nothing changed) when it cannot.</summary>
    public bool Accept(SuggestItem item)
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

        Position cursor2 = _context.Selection.Position;
        int startColumn = Math.Max(1, cursor2.Column - overwriteBefore);
        int endColumn = Math.Min(model.GetLineMaxColumn(cursor2.LineNumber), cursor2.Column + overwriteAfter);
        var mainRange = new Range(cursor2.LineNumber, startColumn, cursor2.LineNumber, endColumn);
        vm.ExecuteEdits(
            "suggest",
            [new IdentifiedEditOperation(mainRange, item.Completion.InsertText)],
            undoEdits => [Selection.Collapsed(undoEdits[0].Range.End)]);

        model.PushStackElement();
        return true;
    }
}
