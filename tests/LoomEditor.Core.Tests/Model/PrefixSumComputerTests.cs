// Ported from microsoft/vscode: src/vs/editor/test/common/viewModel/prefixSumComputer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.Core.Tests.Model;

// Only PrefixSumComputer is ported: the Monaco tests also run every case on ConstantTimePrefixSumComputer, which is not.
public class PrefixSumComputerTests
{
    // Like Monaco's `createBoth` wrapper: getPrefixSum(count) is the sum of the first `count` values.
    private sealed class Psc(params int[] values)
    {
        private readonly PrefixSumComputer _psc = new([.. values.Select(v => (uint)v)]);

        public int GetTotalSum() => _psc.GetTotalSum();

        public int GetPrefixSum(int count) => count == 0 ? 0 : _psc.GetPrefixSum(count - 1);

        public void SetValue(int index, int value) => _psc.SetValue(index, value);

        public void InsertValues(int index, params int[] items) => _psc.InsertValues(index, [.. items.Select(v => (uint)v)]);

        public void RemoveValues(int start, int count) => _psc.RemoveValues(start, count);

        public void AssertIndexOf(int sum, int index, int remainder) =>
            Assert.Equal(new PrefixSumIndexOfResult(index, remainder), _psc.GetIndexOf(sum));
    }

    private static void AssertPrefixSums(Psc psc, params int[] expectedFromOne)
    {
        for (int i = 0; i < expectedFromOne.Length; i++)
        {
            Assert.Equal(expectedFromOne[i], psc.GetPrefixSum(i + 1));
        }
    }

    // Monaco: 'comprehensive setValue and getIndexOf'
    [Fact]
    public void ComprehensiveSetValueAndGetIndexOf()
    {
        var psc = new Psc(1, 1, 2, 1, 3);
        Assert.Equal(8, psc.GetTotalSum());
        Assert.Equal(0, psc.GetPrefixSum(0));
        AssertPrefixSums(psc, 1, 2, 4, 5, 8);
        (int Sum, int Index, int Remainder)[] first = [(0, 0, 0), (1, 1, 0), (2, 2, 0), (3, 2, 1), (4, 3, 0), (5, 4, 0), (6, 4, 1), (7, 4, 2), (8, 4, 3)];
        foreach ((int sum, int index, int remainder) in first)
        {
            psc.AssertIndexOf(sum, index, remainder);
        }

        // [1, 2, 2, 1, 3]
        psc.SetValue(1, 2);
        Assert.Equal(9, psc.GetTotalSum());
        AssertPrefixSums(psc, 1, 3, 5, 6, 9);

        // [1, 0, 2, 1, 3]
        psc.SetValue(1, 0);
        Assert.Equal(7, psc.GetTotalSum());
        AssertPrefixSums(psc, 1, 1, 3, 4, 7);
        (int Sum, int Index, int Remainder)[] second = [(0, 0, 0), (1, 2, 0), (2, 2, 1), (3, 3, 0), (4, 4, 0), (5, 4, 1), (6, 4, 2)];
        foreach ((int sum, int index, int remainder) in second)
        {
            psc.AssertIndexOf(sum, index, remainder);
        }

        // [1, 0, 0, 1, 3]
        psc.SetValue(2, 0);
        Assert.Equal(5, psc.GetTotalSum());
        AssertPrefixSums(psc, 1, 1, 1, 2, 5);
        (int Sum, int Index, int Remainder)[] third = [(0, 0, 0), (1, 3, 0), (2, 4, 0), (3, 4, 1), (4, 4, 2)];
        foreach ((int sum, int index, int remainder) in third)
        {
            psc.AssertIndexOf(sum, index, remainder);
        }

        // [1, 0, 0, 0, 3]
        psc.SetValue(3, 0);
        Assert.Equal(4, psc.GetTotalSum());
        AssertPrefixSums(psc, 1, 1, 1, 1, 4);
        (int Sum, int Index, int Remainder)[] fourth = [(0, 0, 0), (1, 4, 0), (2, 4, 1), (3, 4, 2)];
        foreach ((int sum, int index, int remainder) in fourth)
        {
            psc.AssertIndexOf(sum, index, remainder);
        }

        // [1, 1, 0, 1, 1]
        psc.SetValue(1, 1);
        psc.SetValue(3, 1);
        psc.SetValue(4, 1);
        Assert.Equal(4, psc.GetTotalSum());
        AssertPrefixSums(psc, 1, 2, 2, 3, 4);
        (int Sum, int Index, int Remainder)[] fifth = [(0, 0, 0), (1, 1, 0), (2, 3, 0), (3, 4, 0)];
        foreach ((int sum, int index, int remainder) in fifth)
        {
            psc.AssertIndexOf(sum, index, remainder);
        }
    }

    // --- getTotalSum ---

    // Monaco: 'getTotalSum with typical values'
    [Fact]
    public void GetTotalSum_TypicalValues()
    {
        Assert.Equal(8, new Psc(1, 1, 2, 1, 3).GetTotalSum());
        Assert.Equal(10, new Psc(10).GetTotalSum());
        Assert.Equal(15, new Psc(5, 5, 5).GetTotalSum());
    }

    // Monaco: 'getTotalSum with all zeroes'
    [Fact]
    public void GetTotalSum_AllZeroes()
    {
        Assert.Equal(0, new Psc(0, 0, 0).GetTotalSum());
        Assert.Equal(0, new Psc(0).GetTotalSum());
    }

    // Monaco: 'getTotalSum with empty array'
    [Fact]
    public void GetTotalSum_EmptyArray() => Assert.Equal(0, new Psc().GetTotalSum());

    // Monaco: 'getTotalSum with single element'
    [Fact]
    public void GetTotalSum_SingleElement()
    {
        Assert.Equal(0, new Psc(0).GetTotalSum());
        Assert.Equal(1, new Psc(1).GetTotalSum());
        Assert.Equal(100, new Psc(100).GetTotalSum());
    }

    // --- getPrefixSum ---

    // Monaco: 'getPrefixSum with typical values'
    [Fact]
    public void GetPrefixSum_TypicalValues()
    {
        var psc = new Psc(1, 1, 2, 1, 3);
        Assert.Equal(0, psc.GetPrefixSum(0));
        AssertPrefixSums(psc, 1, 2, 4, 5, 8);
    }

    // Monaco: 'getPrefixSum with all zeroes'
    [Fact]
    public void GetPrefixSum_AllZeroes()
    {
        var psc = new Psc(0, 0, 0);
        for (int i = 0; i <= 3; i++)
        {
            Assert.Equal(0, psc.GetPrefixSum(i));
        }
    }

    // Monaco: 'getPrefixSum with single element'
    [Fact]
    public void GetPrefixSum_SingleElement()
    {
        var psc = new Psc(7);
        Assert.Equal(0, psc.GetPrefixSum(0));
        Assert.Equal(7, psc.GetPrefixSum(1));
    }

    // Monaco: 'getPrefixSum with empty array'
    [Fact]
    public void GetPrefixSum_EmptyArray() => Assert.Equal(0, new Psc().GetPrefixSum(0));

    // Monaco: 'getPrefixSum with leading/trailing zeroes'
    [Fact]
    public void GetPrefixSum_LeadingAndTrailingZeroes()
    {
        var psc = new Psc(0, 0, 3, 0, 0);
        Assert.Equal(0, psc.GetPrefixSum(0));
        AssertPrefixSums(psc, 0, 0, 3, 3, 3);
    }

    // --- getIndexOf ---

    // Monaco: 'getIndexOf with typical values'
    [Fact]
    public void GetIndexOf_TypicalValues()
    {
        var psc = new Psc(1, 1, 2, 1, 3);
        (int Sum, int Index, int Remainder)[] expected = [(0, 0, 0), (1, 1, 0), (2, 2, 0), (3, 2, 1), (4, 3, 0), (5, 4, 0), (6, 4, 1), (7, 4, 2), (8, 4, 3)];
        foreach ((int sum, int index, int remainder) in expected)
        {
            psc.AssertIndexOf(sum, index, remainder);
        }
    }

    // Monaco: 'getIndexOf with all zeroes'
    [Fact]
    public void GetIndexOf_AllZeroes() => new Psc(0, 0, 0).AssertIndexOf(0, 2, 0);

    // Monaco: 'getIndexOf with single zero'
    [Fact]
    public void GetIndexOf_SingleZero() => new Psc(0).AssertIndexOf(0, 0, 0);

    // Monaco: 'getIndexOf with single element'
    [Fact]
    public void GetIndexOf_SingleElement()
    {
        var psc = new Psc(5);
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 0, 1);
        psc.AssertIndexOf(4, 0, 4);
    }

    // Monaco: 'getIndexOf with leading zeroes'
    [Fact]
    public void GetIndexOf_LeadingZeroes()
    {
        var psc = new Psc(0, 0, 3);
        psc.AssertIndexOf(0, 2, 0);
        psc.AssertIndexOf(1, 2, 1);
        psc.AssertIndexOf(2, 2, 2);
    }

    // Monaco: 'getIndexOf with trailing zeroes'
    [Fact]
    public void GetIndexOf_TrailingZeroes()
    {
        var psc = new Psc(3, 0, 0);
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 0, 1);
        psc.AssertIndexOf(2, 0, 2);
    }

    // Monaco: 'getIndexOf with interleaved zeroes'
    [Fact]
    public void GetIndexOf_InterleavedZeroes()
    {
        var psc = new Psc(0, 1, 0, 2, 0);
        psc.AssertIndexOf(0, 1, 0);
        psc.AssertIndexOf(1, 3, 0);
        psc.AssertIndexOf(2, 3, 1);
    }

    // Monaco: 'getIndexOf with all ones'
    [Fact]
    public void GetIndexOf_AllOnes()
    {
        var psc = new Psc(1, 1, 1, 1, 1);
        for (int i = 0; i < 5; i++)
        {
            psc.AssertIndexOf(i, i, 0);
        }
    }

    // Monaco: 'getIndexOf with large value in single element'
    [Fact]
    public void GetIndexOf_LargeValueInSingleElement()
    {
        var psc = new Psc(1000);
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(500, 0, 500);
        psc.AssertIndexOf(999, 0, 999);
    }

    // --- setValue ---

    // Monaco: 'setValue no-op when value unchanged'
    [Fact]
    public void SetValue_ValueUnchanged_IsANoOp()
    {
        var psc = new Psc(1, 2, 3);
        Assert.Equal(6, psc.GetTotalSum());
        psc.SetValue(1, 2);
        Assert.Equal(6, psc.GetTotalSum());
    }

    // Monaco: 'setValue increase'
    [Fact]
    public void SetValue_Increase()
    {
        var psc = new Psc(1, 2, 3);
        psc.SetValue(1, 5);
        Assert.Equal(9, psc.GetTotalSum());
        Assert.Equal(6, psc.GetPrefixSum(2));
        Assert.Equal(9, psc.GetPrefixSum(3));
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(5, 1, 4);
        psc.AssertIndexOf(6, 2, 0);
    }

    // Monaco: 'setValue decrease'
    [Fact]
    public void SetValue_Decrease()
    {
        var psc = new Psc(1, 5, 3);
        psc.SetValue(1, 2);
        Assert.Equal(6, psc.GetTotalSum());
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(2, 1, 1);
        psc.AssertIndexOf(3, 2, 0);
    }

    // Monaco: 'setValue to zero'
    [Fact]
    public void SetValue_ToZero()
    {
        var psc = new Psc(1, 2, 3);
        psc.SetValue(1, 0);
        Assert.Equal(4, psc.GetTotalSum());
        Assert.Equal(1, psc.GetPrefixSum(2));
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 2, 0);
    }

    // Monaco: 'setValue from zero'
    [Fact]
    public void SetValue_FromZero()
    {
        var psc = new Psc(0, 0, 0);
        psc.SetValue(1, 3);
        Assert.Equal(3, psc.GetTotalSum());
        psc.AssertIndexOf(0, 1, 0);
        psc.AssertIndexOf(2, 1, 2);
    }

    // Monaco: 'setValue on first element'
    [Fact]
    public void SetValue_OnFirstElement()
    {
        var psc = new Psc(1, 2, 3);
        psc.SetValue(0, 10);
        Assert.Equal(15, psc.GetTotalSum());
        Assert.Equal(10, psc.GetPrefixSum(1));
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(9, 0, 9);
        psc.AssertIndexOf(10, 1, 0);
    }

    // Monaco: 'setValue on last element'
    [Fact]
    public void SetValue_OnLastElement()
    {
        var psc = new Psc(1, 2, 3);
        psc.SetValue(2, 10);
        Assert.Equal(13, psc.GetTotalSum());
        psc.AssertIndexOf(3, 2, 0);
        psc.AssertIndexOf(12, 2, 9);
    }

    // Monaco: 'set all values to zero then restore'
    [Fact]
    public void SetValue_AllToZeroThenRestore()
    {
        var psc = new Psc(1, 2, 3);
        psc.SetValue(0, 0);
        psc.SetValue(1, 0);
        psc.SetValue(2, 0);
        Assert.Equal(0, psc.GetTotalSum());
        psc.AssertIndexOf(0, 2, 0);

        psc.SetValue(0, 4);
        Assert.Equal(4, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(3, 0, 3);
    }

    // Monaco: 'setValue multiple times on same index'
    [Fact]
    public void SetValue_MultipleTimesOnSameIndex()
    {
        var psc = new Psc(1, 1, 1);
        psc.SetValue(1, 5);
        psc.SetValue(1, 2);
        psc.SetValue(1, 10);
        Assert.Equal(12, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(10, 1, 9);
        psc.AssertIndexOf(11, 2, 0);
    }

    // --- insertValues ---

    // Monaco: 'insertValues at beginning'
    [Fact]
    public void InsertValues_AtBeginning()
    {
        var psc = new Psc(3, 4);
        psc.InsertValues(0, 1, 2);
        Assert.Equal(10, psc.GetTotalSum());
        AssertPrefixSums(psc, 1, 3, 6, 10);
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(3, 2, 0);
    }

    // Monaco: 'insertValues at end'
    [Fact]
    public void InsertValues_AtEnd()
    {
        var psc = new Psc(1, 2);
        psc.InsertValues(2, 3, 4);
        Assert.Equal(10, psc.GetTotalSum());
        Assert.Equal(6, psc.GetPrefixSum(3));
        Assert.Equal(10, psc.GetPrefixSum(4));
    }

    // Monaco: 'insertValues in the middle'
    [Fact]
    public void InsertValues_InTheMiddle()
    {
        var psc = new Psc(1, 4);
        psc.InsertValues(1, 2, 3);
        Assert.Equal(10, psc.GetTotalSum());
        AssertPrefixSums(psc, 1, 3, 6, 10);
    }

    // Monaco: 'insertValues with zeroes'
    [Fact]
    public void InsertValues_WithZeroes()
    {
        var psc = new Psc(1, 2);
        psc.InsertValues(1, 0, 0);
        Assert.Equal(3, psc.GetTotalSum());
        AssertPrefixSums(psc, 1, 1, 1, 3);
    }

    // Monaco: 'insertValues into all-zeroes'
    [Fact]
    public void InsertValues_IntoAllZeroes()
    {
        var psc = new Psc(0, 0, 0);
        psc.InsertValues(1, 2, 3);
        Assert.Equal(5, psc.GetTotalSum());
        Assert.Equal(0, psc.GetPrefixSum(1));
        Assert.Equal(2, psc.GetPrefixSum(2));
        Assert.Equal(5, psc.GetPrefixSum(3));
        psc.AssertIndexOf(0, 1, 0);
        psc.AssertIndexOf(2, 2, 0);
        psc.AssertIndexOf(4, 2, 2);
    }

    // Monaco: 'insertValues into empty computer'
    [Fact]
    public void InsertValues_IntoEmptyComputer()
    {
        var psc = new Psc();
        psc.InsertValues(0, 5, 3);
        Assert.Equal(8, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(4, 0, 4);
        psc.AssertIndexOf(5, 1, 0);
    }

    // --- removeValues ---

    // Monaco: 'removeValues from beginning'
    [Fact]
    public void RemoveValues_FromBeginning()
    {
        var psc = new Psc(1, 2, 3, 4);
        psc.RemoveValues(0, 2);
        Assert.Equal(7, psc.GetTotalSum());
        Assert.Equal(3, psc.GetPrefixSum(1));
        Assert.Equal(7, psc.GetPrefixSum(2));
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(3, 1, 0);
    }

    // Monaco: 'removeValues from end'
    [Fact]
    public void RemoveValues_FromEnd()
    {
        var psc = new Psc(1, 2, 3, 4);
        psc.RemoveValues(2, 2);
        Assert.Equal(3, psc.GetTotalSum());
        Assert.Equal(1, psc.GetPrefixSum(1));
        Assert.Equal(3, psc.GetPrefixSum(2));
    }

    // Monaco: 'removeValues from the middle'
    [Fact]
    public void RemoveValues_FromTheMiddle()
    {
        var psc = new Psc(1, 2, 3, 4);
        psc.RemoveValues(1, 2);
        Assert.Equal(5, psc.GetTotalSum());
        Assert.Equal(1, psc.GetPrefixSum(1));
        Assert.Equal(5, psc.GetPrefixSum(2));
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(4, 1, 3);
    }

    // Monaco: 'removeValues all'
    [Fact]
    public void RemoveValues_All()
    {
        var psc = new Psc(1, 2, 3);
        psc.RemoveValues(0, 3);
        Assert.Equal(0, psc.GetTotalSum());
    }

    // Monaco: 'removeValues single element'
    [Fact]
    public void RemoveValues_SingleElement()
    {
        var psc = new Psc(5, 10, 15);
        psc.RemoveValues(1, 1);
        Assert.Equal(20, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(5, 1, 0);
        psc.AssertIndexOf(19, 1, 14);
    }

    // Monaco: 'removeValues zero-valued elements'
    [Fact]
    public void RemoveValues_ZeroValuedElements()
    {
        var psc = new Psc(0, 0, 5, 0, 0);
        psc.RemoveValues(0, 2);
        Assert.Equal(5, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(4, 0, 4);
    }

    // --- combined operations ---

    // Monaco: 'insert then remove'
    [Fact]
    public void InsertThenRemove()
    {
        var psc = new Psc(1, 2, 3);
        psc.InsertValues(1, 10, 20);
        Assert.Equal(36, psc.GetTotalSum());
        psc.RemoveValues(1, 2);
        Assert.Equal(6, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(3, 2, 0);
    }

    // Monaco: 'remove then insert at same position'
    [Fact]
    public void RemoveThenInsertAtSamePosition()
    {
        var psc = new Psc(1, 2, 3);
        psc.RemoveValues(1, 1);
        psc.InsertValues(1, 5);
        Assert.Equal(9, psc.GetTotalSum());
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(5, 1, 4);
        psc.AssertIndexOf(6, 2, 0);
    }

    // Monaco: 'setValue then insert then remove'
    [Fact]
    public void SetValueThenInsertThenRemove()
    {
        var psc = new Psc(1, 1, 1);
        psc.SetValue(0, 5);
        psc.InsertValues(1, 10);
        psc.RemoveValues(3, 1);

        // [5, 10, 1]
        Assert.Equal(16, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(4, 0, 4);
        psc.AssertIndexOf(5, 1, 0);
        psc.AssertIndexOf(14, 1, 9);
        psc.AssertIndexOf(15, 2, 0);
    }

    // Monaco: 'multiple queries between mutations are consistent'
    [Fact]
    public void MultipleQueriesBetweenMutations_AreConsistent()
    {
        var psc = new Psc(2, 3, 5);
        Assert.Equal(10, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);

        psc.SetValue(1, 0);
        Assert.Equal(7, psc.GetTotalSum());
        psc.AssertIndexOf(2, 2, 0);

        psc.SetValue(1, 3);
        Assert.Equal(10, psc.GetTotalSum());
        psc.AssertIndexOf(2, 1, 0);
    }

    // --- edge cases ---

    // Monaco: 'large values'
    [Fact]
    public void LargeValues()
    {
        var psc = new Psc(100, 200, 300);
        Assert.Equal(600, psc.GetTotalSum());
        AssertPrefixSums(psc, 100, 300, 600);
        (int Sum, int Index, int Remainder)[] expected = [(0, 0, 0), (99, 0, 99), (100, 1, 0), (299, 1, 199), (300, 2, 0), (599, 2, 299)];
        foreach ((int sum, int index, int remainder) in expected)
        {
            psc.AssertIndexOf(sum, index, remainder);
        }
    }

    // Monaco: 'many elements'
    [Fact]
    public void ManyElements()
    {
        var psc = new Psc([.. Enumerable.Repeat(1, 100)]);
        Assert.Equal(100, psc.GetTotalSum());
        Assert.Equal(50, psc.GetPrefixSum(50));
        for (int i = 0; i < 100; i++)
        {
            psc.AssertIndexOf(i, i, 0);
        }
    }

    // Monaco: 'many elements all zeroes'
    [Fact]
    public void ManyElements_AllZeroes()
    {
        var psc = new Psc([.. Enumerable.Repeat(0, 100)]);
        Assert.Equal(0, psc.GetTotalSum());
        for (int i = 0; i <= 100; i++)
        {
            Assert.Equal(0, psc.GetPrefixSum(i));
        }

        psc.AssertIndexOf(0, 99, 0);
    }

    // Monaco: 'setValue between queries re-validates correctly'
    [Fact]
    public void SetValue_BetweenQueries_RevalidatesCorrectly()
    {
        var psc = new Psc(1, 1, 1, 1, 1);
        Assert.Equal(5, psc.GetTotalSum());

        psc.SetValue(2, 10);
        Assert.Equal(14, psc.GetTotalSum());
        Assert.Equal(12, psc.GetPrefixSum(3));
        psc.AssertIndexOf(2, 2, 0);
        psc.AssertIndexOf(11, 2, 9);
        psc.AssertIndexOf(12, 3, 0);
        psc.AssertIndexOf(13, 4, 0);

        psc.SetValue(0, 0);
        Assert.Equal(13, psc.GetTotalSum());
        psc.AssertIndexOf(0, 1, 0);
    }
}
