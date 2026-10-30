using System.Globalization;
using LoomEditor.View.Layout;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI;
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
    private readonly CanvasTypography _typography = new();
    private readonly ushort _baseWeight;
    private readonly float _letterSpacing;

    /// <summary>Creates the measurer and measures the font.</summary>
    /// <param name="resourceCreator">The Win2D device or control used to measure text.</param>
    /// <param name="fontFamilies">
    /// Comma-separated list of font families, e.g. <c>"Cascadia Mono, Consolas"</c>. The first one installed is used.
    /// </param>
    /// <param name="fontSize">Font size in DIPs.</param>
    /// <param name="tabSize">Width of a tab stop, in spaces.</param>
    /// <param name="pixelRatio">Device pixel ratio, passed to the font measurements.</param>
    /// <param name="fontOptions">
    /// Weight, letter spacing, font features and variations. Only the <c>wght</c> variation axis is applied; other axes
    /// are ignored (Win2D has no API for them, ADR 0013 decision 5).
    /// </param>
    /// <param name="zoomLevel">The editor zoom level, applied to the font size and the line height.</param>
    public Win2DTextMeasurer(
        ICanvasResourceCreator resourceCreator, string fontFamilies, double fontSize, int tabSize, double pixelRatio,
        EditorFontOptions fontOptions, double zoomLevel = 0)
    {
        ArgumentNullException.ThrowIfNull(resourceCreator);
        ArgumentNullException.ThrowIfNull(fontFamilies);
        ArgumentNullException.ThrowIfNull(fontOptions);
        ArgumentOutOfRangeException.ThrowIfLessThan(tabSize, 1);

        _resourceCreator = resourceCreator;
        TabSize = tabSize;
        string family = ResolveInstalledFamily(fontFamilies);

        var bare = BareFontInfo.Create(
            family, fontOptions.FontWeight, fontSize, lineHeight: fontOptions.LineHeight, fontOptions.LetterSpacing, pixelRatio,
            fontOptions.FontFeatureSettings, fontOptions.FontVariationSettings, zoomLevel);
        _baseWeight = ResolveBaseWeight(bare);
        _letterSpacing = (float)bare.LetterSpacing;
        foreach (FontFeature feature in FontFeatureSettings.Parse(bare.FontFeatureSettings))
        {
            _typography.AddFeature(new CanvasTypographyFeature((CanvasTypographyFeatureName)FontFeatureTags.Pack(feature.Tag), (uint)feature.Value));
        }

        FontInfo = FontMeasurements.Read(
            bare, new CharacterWidthReader(resourceCreator, family, bare.FontSize, _baseWeight, _letterSpacing), pixelRatio);

        _textFormat = CreateFormat(family, bare.FontSize, CharacterStyle.Regular, _baseWeight);
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
    public ILineLayout CreateLineLayout(string text, LineStyle style)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(style);

        var textLayout = new CanvasTextLayout(_resourceCreator, text, _textFormat, float.MaxValue, float.MaxValue);
        if (text.Length > 0)
        {
            textLayout.SetTypography(0, text.Length, _typography);
            if (_letterSpacing != 0)
            {
                textLayout.SetCharacterSpacing(0, text.Length, 0, _letterSpacing, 0);
            }
        }

        foreach (StyleRun run in style.Runs)
        {
            textLayout.SetColor(run.Start, run.Length, Color.FromArgb((byte)(run.ForegroundArgb >> 24), (byte)(run.ForegroundArgb >> 16), (byte)(run.ForegroundArgb >> 8), (byte)run.ForegroundArgb));
            if ((run.FontStyle & Core.Tokens.FontStyle.Italic) != 0)
            {
                textLayout.SetFontStyle(run.Start, run.Length, FontStyle.Italic);
            }

            if ((run.FontStyle & Core.Tokens.FontStyle.Bold) != 0)
            {
                textLayout.SetFontWeight(run.Start, run.Length, new FontWeight { Weight = (ushort)Math.Max(700, (int)_baseWeight) });
            }

            if ((run.FontStyle & Core.Tokens.FontStyle.Underline) != 0)
            {
                textLayout.SetUnderline(run.Start, run.Length, true);
            }

            if ((run.FontStyle & Core.Tokens.FontStyle.Strikethrough) != 0)
            {
                textLayout.SetStrikethrough(run.Start, run.Length, true);
            }
        }

        return new Win2DLineLayout(text, textLayout);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _textFormat.Dispose();
        _typography.Dispose();
    }

    private static CanvasTextFormat CreateFormat(string family, double fontSize, CharacterStyle style, ushort baseWeight) => new()
    {
        FontFamily = family,
        FontSize = (float)fontSize,
        WordWrapping = CanvasWordWrapping.NoWrap,
        Options = CanvasDrawTextOptions.EnableColorFont,
        FontWeight = new FontWeight { Weight = style == CharacterStyle.Bold ? (ushort)Math.Max(700, (int)baseWeight) : baseWeight },
        FontStyle = style == CharacterStyle.Italic ? FontStyle.Italic : FontStyle.Normal,
    };

    // `FontVariationSettings` has been translated by BareFontInfo.Create: a `'wght' N` replaces the weight.
    private static ushort ResolveBaseWeight(BareFontInfo bare)
    {
        if (TryGetWght(bare.FontVariationSettings, out int wght))
        {
            return (ushort)Math.Clamp(wght, 1, 999);
        }

        return bare.FontWeight switch
        {
            "normal" => (ushort)400,
            "bold" => (ushort)700,
            _ => int.TryParse(bare.FontWeight, NumberStyles.None, CultureInfo.InvariantCulture, out int n) ? (ushort)Math.Clamp(n, 1, 999) : (ushort)400,
        };
    }

    private static bool TryGetWght(string settings, out int value)
    {
        value = 0;
        foreach (string item in settings.Split(','))
        {
            string trimmed = item.Trim();
            if (trimmed.Length > 6 && trimmed[0] is '\x27' or '"' && trimmed[5] == trimmed[0] && trimmed.AsSpan(1, 4).SequenceEqual("wght")
                && double.TryParse(trimmed.AsSpan(6), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                value = (int)d;
                return true;
            }
        }

        return false;
    }

    // Unlike XAML, DirectWrite takes ONE family name: a "A, B" list would silently fall back to a
    // proportional system font. So pick the first installed family ourselves.
    private static string ResolveInstalledFamily(string fontFamilies)
    {
        string[] candidates = fontFamilies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var installed = new HashSet<string>(CanvasTextFormat.GetSystemFontFamilies(), StringComparer.OrdinalIgnoreCase);
        return Array.Find(candidates, installed.Contains) ?? "Consolas";
    }

    /// <summary>Measures one character with a <see cref="CanvasTextLayout"/>.</summary>
    private sealed class CharacterWidthReader(
        ICanvasResourceCreator resourceCreator, string family, double fontSize, ushort baseWeight, float letterSpacing)
        : ICharacterWidthReader
    {
        public double GetWidth(string character, CharacterStyle style)
        {
            using CanvasTextFormat format = CreateFormat(family, fontSize, style, baseWeight);
            using var layout = new CanvasTextLayout(resourceCreator, character, format, float.MaxValue, float.MaxValue);
            if (letterSpacing != 0)
            {
                layout.SetCharacterSpacing(0, character.Length, 0, letterSpacing, 0);
            }

            // The caret after the character is its advance width, also for spaces (LayoutBounds.Width skips trailing whitespace).
            return layout.GetCaretPosition(character.Length, false).X;
        }
    }
}
