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
}
