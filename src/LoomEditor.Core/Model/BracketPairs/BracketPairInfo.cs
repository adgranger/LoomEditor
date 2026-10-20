// Ported from microsoft/vscode: src/vs/editor/common/textModelBracketPairs.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model.BracketPairs.Tree;

namespace LoomEditor.Core.Model.BracketPairs;

/// <summary>A bracket pair of the text with its nesting levels (Monaco: <c>BracketPairInfo</c>).</summary>
/// <remarks>Threading: immutable; read on the UI thread.</remarks>
internal class BracketPairInfo
{
    private readonly PairAstNode _bracketPairNode;

    /// <summary>Creates a bracket pair info.</summary>
    /// <param name="range">The range of the whole pair.</param>
    /// <param name="openingBracketRange">The range of the opening bracket.</param>
    /// <param name="closingBracketRange">The range of the closing bracket, or <see langword="null"/> if the pair is unclosed.</param>
    /// <param name="nestingLevel">The 0-based nesting level.</param>
    /// <param name="nestingLevelOfEqualBracketType">The 0-based nesting level among the pairs of the same type.</param>
    /// <param name="bracketPairNode">The AST node of the pair.</param>
    public BracketPairInfo(
        Range range,
        Range openingBracketRange,
        Range? closingBracketRange,
        int nestingLevel,
        int nestingLevelOfEqualBracketType,
        PairAstNode bracketPairNode)
    {
        ArgumentNullException.ThrowIfNull(bracketPairNode);

        Range = range;
        OpeningBracketRange = openingBracketRange;
        ClosingBracketRange = closingBracketRange;
        NestingLevel = nestingLevel;
        NestingLevelOfEqualBracketType = nestingLevelOfEqualBracketType;
        _bracketPairNode = bracketPairNode;
    }

    /// <summary>The range of the whole pair.</summary>
    public Range Range { get; }

    /// <summary>The range of the opening bracket.</summary>
    public Range OpeningBracketRange { get; }

    /// <summary>The range of the closing bracket, or <see langword="null"/> if the pair is unclosed.</summary>
    public Range? ClosingBracketRange { get; }

    /// <summary>The 0-based nesting level.</summary>
    public int NestingLevel { get; }

    /// <summary>The 0-based nesting level among the pairs of the same type.</summary>
    public int NestingLevelOfEqualBracketType { get; }

    /// <summary>The kind of the opening bracket.</summary>
    public OpeningBracketKind OpeningBracketInfo => (OpeningBracketKind)_bracketPairNode.OpeningBracket.BracketInfo;

    /// <summary>The kind of the closing bracket, or <see langword="null"/> if the pair is unclosed.</summary>
    public ClosingBracketKind? ClosingBracketInfo => (ClosingBracketKind?)_bracketPairNode.ClosingBracket?.BracketInfo;
}

/// <summary>A <see cref="BracketPairInfo"/> that also tells the minimum indentation inside the pair (Monaco: <c>BracketPairWithMinIndentationInfo</c>).</summary>
/// <remarks>Threading: immutable; read on the UI thread.</remarks>
internal sealed class BracketPairWithMinIndentationInfo : BracketPairInfo
{
    /// <summary>Creates a bracket pair info with its minimum indentation.</summary>
    /// <param name="range">The range of the whole pair.</param>
    /// <param name="openingBracketRange">The range of the opening bracket.</param>
    /// <param name="closingBracketRange">The range of the closing bracket, or <see langword="null"/> if the pair is unclosed.</param>
    /// <param name="nestingLevel">The 0-based nesting level.</param>
    /// <param name="nestingLevelOfEqualBracketType">The 0-based nesting level among the pairs of the same type.</param>
    /// <param name="bracketPairNode">The AST node of the pair.</param>
    /// <param name="minVisibleColumnIndentation">
    /// -1 if not requested, otherwise the size of the minimum indentation in the bracket pair in terms of visible columns.
    /// </param>
    public BracketPairWithMinIndentationInfo(
        Range range,
        Range openingBracketRange,
        Range? closingBracketRange,
        int nestingLevel,
        int nestingLevelOfEqualBracketType,
        PairAstNode bracketPairNode,
        int minVisibleColumnIndentation)
        : base(range, openingBracketRange, closingBracketRange, nestingLevel, nestingLevelOfEqualBracketType, bracketPairNode)
    {
        MinVisibleColumnIndentation = minVisibleColumnIndentation;
    }

    /// <summary>-1 if not requested, otherwise the minimum indentation in the pair, in visible columns.</summary>
    public int MinVisibleColumnIndentation { get; }
}
