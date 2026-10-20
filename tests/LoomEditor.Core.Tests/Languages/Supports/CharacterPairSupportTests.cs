// Ported from microsoft/vscode: src/vs/editor/test/common/modes/supports/characterPair.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Languages.Supports;

public class CharacterPairSupportTests
{
    private static readonly (string Open, string Close)[] Ab = [("a", "b")];

    private static (string Open, string Close)[] Pairs(IEnumerable<IAutoClosingPair> pairs) => [.. pairs.Select(p => (p.Open, p.Close))];

    private static bool TestShouldAutoClose(CharacterPairSupport characterPairSupport, TokenText[] line, int column)
    {
        StandardAutoClosingPairConditional autoClosingPair = characterPairSupport.GetAutoClosingPairs()[0];
        return autoClosingPair.ShouldAutoClose(ModesTestUtils.CreateFakeScopedLineTokens(line), column);
    }

    private static CharacterPairSupport CurlyNotInStringOrComment() =>
        new(new LanguageConfiguration { AutoClosingPairs = [new AutoClosingPairConditional("{", "}", ["string", "comment"])] });

    // Monaco: 'only autoClosingPairs'
    [Fact]
    public void OnlyAutoClosingPairs()
    {
        var characaterPairSupport = new CharacterPairSupport(new LanguageConfiguration { AutoClosingPairs = [new AutoClosingPairConditional("a", "b")] });
        Assert.Equal(Ab, Pairs(characaterPairSupport.GetAutoClosingPairs()));
        Assert.Equal(Ab, Pairs(characaterPairSupport.GetSurroundingPairs()));
    }

    // Monaco: 'only empty autoClosingPairs'
    [Fact]
    public void OnlyEmptyAutoClosingPairs()
    {
        var characaterPairSupport = new CharacterPairSupport(new LanguageConfiguration { AutoClosingPairs = [] });
        Assert.Empty(characaterPairSupport.GetAutoClosingPairs());
        Assert.Empty(characaterPairSupport.GetSurroundingPairs());
    }

    // Monaco: 'only brackets'
    [Fact]
    public void OnlyBrackets()
    {
        var characaterPairSupport = new CharacterPairSupport(new LanguageConfiguration { Brackets = [new CharacterPair("a", "b")] });
        Assert.Equal(Ab, Pairs(characaterPairSupport.GetAutoClosingPairs()));
        Assert.Equal(Ab, Pairs(characaterPairSupport.GetSurroundingPairs()));
    }

    // Monaco: 'only empty brackets'
    [Fact]
    public void OnlyEmptyBrackets()
    {
        var characaterPairSupport = new CharacterPairSupport(new LanguageConfiguration { Brackets = [] });
        Assert.Empty(characaterPairSupport.GetAutoClosingPairs());
        Assert.Empty(characaterPairSupport.GetSurroundingPairs());
    }

    // Monaco: 'only surroundingPairs'
    [Fact]
    public void OnlySurroundingPairs()
    {
        var characaterPairSupport = new CharacterPairSupport(new LanguageConfiguration { SurroundingPairs = [new AutoClosingPair("a", "b")] });
        Assert.Empty(characaterPairSupport.GetAutoClosingPairs());
        Assert.Equal(Ab, Pairs(characaterPairSupport.GetSurroundingPairs()));
    }

    // Monaco: 'only empty surroundingPairs'
    [Fact]
    public void OnlyEmptySurroundingPairs()
    {
        var characaterPairSupport = new CharacterPairSupport(new LanguageConfiguration { SurroundingPairs = [] });
        Assert.Empty(characaterPairSupport.GetAutoClosingPairs());
        Assert.Empty(characaterPairSupport.GetSurroundingPairs());
    }

    // Monaco: 'brackets is ignored when having autoClosingPairs'
    [Fact]
    public void BracketsIsIgnoredWhenHavingAutoClosingPairs()
    {
        var characaterPairSupport = new CharacterPairSupport(new LanguageConfiguration { AutoClosingPairs = [], Brackets = [new CharacterPair("a", "b")] });
        Assert.Empty(characaterPairSupport.GetAutoClosingPairs());
        Assert.Empty(characaterPairSupport.GetSurroundingPairs());
    }

    // Monaco: 'shouldAutoClosePair in empty line'
    [Fact]
    public void ShouldAutoClosePair_InEmptyLine()
    {
        CharacterPairSupport sup = CurlyNotInStringOrComment();
        TokenText[] tokenText = [];
        Assert.True(TestShouldAutoClose(sup, tokenText, 1));
    }

    // Monaco: 'shouldAutoClosePair in not interesting line 1'
    [Fact]
    public void ShouldAutoClosePair_InNotInterestingLine1()
    {
        CharacterPairSupport sup = CurlyNotInStringOrComment();
        TokenText[] tokenText = [new("do", StandardTokenType.Other)];
        Assert.True(TestShouldAutoClose(sup, tokenText, 3));
    }

    // Monaco: 'shouldAutoClosePair in not interesting line 2'
    [Fact]
    public void ShouldAutoClosePair_InNotInterestingLine2()
    {
        var sup = new CharacterPairSupport(new LanguageConfiguration { AutoClosingPairs = [new AutoClosingPairConditional("{", "}")] });
        TokenText[] tokenText = [new("do", StandardTokenType.String)];
        Assert.True(TestShouldAutoClose(sup, tokenText, 3));
    }

    // Monaco: 'shouldAutoClosePair in interesting line 1'
    [Fact]
    public void ShouldAutoClosePair_InInterestingLine1()
    {
        CharacterPairSupport sup = CurlyNotInStringOrComment();
        TokenText[] tokenText = [new("\"a\"", StandardTokenType.String)];
        Assert.False(TestShouldAutoClose(sup, tokenText, 1));
        Assert.False(TestShouldAutoClose(sup, tokenText, 2));
        Assert.False(TestShouldAutoClose(sup, tokenText, 3));
        Assert.False(TestShouldAutoClose(sup, tokenText, 4));
    }

    // Monaco: 'shouldAutoClosePair in interesting line 2'
    [Fact]
    public void ShouldAutoClosePair_InInterestingLine2()
    {
        CharacterPairSupport sup = CurlyNotInStringOrComment();
        TokenText[] tokenText =
        [
            new("x=", StandardTokenType.Other),
            new("\"a\"", StandardTokenType.String),
            new(";", StandardTokenType.Other),
        ];
        Assert.True(TestShouldAutoClose(sup, tokenText, 1));
        Assert.True(TestShouldAutoClose(sup, tokenText, 2));
        Assert.True(TestShouldAutoClose(sup, tokenText, 3));
        Assert.False(TestShouldAutoClose(sup, tokenText, 4));
        Assert.False(TestShouldAutoClose(sup, tokenText, 5));
        Assert.False(TestShouldAutoClose(sup, tokenText, 6));
        Assert.True(TestShouldAutoClose(sup, tokenText, 7));
    }

    // Monaco: 'shouldAutoClosePair in interesting line 3'
    [Fact]
    public void ShouldAutoClosePair_InInterestingLine3()
    {
        CharacterPairSupport sup = CurlyNotInStringOrComment();
        TokenText[] tokenText =
        [
            new(" ", StandardTokenType.Other),
            new("//a", StandardTokenType.Comment),
        ];
        Assert.True(TestShouldAutoClose(sup, tokenText, 1));
        Assert.True(TestShouldAutoClose(sup, tokenText, 2));
        Assert.False(TestShouldAutoClose(sup, tokenText, 3));
        Assert.False(TestShouldAutoClose(sup, tokenText, 4));
        Assert.False(TestShouldAutoClose(sup, tokenText, 5));
    }
}
