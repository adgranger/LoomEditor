using LoomEditor.Core.Model;
using LoomEditor.View.Features;

namespace LoomEditor.View.Accessibility;

/// <summary>
/// Turns the selection and text changes of an editor into at most one automation event of each kind per dispatcher turn,
/// so that screen readers are not flooded (typing changes both; Replace all makes thousands of edits).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class AutomationEventCoalescer : IDisposable
{
    private readonly IEditorContext _context;
    private readonly IAutomationEventSink _sink;
    private IDisposable? _pendingFlush;
    private bool _textChanged;
    private bool _selectionChanged;
    private bool _disposed;

    /// <summary>Starts listening to <paramref name="context"/>.</summary>
    public AutomationEventCoalescer(IEditorContext context, IAutomationEventSink sink)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sink);
        _context = context;
        _sink = sink;
        context.SelectionChanged += OnSelectionChanged;
        context.Model.ContentChanged += OnContentChanged;
    }

    /// <summary>Stops listening and cancels a pending flush. Safe to call twice.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _context.SelectionChanged -= OnSelectionChanged;
        _context.Model.ContentChanged -= OnContentChanged;
        _pendingFlush?.Dispose();
        _pendingFlush = null;
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        _selectionChanged = true;
        ScheduleFlush();
    }

    private void OnContentChanged(object? sender, ModelContentChangedEventArgs e)
    {
        _textChanged = true;
        ScheduleFlush();
    }

    private void ScheduleFlush() => _pendingFlush ??= _context.Scheduler.Schedule(TimeSpan.Zero, Flush);

    private void Flush()
    {
        _pendingFlush = null;
        if (_disposed)
        {
            return;
        }

        bool text = _textChanged;
        bool selection = _selectionChanged;
        _textChanged = false;
        _selectionChanged = false;

        // UIA clients expect the text change before the caret move.
        if (text)
        {
            try
            {
                _sink.RaiseTextChanged();
            }
            catch (Exception)
            {
                // A faulty automation client must not break typing.
            }
        }

        if (selection)
        {
            try
            {
                _sink.RaiseSelectionChanged();
            }
            catch (Exception)
            {
                // A faulty automation client must not break typing.
            }
        }
    }
}
