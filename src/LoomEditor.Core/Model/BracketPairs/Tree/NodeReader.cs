// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/nodeReader.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>
/// Allows to efficiently find a longest child at a given offset in a fixed node.
/// The requested offsets must increase monotonously (Monaco: <c>NodeReader</c>).
/// </summary>
/// <remarks>Threading: not thread-safe; used by one parser on the UI thread.</remarks>
internal sealed class NodeReader
{
    private readonly List<AstNode> _nextNodes;
    private readonly List<Length> _offsets;
    private readonly List<int> _idxs = [];
    private Length _lastOffset = Length.Zero;

    public NodeReader(AstNode node)
    {
        _nextNodes = [node];
        _offsets = [Length.Zero];
    }

    /// <summary>Returns the longest node at <paramref name="offset"/> that satisfies the predicate.</summary>
    /// <param name="offset">Must be greater than or equal to the last offset this method has been called with.</param>
    /// <param name="predicate">The node must satisfy it to be returned.</param>
    /// <exception cref="InvalidOperationException"><paramref name="offset"/> is lower than the previous one.</exception>
    public AstNode? ReadLongestNodeAt(Length offset, Func<AstNode, bool> predicate)
    {
        if (Length.LessThan(offset, _lastOffset))
        {
            throw new InvalidOperationException("Invalid offset");
        }

        _lastOffset = offset;

        // Find the longest node of all those that are closest to the current offset.
        while (true)
        {
            if (_nextNodes.Count == 0)
            {
                return null;
            }

            var curNode = _nextNodes[^1];
            var curNodeOffset = _offsets[^1];

            if (Length.LessThan(offset, curNodeOffset))
            {
                // The next best node is not here yet. The reader must advance before a cached node is hit.
                return null;
            }

            if (Length.LessThan(curNodeOffset, offset))
            {
                // The reader is ahead of the current node.
                if (Length.LessThanEqual(Length.Add(curNodeOffset, curNode.Length), offset))
                {
                    // The reader is after the end of the current node.
                    NextNodeAfterCurrent();
                }
                else
                {
                    // The reader is somewhere in the current node.
                    var nextChildIdx = GetNextChildIdx(curNode);
                    if (nextChildIdx != -1)
                    {
                        // Go to the first child and repeat.
                        _nextNodes.Add(curNode.GetChild(nextChildIdx)!);
                        _offsets.Add(curNodeOffset);
                        _idxs.Add(nextChildIdx);
                    }
                    else
                    {
                        // We do not have children.
                        NextNodeAfterCurrent();
                    }
                }
            }
            else
            {
                // The reader offset equals curNodeOffset.
                if (predicate(curNode))
                {
                    NextNodeAfterCurrent();
                    return curNode;
                }

                var nextChildIdx = GetNextChildIdx(curNode);

                // Look for a shorter node.
                if (nextChildIdx == -1)
                {
                    // There is no shorter node.
                    NextNodeAfterCurrent();
                    return null;
                }

                // Descend into the first child and repeat.
                _nextNodes.Add(curNode.GetChild(nextChildIdx)!);
                _offsets.Add(curNodeOffset);
                _idxs.Add(nextChildIdx);
            }
        }
    }

    // Navigates to the longest node that continues after the current node.
    private void NextNodeAfterCurrent()
    {
        while (true)
        {
            var currentOffset = _offsets[^1];
            var currentNode = _nextNodes[^1];
            _nextNodes.RemoveAt(_nextNodes.Count - 1);
            _offsets.RemoveAt(_offsets.Count - 1);

            if (_idxs.Count == 0)
            {
                // We just popped the root node, there is no next node.
                break;
            }

            // The parent exists, because idxs is not empty.
            var parent = _nextNodes[^1];
            var nextChildIdx = GetNextChildIdx(parent, _idxs[^1]);

            if (nextChildIdx != -1)
            {
                _nextNodes.Add(parent.GetChild(nextChildIdx)!);
                _offsets.Add(Length.Add(currentOffset, currentNode.Length));
                _idxs[^1] = nextChildIdx;
                break;
            }

            _idxs.RemoveAt(_idxs.Count - 1);

            // We fully consumed the parent. The current node is now the parent, so loop again.
        }
    }

    private static int GetNextChildIdx(AstNode node, int curIdx = -1)
    {
        while (true)
        {
            curIdx++;
            if (curIdx >= node.ChildrenLength)
            {
                return -1;
            }

            if (node.GetChild(curIdx) is not null)
            {
                return curIdx;
            }
        }
    }
}
