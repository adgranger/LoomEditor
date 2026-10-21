// Ported from microsoft/vscode: src/vs/editor/test/common/viewModel/prefixSumComputer.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;

namespace LoomEditor.Core.Tests.Model;

// Like Monaco's `forBoth`, every test runs on PrefixSumComputer (constantTime: false) and on ConstantTimePrefixSumComputer (true).
public class PrefixSumComputerTests
{
    // Like Monaco's `createBoth` wrapper: getPrefixSum(count) is the sum of the first `count` values.
    private abstract class Psc
    {
        public static Psc Create(bool constantTime, params int[] values) => constantTime ? new ConstantTimePsc(values) : new LinearPsc(values);

        public abstract int GetTotalSum();

        public abstract int GetPrefixSum(int count);

        public abstract void SetValue(int index, int value);

        public abstract void InsertValues(int index, params int[] items);

        public abstract void RemoveValues(int start, int count);

        public abstract PrefixSumIndexOfResult GetIndexOf(int sum);

        public void AssertIndexOf(int sum, int index, int remainder) =>
            Assert.Equal(new PrefixSumIndexOfResult(index, remainder), GetIndexOf(sum));
    }

    private sealed class LinearPsc(int[] values) : Psc
    {
        private readonly PrefixSumComputer _psc = new([.. values.Select(v => (uint)v)]);

        public override int GetTotalSum() => _psc.GetTotalSum();

        public override int GetPrefixSum(int count) => count == 0 ? 0 : _psc.GetPrefixSum(count - 1);

        public override void SetValue(int index, int value) => _psc.SetValue(index, value);

        public override void InsertValues(int index, params int[] items) => _psc.InsertValues(index, [.. items.Select(v => (uint)v)]);

        public override void RemoveValues(int start, int count) => _psc.RemoveValues(start, count);

        public override PrefixSumIndexOfResult GetIndexOf(int sum) => _psc.GetIndexOf(sum);
    }

    private sealed class ConstantTimePsc(int[] values) : Psc
    {
        private readonly ConstantTimePrefixSumComputer _psc = new([.. values]);

        public override int GetTotalSum() => _psc.GetTotalSum();

        public override int GetPrefixSum(int count) => _psc.GetPrefixSum(count);

        public override void SetValue(int index, int value) => _psc.SetValue(index, value);

        public override void InsertValues(int index, params int[] items) => _psc.InsertValues(index, items);

        public override void RemoveValues(int start, int count) => _psc.RemoveValues(start, count);

        public override PrefixSumIndexOfResult GetIndexOf(int sum) => _psc.GetIndexOf(sum);
    }

    private static void AssertPrefixSums(Psc psc, params int[] expectedFromOne)
    {
        for (int i = 0; i < expectedFromOne.Length; i++)
        {
            Assert.Equal(expectedFromOne[i], psc.GetPrefixSum(i + 1));
        }
    }

    // Monaco: 'comprehensive setValue and getIndexOf'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ComprehensiveSetValueAndGetIndexOf(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 1, 2, 1, 3);
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetTotalSum_TypicalValues(bool constantTime)
    {
        Assert.Equal(8, Psc.Create(constantTime, 1, 1, 2, 1, 3).GetTotalSum());
        Assert.Equal(10, Psc.Create(constantTime, 10).GetTotalSum());
        Assert.Equal(15, Psc.Create(constantTime, 5, 5, 5).GetTotalSum());
    }

    // Monaco: 'getTotalSum with all zeroes'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetTotalSum_AllZeroes(bool constantTime)
    {
        Assert.Equal(0, Psc.Create(constantTime, 0, 0, 0).GetTotalSum());
        Assert.Equal(0, Psc.Create(constantTime, 0).GetTotalSum());
    }

    // Monaco: 'getTotalSum with empty array'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetTotalSum_EmptyArray(bool constantTime) => Assert.Equal(0, Psc.Create(constantTime).GetTotalSum());

    // Monaco: 'getTotalSum with single element'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetTotalSum_SingleElement(bool constantTime)
    {
        Assert.Equal(0, Psc.Create(constantTime, 0).GetTotalSum());
        Assert.Equal(1, Psc.Create(constantTime, 1).GetTotalSum());
        Assert.Equal(100, Psc.Create(constantTime, 100).GetTotalSum());
    }

    // --- getPrefixSum ---

    // Monaco: 'getPrefixSum with typical values'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetPrefixSum_TypicalValues(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 1, 2, 1, 3);
        Assert.Equal(0, psc.GetPrefixSum(0));
        AssertPrefixSums(psc, 1, 2, 4, 5, 8);
    }

    // Monaco: 'getPrefixSum with all zeroes'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetPrefixSum_AllZeroes(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 0, 0, 0);
        for (int i = 0; i <= 3; i++)
        {
            Assert.Equal(0, psc.GetPrefixSum(i));
        }
    }

    // Monaco: 'getPrefixSum with single element'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetPrefixSum_SingleElement(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 7);
        Assert.Equal(0, psc.GetPrefixSum(0));
        Assert.Equal(7, psc.GetPrefixSum(1));
    }

    // Monaco: 'getPrefixSum with empty array'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetPrefixSum_EmptyArray(bool constantTime) => Assert.Equal(0, Psc.Create(constantTime).GetPrefixSum(0));

    // Monaco: 'getPrefixSum with leading/trailing zeroes'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetPrefixSum_LeadingAndTrailingZeroes(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 0, 0, 3, 0, 0);
        Assert.Equal(0, psc.GetPrefixSum(0));
        AssertPrefixSums(psc, 0, 0, 3, 3, 3);
    }

    // --- getIndexOf ---

    // Monaco: 'getIndexOf with typical values'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetIndexOf_TypicalValues(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 1, 2, 1, 3);
        (int Sum, int Index, int Remainder)[] expected = [(0, 0, 0), (1, 1, 0), (2, 2, 0), (3, 2, 1), (4, 3, 0), (5, 4, 0), (6, 4, 1), (7, 4, 2), (8, 4, 3)];
        foreach ((int sum, int index, int remainder) in expected)
        {
            psc.AssertIndexOf(sum, index, remainder);
        }
    }

    // Monaco: 'getIndexOf with all zeroes'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetIndexOf_AllZeroes(bool constantTime) => Psc.Create(constantTime, 0, 0, 0).AssertIndexOf(0, 2, 0);

    // Monaco: 'getIndexOf with single zero'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetIndexOf_SingleZero(bool constantTime) => Psc.Create(constantTime, 0).AssertIndexOf(0, 0, 0);

    // Monaco: 'getIndexOf with single element'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetIndexOf_SingleElement(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 5);
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 0, 1);
        psc.AssertIndexOf(4, 0, 4);
    }

    // Monaco: 'getIndexOf with leading zeroes'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetIndexOf_LeadingZeroes(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 0, 0, 3);
        psc.AssertIndexOf(0, 2, 0);
        psc.AssertIndexOf(1, 2, 1);
        psc.AssertIndexOf(2, 2, 2);
    }

    // Monaco: 'getIndexOf with trailing zeroes'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetIndexOf_TrailingZeroes(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 3, 0, 0);
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 0, 1);
        psc.AssertIndexOf(2, 0, 2);
    }

    // Monaco: 'getIndexOf with interleaved zeroes'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetIndexOf_InterleavedZeroes(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 0, 1, 0, 2, 0);
        psc.AssertIndexOf(0, 1, 0);
        psc.AssertIndexOf(1, 3, 0);
        psc.AssertIndexOf(2, 3, 1);
    }

    // Monaco: 'getIndexOf with all ones'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetIndexOf_AllOnes(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 1, 1, 1, 1);
        for (int i = 0; i < 5; i++)
        {
            psc.AssertIndexOf(i, i, 0);
        }
    }

    // Monaco: 'getIndexOf with large value in single element'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetIndexOf_LargeValueInSingleElement(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1000);
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(500, 0, 500);
        psc.AssertIndexOf(999, 0, 999);
    }

    // --- setValue ---

    // Monaco: 'setValue no-op when value unchanged'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetValue_ValueUnchanged_IsANoOp(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3);
        Assert.Equal(6, psc.GetTotalSum());
        psc.SetValue(1, 2);
        Assert.Equal(6, psc.GetTotalSum());
    }

    // Monaco: 'setValue increase'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetValue_Increase(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3);
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetValue_Decrease(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 5, 3);
        psc.SetValue(1, 2);
        Assert.Equal(6, psc.GetTotalSum());
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(2, 1, 1);
        psc.AssertIndexOf(3, 2, 0);
    }

    // Monaco: 'setValue to zero'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetValue_ToZero(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3);
        psc.SetValue(1, 0);
        Assert.Equal(4, psc.GetTotalSum());
        Assert.Equal(1, psc.GetPrefixSum(2));
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 2, 0);
    }

    // Monaco: 'setValue from zero'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetValue_FromZero(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 0, 0, 0);
        psc.SetValue(1, 3);
        Assert.Equal(3, psc.GetTotalSum());
        psc.AssertIndexOf(0, 1, 0);
        psc.AssertIndexOf(2, 1, 2);
    }

    // Monaco: 'setValue on first element'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetValue_OnFirstElement(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3);
        psc.SetValue(0, 10);
        Assert.Equal(15, psc.GetTotalSum());
        Assert.Equal(10, psc.GetPrefixSum(1));
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(9, 0, 9);
        psc.AssertIndexOf(10, 1, 0);
    }

    // Monaco: 'setValue on last element'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetValue_OnLastElement(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3);
        psc.SetValue(2, 10);
        Assert.Equal(13, psc.GetTotalSum());
        psc.AssertIndexOf(3, 2, 0);
        psc.AssertIndexOf(12, 2, 9);
    }

    // Monaco: 'set all values to zero then restore'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetValue_AllToZeroThenRestore(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3);
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetValue_MultipleTimesOnSameIndex(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 1, 1);
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InsertValues_AtBeginning(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 3, 4);
        psc.InsertValues(0, 1, 2);
        Assert.Equal(10, psc.GetTotalSum());
        AssertPrefixSums(psc, 1, 3, 6, 10);
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(3, 2, 0);
    }

    // Monaco: 'insertValues at end'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InsertValues_AtEnd(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2);
        psc.InsertValues(2, 3, 4);
        Assert.Equal(10, psc.GetTotalSum());
        Assert.Equal(6, psc.GetPrefixSum(3));
        Assert.Equal(10, psc.GetPrefixSum(4));
    }

    // Monaco: 'insertValues in the middle'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InsertValues_InTheMiddle(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 4);
        psc.InsertValues(1, 2, 3);
        Assert.Equal(10, psc.GetTotalSum());
        AssertPrefixSums(psc, 1, 3, 6, 10);
    }

    // Monaco: 'insertValues with zeroes'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InsertValues_WithZeroes(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2);
        psc.InsertValues(1, 0, 0);
        Assert.Equal(3, psc.GetTotalSum());
        AssertPrefixSums(psc, 1, 1, 1, 3);
    }

    // Monaco: 'insertValues into all-zeroes'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InsertValues_IntoAllZeroes(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 0, 0, 0);
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InsertValues_IntoEmptyComputer(bool constantTime)
    {
        var psc = Psc.Create(constantTime);
        psc.InsertValues(0, 5, 3);
        Assert.Equal(8, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(4, 0, 4);
        psc.AssertIndexOf(5, 1, 0);
    }

    // --- removeValues ---

    // Monaco: 'removeValues from beginning'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemoveValues_FromBeginning(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3, 4);
        psc.RemoveValues(0, 2);
        Assert.Equal(7, psc.GetTotalSum());
        Assert.Equal(3, psc.GetPrefixSum(1));
        Assert.Equal(7, psc.GetPrefixSum(2));
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(3, 1, 0);
    }

    // Monaco: 'removeValues from end'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemoveValues_FromEnd(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3, 4);
        psc.RemoveValues(2, 2);
        Assert.Equal(3, psc.GetTotalSum());
        Assert.Equal(1, psc.GetPrefixSum(1));
        Assert.Equal(3, psc.GetPrefixSum(2));
    }

    // Monaco: 'removeValues from the middle'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemoveValues_FromTheMiddle(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3, 4);
        psc.RemoveValues(1, 2);
        Assert.Equal(5, psc.GetTotalSum());
        Assert.Equal(1, psc.GetPrefixSum(1));
        Assert.Equal(5, psc.GetPrefixSum(2));
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(4, 1, 3);
    }

    // Monaco: 'removeValues all'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemoveValues_All(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3);
        psc.RemoveValues(0, 3);
        Assert.Equal(0, psc.GetTotalSum());
    }

    // Monaco: 'removeValues single element'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemoveValues_SingleElement(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 5, 10, 15);
        psc.RemoveValues(1, 1);
        Assert.Equal(20, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(5, 1, 0);
        psc.AssertIndexOf(19, 1, 14);
    }

    // Monaco: 'removeValues zero-valued elements'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemoveValues_ZeroValuedElements(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 0, 0, 5, 0, 0);
        psc.RemoveValues(0, 2);
        Assert.Equal(5, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(4, 0, 4);
    }

    // --- combined operations ---

    // Monaco: 'insert then remove'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InsertThenRemove(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3);
        psc.InsertValues(1, 10, 20);
        Assert.Equal(36, psc.GetTotalSum());
        psc.RemoveValues(1, 2);
        Assert.Equal(6, psc.GetTotalSum());
        psc.AssertIndexOf(0, 0, 0);
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(3, 2, 0);
    }

    // Monaco: 'remove then insert at same position'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemoveThenInsertAtSamePosition(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 2, 3);
        psc.RemoveValues(1, 1);
        psc.InsertValues(1, 5);
        Assert.Equal(9, psc.GetTotalSum());
        psc.AssertIndexOf(1, 1, 0);
        psc.AssertIndexOf(5, 1, 4);
        psc.AssertIndexOf(6, 2, 0);
    }

    // Monaco: 'setValue then insert then remove'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetValueThenInsertThenRemove(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 1, 1);
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultipleQueriesBetweenMutations_AreConsistent(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 2, 3, 5);
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LargeValues(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 100, 200, 300);
        Assert.Equal(600, psc.GetTotalSum());
        AssertPrefixSums(psc, 100, 300, 600);
        (int Sum, int Index, int Remainder)[] expected = [(0, 0, 0), (99, 0, 99), (100, 1, 0), (299, 1, 199), (300, 2, 0), (599, 2, 299)];
        foreach ((int sum, int index, int remainder) in expected)
        {
            psc.AssertIndexOf(sum, index, remainder);
        }
    }

    // Monaco: 'many elements'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ManyElements(bool constantTime)
    {
        var psc = Psc.Create(constantTime, [.. Enumerable.Repeat(1, 100)]);
        Assert.Equal(100, psc.GetTotalSum());
        Assert.Equal(50, psc.GetPrefixSum(50));
        for (int i = 0; i < 100; i++)
        {
            psc.AssertIndexOf(i, i, 0);
        }
    }

    // Monaco: 'many elements all zeroes'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ManyElements_AllZeroes(bool constantTime)
    {
        var psc = Psc.Create(constantTime, [.. Enumerable.Repeat(0, 100)]);
        Assert.Equal(0, psc.GetTotalSum());
        for (int i = 0; i <= 100; i++)
        {
            Assert.Equal(0, psc.GetPrefixSum(i));
        }

        psc.AssertIndexOf(0, 99, 0);
    }

    // Monaco: 'setValue between queries re-validates correctly'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetValue_BetweenQueries_RevalidatesCorrectly(bool constantTime)
    {
        var psc = Psc.Create(constantTime, 1, 1, 1, 1, 1);
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
