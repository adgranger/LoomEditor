// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/textMateTokenizationFeatureImpl.ts (_createTokenizationSupport)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>
/// Creates the tokenization support of a language: grammar factory → <see cref="TextMateTokenizationSupport"/> →
/// <see cref="TokenizationSupportWithLineLimit"/> (VS Code: <c>_createTokenizationSupport</c>). Not ported: the
/// <c>onDidEncounterLanguage</c> hook (<c>requestBasicLanguageFeatures</c>) and the time telemetry.
/// </summary>
/// <remarks>Threading: same as the <see cref="TMGrammarFactory"/> (one per thread).</remarks>
internal sealed class TextMateTokenizationFactory(
    LanguageRegistry languages,
    TMGrammarFactory grammarFactory,
    Func<TextModel, IBackgroundTokenizationStore, IBackgroundTokenizer?>? createBackgroundTokenizer = null,
    Func<bool>? backgroundTokenizerShouldOnlyVerifyTokens = null,
    int maxTokenizationLineLength = TokenizationSupportWithLineLimit.DefaultMaxTokenizationLineLength)
{
    /// <summary>
    /// The support of <paramref name="languageId"/>, or <see langword="null"/> (unknown language, no grammar, a broken
    /// grammar: tokens are then plain text). Errors are traced, like VS Code logs them.
    /// </summary>
    public ITokenizationSupport? CreateTokenizationSupport(string languageId)
    {
        if (!languages.IsRegisteredLanguageId(languageId) || !grammarFactory.Has(languageId))
        {
            return null;
        }

        try
        {
            int encodedLanguageId = languages.Codec.EncodeLanguageId(languageId);
            CreateGrammarResult r = grammarFactory.CreateGrammar(languageId, encodedLanguageId);
            if (r.Grammar is null)
            {
                return null;
            }

            var tokenization = new TextMateTokenizationSupport(
                r.Grammar,
                r.InitialState,
                r.ContainsEmbeddedLanguages,
                createBackgroundTokenizer,
                backgroundTokenizerShouldOnlyVerifyTokens ?? (() => false));
            return new TokenizationSupportWithLineLimit(encodedLanguageId, tokenization, maxTokenizationLineLength);
        }
        catch (InvalidOperationException e) when (e.Message == TMGrammarFactory.MissingTMGrammarErrorMessage)
        {
            // Don't log this error message
            return null;
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Unable to create the tokenization support of {languageId}: {e}");
            return null;
        }
    }
}
