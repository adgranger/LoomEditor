// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimapCharRendererFactory.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Minimap;

/// <summary>
/// Creates character renderers for a scale: pre-baked data for scales 1 and 2, font sample data (16-px glyphs, see
/// P6-45) downsampled for the others. Monaco: <c>MinimapCharRendererFactory</c>.
/// </summary>
/// <remarks>
/// Threading: UI thread only (the last created renderer is cached in static fields).
/// Behavior difference: Monaco's <c>createSampleData</c> (canvas) is not ported; the caller supplies the sample data.
/// </remarks>
internal static class MinimapCharRendererFactory
{
    private static MinimapCharRenderer? _lastCreated;
    private static string? _lastFontFamily;

    /// <summary>
    /// Creates (or returns the cached) renderer. Scales above 2 need <paramref name="sampleDataProvider"/> (font family to
    /// RGBA sample data); when it is <c>null</c> the pre-baked scale-2 data is used and the renderer has scale 2.
    /// </summary>
    public static MinimapCharRenderer Create(int scale, string fontFamily, Func<string, byte[]>? sampleDataProvider)
    {
        // Renderers are immutable: reuse the last one when nothing changed.
        if (_lastCreated is not null && scale == _lastCreated.Scale && fontFamily == _lastFontFamily)
        {
            return _lastCreated;
        }

        MinimapCharRenderer renderer;
        if (MinimapPreBaked.Get(scale) is { } prebaked)
        {
            renderer = new MinimapCharRenderer(prebaked, scale);
        }
        else if (sampleDataProvider is null)
        {
            renderer = new MinimapCharRenderer(MinimapPreBaked.Get(2)!, 2);
        }
        else
        {
            renderer = CreateFromSampleData(sampleDataProvider(fontFamily), scale);
        }

        _lastFontFamily = fontFamily;
        _lastCreated = renderer;
        return renderer;
    }

    /// <summary>Creates a character renderer from RGBA font sample data.</summary>
    public static MinimapCharRenderer CreateFromSampleData(byte[] source, int scale)
    {
        const int ExpectedLength = MinimapCharSheet.SampledCharHeight * MinimapCharSheet.SampledCharWidth
            * MinimapCharSheet.RgbaChannelsCnt * MinimapCharSheet.CharCount;
        if (source.Length != ExpectedLength)
        {
            throw new ArgumentException("Unexpected source in MinimapCharRenderer", nameof(source));
        }

        return new MinimapCharRenderer(Downsample(source, scale), scale);
    }

    private static double DownsampleChar(byte[] source, int sourceOffset, byte[] dest, int destOffset, int scale)
    {
        var width = MinimapCharSheet.BaseCharWidth * scale;
        var height = MinimapCharSheet.BaseCharHeight * scale;

        var targetIndex = destOffset;
        double brightest = 0;

        // Ad-hoc rescaling: map each source pixel to the target pixels, average the weighted values (see Monaco).
        for (var y = 0; y < height; y++)
        {
            var sourceY1 = (double)y / height * MinimapCharSheet.SampledCharHeight;
            var sourceY2 = (double)(y + 1) / height * MinimapCharSheet.SampledCharHeight;

            for (var x = 0; x < width; x++)
            {
                var sourceX1 = (double)x / width * MinimapCharSheet.SampledCharWidth;
                var sourceX2 = (double)(x + 1) / width * MinimapCharSheet.SampledCharWidth;

                double value = 0;
                double samples = 0;
                for (var sy = sourceY1; sy < sourceY2; sy++)
                {
                    var sourceRow = sourceOffset + ((int)Math.Floor(sy) * MinimapCharSheet.RgbaSampledRowWidth);
                    var yBalance = 1 - (sy - Math.Floor(sy));
                    for (var sx = sourceX1; sx < sourceX2; sx++)
                    {
                        var xBalance = 1 - (sx - Math.Floor(sx));
                        var sourceIndex = sourceRow + ((int)Math.Floor(sx) * MinimapCharSheet.RgbaChannelsCnt);

                        var weight = xBalance * yBalance;
                        samples += weight;
                        value += source[sourceIndex] * source[sourceIndex + 3] / 255.0 * weight;
                    }
                }

                var final = value / samples;
                brightest = Math.Max(brightest, final);
                dest[targetIndex++] = MinimapCharSheet.ClampToByte(MinimapCharSheet.ToUint8(final));
            }
        }

        return brightest;
    }

    private static byte[] Downsample(byte[] data, int scale)
    {
        var pixelsPerCharacter = MinimapCharSheet.BaseCharHeight * scale * MinimapCharSheet.BaseCharWidth * scale;
        var resultLen = pixelsPerCharacter * MinimapCharSheet.CharCount;
        var result = new byte[resultLen];

        var resultOffset = 0;
        var sourceOffset = 0;
        double brightest = 0;
        for (var charIndex = 0; charIndex < MinimapCharSheet.CharCount; charIndex++)
        {
            brightest = Math.Max(brightest, DownsampleChar(data, sourceOffset, result, resultOffset, scale));
            resultOffset += pixelsPerCharacter;
            sourceOffset += MinimapCharSheet.SampledCharWidth * MinimapCharSheet.RgbaChannelsCnt;
        }

        if (brightest > 0)
        {
            var adjust = 255 / brightest;
            for (var i = 0; i < resultLen; i++)
            {
                result[i] = MinimapCharSheet.ClampToByte(result[i] * adjust);
            }
        }

        return result;
    }
}
