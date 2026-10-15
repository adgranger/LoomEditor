// Ported from microsoft/vscode: src/vs/editor/test/common/model/intervalTree.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;

namespace LoomEditor.Core.Tests.Decorations.TestHelpers;

/// <summary>One step of an interval tree test: <c>insert</c>, <c>delete</c>, <c>change</c> or <c>search</c>.</summary>
internal readonly record struct IntervalTreeOperation(string Type, long Begin = 0, long End = 0, int Id = 0);

/// <summary>Monaco's <c>Interval</c> test class.</summary>
internal sealed class TestInterval(long start, long end)
{
    public long Start { get; set; } = start;

    public long End { get; set; } = end;

    public (long, long) Value => (Start, End);
}

/// <summary>Monaco's <c>Oracle</c> test class: a sorted list of intervals.</summary>
internal sealed class IntervalOracle
{
    public List<TestInterval> Intervals { get; } = [];

    public TestInterval Insert(TestInterval interval)
    {
        Intervals.Add(interval);

        // JS sort is stable; LINQ OrderBy is stable too.
        var sorted = Intervals
            .OrderBy(i => i.Start)
            .ThenBy(i => i.End)
            .ToList();
        Intervals.Clear();
        Intervals.AddRange(sorted);
        return interval;
    }

    public void Delete(TestInterval interval)
    {
        for (var i = 0; i < Intervals.Count; i++)
        {
            if (ReferenceEquals(Intervals[i], interval))
            {
                Intervals.RemoveAt(i);
                return;
            }
        }
    }

    public List<TestInterval> Search(TestInterval interval) =>
        Intervals.Where(i => i.Start <= interval.End && i.End >= interval.Start).ToList();
}

/// <summary>Monaco's <c>TestState</c> test class: applies operations to the tree and to the oracle and compares them.</summary>
internal sealed class IntervalTreeTestState
{
    private readonly IntervalOracle _oracle = new();
    private readonly IntervalTree _tree = new();
    private readonly List<IntervalNode?> _treeNodes = [];
    private readonly List<TestInterval?> _oracleNodes = [];

    public void AcceptOp(IntervalTreeOperation op)
    {
        if (op.Type == "insert")
        {
            var node = new IntervalNode(string.Empty, op.Begin, op.End);
            _treeNodes.Add(node);
            _tree.Insert(node);
            _oracleNodes.Add(_oracle.Insert(new TestInterval(op.Begin, op.End)));
        }
        else if (op.Type == "delete")
        {
            _tree.Delete(_treeNodes[op.Id]!);
            _oracle.Delete(_oracleNodes[op.Id]!);

            _treeNodes[op.Id] = null;
            _oracleNodes[op.Id] = null;
        }
        else if (op.Type == "change")
        {
            _tree.Delete(_treeNodes[op.Id]!);
            _treeNodes[op.Id]!.Reset(0, op.Begin, op.End, null!);
            _tree.Insert(_treeNodes[op.Id]!);

            _oracle.Delete(_oracleNodes[op.Id]!);
            _oracleNodes[op.Id]!.Start = op.Begin;
            _oracleNodes[op.Id]!.End = op.End;
            _oracle.Insert(_oracleNodes[op.Id]!);
        }
        else
        {
            var actualNodes = _tree.IntervalSearch(op.Begin, op.End, 0, false, false, 0, false);
            var actualSearch = actualNodes.Select(n => (n.CachedAbsoluteStart, n.CachedAbsoluteEnd)).ToList();
            var expectedSearch = _oracle.Search(new TestInterval(op.Begin, op.End)).Select(i => i.Value).ToList();
            Assert.Equal(expectedSearch, actualSearch);
            return;
        }

        IntervalTreeInvariants.AssertTreeInvariants(_tree);

        var actual = _tree.GetAllInOrder().Select(n => (n.CachedAbsoluteStart, n.CachedAbsoluteEnd)).ToList();
        var expected = _oracle.Intervals.Select(i => i.Value).ToList();
        Assert.Equal(expected, actual);
    }

    public static void Run(params IntervalTreeOperation[] ops)
    {
        var state = new IntervalTreeTestState();
        foreach (var op in ops)
        {
            state.AcceptOp(op);
        }
    }
}
