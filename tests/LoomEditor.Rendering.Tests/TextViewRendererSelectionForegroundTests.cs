using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of view lines (it renders HTML): all tests are new.
public class TextViewRendererSelectionForegroundTests
{
    private static ViewHarness CreateHarness(string text, string theme = "Dark High Contrast")
    {
        var harness = new ViewHarness(text);
        harness.Renderer.Theme = EditorTheme.BuiltIn.Single(t => t.Name == theme);
        return harness;
    }

    // Every line: the first 3 characters are red italic (color id 3), the rest is blue (color id 4).
    private sealed class TwoTokenSupport : ITokenizationSupport
    {
        private const uint RedItalic = (3u << MetadataConsts.ForegroundOffset) | ((uint)FontStyle.Italic << MetadataConsts.FontStyleOffset);
        private const uint BlueMeta = 4u << MetadataConsts.ForegroundOffset;

        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) =>
            new(line.Length > 3 ? [0, RedItalic, 3, BlueMeta] : [0, RedItalic], NullState.Instance);
    }

    private static uint Argb(ThemeColor color) => LineStyle.ToArgb(color);

    private static StyleRun[] RunsOf(ViewHarness harness, string text) =>
        harness.Measurer.Layouts.Last(l => l.Text == text).Style.Runs.ToArray();

    [Fact]
    public void HighContrastDark_SelectedText_UsesTheSelectionForeground()
    {
        using ViewHarness harness = CreateHarness("hello world");

        harness.Render(new Selection(1, 1, 1, 6));

        Assert.Equal(
            [new StyleRun(0, 5, Argb(ThemeColor.Parse("#000000")), FontStyle.None), new StyleRun(5, 6, Argb(harness.Renderer.Theme.Foreground), FontStyle.None)],
            RunsOf(harness, "hello world"));
    }

    [Fact]
    public void HighContrastLight_SelectedText_UsesWhite()
    {
        using ViewHarness harness = CreateHarness("hello world", "Light High Contrast");

        harness.Render(new Selection(1, 1, 1, 6));

        Assert.Equal(new StyleRun(0, 5, Argb(ThemeColor.Parse("#FFFFFF")), FontStyle.None), RunsOf(harness, "hello world")[0]);
    }

    [Fact]
    public void DarkTheme_SelectedText_KeepsTokenColors()
    {
        using ViewHarness harness = CreateHarness("hello world", "Dark Modern");

        harness.Render(new Selection(1, 1, 1, 6));

        Assert.Empty(RunsOf(harness, "hello world"));
    }

    [Fact]
    public void HighContrastSelection_MultiLine_ColorsTheSelectedPartOfEachLine()
    {
        using ViewHarness harness = CreateHarness("abc\ndef");
        uint black = Argb(ThemeColor.Parse("#000000"));

        harness.Render(new Selection(1, 2, 2, 3));

        Assert.Equal(new StyleRun(1, 2, black, FontStyle.None), RunsOf(harness, "abc")[1]);
        Assert.Equal(new StyleRun(0, 2, black, FontStyle.None), RunsOf(harness, "def")[0]);
    }

    [Fact]
    public void HighContrastSelection_Empty_ChangesNothing()
    {
        using ViewHarness harness = CreateHarness("hello world");

        harness.Render(Selection.Collapsed(new Position(1, 3)));

        Assert.Empty(RunsOf(harness, "hello world"));
    }

    [Fact]
    public void HighContrastSelection_WinsOverTokenColors()
    {
        var registry = new TokenizationRegistry();
        registry.Register("two", new TwoTokenSupport());
        registry.SetColorMap(new uint[] { 0, 0xFFCCCCCC, 0xFF1F1F1F, 0xFFFF0000, 0xFF0000FF });
        var codec = new LanguageIdCodec();
        codec.Register("two");
        var model = new TextModel("hello world", DefaultEndOfLine.LF, null, registry, codec, null);
        model.Tokenization.SetLanguageId("two");
        model.Tokenization.ForceTokenization(model.LineCount);
        using var harness = new ViewHarness("hello world", model: model);
        harness.Renderer.Theme = EditorTheme.BuiltIn.Single(t => t.Name == "Dark High Contrast");
        harness.ViewModel.TokenColorMap = registry.GetColorMap();
        uint black = Argb(ThemeColor.Parse("#000000"));

        harness.Render(new Selection(1, 1, 1, 6));

        Assert.Equal(
            [
                new StyleRun(0, 3, black, FontStyle.Italic),
                new StyleRun(3, 2, black, FontStyle.None),
                new StyleRun(5, 6, 0xFF0000FF, FontStyle.None),
            ],
            RunsOf(harness, "hello world"));
    }

    [Fact]
    public void HighContrastSelection_MultipleCursors_ColorsEachSelection()
    {
        using ViewHarness harness = CreateHarness("hello world");
        uint black = Argb(ThemeColor.Parse("#000000"));

        harness.Render([new Selection(1, 1, 1, 3), new Selection(1, 7, 1, 9)]);

        StyleRun[] runs = RunsOf(harness, "hello world");
        Assert.Equal(new StyleRun(0, 2, black, FontStyle.None), runs[0]);
        Assert.Equal(new StyleRun(6, 2, black, FontStyle.None), runs[2]);
    }

    [Fact]
    public void HighContrastSelection_WrappedLine_UsesViewColumns()
    {
        using ViewHarness harness = CreateHarness("aaa bbb ccc ddd");
        harness.ViewModel.SetOptions(
            harness.ViewModel.LayoutOptions with
            {
                Wrapping = new EditorWrappingOptions { WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 8 },
            },
            harness.ViewModel.ViewLayoutOptions);
        uint black = Argb(ThemeColor.Parse("#000000"));

        // View lines: "aaa bbb ", "ccc ddd". The view selection spans (1,5) to (2,4).
        harness.Render(new Selection(1, 5, 2, 4));

        Assert.Equal(new StyleRun(4, 4, black, FontStyle.None), RunsOf(harness, "aaa bbb ")[1]);
        Assert.Equal(new StyleRun(0, 3, black, FontStyle.None), RunsOf(harness, "ccc ddd")[0]);
    }
}
