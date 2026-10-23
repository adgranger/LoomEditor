using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.View.Controller;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of view lines (it renders HTML): all tests are new.
public class TextViewRendererWordWrapTests
{
    // At wrap column 10 the first line wraps in 3 view lines, the 2-space indent being kept on the continuations.
    private const string Text = "  aaa bbb ccc ddd\nshort\nx";

    private static ViewHarness Wrapped(string text = Text)
    {
        var harness = new ViewHarness(text);
        harness.ViewModel.SetOptions(
            harness.ViewModel.LayoutOptions with
            {
                Wrapping = new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 10 },
            },
            harness.ViewModel.ViewLayoutOptions);
        return harness;
    }

    private static List<DrawLineLayoutCall> Draws(RecordingRenderSurface surface, Func<string, bool> filter) =>
        [.. surface.Calls.OfType<DrawLineLayoutCall>().Where(call => call.Text.Length > 0 && filter(call.Text))];

    [Fact]
    public void WrappedLine_DrawsEveryViewLine()
    {
        using ViewHarness harness = Wrapped();

        List<DrawLineLayoutCall> texts = Draws(harness.Render(), text => !char.IsDigit(text[0]));

        Assert.Equal((string[])["  aaa bbb ", "  ccc ddd", "short", "x"], texts.Select(call => call.Text));
        Assert.Equal((float[])[0f, 20f, 40f, 60f], texts.Select(call => call.LineTop));
    }

    [Fact]
    public void WrappedLine_LineNumberOnlyOnTheFirstViewLine()
    {
        using ViewHarness harness = Wrapped();

        List<DrawLineLayoutCall> numbers = Draws(harness.Render(), text => char.IsDigit(text[0]));

        Assert.Equal((string[])["1", "2", "3"], numbers.Select(call => call.Text));
        Assert.Equal((float[])[0f, 40f, 60f], numbers.Select(call => call.LineTop));
    }

    private static FoldingModel AddRegion(ViewHarness harness, bool collapse)
    {
        var provider = new FoldingDecorationProvider(harness.ViewModel) { ShowFoldingControls = ShowFoldingControls.Always };
        var foldingModel = new FoldingModel(harness.Model, provider);
        foldingModel.Update(FoldingRegions.FromFoldRanges((FoldRange[])
            [new FoldRange { StartLineNumber = 1, EndLineNumber = 2, Source = FoldSource.Provider }]));
        if (collapse)
        {
            foldingModel.ToggleCollapseState(new[] { foldingModel.GetRegionAtLine(1)! });
        }

        return foldingModel;
    }

    [Fact]
    public void WrappedLine_FoldingChevronOnlyOnTheFirstViewLine()
    {
        using ViewHarness harness = Wrapped();
        AddRegion(harness, collapse: false);

        List<DrawLineCall> lines = [.. harness.Render().Calls.OfType<DrawLineCall>()];

        // One chevron (two strokes), centred on the first view line.
        Assert.Equal(2, lines.Count);
        Assert.All(lines, line => Assert.InRange(line.Y1, 0, 20));
    }

    [Fact]
    public void FoldingAWrappedRegion_HidesAllItsViewLines()
    {
        using ViewHarness harness = Wrapped();
        harness.ViewModel.SetHiddenAreas(new[] { new Range(2, 1, 2, 6) });

        List<DrawLineLayoutCall> texts = Draws(harness.Render(), text => !char.IsDigit(text[0]));

        // Model line 1 (3 view lines) stays, model line 2 is hidden, "x" moves up.
        Assert.Equal((string[])["  aaa bbb ", "  ccc ddd", "x"], texts.Select(call => call.Text));
        Assert.Equal(3, harness.ViewModel.GetLineCount());
    }

    [Fact]
    public void HitTest_OnAContinuationLine_ReturnsItsViewPosition()
    {
        using ViewHarness harness = Wrapped();
        ILineHitTester tester = harness.Renderer.CreateLineHitTester(harness.ViewModel);
        var layout = new FakeLineLayout("  ccc ddd", FakeTextMeasurer.CharacterWidth, 4);

        Position? position = tester.GetPositionAtCoordinate(2, layout.GetCharacterX(5));

        Assert.Equal(new Position(2, 6), position);
        Assert.Equal(layout.Width, tester.GetLineWidth(2));
    }

    [Fact]
    public void SelectionAcrossAWrapSeam_HighlightsBothViewLines()
    {
        using ViewHarness harness = Wrapped();

        // View selection from the end of view line 1 to the start of the text of view line 2.
        RecordingRenderSurface surface = harness.Render(new Selection(1, 8, 2, 4));

        List<FillRectangleCall> fills = [.. surface.Calls.OfType<FillRectangleCall>().Where(call => call.X >= harness.LayoutInfo.ContentLeft)];
        Assert.Contains(fills, call => call.Y == 0);
        Assert.Contains(fills, call => call.Y == 20);
    }
}
