using LoomEditor.Core.Comments;
using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Comments;

/// <summary>
/// Owns the comment threads of one model: a tracked decoration per thread so it follows its line, and a
/// <see cref="ViewZoneKind.Widget"/> view zone after that line (the gap a host element covers).
/// </summary>
/// <remarks>
/// Not a port: Monaco has no comment widget (ADR 0014). A deleted line collapses the decoration; the thread then stays
/// on the line that took its place. Threading: UI thread only.
/// </remarks>
internal sealed class CommentThreadManager : IDisposable
{
    /// <summary>The height, in pixels, of a new thread's zone.</summary>
    public const int DefaultHeight = 120;

    private const int OrdinalBase = 20000;

    private static readonly ModelDecorationOptions AnchorOptions = ModelDecorationOptions.Register(
        new ModelDecorationOptionsData { Description = "comment-thread-anchor", Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges });

    private readonly TextModel _model;
    private readonly ViewZones _zones;
    private readonly int _ownerId;
    private readonly List<Entry> _entries = [];
    private int _counter;
    private bool _busy;
    private bool _disposed;

    public CommentThreadManager(TextModel model, ViewZones zones, int decorationOwnerId)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(zones);
        _model = model;
        _zones = zones;
        _ownerId = decorationOwnerId;
        _model.DecorationsChanged += OnDecorationsChanged;
        _model.ContentChanged += OnContentChanged;
    }

    /// <summary>Raised after a thread was added or removed.</summary>
    public event EventHandler? ThreadsChanged;

    /// <summary>Raised once per thread whose line changed because of an edit.</summary>
    public event EventHandler<CommentThread>? ThreadMoved;

    /// <summary>Raised when the content of a thread's line changed.</summary>
    public event EventHandler<CommentThread>? ThreadLineTextChanged;

    /// <summary>The threads, in creation order.</summary>
    public IReadOnlyList<CommentThread> Threads => [.. _entries.Select(e => e.Thread)];

    /// <summary>Creates a thread on a line.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The line is not in <c>1..LineCount</c>.</exception>
    public CommentThread Add(int lineNumber, string? title = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(lineNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lineNumber, _model.LineCount);

        var thread = new CommentThread(Guid.NewGuid().ToString("N"), lineNumber, title);
        var zone = new ViewZone
        {
            AfterLineNumber = lineNumber,
            HeightInPx = DefaultHeight,
            Kind = ViewZoneKind.Widget,
            SuppressMouseDown = true,
            ShowInHiddenAreas = false,
            Ordinal = OrdinalBase + _counter++,
            Data = thread,
        };

        string decorationId = "";
        _busy = true;
        try
        {
            _model.ChangeDecorations(_ownerId, accessor =>
                decorationId = accessor.AddDecoration(new Range(lineNumber, 1, lineNumber, _model.GetLineMaxColumn(lineNumber)), AnchorOptions));
            _zones.Change(accessor => accessor.AddZone(zone));
        }
        finally
        {
            _busy = false;
        }

        _entries.Add(new Entry(thread, decorationId, zone, _model.GetLineContent(lineNumber)));
        ThreadsChanged?.Invoke(this, EventArgs.Empty);
        return thread;
    }

    /// <summary>Removes a thread; <see langword="false"/> when unknown.</summary>
    public bool Remove(CommentThread thread)
    {
        Entry? entry = FindEntry(thread);
        if (entry is null)
        {
            return false;
        }

        _entries.Remove(entry);
        Release([entry]);
        ThreadsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>The thread with this id, or <see langword="null"/>.</summary>
    public CommentThread? Find(string id) => _entries.FirstOrDefault(e => e.Thread.Id == id)?.Thread;

    /// <summary>The id of the thread's view zone, or <see langword="null"/> when unknown.</summary>
    public string? GetZoneId(CommentThread thread) => FindEntry(thread)?.Zone.Id;

    /// <summary>The height of the thread's zone in pixels; 0 when unknown.</summary>
    public int GetHeight(CommentThread thread) => FindEntry(thread)?.Zone.HeightInPx ?? 0;

    /// <summary>Sets the zone height (clamped to <c>&gt;= 0</c>); the layout is only redone when it changes.</summary>
    public void SetHeight(CommentThread thread, int heightPx)
    {
        if (FindEntry(thread) is not { } entry)
        {
            return;
        }

        heightPx = Math.Max(0, heightPx);
        if (entry.Zone.HeightInPx == heightPx)
        {
            return;
        }

        entry.Zone.HeightInPx = heightPx;
        _zones.Change(accessor => accessor.LayoutZone(entry.Zone.Id));
    }

    /// <summary>Removes every decoration and zone and stops listening.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _model.DecorationsChanged -= OnDecorationsChanged;
        _model.ContentChanged -= OnContentChanged;
        Entry[] all = [.. _entries];
        _entries.Clear();
        Release(all);
    }

    private Entry? FindEntry(CommentThread thread) => _entries.FirstOrDefault(e => e.Thread == thread);

    private void Release(Entry[] entries)
    {
        _busy = true;
        try
        {
            _model.ChangeDecorations(_ownerId, accessor =>
            {
                foreach (Entry entry in entries)
                {
                    accessor.RemoveDecoration(entry.DecorationId);
                }
            });
            _zones.Change(accessor =>
            {
                foreach (Entry entry in entries)
                {
                    accessor.RemoveZone(entry.Zone.Id);
                }
            });
        }
        finally
        {
            _busy = false;
        }
    }

    private void OnDecorationsChanged(object? sender, EventArgs e) => SyncLines();

    private void OnContentChanged(object? sender, ModelContentChangedEventArgs e)
    {
        SyncLines();
        foreach (Entry entry in _entries.ToArray())
        {
            string text = _model.GetLineContent(Math.Clamp(entry.Thread.LineNumber, 1, _model.LineCount));
            if (text != entry.LineText)
            {
                entry.LineText = text;
                ThreadLineTextChanged?.Invoke(this, entry.Thread);
            }
        }
    }

    // The zone's view position is derived from ViewZone.AfterLineNumber, so it must follow the decoration.
    private void SyncLines()
    {
        if (_busy || _entries.Count == 0)
        {
            return;
        }

        List<Entry> moved = [];
        foreach (Entry entry in _entries)
        {
            int line = Math.Clamp(_model.GetDecorationRange(entry.DecorationId)?.StartLineNumber ?? entry.Thread.LineNumber, 1, _model.LineCount);
            if (line != entry.Thread.LineNumber)
            {
                entry.Thread.LineNumber = line;
                entry.Zone.AfterLineNumber = line;
                moved.Add(entry);
            }
        }

        if (moved.Count == 0)
        {
            return;
        }

        _busy = true;
        try
        {
            _zones.Change(accessor =>
            {
                foreach (Entry entry in moved)
                {
                    accessor.LayoutZone(entry.Zone.Id);
                }
            });
        }
        finally
        {
            _busy = false;
        }

        foreach (Entry entry in moved)
        {
            ThreadMoved?.Invoke(this, entry.Thread);
        }
    }

    private sealed class Entry(CommentThread thread, string decorationId, ViewZone zone, string lineText)
    {
        public CommentThread Thread { get; } = thread;

        public string DecorationId { get; } = decorationId;

        public ViewZone Zone { get; } = zone;

        public string LineText { get; set; } = lineText;
    }
}
