using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;

namespace LoomEditor.Core.Tests.Languages.Supports;

public class LanguageBracketsConfigurationTests
{
    private static LanguageBracketsConfiguration Create(CharacterPair[] brackets, CharacterPair[]? colorized = null) =>
        new("test", new LanguageConfiguration { Brackets = brackets, ColorizedBracketPairs = colorized });

    [Fact]
    public void ColorizedPairs_DefaultToTheBracketsWithoutAngleBrackets()
    {
        var config = Create([new("{", "}"), new("<", ">")]);

        Assert.Equal("{ <", string.Join(' ', config.OpeningBrackets.Select(b => b.BracketText)));
        Assert.Equal("} >", string.Join(' ', config.ClosingBrackets.Select(b => b.BracketText)));
        Assert.True(config.GetClosingBracketInfo("}")!.ClosesColorized(config.GetOpeningBracketInfo("{")!));
        Assert.True(config.GetClosingBracketInfo(">")!.Closes(config.GetOpeningBracketInfo("<")!));
        Assert.False(config.GetClosingBracketInfo(">")!.ClosesColorized(config.GetOpeningBracketInfo("<")!));
    }

    [Fact]
    public void ColorizedPairs_EmptyList_ColorizesNothing()
    {
        var config = Create([new("{", "}"), new("(", ")")], colorized: []);

        Assert.True(config.GetClosingBracketInfo("}")!.Closes(config.GetOpeningBracketInfo("{")!));
        Assert.False(config.GetClosingBracketInfo("}")!.ClosesColorized(config.GetOpeningBracketInfo("{")!));
        Assert.False(config.GetClosingBracketInfo(")")!.ClosesColorized(config.GetOpeningBracketInfo("(")!));
    }

    [Fact]
    public void ClosingBracket_ClosesOnlyItsOpeningBrackets()
    {
        var config = Create([new("{", "}"), new("(", ")"), new("begin", "end"), new("do", "end")]);
        OpeningBracketKind curly = config.GetOpeningBracketInfo("{")!;
        ClosingBracketKind end = config.GetClosingBracketInfo("end")!;

        Assert.True(config.GetClosingBracketInfo("}")!.Closes(curly));
        Assert.False(config.GetClosingBracketInfo(")")!.Closes(curly));
        Assert.Equal("begin do", string.Join(' ', end.GetOpeningBrackets().Select(b => b.BracketText)));
        Assert.Same(end, Assert.Single(config.GetOpeningBracketInfo("do")!.OpenedBrackets));
        Assert.False(end.Closes(curly));
        Assert.True(config.GetBracketInfo("end")!.LanguageId == "test" && !config.GetBracketInfo("end")!.IsOpeningBracket);
        Assert.True(config.GetBracketInfo("{")!.IsOpeningBracket);
        Assert.Null(config.GetBracketInfo("["));
    }

    [Fact]
    public void Closes_BracketOfAnotherConfiguration_IsFalse()
    {
        var a = Create([new("{", "}")]);
        var b = Create([new("{", "}")]);

        Assert.True(a.GetClosingBracketInfo("}")!.Closes(a.GetOpeningBracketInfo("{")!));
        Assert.False(a.GetClosingBracketInfo("}")!.Closes(b.GetOpeningBracketInfo("{")!));
        Assert.False(a.GetClosingBracketInfo("}")!.ClosesColorized(b.GetOpeningBracketInfo("{")!));
    }

    [Fact]
    public void EmptyOpenOrClose_IsIgnored()
    {
        var config = Create([new("", "}"), new("(", ""), new("[", "]")], colorized: [new("", ">"), new("<", "")]);

        Assert.Equal("[", Assert.Single(config.OpeningBrackets).BracketText);
        Assert.Equal("]", Assert.Single(config.ClosingBrackets).BracketText);
    }

    [Fact]
    public void GetBracketRegExp_MatchesEveryBracketIgnoringCase()
    {
        var config = Create([new("begin", "end"), new("(", ")")]);
        var regex = config.GetBracketRegExp();

        Assert.Equal("BEGIN ( ) end", string.Join(' ', regex.Matches("BEGIN ( ) end").Select(m => m.Value)));
        Assert.Matches(regex, "END");
        Assert.DoesNotMatch(regex, "beg");
    }
}
