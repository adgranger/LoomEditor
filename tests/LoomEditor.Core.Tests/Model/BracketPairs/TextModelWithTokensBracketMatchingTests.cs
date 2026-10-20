// Ported from microsoft/vscode: src/vs/editor/test/common/model/textModelWithTokens.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.BracketPairs;

// Monaco runs these tests on the bracket pair tree only. Every test here is a [Theory] with `bool useTree`: `true` is
// Monaco's path, `false` sets `MaxSupportedDocumentLength = 0` so that the model uses the legacy regex search.
public class TextModelWithTokensBracketMatchingTests
{
    private static readonly CharacterPair[] Brackets = [new("{", "}"), new("[", "]"), new("(", ")")];

    private static TextModel Create(string text, bool useTree, params CharacterPair[] brackets)
    {
        var model = BracketPairsTestModels.Create(text, brackets.Length > 0 ? brackets : Brackets).Model;
        SelectPath(model, useTree);
        return model;
    }

    private static void SelectPath(TextModel model, bool useTree)
    {
        if (!useTree)
        {
            model.BracketPairs.MaxSupportedDocumentLength = 0;
        }
    }

    // Monaco: 'TextModelWithTokens - bracket matching' > 'bracket matching 1'
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BracketMatching1(bool useTree)
    {
        var model = Create(")]}{[(\n)]}{[(", useTree);

        BracketAssert.IsNotBracket(model, 1, 1);
        BracketAssert.IsNotBracket(model, 1, 2);
        BracketAssert.IsNotBracket(model, 1, 3);
        BracketAssert.IsBracket(model, new Position(1, 4), (new Range(1, 4, 1, 5), new Range(2, 3, 2, 4)));
        BracketAssert.IsBracket(model, new Position(1, 5), (new Range(1, 5, 1, 6), new Range(2, 2, 2, 3)));
        BracketAssert.IsBracket(model, new Position(1, 6), (new Range(1, 6, 1, 7), new Range(2, 1, 2, 2)));
        BracketAssert.IsBracket(model, new Position(1, 7), (new Range(1, 6, 1, 7), new Range(2, 1, 2, 2)));

        BracketAssert.IsBracket(model, new Position(2, 1), (new Range(2, 1, 2, 2), new Range(1, 6, 1, 7)));
        BracketAssert.IsBracket(model, new Position(2, 2), (new Range(2, 2, 2, 3), new Range(1, 5, 1, 6)));
        BracketAssert.IsBracket(model, new Position(2, 3), (new Range(2, 3, 2, 4), new Range(1, 4, 1, 5)));
        BracketAssert.IsBracket(model, new Position(2, 4), (new Range(2, 3, 2, 4), new Range(1, 4, 1, 5)));
        BracketAssert.IsNotBracket(model, 2, 5);
        BracketAssert.IsNotBracket(model, 2, 6);
        BracketAssert.IsNotBracket(model, 2, 7);
    }

    // Monaco: 'TextModelWithTokens - bracket matching' > 'bracket matching 2'
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BracketMatching2(bool useTree)
    {
        var model = Create("var bar = {\nfoo: {\n}, bar: {hallo: [{\n}, {\n}]}}", useTree);

        (Position Test, Range First, Range Second)[] brackets =
        [
            (new Position(1, 11), new Range(1, 11, 1, 12), new Range(5, 4, 5, 5)),
            (new Position(1, 12), new Range(1, 11, 1, 12), new Range(5, 4, 5, 5)),

            (new Position(2, 6), new Range(2, 6, 2, 7), new Range(3, 1, 3, 2)),
            (new Position(2, 7), new Range(2, 6, 2, 7), new Range(3, 1, 3, 2)),

            (new Position(3, 1), new Range(3, 1, 3, 2), new Range(2, 6, 2, 7)),
            (new Position(3, 2), new Range(3, 1, 3, 2), new Range(2, 6, 2, 7)),
            (new Position(3, 9), new Range(3, 9, 3, 10), new Range(5, 3, 5, 4)),
            (new Position(3, 10), new Range(3, 9, 3, 10), new Range(5, 3, 5, 4)),
            (new Position(3, 17), new Range(3, 17, 3, 18), new Range(5, 2, 5, 3)),
            (new Position(3, 18), new Range(3, 18, 3, 19), new Range(4, 1, 4, 2)),
            (new Position(3, 19), new Range(3, 18, 3, 19), new Range(4, 1, 4, 2)),

            (new Position(4, 1), new Range(4, 1, 4, 2), new Range(3, 18, 3, 19)),
            (new Position(4, 2), new Range(4, 1, 4, 2), new Range(3, 18, 3, 19)),
            (new Position(4, 4), new Range(4, 4, 4, 5), new Range(5, 1, 5, 2)),
            (new Position(4, 5), new Range(4, 4, 4, 5), new Range(5, 1, 5, 2)),

            (new Position(5, 1), new Range(5, 1, 5, 2), new Range(4, 4, 4, 5)),
            (new Position(5, 2), new Range(5, 2, 5, 3), new Range(3, 17, 3, 18)),
            (new Position(5, 3), new Range(5, 3, 5, 4), new Range(3, 9, 3, 10)),
            (new Position(5, 4), new Range(5, 4, 5, 5), new Range(1, 11, 1, 12)),
            (new Position(5, 5), new Range(5, 4, 5, 5), new Range(1, 11, 1, 12)),
        ];

        var isABracket = new HashSet<(int LineNumber, int Column)>();
        foreach (var (testPos, b1, b2) in brackets)
        {
            BracketAssert.IsBracket(model, testPos, (b1, b2));
            isABracket.Add((testPos.LineNumber, testPos.Column));
        }

        for (int i = 1, len = model.LineCount; i <= len; i++)
        {
            string line = model.GetLineContent(i);
            for (int j = 1, lenJ = line.Length + 1; j <= lenJ; j++)
            {
                if (!isABracket.Contains((i, j)))
                {
                    BracketAssert.IsNotBracket(model, i, j);
                }
            }
        }
    }

    // Monaco: 'TextModelWithTokens 2' > 'bracket matching 3'
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BracketMatching3(bool useTree)
    {
        string text = string.Join("\n",
        [
            "begin",
            "    loop",
            "        if then",
            "        end if;",
            "    end loop;",
            "end;",
            "",
            "begin",
            "    loop",
            "        if then",
            "        end ifa;",
            "    end loop;",
            "end;",
        ]);
        var model = Create(text, useTree, new("if", "end if"), new("loop", "end loop"), new("begin", "end"));

        // <if> ... <end ifa> is not matched
        BracketAssert.IsNotBracket(model, 10, 9);

        // <if> ... <end if> is matched
        BracketAssert.IsBracket(model, new Position(3, 9), (new Range(3, 9, 3, 11), new Range(4, 9, 4, 15)));
        BracketAssert.IsBracket(model, new Position(4, 9), (new Range(4, 9, 4, 15), new Range(3, 9, 3, 11)));

        // <loop> ... <end loop> is matched
        BracketAssert.IsBracket(model, new Position(2, 5), (new Range(2, 5, 2, 9), new Range(5, 5, 5, 13)));
        BracketAssert.IsBracket(model, new Position(5, 5), (new Range(5, 5, 5, 13), new Range(2, 5, 2, 9)));

        // <begin> ... <end> is matched
        BracketAssert.IsBracket(model, new Position(1, 1), (new Range(1, 1, 1, 6), new Range(6, 1, 6, 4)));
        BracketAssert.IsBracket(model, new Position(6, 1), (new Range(6, 1, 6, 4), new Range(1, 1, 1, 6)));
    }

    // Monaco: 'TextModelWithTokens 2' > 'bracket matching 4'
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BracketMatching4(bool useTree)
    {
        string text = string.Join("\n",
        [
            "recordbegin",
            "  simplerecordbegin",
            "  endrecord",
            "endrecord",
        ]);
        var model = Create(text, useTree, new("recordbegin", "endrecord"), new("simplerecordbegin", "endrecord"));

        // <recordbegin> ... <endrecord> is matched
        BracketAssert.IsBracket(model, new Position(1, 1), (new Range(1, 1, 1, 12), new Range(4, 1, 4, 10)));
        BracketAssert.IsBracket(model, new Position(4, 1), (new Range(4, 1, 4, 10), new Range(1, 1, 1, 12)));

        // <simplerecordbegin> ... <endrecord> is matched
        BracketAssert.IsBracket(model, new Position(2, 3), (new Range(2, 3, 2, 20), new Range(3, 3, 3, 12)));
        BracketAssert.IsBracket(model, new Position(3, 3), (new Range(3, 3, 3, 12), new Range(2, 3, 2, 20)));
    }

    // Monaco: 'TextModelWithTokens 2' > 'issue #95843: Highlighting of closing braces is indicating wrong brace when cursor is behind opening brace'
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Issue95843_ClosingBraceIsMatchedWhenCursorIsBehindOpeningBrace(bool useTree)
    {
        const string mode1 = "testMode1";
        const string mode2 = "testMode2";
        var configuration = new LanguageConfiguration { Brackets = Brackets };

        var test = TestLanguageModels.Create(
            "function f() {\n  return <p>{true}</p>;\n}",
            mode1,
            configuration,
            codec =>
            {
                uint Other(string mode) =>
                    ((uint)codec.EncodeLanguageId(mode) << MetadataConsts.LanguageIdOffset)
                    | ((uint)StandardTokenType.Other << MetadataConsts.TokenTypeOffset)
                    | MetadataConsts.BalancedBracketsMask;
                uint otherMetadata1 = Other(mode1);
                uint otherMetadata2 = Other(mode2);
                return new LineTokensSupport(new Dictionary<string, uint[]>
                {
                    ["function f() {"] = [0, otherMetadata1, 8, otherMetadata1, 9, otherMetadata1, 10, otherMetadata1, 11, otherMetadata1, 12, otherMetadata1, 13, otherMetadata1],
                    ["  return <p>{true}</p>;"] =
                    [
                        0, otherMetadata1,
                        2, otherMetadata1,
                        8, otherMetadata1,
                        9, otherMetadata2,
                        10, otherMetadata2,
                        11, otherMetadata2,
                        12, otherMetadata2,
                        13, otherMetadata1,
                        17, otherMetadata2,
                        18, otherMetadata2,
                        20, otherMetadata2,
                        21, otherMetadata2,
                        22, otherMetadata2,
                    ],
                    ["}"] = [0, otherMetadata1],
                });
            },
            mode2);
        test.Configurations.Register(mode2, configuration);
        var model = test.Model;
        SelectPath(model, useTree);

        model.Tokenization.ForceTokenization(1);
        model.Tokenization.ForceTokenization(2);
        model.Tokenization.ForceTokenization(3);

        Assert.Equal(
            ((Range, Range)?)(new Range(2, 13, 2, 14), new Range(2, 18, 2, 19)),
            model.BracketPairs.MatchBracket(new Position(2, 14)));
    }

    // Monaco: 'TextModelWithTokens 2' > 'issue #88075: TypeScript brace matching is incorrect in `${}` strings'
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Issue88075_TypeScriptBraceMatchingIsCorrectInTemplateStrings(bool useTree)
    {
        const string mode = "testMode";

        var test = TestLanguageModels.Create(
            "function hello() {\n    console.log(`${100}`);\n}",
            mode,
            new LanguageConfiguration { Brackets = Brackets },
            codec =>
            {
                uint otherMetadata = ((uint)codec.EncodeLanguageId(mode) << MetadataConsts.LanguageIdOffset)
                    | ((uint)StandardTokenType.Other << MetadataConsts.TokenTypeOffset);
                uint stringMetadata = ((uint)codec.EncodeLanguageId(mode) << MetadataConsts.LanguageIdOffset)
                    | ((uint)StandardTokenType.String << MetadataConsts.TokenTypeOffset);
                return new LineTokensSupport(new Dictionary<string, uint[]>
                {
                    ["function hello() {"] = [0, otherMetadata],
                    ["    console.log(`${100}`);"] = [0, otherMetadata, 16, stringMetadata, 19, otherMetadata, 22, stringMetadata, 24, otherMetadata],
                    ["}"] = [0, otherMetadata],
                });
            });
        var model = test.Model;
        SelectPath(model, useTree);

        model.Tokenization.ForceTokenization(1);
        model.Tokenization.ForceTokenization(2);
        model.Tokenization.ForceTokenization(3);

        Assert.Null(model.BracketPairs.MatchBracket(new Position(2, 23)));
        Assert.Null(model.BracketPairs.MatchBracket(new Position(2, 20)));
    }
}
