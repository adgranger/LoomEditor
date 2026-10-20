// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/ast.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>
/// A bracket pair including its child (e.g. <c>{ ... }</c>). Might be unclosed (Monaco: <c>PairAstNode</c>).
/// </summary>
/// <remarks>Threading: immutable after construction (if all children are immutable); read on the UI thread.</remarks>
internal sealed class PairAstNode : AstNode
{
    private PairAstNode(
        Length length,
        BracketAstNode openingBracket,
        AstNode? child,
        BracketAstNode? closingBracket,
        SmallImmutableSet<int> missingOpeningBracketIds)
        : base(length)
    {
        OpeningBracket = openingBracket;
        Child = child;
        ClosingBracket = closingBracket;
        MissingOpeningBracketIds = missingOpeningBracketIds;
    }

    public BracketAstNode OpeningBracket { get; }

    public AstNode? Child { get; }

    public BracketAstNode? ClosingBracket { get; }

    public override SmallImmutableSet<int> MissingOpeningBracketIds { get; }

    public override AstNodeKind Kind => AstNodeKind.Pair;

    public override int ListHeight => 0;

    public override int ChildrenLength => 3;

    /// <summary>Avoid using this property, it allocates a list!</summary>
    public override IReadOnlyList<AstNode> Children
    {
        get
        {
            var result = new List<AstNode> { OpeningBracket };
            if (Child is not null)
            {
                result.Add(Child);
            }

            if (ClosingBracket is not null)
            {
                result.Add(ClosingBracket);
            }

            return result;
        }
    }

    public static PairAstNode Create(BracketAstNode openingBracket, AstNode? child, BracketAstNode? closingBracket)
    {
        var length = openingBracket.Length;
        if (child is not null)
        {
            length = Length.Add(length, child.Length);
        }

        if (closingBracket is not null)
        {
            length = Length.Add(length, closingBracket.Length);
        }

        return new PairAstNode(length, openingBracket, child, closingBracket, child?.MissingOpeningBracketIds ?? SmallImmutableSet<int>.Empty);
    }

    public override AstNode? GetChild(int idx)
    {
        return idx switch
        {
            0 => OpeningBracket,
            1 => Child,
            2 => ClosingBracket,
            _ => throw new ArgumentOutOfRangeException(nameof(idx), idx, "Invalid child index"),
        };
    }

    public override bool CanBeReused(SmallImmutableSet<int> openBracketIds)
    {
        if (ClosingBracket is null)
        {
            // Unclosed pair ast nodes only end at the end of the document or when a parent node is closed.
            // This could be improved: only return false if some next token is neither "undefined" nor a bracket that closes a parent.
            return false;
        }

        return !openBracketIds.Intersects(MissingOpeningBracketIds);
    }

    public override AstNode FlattenLists()
    {
        return Create((BracketAstNode)OpeningBracket.FlattenLists(), Child?.FlattenLists(), (BracketAstNode?)ClosingBracket?.FlattenLists());
    }

    public override AstNode DeepClone()
    {
        return new PairAstNode(
            Length,
            (BracketAstNode)OpeningBracket.DeepClone(),
            Child?.DeepClone(),
            (BracketAstNode?)ClosingBracket?.DeepClone(),
            MissingOpeningBracketIds);
    }

    public override int ComputeMinIndentation(Length offset, TextModel textModel)
    {
        return Child is not null ? Child.ComputeMinIndentation(Length.Add(offset, OpeningBracket.Length), textModel) : int.MaxValue;
    }
}
