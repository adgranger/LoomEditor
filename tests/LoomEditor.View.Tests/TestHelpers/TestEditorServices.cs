// Ported from microsoft/vscode: src/vs/editor/test/browser/testCodeEditor.ts (createCodeEditorServices, subset) and src/vs/editor/test/common/testTextModel.ts (instantiateTextModel)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Tests.TestHelpers;

/// <summary>
/// The services a test editor needs to give a model a language: a language id codec, a tokenization registry and a language
/// configuration registry (Monaco: the subset of <c>createCodeEditorServices</c> that the cursor tests use).
/// </summary>
/// <remarks>Threading: use from one test thread.</remarks>
internal sealed class TestEditorServices : IDisposable
{
    private readonly List<IDisposable> _disposables = [];

    public LanguageIdCodec LanguageIdCodec { get; } = new();

    public TokenizationRegistry TokenizationRegistry { get; } = new();

    public LanguageConfigurationRegistry LanguageConfigurationRegistry { get; } = new();

    /// <summary>Monaco: <c>languageService.registerLanguage({ id })</c>.</summary>
    public void RegisterLanguage(string languageId) => LanguageIdCodec.Register(languageId);

    /// <summary>Monaco: <c>disposables.add</c>.</summary>
    public T Add<T>(T disposable)
        where T : IDisposable
    {
        _disposables.Add(disposable);
        return disposable;
    }

    /// <summary>Monaco: <c>instantiateTextModel</c>.</summary>
    public TextModel CreateTextModel(string text, string? languageId = null, TextModelOptions? options = null)
    {
        var model = new TextModel(text, DefaultEndOfLine.LF, options, TokenizationRegistry, LanguageIdCodec, null, LanguageConfigurationRegistry);
        if (languageId is not null)
        {
            model.Tokenization.SetLanguageId(languageId);
        }

        return model;
    }

    public void Dispose()
    {
        for (int i = _disposables.Count - 1; i >= 0; i--)
        {
            _disposables[i].Dispose();
        }

        _disposables.Clear();
    }
}
