// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model;

public sealed partial class TextModel
{
    private TokenizationTextModelPart _tokenization = null!;

    /// <summary>The tokens of the lines of the model changed (Monaco: <c>onDidChangeTokens</c>).</summary>
    internal event EventHandler<ModelTokensChangedEventArgs>? TokensChanged;

    /// <summary>The language of the model changed (Monaco: <c>onDidChangeLanguage</c>).</summary>
    internal event EventHandler<ModelLanguageChangedEventArgs>? LanguageChanged;

    /// <summary>The language id and the syntax tokens of the model (Monaco: <c>tokenization</c>).</summary>
    internal TokenizationTextModelPart Tokenization => _tokenization;

    /// <summary>
    /// Creates a model whose language is plain text and whose tokenization supports come from
    /// <paramref name="registry"/> (the public constructors use an empty registry: no tokens other than plain text).
    /// </summary>
    /// <param name="text">The initial text.</param>
    /// <param name="defaultEol">The EOL to use when the text has no line break.</param>
    /// <param name="options">The indentation options, or <see langword="null"/> for the defaults.</param>
    /// <param name="registry">Where the tokenization supports are looked up.</param>
    /// <param name="languageIdCodec">Maps language names to the ids stored in token metadata.</param>
    /// <param name="scheduler">Runs the default background tokenizer; without it, tokens are only computed on demand.</param>
    internal TextModel(
        string text,
        DefaultEndOfLine defaultEol,
        TextModelOptions? options,
        TokenizationRegistry registry,
        ILanguageIdCodec languageIdCodec,
        ITokenizationScheduler? scheduler)
        : this(CreateBuffer(text, options?.DefaultEol ?? defaultEol), ResolveOptions(defaultEol, options), registry, languageIdCodec, scheduler)
    {
    }

    private void CreateTokenization(TokenizationRegistry registry, ILanguageIdCodec languageIdCodec, ITokenizationScheduler? scheduler) =>
        _tokenization = new TokenizationTextModelPart(this, LanguageIdCodec.PlainTextLanguageId, registry, languageIdCodec, scheduler);

    internal void RaiseTokensChanged(ModelTokensChangedEventArgs e) => TokensChanged?.Invoke(this, e);

    internal void RaiseLanguageChanged(ModelLanguageChangedEventArgs e) => LanguageChanged?.Invoke(this, e);
}
