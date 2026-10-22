// Ported from microsoft/vscode: src/vs/editor/contrib/folding/test/browser/foldingModel.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Tests.Features.Folding.TestHelpers;
using static LoomEditor.View.Features.Folding.FoldingModelFunctions;

namespace LoomEditor.View.Tests.Features.Folding;

public class FoldingModelFunctionsTests
{
    private readonly record struct ExpectedRegion(int StartLineNumber, int EndLineNumber, bool IsCollapsed);

    private static readonly FoldingMarkers RegionMarkers = new(new Regex(@"^//#region$"), new Regex(@"^//#endregion$"));

    private static readonly FoldingMarkers IndentedRegionMarkers = new(new Regex(@"^\s*//#region$"), new Regex(@"^\s*//#endregion$"));

    private static ExpectedRegion R(int startLineNumber, int endLineNumber, bool isCollapsed = false) =>
        new(startLineNumber, endLineNumber, isCollapsed);

    private static FoldingModel Create(string[] lines, FoldingMarkers? markers = null)
    {
        var textModel = new TextModel(string.Join('\n', lines));
        var foldingModel = new FoldingModel(textModel, new TestDecorationProvider(textModel));
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false, markers));
        return foldingModel;
    }

    private static void AssertRanges(FoldingModel foldingModel, params ExpectedRegion[] expected)
    {
        FoldingRegions actual = foldingModel.Regions;
        var actualRanges = new List<ExpectedRegion>();
        for (int i = 0; i < actual.Length; i++)
        {
            actualRanges.Add(R(actual.GetStartLineNumber(i), actual.GetEndLineNumber(i), actual.IsCollapsed(i)));
        }

        Assert.Equal(expected, actualRanges);
    }

    private static void AssertFoldedRanges(FoldingModel foldingModel, params ExpectedRegion[] expected)
    {
        FoldingRegions actual = foldingModel.Regions;
        var actualRanges = new List<ExpectedRegion>();
        for (int i = 0; i < actual.Length; i++)
        {
            if (actual.IsCollapsed(i))
            {
                actualRanges.Add(R(actual.GetStartLineNumber(i), actual.GetEndLineNumber(i)));
            }
        }

        Assert.Equal(expected, actualRanges);
    }

    private static readonly string[] NestedLines =
    [
        "//#region",
        "//#endregion",
        "class A {",
        "  void foo() {",
        "    if (true) {",
        "        return;",
        "    }",
        "",
        "    if (true) {",
        "      return;",
        "    }",
        "  }",
        "}",
    ];

    private static readonly ExpectedRegion R1 = R(1, 2);
    private static readonly ExpectedRegion R2 = R(3, 12);
    private static readonly ExpectedRegion R3 = R(4, 11);
    private static readonly ExpectedRegion R4 = R(5, 6);
    private static readonly ExpectedRegion R5 = R(9, 10);

    // Monaco: 'Folding Model' › 'setCollapseStateRecursivly'
    [Fact]
    public void SetCollapseStateRecursivly()
    {
        FoldingModel foldingModel = Create(NestedLines, RegionMarkers);
        AssertRanges(foldingModel, R1, R2, R3, R4, R5);

        SetCollapseStateLevelsDown(foldingModel, true, int.MaxValue, [4]);
        AssertFoldedRanges(foldingModel, R3, R4, R5);

        SetCollapseStateLevelsDown(foldingModel, false, int.MaxValue, [8]);
        AssertFoldedRanges(foldingModel);

        SetCollapseStateLevelsDown(foldingModel, true, int.MaxValue, [12]);
        AssertFoldedRanges(foldingModel, R2, R3, R4, R5);

        SetCollapseStateLevelsDown(foldingModel, false, int.MaxValue, [7]);
        AssertFoldedRanges(foldingModel, R2);

        SetCollapseStateLevelsDown(foldingModel, false);
        AssertFoldedRanges(foldingModel);

        SetCollapseStateLevelsDown(foldingModel, true);
        AssertFoldedRanges(foldingModel, R1, R2, R3, R4, R5);
    }

    // Monaco: 'Folding Model' › 'setCollapseStateAtLevel'
    [Fact]
    public void SetCollapseStateAtLevel_Levels()
    {
        FoldingModel foldingModel = Create(
        [
            "//#region",
            "//#endregion",
            "class A {",
            "  void foo() {",
            "    if (true) {",
            "        return;",
            "    }",
            "",
            "    if (true) {",
            "      return;",
            "    }",
            "  }",
            "  //#region",
            "  const bar = 9;",
            "  //#endregion",
            "}",
        ], IndentedRegionMarkers);

        ExpectedRegion r2 = R(3, 15), r6 = R(13, 15);
        AssertRanges(foldingModel, R1, r2, R3, R4, R5, r6);

        SetCollapseStateAtLevel(foldingModel, 1, true, []);
        AssertFoldedRanges(foldingModel, R1, r2);

        SetCollapseStateAtLevel(foldingModel, 1, false, [5]);
        AssertFoldedRanges(foldingModel, r2);

        SetCollapseStateAtLevel(foldingModel, 1, false, [1]);
        AssertFoldedRanges(foldingModel);

        SetCollapseStateAtLevel(foldingModel, 2, true, []);
        AssertFoldedRanges(foldingModel, R3, r6);

        SetCollapseStateAtLevel(foldingModel, 2, false, [5, 6]);
        AssertFoldedRanges(foldingModel, R3);

        SetCollapseStateAtLevel(foldingModel, 3, true, [4, 9]);
        AssertFoldedRanges(foldingModel, R3, R4);

        SetCollapseStateAtLevel(foldingModel, 3, false, [4, 9]);
        AssertFoldedRanges(foldingModel, R3);
    }

    // Monaco: 'Folding Model' › 'setCollapseStateLevelsDown'
    [Fact]
    public void SetCollapseStateLevelsDown_Levels()
    {
        FoldingModel foldingModel = Create(NestedLines, RegionMarkers);
        AssertRanges(foldingModel, R1, R2, R3, R4, R5);

        SetCollapseStateLevelsDown(foldingModel, true, 1, [4]);
        AssertFoldedRanges(foldingModel, R3);

        SetCollapseStateLevelsDown(foldingModel, true, 2, [4]);
        AssertFoldedRanges(foldingModel, R3, R4, R5);

        SetCollapseStateLevelsDown(foldingModel, false, 2, [3]);
        AssertFoldedRanges(foldingModel, R4, R5);

        SetCollapseStateLevelsDown(foldingModel, false, 2, [2]);
        AssertFoldedRanges(foldingModel, R4, R5);

        SetCollapseStateLevelsDown(foldingModel, true, 4, [2]);
        AssertFoldedRanges(foldingModel, R1, R4, R5);

        SetCollapseStateLevelsDown(foldingModel, false, 4, [2, 3]);
        AssertFoldedRanges(foldingModel);
    }

    // Monaco: 'Folding Model' › 'setCollapseStateLevelsUp'
    [Fact]
    public void SetCollapseStateLevelsUp_Levels()
    {
        FoldingModel foldingModel = Create(NestedLines, RegionMarkers);
        AssertRanges(foldingModel, R1, R2, R3, R4, R5);

        SetCollapseStateLevelsUp(foldingModel, true, 1, [4]);
        AssertFoldedRanges(foldingModel, R3);

        SetCollapseStateLevelsUp(foldingModel, true, 2, [4]);
        AssertFoldedRanges(foldingModel, R2, R3);

        SetCollapseStateLevelsUp(foldingModel, false, 4, [1, 3, 4]);
        AssertFoldedRanges(foldingModel);

        SetCollapseStateLevelsUp(foldingModel, true, 2, [10]);
        AssertFoldedRanges(foldingModel, R3, R5);
    }

    // Monaco: 'Folding Model' › 'setCollapseStateUp'
    [Fact]
    public void SetCollapseStateUp_Parents()
    {
        FoldingModel foldingModel = Create(NestedLines, RegionMarkers);
        AssertRanges(foldingModel, R1, R2, R3, R4, R5);

        SetCollapseStateUp(foldingModel, true, [5]);
        AssertFoldedRanges(foldingModel, R4);

        SetCollapseStateUp(foldingModel, true, [5]);
        AssertFoldedRanges(foldingModel, R3, R4);

        SetCollapseStateUp(foldingModel, true, [4]);
        AssertFoldedRanges(foldingModel, R2, R3, R4);
    }

    // Monaco: 'Folding Model' › 'setCollapseStateForMatchingLines'
    [Fact]
    public void SetCollapseStateForMatchingLines_Comments()
    {
        FoldingModel foldingModel = Create(
        [
            "/**",
            " * the class",
            " */",
            "class A {",
            "  /**",
            "   * the foo",
            "   */",
            "  void foo() {",
            "    /*",
            "     * the comment",
            "     */",
            "  }",
            "}",
        ], RegionMarkers);

        ExpectedRegion r1 = R(1, 3), r2 = R(4, 12), r3 = R(5, 7), r4 = R(8, 11), r5 = R(9, 11);
        AssertRanges(foldingModel, r1, r2, r3, r4, r5);

        var regExp = new Regex(@"^\s*" + Strings.EscapeRegExpCharacters("/*"));
        SetCollapseStateForMatchingLines(foldingModel, regExp, true);
        AssertFoldedRanges(foldingModel, r1, r3, r5);
    }

    // Monaco: 'Folding Model' › 'setCollapseStateForRest'
    [Fact]
    public void SetCollapseStateForRest_BlockedLines()
    {
        FoldingModel foldingModel = Create(NestedLines, RegionMarkers);
        AssertRanges(foldingModel, R1, R2, R3, R4, R5);

        SetCollapseStateForRest(foldingModel, true, [5]);
        AssertFoldedRanges(foldingModel, R1, R5);

        SetCollapseStateForRest(foldingModel, false, [5]);
        AssertFoldedRanges(foldingModel);

        SetCollapseStateForRest(foldingModel, true, [1]);
        AssertFoldedRanges(foldingModel, R2, R3, R4, R5);

        SetCollapseStateForRest(foldingModel, true, [3]);
        AssertFoldedRanges(foldingModel, R1, R2, R3, R4, R5);
    }

    // Monaco: 'Folding Model' › 'fold jumping'
    [Fact]
    public void FoldJumping()
    {
        FoldingModel foldingModel = Create(
        [
            "class A {",
            "  void foo() {",
            "    if (1) {",
            "      a();",
            "    } else if (2) {",
            "      if (true) {",
            "        b();",
            "      }",
            "    } else {",
            "      c();",
            "    }",
            "  }",
            "}",
        ]);
        AssertRanges(foldingModel, R(1, 12), R(2, 11), R(3, 4), R(5, 8), R(6, 7), R(9, 10));

        Assert.Equal(6, GetParentFoldLine(7, foldingModel));
        Assert.Equal(5, GetParentFoldLine(6, foldingModel));
        Assert.Equal(2, GetParentFoldLine(5, foldingModel));
        Assert.Equal(1, GetParentFoldLine(2, foldingModel));
        Assert.Null(GetParentFoldLine(1, foldingModel));

        Assert.Equal(9, GetPreviousFoldLine(10, foldingModel));
        Assert.Equal(5, GetPreviousFoldLine(9, foldingModel));
        Assert.Equal(3, GetPreviousFoldLine(5, foldingModel));
        Assert.Null(GetPreviousFoldLine(3, foldingModel));
        Assert.Equal(3, GetPreviousFoldLine(4, foldingModel));
        Assert.Equal(6, GetPreviousFoldLine(7, foldingModel));
        Assert.Equal(6, GetPreviousFoldLine(8, foldingModel));

        Assert.Equal(5, GetNextFoldLine(3, foldingModel));
        Assert.Equal(9, GetNextFoldLine(5, foldingModel));
        Assert.Null(GetNextFoldLine(9, foldingModel));
        Assert.Equal(5, GetNextFoldLine(4, foldingModel));
        Assert.Equal(9, GetNextFoldLine(7, foldingModel));
        Assert.Equal(9, GetNextFoldLine(8, foldingModel));
    }

    // Monaco: 'Folding Model' › 'fold jumping issue #129503'
    [Fact]
    public void FoldJumping_Issue129503()
    {
        FoldingModel foldingModel = Create(["", "if True:", "  print(1)", "if True:", "  print(1)", ""]);
        AssertRanges(foldingModel, R(2, 3), R(4, 6));

        Assert.Equal(2, GetNextFoldLine(1, foldingModel));
        Assert.Equal(4, GetNextFoldLine(2, foldingModel));
        Assert.Equal(4, GetNextFoldLine(3, foldingModel));
        Assert.Null(GetNextFoldLine(4, foldingModel));
        Assert.Null(GetNextFoldLine(5, foldingModel));
        Assert.Null(GetNextFoldLine(6, foldingModel));

        Assert.Null(GetPreviousFoldLine(1, foldingModel));
        Assert.Null(GetPreviousFoldLine(2, foldingModel));
        Assert.Equal(2, GetPreviousFoldLine(3, foldingModel));
        Assert.Equal(2, GetPreviousFoldLine(4, foldingModel));
        Assert.Equal(4, GetPreviousFoldLine(5, foldingModel));
        Assert.Equal(4, GetPreviousFoldLine(6, foldingModel));
    }

    [Fact]
    public void SetCollapseStateForType_CollapsesOnlyThatType()
    {
        var textModel = new TextModel("a\nb\nc\nd\ne\nf");
        var foldingModel = new FoldingModel(textModel, new TestDecorationProvider(textModel));
        foldingModel.Update(FoldingRegions.FromFoldRanges(
        [
            new FoldRange { StartLineNumber = 1, EndLineNumber = 2, Type = "comment" },
            new FoldRange { StartLineNumber = 3, EndLineNumber = 4, Type = "region" },
            new FoldRange { StartLineNumber = 5, EndLineNumber = 6, Type = "comment" },
        ]));

        SetCollapseStateForType(foldingModel, "comment", true);

        AssertFoldedRanges(foldingModel, R(1, 2), R(5, 6));
    }

    [Fact]
    public void ToggleCollapseState_LevelsOne_OnlyTheRegionAtTheLine()
    {
        FoldingModel foldingModel = Create(NestedLines, RegionMarkers);

        ToggleCollapseState(foldingModel, 1, [4]);

        AssertFoldedRanges(foldingModel, R3);
    }
}
