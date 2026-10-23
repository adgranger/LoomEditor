// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (MinimapBuffers)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Minimap;

/// <summary>Two pixel buffers rotated for painting, each handed out filled with the background color. Monaco: <c>MinimapBuffers</c>.</summary>
/// <remarks>Threading: UI thread only. Behavior difference: the buffers are <see cref="MinimapImageData"/> instead of canvas <c>ImageData</c>.</remarks>
internal sealed class MinimapBuffers
{
    private readonly byte[] _backgroundFillData;
    private readonly MinimapImageData[] _buffers;
    private int _lastUsedBuffer;

    /// <summary>Builds the two buffers of <paramref name="width"/> × <paramref name="height"/> pixels.</summary>
    public MinimapBuffers(int width, int height, Rgba8 background)
    {
        _backgroundFillData = new byte[width * height * MinimapCharSheet.RgbaChannelsCnt];
        for (int offset = 0; offset < _backgroundFillData.Length; offset += MinimapCharSheet.RgbaChannelsCnt)
        {
            _backgroundFillData[offset] = background.R;
            _backgroundFillData[offset + 1] = background.G;
            _backgroundFillData[offset + 2] = background.B;
            _backgroundFillData[offset + 3] = background.A;
        }

        _buffers = [new MinimapImageData(width, height), new MinimapImageData(width, height)];
    }

    /// <summary>Rotates the buffers and returns the next one, filled with the background color. Monaco: <c>getBuffer</c>.</summary>
    public MinimapImageData GetBuffer()
    {
        _lastUsedBuffer = 1 - _lastUsedBuffer;
        MinimapImageData result = _buffers[_lastUsedBuffer];
        _backgroundFillData.CopyTo(result.Data, 0);
        return result;
    }
}
