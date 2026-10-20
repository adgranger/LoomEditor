// Ported from microsoft/vscode: src/vs/editor/test/common/model/textModelWithTokens.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.BracketPairs;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.BracketPairs;

// Monaco runs `brackets1` on the bracket pair tree only. Every test here is a [Theory] with `bool useTree`: `true` is
// Monaco's path, `false` sets `MaxSupportedDocumentLength = 0` so that the model uses the legacy regex search.
public class TextModelWithTokensBracketsTests
{
    private static readonly CharacterPair[] Brackets = [new("{", "}"), new("[", "]"), new("(", ")")];

    private static void SelectPath(TextModel model, bool useTree)
    {
        if (!useTree)
        {
            model.BracketPairs.MaxSupportedDocumentLength = 0;
        }
    }

    // Monaco: 'TextModelWithTokens' > 'brackets1' (helpers `testBrackets` and `toRelaxedFoundBracket`)
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Brackets1(bool useTree)
    {
        string[] contents = ["if (a == 3) { return (7 * (a + 5)); }"];
        var test = BracketPairsTestModels.Create(string.Join('\n', contents), Brackets);
        var model = test.Model;
        SelectPath(model, useTree);
        var bracketsNew = test.Configurations.GetLanguageConfiguration(BracketPairsTestModels.LanguageId).BracketsNew;

        static (string Range, BracketKind Info)? ToRelaxed(FoundBracket? a) => a is { } found ? (found.Range.ToString(), found.BracketInfo) : null;

        var expectedBrackets = new List<FoundBracket>();
        for (int lineIndex = 0; lineIndex < contents.Length; lineIndex++)
        {
            string lineText = contents[lineIndex];
            for (int charIndex = 0; charIndex < lineText.Length; charIndex++)
            {
                string ch = lineText[charIndex].ToString();
                if (Brackets.Any(b => b.Open == ch || b.Close == ch))
                {
                    expectedBrackets.Add(new FoundBracket(
                        new Range(lineIndex + 1, charIndex + 1, lineIndex + 1, charIndex + 2),
                        bracketsNew.GetBracketInfo(ch)!));
                }
            }
        }

        // findPrevBracket
        {
            int expectedBracketIndex = expectedBrackets.Count - 1;
            FoundBracket? currentExpectedBracket = expectedBracketIndex >= 0 ? expectedBrackets[expectedBracketIndex] : null;
            for (int lineNumber = contents.Length; lineNumber >= 1; lineNumber--)
            {
                string lineText = contents[lineNumber - 1];
                for (int column = lineText.Length + 1; column >= 1; column--)
                {
                    if (currentExpectedBracket is { } expected
                        && lineNumber == expected.Range.StartLineNumber && column < expected.Range.EndColumn)
                    {
                        expectedBracketIndex--;
                        currentExpectedBracket = expectedBracketIndex >= 0 ? expectedBrackets[expectedBracketIndex] : null;
                    }

                    var actual = model.BracketPairs.FindPrevBracket(new Position(lineNumber, column));

                    Assert.True(ToRelaxed(actual).Equals(ToRelaxed(currentExpectedBracket)), $"findPrevBracket of {lineNumber}, {column}");
                }
            }
        }

        // findNextBracket
        {
            int expectedBracketIndex = 0;
            FoundBracket? currentExpectedBracket = expectedBracketIndex < expectedBrackets.Count ? expectedBrackets[expectedBracketIndex] : null;
            for (int lineNumber = 1; lineNumber <= contents.Length; lineNumber++)
            {
                string lineText = contents[lineNumber - 1];
                for (int column = 1; column <= lineText.Length + 1; column++)
                {
                    if (currentExpectedBracket is { } expected
                        && lineNumber == expected.Range.StartLineNumber && column > expected.Range.StartColumn)
                    {
                        expectedBracketIndex++;
                        currentExpectedBracket = expectedBracketIndex < expectedBrackets.Count ? expectedBrackets[expectedBracketIndex] : null;
                    }

                    var actual = model.BracketPairs.FindNextBracket(new Position(lineNumber, column));

                    Assert.True(ToRelaxed(actual).Equals(ToRelaxed(currentExpectedBracket)), $"findNextBracket of {lineNumber}, {column}");
                }
            }
        }
    }

    [Fact]
    public void FindEnclosingBrackets_Legacy_NestedPairs_ReturnsInnermost()
    {
        var model = BracketPairsTestModels.Create("{ [ a ] ( ) } (", Brackets).Model;
        model.BracketPairs.MaxSupportedDocumentLength = 0;
        // Like Monaco, the legacy search answers the closing bracket it found first, then the opening one
        // (the tree path answers opening first).
        (Range, Range)? bracket = (new Range(1, 7, 1, 8), new Range(1, 3, 1, 4));
        (Range, Range)? brace = (new Range(1, 13, 1, 14), new Range(1, 1, 1, 2));

        Assert.Equal(bracket, model.BracketPairs.FindEnclosingBrackets(new Position(1, 5)));
        Assert.Equal(brace, model.BracketPairs.FindEnclosingBrackets(new Position(1, 8)));
        Assert.Equal(brace, model.BracketPairs.FindEnclosingBrackets(new Position(1, 2)));
        Assert.Null(model.BracketPairs.FindEnclosingBrackets(new Position(1, 1)));
        Assert.Null(model.BracketPairs.FindEnclosingBrackets(new Position(1, 16)));
    }

    [Fact]
    public void FindNextBracket_Legacy_SkipsBracketsInStrings()
    {
        const string mode = "testMode";
        const string text = "\"(\" )";
        var test = TestLanguageModels.Create(
            text,
            mode,
            new LanguageConfiguration { Brackets = [new("(", ")")] },
            codec =>
            {
                uint Metadata(StandardTokenType type) =>
                    ((uint)codec.EncodeLanguageId(mode) << MetadataConsts.LanguageIdOffset)
                    | ((uint)type << MetadataConsts.TokenTypeOffset);
                return new LineTokensSupport(new Dictionary<string, uint[]>
                {
                    [text] = [0, Metadata(StandardTokenType.String), 3, Metadata(StandardTokenType.Other)],
                });
            });
        var model = test.Model;
        model.BracketPairs.MaxSupportedDocumentLength = 0;
        model.Tokenization.ForceTokenization(1);

        var next = model.BracketPairs.FindNextBracket(new Position(1, 1));

        Assert.Equal(new Range(1, 5, 1, 6), next?.Range);
        Assert.Null(model.BracketPairs.FindNextBracket(new Position(1, 6)));
    }

    [Fact]
    public void FindPrevBracket_Legacy_TwoLanguages_UsesLanguageOfEachToken()
    {
        const string mode1 = "testMode1";
        const string mode2 = "testMode2";
        const string text = "(x(y";
        var test = TestLanguageModels.Create(
            text,
            mode1,
            new LanguageConfiguration { Brackets = [new("(", ")")] },
            codec =>
            {
                uint Other(string mode) =>
                    ((uint)codec.EncodeLanguageId(mode) << MetadataConsts.LanguageIdOffset)
                    | ((uint)StandardTokenType.Other << MetadataConsts.TokenTypeOffset);
                return new LineTokensSupport(new Dictionary<string, uint[]>
                {
                    [text] = [0, Other(mode1), 2, Other(mode2)],
                });
            },
            mode2);
        // `testMode2` only knows square brackets: the `(` of its token is not a bracket.
        test.Configurations.Register(mode2, new LanguageConfiguration { Brackets = [new("[", "]")] });
        var model = test.Model;
        model.BracketPairs.MaxSupportedDocumentLength = 0;
        model.Tokenization.ForceTokenization(1);

        var prev = model.BracketPairs.FindPrevBracket(new Position(1, 5));

        Assert.Equal(new Range(1, 1, 1, 2), prev?.Range);
    }
}
