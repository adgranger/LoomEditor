// Ported from microsoft/vscode: src/vs/editor/test/common/modes/supports/richEditBrackets.test.ts (suite 'richEditBrackets')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Languages.Supports;

// All the Monaco tests are ported; the last tests are new.
public class RichEditBracketsTests
{
    private static Regex R(string pattern) => new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static Range? FindPrevBracketInRange(Regex reversedBracketRegex, string lineText, int currentTokenStart, int currentTokenEnd) =>
        BracketsUtils.FindPrevBracketInRange(reversedBracketRegex, 1, lineText, currentTokenStart, currentTokenEnd);

    private static Range? FindNextBracketInRange(Regex forwardBracketRegex, string lineText, int currentTokenStart, int currentTokenEnd) =>
        BracketsUtils.FindNextBracketInRange(forwardBracketRegex, 1, lineText, currentTokenStart, currentTokenEnd);

    private static void AssertColumns(Range? result, int startColumn, int endColumn)
    {
        Assert.NotNull(result);
        Assert.Equal(startColumn, result.Value.StartColumn);
        Assert.Equal(endColumn, result.Value.EndColumn);
    }

    // Monaco: 'findPrevBracketInToken one char 1'
    [Fact]
    public void FindPrevBracketInToken_OneChar1() => AssertColumns(FindPrevBracketInRange(R(@"(\{)|(\})"), "{", 0, 1), 1, 2);

    // Monaco: 'findPrevBracketInToken one char 2'
    [Fact]
    public void FindPrevBracketInToken_OneChar2() => AssertColumns(FindPrevBracketInRange(R(@"(\{)|(\})"), "{{", 0, 1), 1, 2);

    // Monaco: 'findPrevBracketInToken one char 3'
    [Fact]
    public void FindPrevBracketInToken_OneChar3() => AssertColumns(FindPrevBracketInRange(R(@"(\{)|(\})"), "{hello world!", 0, 13), 1, 2);

    // Monaco: 'findPrevBracketInToken more chars 1'
    [Fact]
    public void FindPrevBracketInToken_MoreChars1() => AssertColumns(FindPrevBracketInRange(R("(olleh)"), "hello world!", 0, 12), 1, 6);

    // Monaco: 'findPrevBracketInToken more chars 2'
    [Fact]
    public void FindPrevBracketInToken_MoreChars2() => AssertColumns(FindPrevBracketInRange(R("(olleh)"), "hello world!", 0, 5), 1, 6);

    // Monaco: 'findPrevBracketInToken more chars 3'
    [Fact]
    public void FindPrevBracketInToken_MoreChars3() => AssertColumns(FindPrevBracketInRange(R("(olleh)"), " hello world!", 0, 6), 2, 7);

    // Monaco: 'findNextBracketInToken one char'
    [Fact]
    public void FindNextBracketInToken_OneChar() => AssertColumns(FindNextBracketInRange(R(@"(\{)|(\})"), "{", 0, 1), 1, 2);

    // Monaco: 'findNextBracketInToken more chars'
    [Fact]
    public void FindNextBracketInToken_MoreChars() => AssertColumns(FindNextBracketInRange(R("(world)"), "hello world!", 0, 12), 7, 12);

    // Monaco: 'findNextBracketInToken with emoty result'
    [Fact]
    public void FindNextBracketInToken_WithEmptyResult() => Assert.Null(FindNextBracketInRange(R(@"(\{)|(\})"), "", 0, 0));

    // Monaco: 'issue #3894: [Handlebars] Curly braces edit issues'
    [Fact]
    public void Issue3894_HandlebarsCurlyBracesEditIssues() =>
        AssertColumns(FindPrevBracketInRange(R(@"(\-\-!<)|(>\-\-)|(\{\{)|(\}\})"), "{{asd}}", 0, 2), 1, 3);

    [Fact]
    public void EscapeRegExpCharacters_EscapesLikeJavaScript()
    {
        Assert.Equal(@"\\\{\}\*\+\?\|\^\$\.\[\]\(\)", Strings.EscapeRegExpCharacters(@"\{}*+?|^$.[]()"));
        Assert.Equal("a-b#c<>/", Strings.EscapeRegExpCharacters("a-b#c<>/"));
    }

    [Fact]
    public void RichEditBrackets_GroupsBracketsWithTheSameOpenOrClose()
    {
        var brackets = new RichEditBrackets("test", [new CharacterPair("IF", "END"), new CharacterPair("for", "end"), new CharacterPair("{", "}")]);

        Assert.Equal(2, brackets.Brackets.Count);
        Assert.Equal(["if", "for"], brackets.Brackets[0].Open);
        Assert.Equal(["end", "end"], brackets.Brackets[0].Close);
        Assert.Equal(["{"], brackets.Brackets[1].Open);
        Assert.Equal(1, brackets.Brackets[1].Index);
        Assert.True(brackets.TextIsOpenBracket["for"]);
        Assert.False(brackets.TextIsOpenBracket["end"]);
        Assert.Same(brackets.Brackets[0], brackets.TextIsBracket["if"]);
        Assert.Equal(3, brackets.MaxBracketLength);
    }

    [Fact]
    public void CreateBracketOrRegExp_WordBrackets_GetWordBoundaries()
    {
        Regex regex = BracketsUtils.CreateBracketOrRegExp(["begin", "{"]);

        Assert.Equal(@"(\bbegin\b)|(\{)", regex.ToString());
        Assert.DoesNotMatch(regex, "beginning");
    }

    [Fact]
    public void CreateBracketOrRegExp_IsCaseInsensitive() =>
        Assert.Matches(BracketsUtils.CreateBracketOrRegExp(["begin"]), "BEGIN");

    [Fact]
    public void ForwardRegexForBracketPair_IncludesSuperstrings()
    {
        var brackets = new RichEditBrackets("test", [new CharacterPair("begin", "end"), new CharacterPair("if", "end if")]);

        // "end" is in "end if": the regex of the first pair also matches "end if", longest first.
        Assert.Equal(2, brackets.Brackets.Count);
        Assert.Equal(@"(\bend if\b)|(\bbegin\b)|(\bend\b)", brackets.Brackets[0].ForwardRegex.ToString());
        Assert.Equal(@"(\bfi dne\b)|(\bnigeb\b)|(\bdne\b)", brackets.Brackets[0].ReversedRegex.ToString());
    }
}
