// Ported from microsoft/vscode: src/vs/editor/common/core/misc/rgba.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Minimap;

/// <summary>An RGBA color with integer channels in 0–255. Monaco: <c>RGBA8</c>.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal readonly record struct Rgba8(byte R, byte G, byte B, byte A)
{
    public static readonly Rgba8 Empty = new(0, 0, 0, 0);

    /// <summary>Builds a color clamping (and truncating) each channel like Monaco's constructor.</summary>
    public static Rgba8 Create(double r, double g, double b, double a) => new(Clamp(r), Clamp(g), Clamp(b), Clamp(a));

    /// <summary>Monaco: <c>_clamp</c>.</summary>
    public static byte Clamp(double c) => c < 0 ? (byte)0 : c > 255 ? (byte)255 : (byte)(int)c;
}
