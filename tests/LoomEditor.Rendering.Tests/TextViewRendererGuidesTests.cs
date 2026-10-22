using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the indent guides overlay (it renders HTML): all tests are new.
public class TextViewRendererGuidesTests
{
    private const float CharacterWidth = (float)FakeTextMeasurer.CharacterWidth;
    private const float LineHeight = FakeTextMeasurer.LineHeight;

    private const string ThemeJson = """
        {
          "colors": {
            "editorIndentGuide.background1": "#111111",
            "editorIndentGuide.activeBackground1": "#eeeeee",
            "editorBracketPairGuide.background1": "#220000",
            "editorBracketPairGuide.activeBackground1": "#ff0000"
          }
        }
        """;

    private static readonly ThemeColor IndentColor = ThemeColor.Parse("#111111");
    private static readonly ThemeColor ActiveIndentColor = ThemeColor.Parse("#eeeeee");

    private static ViewHarness CreateHarness(string text, EditorGuidesOptions? guides = null, int width = 500)
    {
        var harness = new ViewHarness(text, width: width);
        harness.Renderer.Theme = EditorTheme.Load(ThemeJson, "vs-dark", "Test");
        if (guides is not null)
        {
            harness.ViewModel.SetOptions(harness.ViewModel.LayoutOptions with { Guides = guides }, harness.ViewModel.ViewLayoutOptions);
        }

        return harness;
    }

    private static List<FillRectangleCall> VerticalGuides(RecordingRenderSurface surface, params ThemeColor[] colors) =>
        [.. surface.Calls.OfType<FillRectangleCall>().Where(call => call.Width == 1 && call.Height == LineHeight && colors.Contains(call.Color))];

    [Fact]
    public void IndentGuides_DrawnAtEachLevel()
    {
        // Indent size 4: guides at visible columns 1 and 5 of the third line.
        using ViewHarness harness = CreateHarness("a\n    b\n        c");

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(1, 1)));

        float left = harness.LayoutInfo.ContentLeft;
        List<FillRectangleCall> guides = VerticalGuides(surface, IndentColor, ActiveIndentColor);
        // The guide of the cursor's block (level 1 of the lines 2 and 3) is the active one.
        Assert.Contains(new FillRectangleCall(left, LineHeight, 1, LineHeight, ActiveIndentColor), guides);
        Assert.Contains(new FillRectangleCall(left, 2 * LineHeight, 1, LineHeight, ActiveIndentColor), guides);
        Assert.Contains(new FillRectangleCall(left + (4 * CharacterWidth), 2 * LineHeight, 1, LineHeight, IndentColor), guides);
    }

    [Fact]
    public void ActiveIndentGuide_UsesTheActiveColor()
    {
        using ViewHarness harness = CreateHarness("if (x) {\n    a\n    b\n}");

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(2, 6)));

        Assert.NotEmpty(VerticalGuides(surface, ActiveIndentColor));
    }

    [Fact]
    public void RenderIndentGuidesOff_DrawsNothing()
    {
        using ViewHarness harness = CreateHarness("a\n    b\n        c", new EditorGuidesOptions { Indentation = false });

        RecordingRenderSurface surface = harness.Render();

        Assert.Empty(VerticalGuides(surface, IndentColor, ActiveIndentColor));
    }

    [Fact]
    public void BracketPairGuides_On_DrawsVerticalAndHorizontalGuides()
    {
        // The multi-line pair gives a vertical guide on its inner line; the single-line pair gives a horizontal one.
        using ViewHarness harness = CreateHarness(
            "{\n  a\n}\n(b)",
            new EditorGuidesOptions { BracketPairs = GuideMode.On, BracketPairsHorizontal = GuideMode.On, Indentation = false });
        ThemeColor bracket = ThemeColor.Parse("#220000");
        ThemeColor activeBracket = ThemeColor.Parse("#ff0000");

        RecordingRenderSurface surface = harness.Render(Selection.Collapsed(new Position(1, 1)));

        var fills = surface.Calls.OfType<FillRectangleCall>().Where(call => call.Color == bracket || call.Color == activeBracket).ToList();
        float left = harness.LayoutInfo.ContentLeft;
        Assert.Contains(fills, call => call.Width == 1 && call.Height == LineHeight && call.X == left && call.Y == LineHeight);
        // Line 4 "(b)": from the end of "(" (column 2) to the closing bracket (column 3), at the bottom of the line.
        Assert.Contains(fills, call => call.Height == 1 && call.Width == CharacterWidth && call.X == left + CharacterWidth && call.Y == (4 * LineHeight) - 1);
    }

    [Fact]
    public void Guides_PastTheWrappingColumn_AreNotDrawn()
    {
        // The wrapping column is 4: a guide at visible column 9 (left = 8 characters) is beyond 4 characters.
        using ViewHarness harness = CreateHarness("        c");
        harness.ViewModel.SetOptions(
            harness.ViewModel.LayoutOptions with
            {
                Wrapping = harness.ViewModel.LayoutOptions.Wrapping with { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 4 },
            },
            harness.ViewModel.ViewLayoutOptions);

        RecordingRenderSurface surface = harness.Render();

        float maxLeft = harness.LayoutInfo.ContentLeft + (harness.LayoutInfo.WrappingColumn * CharacterWidth);
        Assert.All(VerticalGuides(surface, IndentColor, ActiveIndentColor), call => Assert.True(call.X <= maxLeft));
    }
}
