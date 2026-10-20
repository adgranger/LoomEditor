// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/ast.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>
/// The base implementation for all AST nodes (Monaco: the <c>AstNode</c> union and <c>BaseAstNode</c>).
/// Code that narrows on <see cref="Kind"/> switches on it and casts, exactly where Monaco narrows.
/// </summary>
/// <remarks>Threading: immutable after construction; read on the UI thread.</remarks>
internal abstract class AstNode
{
    // The one protected field of the code base (not private): ListAstNode updates it when its children change.
    protected Length _length;

    protected AstNode(Length length)
    {
        _length = length;
    }

    /// <summary>The kind of this node.</summary>
    public abstract AstNodeKind Kind { get; }

    /// <summary>The number of children.</summary>
    public abstract int ChildrenLength { get; }

    /// <summary>
    /// Represents the set of all (potentially) missing opening bracket ids in this node.
    /// E.g. in <c>{ ] ) }</c> that set is {<c>[</c>, <c>(</c>}.
    /// </summary>
    public abstract SmallImmutableSet<int> MissingOpeningBracketIds { get; }

    /// <summary>In case of a list, the height of the (2,3) tree.</summary>
    public abstract int ListHeight { get; }

    /// <summary>The children. Try to avoid it: implementations might need to allocate the resulting list.</summary>
    public abstract IReadOnlyList<AstNode> Children { get; }

    /// <summary>The length of the entire node, which should equal the sum of lengths of all children.</summary>
    public Length Length => _length;

    /// <summary>Might return <c>null</c> even if <paramref name="idx"/> is smaller than <see cref="ChildrenLength"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is not a valid child index.</exception>
    public abstract AstNode? GetChild(int idx);

    /// <param name="openBracketIds">The set of all opening brackets that have not yet been closed.</param>
    public abstract bool CanBeReused(SmallImmutableSet<int> openBracketIds);

    /// <summary>Flattens all lists in this AST. Only for debugging.</summary>
    public abstract AstNode FlattenLists();

    /// <summary>Creates a deep clone.</summary>
    public abstract AstNode DeepClone();

    /// <summary>The smallest indentation (in visible columns) of the lines of this node, or <see cref="int.MaxValue"/>.</summary>
    public abstract int ComputeMinIndentation(Length offset, TextModel textModel);
}
