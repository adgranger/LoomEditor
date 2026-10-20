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

    /// <summary>Models bigger than this (20 MB) are never tokenized (Monaco: <c>LARGE_FILE_SIZE_THRESHOLD</c>).</summary>
    private const int LargeFileSizeThreshold = 20 * 1024 * 1024;

    /// <summary>Models with more lines than this (300K) are never tokenized (Monaco: <c>LARGE_FILE_LINE_COUNT_THRESHOLD</c>).</summary>
    private const int LargeFileLineCountThreshold = 300 * 1000;

    private readonly AttachedViews _attachedViews = new();

    /// <summary>The views attached to the model and their visible lines (Monaco: <c>_attachedViews</c>).</summary>
    internal AttachedViews AttachedViews => _attachedViews;
    private int _attachedEditorCount;

    /// <summary>
    /// Whether the model is too large to be tokenized (Monaco: <c>isTooLargeForTokenization</c>), decided once at creation
    /// from its length and line count. <c>largeFileOptimizations</c> is always on.
    /// </summary>
    internal bool IsTooLargeForTokenization { get; private set; }

    /// <summary>The first view was attached or the last one detached (Monaco: <c>onDidChangeAttached</c>).</summary>
    internal event EventHandler? AttachedChanged;

    /// <summary>
    /// Models bigger than this (50 MB) are not mirrored to the tokenization worker (Monaco: <c>_MODEL_SYNC_LIMIT</c>,
    /// which is also a static that tests lower). A model that is too large for syncing falls back to the default
    /// background tokenizer.
    /// </summary>
    internal static int ModelSyncLimit { get; set; } = 50 * 1024 * 1024;

    /// <summary>Whether the model was too large to sync at creation (Monaco: <c>isTooLargeForSyncing</c>).</summary>
    internal bool IsTooLargeForSyncing { get; private set; }

    /// <summary>Whether at least one view is attached (Monaco: <c>isAttachedToEditor</c>).</summary>
    internal bool IsAttachedToEditor => _attachedEditorCount > 0;

    /// <summary>Attaches a view; dispose it to detach it (Monaco: <c>onBeforeAttached</c>).</summary>
    internal IAttachedView AttachView()
    {
        _attachedEditorCount++;
        if (_attachedEditorCount == 1)
        {
            _tokenization.HandleDidChangeAttached();
            AttachedChanged?.Invoke(this, EventArgs.Empty);
        }

        return _attachedViews.AttachView(DetachView);
    }

    /// <summary>Detaches a view (Monaco: <c>onBeforeDetached</c>); called when the view is disposed.</summary>
    internal void DetachView(IAttachedView view)
    {
        _attachedEditorCount--;
        if (_attachedEditorCount == 0)
        {
            _tokenization.HandleDidChangeAttached();
            AttachedChanged?.Invoke(this, EventArgs.Empty);
        }

        _attachedViews.DetachView(view);
    }

    private void CreateTokenization(TokenizationRegistry registry, ILanguageIdCodec languageIdCodec, ITokenizationScheduler? scheduler)
    {
        // Make a decision here and permanently respect it: a model too large at creation is never tokenized.
        IsTooLargeForTokenization = _buffer.GetLength() > LargeFileSizeThreshold || _buffer.GetLineCount() > LargeFileLineCountThreshold;
        IsTooLargeForSyncing = _buffer.GetLength() > ModelSyncLimit;
        _tokenization = new TokenizationTextModelPart(this, LanguageIdCodec.PlainTextLanguageId, registry, languageIdCodec, scheduler, _attachedViews);
    }

    internal void RaiseTokensChanged(ModelTokensChangedEventArgs e) => TokensChanged?.Invoke(this, e);

    internal void RaiseLanguageChanged(ModelLanguageChangedEventArgs e) => LanguageChanged?.Invoke(this, e);
}
