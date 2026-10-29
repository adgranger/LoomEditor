using LoomEditor.Core.Comments;
using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Comments;

/// <summary>
/// Owns the comment threads of one model: a tracked decoration per thread so it follows its lines or its exact text, and a
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

    private static readonly ModelDecorationOptions RangeAnchorOptions = ModelDecorationOptions.Register(
        new ModelDecorationOptionsData
        {
            Description = "comment-thread-range",
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ClassName = "comment-thread-range",
            IsWholeLine = true,
        });

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

    /// <summary>Raised once per thread whose lines changed because of an edit (not for a change of columns only).</summary>
    public event EventHandler<CommentThread>? ThreadMoved;

    /// <summary>Raised when the text of a thread's lines changed.</summary>
    public event EventHandler<CommentThread>? ThreadLineTextChanged;

    /// <summary>The threads, in creation order.</summary>
    public IReadOnlyList<CommentThread> Threads => [.. _entries.Select(e => e.Thread)];

    /// <summary>Creates a thread on a line.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The line is not in <c>1..LineCount</c>.</exception>
    public CommentThread Add(int lineNumber, string? title = null) => Add(lineNumber, lineNumber, title);

    /// <summary>Creates a thread on lines <paramref name="startLineNumber"/> to <paramref name="endLineNumber"/>, inclusive; its zone sits after the last line.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The range is not <c>1 &lt;= start &lt;= end &lt;= LineCount</c>.</exception>
    public CommentThread Add(int startLineNumber, int endLineNumber, string? title = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(startLineNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startLineNumber, _model.LineCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(endLineNumber, startLineNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(endLineNumber, _model.LineCount);

        var thread = new CommentThread(Guid.NewGuid().ToString("N"), startLineNumber, endLineNumber, title);
        int endColumn = _model.GetLineMaxColumn(endLineNumber);
        thread.SetRange(startLineNumber, 1, endLineNumber, endColumn);
        return AddCore(thread, new Range(startLineNumber, 1, endLineNumber, endColumn));
    }

    /// <summary>Creates a thread on the exact text <paramref name="textRange"/> (columns are clamped to the document); its zone sits after the last line.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A line of the range is not in <c>1..LineCount</c>.</exception>
    public CommentThread Add(Range textRange, string? title = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(textRange.StartLineNumber, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(textRange.StartLineNumber, _model.LineCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(textRange.EndLineNumber, _model.LineCount);
        Range r = _model.ValidateRange(textRange);
        return AddCore(new CommentThread(Guid.NewGuid().ToString("N"), r, title), r);
    }

    private CommentThread AddCore(CommentThread thread, Range decorationRange)
    {
        var zone = new ViewZone
        {
            AfterLineNumber = thread.LineNumber,
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
                decorationId = accessor.AddDecoration(decorationRange, OptionsFor(thread)));
            _zones.Change(accessor => accessor.AddZone(zone));
        }
        finally
        {
            _busy = false;
        }

        var entry = new Entry(thread, decorationId, zone, SuggestionEdit.GetRangeText(_model, thread.StartLineNumber, thread.LineNumber), OptionsFor(thread));
        entry.OnChanged = (_, e) =>
        {
            if (e.Kind == CommentThreadChangeKind.StateChanged)
            {
                UpdateOptions([entry]);
            }
        };
        thread.Changed += entry.OnChanged;
        _entries.Add(entry);
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

    /// <summary>The id of the thread's decoration, or <see langword="null"/> when unknown.</summary>
    public string? GetDecorationId(CommentThread thread) => FindEntry(thread)?.DecorationId;

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

    private static ModelDecorationOptions OptionsFor(CommentThread thread) =>
        thread.StartLineNumber < thread.LineNumber && thread.State == CommentThreadState.Open ? RangeAnchorOptions : AnchorOptions;

    private Entry? FindEntry(CommentThread thread) => _entries.FirstOrDefault(e => e.Thread == thread);

    private void Release(Entry[] entries)
    {
        foreach (Entry entry in entries)
        {
            entry.Thread.Changed -= entry.OnChanged;
        }

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
            int start = Math.Clamp(entry.Thread.StartLineNumber, 1, _model.LineCount);
            string text = SuggestionEdit.GetRangeText(_model, start, Math.Clamp(entry.Thread.LineNumber, start, _model.LineCount));
            if (text != entry.RangeText)
            {
                entry.RangeText = text;
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
        List<Entry> changed = [];
        foreach (Entry entry in _entries)
        {
            CommentThread thread = entry.Thread;
            Range? range = _model.GetDecorationRange(entry.DecorationId);
            int start = Math.Clamp(range?.StartLineNumber ?? thread.StartLineNumber, 1, _model.LineCount);
            int end = Math.Clamp(range?.EndLineNumber ?? thread.LineNumber, start, _model.LineCount);
            int startColumn = 1;
            int endColumn = _model.GetLineMaxColumn(end);
            if (!thread.IsWholeLines)
            {
                startColumn = Math.Clamp(range?.StartColumn ?? thread.StartColumn, 1, _model.GetLineMaxColumn(start));
                endColumn = Math.Clamp(range?.EndColumn ?? thread.EndColumn, 1, endColumn);
                if (start == end && endColumn < startColumn)
                {
                    endColumn = startColumn;
                }
            }

            bool linesMoved = start != thread.StartLineNumber || end != thread.LineNumber;
            if (linesMoved || startColumn != thread.StartColumn || endColumn != thread.EndColumn)
            {
                thread.SetRange(start, startColumn, end, endColumn);
                changed.Add(entry);
            }

            if (linesMoved)
            {
                entry.Zone.AfterLineNumber = end;
                moved.Add(entry);
            }
        }

        if (changed.Count == 0)
        {
            return;
        }

        if (moved.Count > 0)
        {
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
        }

        UpdateOptions(changed);
        foreach (Entry entry in moved)
        {
            ThreadMoved?.Invoke(this, entry.Thread);
        }
    }

    // Switching the options keeps the tracked range.
    private void UpdateOptions(IEnumerable<Entry> entries)
    {
        Entry[] changed = [.. entries.Where(e => OptionsFor(e.Thread) != e.Options)];
        if (changed.Length == 0)
        {
            return;
        }

        _busy = true;
        try
        {
            _model.ChangeDecorations(_ownerId, accessor =>
            {
                foreach (Entry entry in changed)
                {
                    entry.Options = OptionsFor(entry.Thread);
                    accessor.ChangeDecorationOptions(entry.DecorationId, entry.Options);
                }
            });
        }
        finally
        {
            _busy = false;
        }
    }

    private sealed class Entry(CommentThread thread, string decorationId, ViewZone zone, string rangeText, ModelDecorationOptions options)
    {
        public ModelDecorationOptions Options { get; set; } = options;

        public EventHandler<CommentThreadChangedEventArgs> OnChanged { get; set; } = null!;

        public CommentThread Thread { get; } = thread;

        public string DecorationId { get; } = decorationId;

        public ViewZone Zone { get; } = zone;

        public string RangeText { get; set; } = rangeText;
    }
}
