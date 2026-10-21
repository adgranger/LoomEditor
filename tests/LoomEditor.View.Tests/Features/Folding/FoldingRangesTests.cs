using LoomEditor.View.Features.Folding;

namespace LoomEditor.View.Tests.Features.Folding;

// Ported from microsoft/vscode: src/vs/editor/contrib/folding/test/browser/foldingRanges.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
public class FoldingRangesTests
{
    private static FoldRange Fold(int from, int to, bool collapsed = false, FoldSource source = FoldSource.Provider, string? type = null) =>
        new() { StartLineNumber = from, EndLineNumber = to, Type = type, IsCollapsed = collapsed, Source = source };

    private static void AssertEqualRanges(FoldRange expected, FoldRange actual, string msg)
    {
        Assert.True(
            expected.StartLineNumber == actual.StartLineNumber && expected.EndLineNumber == actual.EndLineNumber
            && expected.Type == actual.Type && expected.IsCollapsed == actual.IsCollapsed && expected.Source == actual.Source,
            $"{msg}: expected {expected.StartLineNumber}/{expected.EndLineNumber} {expected.Type} {expected.IsCollapsed} {expected.Source}"
            + $" but got {actual.StartLineNumber}/{actual.EndLineNumber} {actual.Type} {actual.IsCollapsed} {actual.Source}");
    }

    // Monaco: 'sanitizeAndMerge1'
    [Fact]
    public void SanitizeAndMerge1()
    {
        FoldRange[] set1 =
        [
            Fold(0, 100), // invalid, should be removed
            Fold(1, 100, false, FoldSource.Provider, "A"), // valid
            Fold(1, 100, false, FoldSource.Provider, "Z"), // invalid, duplicate start
            Fold(10, 10, false), // invalid, should be removed
            Fold(20, 80, false, FoldSource.Provider, "C1"), // valid inside 'B'
            Fold(22, 80, true, FoldSource.Provider, "D1"), // valid inside 'C1'
            Fold(90, 101), // invalid, should be removed
        ];
        FoldRange[] set2 =
        [
            Fold(20, 80, true), // should merge with C1
            Fold(18, 80, true), // invalid, out of order
            Fold(21, 81, true, FoldSource.Provider, "Z"), // invalid, overlapping
            Fold(22, 80, true, FoldSource.Provider, "D2"), // should merge with D1
        ];

        List<FoldRange> result = FoldingRegions.SanitizeAndMerge(set1, set2, 100);

        Assert.Equal(3, result.Count);
        AssertEqualRanges(Fold(1, 100, false, FoldSource.Provider, "A"), result[0], "A1");
        AssertEqualRanges(Fold(20, 80, true, FoldSource.Provider, "C1"), result[1], "C1");
        AssertEqualRanges(Fold(22, 80, true, FoldSource.Provider, "D1"), result[2], "D1");
    }

    // Monaco: 'sanitizeAndMerge2'
    [Fact]
    public void SanitizeAndMerge2()
    {
        FoldRange[] set1 =
        [
            Fold(1, 100, false, FoldSource.Provider, "a1"), // valid
            Fold(2, 100, false, FoldSource.Provider, "a2"), // valid
            Fold(3, 19, false, FoldSource.Provider, "a3"), // valid
            Fold(20, 71, false, FoldSource.Provider, "a4"), // overlaps b3
            Fold(21, 29, false, FoldSource.Provider, "a5"), // valid
            Fold(81, 91, false, FoldSource.Provider, "a6"), // overlaps b4
        ];
        FoldRange[] set2 =
        [
            Fold(30, 39, true, FoldSource.Provider, "b1"), // valid, will be recovered
            Fold(40, 49, true, FoldSource.UserDefined, "b2"), // valid
            Fold(50, 100, true, FoldSource.UserDefined, "b3"), // overlaps a4
            Fold(80, 90, true, FoldSource.UserDefined, "b4"), // overlaps a6
            Fold(92, 100, true, FoldSource.UserDefined, "b5"), // valid
        ];

        List<FoldRange> result = FoldingRegions.SanitizeAndMerge(set1, set2, 100);

        Assert.Equal(9, result.Count);
        AssertEqualRanges(Fold(1, 100, false, FoldSource.Provider, "a1"), result[0], "P1");
        AssertEqualRanges(Fold(2, 100, false, FoldSource.Provider, "a2"), result[1], "P2");
        AssertEqualRanges(Fold(3, 19, false, FoldSource.Provider, "a3"), result[2], "P3");
        AssertEqualRanges(Fold(21, 29, false, FoldSource.Provider, "a5"), result[3], "P4");
        AssertEqualRanges(Fold(30, 39, true, FoldSource.Recovered, "b1"), result[4], "P5");
        AssertEqualRanges(Fold(40, 49, true, FoldSource.UserDefined, "b2"), result[5], "P6");
        AssertEqualRanges(Fold(50, 100, true, FoldSource.UserDefined, "b3"), result[6], "P7");
        AssertEqualRanges(Fold(80, 90, true, FoldSource.UserDefined, "b4"), result[7], "P8");
        AssertEqualRanges(Fold(92, 100, true, FoldSource.UserDefined, "b5"), result[8], "P9");
    }

    // Monaco: 'sanitizeAndMerge3'
    [Fact]
    public void SanitizeAndMerge3()
    {
        FoldRange[] set1 =
        [
            Fold(1, 100, false, FoldSource.Provider, "a1"), // valid
            Fold(10, 29, false, FoldSource.Provider, "a2"), // matches manual hidden
            Fold(35, 39, true, FoldSource.Recovered, "a3"), // valid
        ];
        FoldRange[] set2 =
        [
            Fold(10, 29, true, FoldSource.Recovered, "b1"), // matches a
            Fold(20, 28, true, FoldSource.Provider, "b2"), // should remain
            Fold(30, 39, true, FoldSource.Recovered, "b3"), // should remain
        ];

        List<FoldRange> result = FoldingRegions.SanitizeAndMerge(set1, set2, 100);

        Assert.Equal(5, result.Count);
        AssertEqualRanges(Fold(1, 100, false, FoldSource.Provider, "a1"), result[0], "R1");
        AssertEqualRanges(Fold(10, 29, true, FoldSource.Provider, "a2"), result[1], "R2");
        AssertEqualRanges(Fold(20, 28, true, FoldSource.Recovered, "b2"), result[2], "R3");
        AssertEqualRanges(Fold(30, 39, true, FoldSource.Recovered, "b3"), result[3], "R3");
        AssertEqualRanges(Fold(35, 39, true, FoldSource.Recovered, "a3"), result[4], "R4");
    }

    // Monaco: 'sanitizeAndMerge4'
    [Fact]
    public void SanitizeAndMerge4()
    {
        FoldRange[] set1 = [Fold(1, 100, false, FoldSource.Provider, "a1")];
        FoldRange[] set2 =
        [
            Fold(20, 28, true, FoldSource.Provider, "b1"), // hidden
            Fold(30, 38, true, FoldSource.Provider, "b2"), // hidden
        ];

        List<FoldRange> result = FoldingRegions.SanitizeAndMerge(set1, set2, 100);

        Assert.Equal(3, result.Count);
        AssertEqualRanges(Fold(1, 100, false, FoldSource.Provider, "a1"), result[0], "R1");
        AssertEqualRanges(Fold(20, 28, true, FoldSource.Recovered, "b1"), result[1], "R2");
        AssertEqualRanges(Fold(30, 38, true, FoldSource.Recovered, "b2"), result[2], "R3");
    }

    [Fact]
    public void GetParentIndex_NestedRegions_ReturnsTheEnclosingRegion()
    {
        FoldingRegions regions = FoldingRegions.FromFoldRanges([Fold(1, 10), Fold(2, 5), Fold(3, 4), Fold(6, 9), Fold(20, 30)]);

        Assert.Equal(-1, regions.GetParentIndex(0));
        Assert.Equal(0, regions.GetParentIndex(1));
        Assert.Equal(1, regions.GetParentIndex(2));
        Assert.Equal(0, regions.GetParentIndex(3));
        Assert.Equal(-1, regions.GetParentIndex(4));
        Assert.Equal(2, regions.FindRange(4));
    }

    [Fact]
    public void SetSource_UserDefinedThenProvider_ClearsBothFlags()
    {
        FoldingRegions regions = FoldingRegions.FromFoldRanges([Fold(1, 10)]);

        regions.SetSource(0, FoldSource.UserDefined);
        Assert.Equal(FoldSource.UserDefined, regions.GetSource(0));
        regions.SetSource(0, FoldSource.Recovered);
        Assert.Equal(FoldSource.Recovered, regions.GetSource(0));
        regions.SetSource(0, FoldSource.Provider);

        Assert.Equal(FoldSource.Provider, regions.GetSource(0));
    }

    [Fact]
    public void Constructor_MoreThanMaxFoldingRegions_Throws()
    {
        int count = (int)FoldingRegions.MaxFoldingRegions + 1;

        Assert.Throws<ArgumentException>(() => new FoldingRegions(new uint[count], new uint[count]));
    }

    [Fact]
    public void FromFoldRanges_NoTypes_HasTypesIsFalse()
    {
        Assert.False(FoldingRegions.FromFoldRanges([Fold(1, 2)]).HasTypes());
        Assert.True(FoldingRegions.FromFoldRanges([Fold(1, 2, type: "region")]).HasTypes());
    }
}
