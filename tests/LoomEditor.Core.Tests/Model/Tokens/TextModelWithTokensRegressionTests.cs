// Ported from microsoft/vscode: src/vs/editor/test/common/model/textModelWithTokens.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.Tokens;

public class TextModelWithTokensRegressionTests
{
    private sealed class IncrementingSupport : ITokenizationSupport
    {
        private uint _tokenId = 10;

        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
        {
            uint myId = ++_tokenId;
            return new EncodedTokenizationResult([0, myId << MetadataConsts.ForegroundOffset], state);
        }
    }

    private sealed class FixedLanguageSupport(int languageId) : ITokenizationSupport
    {
        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) =>
            new([0, (uint)languageId << MetadataConsts.LanguageIdOffset], state);
    }

    private static void AssertViewLineTokens(TextModel model, int lineNumber, bool forceTokenization, (int EndIndex, int Foreground)[] expected)
    {
        if (forceTokenization)
        {
            model.Tokenization.ForceTokenization(lineNumber);
        }

        IViewLineTokens actual = model.Tokenization.GetLineTokens(lineNumber).Inflate();
        var actualTokens = new (int EndIndex, int Foreground)[actual.GetCount()];
        for (int i = 0; i < actualTokens.Length; i++)
        {
            actualTokens[i] = (actual.GetEndOffset(i), actual.GetForeground(i));
        }

        Assert.Equal(expected, actualTokens);
    }

    // Monaco: 'TextModelWithTokens regression tests' > 'microsoft/monaco-editor#122: Unhandled Exception: TypeError: Unable to get property 'replace' of undefined or null reference'
    [Fact]
    public void MonacoEditor_122_ChangingLanguageGivesTheTokensOfTheNewLanguage()
    {
        var support = new IncrementingSupport();
        var registry = new TokenizationRegistry();
        registry.Register("indicisiveMode1", support);
        registry.Register("indicisiveMode2", support);
        var model = new TextModel("A model with\ntwo lines", DefaultEndOfLine.LF, null, registry, new LanguageIdCodec(), null);

        AssertViewLineTokens(model, 1, true, [(12, 1)]);
        AssertViewLineTokens(model, 2, true, [(9, 1)]);

        model.Tokenization.SetLanguageId("indicisiveMode1");

        AssertViewLineTokens(model, 1, true, [(12, 11)]);
        AssertViewLineTokens(model, 2, true, [(9, 12)]);

        model.Tokenization.SetLanguageId("indicisiveMode2");

        AssertViewLineTokens(model, 1, false, [(12, 1)]);
        AssertViewLineTokens(model, 2, false, [(9, 1)]);
    }

    // Monaco: 'TextModelWithTokens regression tests' > 'microsoft/monaco-editor#133: Error: Cannot read property 'modeId' of undefined'
    // Monaco runs it on the bracket pair tree only: useTree = false runs it on the legacy regex search too.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MonacoEditor_133_MatchBracketWorksWithoutTokenizationSupport(bool useTree)
    {
        string text = string.Join("\n",
        [
            "Imports System",
            "Imports System.Collections.Generic",
            "",
            "Module m1",
            "",
            "\tSub Main()",
            "\tEnd Sub",
            "",
            "End Module",
        ]);
        var model = BracketPairsTestModels.Create(text, [new CharacterPair("module", "end module"), new CharacterPair("sub", "end sub")]).Model;
        SelectPath(model, useTree);

        var actual = model.BracketPairs.MatchBracket(new Position(4, 1));

        Assert.Equal(((Range, Range)?)(new Range(4, 1, 4, 7), new Range(9, 1, 9, 11)), actual);
    }

    // Monaco: 'TextModelWithTokens regression tests' > 'issue #11856: Bracket matching does not work as expected if the opening brace symbol is contained in the closing brace symbol'
    // Monaco runs it on the bracket pair tree only: useTree = false runs it on the legacy regex search too.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Issue11856_OpeningBraceContainedInTheClosingBraceIsMatched(bool useTree)
    {
        string text = string.Join("\n",
        [
            "sequence \"outer\"",
            "     sequence \"inner\"",
            "     endsequence",
            "endsequence",
        ]);
        var model = BracketPairsTestModels.Create(text, [new CharacterPair("sequence", "endsequence"), new CharacterPair("feature", "endfeature")]).Model;
        SelectPath(model, useTree);

        var position = new Position(3, 9);
        if (useTree)
        {
            Assert.Equal(((Range, Range)?)(new Range(2, 6, 2, 14), new Range(3, 6, 3, 17)), model.BracketPairs.MatchBracket(position));
        }
        else
        {
            // The legacy search answers the bracket at the position first (here the closing one): compare the sorted pair, like assertIsBracket.
            BracketAssert.IsBracket(model, position, (new Range(2, 6, 2, 14), new Range(3, 6, 3, 17)));
        }
    }

    private static void SelectPath(TextModel model, bool useTree)
    {
        if (!useTree)
        {
            model.BracketPairs.MaxSupportedDocumentLength = 0;
        }
    }

    // Monaco: 'TextModelWithTokens regression tests' > 'issue #63822: Wrong embedded language detected for empty lines'
    [Fact]
    public void Issue63822_WrongEmbeddedLanguageDetectedForEmptyLines()
    {
        var codec = new LanguageIdCodec();
        codec.Register("outerMode");
        codec.Register("innerMode");
        int encodedInnerMode = codec.EncodeLanguageId("innerMode");
        var registry = new TokenizationRegistry();
        registry.Register("outerMode", new FixedLanguageSupport(encodedInnerMode));
        var model = new TextModel("A model with one line", DefaultEndOfLine.LF, null, registry, codec, null);
        model.Tokenization.SetLanguageId("outerMode");

        model.Tokenization.ForceTokenization(1);

        Assert.Equal("innerMode", model.Tokenization.GetLanguageIdAtPosition(1, 1));
    }
}
