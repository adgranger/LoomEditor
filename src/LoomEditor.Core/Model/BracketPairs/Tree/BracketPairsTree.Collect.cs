// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/bracketPairsTree.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Supports;

namespace LoomEditor.Core.Model.BracketPairs.Tree;

internal sealed partial class BracketPairsTree
{
    private static FoundBracket? GetFirstBracketBefore(AstNode node, Length nodeOffsetStart, Length nodeOffsetEnd, Length position)
    {
        if (node.Kind is AstNodeKind.List or AstNodeKind.Pair)
        {
            var children = node.Children;
            var lengths = new List<(Length Start, Length End)>(children.Count);
            foreach (var child in children)
            {
                nodeOffsetEnd = Length.Add(nodeOffsetStart, child.Length);
                lengths.Add((nodeOffsetStart, nodeOffsetEnd));
                nodeOffsetStart = nodeOffsetEnd;
            }

            for (var i = lengths.Count - 1; i >= 0; i--)
            {
                var (start, end) = lengths[i];
                if (Length.LessThan(start, position))
                {
                    var result = GetFirstBracketBefore(children[i], start, end, position);
                    if (result is not null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        if (node.Kind == AstNodeKind.UnexpectedClosingBracket)
        {
            return null;
        }

        if (node.Kind == AstNodeKind.Bracket)
        {
            var range = Length.ToRange(nodeOffsetStart, nodeOffsetEnd);
            return new FoundBracket(range, ((BracketAstNode)node).BracketInfo);
        }

        return null;
    }

    private static FoundBracket? GetFirstBracketAfter(AstNode node, Length nodeOffsetStart, Length nodeOffsetEnd, Length position)
    {
        if (node.Kind is AstNodeKind.List or AstNodeKind.Pair)
        {
            foreach (var child in node.Children)
            {
                nodeOffsetEnd = Length.Add(nodeOffsetStart, child.Length);
                if (Length.LessThan(position, nodeOffsetEnd))
                {
                    var result = GetFirstBracketAfter(child, nodeOffsetStart, nodeOffsetEnd, position);
                    if (result is not null)
                    {
                        return result;
                    }
                }

                nodeOffsetStart = nodeOffsetEnd;
            }

            return null;
        }

        if (node.Kind == AstNodeKind.UnexpectedClosingBracket)
        {
            return null;
        }

        if (node.Kind == AstNodeKind.Bracket)
        {
            var range = Length.ToRange(nodeOffsetStart, nodeOffsetEnd);
            return new FoundBracket(range, ((BracketAstNode)node).BracketInfo);
        }

        return null;
    }

    // Monaco's `continue whileLoop` (jump to the loop head with another node) is `descend = true` and a `break` out of
    // the `for`; the code after the `switch` then continues the `while`.
    private static bool CollectBrackets(
        AstNode node,
        Length nodeOffsetStart,
        Length nodeOffsetEnd,
        Length startOffset,
        Length endOffset,
        Func<BracketInfo, bool> push,
        int level,
        int nestingLevelOfEqualBracketType,
        Dictionary<string, int> levelPerBracketType,
        bool onlyColorizedBrackets,
        bool parentPairIsIncomplete = false)
    {
        if (level > 200)
        {
            return true;
        }

        while (true)
        {
            var descend = false;
            switch (node.Kind)
            {
                case AstNodeKind.List:
                {
                    var childCount = node.ChildrenLength;
                    for (var i = 0; i < childCount; i++)
                    {
                        var child = node.GetChild(i);
                        if (child is null)
                        {
                            continue;
                        }

                        nodeOffsetEnd = Length.Add(nodeOffsetStart, child.Length);
                        if (Length.LessThanEqual(nodeOffsetStart, endOffset) && Length.GreaterThanEqual(nodeOffsetEnd, startOffset))
                        {
                            var childEndsAfterEnd = Length.GreaterThanEqual(nodeOffsetEnd, endOffset);
                            if (childEndsAfterEnd)
                            {
                                // No child after this child in the requested window, don't recurse
                                node = child;
                                descend = true;
                                break;
                            }

                            var shouldContinue = CollectBrackets(child, nodeOffsetStart, nodeOffsetEnd, startOffset, endOffset, push, level, 0, levelPerBracketType, onlyColorizedBrackets);
                            if (!shouldContinue)
                            {
                                return false;
                            }
                        }

                        nodeOffsetStart = nodeOffsetEnd;
                    }

                    if (!descend)
                    {
                        return true;
                    }

                    break;
                }

                case AstNodeKind.Pair:
                {
                    var pair = (PairAstNode)node;
                    var colorize = !onlyColorizedBrackets
                        || pair.ClosingBracket is null
                        || ((ClosingBracketKind)pair.ClosingBracket.BracketInfo).ClosesColorized((OpeningBracketKind)pair.OpeningBracket.BracketInfo);

                    levelPerBracketType.TryGetValue(pair.OpeningBracket.Text, out var levelPerBracket);
                    if (colorize)
                    {
                        levelPerBracketType[pair.OpeningBracket.Text] = levelPerBracket + 1;
                    }

                    var childCount = pair.ChildrenLength;
                    for (var i = 0; i < childCount; i++)
                    {
                        var child = pair.GetChild(i);
                        if (child is null)
                        {
                            continue;
                        }

                        nodeOffsetEnd = Length.Add(nodeOffsetStart, child.Length);
                        if (Length.LessThanEqual(nodeOffsetStart, endOffset) && Length.GreaterThanEqual(nodeOffsetEnd, startOffset))
                        {
                            var childEndsAfterEnd = Length.GreaterThanEqual(nodeOffsetEnd, endOffset);
                            if (childEndsAfterEnd && child.Kind != AstNodeKind.Bracket)
                            {
                                // No child after this child in the requested window, don't recurse
                                // Don't do this for brackets because of unclosed/unopened brackets
                                node = child;
                                if (colorize)
                                {
                                    level++;
                                    nestingLevelOfEqualBracketType = levelPerBracket + 1;
                                }
                                else
                                {
                                    nestingLevelOfEqualBracketType = levelPerBracket;
                                }

                                descend = true;
                                break;
                            }

                            if (colorize || child.Kind != AstNodeKind.Bracket || pair.ClosingBracket is null)
                            {
                                var shouldContinue = CollectBrackets(
                                    child,
                                    nodeOffsetStart,
                                    nodeOffsetEnd,
                                    startOffset,
                                    endOffset,
                                    push,
                                    colorize ? level + 1 : level,
                                    colorize ? levelPerBracket + 1 : levelPerBracket,
                                    levelPerBracketType,
                                    onlyColorizedBrackets,
                                    pair.ClosingBracket is null);
                                if (!shouldContinue)
                                {
                                    return false;
                                }
                            }
                        }

                        nodeOffsetStart = nodeOffsetEnd;
                    }

                    if (!descend)
                    {
                        levelPerBracketType[pair.OpeningBracket.Text] = levelPerBracket;
                        return true;
                    }

                    break;
                }

                case AstNodeKind.UnexpectedClosingBracket:
                {
                    var range = Length.ToRange(nodeOffsetStart, nodeOffsetEnd);
                    return push(new BracketInfo(range, level - 1, 0, true));
                }

                case AstNodeKind.Bracket:
                {
                    var range = Length.ToRange(nodeOffsetStart, nodeOffsetEnd);
                    return push(new BracketInfo(range, level - 1, nestingLevelOfEqualBracketType - 1, parentPairIsIncomplete));
                }

                case AstNodeKind.Text:
                    return true;

                default:
                    throw new InvalidOperationException("unexpected");
            }
        }
    }

    private sealed class CollectBracketPairsContext(
        Func<BracketPairWithMinIndentationInfo, bool> push,
        bool includeMinIndentation,
        TextModel textModel)
    {
        public Func<BracketPairWithMinIndentationInfo, bool> Push { get; } = push;

        public bool IncludeMinIndentation { get; } = includeMinIndentation;

        public TextModel TextModel { get; } = textModel;
    }

    private static bool CollectBracketPairs(
        AstNode node,
        Length nodeOffsetStart,
        Length nodeOffsetEnd,
        Length startOffset,
        Length endOffset,
        CollectBracketPairsContext context,
        int level,
        Dictionary<string, int> levelPerBracketType)
    {
        if (level > 200)
        {
            return true;
        }

        var shouldContinue = true;

        if (node.Kind == AstNodeKind.Pair)
        {
            var pair = (PairAstNode)node;

            levelPerBracketType.TryGetValue(pair.OpeningBracket.Text, out var levelPerBracket);
            levelPerBracketType[pair.OpeningBracket.Text] = levelPerBracket + 1;

            var openingBracketEnd = Length.Add(nodeOffsetStart, pair.OpeningBracket.Length);
            var minIndentation = -1;
            if (context.IncludeMinIndentation)
            {
                minIndentation = pair.ComputeMinIndentation(nodeOffsetStart, context.TextModel);
            }

            shouldContinue = context.Push(
                new BracketPairWithMinIndentationInfo(
                    Length.ToRange(nodeOffsetStart, nodeOffsetEnd),
                    Length.ToRange(nodeOffsetStart, openingBracketEnd),
                    pair.ClosingBracket is not null
                        ? Length.ToRange(Length.Add(openingBracketEnd, pair.Child?.Length ?? Length.Zero), nodeOffsetEnd)
                        : null,
                    level,
                    levelPerBracket,
                    pair,
                    minIndentation));

            nodeOffsetStart = openingBracketEnd;
            if (shouldContinue && pair.Child is not null)
            {
                var child = pair.Child;
                nodeOffsetEnd = Length.Add(nodeOffsetStart, child.Length);
                if (Length.LessThanEqual(nodeOffsetStart, endOffset) && Length.GreaterThanEqual(nodeOffsetEnd, startOffset))
                {
                    shouldContinue = CollectBracketPairs(
                        child,
                        nodeOffsetStart,
                        nodeOffsetEnd,
                        startOffset,
                        endOffset,
                        context,
                        level + 1,
                        levelPerBracketType);
                    if (!shouldContinue)
                    {
                        return false;
                    }
                }
            }

            levelPerBracketType[pair.OpeningBracket.Text] = levelPerBracket;
        }
        else
        {
            var curOffset = nodeOffsetStart;
            foreach (var child in node.Children)
            {
                var childOffset = curOffset;
                curOffset = Length.Add(curOffset, child.Length);

                if (Length.LessThanEqual(childOffset, endOffset) && Length.LessThanEqual(startOffset, curOffset))
                {
                    shouldContinue = CollectBracketPairs(
                        child,
                        childOffset,
                        curOffset,
                        startOffset,
                        endOffset,
                        context,
                        level,
                        levelPerBracketType);
                    if (!shouldContinue)
                    {
                        return false;
                    }
                }
            }
        }

        return shouldContinue;
    }
}
