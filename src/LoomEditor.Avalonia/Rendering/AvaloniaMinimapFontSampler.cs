// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimapCharRendererFactory.ts (createSampleData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using LoomEditor.View.Minimap;

namespace LoomEditor.Avalonia.Rendering;

/// <summary>
/// Samples the editor font for minimap scales above 2: every character of the minimap sheet drawn bold at 16 px in a
/// 10 px cell, read back as RGBA bytes. Monaco: <c>createSampleData</c> (canvas).
/// </summary>
/// <remarks>
/// Behavior difference: glyphs are rasterized by Skia, not by Chromium's canvas, so the bytes are not identical to Monaco's.
/// Threading: UI thread only.
/// </remarks>
internal static class AvaloniaMinimapFontSampler
{
    /// <summary>Draws the sheet and returns <c>SampledCharHeight * SampledCharWidth * 4 * CharCount</c> RGBA bytes.</summary>
    public static byte[] CreateSampleData(string fontFamily)
    {
        ArgumentNullException.ThrowIfNull(fontFamily);

        int width = MinimapCharSheet.CharCount * MinimapCharSheet.SampledCharWidth;
        int height = MinimapCharSheet.SampledCharHeight;
        var typeface = new Typeface(new FontFamily(fontFamily), FontStyle.Normal, FontWeight.Bold);
        var white = new ImmutableSolidColorBrush(Colors.White);

        using var target = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        using (DrawingContext context = target.CreateDrawingContext())
        {
            for (int i = 0; i < MinimapCharSheet.AllCharCodes.Length; i++)
            {
                string character = char.ConvertFromUtf32(MinimapCharSheet.AllCharCodes[i]);
                using var layout = new TextLayout(
                    character, typeface, MinimapCharSheet.SampledCharHeight, white);
                // Vertically centered in the cell.
                layout.Draw(context, new Point(i * MinimapCharSheet.SampledCharWidth, (height - layout.Height) / 2));
            }
        }

        byte[] bgra = new byte[width * height * 4];
        GCHandle handle = GCHandle.Alloc(bgra, GCHandleType.Pinned);
        try
        {
            target.CopyPixels(new PixelRect(0, 0, width, height), handle.AddrOfPinnedObject(), bgra.Length, width * 4);
        }
        finally
        {
            handle.Free();
        }

        return MinimapSampleConversion.ConvertBgraPremultipliedToRgba(bgra);
    }
}
