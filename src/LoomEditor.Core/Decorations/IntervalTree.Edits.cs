// Ported from microsoft/vscode: src/vs/editor/common/model/intervalTree.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.Core.Decorations;

internal sealed partial class IntervalTree
{
    /// <summary>
    /// Applies a text replacement to every interval: <paramref name="length"/> characters at <paramref name="offset"/>
    /// are replaced by <paramref name="textLength"/> characters.
    /// </summary>
    public void AcceptReplace(long offset, long length, long textLength, bool forceMoveMarkers)
    {
        // Our strategy is to remove all directly impacted nodes, and then add them back to the tree.

        // (1) collect all nodes that are intersecting this edit as nodes of interest
        var nodesOfInterest = SearchForEditing(offset, offset + length);

        // (2) remove all nodes that are intersecting this edit
        for (int i = 0, len = nodesOfInterest.Count; i < len; i++)
        {
            RbTreeDelete(nodesOfInterest[i]);
        }

        NormalizeDeltaIfNecessary();

        // (3) edit all tree nodes except the nodes of interest
        NoOverlapReplace(offset, offset + length, textLength);
        NormalizeDeltaIfNecessary();

        // (4) edit the nodes of interest and insert them back in the tree
        for (int i = 0, len = nodesOfInterest.Count; i < len; i++)
        {
            var node = nodesOfInterest[i];
            node.Start = node.CachedAbsoluteStart;
            node.End = node.CachedAbsoluteEnd;
            NodeAcceptEdit(node, offset, offset + length, textLength, forceMoveMarkers);
            node.MaxEnd = node.End;
            RbTreeInsert(node);
        }

        NormalizeDeltaIfNecessary();
    }

    private enum MarkerMoveSemantics
    {
        MarkerDefined = 0,
        ForceMove = 1,
        ForceStay = 2,
    }

    private static bool AdjustMarkerBeforeColumn(long markerOffset, bool markerStickToPreviousCharacter, long checkOffset, MarkerMoveSemantics moveSemantics)
    {
        if (markerOffset < checkOffset)
        {
            return true;
        }

        if (markerOffset > checkOffset)
        {
            return false;
        }

        if (moveSemantics == MarkerMoveSemantics.ForceMove)
        {
            return false;
        }

        if (moveSemantics == MarkerMoveSemantics.ForceStay)
        {
            return true;
        }

        return markerStickToPreviousCharacter;
    }

    /// <summary>
    /// Moves the interval of <paramref name="node"/> for the replacement of [<paramref name="start"/>, <paramref name="end"/>)
    /// by <paramref name="textLength"/> characters. This is a lot more complicated than strictly necessary to maintain
    /// the same behaviour as when decorations were implemented using two markers.
    /// </summary>
    internal static void NodeAcceptEdit(IntervalNode node, long start, long end, long textLength, bool forceMoveMarkers)
    {
        var nodeStickiness = node.Stickiness;
        var startStickToPreviousCharacter =
            nodeStickiness == TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges
            || nodeStickiness == TrackedRangeStickiness.GrowsOnlyWhenTypingBefore;
        var endStickToPreviousCharacter =
            nodeStickiness == TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges
            || nodeStickiness == TrackedRangeStickiness.GrowsOnlyWhenTypingBefore;

        var deletingCnt = end - start;
        var insertingCnt = textLength;
        var commonLength = Math.Min(deletingCnt, insertingCnt);

        var nodeStart = node.Start;
        var startDone = false;

        var nodeEnd = node.End;
        var endDone = false;

        if (start <= nodeStart && nodeEnd <= end && node.CollapseOnReplaceEdit)
        {
            // This edit encompasses the entire decoration range
            // and the decoration has asked to become collapsed
            node.Start = start;
            startDone = true;
            node.End = start;
            endDone = true;
        }

        {
            var moveSemantics = forceMoveMarkers
                ? MarkerMoveSemantics.ForceMove
                : (deletingCnt > 0 ? MarkerMoveSemantics.ForceStay : MarkerMoveSemantics.MarkerDefined);
            if (!startDone && AdjustMarkerBeforeColumn(nodeStart, startStickToPreviousCharacter, start, moveSemantics))
            {
                startDone = true;
            }

            if (!endDone && AdjustMarkerBeforeColumn(nodeEnd, endStickToPreviousCharacter, start, moveSemantics))
            {
                endDone = true;
            }
        }

        if (commonLength > 0 && !forceMoveMarkers)
        {
            var moveSemantics = deletingCnt > insertingCnt ? MarkerMoveSemantics.ForceStay : MarkerMoveSemantics.MarkerDefined;
            if (!startDone && AdjustMarkerBeforeColumn(nodeStart, startStickToPreviousCharacter, start + commonLength, moveSemantics))
            {
                startDone = true;
            }

            if (!endDone && AdjustMarkerBeforeColumn(nodeEnd, endStickToPreviousCharacter, start + commonLength, moveSemantics))
            {
                endDone = true;
            }
        }

        {
            var moveSemantics = forceMoveMarkers ? MarkerMoveSemantics.ForceMove : MarkerMoveSemantics.MarkerDefined;
            if (!startDone && AdjustMarkerBeforeColumn(nodeStart, startStickToPreviousCharacter, end, moveSemantics))
            {
                node.Start = start + insertingCnt;
                startDone = true;
            }

            if (!endDone && AdjustMarkerBeforeColumn(nodeEnd, endStickToPreviousCharacter, end, moveSemantics))
            {
                node.End = start + insertingCnt;
                endDone = true;
            }
        }

        // Finish
        var deltaColumn = insertingCnt - deletingCnt;
        if (!startDone)
        {
            node.Start = Math.Max(0, nodeStart + deltaColumn);
        }

        if (!endDone)
        {
            node.End = Math.Max(0, nodeEnd + deltaColumn);
        }

        if (node.Start > node.End)
        {
            node.End = node.Start;
        }
    }

    private List<IntervalNode> SearchForEditing(long start, long end)
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
                if (nodeMaxEnd < start)
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
            if (nodeStart > end)
            {
                // cover case a) from above
                // there is no need to search this node or its right subtree
                node.IsVisited = true;
                continue;
            }

            var nodeEnd = delta + node.End;
            if (nodeEnd >= start)
            {
                node.SetCachedOffsets(nodeStart, nodeEnd, 0);
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

    private void NoOverlapReplace(long start, long end, long textLength)
    {
        // Same traversal as SearchForEditing (see the comments there), shifting every node that is after the edit.
        var node = Root;
        long delta = 0;
        var editDelta = textLength - (end - start);
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

                RecomputeMaxEnd(node);
                node = node.Parent;
                continue;
            }

            if (!node.Left.IsVisited)
            {
                // first time seeing this node
                var nodeMaxEnd = delta + node.MaxEnd;
                if (nodeMaxEnd < start)
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
            if (nodeStart > end)
            {
                node.Start += editDelta;
                node.End += editDelta;
                node.Delta += editDelta;
                if (node.Delta < MinSafeDelta || node.Delta > MaxSafeDelta)
                {
                    RequestNormalizeDelta = true;
                }

                // cover case a) from above
                // there is no need to search this node or its right subtree
                node.IsVisited = true;
                continue;
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
    }
}
