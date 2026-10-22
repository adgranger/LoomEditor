using LoomEditor.Core.Decorations;
using LoomEditor.Core.Tokens;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of injected text (it renders HTML): all tests are new.
public class TextViewRendererInjectedTextTests
{
    private const string ThemeJson = """{ "colors": { "editor.findMatchForeground": "#ff00ff" } }""";

    // "hello" with "XYZ" injected after "he": the drawn line is "heXYZllo".
    private static ViewHarness CreateHarness()
    {
        var harness = new ViewHarness("hello");
        harness.Renderer.Theme = EditorTheme.Load(ThemeJson, "vs-dark", "Test");
        ModelDecorationOptions options = ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
        {
            ShowIfCollapsed = true,
            Description = "test",
            After = new InjectedTextOptions { Content = "XYZ", InlineClassName = "findMatchInline" },
        });
        harness.Model.DeltaDecorations(harness.ViewModel.EditorId, [], new[] { new ModelDeltaDecoration(new Range(1, 3, 1, 3), options) });
        return harness;
    }

    [Fact]
    public void InjectedText_IsDrawnWithItsInlineClassForeground()
    {
        using ViewHarness harness = CreateHarness();

        harness.Render();

        FakeLineLayout layout = harness.Measurer.Layouts.Last(l => l.Text == "heXYZllo");
        uint magenta = LineStyle.ToArgb(harness.Renderer.Theme.GetColor("editor.findMatchForeground")!.Value);
        Assert.Contains(new StyleRun(2, 3, magenta, FontStyle.None), layout.Style.Runs);
    }

    [Fact]
    public void InjectedText_ShiftsTheFollowingText()
    {
        using ViewHarness harness = CreateHarness();

        RecordingRenderSurface surface = harness.Render();

        // The model text after the injection is part of the same drawn line, so it starts after the injected text.
        Assert.Single(surface.Calls.OfType<DrawLineLayoutCall>(), c => c.Text == "heXYZllo");
        FakeLineLayout layout = harness.Measurer.Layouts.Last(l => l.Text == "heXYZllo");
        Assert.Equal(5 * FakeTextMeasurer.CharacterWidth, layout.GetCharacterX(5));
    }
}
