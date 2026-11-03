using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using LoomEditor.Rendering;
using LoomEditor.View.Layout;
using AvaloniaFontFeature = Avalonia.Media.FontFeature;
using FontFeature = LoomEditor.View.Layout.FontFeature;
using FontStyle = Avalonia.Media.FontStyle;
using TokenFontStyle = LoomEditor.Core.Tokens.FontStyle;

namespace LoomEditor.Avalonia.Rendering;

/// <summary>
/// The editor font for Avalonia: a typeface and its run properties, measured the way Monaco measures fonts
/// (<see cref="FontMeasurements"/>), and the factory of line layouts. The twin of the WinUI <c>Win2DTextMeasurer</c>.
/// </summary>
/// <remarks>
/// Create it once per control and reuse it for every frame. Tabs go to the next tab stop
/// (<c>tabSize × space width</c>, ADR 0005 decision 5): Avalonia's <c>DefaultIncrementalTab</c> is set to it, but its shaper
/// gives every tab that same advance, so each tab is laid out as a run of its own (<see cref="LineTextSource"/>).
/// Behavior difference with the Win2D twin: that run has no ink, so an underline or strikethrough is not drawn across a tab. Text outside the style runs has no
/// foreground brush: the drawing code gives it the default foreground.
/// Threading: UI thread only.
/// </remarks>
internal sealed partial class AvaloniaTextMeasurer : ITextMeasurer
{
    // Rounding slack when a tab starts on a tab stop, in DIPs.
    private const double TabStopTolerance = 0.001;

    private readonly FontFamily _family;
    private readonly double _fontSize;
    private readonly FontWeight _baseWeight;
    private readonly double _letterSpacing;
    private readonly FontFeatureCollection _fontFeatures = [];
    private readonly TextRunProperties _defaultProperties;
    private readonly double _incrementalTab;

    /// <summary>Creates the measurer and measures the font.</summary>
    /// <param name="fontFamilies">
    /// Comma-separated list of font families, e.g. <c>"Cascadia Mono, Consolas"</c>. The first one installed, or embedded
    /// by the host (<c>avares://…#Family</c>, <c>fonts:…#Family</c>), is used.
    /// </param>
    /// <param name="fontSize">Font size in DIPs.</param>
    /// <param name="tabSize">Width of a tab stop, in spaces.</param>
    /// <param name="pixelRatio">Device pixel ratio (<c>TopLevel.RenderScaling</c>), passed to the font measurements.</param>
    /// <param name="fontOptions">
    /// Weight, letter spacing, font features and variations. Only the <c>wght</c> variation axis is applied; other axes
    /// are ignored (Avalonia has no API for them, ADR 0013 decision 5).
    /// </param>
    /// <param name="zoomLevel">The editor zoom level, applied to the font size and the line height.</param>
    public AvaloniaTextMeasurer(string fontFamilies, double fontSize, int tabSize, double pixelRatio, EditorFontOptions fontOptions, double zoomLevel = 0)
    {
        ArgumentNullException.ThrowIfNull(fontFamilies);
        ArgumentNullException.ThrowIfNull(fontOptions);
        ArgumentOutOfRangeException.ThrowIfLessThan(tabSize, 1);

        TabSize = tabSize;
        string family = ResolveInstalledFamily(fontFamilies);
        _family = new FontFamily(family);

        var bare = BareFontInfo.Create(
            family, fontOptions.FontWeight, fontSize, lineHeight: fontOptions.LineHeight, fontOptions.LetterSpacing, pixelRatio,
            fontOptions.FontFeatureSettings, fontOptions.FontVariationSettings, zoomLevel);
        _fontSize = bare.FontSize;
        _baseWeight = ResolveBaseWeight(bare);
        _letterSpacing = bare.LetterSpacing;
        foreach (FontFeature feature in FontFeatureSettings.Parse(bare.FontFeatureSettings))
        {
            _fontFeatures.Add(new AvaloniaFontFeature { Tag = feature.Tag, Value = feature.Value });
        }

        _defaultProperties = CreateRunProperties(DefaultForeground, TokenFontStyle.None);
        // The widths are measured on one-character layouts, which have no tab: the default tab is fine until then.
        _incrementalTab = 4 * _fontSize;
        FontInfo = FontMeasurements.Read(bare, new CharacterWidthReader(this), pixelRatio);
        _incrementalTab = tabSize * FontInfo.SpaceWidth;

        using TextLayout probe = CreateTextLayout("n", []);
        GlyphTopOffset = (FontInfo.LineHeight - probe.Height) / 2;
    }

    /// <inheritdoc />
    public FontInfo FontInfo { get; }

    /// <inheritdoc />
    public int TabSize { get; }

    /// <summary>
    /// The foreground of the text that has no style run of its own. Layouts share it and the surface sets its color before
    /// each draw, as Win2D takes the default color at draw time.
    /// </summary>
    public SolidColorBrush DefaultForeground { get; } = new(Colors.Black);

    /// <summary>Distance from the top of a line box to the top of its glyph layout, in DIPs.</summary>
    public double GlyphTopOffset { get; }

    /// <inheritdoc />
    public ILineLayout CreateLineLayout(string text, LineStyle style)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(style);

        var overrides = new ValueSpan<TextRunProperties>[style.Runs.Count];
        for (int i = 0; i < overrides.Length; i++)
        {
            StyleRun run = style.Runs[i];
            overrides[i] = new ValueSpan<TextRunProperties>(
                run.Start, run.Length, CreateRunProperties(new ImmutableSolidColorBrush(run.ForegroundArgb), run.FontStyle));
        }

        return new AvaloniaLineLayout(text, CreateTextLayout(text, overrides));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to release: Avalonia's typefaces are cached by its font manager.
    }

    // Avalonia's shaper gives every tab the same advance (DefaultIncrementalTab): a tab after "ab" would not stop at the
    // next tab stop. So each tab is a run of its own whose width goes to the next stop, found by laying out the text before it.
    private TextLayout CreateTextLayout(string text, IReadOnlyList<ValueSpan<TextRunProperties>> overrides)
    {
        var source = new LineTextSource(text, _defaultProperties, overrides);
        double x = 0;
        for (int tab = text.IndexOf('	', StringComparison.Ordinal), previous = -1; tab >= 0; previous = tab, tab = text.IndexOf('	', tab + 1))
        {
            if (tab != previous + 1)
            {
                source.Length = tab;
                using TextLayout prefix = CreateTextLayout(source);
                x = prefix.HitTestTextPosition(tab).X;
            }

            // A tab that starts on a stop goes to the next one, as in DirectWrite.
            double width = ((Math.Floor((x + TabStopTolerance) / _incrementalTab) + 1) * _incrementalTab) - x;
            source.SetTabWidth(tab, width);
            x += width;
        }

        source.Length = text.Length;
        return CreateTextLayout(source);
    }

    private TextLayout CreateTextLayout(LineTextSource source) =>
        new(
            source, new LineParagraphProperties(_defaultProperties, _incrementalTab, _letterSpacing),
            TextTrimming.None, double.PositiveInfinity, double.PositiveInfinity, maxLines: 0);

    private GenericTextRunProperties CreateRunProperties(IBrush? foreground, TokenFontStyle fontStyle)
    {
        FontStyle style = (fontStyle & TokenFontStyle.Italic) != 0 ? FontStyle.Italic : FontStyle.Normal;
        FontWeight weight = (fontStyle & TokenFontStyle.Bold) != 0 ? (FontWeight)Math.Max(700, (int)_baseWeight) : _baseWeight;
        TextDecorationCollection? decorations = null;
        if ((fontStyle & (TokenFontStyle.Underline | TokenFontStyle.Strikethrough)) != 0)
        {
            decorations = [];
            if ((fontStyle & TokenFontStyle.Underline) != 0)
            {
                decorations.AddRange(TextDecorations.Underline);
            }

            if ((fontStyle & TokenFontStyle.Strikethrough) != 0)
            {
                decorations.AddRange(TextDecorations.Strikethrough);
            }
        }

        return new GenericTextRunProperties(
            new Typeface(_family, style, weight), _fontSize, decorations, foreground, backgroundBrush: null,
            BaselineAlignment.Baseline, CultureInfo.CurrentCulture, _fontFeatures);
    }

    // `FontVariationSettings` has been translated by BareFontInfo.Create: a `'wght' N` replaces the weight.
    private static FontWeight ResolveBaseWeight(BareFontInfo bare)
    {
        if (TryGetWght(bare.FontVariationSettings, out int wght))
        {
            return (FontWeight)Math.Clamp(wght, 1, 999);
        }

        return bare.FontWeight switch
        {
            "normal" => FontWeight.Normal,
            "bold" => FontWeight.Bold,
            _ => int.TryParse(bare.FontWeight, NumberStyles.None, CultureInfo.InvariantCulture, out int n) ? (FontWeight)Math.Clamp(n, 1, 999) : FontWeight.Normal,
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

    // Avalonia takes "A, B" lists, but FontMeasurements needs ONE family name (as DirectWrite in the Win2D twin): pick the
    // first installed or embedded family, else the first installed default family of the OS, else Avalonia's default.
    private static string ResolveInstalledFamily(string fontFamilies)
    {
        FontManager fonts = FontManager.Current;
        var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (FontFamily systemFamily in fonts.SystemFonts)
        {
            installed.Add(systemFamily.Name);
        }

        return FirstAvailable(fontFamilies) ?? FirstAvailable(BareFontInfo.DefaultFontFamilyForCurrentOS) ?? fonts.DefaultFontFamily.Name;

        string? FirstAvailable(string list)
        {
            foreach (string candidate in list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string name = candidate.Trim('\'', '"');
                if (installed.Contains(name) || IsEmbedded(name))
                {
                    return name;
                }
            }

            return null;
        }

        // "avares://Assembly/Folder#Family" or "fonts:Collection#Family": the family must exist in that collection.
        bool IsEmbedded(string name)
        {
            int hash = name.IndexOf('#', StringComparison.Ordinal);
            return hash > 0
                && fonts.TryGetGlyphTypeface(new Typeface(new FontFamily(name)), out GlyphTypeface? glyphTypeface)
                && string.Equals(glyphTypeface.FamilyName, name[(hash + 1)..], StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Measures one character with a one-character layout.</summary>
    private sealed class CharacterWidthReader(AvaloniaTextMeasurer measurer) : ICharacterWidthReader
    {
        public double GetWidth(string character, CharacterStyle style)
        {
            TokenFontStyle fontStyle = style switch
            {
                CharacterStyle.Bold => TokenFontStyle.Bold,
                CharacterStyle.Italic => TokenFontStyle.Italic,
                _ => TokenFontStyle.None,
            };
            using TextLayout layout = measurer.CreateTextLayout(
                character, [new ValueSpan<TextRunProperties>(0, character.Length, measurer.CreateRunProperties(null, fontStyle))]);

            // The caret after the character is its advance width, also for spaces (TextLayout.Width skips trailing whitespace).
            return layout.HitTestTextPosition(character.Length).X;
        }
    }
}
