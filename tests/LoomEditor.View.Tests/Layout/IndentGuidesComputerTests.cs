using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Guides;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no test for IndentGuidesOverlay.getGuidesByLine: all tests are new.
public class IndentGuidesComputerTests
{
    private static readonly EditorGuidesOptions IndentOnly = new();
    private static readonly EditorGuidesOptions WithBrackets = new() { BracketPairs = GuideMode.On };

    private static ViewModel Create(string text, bool brackets = false)
    {
        var codec = new LanguageIdCodec();
        codec.Register("brackets");
        var configurations = new LanguageConfigurationRegistry();
        configurations.Register("brackets", new LanguageConfiguration { Brackets = [new("{", "}"), new("(", ")")] });
        var model = new TextModel(text, DefaultEndOfLine.LF, null, new TokenizationRegistry(), codec, null, configurations);
        if (brackets)
        {
            model.Tokenization.SetLanguageId("brackets");
        }

        model.UpdateOptions(new TextModelUpdateOptions(TabSize: 4, IndentSize: 4));
        return new ViewModel(model, FontInfo.Default, new EditorLayoutOptions(), new ViewLayoutOptions(20), new FakeFrameScheduler().Schedule, new FakeTimeProvider(), 0);
    }

    private static string[] ClassNames(IReadOnlyList<IndentGuide> guides) => [.. guides.Select(g => g.ClassName)];

    [Fact]
    public void GetGuidesByLine_BothDisabled_ReturnsNull()
    {
        using ViewModel viewModel = Create("{\n\ta\n}");
        var options = new EditorGuidesOptions { Indentation = false, BracketPairs = GuideMode.Off };

        Assert.Null(IndentGuidesComputer.GetGuidesByLine(viewModel, options, 1, 3, null));
    }

    [Fact]
    public void GetGuidesByLine_IndentOnly_OneGuidePerLevel()
    {
        using ViewModel viewModel = Create("{\n\t\ta\n}");

        var result = IndentGuidesComputer.GetGuidesByLine(viewModel, IndentOnly, 1, 3, null)!;

        Assert.Empty(result[0]);
        Assert.Equal([1, 5], result[1].Select(g => g.VisibleColumn));
        Assert.Equal(["core-guide-indent lvl-0", "core-guide-indent lvl-1"], ClassNames(result[1]));
        Assert.Empty(result[2]);
    }

    [Fact]
    public void GetGuidesByLine_ActiveIndent_IsMarkedOnlyInTheActiveBlock()
    {
        using ViewModel viewModel = Create("a\n{\n\tb\n\tc\n}\n{\n\td\n}");

        var result = IndentGuidesComputer.GetGuidesByLine(viewModel, IndentOnly, 1, 8, new Position(3, 2))!;

        Assert.Equal(["core-guide-indent lvl-0 indent-active"], ClassNames(result[2]));
        Assert.Equal(["core-guide-indent lvl-0 indent-active"], ClassNames(result[3]));
        Assert.Equal(["core-guide-indent lvl-0"], ClassNames(result[6]));
    }

    [Fact]
    public void GetGuidesByLine_BracketGuideAtTheSameColumn_ReplacesTheIndentGuide()
    {
        using ViewModel viewModel = Create("{\n    a\n}", brackets: true);

        var result = IndentGuidesComputer.GetGuidesByLine(viewModel, WithBrackets, 1, 3, null)!;

        IndentGuide guide = Assert.Single(result[1]);
        Assert.StartsWith("bracket-indent-guide", guide.ClassName, StringComparison.Ordinal);
        Assert.Equal(1, guide.VisibleColumn);
    }

    [Fact]
    public void GetGuidesByLine_BracketGuidesPresent_ActiveIndentNotHighlighted_UnlessAlways()
    {
        // The bracket guide sits at column 1, so the indent guide of level 2 (column 5) stays and could be active.
        using ViewModel viewModel = Create("{\n    y\n        a\n    z\n}", brackets: true);
        var cursor = new Position(3, 9);
        var options = WithBrackets with { HighlightActiveBracketPair = false };

        var on = IndentGuidesComputer.GetGuidesByLine(viewModel, options, 1, 5, cursor)!;
        var always = IndentGuidesComputer.GetGuidesByLine(
            viewModel, options with { HighlightActiveIndentation = ActiveIndentationHighlight.Always }, 1, 5, cursor)!;

        Assert.DoesNotContain(on[2], g => g.ClassName.EndsWith("indent-active", StringComparison.Ordinal) && g.ClassName.StartsWith("core", StringComparison.Ordinal));
        Assert.Contains(always[2], g => g.ClassName.StartsWith("core-guide-indent", StringComparison.Ordinal) && g.ClassName.EndsWith("indent-active", StringComparison.Ordinal));
    }

    [Fact]
    public void GetGuidesByLine_BracketGuidesAfterTheIndents_AreAppended()
    {
        // The indent of line 2 is one level (column 1); the bracket guide of "(" is at visible column 5.
        using ViewModel viewModel = Create("{\n    (\n        a\n    )\n}", brackets: true);

        var result = IndentGuidesComputer.GetGuidesByLine(viewModel, WithBrackets, 1, 5, null)!;

        IReadOnlyList<IndentGuide> line3 = result[2];
        Assert.Equal([1, 5], line3.Select(g => g.VisibleColumn));
        Assert.StartsWith("bracket-indent-guide", line3[1].ClassName, StringComparison.Ordinal);
    }
}
