// Ported from microsoft/vscode: src/vs/editor/test/common/model/linesTextBuffer/linesTextBuffer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

// Monaco suite: 'PieceTreeTextBuffer._getInverseEdits'
public class PieceTreeTextBufferInverseEditsTests
{
    private static ValidatedEditOperation EditOp(int startLineNumber, int startColumn, int endLineNumber, int endColumn, string[]? text) =>
        TestEditOperations.EditOp(startLineNumber, startColumn, endLineNumber, endColumn, 0, 0, text);

    private static Range Inverse(int startLineNumber, int startColumn, int endLineNumber, int endColumn) =>
        new(startLineNumber, startColumn, endLineNumber, endColumn);

    private static void AssertInverseEdits(ValidatedEditOperation[] ops, Range[] expected)
    {
        Range[] actual = PieceTreeTextBuffer.GetInverseEditRanges(ops);
        Assert.Equal(expected, actual);
    }

    // Monaco: 'single insert'
    [Fact]
    public void GetInverseEditRanges_SingleInsert()
    {
        AssertInverseEdits([EditOp(1, 1, 1, 1, ["hello"])], [Inverse(1, 1, 1, 6)]);
    }

    // Monaco: 'Bug 19872: Undo is funky'
    [Fact]
    public void GetInverseEditRanges_Bug19872UndoIsFunky()
    {
        AssertInverseEdits(
            [EditOp(2, 1, 2, 2, [""]), EditOp(3, 1, 4, 2, [""])],
            [Inverse(2, 1, 2, 1), Inverse(3, 1, 3, 1)]);
    }

    // Monaco: 'two single unrelated inserts'
    [Fact]
    public void GetInverseEditRanges_TwoSingleUnrelatedInserts()
    {
        AssertInverseEdits(
            [EditOp(1, 1, 1, 1, ["hello"]), EditOp(2, 1, 2, 1, ["world"])],
            [Inverse(1, 1, 1, 6), Inverse(2, 1, 2, 6)]);
    }

    // Monaco: 'two single inserts 1'
    [Fact]
    public void GetInverseEditRanges_TwoSingleInserts1()
    {
        AssertInverseEdits(
            [EditOp(1, 1, 1, 1, ["hello"]), EditOp(1, 2, 1, 2, ["world"])],
            [Inverse(1, 1, 1, 6), Inverse(1, 7, 1, 12)]);
    }

    // Monaco: 'two single inserts 2'
    [Fact]
    public void GetInverseEditRanges_TwoSingleInserts2()
    {
        AssertInverseEdits(
            [EditOp(1, 1, 1, 1, ["hello"]), EditOp(1, 4, 1, 4, ["world"])],
            [Inverse(1, 1, 1, 6), Inverse(1, 9, 1, 14)]);
    }

    // Monaco: 'multiline insert'
    [Fact]
    public void GetInverseEditRanges_MultilineInsert()
    {
        AssertInverseEdits([EditOp(1, 1, 1, 1, ["hello", "world"])], [Inverse(1, 1, 2, 6)]);
    }

    // Monaco: 'two unrelated multiline inserts'
    [Fact]
    public void GetInverseEditRanges_TwoUnrelatedMultilineInserts()
    {
        AssertInverseEdits(
            [EditOp(1, 1, 1, 1, ["hello", "world"]), EditOp(2, 1, 2, 1, ["how", "are", "you?"])],
            [Inverse(1, 1, 2, 6), Inverse(3, 1, 5, 5)]);
    }

    // Monaco: 'two multiline inserts 1'
    [Fact]
    public void GetInverseEditRanges_TwoMultilineInserts1()
    {
        AssertInverseEdits(
            [EditOp(1, 1, 1, 1, ["hello", "world"]), EditOp(1, 2, 1, 2, ["how", "are", "you?"])],
            [Inverse(1, 1, 2, 6), Inverse(2, 7, 4, 5)]);
    }

    // Monaco: 'single delete'
    [Fact]
    public void GetInverseEditRanges_SingleDelete()
    {
        AssertInverseEdits([EditOp(1, 1, 1, 6, null)], [Inverse(1, 1, 1, 1)]);
    }

    // Monaco: 'two single unrelated deletes'
    [Fact]
    public void GetInverseEditRanges_TwoSingleUnrelatedDeletes()
    {
        AssertInverseEdits(
            [EditOp(1, 1, 1, 6, null), EditOp(2, 1, 2, 6, null)],
            [Inverse(1, 1, 1, 1), Inverse(2, 1, 2, 1)]);
    }

    // Monaco: 'two single deletes 1'
    [Fact]
    public void GetInverseEditRanges_TwoSingleDeletes1()
    {
        AssertInverseEdits(
            [EditOp(1, 1, 1, 6, null), EditOp(1, 7, 1, 12, null)],
            [Inverse(1, 1, 1, 1), Inverse(1, 2, 1, 2)]);
    }

    // Monaco: 'two single deletes 2'
    [Fact]
    public void GetInverseEditRanges_TwoSingleDeletes2()
    {
        AssertInverseEdits(
            [EditOp(1, 1, 1, 6, null), EditOp(1, 9, 1, 14, null)],
            [Inverse(1, 1, 1, 1), Inverse(1, 4, 1, 4)]);
    }

    // Monaco: 'multiline delete'
    [Fact]
    public void GetInverseEditRanges_MultilineDelete()
    {
        AssertInverseEdits([EditOp(1, 1, 2, 6, null)], [Inverse(1, 1, 1, 1)]);
    }

    // Monaco: 'two unrelated multiline deletes'
    [Fact]
    public void GetInverseEditRanges_TwoUnrelatedMultilineDeletes()
    {
        AssertInverseEdits(
            [EditOp(1, 1, 2, 6, null), EditOp(3, 1, 5, 5, null)],
            [Inverse(1, 1, 1, 1), Inverse(2, 1, 2, 1)]);
    }

    // Monaco: 'two multiline deletes 1'
    [Fact]
    public void GetInverseEditRanges_TwoMultilineDeletes1()
    {
        AssertInverseEdits(
            [EditOp(1, 1, 2, 6, null), EditOp(2, 7, 4, 5, null)],
            [Inverse(1, 1, 1, 1), Inverse(1, 2, 1, 2)]);
    }

    // Monaco: 'single replace'
    [Fact]
    public void GetInverseEditRanges_SingleReplace()
    {
        AssertInverseEdits([EditOp(1, 1, 1, 6, ["Hello world"])], [Inverse(1, 1, 1, 12)]);
    }

    // Monaco: 'two replaces'
    [Fact]
    public void GetInverseEditRanges_TwoReplaces()
    {
        AssertInverseEdits(
            [EditOp(1, 1, 1, 6, ["Hello world"]), EditOp(1, 7, 1, 8, ["How are you?"])],
            [Inverse(1, 1, 1, 12), Inverse(1, 13, 1, 25)]);
    }

    // Monaco: 'many edits'
    [Fact]
    public void GetInverseEditRanges_ManyEdits()
    {
        AssertInverseEdits(
            [EditOp(1, 2, 1, 2, ["", "  "]), EditOp(1, 5, 1, 6, [""]), EditOp(1, 9, 1, 9, ["", ""])],
            [Inverse(1, 2, 2, 3), Inverse(2, 6, 2, 6), Inverse(2, 9, 3, 1)]);
    }

    [Fact]
    public void GetInverseEditRange_MultiLineText_ReturnsRangeEndingOnLastLine()
    {
        Range actual = PieceTreeTextBuffer.GetInverseEditRange(new Range(3, 4, 3, 9), "ab\r\ncde");

        Assert.Equal(new Range(3, 4, 4, 4), actual);
    }

    [Fact]
    public void GetInverseEditRange_EmptyText_ReturnsCollapsedRange()
    {
        Range actual = PieceTreeTextBuffer.GetInverseEditRange(new Range(3, 4, 5, 9), string.Empty);

        Assert.Equal(new Range(3, 4, 3, 4), actual);
    }

    [Fact]
    public void SortOps_EqualRanges_UseSortIndex()
    {
        ValidatedEditOperation a = EditOp(1, 1, 1, 2, ["x"]);
        ValidatedEditOperation b = EditOp(1, 1, 1, 2, ["y"]);
        a.SortIndex = 0;
        b.SortIndex = 1;

        Assert.True(PieceTreeTextBuffer.SortOpsAscending(a, b) < 0);
        Assert.True(PieceTreeTextBuffer.SortOpsDescending(a, b) > 0);
    }

    [Fact]
    public void SortOps_DifferentRanges_UseRangeEnds()
    {
        ValidatedEditOperation a = EditOp(1, 1, 1, 2, ["x"]);
        ValidatedEditOperation b = EditOp(2, 1, 2, 2, ["y"]);

        Assert.True(PieceTreeTextBuffer.SortOpsAscending(a, b) < 0);
        Assert.True(PieceTreeTextBuffer.SortOpsDescending(a, b) > 0);
    }
}
