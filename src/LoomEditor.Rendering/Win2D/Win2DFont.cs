using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;

namespace LoomEditor.Rendering.Win2D;

/// <summary>
/// The editor font for Win2D: a DirectWrite text format and its measured <see cref="FontMetrics"/>.
/// Create it once per control (measuring is not free) and reuse it for every frame.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class Win2DFont : IDisposable
{
    /// <summary>Creates the font and measures it.</summary>
    /// <param name="resourceCreator">The Win2D device or control used to measure text.</param>
    /// <param name="fontFamilies">
    /// Comma-separated list of font families, e.g. <c>"Cascadia Mono, Consolas"</c>. The first one installed is used.
    /// </param>
    /// <param name="fontSize">Font size in DIPs.</param>
    public Win2DFont(ICanvasResourceCreator resourceCreator, string fontFamilies, float fontSize)
    {
        ArgumentNullException.ThrowIfNull(resourceCreator);
        ArgumentNullException.ThrowIfNull(fontFamilies);

        TextFormat = new CanvasTextFormat
        {
            FontFamily = ResolveInstalledFamily(fontFamilies),
            FontSize = fontSize,
            WordWrapping = CanvasWordWrapping.NoWrap,
            Options = CanvasDrawTextOptions.EnableColorFont,
        };

        // Measure one character of a monospaced font. Phase 2 replaces this with per-line layout.
        using var layout = new CanvasTextLayout(resourceCreator, "M", TextFormat, float.MaxValue, float.MaxValue);
        Metrics = new FontMetrics((float)layout.LayoutBounds.Width, (float)layout.LayoutBounds.Height);
    }

    /// <summary>The Win2D (DirectWrite) text format.</summary>
    public CanvasTextFormat TextFormat { get; }

    /// <summary>The measured size of a character and of a line.</summary>
    public FontMetrics Metrics { get; }

    /// <inheritdoc />
    public void Dispose() => TextFormat.Dispose();

    // Unlike XAML, DirectWrite takes ONE family name: a "A, B" list would silently fall back to a
    // proportional system font. So pick the first installed family ourselves.
    private static string ResolveInstalledFamily(string fontFamilies)
    {
        string[] candidates = fontFamilies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var installed = new HashSet<string>(CanvasTextFormat.GetSystemFontFamilies(), StringComparer.OrdinalIgnoreCase);
        return Array.Find(candidates, installed.Contains) ?? "Consolas";
    }
}
