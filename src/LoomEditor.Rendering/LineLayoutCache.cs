namespace LoomEditor.Rendering;

/// <summary>
/// A least-recently-used cache of line layouts, keyed by the text of the line and its <see cref="LineStyle"/>.
/// </summary>
/// <remarks>
/// <para>
/// Unchanged lines keep the same text, so their layouts survive scrolling and edits (ADR 0005, decision 4).
/// The cache owns the layouts: callers must not dispose what <see cref="Get(string, LineStyle)"/> returns, and must not use it after
/// the next call to <see cref="Get(string, LineStyle)"/> that could evict it (use it within one frame).
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class LineLayoutCache : IDisposable
{
    private readonly ITextMeasurer _measurer;
    private readonly Dictionary<(string Text, LineStyle Style), LinkedListNode<Entry>> _nodes = [];
    private readonly LinkedList<Entry> _recentFirst = new();
    private int _capacity;

    /// <summary>Creates an empty cache.</summary>
    /// <param name="measurer">Creates the layouts.</param>
    /// <param name="capacity">Maximum number of layouts kept; at least 1.</param>
    public LineLayoutCache(ITextMeasurer measurer, int capacity)
    {
        ArgumentNullException.ThrowIfNull(measurer);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        _measurer = measurer;
        _capacity = capacity;
    }

    /// <summary>Number of layouts currently cached.</summary>
    public int Count => _nodes.Count;

    /// <summary>Returns the plain layout of <paramref name="text"/> (no colors), creating it if needed. The cache owns it.</summary>
    public ILineLayout Get(string text) => Get(text, LineStyle.Plain);

    /// <summary>Returns the layout of <paramref name="text"/> drawn with <paramref name="style"/>, creating it if needed. The cache owns it.</summary>
    public ILineLayout Get(string text, LineStyle style)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(style);

        if (_nodes.TryGetValue((text, style), out LinkedListNode<Entry>? node))
        {
            _recentFirst.Remove(node);
            _recentFirst.AddFirst(node);
            return node.Value.Layout;
        }

        if (_nodes.Count >= _capacity)
        {
            EvictLeastRecentlyUsed();
        }

        ILineLayout layout = _measurer.CreateLineLayout(text, style);
        _nodes.Add((text, style), _recentFirst.AddFirst(new Entry(text, style, layout)));
        return layout;
    }

    /// <summary>Makes the cache hold at least <paramref name="capacity"/> layouts. It never shrinks.</summary>
    public void EnsureCapacity(int capacity)
    {
        if (capacity > _capacity)
        {
            _capacity = capacity;
        }
    }

    /// <summary>Disposes and forgets every layout (font or DPI change).</summary>
    public void Clear()
    {
        foreach (Entry entry in _recentFirst)
        {
            entry.Layout.Dispose();
        }

        _recentFirst.Clear();
        _nodes.Clear();
    }

    /// <inheritdoc />
    public void Dispose() => Clear();

    private void EvictLeastRecentlyUsed()
    {
        LinkedListNode<Entry>? last = _recentFirst.Last;
        if (last is null)
        {
            return;
        }

        _recentFirst.RemoveLast();
        _nodes.Remove((last.Value.Text, last.Value.Style));
        last.Value.Layout.Dispose();
    }

    private sealed record Entry(string Text, LineStyle Style, ILineLayout Layout);
}
