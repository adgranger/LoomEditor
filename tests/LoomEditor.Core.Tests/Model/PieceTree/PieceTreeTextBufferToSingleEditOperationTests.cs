// Ported from microsoft/vscode: src/vs/editor/test/common/model/linesTextBuffer/linesTextBuffer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

// Monaco suite: 'PieceTreeTextBuffer._toSingleEditOperation'
public class PieceTreeTextBufferToSingleEditOperationTests
{
    private static ValidatedEditOperation EditOp(int startLineNumber, int startColumn, int endLineNumber, int endColumn, int rangeOffset, int rangeLength, string[]? text) =>
        TestEditOperations.EditOp(startLineNumber, startColumn, endLineNumber, endColumn, rangeOffset, rangeLength, text);

    private static void AssertToSingleEditOperation(string[] original, ValidatedEditOperation[] edits, ValidatedEditOperation expected)
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create([string.Join('\n', original)]);

        ValidatedEditOperation actual = buffer.ToSingleEditOperation(edits);

        TestEditOperations.AssertEditOperation(expected, actual);
    }

    // Monaco: 'one edit op is unchanged'
    [Fact]
    public void ToSingleEditOperation_OneEditOp_IsUnchanged()
    {
        AssertToSingleEditOperation(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 3, 1, 3, 2, 0, [" new line", "No longer"])],
            EditOp(1, 3, 1, 3, 2, 0, [" new line", "No longer"]));
    }

    // Monaco: 'two edits on one line'
    [Fact]
    public void ToSingleEditOperation_TwoEditsOnOneLine()
    {
        AssertToSingleEditOperation(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [
                EditOp(1, 1, 1, 3, 0, 2, ["Your"]),
                EditOp(1, 4, 1, 4, 3, 0, ["Interesting "]),
                EditOp(2, 3, 2, 6, 16, 3, null),
            ],
            EditOp(1, 1, 2, 6, 0, 19, ["Your Interesting First Line", "\t\t"]));
    }

    // Monaco: 'insert multiple newlines'
    [Fact]
    public void ToSingleEditOperation_InsertMultipleNewlines()
    {
        AssertToSingleEditOperation(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [
                EditOp(1, 3, 1, 3, 2, 0, ["", "", "", "", ""]),
                EditOp(3, 15, 3, 15, 45, 0, ["a", "b"]),
            ],
            EditOp(1, 3, 3, 15, 2, 43, ["", "", "", "", " First Line", "\t\tMy Second Line", "    Third Linea", "b"]));
    }

    // Monaco: 'delete empty text'
    [Fact]
    public void ToSingleEditOperation_DeleteEmptyText()
    {
        AssertToSingleEditOperation(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "1"],
            [EditOp(1, 1, 1, 1, 0, 0, [""])],
            EditOp(1, 1, 1, 1, 0, 0, [""]));
    }

    // Monaco: 'two unrelated edits'
    [Fact]
    public void ToSingleEditOperation_TwoUnrelatedEdits()
    {
        AssertToSingleEditOperation(
            ["My First Line", "\t\tMy Second Line", "    Third Line", "", "123"],
            [
                EditOp(2, 1, 2, 3, 14, 2, ["\t"]),
                EditOp(3, 1, 3, 5, 31, 4, [""]),
            ],
            EditOp(2, 1, 3, 5, 14, 21, ["\tMy Second Line", ""]));
    }

    // Monaco: 'many edits'
    [Fact]
    public void ToSingleEditOperation_ManyEdits()
    {
        AssertToSingleEditOperation(
            ["{\"x\" : 1}"],
            [
                EditOp(1, 2, 1, 2, 1, 0, ["\n  "]),
                EditOp(1, 5, 1, 6, 4, 1, [""]),
                EditOp(1, 9, 1, 9, 8, 0, ["\n"]),
            ],
            EditOp(1, 2, 1, 9, 1, 7, ["", "  \"x\": 1", ""]));
    }

    // Monaco: 'many edits reversed'
    [Fact]
    public void ToSingleEditOperation_ManyEditsReversed()
    {
        AssertToSingleEditOperation(
            ["{", "  \"x\": 1", "}"],
            [
                EditOp(1, 2, 2, 3, 1, 3, [""]),
                EditOp(2, 6, 2, 6, 7, 0, [" "]),
                EditOp(2, 9, 3, 1, 10, 1, [""]),
            ],
            EditOp(1, 2, 3, 1, 1, 10, ["\"x\" : 1"]));
    }

    // Monaco: 'replacing newlines 1'
    [Fact]
    public void ToSingleEditOperation_ReplacingNewlines1()
    {
        AssertToSingleEditOperation(
            ["{", "\"a\": true,", "", "\"b\": true", "}"],
            [
                EditOp(1, 2, 2, 1, 1, 1, ["", "\t"]),
                EditOp(2, 11, 4, 1, 12, 2, ["", "\t"]),
            ],
            EditOp(1, 2, 4, 1, 1, 13, ["", "\t\"a\": true,", "\t"]));
    }

    // Monaco: 'replacing newlines 2'
    [Fact]
    public void ToSingleEditOperation_ReplacingNewlines2()
    {
        AssertToSingleEditOperation(
            ["some text", "some more text", "now comes an empty line", "", "after empty line", "and the last line"],
            [
                EditOp(1, 5, 3, 1, 4, 21, [" text", "some more text", "some more text"]),
                EditOp(3, 2, 4, 1, 26, 23, ["o more lines", "asd", "asd", "asd"]),
                EditOp(5, 1, 5, 6, 50, 5, ["zzzzzzzz"]),
                EditOp(5, 11, 6, 16, 60, 22, ["1", "2", "3", "4"]),
            ],
            EditOp(
                1, 5, 6, 16, 4, 78,
                [" text", "some more text", "some more textno more lines", "asd", "asd", "asd", "zzzzzzzz empt1", "2", "3", "4"]));
    }

    // Monaco: 'advanced'
    [Fact]
    public void ToSingleEditOperation_Advanced()
    {
        AssertToSingleEditOperation(
            [
                " {       \"d\": [",
                "             null",
                "        ] /*comment*/",
                "        ,\"e\": /*comment*/ [null] }",
            ],
            [
                EditOp(1, 1, 1, 2, 0, 1, [""]),
                EditOp(1, 3, 1, 10, 2, 7, ["", "  "]),
                EditOp(1, 16, 2, 14, 15, 14, ["", "    "]),
                EditOp(2, 18, 3, 9, 33, 9, ["", "  "]),
                EditOp(3, 22, 4, 9, 55, 9, [""]),
                EditOp(4, 10, 4, 10, 65, 0, ["", "  "]),
                EditOp(4, 28, 4, 28, 83, 0, ["", "    "]),
                EditOp(4, 32, 4, 32, 87, 0, ["", "  "]),
                EditOp(4, 33, 4, 34, 88, 1, ["", ""]),
            ],
            EditOp(
                1, 1, 4, 34, 0, 89,
                ["{", "  \"d\": [", "    null", "  ] /*comment*/,", "  \"e\": /*comment*/ [", "    null", "  ]", ""]));
    }

    // Monaco: 'advanced simplified'
    [Fact]
    public void ToSingleEditOperation_AdvancedSimplified()
    {
        AssertToSingleEditOperation(
            ["   abc", " ,def"],
            [
                EditOp(1, 1, 1, 4, 0, 3, [""]),
                EditOp(1, 7, 2, 2, 6, 2, [""]),
                EditOp(2, 3, 2, 3, 9, 0, ["", ""]),
            ],
            EditOp(1, 1, 2, 3, 0, 9, ["abc,", ""]));
    }

    [Fact]
    public void ToSingleEditOperation_AnyForceMoveMarkers_IsPropagated()
    {
        ValidatedEditOperation first = EditOp(1, 1, 1, 1, 0, 0, ["a"]);
        ValidatedEditOperation second = new()
        {
            Range = new Range(1, 3, 1, 3),
            Text = "b",
            FirstLineLength = 1,
            LastLineLength = 1,
            ForceMoveMarkers = true,
        };
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["hello"]);

        ValidatedEditOperation actual = buffer.ToSingleEditOperation([first, second]);

        Assert.True(actual.ForceMoveMarkers);
        Assert.Equal("aheb", actual.Text);
    }
}
