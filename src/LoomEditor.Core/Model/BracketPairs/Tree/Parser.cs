// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/parser.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>Parses a token stream into a bracket pair AST (Monaco: <c>parseDocument</c> and <c>Parser</c>).</summary>
/// <remarks>Threading: single-threaded (UI thread); a parser is used for one parse only.</remarks>
internal static class BracketPairParser
{
    /// <summary>
    /// Parses the whole document, reusing the nodes of <paramref name="oldNode"/> that the <paramref name="edits"/>
    /// did not touch. Non incrementally built ASTs are immutable.
    /// </summary>
    /// <param name="tokenizer">The tokens of the new text.</param>
    /// <param name="edits">The edits that turned the old text into the new one, sorted in ascending order.</param>
    /// <param name="oldNode">The AST of the old text, or <see langword="null"/> to parse from scratch.</param>
    /// <param name="createImmutableLists">Whether the lists are immutable. Not supported with an old node.</param>
    /// <exception cref="InvalidOperationException">An old node is given together with immutable lists.</exception>
    public static AstNode ParseDocument(ITokenizer tokenizer, IReadOnlyList<TextEditInfo> edits, AstNode? oldNode, bool createImmutableLists)
    {
        var parser = new Parser(tokenizer, edits, oldNode, createImmutableLists);
        return parser.ParseDocument();
    }

    /// <summary>
    /// The parser. It recurses once per nesting level, which stops at level 300 (about 600 frames).
    /// Internal (not private) so that tests can read <see cref="NodesConstructed"/> and <see cref="NodesReused"/>.
    /// </summary>
    /// <remarks>Threading: single-threaded (UI thread); not reusable across threads.</remarks>
    internal sealed class Parser
    {
        private readonly ITokenizer _tokenizer;
        private readonly NodeReader? _oldNodeReader;
        private readonly BeforeEditPositionMapper _positionMapper;
        private readonly bool _createImmutableLists;
        private int _itemsConstructed;
        private int _itemsFromCache;

        public Parser(ITokenizer tokenizer, IReadOnlyList<TextEditInfo> edits, AstNode? oldNode, bool createImmutableLists)
        {
            ArgumentNullException.ThrowIfNull(tokenizer);
            ArgumentNullException.ThrowIfNull(edits);

            if (oldNode is not null && createImmutableLists)
            {
                throw new InvalidOperationException("Not supported");
            }

            _tokenizer = tokenizer;
            _createImmutableLists = createImmutableLists;
            _oldNodeReader = oldNode is not null ? new NodeReader(oldNode) : null;
            _positionMapper = new BeforeEditPositionMapper(edits);
        }

        /// <summary>Reports how many nodes were constructed in the last parse operation.</summary>
        public int NodesConstructed => _itemsConstructed;

        /// <summary>Reports how many nodes were reused in the last parse operation.</summary>
        public int NodesReused => _itemsFromCache;

        public AstNode ParseDocument()
        {
            _itemsConstructed = 0;
            _itemsFromCache = 0;

            return ParseList(SmallImmutableSet<int>.Empty, 0) ?? ListAstNode.GetEmpty();
        }

        private AstNode? ParseList(SmallImmutableSet<int> openedBracketIds, int level)
        {
            var items = new List<AstNode>();

            while (true)
            {
                var child = TryReadChildFromCache(openedBracketIds);

                if (child is null)
                {
                    var token = _tokenizer.Peek();
                    if (token is null
                        || (token.Kind == TokenKind.ClosingBracket && token.BracketIds.Intersects(openedBracketIds)))
                    {
                        break;
                    }

                    child = ParseChild(openedBracketIds, level + 1);
                }

                if (child.Kind == AstNodeKind.List && child.ChildrenLength == 0)
                {
                    continue;
                }

                items.Add(child);
            }

            // When there is no oldNodeReader, all items are created from scratch and must have the same height.
            return _oldNodeReader is not null
                ? Concat23Trees.Concat(items)
                : Concat23Trees.ConcatOfSameHeight(items, _createImmutableLists);
        }

        private AstNode? TryReadChildFromCache(SmallImmutableSet<int> openedBracketIds)
        {
            if (_oldNodeReader is null)
            {
                return null;
            }

            var maxCacheableLength = _positionMapper.GetDistanceToNextChange(_tokenizer.Offset);
            if (maxCacheableLength is not null && maxCacheableLength.Value.IsZero)
            {
                return null;
            }

            var cachedNode = _oldNodeReader.ReadLongestNodeAt(_positionMapper.GetOffsetBeforeChange(_tokenizer.Offset), curNode =>
            {
                // The edit could extend the ending token, thus we cannot re-use nodes that touch the edit.
                // If there is no edit anymore, we can re-use the node in any case.
                if (maxCacheableLength is not null && !Length.LessThan(curNode.Length, maxCacheableLength.Value))
                {
                    // Either the node contains edited text or touches edited text.
                    // In the latter case, brackets might have been extended (`end` -> `ending`), so even touching nodes cannot be reused.
                    return false;
                }

                return curNode.CanBeReused(openedBracketIds);
            });

            if (cachedNode is null)
            {
                return null;
            }

            _itemsFromCache++;
            _tokenizer.Skip(cachedNode.Length);
            return cachedNode;
        }

        private AstNode ParseChild(SmallImmutableSet<int> openedBracketIds, int level)
        {
            _itemsConstructed++;

            // The caller peeked a token (or the cache read failed after a peek), so there is one.
            var token = _tokenizer.Read()!;

            switch (token.Kind)
            {
                case TokenKind.ClosingBracket:
                    return new InvalidBracketAstNode(token.BracketIds, token.Length);

                case TokenKind.Text:
                    return token.AstNode!;

                case TokenKind.OpeningBracket:
                {
                    if (level > 300)
                    {
                        // To prevent stack overflows
                        return new TextAstNode(token.Length);
                    }

                    var set = openedBracketIds.Merge(token.BracketIds);
                    var child = ParseList(set, level + 1);

                    var nextToken = _tokenizer.Peek();
                    if (nextToken is not null
                        && nextToken.Kind == TokenKind.ClosingBracket
                        && (nextToken.BracketId == token.BracketId || nextToken.BracketIds.Intersects(token.BracketIds)))
                    {
                        _tokenizer.Read();
                        return PairAstNode.Create((BracketAstNode)token.AstNode!, child, (BracketAstNode)nextToken.AstNode!);
                    }

                    return PairAstNode.Create((BracketAstNode)token.AstNode!, child, null);
                }

                default:
                    throw new InvalidOperationException("unexpected");
            }
        }
    }
}
