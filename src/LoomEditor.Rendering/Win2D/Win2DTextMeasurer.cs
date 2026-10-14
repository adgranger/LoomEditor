using LoomEditor.View.Layout;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI.Text;

namespace LoomEditor.Rendering.Win2D;

/// <summary>
/// The editor font for Win2D: a DirectWrite text format, measured the way Monaco measures fonts
/// (<see cref="FontMeasurements"/>), and the factory of line layouts.
/// </summary>
/// <remarks>
/// Create it once per control and reuse it for every frame. Tabs use DirectWrite tab stops
/// (<c>IncrementalTabStop = tabSize × space width</c>, ADR 0005 decision 5).
/// Threading: UI thread only.
/// </remarks>
internal sealed class Win2DTextMeasurer : ITextMeasurer
{
    private readonly ICanvasResourceCreator _resourceCreator;
    private readonly CanvasTextFormat _textFormat;

    /// <summary>Creates the measurer and measures the font.</summary>
    /// <param name="resourceCreator">The Win2D device or control used to measure text.</param>
    /// <param name="fontFamilies">
    /// Comma-separated list of font families, e.g. <c>"Cascadia Mono, Consolas"</c>. The first one installed is used.
    /// </param>
    /// <param name="fontSize">Font size in DIPs.</param>
    /// <param name="tabSize">Width of a tab stop, in spaces.</param>
    /// <param name="pixelRatio">Device pixel ratio, passed to the font measurements.</param>
    public Win2DTextMeasurer(
        ICanvasResourceCreator resourceCreator, string fontFamilies, double fontSize, int tabSize, double pixelRatio)
    {
        ArgumentNullException.ThrowIfNull(resourceCreator);
        ArgumentNullException.ThrowIfNull(fontFamilies);
        ArgumentOutOfRangeException.ThrowIfLessThan(tabSize, 1);

        _resourceCreator = resourceCreator;
        TabSize = tabSize;
        string family = ResolveInstalledFamily(fontFamilies);

        var bare = BareFontInfo.Create(family, "normal", fontSize, lineHeight: 0, letterSpacing: 0, pixelRatio);
        FontInfo = FontMeasurements.Read(bare, new CharacterWidthReader(resourceCreator, family, fontSize), pixelRatio);

        _textFormat = CreateFormat(family, fontSize, CharacterStyle.Regular);
        _textFormat.IncrementalTabStop = (float)(tabSize * FontInfo.SpaceWidth);

        using var probe = new CanvasTextLayout(resourceCreator, "n", _textFormat, float.MaxValue, float.MaxValue);
        GlyphTopOffset = (float)((FontInfo.LineHeight - probe.LayoutBounds.Height) / 2);
    }

    /// <inheritdoc />
    public FontInfo FontInfo { get; }

    /// <inheritdoc />
    public int TabSize { get; }

    /// <summary>Distance from the top of a line box to the top of its glyph layout, in DIPs.</summary>
    public float GlyphTopOffset { get; }

    /// <inheritdoc />
    public ILineLayout CreateLineLayout(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new Win2DLineLayout(text, new CanvasTextLayout(_resourceCreator, text, _textFormat, float.MaxValue, float.MaxValue));
    }

    /// <inheritdoc />
    public void Dispose() => _textFormat.Dispose();

    private static CanvasTextFormat CreateFormat(string family, double fontSize, CharacterStyle style) => new()
    {
        FontFamily = family,
        FontSize = (float)fontSize,
        WordWrapping = CanvasWordWrapping.NoWrap,
        Options = CanvasDrawTextOptions.EnableColorFont,
        FontWeight = new FontWeight { Weight = style == CharacterStyle.Bold ? (ushort)700 : (ushort)400 },
        FontStyle = style == CharacterStyle.Italic ? FontStyle.Italic : FontStyle.Normal,
    };

    // Unlike XAML, DirectWrite takes ONE family name: a "A, B" list would silently fall back to a
    // proportional system font. So pick the first installed family ourselves.
    private static string ResolveInstalledFamily(string fontFamilies)
    {
        string[] candidates = fontFamilies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var installed = new HashSet<string>(CanvasTextFormat.GetSystemFontFamilies(), StringComparer.OrdinalIgnoreCase);
        return Array.Find(candidates, installed.Contains) ?? "Consolas";
    }

    /// <summary>Measures one character with a <see cref="CanvasTextLayout"/>.</summary>
    private sealed class CharacterWidthReader(ICanvasResourceCreator resourceCreator, string family, double fontSize)
        : ICharacterWidthReader
    {
        public double GetWidth(string character, CharacterStyle style)
        {
            using CanvasTextFormat format = CreateFormat(family, fontSize, style);
            using var layout = new CanvasTextLayout(resourceCreator, character, format, float.MaxValue, float.MaxValue);
            // The caret after the character is its advance width, also for spaces (LayoutBounds.Width skips trailing whitespace).
            return layout.GetCaretPosition(character.Length, false).X;
        }
    }
}
