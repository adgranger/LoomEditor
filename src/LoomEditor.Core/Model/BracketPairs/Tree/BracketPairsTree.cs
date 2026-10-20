// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/bracketPairsTree.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>
/// The bracket pair AST of a <see cref="TextModel"/> and the queries on it. It queues the text and token edits and
/// reparses incrementally when a query reads the tree (Monaco: <c>BracketPairsTree</c>).
/// </summary>
/// <remarks>
/// <para>
/// There are two trees: the initial tree that has no token information and is used for performant initial bracket
/// colorization, and the tree that uses token information to detect bracket pairs. To prevent flickering, the
/// tree with token information is only used once tokenization completes. Since the text can be edited while background
/// tokenization is in progress, both trees are updated.
/// </para>
/// <para>
/// Differences from Monaco: <c>Disposable</c> and <c>Emitter</c> become the <see cref="Changed"/> event (the tree owns no
/// resource, so it is not disposable); the queries that may find nothing return a nullable <see cref="FoundBracket"/>.
/// </para>
/// <para>Threading: UI thread only (it reads the model).</para>
/// </remarks>
internal sealed partial class BracketPairsTree
{
    private readonly TextModel _textModel;
    private readonly LanguageAgnosticBracketTokens _brackets;

    private AstNode? _initialAstWithoutTokens;
    private AstNode? _astWithTokens;

    private List<TextEditInfo> _queuedTextEditsForInitialAstWithoutTokens = [];
    private List<TextEditInfo> _queuedTextEdits = [];

    /// <summary>Creates the tree of a model.</summary>
    /// <param name="textModel">The model to build the tree of.</param>
    /// <param name="getLanguageConfiguration">Returns the language configuration of a language id.</param>
    public BracketPairsTree(TextModel textModel, Func<string, ResolvedLanguageConfiguration> getLanguageConfiguration)
    {
        ArgumentNullException.ThrowIfNull(textModel);
        ArgumentNullException.ThrowIfNull(getLanguageConfiguration);

        _textModel = textModel;
        _brackets = new LanguageAgnosticBracketTokens(new DenseKeyProvider<string>(), getLanguageConfiguration);

        if (!textModel.Tokenization.HasTokens)
        {
            var brackets = _brackets.GetSingleLanguageBracketTokens(_textModel.Tokenization.GetLanguageId());
            var tokenizer = new FastTokenizer(_textModel.GetValue(), brackets);
            _initialAstWithoutTokens = BracketPairParser.ParseDocument(tokenizer, [], null, true);
            _astWithTokens = _initialAstWithoutTokens;
        }
        else if (textModel.Tokenization.BackgroundTokenizationState == BackgroundTokenizationState.Completed)
        {
            // Skip the initial ast, as there is no flickering.
            // Directly create the tree with token information.
            _initialAstWithoutTokens = null;
            _astWithTokens = ParseDocumentFromTextBuffer([], null, false);
        }
        else
        {
            // We missed some token changes already, so we cannot use the fast tokenizer + delta increments
            _initialAstWithoutTokens = ParseDocumentFromTextBuffer([], null, true);
            _astWithTokens = _initialAstWithoutTokens;
        }
    }

    /// <summary>The tree changed in a way that affects the brackets of the text (Monaco: <c>onDidChange</c>).</summary>
    public event EventHandler? Changed;

    /// <summary>Whether the language configuration of <paramref name="languageId"/> was used by this tree.</summary>
    public bool DidLanguageChange(string languageId)
    {
        return _brackets.DidLanguageChange(languageId);
    }

    #region TextModel events

    /// <summary>
    /// Drops the initial tree when the background tokenization completed, and fires <see cref="Changed"/> if there
    /// was one.
    /// </summary>
    public void HandleDidChangeBackgroundTokenizationState()
    {
        if (_textModel.Tokenization.BackgroundTokenizationState == BackgroundTokenizationState.Completed)
        {
            var wasNull = _initialAstWithoutTokens is null;
            // Clear the initial tree as we can use the tree with token information now.
            _initialAstWithoutTokens = null;
            if (!wasNull)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Queues the lines whose tokens changed as edits.</summary>
    public void HandleDidChangeTokens(ModelTokensChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var edits = new List<TextEditInfo>(e.Ranges.Count);
        foreach (var r in e.Ranges)
        {
            edits.Add(new TextEditInfo(
                Length.Of(r.FromLineNumber - 1, 0),
                Length.Of(r.ToLineNumber, 0),
                Length.Of(r.ToLineNumber - r.FromLineNumber + 1, 0)));
        }

        HandleEdits(edits, true);

        if (_initialAstWithoutTokens is null)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Queues the text changes of the model as edits.</summary>
    public void HandleContentChanged(ModelContentChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var edits = TextEditInfo.FromModelContentChanges(e.Changes);
        HandleEdits(edits, false);
    }

    private void HandleEdits(List<TextEditInfo> edits, bool tokenChange)
    {
        // Lazily queue the edits and only apply them when the tree is accessed.
        var result = TextEditInfoCombiner.Combine(_queuedTextEdits, edits);

        _queuedTextEdits = result;
        if (_initialAstWithoutTokens is not null && !tokenChange)
        {
            _queuedTextEditsForInitialAstWithoutTokens = TextEditInfoCombiner.Combine(_queuedTextEditsForInitialAstWithoutTokens, edits);
        }
    }

    #endregion

    private void FlushQueue()
    {
        if (_queuedTextEdits.Count > 0)
        {
            _astWithTokens = ParseDocumentFromTextBuffer(_queuedTextEdits, _astWithTokens, false);
            _queuedTextEdits = [];
        }

        if (_queuedTextEditsForInitialAstWithoutTokens.Count > 0)
        {
            if (_initialAstWithoutTokens is not null)
            {
                _initialAstWithoutTokens = ParseDocumentFromTextBuffer(_queuedTextEditsForInitialAstWithoutTokens, _initialAstWithoutTokens, false);
            }

            _queuedTextEditsForInitialAstWithoutTokens = [];
        }
    }

    private AstNode ParseDocumentFromTextBuffer(List<TextEditInfo> edits, AstNode? previousAst, bool immutable)
    {
        // Is much faster if `isPure = false`: the previous AST is reused (and mutated) without a deep clone.
        var tokenizer = new TextBufferTokenizer(_textModel, _brackets);
        return BracketPairParser.ParseDocument(tokenizer, edits, previousAst, immutable);
    }

    /// <summary>
    /// The brackets of <paramref name="range"/> with their nesting levels. The queued edits are applied now; the walk
    /// of the tree runs when the result is iterated.
    /// </summary>
    /// <param name="range">The range to search.</param>
    /// <param name="onlyColorizedBrackets">Whether to skip the pairs that are not colorized.</param>
    public CallbackIterable<BracketInfo> GetBracketsInRange(Range range, bool onlyColorizedBrackets)
    {
        FlushQueue();

        var startOffset = Length.Of(range.StartLineNumber - 1, range.StartColumn - 1);
        var endOffset = Length.Of(range.EndLineNumber - 1, range.EndColumn - 1);
        return new CallbackIterable<BracketInfo>(cb =>
        {
            var node = _initialAstWithoutTokens ?? _astWithTokens!;
            CollectBrackets(node, Length.Zero, node.Length, startOffset, endOffset, cb, 0, 0, [], onlyColorizedBrackets);
        });
    }

    /// <summary>
    /// The bracket pairs of <paramref name="range"/>, in order of their opening bracket. The queued edits are applied
    /// now; the walk of the tree runs when the result is iterated.
    /// </summary>
    /// <param name="range">The range to search.</param>
    /// <param name="includeMinIndentation">Whether to compute the minimum indentation inside each pair.</param>
    public CallbackIterable<BracketPairWithMinIndentationInfo> GetBracketPairsInRange(Range range, bool includeMinIndentation)
    {
        FlushQueue();

        var startLength = Length.FromPosition(range.Start);
        var endLength = Length.FromPosition(range.End);

        return new CallbackIterable<BracketPairWithMinIndentationInfo>(cb =>
        {
            var node = _initialAstWithoutTokens ?? _astWithTokens!;
            var context = new CollectBracketPairsContext(cb, includeMinIndentation, _textModel);
            CollectBracketPairs(node, Length.Zero, node.Length, startLength, endLength, context, 0, []);
        });
    }

    /// <summary>
    /// Whether an unmatched closing bracket that could close <paramref name="openingBracket"/> follows
    /// <paramref name="position"/>. Always false until the tokens of the whole model are accurate.
    /// </summary>
    public bool HasUnmatchedClosingBracketAfter(Position position, OpeningBracketKind openingBracket)
    {
        ArgumentNullException.ThrowIfNull(openingBracket);

        // Defer semantic bracket checks until tokenization is accurate for the entire model.
        if (!_textModel.Tokenization.HasAccurateTokensForLine(_textModel.LineCount))
        {
            return false;
        }

        FlushQueue();
        var openingBracketToken = _brackets.GetToken(openingBracket.BracketText, openingBracket.LanguageId);
        if (openingBracketToken is null || openingBracketToken.Kind != TokenKind.OpeningBracket)
        {
            return false;
        }

        var node = _astWithTokens!;
        return HasUnmatchedClosingBracketAfter(node, Length.FromPosition(position), openingBracketToken);
    }

    /// <summary>The first bracket that starts after <paramref name="position"/>, or <see langword="null"/>.</summary>
    public FoundBracket? GetFirstBracketAfter(Position position)
    {
        FlushQueue();

        var node = _initialAstWithoutTokens ?? _astWithTokens!;
        return GetFirstBracketAfter(node, Length.Zero, node.Length, Length.FromPosition(position));
    }

    /// <summary>The last bracket that starts before <paramref name="position"/>, or <see langword="null"/>.</summary>
    public FoundBracket? GetFirstBracketBefore(Position position)
    {
        FlushQueue();

        var node = _initialAstWithoutTokens ?? _astWithTokens!;
        return GetFirstBracketBefore(node, Length.Zero, node.Length, Length.FromPosition(position));
    }

    private static bool HasUnmatchedClosingBracketAfter(AstNode root, Length positionLength, Token openingBracketToken)
    {
        var openingBracketIds = openingBracketToken.BracketIds;
        if (!root.MissingOpeningBracketIds.Intersects(openingBracketIds))
        {
            return false;
        }

        var nodesToVisit = new List<(AstNode Node, Length Offset)> { (root, Length.Zero) };
        while (nodesToVisit.Count > 0)
        {
            var (node, offset) = nodesToVisit[^1];
            nodesToVisit.RemoveAt(nodesToVisit.Count - 1);

            // UnexpectedClosingBracket nodes represent globally unmatched closing brackets.
            if (node.Kind == AstNodeKind.UnexpectedClosingBracket)
            {
                // Ignore unmatched closers before the cursor because the new opener cannot pair with them.
                if (Length.GreaterThanEqual(offset, positionLength))
                {
                    return true;
                }

                continue;
            }

            var childOffset = offset;
            foreach (var child in node.Children)
            {
                // child.Length is the span of source text represented by that AST child, not its number of descendants
                var childEndOffset = Length.Add(childOffset, child.Length);
                var childEndsAfterPosition = Length.GreaterThanEqual(childEndOffset, positionLength);
                var childCanContainMatchingOrphan = child.MissingOpeningBracketIds.Intersects(openingBracketIds);

                // Visit only children that are after the cursor and may contain the matching orphan
                if (childEndsAfterPosition && childCanContainMatchingOrphan)
                {
                    nodesToVisit.Add((child, childOffset));
                }

                childOffset = childEndOffset;
            }
        }

        return false;
    }
}
