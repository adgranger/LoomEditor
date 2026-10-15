using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;

namespace LoomEditor.Rendering.Tests;

// Monaco: `viewLines.ts` `onConfigurationChanged` has no unit test, all tests are new.
public class TextViewRendererFontTests
{
    [Fact]
    public void OnFontChanged_AfterAFrame_DisposesTheLayoutsCreatedBefore()
    {
        using var harness = new ViewHarness("L1\nL2");
        harness.Render();
        List<FakeLineLayout> before = [.. harness.Measurer.Layouts];

        using var newMeasurer = new FakeTextMeasurer();
        harness.Renderer.OnFontChanged(newMeasurer);

        Assert.NotEmpty(before);
        Assert.All(before, layout => Assert.True(layout.IsDisposed));
    }

    [Fact]
    public void Render_AfterOnFontChanged_LaysOutLinesWithTheNewMeasurer()
    {
        using var harness = new ViewHarness("L1\nL2");
        harness.Render();
        using var newMeasurer = new FakeTextMeasurer();

        harness.Renderer.OnFontChanged(newMeasurer);
        harness.Render();

        Assert.NotEmpty(newMeasurer.Layouts);
    }

    [Fact]
    public void Render_AfterOnFontChanged_MeasuresTheWidestLineAgain()
    {
        string text = string.Join('\n', Enumerable.Range(1, 1000).Select(i => i == 3 ? "L" + new string('x', 59) : "L"));
        using var harness = new ViewHarness(text, height: 200);
        harness.Render();
        int wideContentWidth = harness.ViewModel.ViewLayout.GetContentWidth();
        harness.Model.ApplyEdits(new[] { new TextEdit(new Range(3, 6, 3, 61), string.Empty) });
        harness.Render();
        Assert.Equal(wideContentWidth, harness.ViewModel.ViewLayout.GetContentWidth()); // it only grows

        using var newMeasurer = new FakeTextMeasurer();
        harness.Renderer.OnFontChanged(newMeasurer);
        harness.Render();

        Assert.True(harness.ViewModel.ViewLayout.GetContentWidth() < wideContentWidth);
    }
}
