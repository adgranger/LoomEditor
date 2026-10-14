namespace LoomEditor.Rendering;

/// <summary>
/// A least-recently-used cache of line layouts, keyed by the text of the line.
/// </summary>
/// <remarks>
/// <para>
/// Unchanged lines keep the same text, so their layouts survive scrolling and edits (ADR 0005, decision 4).
/// The cache owns the layouts: callers must not dispose what <see cref="Get"/> returns, and must not use it after
/// the next call to <see cref="Get"/> that could evict it (use it within one frame).
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class LineLayoutCache : IDisposable
{
    private readonly ITextMeasurer _measurer;
    private readonly Dictionary<string, LinkedListNode<ILineLayout>> _nodes = new(StringComparer.Ordinal);
    private readonly LinkedList<ILineLayout> _recentFirst = new();
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

    /// <summary>Returns the layout of <paramref name="text"/>, creating it if needed. The cache owns it.</summary>
    public ILineLayout Get(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (_nodes.TryGetValue(text, out LinkedListNode<ILineLayout>? node))
        {
            _recentFirst.Remove(node);
            _recentFirst.AddFirst(node);
            return node.Value;
        }

        if (_nodes.Count >= _capacity)
        {
            EvictLeastRecentlyUsed();
        }

        ILineLayout layout = _measurer.CreateLineLayout(text);
        _nodes.Add(text, _recentFirst.AddFirst(layout));
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
        foreach (ILineLayout layout in _recentFirst)
        {
            layout.Dispose();
        }

        _recentFirst.Clear();
        _nodes.Clear();
    }

    /// <inheritdoc />
    public void Dispose() => Clear();

    private void EvictLeastRecentlyUsed()
    {
        LinkedListNode<ILineLayout>? last = _recentFirst.Last;
        if (last is null)
        {
            return;
        }

        _recentFirst.RemoveLast();
        _nodes.Remove(last.Value.Text);
        last.Value.Dispose();
    }
}
