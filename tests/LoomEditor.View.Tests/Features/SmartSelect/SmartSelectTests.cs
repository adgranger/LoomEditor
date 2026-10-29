// Ported from microsoft/vscode: src/vs/editor/contrib/smartSelect/test/browser/smartSelect.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features;
using LoomEditor.View.Features.SmartSelect;

namespace LoomEditor.View.Tests.Features.SmartSelect;

public sealed class SmartSelectTests : IDisposable
{
    private static readonly Regex WordPattern = new(@"(-?\d*\.\d\w*)|([^\`\~\!\@\#\$\%\^\&\*\(\)\=\+\[\{\]\}\\\;\:\'\""\,\.\<\>\/\?\s]+)");

    private readonly EditorContext _context = new();
    private readonly SmartSelectFeature _feature = new();

    public SmartSelectTests()
    {
        _context.RegisterLanguage("mockJSMode");
        _context.Model.LanguageConfigurationRegistry.Register("mockJSMode", new LanguageConfiguration
        {
            Brackets = [new("(", ")"), new("{", "}"), new("[", "]")],
            WordPattern = WordPattern,
        });
        _context.Model.Tokenization.SetLanguageId("mockJSMode");
        _feature.Attach(_context);
    }

    public void Dispose()
    {
        _feature.Dispose();
        _context.Dispose();
    }

    private TextModel SetText(string text)
    {
        _context.SetText(text);
        return _context.Model;
    }

    private void AssertGetRangesToPosition(string[] text, int lineNumber, int column, Range[] ranges, bool selectLeadingAndTrailingWhitespace = true)
    {
        TextModel model = SetText(string.Join("\n", text));
        List<Range> actual = SmartSelectRanges.Provide(model, [new Position(lineNumber, column)], new SmartSelectOptions(selectLeadingAndTrailingWhitespace, true))[0];
        Assert.Equal(ranges.Reverse(), actual);
    }

    private void AssertRanges(ISelectionRangeProvider provider, string value, params Range[] expected)
    {
        int index = value.IndexOf('|');
        TextModel model = SetText(value.Remove(index, 1));
        Position pos = model.GetPositionAt(index);
        Assert.Equal(expected, provider.ProvideSelectionRanges(model, [pos])[0]);
    }

    // Monaco: 'getRangesToPosition #1'
    [Fact]
    public void GetRangesToPosition_1()
    {
        AssertGetRangesToPosition(
            ["function a(bar, foo){", "\tif (bar) {", "\t\treturn (bar + (2 * foo))", "\t}", "}"],
            3,
            20,
            [
                new Range(1, 1, 5, 2), // all
                new Range(1, 21, 5, 2), // {} outside
                new Range(1, 22, 5, 1), // {} inside
                new Range(2, 1, 4, 3), // block
                new Range(2, 1, 4, 3),
                new Range(2, 2, 4, 3),
                new Range(2, 11, 4, 3),
                new Range(2, 12, 4, 2),
                new Range(3, 1, 3, 27), // line w/ triva
                new Range(3, 3, 3, 27), // line w/o triva
                new Range(3, 10, 3, 27), // () outside
                new Range(3, 11, 3, 26), // () inside
                new Range(3, 17, 3, 26), // () outside
                new Range(3, 18, 3, 25), // () inside
            ]);
    }

    // Monaco: 'config: selectLeadingAndTrailingWhitespace'
    [Fact]
    public void Config_SelectLeadingAndTrailingWhitespace()
    {
        AssertGetRangesToPosition(
            ["aaa", "\tbbb", string.Empty],
            2,
            3,
            [
                new Range(1, 1, 3, 1), // all
                new Range(2, 1, 2, 5), // line w/ triva
                new Range(2, 2, 2, 5), // bbb
            ],
            true);

        AssertGetRangesToPosition(
            ["aaa", "\tbbb", string.Empty],
            2,
            3,
            [
                new Range(1, 1, 3, 1), // all
                new Range(2, 2, 2, 5), // () inside
            ],
            false);
    }

    // Monaco: 'getRangesToPosition #56886. Skip empty lines correctly.'
    [Fact]
    public void GetRangesToPosition_56886_SkipEmptyLines()
    {
        AssertGetRangesToPosition(
            ["function a(bar, foo){", "\tif (bar) {", string.Empty, "\t}", "}"],
            3,
            1,
            [
                new Range(1, 1, 5, 2),
                new Range(1, 21, 5, 2),
                new Range(1, 22, 5, 1),
                new Range(2, 1, 4, 3),
                new Range(2, 1, 4, 3),
                new Range(2, 2, 4, 3),
                new Range(2, 11, 4, 3),
                new Range(2, 12, 4, 2),
            ]);
    }

    // Monaco: 'getRangesToPosition #56886. Do not skip lines with only whitespaces.'
    [Fact]
    public void GetRangesToPosition_56886_DoNotSkipWhitespaceOnlyLines()
    {
        AssertGetRangesToPosition(
            ["function a(bar, foo){", "\tif (bar) {", " ", "\t}", "}"],
            3,
            1,
            [
                new Range(1, 1, 5, 2), // all
                new Range(1, 21, 5, 2), // {} outside
                new Range(1, 22, 5, 1), // {} inside
                new Range(2, 1, 4, 3),
                new Range(2, 1, 4, 3),
                new Range(2, 2, 4, 3),
                new Range(2, 11, 4, 3),
                new Range(2, 12, 4, 2),
                new Range(3, 1, 3, 2), // block
                new Range(3, 1, 3, 2), // empty line
            ]);
    }

    // Monaco: 'getRangesToPosition #40658. Cursor at first position inside brackets should select line inside.'
    [Fact]
    public void GetRangesToPosition_40658_CursorAtFirstPositionInsideBrackets()
    {
        AssertGetRangesToPosition(
            [" [ ]", " { } ", "( ) "],
            2,
            3,
            [
                new Range(1, 1, 3, 5),
                new Range(2, 1, 2, 6), // line w/ triava
                new Range(2, 2, 2, 5), // {} inside, line w/o triva
                new Range(2, 3, 2, 4), // {} inside
            ]);
    }

    // Monaco: 'getRangesToPosition #40658. Cursor in empty brackets should reveal brackets first.'
    [Fact]
    public void GetRangesToPosition_40658_CursorInEmptyBrackets()
    {
        AssertGetRangesToPosition(
            [" [] ", " { } ", "  ( ) "],
            1,
            3,
            [
                new Range(1, 1, 3, 7), // all
                new Range(1, 1, 1, 5), // line w/ trival
                new Range(1, 2, 1, 4), // [] outside, line w/o trival
                new Range(1, 3, 1, 3), // [] inside
            ]);
    }

    // Monaco: 'getRangesToPosition #40658. Tokens before bracket will be revealed first.'
    [Fact]
    public void GetRangesToPosition_40658_TokensBeforeBracket()
    {
        AssertGetRangesToPosition(
            ["  [] ", " { } ", "selectthis( ) "],
            3,
            11,
            [
                new Range(1, 1, 3, 15), // all
                new Range(3, 1, 3, 15), // line w/ trivia
                new Range(3, 1, 3, 14), // line w/o trivia
                new Range(3, 1, 3, 11), // word
            ]);
    }

    // Monaco: 'bracket selection'
    [Fact]
    public void BracketSelection()
    {
        var provider = new BracketSelectionRangeProvider();
        AssertRanges(provider, "(|)", new Range(1, 2, 1, 2), new Range(1, 1, 1, 3));

        AssertRanges(
            provider,
            "[[[](|)]]",
            new Range(1, 6, 1, 6),
            new Range(1, 5, 1, 7), // ()
            new Range(1, 3, 1, 7),
            new Range(1, 2, 1, 8), // [[]()]
            new Range(1, 2, 1, 8),
            new Range(1, 1, 1, 9)); // [[[]()]]

        AssertRanges(
            provider,
            "[a[](|)a]",
            new Range(1, 6, 1, 6),
            new Range(1, 5, 1, 7),
            new Range(1, 2, 1, 8),
            new Range(1, 1, 1, 9));

        // no bracket
        AssertRanges(provider, "fofof|fofo");

        // empty
        AssertRanges(provider, "[[[]()]]|");
        AssertRanges(provider, "|[[[]()]]");

        // edge
        AssertRanges(provider, "[|[[]()]]", new Range(1, 2, 1, 8), new Range(1, 1, 1, 9));
        AssertRanges(provider, "[[[]()]|]", new Range(1, 2, 1, 8), new Range(1, 1, 1, 9));

        AssertRanges(provider, "aaa(aaa)bbb(b|b)ccc(ccc)", new Range(1, 13, 1, 15), new Range(1, 12, 1, 16));
        AssertRanges(
            provider,
            "(aaa(aaa)bbb(b|b)ccc(ccc))",
            new Range(1, 14, 1, 16),
            new Range(1, 13, 1, 17),
            new Range(1, 2, 1, 25),
            new Range(1, 1, 1, 26));
    }

    // Monaco: 'bracket with leading/trailing'
    [Fact]
    public void BracketWithLeadingTrailing()
    {
        var provider = new BracketSelectionRangeProvider();
        AssertRanges(
            provider,
            "for(a of b){\n  foo(|);\n}",
            new Range(2, 7, 2, 7),
            new Range(2, 6, 2, 8),
            new Range(1, 13, 3, 1),
            new Range(1, 12, 3, 2),
            new Range(1, 1, 3, 2),
            new Range(1, 1, 3, 2));

        AssertRanges(
            provider,
            "for(a of b)\n{\n  foo(|);\n}",
            new Range(3, 7, 3, 7),
            new Range(3, 6, 3, 8),
            new Range(2, 2, 4, 1),
            new Range(2, 1, 4, 2),
            new Range(1, 1, 4, 2),
            new Range(1, 1, 4, 2));
    }

    // Monaco: 'in-word ranges'
    [Fact]
    public void InWordRanges()
    {
        var provider = new WordSelectionRangeProvider();
        foreach (string text in new[] { "f|ooBar", "f|oo_Ba", "f|oo-Ba" })
        {
            AssertRanges(provider, text, new Range(1, 1, 1, 4), new Range(1, 1, 1, 7), new Range(1, 1, 1, 7));
        }
    }

    // Monaco: 'in-word ranges with selectSubwords=false'
    [Fact]
    public void InWordRanges_SelectSubwordsFalse()
    {
        var provider = new WordSelectionRangeProvider(false);
        foreach (string text in new[] { "f|ooBar", "f|oo_Ba", "f|oo-Ba" })
        {
            AssertRanges(provider, text, new Range(1, 1, 1, 7), new Range(1, 1, 1, 7));
        }
    }

    // Monaco: 'Default selection should select current word/hump first in camelCase #67493'
    [Fact]
    public void DefaultSelection_CamelCase_67493()
    {
        var provider = new WordSelectionRangeProvider();
        AssertRanges(provider, "Abs|tractSmartSelect", new Range(1, 1, 1, 9), new Range(1, 1, 1, 20), new Range(1, 1, 1, 20));
        foreach (string text in new[] { "AbstractSma|rtSelect", "Abstrac-Sma|rt-elect", "Abstrac_Sma|rt_elect", "Abstrac_Sma|rt-elect", "Abstrac_Sma|rtSelect" })
        {
            AssertRanges(provider, text, new Range(1, 9, 1, 14), new Range(1, 1, 1, 20), new Range(1, 1, 1, 20));
        }
    }

    // Monaco: 'Smart select: only add line ranges if they're contained by the next range #73850' — needs a registered provider (later)

    // Monaco: 'Expand selection in words with underscores is inconsistent #90589'
    [Fact]
    public void ExpandSelectionInWordsWithUnderscores_90589()
    {
        var provider = new WordSelectionRangeProvider();
        AssertRanges(provider, "Hel|lo_World", new Range(1, 1, 1, 6), new Range(1, 1, 1, 12), new Range(1, 1, 1, 12));
        AssertRanges(provider, "Hello_Wo|rld", new Range(1, 7, 1, 12), new Range(1, 1, 1, 12), new Range(1, 1, 1, 12));
        AssertRanges(provider, "Hello|_World", new Range(1, 1, 1, 6), new Range(1, 1, 1, 12), new Range(1, 1, 1, 12));
        AssertRanges(provider, "Hello_|World", new Range(1, 7, 1, 12), new Range(1, 1, 1, 12), new Range(1, 1, 1, 12));
        AssertRanges(provider, "Hello|-World", new Range(1, 1, 1, 6), new Range(1, 1, 1, 12), new Range(1, 1, 1, 12));
        AssertRanges(provider, "Hello-|World", new Range(1, 7, 1, 12), new Range(1, 1, 1, 12), new Range(1, 1, 1, 12));
        AssertRanges(provider, "Hello|World", new Range(1, 6, 1, 11), new Range(1, 1, 1, 11), new Range(1, 1, 1, 11));
    }

    private void Expand() => Assert.True(_context.Commands.Execute(SmartSelectCommands.Expand));

    private void Shrink() => Assert.True(_context.Commands.Execute(SmartSelectCommands.Shrink));

    [Fact]
    public void Expand_Repeatedly_GrowsToWholeDocument()
    {
        SetText("foo bar\nbaz");
        _context.Selection = Selection.Collapsed(new Position(1, 2));
        Expand();
        Assert.Equal(new Selection(1, 1, 1, 4), _context.Selection);
        Expand();
        Assert.Equal(new Selection(1, 1, 1, 8), _context.Selection); // the line (with trivia, same as the line here)
        Expand();
        Assert.Equal(new Selection(1, 1, 2, 4), _context.Selection);
        Expand();
        Assert.Equal(new Selection(1, 1, 2, 4), _context.Selection);
    }

    [Fact]
    public void Shrink_AfterExpand_ReturnsToPreviousSelections()
    {
        SetText("foo bar\nbaz");
        _context.Selection = Selection.Collapsed(new Position(1, 2));
        Expand();
        Expand();
        Expand();
        Shrink();
        Assert.Equal(new Selection(1, 1, 1, 8), _context.Selection);
        Shrink();
        Assert.Equal(new Selection(1, 1, 1, 4), _context.Selection);
        Shrink();
        Assert.Equal(Selection.Collapsed(new Position(1, 2)), _context.Selection);
        Shrink();
        Assert.Equal(Selection.Collapsed(new Position(1, 2)), _context.Selection);
    }

    [Fact]
    public void Expand_MultiCursor_EachGrowsIndependently()
    {
        SetText("foo bar\nbaz qux");
        _context.SetSelections([Selection.Collapsed(new Position(1, 2)), Selection.Collapsed(new Position(2, 6))]);
        Expand();
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 5, 2, 8)], _context.Selections);
        Expand();
        Assert.Equal([new Selection(1, 1, 1, 8), new Selection(2, 1, 2, 8)], _context.Selections);
    }

    [Fact]
    public void CaretMove_ForgetsState()
    {
        SetText("foo bar baz");
        _context.Selection = Selection.Collapsed(new Position(1, 2));
        Expand();
        Assert.Equal(new Selection(1, 1, 1, 4), _context.Selection);
        _context.Selection = Selection.Collapsed(new Position(1, 6));
        Expand();
        Assert.Equal(new Selection(1, 5, 1, 8), _context.Selection);
        Shrink();
        Assert.Equal(Selection.Collapsed(new Position(1, 6)), _context.Selection);
    }

    [Fact]
    public void Expand_InsideBrackets_SelectsInnerThenWithBrackets()
    {
        SetText("a(b + c)d");
        _context.Selection = Selection.Collapsed(new Position(1, 5));
        Expand();
        Assert.Equal(new Selection(1, 3, 1, 8), _context.Selection);
        Expand();
        Assert.Equal(new Selection(1, 2, 1, 9), _context.Selection);
    }

    [Fact]
    public void Expand_CamelCaseWord_SelectsSubwordThenWord()
    {
        SetText("fooBarBaz");
        _context.Selection = Selection.Collapsed(new Position(1, 5));
        Expand();
        Assert.Equal(new Selection(1, 4, 1, 7), _context.Selection);
        Expand();
        Assert.Equal(new Selection(1, 1, 1, 10), _context.Selection);
    }

    [Fact]
    public void Expand_SnakeCaseWord_SelectsPartThenWord()
    {
        SetText("foo_bar_baz");
        _context.Selection = Selection.Collapsed(new Position(1, 6));
        Expand();
        Assert.Equal(new Selection(1, 5, 1, 8), _context.Selection);
        Expand();
        Assert.Equal(new Selection(1, 1, 1, 12), _context.Selection);
    }

    [Fact]
    public void Expand_WhitespaceOnlyLine_SelectsTheLine()
    {
        SetText("a\n   \nb");
        _context.Selection = Selection.Collapsed(new Position(2, 2));
        Expand();
        Assert.Equal(new Selection(2, 1, 2, 4), _context.Selection);
        Expand();
        Assert.Equal(new Selection(1, 1, 3, 2), _context.Selection);
    }

    [Fact]
    public void ProvideSelectionRanges_EqualRangesCollapsed()
    {
        TextModel model = SetText("foo");
        List<Range> ranges = SmartSelectRanges.Provide(model, [new Position(1, 2)], new SmartSelectOptions())[0];
        Assert.Equal([new Range(1, 1, 1, 4)], ranges);
    }

    [Fact]
    public void Keybindings_ResolveToTheTwoCommands()
    {
        var focused = new KeybindingContext(TextInputFocus: true, EditorTextFocus: true);
        var dispatcher = new KeybindingDispatcher(KeybindingResolver.Default);

        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.RightArrow), focused, out string? expand));
        Assert.Equal(SmartSelectCommands.Expand, expand);
        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Shift | KeyModifiers.Alt, KeyCode.LeftArrow), focused, out string? shrink));
        Assert.Equal(SmartSelectCommands.Shrink, shrink);
    }
}
