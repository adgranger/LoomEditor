using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;
using LoomEditor.Rendering.Tests.TestHelpers;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of the lines (it renders HTML): all tests are new.
public class TextViewRendererTokensTests
{
    private const uint Red = 0xFFFF0000;
    private const uint Blue = 0xFF0000FF;

    // Every line: the first 3 characters are red italic (color id 3), the rest is blue (color id 4).
    private sealed class TwoTokenSupport : ITokenizationSupport
    {
        private const uint RedItalic = (3u << MetadataConsts.ForegroundOffset) | ((uint)FontStyle.Italic << MetadataConsts.FontStyleOffset);
        private const uint BlueMeta = 4u << MetadataConsts.ForegroundOffset;

        public IState GetInitialState() => NullState.Instance;

        public EncodedTokenizationResult TokenizeEncoded(string line, bool hasEol, IState state) =>
            new(line.Length > 3 ? [0, RedItalic, 3, BlueMeta] : [0, RedItalic], NullState.Instance);
    }

    private static ViewHarness CreateHarness(string text)
    {
        var registry = new TokenizationRegistry();
        registry.Register("two", new TwoTokenSupport());
        registry.SetColorMap(new uint[] { 0, 0xFFCCCCCC, 0xFF1F1F1F, Red, Blue });
        var codec = new LanguageIdCodec();
        codec.Register("two");
        var model = new TextModel(text, DefaultEndOfLine.LF, null, registry, codec, null);
        model.Tokenization.SetLanguageId("two");
        model.Tokenization.ForceTokenization(model.LineCount);
        var harness = new ViewHarness(text, model: model);
        harness.ViewModel.TokenColorMap = registry.GetColorMap();
        return harness;
    }

    [Fact]
    public void Render_TokensOfVisibleLines_AreGivenToTheMeasurer()
    {
        using ViewHarness harness = CreateHarness("hello\nhi");

        harness.Render();

        FakeLineLayout hello = harness.Measurer.Layouts.Single(l => l.Text == "hello");
        Assert.Equal(
            new StyleRun[] { new(0, 3, Red, FontStyle.Italic), new(3, 2, Blue, FontStyle.None) },
            hello.Style.Runs);
        Assert.Equal(new StyleRun[] { new(0, 2, Red, FontStyle.Italic) }, harness.Measurer.Layouts.Single(l => l.Text == "hi").Style.Runs);
    }

    [Fact]
    public void Render_LongLine_RunsAreCutAtRenderedLength()
    {
        using ViewHarness harness = CreateHarness(new string('x', 10_050));

        harness.Render();

        FakeLineLayout line = harness.Measurer.Layouts.Single(l => l.Text.Length == 10_000);
        StyleRun last = line.Style.Runs[^1];
        Assert.Equal(10_000, last.Start + last.Length);
    }
}
