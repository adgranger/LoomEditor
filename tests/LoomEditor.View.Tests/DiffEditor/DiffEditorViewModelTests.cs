using LoomEditor.Core.Diff;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Tests.DiffEditor.TestHelpers;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.DiffEditor;

public class DiffEditorViewModelTests
{
    private sealed class Fixture : IDisposable
    {
        public Fixture(int lines = 30, DocumentDiff? diff = null, bool hide = false)
        {
            Text = string.Join('\n', Enumerable.Range(1, lines).Select(i => "line " + i));
            Original = new TextModel(Text);
            Modified = new TextModel(Text);
            Options = new DiffEditorOptions();
            if (hide)
            {
                Options.HideUnchangedRegions = new HideUnchangedRegionsOptions(true, 3, 3, 20);
            }

            Provider = new FakeDocumentDiffProvider { Immediate = diff ?? Change(15, 16) };
            Scheduler = new ManualEditorScheduler();
            Vm = new DiffEditorViewModel(Original, Modified, Options, Provider, InlineUiDispatcher.Instance, Scheduler);
        }

        public string Text { get; }

        public TextModel Original { get; }

        public TextModel Modified { get; }

        public DiffEditorOptions Options { get; }

        public FakeDocumentDiffProvider Provider { get; }

        public ManualEditorScheduler Scheduler { get; }

        public DiffEditorViewModel Vm { get; }

        public void EditModified(int line = 1) => Modified.ApplyEdits([new TextEdit(new Range(line, 1, line, 1), "x")]);

        public void Dispose() => Vm.Dispose();
    }

    private static DocumentDiff Change(int start, int end) =>
        new(false, false, [new DetailedLineRangeMapping(new LineRange(start, end), new LineRange(start, end), null)], []);

    [Fact]
    public void Start_ComputesDiffImmediately()
    {
        using var f = new Fixture();

        Assert.Single(f.Provider.Calls);
        Assert.True(f.Vm.IsDiffUpToDate);
        Assert.Single(f.Vm.Diff!.Mappings);
    }

    [Fact]
    public void Edit_MarksNotUpToDate_ThenRecomputesAfter200ms()
    {
        using var f = new Fixture();
        var changes = 0;
        f.Vm.DiffChanged += (_, _) => changes++;

        f.EditModified();
        Assert.False(f.Vm.IsDiffUpToDate);
        Assert.Equal(1, changes);
        Assert.NotNull(f.Vm.Diff);

        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(199));
        Assert.Single(f.Provider.Calls);
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));

        Assert.Equal(2, f.Provider.Calls.Count);
        Assert.True(f.Vm.IsDiffUpToDate);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void Edit_TwiceQuickly_OneRecompute()
    {
        using var f = new Fixture();

        f.EditModified();
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(150));
        f.EditModified();
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(150));
        Assert.Single(f.Provider.Calls);
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100));

        Assert.Equal(2, f.Provider.Calls.Count);
    }

    [Fact]
    public void Edit_WhileComputing_CancelsOldResult()
    {
        using var f = new Fixture();
        f.Provider.Immediate = null;
        f.EditModified();
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(200));
        f.EditModified();
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(200));
        var (old, current) = (f.Provider.Calls[1], f.Provider.Calls[2]);

        Assert.True(old.Token.IsCancellationRequested);
        old.Result.SetResult(Change(1, 2));
        Assert.False(f.Vm.IsDiffUpToDate);
        Assert.Equal(15, f.Vm.Diff!.Mappings[0].LineRangeMapping.Original.StartLineNumber);

        current.Result.SetResult(Change(5, 6));
        Assert.True(f.Vm.IsDiffUpToDate);
        Assert.Equal(5, f.Vm.Diff!.Mappings[0].LineRangeMapping.Original.StartLineNumber);
    }

    [Fact]
    public void ProviderThrows_KeepsOldState()
    {
        using var f = new Fixture();
        var old = f.Vm.Diff;
        f.Provider.Throw = new InvalidOperationException("boom");

        f.EditModified();
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(200));

        Assert.Same(old, f.Vm.Diff);
        Assert.False(f.Vm.IsDiffUpToDate);
    }

    [Fact]
    public void OptionChange_IgnoreTrimWhitespace_Recomputes()
    {
        using var f = new Fixture();

        f.Options.IgnoreTrimWhitespace = false;

        Assert.Equal(2, f.Provider.Calls.Count);
        Assert.False(f.Provider.Calls[1].Options.IgnoreTrimWhitespace);
    }

    [Fact]
    public void UnchangedRegions_Disabled_Empty()
    {
        using var f = new Fixture();

        Assert.Empty(f.Vm.UnchangedRegions);
    }

    [Fact]
    public void UnchangedRegions_Enabled_FromDiff()
    {
        using var f = new Fixture(hide: true);

        Assert.Collection(
            f.Vm.UnchangedRegions,
            r => Assert.Equal((1, 1, 11), (r.OriginalLineNumber, r.ModifiedLineNumber, r.LineCount)),
            r => Assert.Equal((19, 19, 12), (r.OriginalLineNumber, r.ModifiedLineNumber, r.LineCount)));
    }

    [Fact]
    public void UnchangedRegions_VisibleCountsSurviveRecompute()
    {
        using var f = new Fixture(hide: true);
        f.Vm.UnchangedRegions[0].ShowMoreAbove(4);

        f.EditModified(25);
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(200));

        var first = f.Vm.UnchangedRegions[0];
        Assert.Equal(4, first.VisibleLineCountTop);
        Assert.Equal(0, first.VisibleLineCountBottom);
    }

    [Fact]
    public void UnchangedRegions_TouchingRegionsMergedAfterEdit()
    {
        using var f = new Fixture(hide: true);
        // Deleting the changed lines between the two collapsed regions makes their hidden ranges touch.
        f.Original.ApplyEdits([new TextEdit(new Range(12, 1, 19, 1), "")]);
        f.Modified.ApplyEdits([new TextEdit(new Range(12, 1, 19, 1), "")]);

        var merged = Assert.Single(f.Vm.UnchangedRegions);
        Assert.Equal((1, 1, 23), (merged.OriginalLineNumber, merged.ModifiedLineNumber, merged.LineCount));
    }

    [Fact]
    public void UnchangedRegions_FollowEditsAbove()
    {
        using var f = new Fixture(hide: true);
        f.Vm.UnchangedRegions[1].ShowMoreBelow(5);

        // Both models get two lines on top: the decorations follow, so the counts carry over to the shifted region.
        f.Original.ApplyEdits([new TextEdit(new Range(1, 1, 1, 1), "a\nb\n")]);
        f.Modified.ApplyEdits([new TextEdit(new Range(1, 1, 1, 1), "a\nb\n")]);
        f.Provider.Immediate = Change(17, 18);
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(200));

        var region = f.Vm.UnchangedRegions[1];
        Assert.Equal(21, region.ModifiedLineNumber);
        Assert.Equal(5, region.VisibleLineCountBottom);
    }

    [Fact]
    public void EnsureModifiedLineIsVisible_InsideRegion_ShowsLine()
    {
        using var f = new Fixture(hide: true);

        f.Vm.EnsureModifiedLineIsVisible(2, RevealPreference.FromTop);

        Assert.True(f.Vm.UnchangedRegions[0].VisibleLineCountTop >= 2);
    }

    [Fact]
    public void EnsureModifiedLineIsVisible_OutsideRegion_NoChange()
    {
        using var f = new Fixture(hide: true);

        f.Vm.EnsureModifiedLineIsVisible(15, RevealPreference.FromTop);

        Assert.All(f.Vm.UnchangedRegions, r => Assert.Equal(0, r.VisibleLineCountTop + r.VisibleLineCountBottom));
    }

    [Fact]
    public void EnsureOriginalLineIsVisible_InsideRegion_ShowsLine()
    {
        using var f = new Fixture(hide: true);

        f.Vm.EnsureOriginalLineIsVisible(2, RevealPreference.FromTop);

        Assert.True(f.Vm.UnchangedRegions[0].VisibleLineCountTop > 0);
    }

    [Fact]
    public void EnsureOriginalLineIsVisible_OutsideRegion_NoChange()
    {
        using var f = new Fixture(hide: true);

        f.Vm.EnsureOriginalLineIsVisible(15, RevealPreference.FromTop);

        Assert.All(f.Vm.UnchangedRegions, r => Assert.Equal(0, r.VisibleLineCountTop + r.VisibleLineCountBottom));
    }

    [Fact]
    public async Task WaitForDiffAsync_CompletesWhenUpToDate()
    {
        using var f = new Fixture();
        f.Provider.Immediate = null;
        f.EditModified();
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(200));

        var wait = f.Vm.WaitForDiffAsync(TestContext.Current.CancellationToken);
        Assert.False(wait.IsCompleted);
        f.Provider.Calls[^1].Result.SetResult(Change(1, 2));

        await wait.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Dispose_CancelsAndStopsEvents()
    {
        var f = new Fixture(hide: true);
        f.Provider.Immediate = null;
        f.EditModified();
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(200));
        var pending = f.Provider.Calls[^1];
        var changes = 0;
        f.Vm.DiffChanged += (_, _) => changes++;

        f.Vm.Dispose();
        f.EditModified();
        f.Scheduler.AdvanceBy(TimeSpan.FromMilliseconds(500));
        pending.Result.SetResult(Change(1, 2));

        Assert.True(pending.Token.IsCancellationRequested);
        Assert.Equal(0, changes);
        Assert.Equal(2, f.Provider.Calls.Count);
        Assert.Empty(f.Vm.UnchangedRegions);
    }
}
