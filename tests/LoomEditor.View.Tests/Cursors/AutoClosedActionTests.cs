// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts (AutoClosedAction, _pushAutoClosedAction, executeEdits)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

// Monaco has no test for these members on their own (cursor.test.ts reaches them through typing: P5-43): all tests are new.
public class AutoClosedActionTests
{
    private static void WithTrackedQuote(Action<TextModel, ViewModel> callback)
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["<div id"], EditorControllerLanguages.AutoClosingLanguageId), (editor, model, viewModel) =>
        {
            viewModel.ExecuteEdits(
                "snippet",
                [new IdentifiedEditOperation(new Range(1, 6, 1, 8), "id=\"\"")],
                _ => [new Selection(1, 10, 1, 10)]);
            callback(model, viewModel);
        });
    }

    [Fact]
    public void ExecuteEdits_SnippetWithQuotes_TracksTheClosingQuote()
    {
        WithTrackedQuote((model, viewModel) =>
        {
            Assert.Equal("<div id=\"\"", model.GetLineContent(1));
            Assert.Equal([new Range(1, 10, 1, 11)], viewModel.GetCursorAutoClosedCharacters());
        });
    }

    [Fact]
    public void ExecuteEdits_OtherSource_TracksNothing()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["<div id"], EditorControllerLanguages.AutoClosingLanguageId), (editor, model, viewModel) =>
        {
            viewModel.ExecuteEdits(
                "test",
                [new IdentifiedEditOperation(new Range(1, 6, 1, 8), "id=\"\"")],
                _ => [new Selection(1, 10, 1, 10)]);

            Assert.Empty(viewModel.GetCursorAutoClosedCharacters());
        });
    }

    [Fact]
    public void AutoClosedAction_CursorLeavesThePair_IsForgotten()
    {
        WithTrackedQuote((model, viewModel) =>
        {
            MoveTo(viewModel, 1, 1);

            Assert.Empty(viewModel.GetCursorAutoClosedCharacters());
        });
    }

    [Fact]
    public void AutoClosedAction_PairBecomesMultiline_IsForgotten()
    {
        WithTrackedQuote((model, viewModel) =>
        {
            model.ApplyEdits([TextEdit.Insert(new Position(1, 10), "\n")]);

            Assert.Empty(viewModel.GetCursorAutoClosedCharacters());
        });
    }

    [Fact]
    public void AutoClosedAction_Forgotten_RemovesItsDecorations()
    {
        WithTrackedQuote((model, viewModel) =>
        {
            MoveTo(viewModel, 1, 1);

            Range all = new(1, 1, model.LineCount, model.GetLineMaxColumn(model.LineCount));
            Assert.DoesNotContain(model.GetDecorationsInRange(all, 0, false, false), d => d.Options.Description.StartsWith("auto-closed-", StringComparison.Ordinal));
        });
    }
}
