using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;

/// <summary>A model and the registries it was created with.</summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal sealed class TestLanguageModel(TextModel model, LanguageConfigurationRegistry configurations)
{
    public TextModel Model { get; } = model;

    public LanguageConfigurationRegistry Configurations { get; } = configurations;
}

/// <summary>
/// Creates a model of a given language (Monaco: <c>createModelServices</c>, <c>languageService.registerLanguage</c>,
/// <c>languageConfigurationService.register</c> and <c>instantiateTextModel</c>, the pattern of
/// <c>getBracketPairsInRange.test.ts</c>).
/// </summary>
/// <remarks>Threading: stateless.</remarks>
internal static class TestLanguageModels
{
    /// <summary>The language the models use unless a test says otherwise.</summary>
    public const string DefaultLanguageId = "testLanguage";

    /// <summary>Creates a model whose language is <paramref name="languageId"/>.</summary>
    /// <param name="text">The text of the model.</param>
    /// <param name="languageId">The language of the model.</param>
    /// <param name="configuration">The language configuration of <paramref name="languageId"/>.</param>
    /// <param name="createSupport">Creates the tokenization support of <paramref name="languageId"/>; none when <see langword="null"/>.</param>
    /// <param name="otherLanguages">Other languages the tokens may use; they are registered with the codec.</param>
    public static TestLanguageModel Create(
        string text,
        string languageId,
        LanguageConfiguration configuration,
        Func<LanguageIdCodec, ITokenizationSupport>? createSupport = null,
        params string[] otherLanguages)
    {
        var codec = new LanguageIdCodec();
        codec.Register(languageId);
        foreach (var other in otherLanguages)
        {
            codec.Register(other);
        }

        var tokenization = new TokenizationRegistry();
        if (createSupport is not null)
        {
            tokenization.Register(languageId, createSupport(codec));
        }

        var configurations = new LanguageConfigurationRegistry();
        configurations.Register(languageId, configuration);

        var model = new TextModel(text, DefaultEndOfLine.LF, null, tokenization, codec, null, configurations);
        model.Tokenization.SetLanguageId(languageId);
        return new TestLanguageModel(model, configurations);
    }
}
