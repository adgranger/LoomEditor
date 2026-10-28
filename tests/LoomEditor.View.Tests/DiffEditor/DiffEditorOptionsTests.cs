using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public class DiffEditorOptionsTests
{
    [Fact]
    public void Defaults_MatchMonaco()
    {
        var o = new DiffEditorOptions();
        Assert.True(o.EnableSplitViewResizing);
        Assert.Equal(0.5, o.SplitViewDefaultRatio);
        Assert.True(o.RenderSideBySide);
        Assert.Equal(5000, o.MaxComputationTimeMs);
        Assert.True(o.IgnoreTrimWhitespace);
        Assert.True(o.RenderIndicators);
        Assert.False(o.OriginalEditable);
        Assert.False(o.ReadOnly);
        Assert.True(o.RenderOverviewRuler);
        Assert.False(o.ShowMoves);
        Assert.True(o.ShowEmptyDecorations);
        Assert.False(o.HideOriginalLineNumbers);
        Assert.Equal(900, o.RenderSideBySideInlineBreakpoint);
        Assert.True(o.UseInlineViewWhenSpaceIsLimited);
        Assert.Equal(new HideUnchangedRegionsOptions(false, 3, 3, 20), o.HideUnchangedRegions);
        Assert.False(o.IsScreenReaderOptimized);
    }

    [Fact]
    public void SplitViewDefaultRatio_Clamped_0_1_To_0_9()
    {
        var o = new DiffEditorOptions { SplitViewDefaultRatio = 0.01 };
        Assert.Equal(0.1, o.SplitViewDefaultRatio);
        o.SplitViewDefaultRatio = 5;
        Assert.Equal(0.9, o.SplitViewDefaultRatio);
    }

    [Fact]
    public void NegativeCounts_ClampedToZero()
    {
        var o = new DiffEditorOptions
        {
            MaxComputationTimeMs = -1,
            RenderSideBySideInlineBreakpoint = -5,
            HideUnchangedRegions = new HideUnchangedRegionsOptions(true, -1, -2, -3),
        };
        Assert.Equal(0, o.MaxComputationTimeMs);
        Assert.Equal(0, o.RenderSideBySideInlineBreakpoint);
        Assert.Equal(new HideUnchangedRegionsOptions(true, 0, 0, 0), o.HideUnchangedRegions);
    }

    [Fact]
    public void Changed_RaisedOnlyWhenValueChanges()
    {
        var o = new DiffEditorOptions();
        int count = 0;
        o.Changed += (_, _) => count++;
        o.ReadOnly = false;
        o.SplitViewDefaultRatio = 0.5;
        Assert.Equal(0, count);
        o.ReadOnly = true;
        Assert.Equal(1, count);
    }

    [Fact]
    public void Changed_CarriesTheChangedNames()
    {
        var o = new DiffEditorOptions();
        IReadOnlySet<string>? names = null;
        o.Changed += (_, e) => names = e.ChangedOptions;
        o.IgnoreTrimWhitespace = false;
        Assert.Equal([nameof(DiffEditorOptions.IgnoreTrimWhitespace)], names);
    }

    [Fact]
    public void RenderSideBySideEffective_WideEditor_True()
    {
        var o = new DiffEditorOptions();
        o.SetWidth(1000);
        Assert.True(o.RenderSideBySideEffective);
        Assert.True(o.RenderSideBySideInAutomaticMode);
    }

    [Fact]
    public void RenderSideBySideEffective_NarrowEditor_False_WhenUseInlineViewWhenSpaceIsLimited()
    {
        var o = new DiffEditorOptions();
        o.SetWidth(500);
        Assert.False(o.RenderSideBySideEffective);
        Assert.True(o.CouldShowInlineViewBecauseOfSize);
    }

    [Fact]
    public void RenderSideBySideEffective_NarrowEditor_True_WhenOptionOff()
    {
        var o = new DiffEditorOptions { UseInlineViewWhenSpaceIsLimited = false };
        o.SetWidth(500);
        Assert.True(o.RenderSideBySideEffective);
    }

    [Fact]
    public void RenderSideBySideEffective_ScreenReader_StaysSideBySide()
    {
        var o = new DiffEditorOptions { IsScreenReaderOptimized = true };
        o.SetWidth(500);
        Assert.True(o.RenderSideBySideEffective);
        Assert.True(o.RenderSideBySideInAutomaticMode);
    }

    [Fact]
    public void RenderSideBySideInlineOption_False_AlwaysInline()
    {
        var o = new DiffEditorOptions { RenderSideBySide = false };
        o.SetWidth(2000);
        Assert.False(o.RenderSideBySideEffective);
        Assert.False(o.ShowMovesEffective);
    }

    [Fact]
    public void SetWidth_RaisesChangedWhenEffectiveValueFlips()
    {
        var o = new DiffEditorOptions();
        o.SetWidth(1000);
        var raised = new List<IReadOnlySet<string>>();
        o.Changed += (_, e) => raised.Add(e.ChangedOptions);
        o.SetWidth(1200);
        Assert.Empty(raised);
        o.SetWidth(500);
        Assert.Single(raised);
        Assert.Contains(nameof(DiffEditorOptions.RenderSideBySideEffective), raised[0]);
    }

    [Fact]
    public void HideUnchangedRegions_Replace_RaisesChanged()
    {
        var o = new DiffEditorOptions();
        IReadOnlySet<string>? names = null;
        o.Changed += (_, e) => names = e.ChangedOptions;
        o.HideUnchangedRegions = new HideUnchangedRegionsOptions(Enabled: true);
        Assert.Equal([nameof(DiffEditorOptions.HideUnchangedRegions)], names);
    }
}
