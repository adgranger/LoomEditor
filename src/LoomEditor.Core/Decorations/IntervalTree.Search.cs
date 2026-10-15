// Ported from microsoft/vscode: src/vs/editor/common/model/intervalTree.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

// Searching. The traversals use the IsVisited metadata bit as an explicit-stack marker; each one leaves every bit cleared.
internal sealed partial class IntervalTree
{
    /// <summary>
    /// The nodes that intersect [<paramref name="start"/>, <paramref name="end"/>], in order. Sets the cached absolute
    /// offsets of the nodes it visits.
    /// </summary>
    public List<IntervalNode> IntervalSearch(
        long start,
        long end,
        int filterOwnerId,
        bool filterOutValidation,
        bool filterFontDecorations,
        int cachedVersionId,
        bool onlyMarginDecorations)
    {
        if (Root == Sentinel)
        {
            return [];
        }

        return IntervalSearchCore(start, end, filterOwnerId, filterOutValidation, filterFontDecorations, cachedVersionId, onlyMarginDecorations);
    }

    /// <summary>All the nodes that pass the filters, in order. Sets the cached absolute offsets of every node.</summary>
    public List<IntervalNode> Search(
        int filterOwnerId,
        bool filterOutValidation,
        bool filterFontDecorations,
        int cachedVersionId,
        bool onlyMarginDecorations)
    {
        if (Root == Sentinel)
        {
            return [];
        }

        return SearchCore(filterOwnerId, filterOutValidation, filterFontDecorations, cachedVersionId, onlyMarginDecorations);
    }

    /// <summary>Will not set <c>CachedAbsoluteStart</c> nor <c>CachedAbsoluteEnd</c> on the returned nodes.</summary>
    public List<IntervalNode> CollectNodesFromOwner(int ownerId)
    {
        var node = Root;
        var result = new List<IntervalNode>();
        while (node != Sentinel)
        {
            if (node.IsVisited)
            {
                // going up from this node
                node.Left.IsVisited = false;
                node.Right.IsVisited = false;
                node = node.Parent;
                continue;
            }

            if (node.Left != Sentinel && !node.Left.IsVisited)
            {
                // go left
                node = node.Left;
                continue;
            }

            // handle current node
            if (node.OwnerId == ownerId)
            {
                result.Add(node);
            }

            node.IsVisited = true;

            if (node.Right != Sentinel && !node.Right.IsVisited)
            {
                // go right
                node = node.Right;
                continue;
            }
        }

        Root.IsVisited = false;

        return result;
    }

    /// <summary>Will not set <c>CachedAbsoluteStart</c> nor <c>CachedAbsoluteEnd</c> on the returned nodes.</summary>
    public List<IntervalNode> CollectNodesPostOrder()
    {
        var node = Root;
        var result = new List<IntervalNode>();
        while (node != Sentinel)
        {
            if (node.IsVisited)
            {
                // going up from this node
                node.Left.IsVisited = false;
                node.Right.IsVisited = false;
                node = node.Parent;
                continue;
            }

            if (node.Left != Sentinel && !node.Left.IsVisited)
            {
                // go left
                node = node.Left;
                continue;
            }

            if (node.Right != Sentinel && !node.Right.IsVisited)
            {
                // go right
                node = node.Right;
                continue;
            }

            // handle current node
            result.Add(node);
            node.IsVisited = true;
        }

        Root.IsVisited = false;

        return result;
    }

    /// <summary>Computes the absolute offsets of one node and stores them in its cache.</summary>
    public void ResolveNode(IntervalNode node, int cachedVersionId)
    {
        var initialNode = node;
        long delta = 0;
        while (node != Root)
        {
            if (node == node.Parent.Right)
            {
                delta += node.Parent.Delta;
            }

            node = node.Parent;
        }

        var nodeStart = initialNode.Start + delta;
        var nodeEnd = initialNode.End + delta;
        initialNode.SetCachedOffsets(nodeStart, nodeEnd, cachedVersionId);
    }

    /// <summary>All the nodes, in order, with their cached absolute offsets set (version id 0).</summary>
    public List<IntervalNode> GetAllInOrder() => Search(0, false, false, 0, false);

    private static bool Include(IntervalNode node, int filterOwnerId, bool filterOutValidation, bool filterFontDecorations, bool onlyMarginDecorations)
    {
        var include = true;
        if (filterOwnerId != 0 && node.OwnerId != 0 && node.OwnerId != filterOwnerId)
        {
            include = false;
        }

        if (filterOutValidation && node.IsForValidation)
        {
            include = false;
        }

        if (filterFontDecorations && node.AffectsFont)
        {
            include = false;
        }

        if (onlyMarginDecorations && !node.IsInGlyphMargin)
        {
            include = false;
        }

        return include;
    }

    private List<IntervalNode> SearchCore(
        int filterOwnerId,
        bool filterOutValidation,
        bool filterFontDecorations,
        int cachedVersionId,
        bool onlyMarginDecorations)
    {
        var node = Root;
        long delta = 0;
        var result = new List<IntervalNode>();
        while (node != Sentinel)
        {
            if (node.IsVisited)
            {
                // going up from this node
                node.Left.IsVisited = false;
                node.Right.IsVisited = false;
                if (node == node.Parent.Right)
                {
                    delta -= node.Parent.Delta;
                }

                node = node.Parent;
                continue;
            }

            if (node.Left != Sentinel && !node.Left.IsVisited)
            {
                // go left
                node = node.Left;
                continue;
            }

            // handle current node
            var nodeStart = delta + node.Start;
            var nodeEnd = delta + node.End;

            node.SetCachedOffsets(nodeStart, nodeEnd, cachedVersionId);

            if (Include(node, filterOwnerId, filterOutValidation, filterFontDecorations, onlyMarginDecorations))
            {
                result.Add(node);
            }

            node.IsVisited = true;

            if (node.Right != Sentinel && !node.Right.IsVisited)
            {
                // go right
                delta += node.Delta;
                node = node.Right;
                continue;
            }
        }

        Root.IsVisited = false;

        return result;
    }

    private List<IntervalNode> IntervalSearchCore(
        long intervalStart,
        long intervalEnd,
        int filterOwnerId,
        bool filterOutValidation,
        bool filterFontDecorations,
        int cachedVersionId,
        bool onlyMarginDecorations)
    {
        // https://en.wikipedia.org/wiki/Interval_tree#Augmented_tree
        // Now, it is known that two intervals A and B overlap only when both
        // A.low <= B.high and A.high >= B.low. When searching the trees for
        // nodes overlapping with a given interval, you can immediately skip:
        //  a) all nodes to the right of nodes whose low value is past the end of the given interval.
        //  b) all nodes that have their maximum 'high' value below the start of the given interval.
        var node = Root;
        long delta = 0;
        var result = new List<IntervalNode>();
        while (node != Sentinel)
        {
            if (node.IsVisited)
            {
                // going up from this node
                node.Left.IsVisited = false;
                node.Right.IsVisited = false;
                if (node == node.Parent.Right)
                {
                    delta -= node.Parent.Delta;
                }

                node = node.Parent;
                continue;
            }

            if (!node.Left.IsVisited)
            {
                // first time seeing this node
                var nodeMaxEnd = delta + node.MaxEnd;
                if (nodeMaxEnd < intervalStart)
                {
                    // cover case b) from above
                    // there is no need to search this node or its children
                    node.IsVisited = true;
                    continue;
                }

                if (node.Left != Sentinel)
                {
                    // go left
                    node = node.Left;
                    continue;
                }
            }

            // handle current node
            var nodeStart = delta + node.Start;
            if (nodeStart > intervalEnd)
            {
                // cover case a) from above
                // there is no need to search this node or its right subtree
                node.IsVisited = true;
                continue;
            }

            var nodeEnd = delta + node.End;

            if (nodeEnd >= intervalStart)
            {
                // There is overlap
                node.SetCachedOffsets(nodeStart, nodeEnd, cachedVersionId);

                if (Include(node, filterOwnerId, filterOutValidation, filterFontDecorations, onlyMarginDecorations))
                {
                    result.Add(node);
                }
            }

            node.IsVisited = true;

            if (node.Right != Sentinel && !node.Right.IsVisited)
            {
                // go right
                delta += node.Delta;
                node = node.Right;
                continue;
            }
        }

        Root.IsVisited = false;

        return result;
    }
}
