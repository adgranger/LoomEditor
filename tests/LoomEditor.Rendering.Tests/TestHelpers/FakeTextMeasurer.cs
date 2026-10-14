using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests.TestHelpers;

/// <summary>An <see cref="ITextMeasurer"/> with a fixed width per character; a tab jumps to the next tab stop.</summary>
/// <remarks>Threading: not thread-safe; one test, one thread.</remarks>
internal sealed class FakeTextMeasurer : ITextMeasurer
{
    public const double CharacterWidth = 10;
    public const int LineHeight = 20;

    public FakeTextMeasurer(double pixelRatio = 1)
    {
        FontInfo = FontMeasurements.Read(
            BareFontInfo.Create("Fake", "normal", fontSize: 14, lineHeight: LineHeight, letterSpacing: 0, pixelRatio: pixelRatio),
            new FixedWidthReader(),
            pixelRatio: pixelRatio);
    }

    public FontInfo FontInfo { get; }

    public int TabSize => 4;

    public List<FakeLineLayout> Layouts { get; } = [];

    public ILineLayout CreateLineLayout(string text)
    {
        var layout = new FakeLineLayout(text, CharacterWidth, TabSize);
        Layouts.Add(layout);
        return layout;
    }

    public void Dispose()
    {
    }

    private sealed class FixedWidthReader : ICharacterWidthReader
    {
        public double GetWidth(string character, CharacterStyle style) => CharacterWidth;
    }
}
