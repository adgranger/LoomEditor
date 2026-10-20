// Ported from microsoft/vscode: src/vs/editor/contrib/indentation/test/browser/indentation.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Languages.TestHelpers;

/// <summary>A token of a fake tokenizer: where it starts on its line and its standard token type.</summary>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct StandardTokenTypeData(int StartIndex, StandardTokenType StandardTokenType);

/// <summary>The test languages of Monaco's indentation tests (Monaco: the <c>Language</c> enum and its helpers).</summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal static class TestLanguages
{
    public const string TypeScript = "ts-test";
    public const string Ruby = "ruby-test";
    public const string PHP = "php-test";
    public const string Go = "go-test";
    public const string CPP = "cpp-test";
    public const string HTML = "html-test";
    public const string VB = "vb-test";
    public const string Latex = "latex-test";
    public const string Lua = "lua-test";

    public static void RegisterLanguage(LanguageIdCodec codec, string language) => codec.Register(language);

    public static IDisposable RegisterLanguageConfiguration(LanguageConfigurationRegistry registry, string language) => language switch
    {
        TypeScript => registry.Register(language, new LanguageConfiguration
        {
            Brackets = BracketRules.TypescriptBracketRules,
            Comments = new CommentRule(
                LineComment: new LineCommentConfig("//"),
                BlockComment: new CharacterPair("/*", "*/")),
            AutoClosingPairs = AutoClosingPairsRules.JavascriptAutoClosingPairsRules,
            IndentationRules = IndentationRules.JavascriptIndentationRules,
            OnEnterRules = OnEnterRules.JavascriptOnEnterRules,
        }),
        Ruby => registry.Register(language, new LanguageConfiguration
        {
            Brackets = BracketRules.RubyBracketRules,
            IndentationRules = IndentationRules.RubyIndentationRules,
        }),
        PHP => registry.Register(language, new LanguageConfiguration
        {
            Brackets = BracketRules.PhpBracketRules,
            IndentationRules = IndentationRules.PhpIndentationRules,
            OnEnterRules = OnEnterRules.PhpOnEnterRules,
        }),
        Go => registry.Register(language, new LanguageConfiguration
        {
            Brackets = BracketRules.GoBracketRules,
            IndentationRules = IndentationRules.GoIndentationRules,
        }),
        CPP => registry.Register(language, new LanguageConfiguration
        {
            Brackets = BracketRules.CppBracketRules,
            OnEnterRules = OnEnterRules.CppOnEnterRules,
        }),
        HTML => registry.Register(language, new LanguageConfiguration
        {
            Brackets = BracketRules.HtmlBracketRules,
            IndentationRules = IndentationRules.HtmlIndentationRules,
            OnEnterRules = OnEnterRules.HtmlOnEnterRules,
        }),
        VB => registry.Register(language, new LanguageConfiguration
        {
            Brackets = BracketRules.VbBracketRules,
            IndentationRules = IndentationRules.VbIndentationRules,
            OnEnterRules = OnEnterRules.VbOnEnterRules,
        }),
        Latex => registry.Register(language, new LanguageConfiguration
        {
            Brackets = BracketRules.LatexBracketRules,
            // TypeScript's structural typing accepts plain pairs where conditional pairs are expected.
            AutoClosingPairs = [.. AutoClosingPairsRules.LatexAutoClosingPairsRules.Select(p => new AutoClosingPairConditional(p.Open, p.Close))],
            IndentationRules = IndentationRules.LatexIndentationRules,
        }),
        Lua => registry.Register(language, new LanguageConfiguration
        {
            Brackets = BracketRules.LuaBracketRules,
            IndentationRules = IndentationRules.LuaIndentationRules,
        }),
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown test language"),
    };

    /// <summary>Registers a tokenizer that returns <paramref name="tokens"/>[i] for the i-th line it tokenizes.</summary>
    public static IDisposable RegisterTokenizationSupport(TokenizationRegistry registry, ILanguageIdCodec codec, StandardTokenTypeData[][] tokens, string languageId) =>
        registry.Register(languageId, new FakeTokenizationSupport(codec, tokens, languageId));

    /// <summary>Monaco's <c>createTextModel(text, languageId)</c> with the given registries.</summary>
    public static TextModel CreateTextModel(string text, string languageId, TokenizationRegistry tokenization, LanguageIdCodec codec, LanguageConfigurationRegistry configurations)
    {
        var model = new TextModel(text, DefaultEndOfLine.LF, null, tokenization, codec, null, configurations);
        model.Tokenization.SetLanguageId(languageId);
        return model;
    }

    private sealed class FakeTokenizationSupport(ILanguageIdCodec codec, StandardTokenTypeData[][] tokens, string languageId) : ITokenizationSupport
    {
        private int _lineIndex;

        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
        {
            StandardTokenTypeData[] tokensOnLine = tokens[_lineIndex++];
            int encodedLanguageId = codec.EncodeLanguageId(languageId);
            uint[] result = new uint[2 * tokensOnLine.Length];
            for (int i = 0; i < tokensOnLine.Length; i++)
            {
                result[2 * i] = (uint)tokensOnLine[i].StartIndex;
                result[(2 * i) + 1] =
                    ((uint)encodedLanguageId << MetadataConsts.LanguageIdOffset)
                    | ((uint)tokensOnLine[i].StandardTokenType << MetadataConsts.TokenTypeOffset);
            }

            return new EncodedTokenizationResult(result, state);
        }
    }
}
