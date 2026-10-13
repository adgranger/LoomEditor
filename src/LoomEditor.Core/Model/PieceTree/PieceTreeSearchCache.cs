// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>A node remembered by <see cref="PieceTreeSearchCache"/> with where it starts.</summary>
/// <param name="Node">The cached node.</param>
/// <param name="NodeStartOffset">Offset of the node's first character in the document.</param>
/// <param name="NodeStartLineNumber">Line number at the node's start, when known (Monaco: optional <c>nodeStartLineNumber</c>).</param>
/// <remarks>Monaco: <c>CacheEntry</c>. Threading: immutable.</remarks>
internal sealed record CacheEntry(TreeNode Node, int NodeStartOffset, int? NodeStartLineNumber);

/// <summary>Remembers the last nodes found for an offset or a line number to skip tree walks on consecutive lookups.</summary>
/// <remarks>Monaco: <c>PieceTreeSearchCache</c>. Threading: not thread-safe; owned by one piece tree.</remarks>
internal sealed class PieceTreeSearchCache
{
    private readonly int _limit;
    private readonly List<CacheEntry> _cache = [];

    public PieceTreeSearchCache(int limit)
    {
        _limit = limit;
    }

    /// <summary>Finds the most recent entry whose node covers <paramref name="offset"/> (Monaco: <c>get</c>).</summary>
    public CacheEntry? Get(int offset)
    {
        for (int i = _cache.Count - 1; i >= 0; i--)
        {
            CacheEntry entry = _cache[i];
            if (entry.NodeStartOffset <= offset && entry.NodeStartOffset + entry.Node.Piece.Length >= offset)
            {
                return entry;
            }
        }

        return null;
    }

    /// <summary>Finds the most recent entry whose node contains <paramref name="lineNumber"/> (Monaco: <c>get2</c>).</summary>
    public CacheEntry? GetByLine(int lineNumber)
    {
        for (int i = _cache.Count - 1; i >= 0; i--)
        {
            CacheEntry entry = _cache[i];
            // Monaco tests truthiness, so a missing or zero start line number never matches.
            if (entry.NodeStartLineNumber is int start and not 0
                && start < lineNumber
                && start + entry.Node.Piece.LineFeedCount >= lineNumber)
            {
                return entry;
            }
        }

        return null;
    }

    /// <summary>Adds an entry, evicting the oldest one when the cache is full.</summary>
    public void Set(CacheEntry entry)
    {
        if (_cache.Count >= _limit)
        {
            _cache.RemoveAt(0);
        }

        _cache.Add(entry);
    }

    /// <summary>Drops entries whose node was detached or starts at or after <paramref name="offset"/>.</summary>
    public void Validate(int offset)
    {
        _cache.RemoveAll(entry => entry.Node.IsDetached || entry.NodeStartOffset >= offset);
    }
}
