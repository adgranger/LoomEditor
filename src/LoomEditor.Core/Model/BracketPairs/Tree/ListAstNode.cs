// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/ast.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>
/// A list of nodes of the same height: a (2,3)-tree node or an array node (Monaco: <c>ListAstNode</c>).
/// Mutable and immutable variants exist, as in Monaco; immutable ones throw on every mutation.
/// </summary>
/// <remarks>Threading: not thread-safe when mutable; immutable variants are immutable if all children are; read on the UI thread.</remarks>
internal abstract class ListAstNode : AstNode
{
    private SmallImmutableSet<int> _missingOpeningBracketIds;
    private int _cachedMinIndentation = -1;

    /// <summary>Use <see cref="Create"/> or <see cref="Create23"/>.</summary>
    protected ListAstNode(Length length, int listHeight, SmallImmutableSet<int> missingOpeningBracketIds)
        : base(length)
    {
        ListHeight = listHeight;
        _missingOpeningBracketIds = missingOpeningBracketIds;
    }

    public override AstNodeKind Kind => AstNodeKind.List;

    public override int ListHeight { get; }

    public override SmallImmutableSet<int> MissingOpeningBracketIds => _missingOpeningBracketIds;

    /// <summary>Uses the more memory-efficient list nodes that can only store 2 or 3 children.</summary>
    public static ListAstNode Create23(AstNode item1, AstNode item2, AstNode? item3, bool immutable = false)
    {
        var length = item1.Length;
        var missingBracketIds = item1.MissingOpeningBracketIds;

        if (item1.ListHeight != item2.ListHeight)
        {
            throw new InvalidOperationException("Invalid list heights");
        }

        length = Length.Add(length, item2.Length);
        missingBracketIds = missingBracketIds.Merge(item2.MissingOpeningBracketIds);

        if (item3 is not null)
        {
            if (item1.ListHeight != item3.ListHeight)
            {
                throw new InvalidOperationException("Invalid list heights");
            }

            length = Length.Add(length, item3.Length);
            missingBracketIds = missingBracketIds.Merge(item3.MissingOpeningBracketIds);
        }

        return immutable
            ? new Immutable23ListAstNode(length, item1.ListHeight + 1, item1, item2, item3, missingBracketIds)
            : new TwoThreeListAstNode(length, item1.ListHeight + 1, item1, item2, item3, missingBracketIds);
    }

    /// <summary>Creates an array list node. The node keeps (and, when mutable, mutates) <paramref name="items"/>.</summary>
    public static ListAstNode Create(List<AstNode> items, bool immutable = false)
    {
        if (items.Count == 0)
        {
            return GetEmpty();
        }

        var length = items[0].Length;
        var unopenedBrackets = items[0].MissingOpeningBracketIds;
        for (var i = 1; i < items.Count; i++)
        {
            length = Length.Add(length, items[i].Length);
            unopenedBrackets = unopenedBrackets.Merge(items[i].MissingOpeningBracketIds);
        }

        return immutable
            ? new ImmutableArrayListAstNode(length, items[0].ListHeight + 1, items, unopenedBrackets)
            : new ArrayListAstNode(length, items[0].ListHeight + 1, items, unopenedBrackets);
    }

    /// <summary>A new immutable empty list (Monaco allocates each time).</summary>
    public static ListAstNode GetEmpty()
    {
        return new ImmutableArrayListAstNode(Length.Zero, 0, [], SmallImmutableSet<int>.Empty);
    }

    protected virtual void ThrowIfImmutable()
    {
    }

    protected abstract void SetChild(int idx, AstNode child);

    public AstNode? MakeLastElementMutable()
    {
        ThrowIfImmutable();
        var childCount = ChildrenLength;
        if (childCount == 0)
        {
            return null;
        }

        var lastChild = GetChild(childCount - 1)!;
        var mutable = lastChild is ListAstNode list ? list.ToMutable() : lastChild;
        if (!ReferenceEquals(lastChild, mutable))
        {
            SetChild(childCount - 1, mutable);
        }

        return mutable;
    }

    public AstNode? MakeFirstElementMutable()
    {
        ThrowIfImmutable();
        var childCount = ChildrenLength;
        if (childCount == 0)
        {
            return null;
        }

        var firstChild = GetChild(0)!;
        var mutable = firstChild is ListAstNode list ? list.ToMutable() : firstChild;
        if (!ReferenceEquals(firstChild, mutable))
        {
            SetChild(0, mutable);
        }

        return mutable;
    }

    public override bool CanBeReused(SmallImmutableSet<int> openBracketIds)
    {
        if (openBracketIds.Intersects(MissingOpeningBracketIds))
        {
            return false;
        }

        if (ChildrenLength == 0)
        {
            // Do not reuse empty lists.
            return false;
        }

        AstNode lastChild = this;
        while (lastChild.Kind == AstNodeKind.List)
        {
            var lastLength = lastChild.ChildrenLength;
            if (lastLength == 0)
            {
                // Empty lists should never be contained in other lists.
                throw new InvalidOperationException("An empty list is contained in another list.");
            }

            lastChild = lastChild.GetChild(lastLength - 1)!;
        }

        return lastChild.CanBeReused(openBracketIds);
    }

    public void HandleChildrenChanged()
    {
        ThrowIfImmutable();

        var count = ChildrenLength;
        var length = GetChild(0)!.Length;
        var unopenedBrackets = GetChild(0)!.MissingOpeningBracketIds;
        for (var i = 1; i < count; i++)
        {
            var child = GetChild(i)!;
            length = Length.Add(length, child.Length);
            unopenedBrackets = unopenedBrackets.Merge(child.MissingOpeningBracketIds);
        }

        _length = length;
        _missingOpeningBracketIds = unopenedBrackets;
        _cachedMinIndentation = -1;
    }

    public override AstNode FlattenLists()
    {
        var items = new List<AstNode>();
        foreach (var c in Children)
        {
            var normalized = c.FlattenLists();
            if (normalized.Kind == AstNodeKind.List)
            {
                items.AddRange(normalized.Children);
            }
            else
            {
                items.Add(normalized);
            }
        }

        return Create(items);
    }

    public override int ComputeMinIndentation(Length offset, TextModel textModel)
    {
        if (_cachedMinIndentation != -1)
        {
            return _cachedMinIndentation;
        }

        var minIndentation = int.MaxValue;
        var childOffset = offset;
        for (var i = 0; i < ChildrenLength; i++)
        {
            var child = GetChild(i);
            if (child is not null)
            {
                minIndentation = Math.Min(minIndentation, child.ComputeMinIndentation(childOffset, textModel));
                childOffset = Length.Add(childOffset, child.Length);
            }
        }

        _cachedMinIndentation = minIndentation;
        return minIndentation;
    }

    /// <summary>Creates a shallow clone that is mutable, or itself if it is already mutable.</summary>
    public abstract ListAstNode ToMutable();

    public abstract void AppendChildOfSameHeight(AstNode node);

    public abstract AstNode? UnappendChild();

    public abstract void PrependChildOfSameHeight(AstNode node);

    public abstract AstNode? UnprependChild();
}
