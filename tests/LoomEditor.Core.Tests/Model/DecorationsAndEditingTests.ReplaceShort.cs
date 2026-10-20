// Ported from microsoft/vscode: src/vs/editor/test/common/model/modelDecorations.test.ts (suite 'Decorations and editing' > ReplaceShort)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tests.Model;

public partial class DecorationsAndEditingTests
{
    [Fact]
    public void ReplaceShort_Collapsed_EditEndLtRangeStart()
    {
        // Monaco: 'edit.end < range.start'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 1, 1, 3),
            "c",
            [
                [new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3)],
                [new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3)],
            ]);
    }

    [Fact]
    public void ReplaceShort_Collapsed_EditEndLeRangeStart()
    {
        // Monaco: 'edit.end <= range.start'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 2, 1, 4),
            "c",
            [
                [new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3)],
                [new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3), new Range(1, 3, 1, 3)],
            ]);
    }

    [Fact]
    public void ReplaceShort_Collapsed_EditStartLtRangeStartAndEditEndGtRangeEnd()
    {
        // Monaco: 'edit.start < range.start && edit.end > range.end'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 3, 1, 5),
            "c",
            [
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
            ]);
    }

    [Fact]
    public void ReplaceShort_Collapsed_EditStartGeRangeEnd()
    {
        // Monaco: 'edit.start >= range.end'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 4, 1, 6),
            "c",
            [
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
                [new Range(1, 5, 1, 5), new Range(1, 5, 1, 5), new Range(1, 5, 1, 5), new Range(1, 5, 1, 5)],
            ]);
    }

    [Fact]
    public void ReplaceShort_Collapsed_EditStartGtRangeEnd()
    {
        // Monaco: 'edit.start > range.end'
        RunTest(
            new Range(1, 4, 1, 4),
            new Range(1, 5, 1, 7),
            "c",
            [
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditEndLtRangeStart()
    {
        // Monaco: 'edit.end < range.start'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 1, 1, 3),
            "c",
            [
                [new Range(1, 3, 1, 8), new Range(1, 3, 1, 8), new Range(1, 3, 1, 8), new Range(1, 3, 1, 8)],
                [new Range(1, 3, 1, 8), new Range(1, 3, 1, 8), new Range(1, 3, 1, 8), new Range(1, 3, 1, 8)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditEndLeRangeStart()
    {
        // Monaco: 'edit.end <= range.start'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 2, 1, 4),
            "c",
            [
                [new Range(1, 3, 1, 8), new Range(1, 3, 1, 8), new Range(1, 3, 1, 8), new Range(1, 3, 1, 8)],
                [new Range(1, 3, 1, 8), new Range(1, 3, 1, 8), new Range(1, 3, 1, 8), new Range(1, 3, 1, 8)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditStartLtRangeStartAndEditEndLtRangeEnd()
    {
        // Monaco: 'edit.start < range.start && edit.end < range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 3, 1, 5),
            "c",
            [
                [new Range(1, 4, 1, 8), new Range(1, 4, 1, 8), new Range(1, 4, 1, 8), new Range(1, 4, 1, 8)],
                [new Range(1, 4, 1, 8), new Range(1, 4, 1, 8), new Range(1, 4, 1, 8), new Range(1, 4, 1, 8)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditStartLtRangeStartAndEditEndEqRangeEnd()
    {
        // Monaco: 'edit.start < range.start && edit.end == range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 3, 1, 9),
            "c",
            [
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditStartLtRangeStartAndEditEndGtRangeEnd()
    {
        // Monaco: 'edit.start < range.start && edit.end > range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 3, 1, 10),
            "c",
            [
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
                [new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4), new Range(1, 4, 1, 4)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditStartEqRangeStartAndEditEndLtRangeEnd()
    {
        // Monaco: 'edit.start == range.start && edit.end < range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 4, 1, 6),
            "c",
            [
                [new Range(1, 4, 1, 8), new Range(1, 4, 1, 8), new Range(1, 4, 1, 8), new Range(1, 4, 1, 8)],
                [new Range(1, 5, 1, 8), new Range(1, 5, 1, 8), new Range(1, 5, 1, 8), new Range(1, 5, 1, 8)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditStartEqRangeStartAndEditEndEqRangeEnd()
    {
        // Monaco: 'edit.start == range.start && edit.end == range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 4, 1, 9),
            "c",
            [
                [new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5)],
                [new Range(1, 5, 1, 5), new Range(1, 5, 1, 5), new Range(1, 5, 1, 5), new Range(1, 5, 1, 5)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditStartEqRangeStartAndEditEndGtRangeEnd()
    {
        // Monaco: 'edit.start == range.start && edit.end > range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 4, 1, 10),
            "c",
            [
                [new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5), new Range(1, 4, 1, 5)],
                [new Range(1, 5, 1, 5), new Range(1, 5, 1, 5), new Range(1, 5, 1, 5), new Range(1, 5, 1, 5)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditStartGtRangeStartAndEditStartLtRangeEndAndEditEndLtRangeEnd()
    {
        // Monaco: 'edit.start > range.start && edit.start < range.end && edit.end < range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 5, 1, 7),
            "c",
            [
                [new Range(1, 4, 1, 8), new Range(1, 4, 1, 8), new Range(1, 4, 1, 8), new Range(1, 4, 1, 8)],
                [new Range(1, 4, 1, 8), new Range(1, 4, 1, 8), new Range(1, 4, 1, 8), new Range(1, 4, 1, 8)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditStartGtRangeStartAndEditStartLtRangeEndAndEditEndEqRangeEnd()
    {
        // Monaco: 'edit.start > range.start && edit.start < range.end && edit.end == range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 5, 1, 9),
            "c",
            [
                [new Range(1, 4, 1, 6), new Range(1, 4, 1, 6), new Range(1, 4, 1, 6), new Range(1, 4, 1, 6)],
                [new Range(1, 4, 1, 6), new Range(1, 4, 1, 6), new Range(1, 4, 1, 6), new Range(1, 4, 1, 6)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditStartGtRangeStartAndEditStartLtRangeEndAndEditEndGtRangeEnd()
    {
        // Monaco: 'edit.start > range.start && edit.start < range.end && edit.end > range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 5, 1, 10),
            "c",
            [
                [new Range(1, 4, 1, 6), new Range(1, 4, 1, 6), new Range(1, 4, 1, 6), new Range(1, 4, 1, 6)],
                [new Range(1, 4, 1, 6), new Range(1, 4, 1, 6), new Range(1, 4, 1, 6), new Range(1, 4, 1, 6)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditStartEqRangeEnd()
    {
        // Monaco: 'edit.start == range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 9, 1, 11),
            "c",
            [
                [new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9)],
                [new Range(1, 4, 1, 10), new Range(1, 4, 1, 10), new Range(1, 4, 1, 10), new Range(1, 4, 1, 10)],
            ]);
    }

    [Fact]
    public void ReplaceShort_NonCollapsed_EditStartGtRangeEnd()
    {
        // Monaco: 'edit.start > range.end'
        RunTest(
            new Range(1, 4, 1, 9),
            new Range(1, 10, 1, 11),
            "c",
            [
                [new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9)],
                [new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9), new Range(1, 4, 1, 9)],
            ]);
    }
}
