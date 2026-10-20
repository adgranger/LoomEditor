// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/ast.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>A node without children (Monaco: <c>ImmutableLeafAstNode</c>).</summary>
/// <remarks>Threading: immutable after construction; read on the UI thread.</remarks>
internal abstract class ImmutableLeafAstNode : AstNode
{
    private static readonly AstNode[] EmptyArray = [];

    protected ImmutableLeafAstNode(Length length)
        : base(length)
    {
    }

    public override int ListHeight => 0;

    public override int ChildrenLength => 0;

    public override IReadOnlyList<AstNode> Children => EmptyArray;

    public override AstNode? GetChild(int idx)
    {
        return null;
    }

    public override AstNode FlattenLists()
    {
        return this;
    }

    public override AstNode DeepClone()
    {
        return this;
    }
}

/// <summary>Plain text between brackets (Monaco: <c>TextAstNode</c>).</summary>
/// <remarks>Threading: immutable after construction; read on the UI thread.</remarks>
internal sealed class TextAstNode : ImmutableLeafAstNode
{
    public TextAstNode(Length length)
        : base(length)
    {
    }

    public override AstNodeKind Kind => AstNodeKind.Text;

    public override SmallImmutableSet<int> MissingOpeningBracketIds => SmallImmutableSet<int>.Empty;

    public override bool CanBeReused(SmallImmutableSet<int> openBracketIds)
    {
        return true;
    }

    public override int ComputeMinIndentation(Length offset, TextModel textModel)
    {
        var start = offset.ToObj();

        // Text ast nodes don't have partial indentation (ensured by the tokenizer).
        // Thus, if this text node does not start at column 0, the first line cannot have any indentation at all.
        var startLineNumber = (start.ColumnCount == 0 ? start.LineCount : start.LineCount + 1) + 1;
        var endLineNumber = Length.Add(offset, Length).GetLineCount() + 1;

        var result = int.MaxValue;
        for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
        {
            var firstNonWsColumn = textModel.GetLineFirstNonWhitespaceColumn(lineNumber);
            var lineContent = textModel.GetLineContent(lineNumber);
            if (firstNonWsColumn == 0)
            {
                continue;
            }

            var visibleColumn = CursorColumns.VisibleColumnFromColumn(lineContent, firstNonWsColumn, textModel.Options.TabSize);
            result = Math.Min(result, visibleColumn);
        }

        return result;
    }
}

/// <summary>An opening or closing bracket (Monaco: <c>BracketAstNode</c>).</summary>
/// <remarks>Threading: immutable after construction; read on the UI thread.</remarks>
internal sealed class BracketAstNode : ImmutableLeafAstNode
{
    private BracketAstNode(Length length, BracketKind bracketInfo, SmallImmutableSet<int> bracketIds)
        : base(length)
    {
        BracketInfo = bracketInfo;
        BracketIds = bracketIds;
    }

    public BracketKind BracketInfo { get; }

    /// <summary>
    /// For an opening bracket, the id of the opening bracket.
    /// For a closing bracket, the ids of all opening brackets it can close.
    /// </summary>
    public SmallImmutableSet<int> BracketIds { get; }

    public override AstNodeKind Kind => AstNodeKind.Bracket;

    public override SmallImmutableSet<int> MissingOpeningBracketIds => SmallImmutableSet<int>.Empty;

    public string Text => BracketInfo.BracketText;

    public string LanguageId => BracketInfo.LanguageId;

    public static BracketAstNode Create(Length length, BracketKind bracketInfo, SmallImmutableSet<int> bracketIds)
    {
        return new BracketAstNode(length, bracketInfo, bracketIds);
    }

    public override bool CanBeReused(SmallImmutableSet<int> openBracketIds)
    {
        // These nodes could be reused, but not in a general way. Their parent may be reused.
        return false;
    }

    public override int ComputeMinIndentation(Length offset, TextModel textModel)
    {
        return int.MaxValue;
    }
}

/// <summary>A closing bracket that closes nothing (Monaco: <c>InvalidBracketAstNode</c>).</summary>
/// <remarks>Threading: immutable after construction; read on the UI thread.</remarks>
internal sealed class InvalidBracketAstNode : ImmutableLeafAstNode
{
    public InvalidBracketAstNode(SmallImmutableSet<int> closingBrackets, Length length)
        : base(length)
    {
        MissingOpeningBracketIds = closingBrackets;
    }

    public override AstNodeKind Kind => AstNodeKind.UnexpectedClosingBracket;

    public override SmallImmutableSet<int> MissingOpeningBracketIds { get; }

    public override bool CanBeReused(SmallImmutableSet<int> openBracketIds)
    {
        return !openBracketIds.Intersects(MissingOpeningBracketIds);
    }

    public override int ComputeMinIndentation(Length offset, TextModel textModel)
    {
        return int.MaxValue;
    }
}
