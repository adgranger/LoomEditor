// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (MinimapOptions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;
using LoomEditor.Themes;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Minimap;

/// <summary>The resolved minimap settings for one frame. Monaco: <c>MinimapOptions</c>.</summary>
/// <remarks>
/// Threading: immutable apart from the lazily created <see cref="CharRenderer"/>; UI thread only.
/// Behavior differences: built from explicit values instead of <c>IEditorConfiguration</c>; no padding (always 0); the
/// section header members are not ported; <c>equals</c> is <see cref="EqualsOptions"/>.
/// </remarks>
internal sealed class MinimapOptions
{
    /// <summary>The orthogonal distance to the slider at which dragging "resets" (snapping). Monaco: <c>POINTER_DRAG_RESET_DISTANCE</c>.</summary>
    public const int PointerDragResetDistance = 140;

    /// <summary>Monaco: <c>GUTTER_DECORATION_WIDTH</c>.</summary>
    public const int GutterDecorationWidth = 2;

    private readonly Lazy<MinimapCharRenderer> _charRenderer;

    /// <summary>Builds the options of a frame.</summary>
    /// <param name="layoutInfo">The editor layout (its minimap part and height).</param>
    /// <param name="options">The minimap options.</param>
    /// <param name="fontInfo">The editor font.</param>
    /// <param name="scrollBeyondLastLine">The editor option.</param>
    /// <param name="theme">The theme (minimap colors).</param>
    /// <param name="tracker">The token colors of <paramref name="theme"/>.</param>
    /// <param name="charRendererFactory">Creates the character renderer from the font scale and the font family.</param>
    public MinimapOptions(
        EditorLayoutInfo layoutInfo,
        EditorMinimapOptions options,
        FontInfo fontInfo,
        bool scrollBeyondLastLine,
        EditorTheme theme,
        MinimapTokensColorTracker tracker,
        Func<int, string, MinimapCharRenderer> charRendererFactory)
    {
        EditorMinimapLayoutInfo minimapLayout = layoutInfo.Minimap;
        RenderMinimap = minimapLayout.RenderMinimap;
        Size = options.Size;
        MinimapHeightIsEditorHeight = minimapLayout.MinimapHeightIsEditorHeight;
        ScrollBeyondLastLine = scrollBeyondLastLine;
        PaddingTop = 0;
        PaddingBottom = 0;
        ShowSlider = options.ShowSlider;
        Autohide = options.Autohide;
        PixelRatio = fontInfo.PixelRatio;
        TypicalHalfwidthCharacterWidth = fontInfo.TypicalHalfwidthCharacterWidth;
        LineHeight = fontInfo.LineHeight;
        MinimapLeft = minimapLayout.MinimapLeft;
        MinimapWidth = minimapLayout.MinimapWidth;
        MinimapHeight = layoutInfo.Height;
        CanvasInnerWidth = minimapLayout.MinimapCanvasInnerWidth;
        CanvasInnerHeight = minimapLayout.MinimapCanvasInnerHeight;
        CanvasOuterWidth = minimapLayout.MinimapCanvasOuterWidth;
        CanvasOuterHeight = minimapLayout.MinimapCanvasOuterHeight;
        IsSampling = minimapLayout.MinimapIsSampling;
        EditorHeight = layoutInfo.Height;
        FontScale = minimapLayout.MinimapScale;
        MinimapLineHeight = minimapLayout.MinimapLineHeight;
        MinimapCharWidth = MinimapCharSheet.BaseCharWidth * FontScale;

        int scale = (int)FontScale;
        string fontFamily = fontInfo.FontFamily;
        _charRenderer = new Lazy<MinimapCharRenderer>(() => charRendererFactory(scale, fontFamily));
        DefaultBackgroundColor = tracker.GetColor(ColorId.DefaultBackground);
        BackgroundColor = GetMinimapBackground(theme, DefaultBackgroundColor);
        ForegroundAlpha = GetMinimapForegroundOpacity(theme);
    }

    /// <summary>Monaco: <c>renderMinimap</c>.</summary>
    public RenderMinimap RenderMinimap { get; }

    /// <summary>Monaco: <c>size</c>.</summary>
    public MinimapSize Size { get; }

    /// <summary>Monaco: <c>minimapHeightIsEditorHeight</c>.</summary>
    public bool MinimapHeightIsEditorHeight { get; }

    /// <summary>Monaco: <c>scrollBeyondLastLine</c>.</summary>
    public bool ScrollBeyondLastLine { get; }

    /// <summary>Monaco: <c>paddingTop</c> (always 0).</summary>
    public int PaddingTop { get; }

    /// <summary>Monaco: <c>paddingBottom</c> (always 0).</summary>
    public int PaddingBottom { get; }

    /// <summary>Monaco: <c>showSlider</c>.</summary>
    public MinimapShowSlider ShowSlider { get; }

    /// <summary>Monaco: <c>autohide</c>.</summary>
    public MinimapAutohide Autohide { get; }

    /// <summary>Monaco: <c>pixelRatio</c>.</summary>
    public double PixelRatio { get; }

    /// <summary>Monaco: <c>typicalHalfwidthCharacterWidth</c>.</summary>
    public double TypicalHalfwidthCharacterWidth { get; }

    /// <summary>Monaco: <c>lineHeight</c>.</summary>
    public int LineHeight { get; }

    /// <summary>Container left position (CSS px). Monaco: <c>minimapLeft</c>.</summary>
    public int MinimapLeft { get; }

    /// <summary>Container width (CSS px). Monaco: <c>minimapWidth</c>.</summary>
    public int MinimapWidth { get; }

    /// <summary>Container height (CSS px). Monaco: <c>minimapHeight</c>.</summary>
    public int MinimapHeight { get; }

    /// <summary>Canvas backing store width (device px). Monaco: <c>canvasInnerWidth</c>.</summary>
    public int CanvasInnerWidth { get; }

    /// <summary>Canvas backing store height (device px). Monaco: <c>canvasInnerHeight</c>.</summary>
    public int CanvasInnerHeight { get; }

    /// <summary>Canvas width (CSS px). Monaco: <c>canvasOuterWidth</c>.</summary>
    public double CanvasOuterWidth { get; }

    /// <summary>Canvas height (CSS px). Monaco: <c>canvasOuterHeight</c>.</summary>
    public double CanvasOuterHeight { get; }

    /// <summary>Monaco: <c>isSampling</c>.</summary>
    public bool IsSampling { get; }

    /// <summary>Monaco: <c>editorHeight</c>.</summary>
    public int EditorHeight { get; }

    /// <summary>Monaco: <c>fontScale</c>.</summary>
    public double FontScale { get; }

    /// <summary>Monaco: <c>minimapLineHeight</c>.</summary>
    public double MinimapLineHeight { get; }

    /// <summary>Monaco: <c>minimapCharWidth</c>.</summary>
    public double MinimapCharWidth { get; }

    /// <summary>The character renderer, created on first use. Monaco: <c>charRenderer</c> (<c>createSingleCallFunction</c>).</summary>
    public MinimapCharRenderer CharRenderer => _charRenderer.Value;

    /// <summary>Monaco: <c>defaultBackgroundColor</c>.</summary>
    public Rgba8 DefaultBackgroundColor { get; }

    /// <summary>Monaco: <c>backgroundColor</c>.</summary>
    public Rgba8 BackgroundColor { get; }

    /// <summary>Foreground alpha, an integer in 0–255. Monaco: <c>foregroundAlpha</c>.</summary>
    public int ForegroundAlpha { get; }

    /// <summary>Monaco: <c>equals</c>.</summary>
    public bool EqualsOptions(MinimapOptions other) =>
        RenderMinimap == other.RenderMinimap
        && Size == other.Size
        && MinimapHeightIsEditorHeight == other.MinimapHeightIsEditorHeight
        && ScrollBeyondLastLine == other.ScrollBeyondLastLine
        && PaddingTop == other.PaddingTop
        && PaddingBottom == other.PaddingBottom
        && ShowSlider == other.ShowSlider
        && Autohide == other.Autohide
        && PixelRatio == other.PixelRatio
        && TypicalHalfwidthCharacterWidth == other.TypicalHalfwidthCharacterWidth
        && LineHeight == other.LineHeight
        && MinimapLeft == other.MinimapLeft
        && MinimapWidth == other.MinimapWidth
        && MinimapHeight == other.MinimapHeight
        && CanvasInnerWidth == other.CanvasInnerWidth
        && CanvasInnerHeight == other.CanvasInnerHeight
        && CanvasOuterWidth == other.CanvasOuterWidth
        && CanvasOuterHeight == other.CanvasOuterHeight
        && IsSampling == other.IsSampling
        && EditorHeight == other.EditorHeight
        && FontScale == other.FontScale
        && MinimapLineHeight == other.MinimapLineHeight
        && MinimapCharWidth == other.MinimapCharWidth
        && DefaultBackgroundColor == other.DefaultBackgroundColor
        && BackgroundColor == other.BackgroundColor
        && ForegroundAlpha == other.ForegroundAlpha;

    private static Rgba8 GetMinimapBackground(EditorTheme theme, Rgba8 defaultBackgroundColor) =>
        theme.GetColor("minimap.background") is { } color ? new Rgba8(color.R, color.G, color.B, color.A) : defaultBackgroundColor;

    private static int GetMinimapForegroundOpacity(EditorTheme theme) =>
        theme.GetColor("minimap.foregroundOpacity") is { } color ? color.A : 255;
}
