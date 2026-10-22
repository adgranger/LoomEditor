// Ported from microsoft/vscode: src/vs/editor/common/viewModel/minimapTokensColorTracker.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;
using LoomEditor.Themes;

namespace LoomEditor.View.Minimap;

/// <summary>The token colors as <see cref="Rgba8"/>, by color id. Monaco: <c>MinimapTokensColorTracker</c>.</summary>
/// <remarks>
/// Threading: immutable, safe from any thread.
/// Behavior difference: not a singleton and no <c>onDidChange</c>: it is built from a theme, and a new one is created
/// when the theme changes. A missing color id returns <see cref="Rgba8.Empty"/> without logging.
/// </remarks>
internal sealed class MinimapTokensColorTracker
{
    private readonly Rgba8[] _colors;
    private readonly bool _backgroundIsLight;

    /// <summary>Reads the token color map of <paramref name="theme"/>.</summary>
    public MinimapTokensColorTracker(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        IReadOnlyList<ThemeColor> colorMap = theme.TokenColorMap;
        if (colorMap.Count <= ColorId.DefaultBackground)
        {
            _colors = new Rgba8[ColorId.DefaultBackground + 1];
            _backgroundIsLight = true;
            return;
        }

        _colors = new Rgba8[colorMap.Count];
        for (int colorId = 1; colorId < colorMap.Count; colorId++)
        {
            ThemeColor source = colorMap[colorId];
            _colors[colorId] = new Rgba8(source.R, source.G, source.B, source.A);
        }

        _backgroundIsLight = Color.FromThemeColor(colorMap[ColorId.DefaultBackground]).GetRelativeLuminance() >= 0.5;
    }

    /// <summary>The color of <paramref name="colorId"/>; the default background for an unknown id (basically invisible).</summary>
    public Rgba8 GetColor(int colorId)
    {
        if (colorId < 1 || colorId >= _colors.Length)
        {
            colorId = ColorId.DefaultBackground;
        }

        return _colors[colorId];
    }

    /// <summary>Whether the default background is light (relative luminance of at least 0.5).</summary>
    public bool BackgroundIsLight() => _backgroundIsLight;
}
