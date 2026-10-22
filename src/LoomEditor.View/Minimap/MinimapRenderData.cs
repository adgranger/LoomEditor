// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (MinimapLine, RenderData, ContiguousLineMap)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Events;

namespace LoomEditor.View.Minimap;

/// <summary>A rendered minimap line: its <see cref="Dy"/> in the buffer, or -1 when it must be repainted. Monaco: <c>MinimapLine</c>.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class MinimapLine(double dy) : IRenderedLine
{
    /// <summary>Monaco: <c>MinimapLine.INVALID</c> (a new instance each time: the line is mutable).</summary>
    public static MinimapLine Invalid() => new(-1);

    /// <summary>Monaco: <c>dy</c>.</summary>
    public double Dy { get; private set; } = dy;

    /// <inheritdoc/>
    public void OnContentChanged() => Dy = -1;

    /// <inheritdoc/>
    public void OnTokensChanged() => Dy = -1;
}

/// <summary>What the last frame painted, to reuse it. Monaco: <c>RenderData</c>.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class MinimapRenderData
{
    private readonly RenderedLinesCollection<MinimapLine> _renderedLines = new(MinimapLine.Invalid);

    /// <summary>Builds the data of a frame.</summary>
    public MinimapRenderData(MinimapLayout renderedLayout, MinimapImageData imageData, List<MinimapLine> lines)
    {
        RenderedLayout = renderedLayout;
        ImageData = imageData;
        _renderedLines.Set(renderedLayout.StartLineNumber, lines);
    }

    /// <summary>The last rendered layout. Monaco: <c>renderedLayout</c>.</summary>
    public MinimapLayout RenderedLayout { get; }

    /// <summary>The painted pixels. Monaco: <c>_imageData</c>.</summary>
    public MinimapImageData ImageData { get; }

    /// <summary>The first rendered line number and the rendered lines. Monaco: <c>_get</c>.</summary>
    public (int RendLineNumberStart, List<MinimapLine> Lines) GetLines() => _renderedLines.Get();

    /// <summary>Whether this data matches the new layout and no painting is needed. Monaco: <c>linesEquals</c>.</summary>
    public bool LinesEquals(MinimapLayout layout)
    {
        if (!ScrollEquals(layout))
        {
            return false;
        }

        foreach (MinimapLine line in _renderedLines.Get().Lines)
        {
            if (line.Dy == -1)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Whether this data matches the scroll position of the new layout. Monaco: <c>scrollEquals</c>.</summary>
    public bool ScrollEquals(MinimapLayout layout) =>
        RenderedLayout.StartLineNumber == layout.StartLineNumber && RenderedLayout.EndLineNumber == layout.EndLineNumber;

    /// <summary>Monaco: <c>onLinesChanged</c>.</summary>
    public bool OnLinesChanged(int changeFromLineNumber, int changeCount) => _renderedLines.OnLinesChanged(changeFromLineNumber, changeCount);

    /// <summary>Monaco: <c>onLinesDeleted</c>.</summary>
    public void OnLinesDeleted(int deleteFromLineNumber, int deleteToLineNumber) => _renderedLines.OnLinesDeleted(deleteFromLineNumber, deleteToLineNumber);

    /// <summary>Monaco: <c>onLinesInserted</c>.</summary>
    public void OnLinesInserted(int insertFromLineNumber, int insertToLineNumber) => _renderedLines.OnLinesInserted(insertFromLineNumber, insertToLineNumber);

    /// <summary>Monaco: <c>onTokensChanged</c>.</summary>
    public bool OnTokensChanged(IReadOnlyList<ViewLineRange> ranges) => _renderedLines.OnTokensChanged(ranges);
}

/// <summary>A value per line number of a range, with a default outside of it. Monaco: <c>ContiguousLineMap</c>.</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class ContiguousLineMap<T>
    where T : IEquatable<T>
{
    private readonly int _startLineNumber;
    private readonly int _endLineNumber;
    private readonly T _defaultValue;
    private readonly T[] _values;

    /// <summary>Builds a map of <paramref name="startLineNumber"/>..<paramref name="endLineNumber"/> filled with <paramref name="defaultValue"/>.</summary>
    public ContiguousLineMap(int startLineNumber, int endLineNumber, T defaultValue)
    {
        _startLineNumber = startLineNumber;
        _endLineNumber = endLineNumber;
        _defaultValue = defaultValue;
        _values = new T[Math.Max(0, endLineNumber - startLineNumber + 1)];
        Array.Fill(_values, defaultValue);
    }

    /// <summary>Monaco: <c>has</c>.</summary>
    public bool Has(int lineNumber) => !Get(lineNumber).Equals(_defaultValue);

    /// <summary>Monaco: <c>set</c>.</summary>
    public void Set(int lineNumber, T value)
    {
        if (lineNumber < _startLineNumber || lineNumber > _endLineNumber)
        {
            return;
        }

        _values[lineNumber - _startLineNumber] = value;
    }

    /// <summary>Monaco: <c>get</c>.</summary>
    public T Get(int lineNumber) =>
        lineNumber < _startLineNumber || lineNumber > _endLineNumber ? _defaultValue : _values[lineNumber - _startLineNumber];
}
