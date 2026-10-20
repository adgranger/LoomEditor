// Ported from microsoft/vscode: src/vs/editor/test/common/model/textModelWithTokens.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Tests.Model.Tokens.TestHelpers;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;

/// <summary>A model with a language that has brackets, and the registries it was created with.</summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal sealed class BracketPairsTestModel(TextModel model, LanguageConfigurationRegistry configurations, ManualTokenizationScheduler scheduler)
{
    public TextModel Model { get; } = model;

    public LanguageConfigurationRegistry Configurations { get; } = configurations;

    public ManualTokenizationScheduler Scheduler { get; } = scheduler;

    /// <summary>A new tree on the model.</summary>
    public BracketPairsTree CreateTree() => new(Model, Configurations.GetLanguageConfiguration);

    /// <summary>A new tree on the model, kept up to date with the content changes of the model.</summary>
    public BracketPairsTree CreateTreeFollowingContent()
    {
        var tree = CreateTree();
        Model.ContentChanged += (_, e) => tree.HandleContentChanged(e);
        return tree;
    }

    /// <summary>Tokenizes the first <paramref name="lineNumber"/> lines (forces the tokenization of the model).</summary>
    public void Tokenize(int lineNumber) => Model.Tokenization.ForceTokenization(lineNumber);

    /// <summary>Tokenizes the whole model, which completes its background tokenization.</summary>
    public void TokenizeAll() => Model.Tokenization.ForceTokenization(Model.LineCount);

    /// <summary>The opening bracket kind of <paramref name="text"/>.</summary>
    public OpeningBracketKind Opening(string text) =>
        Configurations.GetLanguageConfiguration(BracketPairsTestModels.LanguageId).BracketsNew.GetOpeningBracketInfo(text)!;
}

/// <summary>
/// Creates models with brackets (Monaco: <c>createTextModelWithBrackets</c> and the <c>createModelServices</c> +
/// <c>instantiateTextModel</c> pattern of the bracket tests).
/// </summary>
/// <remarks>Threading: stateless.</remarks>
internal static class BracketPairsTestModels
{
    /// <summary>The language of the models (Monaco: <c>bracketMode2</c>).</summary>
    public const string LanguageId = "bracketMode2";

    /// <summary>The brackets of the models when the test gives none.</summary>
    public static readonly CharacterPair[] DefaultBrackets = [new("{", "}"), new("[", "]"), new("(", ")")];

    /// <summary>
    /// Creates a model of <see cref="LanguageId"/>. Without <paramref name="withTokenization"/> the language has no
    /// tokenization support, so the model has no tokens; with it, every line is one token that may contain balanced brackets
    /// and nothing is tokenized until the test asks for it.
    /// </summary>
    /// <param name="text">The text of the model.</param>
    /// <param name="brackets">The brackets of the language; <see cref="DefaultBrackets"/> when <see langword="null"/>.</param>
    /// <param name="colorizedBracketPairs">The colorized pairs; all the brackets when <see langword="null"/>.</param>
    /// <param name="withTokenization">Whether the language has a tokenization support.</param>
    public static BracketPairsTestModel Create(
        string text,
        IReadOnlyList<CharacterPair>? brackets = null,
        IReadOnlyList<CharacterPair>? colorizedBracketPairs = null,
        bool withTokenization = false)
    {
        var codec = new LanguageIdCodec();
        codec.Register(LanguageId);

        var tokenization = new TokenizationRegistry();
        if (withTokenization)
        {
            tokenization.Register(LanguageId, new BalancedBracketsSupport(codec.EncodeLanguageId(LanguageId)));
        }

        var configurations = new LanguageConfigurationRegistry();
        configurations.Register(LanguageId, new LanguageConfiguration
        {
            Brackets = brackets ?? DefaultBrackets,
            ColorizedBracketPairs = colorizedBracketPairs,
        });

        var scheduler = new ManualTokenizationScheduler();
        var model = new TextModel(text, DefaultEndOfLine.LF, null, tokenization, codec, withTokenization ? scheduler : null, configurations);
        model.Tokenization.SetLanguageId(LanguageId);
        return new BracketPairsTestModel(model, configurations, scheduler);
    }

    // One token per line, of the given language, that may contain balanced brackets.
    private sealed class BalancedBracketsSupport(int encodedLanguageId) : ITokenizationSupport
    {
        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state)
        {
            uint metadata = ((uint)encodedLanguageId << MetadataConsts.LanguageIdOffset)
                | ((uint)StandardTokenType.Other << MetadataConsts.TokenTypeOffset)
                | MetadataConsts.BalancedBracketsMask;
            return new EncodedTokenizationResult([0, metadata], state);
        }
    }
}
