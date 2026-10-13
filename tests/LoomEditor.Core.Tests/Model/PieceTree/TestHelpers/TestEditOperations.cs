// Ported from microsoft/vscode: src/vs/editor/test/common/model/linesTextBuffer/linesTextBuffer.test.ts (helpers editOp)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;

namespace LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

internal static class TestEditOperations
{
    /// <summary>Monaco: <c>editOp(...)</c>; <paramref name="text"/> lines are joined with <c>\n</c>, <c>null</c> means no text.</summary>
    public static ValidatedEditOperation EditOp(
        int startLineNumber,
        int startColumn,
        int endLineNumber,
        int endColumn,
        int rangeOffset,
        int rangeLength,
        string[]? text)
    {
        return new ValidatedEditOperation
        {
            SortIndex = 0,
            Identifier = null,
            Range = new Range(startLineNumber, startColumn, endLineNumber, endColumn),
            RangeOffset = rangeOffset,
            RangeLength = rangeLength,
            Text = text is null ? string.Empty : string.Join('\n', text),
            EolCount = text is null ? 0 : text.Length - 1,
            FirstLineLength = text is null ? 0 : text[0].Length,
            LastLineLength = text is null ? 0 : text[^1].Length,
            ForceMoveMarkers = false,
            IsAutoWhitespaceEdit = false,
        };
    }

    /// <summary>Compares every field of two edits (records and classes with lists do not compare by value).</summary>
    public static void AssertEditOperation(ValidatedEditOperation expected, ValidatedEditOperation actual)
    {
        Assert.Equal(expected.SortIndex, actual.SortIndex);
        Assert.Equal(expected.Identifier, actual.Identifier);
        Assert.Equal(expected.Range, actual.Range);
        Assert.Equal(expected.RangeOffset, actual.RangeOffset);
        Assert.Equal(expected.RangeLength, actual.RangeLength);
        Assert.Equal(expected.Text, actual.Text);
        Assert.Equal(expected.EolCount, actual.EolCount);
        Assert.Equal(expected.FirstLineLength, actual.FirstLineLength);
        Assert.Equal(expected.LastLineLength, actual.LastLineLength);
        Assert.Equal(expected.ForceMoveMarkers, actual.ForceMoveMarkers);
        Assert.Equal(expected.IsAutoWhitespaceEdit, actual.IsAutoWhitespaceEdit);
    }
}
