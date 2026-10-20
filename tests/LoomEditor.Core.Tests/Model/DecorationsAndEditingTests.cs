// Ported from microsoft/vscode: src/vs/editor/test/common/model/modelDecorations.test.ts (suite 'Decorations and editing')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Tests.Model.TestHelpers;

namespace LoomEditor.Core.Tests.Model;

// The 62 tests (insert 8, delete 18, replace short 18, replace long 18) are split by suite in three files.
public partial class DecorationsAndEditingTests
{
    private static void RunTestCore(Range decRange, TrackedRangeStickiness stickiness, Range editRange, string editText, bool editForceMoveMarkers, Range expectedDecRange, string msg)
    {
        TextModel model = TestTextModels.Create("My First Line\nMy Second Line\nThird Line");

        string id = model.DeltaDecorations(
            0,
            [],
            [new ModelDeltaDecoration(decRange, ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData { Description = "test", Stickiness = stickiness }))])[0];
        model.ApplyIdentifiedEdits([new IdentifiedEditOperation(editRange, editText, ForceMoveMarkers: editForceMoveMarkers)]);
        Range? actual = model.GetDecorationRange(id);

        Assert.True(actual == expectedDecRange, $"{msg}: expected {expectedDecRange}, got {actual}");
    }

    // Monaco: runTest (expectedDecRange[0] without forceMoveMarkers, [1] with it; one entry per stickiness)
    private static void RunTest(Range decRange, Range editRange, string editText, Range[][] expectedDecRange)
    {
        string[] names = ["AlwaysGrowsWhenTypingAtEdges", "NeverGrowsWhenTypingAtEdges", "GrowsOnlyWhenTypingBefore", "GrowsOnlyWhenTypingAfter"];
        for (int stickiness = 0; stickiness < 4; stickiness++)
        {
            RunTestCore(decRange, (TrackedRangeStickiness)stickiness, editRange, editText, false, expectedDecRange[0][stickiness], $"no-{stickiness}-{names[stickiness]}");
        }

        for (int stickiness = 0; stickiness < 4; stickiness++)
        {
            RunTestCore(decRange, (TrackedRangeStickiness)stickiness, editRange, editText, true, expectedDecRange[1][stickiness], $"force-{stickiness}-{names[stickiness]}");
        }
    }

    [Fact]
    public void Insert_Collapsed_Before()
    {
        // Monaco: 'before'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 3, 1, 3),
            "xx",
            [
                [new Range(1, 6, 1, 6), new Range(1, 6, 1, 6), new Range(1, 6, 1, 6), new Range(1, 6, 1, 6)],
                [new Range(1, 6, 1, 6), new Range(1, 6, 1, 6), new Range(1, 6, 1, 6), new Range(1, 6, 1, 6)],
            ]);
    }

    [Fact]
    public void Insert_Collapsed_Equal()
    {
        // Monaco: 'equal'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 4, 1, 4),
            "xx",
            [
                [new Range(1, 4, 1, 6), new Range(1, 6, 1, 6), new Range(1, 4, 1, 4), new Range(1, 6, 1, 6)],
                [new Range(1, 6, 1, 6), new Range(1, 6, 1, 6), new Range(1, 6, 1, 6), new Range(1, 6, 1, 6)],
            ]);
    }

    [Fact]
    public void Insert_Collapsed_After()
    {
        // Monaco: 'after'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 5, 1, 5),
            "xx",
            [
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
            ]);
    }

    [Fact]
    public void Insert_NonCollapsed_Before()
    {
        // Monaco: 'before'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 3, 1, 3),
            "xx",
            [
                [new Range(1, 6, 1, 11), new Range(1, 6, 1, 11), new Range(1, 6, 1, 11), new Range(1, 6, 1, 11)],
                [new Range(1, 6, 1, 11), new Range(1, 6, 1, 11), new Range(1, 6, 1, 11), new Range(1, 6, 1, 11)],
            ]);
    }

    [Fact]
    public void Insert_NonCollapsed_Start()
    {
        // Monaco: 'start'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 4, 1, 4),
            "xx",
            [
                [new Range(1, 4, 1, 11), new Range(1, 6, 1, 11), new Range(1, 4, 1, 11), new Range(1, 6, 1, 11)],
                [new Range(1, 6, 1, 11), new Range(1, 6, 1, 11), new Range(1, 6, 1, 11), new Range(1, 6, 1, 11)],
            ]);
    }

    [Fact]
    public void Insert_NonCollapsed_Inside()
    {
        // Monaco: 'inside'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 5, 1, 5),
            "xx",
            [
                [new Range(1, 4, 1, 11), new Range(1, 4, 1, 11), new Range(1, 4, 1, 11), new Range(1, 4, 1, 11)],
                [new Range(1, 4, 1, 11), new Range(1, 4, 1, 11), new Range(1, 4, 1, 11), new Range(1, 4, 1, 11)],
            ]);
    }

    [Fact]
    public void Insert_NonCollapsed_End()
    {
        // Monaco: 'end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 9, 1, 9),
            "xx",
            [
                [new Range(1, 4, 1, 11), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 11)],
                [new Range(1, 4, 1, 11), new Range(1, 4, 1, 11), new Range(1, 4, 1, 11), new Range(1, 4, 1, 11)],
            ]);
    }

    [Fact]
    public void Insert_NonCollapsed_After()
    {
        // Monaco: 'after'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 10, 1, 10),
            "xx",
            [
                [new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9)],
                [new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9)],
            ]);
    }

    [Fact]
    public void Delete_Collapsed_EditEndLtRangeStart()
    {
        // Monaco: 'edit.end < range.start'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 1, 1, 3),
            "",
            [
                [new Range(1, 2, 1, 2), new Range(1, 2, 1, 2), new Range(1, 2, 1, 2), new Range(1, 2, 1, 2)],
                [new Range(1, 2, 1, 2), new Range(1, 2, 1, 2), new Range(1, 2, 1, 2), new Range(1, 2, 1, 2)],
            ]);
    }

    [Fact]
    public void Delete_Collapsed_EditEndLeRangeStart()
    {
        // Monaco: 'edit.end <= range.start'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 2, 1, 4),
            "",
            [
                [new Range(1, 2, 1, 2), new Range(1, 2, 1, 2), new Range(1, 2, 1, 2), new Range(1, 2, 1, 2)],
                [new Range(1, 2, 1, 2), new Range(1, 2, 1, 2), new Range(1, 2, 1, 2), new Range(1, 2, 1, 2)],
            ]);
    }

    [Fact]
    public void Delete_Collapsed_EditStartLtRangeStartAndEditEndGtRangeEnd()
    {
        // Monaco: 'edit.start < range.start && edit.end > range.end'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 3, 1, 5),
            "",
            [
                [new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3)],
                [new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3)],
            ]);
    }

    [Fact]
    public void Delete_Collapsed_EditStartGeRangeEnd()
    {
        // Monaco: 'edit.start >= range.end'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 4, 1, 6),
            "",
            [
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
            ]);
    }

    [Fact]
    public void Delete_Collapsed_EditStartGtRangeEnd()
    {
        // Monaco: 'edit.start > range.end'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 5, 1, 7),
            "",
            [
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditEndLtRangeStart()
    {
        // Monaco: 'edit.end < range.start'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 1, 1, 3),
            "",
            [
                [new Range(1, 2, 1, 7), new Range(1, 2, 1, 7), new Range(1, 2, 1, 7), new Range(1, 2, 1, 7)],
                [new Range(1, 2, 1, 7), new Range(1, 2, 1, 7), new Range(1, 2, 1, 7), new Range(1, 2, 1, 7)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditEndLeRangeStart()
    {
        // Monaco: 'edit.end <= range.start'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 2, 1, 4),
            "",
            [
                [new Range(1, 2, 1, 7), new Range(1, 2, 1, 7), new Range(1, 2, 1, 7), new Range(1, 2, 1, 7)],
                [new Range(1, 2, 1, 7), new Range(1, 2, 1, 7), new Range(1, 2, 1, 7), new Range(1, 2, 1, 7)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditStartLtRangeStartAndEditEndLtRangeEnd()
    {
        // Monaco: 'edit.start < range.start && edit.end < range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 3, 1, 5),
            "",
            [
                [new Range(1, 3, 1, 7), new Range(1, 3, 1, 7), new Range(1, 3, 1, 7), new Range(1, 3, 1, 7)],
                [new Range(1, 3, 1, 7), new Range(1, 3, 1, 7), new Range(1, 3, 1, 7), new Range(1, 3, 1, 7)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditStartLtRangeStartAndEditEndEqRangeEnd()
    {
        // Monaco: 'edit.start < range.start && edit.end == range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 3, 1, 9),
            "",
            [
                [new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3)],
                [new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditStartLtRangeStartAndEditEndGtRangeEnd()
    {
        // Monaco: 'edit.start < range.start && edit.end > range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 3, 1, 10),
            "",
            [
                [new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3)],
                [new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditStartEqRangeStartAndEditEndLtRangeEnd()
    {
        // Monaco: 'edit.start == range.start && edit.end < range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 4, 1, 6),
            "",
            [
                [new Range(1, 4, 1, 7), new Range(1, 4, 1, 7), new Range(1, 4, 1, 7), new Range(1, 4, 1, 7)],
                [new Range(1, 4, 1, 7), new Range(1, 4, 1, 7), new Range(1, 4, 1, 7), new Range(1, 4, 1, 7)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditStartEqRangeStartAndEditEndEqRangeEnd()
    {
        // Monaco: 'edit.start == range.start && edit.end == range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 4, 1, 9),
            "",
            [
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditStartEqRangeStartAndEditEndGtRangeEnd()
    {
        // Monaco: 'edit.start == range.start && edit.end > range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 4, 1, 10),
            "",
            [
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditStartGtRangeStartAndEditStartLtRangeEndAndEditEndLtRangeEnd()
    {
        // Monaco: 'edit.start > range.start && edit.start < range.end && edit.end < range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 5, 1, 7),
            "",
            [
                [new Range(1, 4, 1, 7), new Range(1, 4, 1, 7), new Range(1, 4, 1, 7), new Range(1, 4, 1, 7)],
                [new Range(1, 4, 1, 7), new Range(1, 4, 1, 7), new Range(1, 4, 1, 7), new Range(1, 4, 1, 7)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditStartGtRangeStartAndEditStartLtRangeEndAndEditEndEqRangeEnd()
    {
        // Monaco: 'edit.start > range.start && edit.start < range.end && edit.end == range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 5, 1, 9),
            "",
            [
                [new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5)],
                [new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditStartGtRangeStartAndEditStartLtRangeEndAndEditEndGtRangeEnd()
    {
        // Monaco: 'edit.start > range.start && edit.start < range.end && edit.end > range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 5, 1, 10),
            "",
            [
                [new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5)],
                [new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditStartEqRangeEnd()
    {
        // Monaco: 'edit.start == range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 9, 1, 11),
            "",
            [
                [new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9)],
                [new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9)],
            ]);
    }

    [Fact]
    public void Delete_NonCollapsed_EditStartGtRangeEnd()
    {
        // Monaco: 'edit.start > range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 10, 1, 11),
            "",
            [
                [new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9)],
                [new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9)],
            ]);
    }
}
