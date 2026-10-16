using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features;

/// <summary>
/// The default <see cref="IEditorContext"/>: holds the text, the selections and the commands of one editor.
/// </summary>
/// <remarks>
/// The selections live in the cursor, which the <see cref="Layout.ViewModel"/> owns (ADR 0006, decision 3).
/// Threading: UI thread only. Not thread-safe.
/// </remarks>
public sealed class EditorContext : IEditorContext
{
    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    /// <summary>Creates a context whose animations never run (smooth scrolling needs a real scheduler).</summary>
    public EditorContext()
        : this(_ => new NoopDisposable(), TimeProvider.System)
    {
    }

    internal EditorContext(Func<Action, IDisposable> scheduleAtNextAnimationFrame, TimeProvider timeProvider)
    {
        FontInfo fontInfo = FontInfo.Default;
        ViewModel = new ViewModel(
            Model,
            fontInfo,
            new EditorLayoutOptions(),
            new ViewLayoutOptions(fontInfo.LineHeight),
            scheduleAtNextAnimationFrame,
            timeProvider);
        ViewModel.EventDispatcher.OnEvent += OnViewModelEvent;
    }

    /// <inheritdoc />
    public CommandRegistry Commands { get; } = new();

    /// <inheritdoc />
    public TextModel Model { get; } = new(string.Empty);

    internal ViewModel ViewModel { get; }

    /// <inheritdoc />
    public Selection Selection
    {
        get => ViewModel.GetSelection();
        set => ViewModel.SetSelections("api", [value]);
    }

    /// <inheritdoc />
    public IReadOnlyList<Selection> Selections => ViewModel.GetSelections();

    /// <inheritdoc />
    public void SetSelections(IReadOnlyList<Selection> selections)
    {
        ArgumentNullException.ThrowIfNull(selections);
        if (selections.Count == 0)
        {
            throw new ArgumentException("There must be at least one selection.", nameof(selections));
        }

        ViewModel.SetSelections("api", selections);
    }

    /// <inheritdoc />
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Replaces the whole text. Accepts <c>\r\n</c>, <c>\n</c> and <c>\r</c> line breaks.
    /// The cursor is reset to the start of the document.
    /// </summary>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Model.SetValue(text);
    }

    private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
    {
        if (e is CursorStateChangedEvent)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
