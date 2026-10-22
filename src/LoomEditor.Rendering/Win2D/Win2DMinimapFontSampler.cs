// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimapCharRendererFactory.ts (createSampleData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Minimap;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI;
using Windows.UI.Text;

namespace LoomEditor.Rendering.Win2D;

/// <summary>
/// Samples the editor font for minimap scales above 2: every character of the minimap sheet drawn bold at 16 px in a
/// 10 px cell, read back as RGBA bytes. Monaco: <c>createSampleData</c> (canvas).
/// </summary>
/// <remarks>
/// Behavior difference: glyphs are rasterized by DirectWrite, not by Chromium's canvas, so the bytes are not identical to Monaco's.
/// Threading: UI thread only.
/// </remarks>
internal static class Win2DMinimapFontSampler
{
    /// <summary>Draws the sheet and returns <c>SampledCharHeight * SampledCharWidth * 4 * CharCount</c> RGBA bytes.</summary>
    public static byte[] CreateSampleData(CanvasDevice device, string fontFamily)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(fontFamily);

        using var target = new CanvasRenderTarget(
            device, MinimapCharSheet.CharCount * MinimapCharSheet.SampledCharWidth, MinimapCharSheet.SampledCharHeight, 96);
        using var format = new CanvasTextFormat
        {
            FontFamily = fontFamily,
            FontSize = MinimapCharSheet.SampledCharHeight,
            FontWeight = new FontWeight { Weight = 700 },
            WordWrapping = CanvasWordWrapping.NoWrap,
            VerticalAlignment = CanvasVerticalAlignment.Center,
        };

        using (CanvasDrawingSession session = target.CreateDrawingSession())
        {
            session.Clear(Color.FromArgb(0, 0, 0, 0));
            for (int i = 0; i < MinimapCharSheet.AllCharCodes.Length; i++)
            {
                string character = char.ConvertFromUtf32(MinimapCharSheet.AllCharCodes[i]);
                var rect = new Windows.Foundation.Rect(i * MinimapCharSheet.SampledCharWidth, 0, MinimapCharSheet.SampledCharWidth * 2, MinimapCharSheet.SampledCharHeight);
                session.DrawText(character, rect, Color.FromArgb(255, 255, 255, 255), format);
            }
        }

        return MinimapSampleConversion.ConvertBgraPremultipliedToRgba(target.GetPixelBytes());
    }
}
