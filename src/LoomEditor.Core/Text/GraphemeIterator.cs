// Ported from microsoft/vscode: src/vs/base/common/strings.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>Walks a string grapheme cluster by grapheme cluster, in both directions (Monaco: <c>GraphemeIterator</c>).</summary>
/// <remarks>Threading: mutable, not thread-safe; use from one thread.</remarks>
internal sealed class GraphemeIterator
{
    private readonly CodePointIterator _iterator;

    public GraphemeIterator(string str, int offset = 0)
    {
        _iterator = new CodePointIterator(str, offset);
    }

    /// <summary>Current UTF-16 offset.</summary>
    public int Offset => _iterator.Offset;

    /// <summary>Moves forward over one grapheme cluster and returns its length in UTF-16 units.</summary>
    public int NextGraphemeLength()
    {
        var tree = GraphemeBreakTree.Instance;
        var iterator = _iterator;
        var initialOffset = iterator.Offset;

        var graphemeBreakType = tree.GetGraphemeBreakType(iterator.NextCodePoint());
        while (!iterator.Eol())
        {
            var offset = iterator.Offset;
            var nextGraphemeBreakType = tree.GetGraphemeBreakType(iterator.NextCodePoint());
            if (Strings.BreakBetweenGraphemeBreakType(graphemeBreakType, nextGraphemeBreakType))
            {
                // move iterator back
                iterator.SetOffset(offset);
                break;
            }

            graphemeBreakType = nextGraphemeBreakType;
        }

        return iterator.Offset - initialOffset;
    }

    /// <summary>Moves backward over one grapheme cluster and returns its length in UTF-16 units.</summary>
    public int PrevGraphemeLength()
    {
        var tree = GraphemeBreakTree.Instance;
        var iterator = _iterator;
        var initialOffset = iterator.Offset;

        var graphemeBreakType = tree.GetGraphemeBreakType(iterator.PrevCodePoint());
        while (iterator.Offset > 0)
        {
            var offset = iterator.Offset;
            var prevGraphemeBreakType = tree.GetGraphemeBreakType(iterator.PrevCodePoint());
            if (Strings.BreakBetweenGraphemeBreakType(prevGraphemeBreakType, graphemeBreakType))
            {
                // move iterator back
                iterator.SetOffset(offset);
                break;
            }

            graphemeBreakType = prevGraphemeBreakType;
        }

        return initialOffset - iterator.Offset;
    }

    /// <summary>True when the offset is at or past the end of the string.</summary>
    public bool Eol() => _iterator.Eol();
}
