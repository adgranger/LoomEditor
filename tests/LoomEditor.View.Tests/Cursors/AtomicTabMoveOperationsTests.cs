// Ported from microsoft/vscode: src/vs/editor/test/common/controller/cursorAtomicMoveOperations.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Cursors;

namespace LoomEditor.View.Tests.Cursors;

// The suite is named 'Cursor move command test' in Monaco.
public class AtomicTabMoveOperationsTests
{
    private sealed record WhitespaceCase(
        string LineContent,
        int TabSize,
        int[] ExpectedPrevTabStopPosition,
        int[] ExpectedPrevTabStopVisibleColumn,
        int[] ExpectedVisibleColumn);

    // Monaco: 'Test whitespaceVisibleColumn'
    [Fact]
    public void WhitespaceVisibleColumn_TestCases_MatchMonaco()
    {
        WhitespaceCase[] testCases =
        [
            new("        ", 4, [-1, 0, 0, 0, 0, 4, 4, 4, 4, -1], [-1, 0, 0, 0, 0, 4, 4, 4, 4, -1], [0, 1, 2, 3, 4, 5, 6, 7, 8, -1]),
            new("  ", 4, [-1, 0, 0, -1], [-1, 0, 0, -1], [0, 1, 2, -1]),
            new("\t", 4, [-1, 0, -1], [-1, 0, -1], [0, 4, -1]),
            new("\t ", 4, [-1, 0, 1, -1], [-1, 0, 4, -1], [0, 4, 5, -1]),
            new(" \t\t ", 4, [-1, 0, 0, 2, 3, -1], [-1, 0, 0, 4, 8, -1], [0, 1, 4, 8, 9, -1]),
            new(" \tA", 4, [-1, 0, 0, -1, -1], [-1, 0, 0, -1, -1], [0, 1, 4, -1, -1]),
            new("A", 4, [-1, -1, -1], [-1, -1, -1], [0, -1, -1]),
            new(string.Empty, 4, [-1, -1], [-1, -1], [0, -1]),
        ];

        foreach (WhitespaceCase testCase in testCases)
        {
            int maxPosition = testCase.ExpectedVisibleColumn.Length;
            for (int position = 0; position < maxPosition; position++)
            {
                (int, int, int) actual = AtomicTabMoveOperations.WhitespaceVisibleColumn(testCase.LineContent, position, testCase.TabSize);
                (int, int, int) expected = (
                    testCase.ExpectedPrevTabStopPosition[position],
                    testCase.ExpectedPrevTabStopVisibleColumn[position],
                    testCase.ExpectedVisibleColumn[position]);
                Assert.True(actual == expected, $"'{testCase.LineContent}' tabSize {testCase.TabSize} position {position}: expected {expected}, got {actual}");
            }
        }
    }

    private sealed record AtomicCase(string LineContent, int TabSize, int[] ExpectedLeft, int[] ExpectedRight, int[] ExpectedNearest);

    // Monaco: 'Test atomicPosition'
    [Fact]
    public void AtomicPosition_TestCases_MatchMonaco()
    {
        AtomicCase[] testCases =
        [
            new("        ", 4, [-1, 0, 0, 0, 0, 4, 4, 4, 4, -1], [4, 4, 4, 4, 8, 8, 8, 8, -1, -1], [0, 0, 0, 4, 4, 4, 4, 8, 8, -1]),
            new(" \t", 4, [-1, 0, 0, -1], [2, 2, -1, -1], [0, 0, 2, -1]),
            new("\t ", 4, [-1, 0, -1, -1], [1, -1, -1, -1], [0, 1, -1, -1]),
            new(" \t ", 4, [-1, 0, 0, -1, -1], [2, 2, -1, -1, -1], [0, 0, 2, -1, -1]),
            new("        A", 4, [-1, 0, 0, 0, 0, 4, 4, 4, 4, -1, -1], [4, 4, 4, 4, 8, 8, 8, 8, -1, -1, -1], [0, 0, 0, 4, 4, 4, 4, 8, 8, -1, -1]),
            new("      foo", 4, [-1, 0, 0, 0, 0, -1, -1, -1, -1, -1, -1], [4, 4, 4, 4, -1, -1, -1, -1, -1, -1, -1], [0, 0, 0, 4, 4, -1, -1, -1, -1, -1, -1]),
        ];

        foreach (AtomicCase testCase in testCases)
        {
            foreach ((AtomicMoveDirection direction, int[] expected) in new[]
            {
                (AtomicMoveDirection.Left, testCase.ExpectedLeft),
                (AtomicMoveDirection.Right, testCase.ExpectedRight),
                (AtomicMoveDirection.Nearest, testCase.ExpectedNearest),
            })
            {
                int[] actual = Enumerable.Range(0, expected.Length)
                    .Select(i => AtomicTabMoveOperations.AtomicPosition(testCase.LineContent, i, testCase.TabSize, direction))
                    .ToArray();
                Assert.True(
                    expected.SequenceEqual(actual),
                    $"'{testCase.LineContent}' tabSize {testCase.TabSize} {direction}: expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}]");
            }
        }
    }
}
