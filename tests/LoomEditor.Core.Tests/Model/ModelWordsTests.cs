// Ported from microsoft/vscode: src/vs/editor/test/common/model/model.test.ts (suite 'Editor Model - Words')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model;

// The three tests of the suite are ported; the others are new.
public class ModelWordsTests
{
    private const string OuterLanguageId = "outerMode";
    private const string InnerLanguageId = "innerMode";

    // Monaco: OuterMode's tokenizer: every 'x' is in the inner language, every other character in the outer one.
    private sealed class OuterModeSupport(LanguageIdCodec languageIdCodec) : ITokenizationSupport
    {
        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
        {
            var tokensArr = new List<uint>();
            string? prevLanguageId = null;
            for (int i = 0; i < line.Length; i++)
            {
                string languageId = line[i] == 'x' ? InnerLanguageId : OuterLanguageId;
                int encodedLanguageId = languageIdCodec.EncodeLanguageId(languageId);
                if (prevLanguageId != languageId)
                {
                    tokensArr.Add((uint)i);
                    tokensArr.Add((uint)encodedLanguageId << MetadataConsts.LanguageIdOffset);
                }

                prevLanguageId = languageId;
            }

            return new EncodedTokenizationResult([.. tokensArr], state);
        }
    }

    private static void AssertWord(TextModel model, int column, string word, int startColumn, int endColumn) =>
        Assert.Equal(new WordAtPosition(word, startColumn, endColumn), model.GetWordAtPosition(new Position(1, column)));

    private static void AssertNoWord(TextModel model, int column) =>
        Assert.Null(model.GetWordAtPosition(new Position(1, column)));

    // Monaco: 'Get word at position'
    [Fact]
    public void GetWordAtPosition()
    {
        string[] text = ["This text has some  words. "];
        var thisModel = new TextModel(string.Join('\n', text));

        AssertWord(thisModel, 1, "This", 1, 5);
        AssertWord(thisModel, 2, "This", 1, 5);
        AssertWord(thisModel, 4, "This", 1, 5);
        AssertWord(thisModel, 5, "This", 1, 5);
        AssertWord(thisModel, 6, "text", 6, 10);
        AssertWord(thisModel, 19, "some", 15, 19);
        AssertNoWord(thisModel, 20);
        AssertWord(thisModel, 21, "words", 21, 26);
        AssertWord(thisModel, 26, "words", 21, 26);
        AssertNoWord(thisModel, 27);
        AssertNoWord(thisModel, 28);
    }

    // Monaco: 'getWordAtPosition at embedded language boundaries'
    [Fact]
    public void GetWordAtPositionAtEmbeddedLanguageBoundaries()
    {
        var codec = new LanguageIdCodec();
        codec.Register(OuterLanguageId);
        codec.Register(InnerLanguageId);
        var tokenizationRegistry = new TokenizationRegistry();
        tokenizationRegistry.Register(OuterLanguageId, new OuterModeSupport(codec));
        var languageConfigurations = new LanguageConfigurationRegistry();
        languageConfigurations.Register(OuterLanguageId, new LanguageConfiguration());
        languageConfigurations.Register(InnerLanguageId, new LanguageConfiguration());

        var model = new TextModel("ab<xx>ab<x>", DefaultEndOfLine.LF, null, tokenizationRegistry, codec, null, languageConfigurations);
        model.Tokenization.SetLanguageId(OuterLanguageId);

        AssertWord(model, 1, "ab", 1, 3);
        AssertWord(model, 2, "ab", 1, 3);
        AssertWord(model, 3, "ab", 1, 3);
        AssertWord(model, 4, "xx", 4, 6);
        AssertWord(model, 5, "xx", 4, 6);
        AssertWord(model, 6, "xx", 4, 6);
        AssertWord(model, 7, "ab", 7, 9);
    }

    // Monaco: 'issue #61296: VS code freezes when editing CSS file with emoji'
    [Fact]
    public void Issue61296_VsCodeFreezesWhenEditingCssFileWithEmoji()
    {
        const string ModeId = "testMode";
        var codec = new LanguageIdCodec();
        codec.Register(ModeId);
        var languageConfigurations = new LanguageConfigurationRegistry();
        languageConfigurations.Register(ModeId, new LanguageConfiguration
        {
            WordPattern = new Regex(@"(#?-?\d*\.\d\w*%?)|(::?[\w-]*(?=[^,{;]*[,{]))|(([@#.!])?[\w-?]+%?|[@#!.])"),
        });

        var thisModel = new TextModel(".🐷-a-b", DefaultEndOfLine.LF, null, new TokenizationRegistry(), codec, null, languageConfigurations);
        thisModel.Tokenization.SetLanguageId(ModeId);

        AssertWord(thisModel, 1, ".", 1, 2);
        AssertWord(thisModel, 2, ".", 1, 2);
        AssertNoWord(thisModel, 3);
        AssertWord(thisModel, 4, "-a-b", 4, 8);
        AssertWord(thisModel, 5, "-a-b", 4, 8);
        AssertWord(thisModel, 6, "-a-b", 4, 8);
        AssertWord(thisModel, 7, "-a-b", 4, 8);
        AssertWord(thisModel, 8, "-a-b", 4, 8);
    }

    [Fact]
    public void GetWordUntilPosition_CutsTheWordAtTheColumn()
    {
        var model = new TextModel("This text");

        Assert.Equal(new WordAtPosition("Th", 1, 3), model.GetWordUntilPosition(new Position(1, 3)));
        Assert.Equal(new WordAtPosition("tex", 6, 9), model.GetWordUntilPosition(new Position(1, 9)));
    }

    [Fact]
    public void GetWordUntilPosition_NoWord_ReturnsEmptyWordAtTheColumn()
    {
        var model = new TextModel("a   b");

        Assert.Equal(new WordAtPosition(string.Empty, 3, 3), model.GetWordUntilPosition(new Position(1, 3)));
    }

    [Fact]
    public void LanguageConfigurationChanged_RegisterForTheModelLanguage_IsRaised()
    {
        var model = new TextModel("text");
        int count = 0;
        model.LanguageConfigurationChanged += (_, _) => count++;

        model.LanguageConfigurationRegistry.Register(LanguageIdCodec.PlainTextLanguageId, new LanguageConfiguration());

        Assert.Equal(1, count);
    }

    [Fact]
    public void LanguageConfigurationChanged_OtherLanguage_IsNotRaised()
    {
        var model = new TextModel("text");
        int count = 0;
        model.LanguageConfigurationChanged += (_, _) => count++;

        model.LanguageConfigurationRegistry.Register("otherLanguage", new LanguageConfiguration());

        Assert.Equal(0, count);
    }

    [Fact]
    public void LanguageConfigurationChanged_SetLanguageId_IsRaisedAfterLanguageChanged()
    {
        var codec = new LanguageIdCodec();
        codec.Register("otherLanguage");
        var model = new TextModel("text", DefaultEndOfLine.LF, null, new TokenizationRegistry(), codec, null);
        var events = new List<string>();
        model.LanguageChanged += (_, _) => events.Add("language");
        model.LanguageConfigurationChanged += (_, _) => events.Add("languageConfiguration");

        model.Tokenization.SetLanguageId("otherLanguage");

        Assert.Equal(["language", "languageConfiguration"], events);
    }
}
