// Ported from microsoft/vscode: src/vs/editor/test/common/modes/supports/electricCharacter.test.ts (suite 'Editor Modes - Auto Indentation')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Tests.Languages.TestHelpers;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Languages.Supports;

public class ElectricCharacterTests
{
    private const string FakeLanguageId = "test";

    private static ElectricAction? TestOnElectricCharacter(BracketElectricCharacterSupport electricCharacterSupport, TokenText[] line, string character, int offset) =>
        electricCharacterSupport.OnElectricCharacter(character, ModesTestUtils.CreateFakeScopedLineTokens(line), offset);

    private static void TestDoesNothing(BracketElectricCharacterSupport electricCharacterSupport, TokenText[] line, string character, int offset) =>
        Assert.Null(TestOnElectricCharacter(electricCharacterSupport, line, character, offset));

    private static void TestMatchBracket(BracketElectricCharacterSupport electricCharacterSupport, TokenText[] line, string character, int offset, string matchOpenBracket) =>
        Assert.Equal(new ElectricAction(matchOpenBracket), TestOnElectricCharacter(electricCharacterSupport, line, character, offset));

    private static BracketElectricCharacterSupport CreateSupport() =>
        new(new RichEditBrackets(FakeLanguageId, [new CharacterPair("{", "}"), new CharacterPair("(", ")")]));

    // Monaco: 'getElectricCharacters uses all sources and dedups'
    [Fact]
    public void GetElectricCharacters_UsesAllSourcesAndDedups() =>
        Assert.Equal(["}", ")"], CreateSupport().GetElectricCharacters());

    // Monaco: 'matchOpenBracket'
    [Fact]
    public void MatchOpenBracket()
    {
        BracketElectricCharacterSupport sup = CreateSupport();

        TestDoesNothing(sup, [new TokenText("\t{", StandardTokenType.Other)], "\t", 1);
        TestDoesNothing(sup, [new TokenText("\t{", StandardTokenType.Other)], "\t", 2);
        TestDoesNothing(sup, [new TokenText("\t\t", StandardTokenType.Other)], "{", 3);

        TestDoesNothing(sup, [new TokenText("\t}", StandardTokenType.Other)], "\t", 1);
        TestDoesNothing(sup, [new TokenText("\t}", StandardTokenType.Other)], "\t", 2);
        TestMatchBracket(sup, [new TokenText("\t\t", StandardTokenType.Other)], "}", 3, "}");
    }
}
