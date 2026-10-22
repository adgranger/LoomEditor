// Ported from microsoft/vscode: src/vs/editor/contrib/folding/test/browser/foldingModel.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Tests.Features.Folding.TestHelpers;

namespace LoomEditor.View.Tests.Features.Folding;

public class FoldingModelTests
{
    private readonly record struct ExpectedRegion(int StartLineNumber, int EndLineNumber, bool IsCollapsed);

    private static readonly FoldingMarkers RegionMarkers = new(new Regex(@"^//#region$"), new Regex(@"^//#endregion$"));

    private static ExpectedRegion R(int startLineNumber, int endLineNumber, bool isCollapsed = false) =>
        new(startLineNumber, endLineNumber, isCollapsed);

    private static ExpectedDecoration D(int line, ExpectedDecorationType type) => new(line, type);

    private static (TextModel Model, FoldingModel Folding) Create(string[] lines)
    {
        var textModel = new TextModel(string.Join('\n', lines));
        return (textModel, new FoldingModel(textModel, new TestDecorationProvider(textModel)));
    }

    private static void AssertRegion(FoldingRegion? actual, ExpectedRegion? expected)
    {
        Assert.Equal(expected is not null, actual is not null);
        if (actual is not null && expected is { } e)
        {
            Assert.Equal(new ExpectedRegion(e.StartLineNumber, e.EndLineNumber, e.IsCollapsed), ToExpected(actual));
        }
    }

    private static ExpectedRegion ToExpected(FoldingRegion r) => new(r.StartLineNumber, r.EndLineNumber, r.IsCollapsed);

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

    private static void AssertDecorations(FoldingModel foldingModel, params ExpectedDecoration[] expected) =>
        Assert.Equal(expected, ((TestDecorationProvider)foldingModel.DecorationProvider).GetDecorations());

    private static void AssertRegions(IEnumerable<FoldingRegion> actual, params ExpectedRegion[] expected) =>
        Assert.Equal(expected, actual.Select(ToExpected));

    private static readonly string[] ClassLines =
    [
        "/**",
        " * Comment",
        " */",
        "class A {",
        "  void foo() {",
        "    // comment {",
        "  }",
        "}",
    ];

    // Monaco: 'Folding Model' › 'getRegionAtLine'
    [Fact]
    public void GetRegionAtLine()
    {
        (TextModel textModel, FoldingModel foldingModel) = Create(ClassLines);
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false));

        ExpectedRegion r1 = R(1, 3), r2 = R(4, 7), r3 = R(5, 6);
        AssertRanges(foldingModel, r1, r2, r3);

        AssertRegion(foldingModel.GetRegionAtLine(1), r1);
        AssertRegion(foldingModel.GetRegionAtLine(2), r1);
        AssertRegion(foldingModel.GetRegionAtLine(3), r1);
        AssertRegion(foldingModel.GetRegionAtLine(4), r2);
        AssertRegion(foldingModel.GetRegionAtLine(5), r3);
        AssertRegion(foldingModel.GetRegionAtLine(6), r3);
        AssertRegion(foldingModel.GetRegionAtLine(7), r2);
        AssertRegion(foldingModel.GetRegionAtLine(8), null);
    }

    // Monaco: 'Folding Model' › 'collapse'
    [Fact]
    public void Collapse()
    {
        (TextModel textModel, FoldingModel foldingModel) = Create(ClassLines);
        FoldingRegions ranges = IndentRanges.ComputeRanges(textModel, false);
        foldingModel.Update(ranges);

        ExpectedRegion r1 = R(1, 3), r2 = R(4, 7), r3 = R(5, 6);
        AssertRanges(foldingModel, r1, r2, r3);

        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!]);
        foldingModel.Update(ranges);
        AssertRanges(foldingModel, R(1, 3, true), r2, r3);

        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(5)!]);
        foldingModel.Update(ranges);
        AssertRanges(foldingModel, R(1, 3, true), r2, R(5, 6, true));

        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(7)!]);
        foldingModel.Update(ranges);
        AssertRanges(foldingModel, R(1, 3, true), R(4, 7, true), R(5, 6, true));
    }

    // Monaco: 'Folding Model' › 'update'
    [Fact]
    public void Update()
    {
        (TextModel textModel, FoldingModel foldingModel) = Create(ClassLines);
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false));

        AssertRanges(foldingModel, R(1, 3), R(4, 7), R(5, 6));
        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(2)!, foldingModel.GetRegionAtLine(5)!]);

        textModel.ApplyEdits([TextEdit.Insert(new Position(4, 1), "//hello\n")]);

        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false));

        AssertRanges(foldingModel, R(1, 3, true), R(5, 8), R(6, 7, true));
    }

    // Monaco: 'Folding Model' › 'delete'
    // Monaco deletes up to column 0, which the model validates to 1; Range rejects column 0.
    [Fact]
    public void Delete()
    {
        (TextModel textModel, FoldingModel foldingModel) = Create(
        [
            "function foo() {",
            "  switch (x) {",
            "    case 1:",
            "      //hello1",
            "      break;",
            "    case 2:",
            "      //hello2",
            "      break;",
            "    case 3:",
            "      //hello3",
            "      break;",
            "  }",
            "}",
        ]);
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false));

        AssertRanges(foldingModel, R(1, 12), R(2, 11), R(3, 5), R(6, 8), R(9, 11));
        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(6)!]);

        textModel.ApplyEdits([TextEdit.Delete(new Range(6, 11, 9, 1))]);

        foldingModel.Update(IndentRanges.ComputeRanges(textModel, true), SelectedLines.From([new Selection(7, 1, 7, 1)]));

        AssertRanges(foldingModel, R(1, 9), R(2, 8), R(3, 5), R(6, 8));
    }

    // Monaco: 'Folding Model' › 'getRegionsInside'
    [Fact]
    public void GetRegionsInside()
    {
        (TextModel textModel, FoldingModel foldingModel) = Create(ClassLines);
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false));

        ExpectedRegion r1 = R(1, 3), r2 = R(4, 7), r3 = R(5, 6);
        AssertRanges(foldingModel, r1, r2, r3);
        FoldingRegion? region1 = foldingModel.GetRegionAtLine(r1.StartLineNumber);
        FoldingRegion? region2 = foldingModel.GetRegionAtLine(r2.StartLineNumber);
        FoldingRegion? region3 = foldingModel.GetRegionAtLine(r3.StartLineNumber);

        AssertRegions(foldingModel.GetRegionsInside(null), r1, r2, r3);
        AssertRegions(foldingModel.GetRegionsInside(region1));
        AssertRegions(foldingModel.GetRegionsInside(region2), r3);
        AssertRegions(foldingModel.GetRegionsInside(region3));
    }

    // Monaco: 'Folding Model' › 'getRegionsInsideWithLevel'
    [Fact]
    public void GetRegionsInsideWithLevel()
    {
        (TextModel textModel, FoldingModel foldingModel) = Create(
        [
            "//#region",
            "//#endregion",
            "class A {",
            "  void foo() {",
            "    if (true) {",
            "        return;",
            "    }",
            "    if (true) {",
            "      return;",
            "    }",
            "  }",
            "}",
        ]);
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false, RegionMarkers));

        ExpectedRegion r1 = R(1, 2), r2 = R(3, 11), r3 = R(4, 10), r4 = R(5, 6), r5 = R(8, 9);
        FoldingRegion? region1 = foldingModel.GetRegionAtLine(r1.StartLineNumber);
        FoldingRegion? region2 = foldingModel.GetRegionAtLine(r2.StartLineNumber);
        FoldingRegion? region3 = foldingModel.GetRegionAtLine(r3.StartLineNumber);

        AssertRanges(foldingModel, r1, r2, r3, r4, r5);

        AssertRegions(foldingModel.GetRegionsInside(null, (r, level) => level == 1), r1, r2);
        AssertRegions(foldingModel.GetRegionsInside(null, (r, level) => level == 2), r3);
        AssertRegions(foldingModel.GetRegionsInside(null, (r, level) => level == 3), r4, r5);

        AssertRegions(foldingModel.GetRegionsInside(region2, (r, level) => level == 1), r3);
        AssertRegions(foldingModel.GetRegionsInside(region2, (r, level) => level == 2), r4, r5);
        AssertRegions(foldingModel.GetRegionsInside(region3, (r, level) => level == 1), r4, r5);

        AssertRegions(foldingModel.GetRegionsInside(region2, (r, level) => r.HidesLine(9)), r3, r5);

        AssertRegions(foldingModel.GetRegionsInside(region1, (r, level) => level == 1));
    }

    // Monaco: 'Folding Model' › 'getRegionAtLine2'
    [Fact]
    public void GetRegionAtLine2()
    {
        (TextModel textModel, FoldingModel foldingModel) = Create(
        [
            "//#region",
            "class A {",
            "  void foo() {",
            "    if (true) {",
            "      //hello",
            "    }",
            "",
            "  }",
            "}",
            "//#endregion",
            "",
        ]);
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false, RegionMarkers));

        ExpectedRegion r1 = R(1, 10), r2 = R(2, 8), r3 = R(3, 7), r4 = R(4, 5);
        AssertRanges(foldingModel, r1, r2, r3, r4);

        AssertRegions(foldingModel.GetAllRegionsAtLine(1), r1);
        AssertRegions(foldingModel.GetAllRegionsAtLine(2), r2, r1);
        AssertRegions(foldingModel.GetAllRegionsAtLine(3), r3, r2, r1);
        AssertRegions(foldingModel.GetAllRegionsAtLine(4), r4, r3, r2, r1);
        AssertRegions(foldingModel.GetAllRegionsAtLine(5), r4, r3, r2, r1);
        AssertRegions(foldingModel.GetAllRegionsAtLine(6), r3, r2, r1);
        AssertRegions(foldingModel.GetAllRegionsAtLine(7), r3, r2, r1);
        AssertRegions(foldingModel.GetAllRegionsAtLine(8), r2, r1);
        AssertRegions(foldingModel.GetAllRegionsAtLine(9), r1);
        AssertRegions(foldingModel.GetAllRegionsAtLine(10), r1);
        AssertRegions(foldingModel.GetAllRegionsAtLine(11));
    }

    // Monaco: 'Folding Model' › 'folding decoration'
    [Fact]
    public void FoldingDecoration()
    {
        (TextModel textModel, FoldingModel foldingModel) = Create(
        [
            "class A {",
            "  void foo() {",
            "    if (true) {",
            "      hoo();",
            "    }",
            "  }",
            "}",
        ]);
        FoldingRegions ranges = IndentRanges.ComputeRanges(textModel, false);
        foldingModel.Update(ranges);

        ExpectedRegion r1 = R(1, 6), r3 = R(3, 4);
        const ExpectedDecorationType Expanded = ExpectedDecorationType.Expanded;
        const ExpectedDecorationType Collapsed = ExpectedDecorationType.Collapsed;
        const ExpectedDecorationType Hidden = ExpectedDecorationType.Hidden;

        AssertRanges(foldingModel, r1, R(2, 5), r3);
        AssertDecorations(foldingModel, D(1, Expanded), D(2, Expanded), D(3, Expanded));

        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(2)!]);
        AssertRanges(foldingModel, r1, R(2, 5, true), r3);
        AssertDecorations(foldingModel, D(1, Expanded), D(2, Collapsed), D(3, Hidden));

        foldingModel.Update(ranges);
        AssertRanges(foldingModel, r1, R(2, 5, true), r3);
        AssertDecorations(foldingModel, D(1, Expanded), D(2, Collapsed), D(3, Hidden));

        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!]);
        AssertRanges(foldingModel, R(1, 6, true), R(2, 5, true), r3);
        AssertDecorations(foldingModel, D(1, Collapsed), D(2, Hidden), D(3, Hidden));

        foldingModel.Update(ranges);
        AssertRanges(foldingModel, R(1, 6, true), R(2, 5, true), r3);
        AssertDecorations(foldingModel, D(1, Collapsed), D(2, Hidden), D(3, Hidden));

        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(1)!, foldingModel.GetRegionAtLine(3)!]);
        AssertRanges(foldingModel, r1, R(2, 5, true), R(3, 4, true));
        AssertDecorations(foldingModel, D(1, Expanded), D(2, Collapsed), D(3, Hidden));

        foldingModel.Update(ranges);
        AssertRanges(foldingModel, r1, R(2, 5, true), R(3, 4, true));
        AssertDecorations(foldingModel, D(1, Expanded), D(2, Collapsed), D(3, Hidden));
    }

    private static FoldRange Fr(int start, int end, FoldSource source) =>
        new() { StartLineNumber = start, EndLineNumber = end, Source = source };

    // Monaco: 'Folding Model' › 'removeManualRanges - cursor on manual range removes innermost only'
    [Fact]
    public void RemoveManualRanges_CursorOnManualRange_RemovesInnermostOnly()
    {
        (_, FoldingModel foldingModel) = Create(["class A {", "  void foo() {", "    if (true) {", "      return;", "    }", "  }", "}"]);
        foldingModel.Update(FoldingRegions.FromFoldRanges(
            [Fr(1, 6, FoldSource.Provider), Fr(2, 5, FoldSource.UserDefined), Fr(3, 4, FoldSource.UserDefined)]));
        AssertRanges(foldingModel, R(1, 6), R(2, 5), R(3, 4));

        foldingModel.RemoveManualRanges([new Range(4, 1, 4, 1)]);
        AssertRanges(foldingModel, R(1, 6), R(2, 5));

        foldingModel.RemoveManualRanges([new Range(3, 1, 3, 1)]);
        AssertRanges(foldingModel, R(1, 6));
    }

    // Monaco: 'Folding Model' › 'removeManualRanges - cursor skips provider ranges to remove nearest manual range'
    [Fact]
    public void RemoveManualRanges_CursorSkipsProviderRanges_RemovesNearestManualRange()
    {
        (_, FoldingModel foldingModel) = Create(["class A {", "  void foo() {", "    return;", "  }", "}"]);
        foldingModel.Update(FoldingRegions.FromFoldRanges([Fr(1, 5, FoldSource.UserDefined), Fr(2, 4, FoldSource.Provider)]));

        foldingModel.RemoveManualRanges([new Range(3, 1, 3, 1)]);
        AssertRanges(foldingModel, R(2, 4));
    }

    // Monaco: 'Folding Model' › 'removeManualRanges - cursor not on manual range removes all manual ranges'
    [Fact]
    public void RemoveManualRanges_CursorNotOnManualRange_RemovesAllManualRanges()
    {
        (_, FoldingModel foldingModel) = Create(["// header", "class A {", "  void foo() {", "  }", "}", "// footer"]);
        foldingModel.Update(FoldingRegions.FromFoldRanges([Fr(2, 4, FoldSource.Provider), Fr(3, 4, FoldSource.UserDefined)]));
        AssertRanges(foldingModel, R(2, 4), R(3, 4));

        foldingModel.RemoveManualRanges([new Range(6, 1, 6, 2)]);
        AssertRanges(foldingModel, R(2, 4), R(3, 4));

        foldingModel.RemoveManualRanges([new Range(6, 1, 6, 1)]);
        AssertRanges(foldingModel, R(2, 4));
    }

    // Monaco: 'Folding Model' › 'removeManualRanges - single-line selection removes all intersecting manual ranges'
    [Fact]
    public void RemoveManualRanges_SingleLineSelection_RemovesAllIntersectingManualRanges()
    {
        (_, FoldingModel foldingModel) = Create(["class A {", "  void foo() {", "    if (true) {", "      return;", "    }", "  }", "}"]);
        foldingModel.Update(FoldingRegions.FromFoldRanges(
            [Fr(1, 6, FoldSource.Provider), Fr(2, 5, FoldSource.UserDefined), Fr(3, 4, FoldSource.UserDefined)]));

        foldingModel.RemoveManualRanges([new Range(4, 1, 4, 2)]);
        AssertRanges(foldingModel, R(1, 6));
    }

    // Monaco: 'Folding Model' › 'removeManualRanges - selection range removes intersecting manual ranges'
    [Fact]
    public void RemoveManualRanges_SelectionRange_RemovesIntersectingManualRanges()
    {
        (_, FoldingModel foldingModel) = Create(["class A {", "  void foo() {", "  }", "  void bar() {", "  }", "}"]);
        foldingModel.Update(FoldingRegions.FromFoldRanges(
            [Fr(1, 5, FoldSource.Provider), Fr(2, 3, FoldSource.UserDefined), Fr(4, 5, FoldSource.UserDefined)]));
        AssertRanges(foldingModel, R(1, 5), R(2, 3), R(4, 5));

        foldingModel.RemoveManualRanges([new Range(2, 1, 3, 1)]);
        AssertRanges(foldingModel, R(1, 5), R(4, 5));
    }

    [Fact]
    public void ToggleCollapseState_RaisesChangedWithTheToggledRegions()
    {
        (TextModel textModel, FoldingModel foldingModel) = Create(ClassLines);
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false));
        var events = new List<FoldingModelChangeEvent>();
        foldingModel.Changed += (_, e) => events.Add(e);

        foldingModel.ToggleCollapseState([foldingModel.GetRegionAtLine(5)!, foldingModel.GetRegionAtLine(1)!]);

        FoldingModelChangeEvent e = Assert.Single(events);
        Assert.Same(foldingModel, e.Model);
        Assert.Equal([0, 2], e.CollapseStateChanged!.Select(r => r.RegionIndex));
    }

    [Fact]
    public void Dispose_RemovesTheDecorations()
    {
        (TextModel textModel, FoldingModel foldingModel) = Create(ClassLines);
        var provider = (TestDecorationProvider)foldingModel.DecorationProvider;
        foldingModel.Update(IndentRanges.ComputeRanges(textModel, false));
        Assert.Equal(3, provider.GetDecorations().Count);

        foldingModel.Dispose();

        Assert.Empty(provider.GetDecorations());
    }
}
