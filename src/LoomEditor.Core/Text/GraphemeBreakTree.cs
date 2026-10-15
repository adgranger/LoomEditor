// Ported from microsoft/vscode: src/vs/base/common/strings.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>Looks up the grapheme break type of a code point in Monaco's implicit binary tree (Monaco: <c>GraphemeBreakTree</c>).</summary>
/// <remarks>Threading: immutable singleton; safe to share between threads.</remarks>
internal sealed class GraphemeBreakTree
{
    private readonly int[] _data;

    private GraphemeBreakTree()
    {
        _data = GraphemeBreakData.Raw.ToArray();
    }

    /// <summary>The shared instance.</summary>
    public static GraphemeBreakTree Instance { get; } = new();

    /// <summary>Returns the break type of <paramref name="codePoint"/>.</summary>
    public GraphemeBreakType GetGraphemeBreakType(int codePoint)
    {
        // Let's make 7bit ASCII a bit faster: 0..31
        if (codePoint < 32)
        {
            if (codePoint == '\n')
            {
                return GraphemeBreakType.LF;
            }

            if (codePoint == '\r')
            {
                return GraphemeBreakType.CR;
            }

            return GraphemeBreakType.Control;
        }

        // Let's make 7bit ASCII a bit faster: 32..126
        if (codePoint < 127)
        {
            return GraphemeBreakType.Other;
        }

        var data = _data;
        var nodeCount = data.Length / 3;
        var nodeIndex = 1;
        while (nodeIndex <= nodeCount)
        {
            if (codePoint < data[3 * nodeIndex])
            {
                // go left
                nodeIndex = 2 * nodeIndex;
            }
            else if (codePoint > data[(3 * nodeIndex) + 1])
            {
                // go right
                nodeIndex = (2 * nodeIndex) + 1;
            }
            else
            {
                // hit
                return (GraphemeBreakType)data[(3 * nodeIndex) + 2];
            }
        }

        return GraphemeBreakType.Other;
    }
}
