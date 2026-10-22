// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>How the minimap is rendered. Monaco: <c>RenderMinimap</c>.</summary>
internal enum RenderMinimap
{
    /// <summary>No minimap.</summary>
    None = 0,

    /// <summary>Characters and blocks.</summary>
    Text = 1,

    /// <summary>Blocks only.</summary>
    Blocks = 2,
}

/// <summary>The minimap part of the editor layout. Monaco: <c>EditorMinimapLayoutInfo</c>.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed record EditorMinimapLayoutInfo(
    RenderMinimap RenderMinimap,
    int MinimapLeft,
    int MinimapWidth,
    bool MinimapHeightIsEditorHeight,
    bool MinimapIsSampling,
    double MinimapScale,
    int MinimapLineHeight,
    int MinimapCanvasInnerWidth,
    int MinimapCanvasInnerHeight,
    int MinimapCanvasOuterWidth,
    int MinimapCanvasOuterHeight);

/// <summary>The input of the minimap layout computation. Monaco: <c>IMinimapLayoutInput</c>.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed record MinimapLayoutInput(
    int OuterWidth,
    int OuterHeight,
    int LineHeight,
    double TypicalHalfwidthCharacterWidth,
    double PixelRatio,
    bool ScrollBeyondLastLine,
    int PaddingTop,
    int PaddingBottom,
    EditorMinimapOptions Minimap,
    int VerticalScrollbarWidth,
    int ViewLineCount,
    int RemainingWidth,
    bool IsViewportWrapping);

/// <summary>State Monaco keeps across layouts to keep the minimap stable. Monaco: <c>ComputeOptionsMemory</c>.</summary>
/// <remarks>Threading: mutable, not thread-safe; owned by the layout computation.</remarks>
internal sealed class ComputeOptionsMemory
{
    /// <summary>Monaco: <c>stableMinimapLayoutInput</c>.</summary>
    public MinimapLayoutInput? StableMinimapLayoutInput { get; set; }

    /// <summary>Monaco: <c>stableFitMaxMinimapScale</c>.</summary>
    public double StableFitMaxMinimapScale { get; set; }

    /// <summary>Monaco: <c>stableFitRemainingWidth</c>.</summary>
    public int StableFitRemainingWidth { get; set; }
}
