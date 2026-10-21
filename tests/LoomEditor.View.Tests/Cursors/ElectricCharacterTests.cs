// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Tests.Cursors.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public class ElectricCharacterTests
{
    private const string LanguageId = EditorControllerLanguages.ElectricCharLanguageId;

    // Types `ch` at (line, column) of a model made of `text`, and returns the content of `line`.
    private static string TypeAt(string[] text, int line, int column, string ch)
    {
        string result = string.Empty;
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(text, LanguageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, line, column);
            viewModel.Type(ch, "keyboard");
            result = model.GetLineContent(line);
        });
        return result;
    }

    // Monaco: 'ElectricCharacter - does nothing if no electric char'
    [Fact]
    public void DoesNothingIfNoElectricChar() =>
        Assert.Equal("*", TypeAt(["  if (a) {", string.Empty], 2, 1, "*"));

    // Monaco: 'ElectricCharacter - indents in order to match bracket'
    [Fact]
    public void IndentsInOrderToMatchBracket() =>
        Assert.Equal("  }", TypeAt(["  if (a) {", string.Empty], 2, 1, "}"));

    // Monaco: 'ElectricCharacter - unindents in order to match bracket'
    [Fact]
    public void UnindentsInOrderToMatchBracket() =>
        Assert.Equal("  }", TypeAt(["  if (a) {", "    "], 2, 5, "}"));

    // Monaco: 'ElectricCharacter - matches with correct bracket'
    [Fact]
    public void MatchesWithCorrectBracket() =>
        Assert.Equal("  }    ", TypeAt(["  if (a) {", "    if (b) {", "    }", "    "], 4, 1, "}"));

    // Monaco: 'ElectricCharacter - does nothing if bracket does not match'
    [Fact]
    public void DoesNothingIfBracketDoesNotMatch() =>
        Assert.Equal("  }  }", TypeAt(["  if (a) {", "    if (b) {", "    }", "  }  "], 4, 6, "}"));

    // Monaco: 'ElectricCharacter - matches bracket even in line with content'
    [Fact]
    public void MatchesBracketEvenInLineWithContent() =>
        Assert.Equal("  }// hello", TypeAt(["  if (a) {", "// hello"], 2, 1, "}"));

    // Monaco: 'ElectricCharacter - is no-op if bracket is lined up'
    [Fact]
    public void IsNoOpIfBracketIsLinedUp() =>
        Assert.Equal("  }", TypeAt(["  if (a) {", "  "], 2, 3, "}"));

    // Monaco: 'ElectricCharacter - is no-op if there is non-whitespace text before'
    [Fact]
    public void IsNoOpIfThereIsNonWhitespaceTextBefore() =>
        Assert.Equal("a}", TypeAt(["  if (a) {", "a"], 2, 2, "}"));

    // Monaco: 'ElectricCharacter - is no-op if pairs are all matched before'
    [Fact]
    public void IsNoOpIfPairsAreAllMatchedBefore() =>
        Assert.Equal("  ( 1 + 2 ) *", TypeAt(["foo(() => {", "  ( 1 + 2 ) ", "})"], 2, 13, "*"));

    // Monaco: 'ElectricCharacter - is no-op if matching bracket is on the same line'
    [Fact]
    public void IsNoOpIfMatchingBracketIsOnTheSameLine()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["(div"], LanguageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, 1, 5);
            string? changeText = null;
            model.ContentChanged += (_, e) => changeText = e.Changes[0].Text;
            viewModel.Type(")", "keyboard");
            Assert.Equal("(div)", model.GetLineContent(1));
            Assert.Equal(")", changeText);
        });
    }

    // Monaco: 'ElectricCharacter - is no-op if the line has other content'
    [Fact]
    public void IsNoOpIfTheLineHasOtherContent() =>
        Assert.Equal("\t3)", TypeAt(["Math.max(", "\t2", "\t3"], 3, 3, ")"));

    // Monaco: 'ElectricCharacter - appends text'
    [Fact]
    public void AppendsText() =>
        Assert.Equal("/** */", TypeAt(["  if (a) {", "/*"], 2, 3, "*"));

    // Monaco: 'ElectricCharacter - appends text 2'
    [Fact]
    public void AppendsText2() =>
        Assert.Equal("  /** */", TypeAt(["  if (a) {", "  /*"], 2, 5, "*"));

    // Monaco: 'ElectricCharacter - issue #23711: Replacing selected text with )]} fails to delete old text with backwards-dragged selection'
    [Fact]
    public void Issue23711_ReplacingSelectedTextWithClosingBracketFailsToDeleteOldTextWithBackwardsDraggedSelection()
    {
        using var languages = new EditorControllerLanguages();
        languages.UsingCursor(new CursorOpts(["{", "word"], LanguageId), (editor, model, viewModel) =>
        {
            MoveTo(viewModel, 2, 5);
            MoveTo(viewModel, 2, 1, true);
            viewModel.Type("}", "keyboard");
            Assert.Equal("}", model.GetLineContent(2));
        });
    }
}
