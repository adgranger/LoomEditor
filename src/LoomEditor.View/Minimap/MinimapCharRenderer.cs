// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimapCharRenderer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Minimap;

/// <summary>Paints minimap characters and blocks into an RGBA buffer from character sheet data. Monaco: <c>MinimapCharRenderer</c>.</summary>
/// <remarks>
/// Threading: immutable after construction; <c>RenderChar</c> and <c>BlockRenderChar</c> only write to the target.
/// Behavior difference: a render request outside the target is ignored silently (Monaco logs a console warning).
/// </remarks>
internal sealed class MinimapCharRenderer
{
    private readonly byte[] _charDataNormal;
    private readonly byte[] _charDataLight;

    public MinimapCharRenderer(byte[] charData, int scale)
    {
        Scale = scale;
        _charDataNormal = Soften(charData, 12.0 / 15);
        _charDataLight = Soften(charData, 50.0 / 60);
    }

    public int Scale { get; }

    private static byte[] Soften(byte[] input, double ratio)
    {
        var result = new byte[input.Length];
        for (var i = 0; i < input.Length; i++)
        {
            result[i] = MinimapCharSheet.ClampToByte(MinimapCharSheet.ToUint8(input[i] * ratio));
        }

        return result;
    }

    public void RenderChar(
        MinimapImageData target,
        int dx,
        int dy,
        int chCode,
        Rgba8 color,
        int foregroundAlpha,
        Rgba8 backgroundColor,
        int backgroundAlpha,
        int fontScale,
        bool useLighterFont,
        bool force1pxHeight)
    {
        var charWidth = MinimapCharSheet.BaseCharWidth * Scale;
        var charHeight = MinimapCharSheet.BaseCharHeight * Scale;
        var renderHeight = force1pxHeight ? 1 : charHeight;
        if (dx + charWidth > target.Width || dy + renderHeight > target.Height)
        {
            return;
        }

        var charData = useLighterFont ? _charDataLight : _charDataNormal;
        var charIndex = MinimapCharSheet.GetCharIndex(chCode, fontScale);

        var destWidth = target.Width * MinimapCharSheet.RgbaChannelsCnt;

        int backgroundR = backgroundColor.R;
        int backgroundG = backgroundColor.G;
        int backgroundB = backgroundColor.B;

        var deltaR = color.R - backgroundR;
        var deltaG = color.G - backgroundG;
        var deltaB = color.B - backgroundB;

        var destAlpha = Math.Max(foregroundAlpha, backgroundAlpha);

        var dest = target.Data;
        var sourceOffset = charIndex * charWidth * charHeight;

        var row = (dy * destWidth) + (dx * MinimapCharSheet.RgbaChannelsCnt);
        for (var y = 0; y < renderHeight; y++)
        {
            var column = row;
            for (var x = 0; x < charWidth; x++)
            {
                var c = charData[sourceOffset++] / 255.0 * (foregroundAlpha / 255.0);
                dest[column++] = MinimapCharSheet.ClampToByte(backgroundR + (deltaR * c));
                dest[column++] = MinimapCharSheet.ClampToByte(backgroundG + (deltaG * c));
                dest[column++] = MinimapCharSheet.ClampToByte(backgroundB + (deltaB * c));
                dest[column++] = MinimapCharSheet.ClampToByte(destAlpha);
            }

            row += destWidth;
        }
    }

    public void BlockRenderChar(
        MinimapImageData target,
        int dx,
        int dy,
        Rgba8 color,
        int foregroundAlpha,
        Rgba8 backgroundColor,
        int backgroundAlpha,
        bool force1pxHeight)
    {
        var charWidth = MinimapCharSheet.BaseCharWidth * Scale;
        var charHeight = MinimapCharSheet.BaseCharHeight * Scale;
        var renderHeight = force1pxHeight ? 1 : charHeight;
        if (dx + charWidth > target.Width || dy + renderHeight > target.Height)
        {
            return;
        }

        var destWidth = target.Width * MinimapCharSheet.RgbaChannelsCnt;

        var c = 0.5 * (foregroundAlpha / 255.0);

        int backgroundR = backgroundColor.R;
        int backgroundG = backgroundColor.G;
        int backgroundB = backgroundColor.B;

        var deltaR = color.R - backgroundR;
        var deltaG = color.G - backgroundG;
        var deltaB = color.B - backgroundB;

        var colorR = MinimapCharSheet.ClampToByte(backgroundR + (deltaR * c));
        var colorG = MinimapCharSheet.ClampToByte(backgroundG + (deltaG * c));
        var colorB = MinimapCharSheet.ClampToByte(backgroundB + (deltaB * c));

        var destAlpha = MinimapCharSheet.ClampToByte(Math.Max(foregroundAlpha, backgroundAlpha));

        var dest = target.Data;

        var row = (dy * destWidth) + (dx * MinimapCharSheet.RgbaChannelsCnt);
        for (var y = 0; y < renderHeight; y++)
        {
            var column = row;
            for (var x = 0; x < charWidth; x++)
            {
                dest[column++] = colorR;
                dest[column++] = colorG;
                dest[column++] = colorB;
                dest[column++] = destAlpha;
            }

            row += destWidth;
        }
    }
}
