using LoomEditor.Core.Tokens;

namespace LoomEditor.Rendering.Tests;

// Inspired by Monaco's viewLineRenderer.ts (transformAndRemoveOverflowing); no upstream test: all tests are new.
public class LineStyleTests
{
    private const uint DefaultColor = 0xFFCCCCCC;
    private const uint Red = 0xFFFF0000;
    private const uint Blue = 0xFF0000FF;

    // Color ids: 1 = default foreground, 2 = default background, 3 = red, 4 = blue.
    private static readonly uint[] ColorMap = [0, DefaultColor, 0xFF1F1F1F, Red, Blue];

    private static uint Meta(int foreground, FontStyle fontStyle = FontStyle.None) =>
        ((uint)foreground << MetadataConsts.ForegroundOffset) | ((uint)fontStyle << MetadataConsts.FontStyleOffset);

    private static LineTokens Tokens(params (string Text, uint Metadata)[] tokens) =>
        LineTokens.CreateFromTextAndMetadata(tokens, new LanguageIdCodec());

    [Fact]
    public void LineStyle_MergesRunsWithSameStyle()
    {
        LineTokens tokens = Tokens(("ab", Meta(3)), ("cd", Meta(3)), ("ef", Meta(4)), ("gh", Meta(4, FontStyle.Italic)));

        LineStyle style = LineStyle.Create(tokens, 8, ColorMap, DefaultColor);

        Assert.Equal(
            new StyleRun[] { new(0, 4, Red, FontStyle.None), new(4, 2, Blue, FontStyle.None), new(6, 2, Blue, FontStyle.Italic) },
            style.Runs);
    }

    [Fact]
    public void LineStyle_PlainLine_IsPlain()
    {
        LineTokens tokens = Tokens(("ab", Meta(ColorId.DefaultForeground)), ("cd", Meta(ColorId.DefaultForeground)));

        Assert.Same(LineStyle.Plain, LineStyle.Create(tokens, 4, ColorMap, DefaultColor));
        Assert.Same(LineStyle.Plain, LineStyle.Create(tokens, 4, null, DefaultColor));
        Assert.Same(LineStyle.Plain, LineStyle.Create(Tokens(("ab", Meta(3))), 2, null, DefaultColor)); // no color map: no colors
    }

    [Fact]
    public void LineStyle_SameRuns_AreEqual()
    {
        LineTokens tokens = Tokens(("ab", Meta(3)), ("cd", Meta(4)));

        LineStyle first = LineStyle.Create(tokens, 4, ColorMap, DefaultColor);
        LineStyle second = LineStyle.Create(tokens, 4, ColorMap, DefaultColor);

        Assert.NotSame(first, second);
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, LineStyle.Plain);
    }

    [Fact]
    public void LineStyle_LongLine_RunsAreCutAtTheRenderedLength()
    {
        LineTokens tokens = Tokens(("abcd", Meta(3)), ("efgh", Meta(4)), ("ijkl", Meta(3)));

        LineStyle style = LineStyle.Create(tokens, 6, ColorMap, DefaultColor);

        Assert.Equal(new StyleRun[] { new(0, 4, Red, FontStyle.None), new(4, 2, Blue, FontStyle.None) }, style.Runs);
    }
}
