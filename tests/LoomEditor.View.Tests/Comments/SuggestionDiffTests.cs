using LoomEditor.View.Comments;

namespace LoomEditor.View.Tests.Comments;

public class SuggestionDiffTests
{
    private static SuggestionDiffResult Compute(string old, string text) => SuggestionDiff.Compute(old, new SuggestionBlock(0, text));

    private static (SuggestionDiffRowKind, string)[] Rows(SuggestionDiffResult r) => [.. r.Rows.Select(x => (x.Kind, x.Text))];

    [Fact]
    public void Compute_OneNewLine_RemovedThenAdded()
    {
        var r = Compute("a", "b");
        Assert.Equal([(SuggestionDiffRowKind.Removed, "a"), (SuggestionDiffRowKind.Added, "b")], Rows(r));
        Assert.False(r.IsNoOp);
        Assert.False(r.IsDeletion);
    }

    [Fact]
    public void Compute_ManyNewLines_RemovedThenAllAdded() =>
        Assert.Equal(
            [(SuggestionDiffRowKind.Removed, "a"), (SuggestionDiffRowKind.Added, "b"), (SuggestionDiffRowKind.Added, "c")],
            Rows(Compute("a", "b\nc")));

    [Fact]
    public void Compute_Deletion_OnlyRemoved()
    {
        var r = Compute("a", "");
        Assert.Equal([(SuggestionDiffRowKind.Removed, "a")], Rows(r));
        Assert.True(r.IsDeletion);
    }

    [Fact]
    public void Compute_SameLine_IsNoOp()
    {
        var r = Compute("a", "a");
        Assert.Equal([(SuggestionDiffRowKind.Context, "a")], Rows(r));
        Assert.True(r.IsNoOp);
    }

    [Fact]
    public void Compute_NewLineAroundOld_KeepsContext() =>
        Assert.Equal(
            [(SuggestionDiffRowKind.Added, "x"), (SuggestionDiffRowKind.Context, "a"), (SuggestionDiffRowKind.Added, "y")],
            Rows(Compute("a", "x\na\ny")));

    [Fact]
    public void Compute_WhitespaceOnlyChange_IsNotNoOp()
    {
        var r = Compute("a", "a ");
        Assert.False(r.IsNoOp);
        Assert.Equal([(SuggestionDiffRowKind.Removed, "a"), (SuggestionDiffRowKind.Added, "a ")], Rows(r));
    }

    [Fact]
    public void Describe_Replacement() => Assert.Equal("Removed: a. Added: b; c.", Compute("a", "b\nc").Describe());

    [Fact]
    public void Describe_Deletion() => Assert.Equal("Removed: a. Line deleted.", Compute("a", "").Describe());

    [Fact]
    public void Describe_NoOp() => Assert.Equal("No changes.", Compute("a", "a").Describe());

    [Fact]
    public void Describe_EmptyLine_SaysEmpty() => Assert.Equal("Removed: (empty). Added: b.", Compute("", "b").Describe());

    [Fact]
    public void Marker_MatchesKind()
    {
        Assert.Equal(' ', new SuggestionDiffRow(SuggestionDiffRowKind.Context, "").Marker);
        Assert.Equal('-', new SuggestionDiffRow(SuggestionDiffRowKind.Removed, "").Marker);
        Assert.Equal('+', new SuggestionDiffRow(SuggestionDiffRowKind.Added, "").Marker);
    }

    private static SuggestionDiffResult ComputeRange(string[] old, string text) => SuggestionDiff.Compute(old, new SuggestionBlock(0, text));

    [Fact]
    public void Compute_ThreeLinesOneChanged_ContextRemovedAddedContext() =>
        Assert.Equal(
            [
                (SuggestionDiffRowKind.Context, "a"), (SuggestionDiffRowKind.Removed, "b"),
                (SuggestionDiffRowKind.Added, "X"), (SuggestionDiffRowKind.Context, "c"),
            ],
            Rows(ComputeRange(["a", "b", "c"], "a\nX\nc")));

    [Fact]
    public void Compute_RangeDeletion_AllRemovedAndIsDeletion()
    {
        var r = ComputeRange(["a", "b"], "");
        Assert.Equal([(SuggestionDiffRowKind.Removed, "a"), (SuggestionDiffRowKind.Removed, "b")], Rows(r));
        Assert.True(r.IsDeletion);
        Assert.False(r.IsNoOp);
    }

    [Fact]
    public void Compute_RangeIdentical_IsNoOpAllContext()
    {
        var r = ComputeRange(["a", "b"], "a\nb");
        Assert.Equal([(SuggestionDiffRowKind.Context, "a"), (SuggestionDiffRowKind.Context, "b")], Rows(r));
        Assert.True(r.IsNoOp);
    }

    [Fact]
    public void Compute_LinesInserted_AddedBetweenContext() =>
        Assert.Equal(
            [(SuggestionDiffRowKind.Context, "a"), (SuggestionDiffRowKind.Added, "b"), (SuggestionDiffRowKind.Context, "c")],
            Rows(ComputeRange(["a", "c"], "a\nb\nc")));

    [Fact]
    public void Compute_AllDifferent_RemovedThenAdded() =>
        Assert.Equal(
            [
                (SuggestionDiffRowKind.Removed, "a"), (SuggestionDiffRowKind.Removed, "b"),
                (SuggestionDiffRowKind.Added, "x"), (SuggestionDiffRowKind.Added, "y"),
            ],
            Rows(ComputeRange(["a", "b"], "x\ny")));

    [Theory]
    [InlineData("a", "b")]
    [InlineData("a", "")]
    [InlineData("a", "b\na\nc")]
    [InlineData("a", "a\na")]
    public void Compute_SingleOldLine_SameRowsAsPhase11(string old, string text)
    {
        var one = SuggestionDiff.Compute(old, new SuggestionBlock(0, text));
        var list = ComputeRange([old], text);
        Assert.Equal(Rows(one), Rows(list));
        Assert.Equal(one.IsNoOp, list.IsNoOp);
        Assert.Equal(one.IsDeletion, list.IsDeletion);
    }

    [Fact]
    public void Compute_SingleOldLine_FirstEqualLineIsContext() =>
        Assert.Equal(
            [(SuggestionDiffRowKind.Added, "b"), (SuggestionDiffRowKind.Context, "a"), (SuggestionDiffRowKind.Added, "a")],
            Rows(ComputeRange(["a"], "b\na\na")));

    [Fact]
    public void Compute_HugeInput_FallsBackWithoutContext()
    {
        string[] old = [.. Enumerable.Range(0, 600).Select(i => "l" + i)];
        var r = ComputeRange(old, string.Join('\n', old.Select(l => l + "x")));
        Assert.DoesNotContain(r.Rows, x => x.Kind == SuggestionDiffRowKind.Context);
        Assert.Equal(1200, r.Rows.Count);
        Assert.False(r.IsNoOp);
    }

    [Fact]
    public void Compute_EmptyOldLines_Throws() =>
        Assert.Throws<ArgumentException>(() => ComputeRange([], "a"));

    [Fact]
    public void Describe_RangeDeletion_SaysLinesDeleted() =>
        Assert.EndsWith("Lines deleted.", ComputeRange(["a", "b"], "").Describe());
}
