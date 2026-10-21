// Ported from microsoft/vscode: src/vs/editor/contrib/bracketMatching/test/browser/bracketMatching.test.ts (suite 'bracket matching')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features.BracketMatching;
using LoomEditor.View.Tests.Features.BracketMatching.TestHelpers;

namespace LoomEditor.View.Tests.Features.BracketMatching;

public class BracketMatchingTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(50);

    private static void SetPosition(BracketEditor editor, int lineNumber, int column) =>
        editor.Context.Selection = new Selection(lineNumber, column, lineNumber, column);

    private static Position GetPosition(BracketEditor editor) => editor.Context.Selection.Position;

    private static List<Range> GetHighlights(BracketEditor editor)
    {
        int lineCount = editor.Context.Model.LineCount;
        Range all = new(1, 1, lineCount, editor.Context.Model.GetLineMaxColumn(lineCount));
        return [.. editor.Context.GetDecorationsInRange(all)
            .Where(d => d.Options.ClassName == "bracket-match")
            .Select(d => d.Range)
            .OrderBy(r => r, Comparer<Range>.Create(Range.CompareUsingStarts))];
    }

    // Monaco: 'issue #183: jump to matching bracket position'
    [Fact]
    public void Issue183JumpToMatchingBracketPosition()
    {
        using var editor = new BracketEditor("var x = (3 + (5-7)) + ((5+3)+5);");

        // start on closing bracket
        SetPosition(editor, 1, 20);
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 9), GetPosition(editor));
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 19), GetPosition(editor));
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 9), GetPosition(editor));

        // start on opening bracket
        SetPosition(editor, 1, 23);
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 31), GetPosition(editor));
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 23), GetPosition(editor));
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 31), GetPosition(editor));
    }

    // Monaco: 'Jump to next bracket'
    [Fact]
    public void JumpToNextBracket()
    {
        using var editor = new BracketEditor("var x = (3 + (5-7)); y();");

        // start position between brackets
        SetPosition(editor, 1, 16);
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 18), GetPosition(editor));
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 14), GetPosition(editor));
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 18), GetPosition(editor));

        // skip brackets in comments
        SetPosition(editor, 1, 21);
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 23), GetPosition(editor));
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 24), GetPosition(editor));
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 23), GetPosition(editor));

        // do not break if no brackets are available
        SetPosition(editor, 1, 26);
        editor.Feature.JumpToBracket();
        Assert.Equal(new Position(1, 26), GetPosition(editor));
    }

    // Monaco: 'Select to next bracket'
    [Fact]
    public void SelectToNextBracket()
    {
        using var editor = new BracketEditor("var x = (3 + (5-7)); y();");

        // start position in open brackets
        SetPosition(editor, 1, 9);
        editor.Feature.SelectToBracket(true);
        Assert.Equal(new Position(1, 20), GetPosition(editor));
        Assert.Equal(new Selection(1, 9, 1, 20), editor.Context.Selection);

        // start position in close brackets (should select backwards)
        SetPosition(editor, 1, 20);
        editor.Feature.SelectToBracket(true);
        Assert.Equal(new Position(1, 9), GetPosition(editor));
        Assert.Equal(new Selection(1, 20, 1, 9), editor.Context.Selection);

        // start position between brackets
        SetPosition(editor, 1, 16);
        editor.Feature.SelectToBracket(true);
        Assert.Equal(new Position(1, 19), GetPosition(editor));
        Assert.Equal(new Selection(1, 14, 1, 19), editor.Context.Selection);

        // start position outside brackets
        SetPosition(editor, 1, 21);
        editor.Feature.SelectToBracket(true);
        Assert.Equal(new Position(1, 25), GetPosition(editor));
        Assert.Equal(new Selection(1, 23, 1, 25), editor.Context.Selection);

        // do not break if no brackets are available
        SetPosition(editor, 1, 26);
        editor.Feature.SelectToBracket(true);
        Assert.Equal(new Position(1, 26), GetPosition(editor));
        Assert.Equal(new Selection(1, 26, 1, 26), editor.Context.Selection);
    }

    private const string NestedText = "const x = {\n    something: [0, 1, 2],\n    another: true,\n    somethingmore: [0, 2, 4]\n};";

    // Monaco: 'issue #1772: jump to enclosing brackets'
    [Fact]
    public void Issue1772JumpToEnclosingBrackets()
    {
        using var editor = new BracketEditor(NestedText);

        SetPosition(editor, 3, 5);
        editor.Feature.JumpToBracket();

        Assert.Equal(new Selection(5, 1, 5, 1), editor.Context.Selection);
    }

    // Monaco: 'issue #43371: argument to not select brackets'
    [Fact]
    public void Issue43371ArgumentToNotSelectBrackets()
    {
        using var editor = new BracketEditor(NestedText);

        SetPosition(editor, 3, 5);
        editor.Feature.SelectToBracket(false);

        Assert.Equal(new Selection(1, 12, 5, 1), editor.Context.Selection);
    }

    // Monaco: 'issue #45369: Select to Bracket with multicursor'
    [Fact]
    public void Issue45369SelectToBracketWithMulticursor()
    {
        using var editor = new BracketEditor("{  }   {   }   { }");

        // cursors inside brackets become selections of the entire bracket contents
        editor.Context.SetSelections([new Selection(1, 3, 1, 3), new Selection(1, 10, 1, 10), new Selection(1, 17, 1, 17)]);
        editor.Feature.SelectToBracket(true);
        Assert.Equal([new Selection(1, 1, 1, 5), new Selection(1, 8, 1, 13), new Selection(1, 16, 1, 19)], editor.Context.Selections);

        // cursors to the left of bracket pairs become selections of the entire pair
        editor.Context.SetSelections([new Selection(1, 1, 1, 1), new Selection(1, 6, 1, 6), new Selection(1, 14, 1, 14)]);
        editor.Feature.SelectToBracket(true);
        Assert.Equal([new Selection(1, 1, 1, 5), new Selection(1, 8, 1, 13), new Selection(1, 16, 1, 19)], editor.Context.Selections);

        // cursors just right of a bracket pair become selections of the entire pair
        editor.Context.SetSelections([new Selection(1, 5, 1, 5), new Selection(1, 13, 1, 13), new Selection(1, 19, 1, 19)]);
        editor.Feature.SelectToBracket(true);
        Assert.Equal([new Selection(1, 5, 1, 1), new Selection(1, 13, 1, 8), new Selection(1, 19, 1, 16)], editor.Context.Selections);
    }

    // Monaco: 'Removes brackets'
    [Fact]
    public void RemovesBrackets()
    {
        using var editor = new BracketEditor("var x = (3 + (5-7)); y();");

        // position before the bracket
        SetPosition(editor, 1, 9);
        editor.Feature.RemoveBrackets("test");
        Assert.Equal("var x = 3 + (5-7); y();", editor.Context.Model.GetValue());
        editor.Context.Model.SetValue("var x = (3 + (5-7)); y();");

        // position between brackets
        SetPosition(editor, 1, 16);
        editor.Feature.RemoveBrackets("test");
        Assert.Equal("var x = (3 + 5-7); y();", editor.Context.Model.GetValue());
        editor.Feature.RemoveBrackets("test");
        Assert.Equal("var x = 3 + 5-7; y();", editor.Context.Model.GetValue());
        editor.Feature.RemoveBrackets("test");
        Assert.Equal("var x = 3 + 5-7; y();", editor.Context.Model.GetValue());
    }

    [Fact]
    public void CursorOnBracket_After50ms_HighlightsBothBrackets()
    {
        using var editor = new BracketEditor("a (b) c");

        SetPosition(editor, 1, 3);
        editor.Scheduler.AdvanceBy(Delay - TimeSpan.FromMilliseconds(1));
        Assert.Empty(GetHighlights(editor));
        editor.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));

        Assert.Equal([new Range(1, 3, 1, 4), new Range(1, 5, 1, 6)], GetHighlights(editor));
    }

    [Fact]
    public void CursorInsidePair_Always_HighlightsEnclosingPair()
    {
        using var editor = new BracketEditor("a (b c) d");

        SetPosition(editor, 1, 5);
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Equal([new Range(1, 3, 1, 4), new Range(1, 7, 1, 8)], GetHighlights(editor));
    }

    [Fact]
    public void CursorInsidePair_Near_HighlightsNothing()
    {
        using var editor = new BracketEditor("a (b c) d");
        editor.Feature.MatchBrackets = MatchBracketsMode.Near;

        SetPosition(editor, 1, 5);
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Empty(GetHighlights(editor));
    }

    [Fact]
    public void CursorOnBracket_Near_HighlightsThePair()
    {
        using var editor = new BracketEditor("a (b c) d");
        editor.Feature.MatchBrackets = MatchBracketsMode.Near;

        SetPosition(editor, 1, 3);
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Equal(2, GetHighlights(editor).Count);
    }

    [Fact]
    public void CursorOnBracket_Never_HighlightsNothing()
    {
        using var editor = new BracketEditor("a (b c) d");
        editor.Feature.MatchBrackets = MatchBracketsMode.Never;

        SetPosition(editor, 1, 3);
        editor.Scheduler.RunAll();

        Assert.Empty(GetHighlights(editor));
    }

    [Fact]
    public void MoreThan100Selections_HighlightsNothing()
    {
        string text = string.Concat(Enumerable.Repeat("(a)\n", 101));
        using var editor = new BracketEditor(text);

        editor.Context.SetSelections([.. Enumerable.Range(1, 101).Select(line => new Selection(line, 1, line, 1))]);
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Empty(GetHighlights(editor));
    }

    [Fact]
    public void NoFocus_HighlightsNothing()
    {
        using var editor = new BracketEditor("a (b) c", focused: false);

        SetPosition(editor, 1, 3);
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Empty(GetHighlights(editor));
    }

    [Fact]
    public void FocusGained_HighlightsTheBrackets()
    {
        using var editor = new BracketEditor("a (b) c", focused: false);
        SetPosition(editor, 1, 3);
        editor.Scheduler.AdvanceBy(Delay);

        editor.Context.ViewModel.SetHasFocus(true);
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Equal(2, GetHighlights(editor).Count);
    }

    [Fact]
    public void SameVersion_ReusesPreviousBracketsData()
    {
        using var editor = new BracketEditor("(a) [b]");
        SetPosition(editor, 1, 1);
        editor.Scheduler.AdvanceBy(Delay);
        Assert.Equal(2, GetHighlights(editor).Count);

        // a second cursor: the first position comes from the previous data, the new one is computed
        editor.Context.SetSelections([new Selection(1, 1, 1, 1), new Selection(1, 5, 1, 5)]);
        editor.Scheduler.AdvanceBy(Delay);

        Assert.Equal([new Range(1, 1, 1, 2), new Range(1, 3, 1, 4), new Range(1, 5, 1, 6), new Range(1, 7, 1, 8)], GetHighlights(editor));
    }

    [Fact]
    public void Keybindings_CtrlShiftBackslash_And_CtrlAltBackspace_AreBound()
    {
        Assert.Contains(DefaultKeybindings.Windows, r =>
            r.CommandId == BracketMatchingCommands.JumpToBracket
            && r.Keybinding == new Keybinding(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.Backslash)
            && r.Weight == KeybindingWeight.EditorContrib);
        Assert.Contains(DefaultKeybindings.Windows, r =>
            r.CommandId == BracketMatchingCommands.RemoveBrackets
            && r.Keybinding == new Keybinding(KeyModifiers.Ctrl | KeyModifiers.Alt, KeyCode.Backspace)
            && r.Weight == KeybindingWeight.EditorContrib);
    }
}
