// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (InnerMinimap.renderLines)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Minimap;

/// <summary>Paints the minimap lines into a pixel buffer, reusing the lines of the previous frame. Monaco: the lines part of <c>InnerMinimap</c>.</summary>
/// <remarks>
/// Threading: UI thread only. Behavior difference: <see cref="RenderLines"/> returns the buffer to draw instead of calling
/// <c>putImageData</c> on a canvas (the upload is the drawing code's job); the dirty rectangle is therefore not computed.
/// </remarks>
internal sealed class MinimapLinesRenderer(IMinimapModel model)
{
    private MinimapRenderData? _lastRenderData;
    private MinimapBuffers? _buffers;

    /// <summary>The options changed (Monaco: <c>onDidChangeOptions</c>, the lines part).</summary>
    public void OnDidChangeOptions()
    {
        _lastRenderData = null;
        _buffers = null;
    }

    /// <summary>Everything changed (Monaco: <c>onFlushed</c>).</summary>
    public void OnFlushed() => _lastRenderData = null;

    /// <summary>The token colors changed (Monaco: <c>onTokensColorsChanged</c>).</summary>
    public void OnTokensColorsChanged()
    {
        _lastRenderData = null;
        _buffers = null;
    }

    /// <summary>Monaco: <c>onLinesChanged</c>.</summary>
    public bool OnLinesChanged(int changeFromLineNumber, int changeCount) =>
        _lastRenderData?.OnLinesChanged(changeFromLineNumber, changeCount) ?? false;

    /// <summary>Monaco: <c>onLinesDeleted</c>.</summary>
    public void OnLinesDeleted(int deleteFromLineNumber, int deleteToLineNumber) =>
        _lastRenderData?.OnLinesDeleted(deleteFromLineNumber, deleteToLineNumber);

    /// <summary>Monaco: <c>onLinesInserted</c>.</summary>
    public void OnLinesInserted(int insertFromLineNumber, int insertToLineNumber) =>
        _lastRenderData?.OnLinesInserted(insertFromLineNumber, insertToLineNumber);

    /// <summary>Monaco: <c>onTokensChanged</c>.</summary>
    public bool OnTokensChanged(IReadOnlyList<ViewLineRange> ranges) => _lastRenderData?.OnTokensChanged(ranges) ?? false;

    /// <summary>Monaco: <c>onZonesChanged</c>.</summary>
    public void OnZonesChanged() => _lastRenderData = null;

    /// <summary>Paints the lines of <paramref name="layout"/>; <c>null</c> for a canvas of no pixels. Monaco: <c>renderLines</c>.</summary>
    public MinimapImageData? RenderLines(MinimapLayout layout)
    {
        MinimapOptions options = model.Options;
        int startLineNumber = layout.StartLineNumber;
        int endLineNumber = layout.EndLineNumber;
        int minimapLineHeight = (int)options.MinimapLineHeight;

        // Check if nothing changed w.r.t. lines from last frame
        if (_lastRenderData is not null && _lastRenderData.LinesEquals(layout))
        {
            (_, List<MinimapLine> lastLines) = _lastRenderData.GetLines();
            _lastRenderData = new MinimapRenderData(layout, _lastRenderData.ImageData, lastLines);
            return _lastRenderData.ImageData;
        }

        MinimapImageData? imageData = GetBuffer();
        if (imageData is null)
        {
            // 0 width or 0 height canvas, nothing to do
            return null;
        }

        // Render untouched lines by using last rendered data.
        bool[] needed = RenderUntouchedLines(imageData, layout.TopPaddingLineCount, startLineNumber, endLineNumber, minimapLineHeight, _lastRenderData);

        // Fetch rendering info from the model for the rest of the lines that need rendering.
        ViewLineData?[] lineInfo = model.GetMinimapLinesRenderingData(startLineNumber, endLineNumber, needed);
        int tabSize = model.GetOptions().TabSize;
        Rgba8 defaultBackground = options.DefaultBackgroundColor;
        Rgba8 background = options.BackgroundColor;
        MinimapTokensColorTracker tokensColorTracker = model.TokensColorTracker;
        bool useLighterFont = tokensColorTracker.BackgroundIsLight();
        RenderMinimap renderMinimap = options.RenderMinimap;
        MinimapCharRenderer charRenderer = options.CharRenderer;
        int fontScale = (int)options.FontScale;
        int minimapCharWidth = (int)options.MinimapCharWidth;

        int baseCharHeight = renderMinimap == RenderMinimap.Text ? MinimapCharSheet.BaseCharHeight : MinimapCharSheet.BaseCharHeight + 1;
        int renderMinimapLineHeight = baseCharHeight * fontScale;
        int innerLinePadding = minimapLineHeight > renderMinimapLineHeight ? (minimapLineHeight - renderMinimapLineHeight) / 2 : 0;

        double backgroundA = background.A / 255.0;
        var renderBackground = new Rgba8(
            RoundToByte((background.R - defaultBackground.R) * backgroundA + defaultBackground.R),
            RoundToByte((background.G - defaultBackground.G) * backgroundA + defaultBackground.G),
            RoundToByte((background.B - defaultBackground.B) * backgroundA + defaultBackground.B),
            255);

        int dy = layout.TopPaddingLineCount * minimapLineHeight;
        var renderedLines = new List<MinimapLine>();
        for (int lineIndex = 0, lineCount = endLineNumber - startLineNumber + 1; lineIndex < lineCount; lineIndex++)
        {
            if (needed[lineIndex])
            {
                RenderLine(
                    imageData, renderBackground, background.A, useLighterFont, renderMinimap, minimapCharWidth, tokensColorTracker,
                    options.ForegroundAlpha, charRenderer, dy, innerLinePadding, tabSize, lineInfo[lineIndex]!, fontScale, minimapLineHeight);
            }

            renderedLines.Add(new MinimapLine(dy));
            dy += minimapLineHeight;
        }

        imageData.Version++;

        // Save rendered data for reuse on next frame if possible
        _lastRenderData = new MinimapRenderData(layout, imageData, renderedLines);
        return imageData;
    }

    // Math.round is half up in JavaScript; the operands here are never negative once clamped to a color.
    private static byte RoundToByte(double value) => Rgba8.Clamp(Math.Round(value, MidpointRounding.AwayFromZero));

    private MinimapImageData? GetBuffer()
    {
        MinimapOptions options = model.Options;
        if (_buffers is null && options.CanvasInnerWidth > 0 && options.CanvasInnerHeight > 0)
        {
            _buffers = new MinimapBuffers(options.CanvasInnerWidth, options.CanvasInnerHeight, options.BackgroundColor);
        }

        return _buffers?.GetBuffer();
    }

    /// <summary>Copies the lines that did not change from the last frame; returns which lines still need rendering. Monaco: <c>_renderUntouchedLines</c>.</summary>
    /// <remarks>Behavior difference: the dirty rows Monaco returns for <c>putImageData</c> are not computed.</remarks>
    private static bool[] RenderUntouchedLines(
        MinimapImageData target, int topPaddingLineCount, int startLineNumber, int endLineNumber, int minimapLineHeight, MinimapRenderData? lastRenderData)
    {
        var needed = new bool[endLineNumber - startLineNumber + 1];
        if (lastRenderData is null)
        {
            Array.Fill(needed, true);
            return needed;
        }

        (int lastStartLineNumber, List<MinimapLine> lastLines) = lastRenderData.GetLines();
        byte[] lastTargetData = lastRenderData.ImageData.Data;
        int width = target.Width;
        byte[] targetData = target.Data;

        int copySourceStart = -1;
        int copySourceEnd = -1;
        int copyDestStart = -1;
        int copyDestEnd = -1;

        int destDy = topPaddingLineCount * minimapLineHeight;
        for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
        {
            int lineIndex = lineNumber - startLineNumber;
            int lastLineIndex = lineNumber - lastStartLineNumber;
            int sourceDy = lastLineIndex >= 0 && lastLineIndex < lastLines.Count ? (int)lastLines[lastLineIndex].Dy : -1;
            if (sourceDy == -1)
            {
                needed[lineIndex] = true;
                destDy += minimapLineHeight;
                continue;
            }

            int sourceStart = sourceDy * width * 4;
            int sourceEnd = (sourceDy + minimapLineHeight) * width * 4;
            int destStart = destDy * width * 4;
            int destEnd = (destDy + minimapLineHeight) * width * 4;

            if (copySourceEnd == sourceStart && copyDestEnd == destStart)
            {
                // contiguous zone => extend copy request
                copySourceEnd = sourceEnd;
                copyDestEnd = destEnd;
            }
            else
            {
                if (copySourceStart != -1)
                {
                    Array.Copy(lastTargetData, copySourceStart, targetData, copyDestStart, copySourceEnd - copySourceStart);
                }

                copySourceStart = sourceStart;
                copySourceEnd = sourceEnd;
                copyDestStart = destStart;
                copyDestEnd = destEnd;
            }

            needed[lineIndex] = false;
            destDy += minimapLineHeight;
        }

        if (copySourceStart != -1)
        {
            Array.Copy(lastTargetData, copySourceStart, targetData, copyDestStart, copySourceEnd - copySourceStart);
        }

        return needed;
    }

    /// <summary>Paints one line. Monaco: <c>_renderLine</c>.</summary>
    private static void RenderLine(
        MinimapImageData target,
        Rgba8 backgroundColor,
        int backgroundAlpha,
        bool useLighterFont,
        RenderMinimap renderMinimap,
        int charWidth,
        MinimapTokensColorTracker colorTracker,
        int foregroundAlpha,
        MinimapCharRenderer minimapCharRenderer,
        int dy,
        int innerLinePadding,
        int tabSize,
        ViewLineData lineData,
        int fontScale,
        int minimapLineHeight)
    {
        string content = lineData.Content;
        IViewLineTokens tokens = lineData.Tokens;
        int maxDx = target.Width - charWidth;
        bool force1pxHeight = minimapLineHeight == 1;

        int dx = MinimapGutterWidth;
        int charIndex = 0;
        int tabsCharDelta = 0;

        for (int tokenIndex = 0, tokensLen = tokens.GetCount(); tokenIndex < tokensLen; tokenIndex++)
        {
            int tokenEndIndex = tokens.GetEndOffset(tokenIndex);
            Rgba8 tokenColor = colorTracker.GetColor(tokens.GetForeground(tokenIndex));

            for (; charIndex < tokenEndIndex; charIndex++)
            {
                if (dx > maxDx)
                {
                    // hit edge of minimap
                    return;
                }

                char charCode = content[charIndex];
                if (charCode == '\t')
                {
                    int insertSpacesCount = tabSize - ((charIndex + tabsCharDelta) % tabSize);
                    tabsCharDelta += insertSpacesCount - 1;
                    // No need to render anything since tab is invisible
                    dx += insertSpacesCount * charWidth;
                }
                else if (charCode == ' ')
                {
                    // No need to render anything since space is invisible
                    dx += charWidth;
                }
                else
                {
                    // Render twice for a full width character
                    int count = Strings.IsFullWidthCharacter(charCode) ? 2 : 1;
                    for (int i = 0; i < count; i++)
                    {
                        if (renderMinimap == RenderMinimap.Blocks)
                        {
                            minimapCharRenderer.BlockRenderChar(target, dx, dy + innerLinePadding, tokenColor, foregroundAlpha, backgroundColor, backgroundAlpha, force1pxHeight);
                        }
                        else
                        {
                            minimapCharRenderer.RenderChar(
                                target, dx, dy + innerLinePadding, charCode, tokenColor, foregroundAlpha, backgroundColor, backgroundAlpha, fontScale, useLighterFont, force1pxHeight);
                        }

                        dx += charWidth;
                        if (dx > maxDx)
                        {
                            // hit edge of minimap
                            return;
                        }
                    }
                }
            }
        }
    }

    /// <summary>Monaco: <c>MINIMAP_GUTTER_WIDTH</c>.</summary>
    private const int MinimapGutterWidth = 8;
}
