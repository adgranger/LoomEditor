// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/ast.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>A list node that stores 2 or 3 children (Monaco: <c>TwoThreeListAstNode</c>).</summary>
/// <remarks>Threading: not thread-safe; read on the UI thread.</remarks>
internal class TwoThreeListAstNode : ListAstNode
{
    private AstNode _item1;
    private AstNode _item2;
    private AstNode? _item3;

    public TwoThreeListAstNode(
        Length length,
        int listHeight,
        AstNode item1,
        AstNode item2,
        AstNode? item3,
        SmallImmutableSet<int> missingOpeningBracketIds)
        : base(length, listHeight, missingOpeningBracketIds)
    {
        _item1 = item1;
        _item2 = item2;
        _item3 = item3;
    }

    public AstNode Item1 => _item1;

    public AstNode Item2 => _item2;

    public AstNode? Item3 => _item3;

    public override int ChildrenLength => _item3 is not null ? 3 : 2;

    public override IReadOnlyList<AstNode> Children => _item3 is not null ? [_item1, _item2, _item3] : [_item1, _item2];

    public override AstNode? GetChild(int idx)
    {
        return idx switch
        {
            0 => _item1,
            1 => _item2,
            2 => _item3,
            _ => throw new ArgumentOutOfRangeException(nameof(idx), idx, "Invalid child index"),
        };
    }

    protected override void SetChild(int idx, AstNode child)
    {
        switch (idx)
        {
            case 0:
                _item1 = child;
                return;
            case 1:
                _item2 = child;
                return;
            case 2:
                _item3 = child;
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(idx), idx, "Invalid child index");
        }
    }

    public override AstNode DeepClone()
    {
        return new TwoThreeListAstNode(Length, ListHeight, _item1.DeepClone(), _item2.DeepClone(), _item3?.DeepClone(), MissingOpeningBracketIds);
    }

    public override void AppendChildOfSameHeight(AstNode node)
    {
        if (_item3 is not null)
        {
            throw new InvalidOperationException("Cannot append to a full (2,3) tree node");
        }

        ThrowIfImmutable();
        _item3 = node;
        HandleChildrenChanged();
    }

    public override AstNode? UnappendChild()
    {
        if (_item3 is null)
        {
            throw new InvalidOperationException("Cannot remove from a non-full (2,3) tree node");
        }

        ThrowIfImmutable();
        var result = _item3;
        _item3 = null;
        HandleChildrenChanged();
        return result;
    }

    public override void PrependChildOfSameHeight(AstNode node)
    {
        if (_item3 is not null)
        {
            throw new InvalidOperationException("Cannot prepend to a full (2,3) tree node");
        }

        ThrowIfImmutable();
        _item3 = _item2;
        _item2 = _item1;
        _item1 = node;
        HandleChildrenChanged();
    }

    public override AstNode? UnprependChild()
    {
        if (_item3 is null)
        {
            throw new InvalidOperationException("Cannot remove from a non-full (2,3) tree node");
        }

        ThrowIfImmutable();
        var result = _item1;
        _item1 = _item2;
        _item2 = _item3;
        _item3 = null;
        HandleChildrenChanged();
        return result;
    }

    public override ListAstNode ToMutable()
    {
        return this;
    }
}

/// <summary>Immutable, if all children are immutable (Monaco: <c>Immutable23ListAstNode</c>).</summary>
/// <remarks>Threading: immutable after construction; read on the UI thread.</remarks>
internal sealed class Immutable23ListAstNode : TwoThreeListAstNode
{
    public Immutable23ListAstNode(
        Length length,
        int listHeight,
        AstNode item1,
        AstNode item2,
        AstNode? item3,
        SmallImmutableSet<int> missingOpeningBracketIds)
        : base(length, listHeight, item1, item2, item3, missingOpeningBracketIds)
    {
    }

    public override ListAstNode ToMutable()
    {
        return new TwoThreeListAstNode(Length, ListHeight, Item1, Item2, Item3, MissingOpeningBracketIds);
    }

    protected override void ThrowIfImmutable()
    {
        throw new InvalidOperationException("this instance is immutable");
    }
}

/// <summary>A list node that stores any number of children (Monaco: <c>ArrayListAstNode</c>).</summary>
/// <remarks>Threading: not thread-safe; read on the UI thread.</remarks>
internal class ArrayListAstNode : ListAstNode
{
    private readonly List<AstNode> _children;

    public ArrayListAstNode(Length length, int listHeight, List<AstNode> children, SmallImmutableSet<int> missingOpeningBracketIds)
        : base(length, listHeight, missingOpeningBracketIds)
    {
        _children = children;
    }

    public override int ChildrenLength => _children.Count;

    public override IReadOnlyList<AstNode> Children => _children;

    public override AstNode? GetChild(int idx)
    {
        return _children[idx];
    }

    protected override void SetChild(int idx, AstNode child)
    {
        _children[idx] = child;
    }

    public override AstNode DeepClone()
    {
        var children = new List<AstNode>(_children.Count);
        foreach (var child in _children)
        {
            children.Add(child.DeepClone());
        }

        return new ArrayListAstNode(Length, ListHeight, children, MissingOpeningBracketIds);
    }

    public override void AppendChildOfSameHeight(AstNode node)
    {
        ThrowIfImmutable();
        _children.Add(node);
        HandleChildrenChanged();
    }

    public override AstNode? UnappendChild()
    {
        ThrowIfImmutable();
        if (_children.Count == 0)
        {
            return null;
        }

        var item = _children[^1];
        _children.RemoveAt(_children.Count - 1);
        HandleChildrenChanged();
        return item;
    }

    public override void PrependChildOfSameHeight(AstNode node)
    {
        ThrowIfImmutable();
        _children.Insert(0, node);
        HandleChildrenChanged();
    }

    public override AstNode? UnprependChild()
    {
        ThrowIfImmutable();
        if (_children.Count == 0)
        {
            return null;
        }

        var item = _children[0];
        _children.RemoveAt(0);
        HandleChildrenChanged();
        return item;
    }

    public override ListAstNode ToMutable()
    {
        return this;
    }
}

/// <summary>Immutable, if all children are immutable (Monaco: <c>ImmutableArrayListAstNode</c>).</summary>
/// <remarks>Threading: immutable after construction; read on the UI thread.</remarks>
internal sealed class ImmutableArrayListAstNode : ArrayListAstNode
{
    public ImmutableArrayListAstNode(Length length, int listHeight, List<AstNode> children, SmallImmutableSet<int> missingOpeningBracketIds)
        : base(length, listHeight, children, missingOpeningBracketIds)
    {
    }

    public override ListAstNode ToMutable()
    {
        return new ArrayListAstNode(Length, ListHeight, [.. Children], MissingOpeningBracketIds);
    }

    protected override void ThrowIfImmutable()
    {
        throw new InvalidOperationException("this instance is immutable");
    }
}
