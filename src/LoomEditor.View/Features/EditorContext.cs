using LoomEditor.Core.Model;
using LoomEditor.Core.Threading;
using LoomEditor.Core.Text;
using LoomEditor.TextMate.Grammars;
using LoomEditor.TextMate.Tokenization;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Commands;
using LoomEditor.View.Events;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Highlighting;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features;

/// <summary>
/// The default <see cref="IEditorContext"/>: holds the text, the selections and the commands of one editor.
/// </summary>
/// <remarks>
/// The selections live in the cursor, which the <see cref="Layout.ViewModel"/> owns (ADR 0006, decision 3).
/// Threading: UI thread only. Not thread-safe.
/// </remarks>
public sealed class EditorContext : IEditorContext, IDisposable
{
    // Keeps the registration of the core commands (cursorLeft, cursorHome...) alive for the life of the editor.
    private readonly IDisposable _coreCommands;

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Creates a context whose animations never run (smooth scrolling needs a real scheduler) and whose
    /// <see cref="Scheduler"/> never runs the actions it is given.
    /// </summary>
    public EditorContext()
        : this(_ => new NoopDisposable(), TimeProvider.System)
    {
    }

    internal EditorContext(IEditorScheduler scheduler)
        : this(_ => new NoopDisposable(), TimeProvider.System, null, scheduler)
    {
    }

    internal EditorContext(
        Func<Action, IDisposable> scheduleAtNextAnimationFrame,
        TimeProvider timeProvider,
        IUiDispatcher? dispatcher = null,
        IEditorScheduler? scheduler = null)
    {
        Scheduler = scheduler ?? NullEditorScheduler.Instance;
        var tokenizationRegistry = new TokenizationRegistry();
        var languages = new LanguageRegistry();
        BuiltInLanguages.RegisterAll(languages);
        Model = new TextModel(string.Empty, DefaultEndOfLine.LF, null, tokenizationRegistry, languages.Codec, NoopTokenizationScheduler.Instance);
        Highlighter = new SyntaxHighlighter(Model, tokenizationRegistry, languages, dispatcher);
        FontInfo fontInfo = FontInfo.Default;
        ViewModel = new ViewModel(
            Model,
            fontInfo,
            new EditorLayoutOptions(),
            new ViewLayoutOptions(fontInfo.LineHeight),
            scheduleAtNextAnimationFrame,
            timeProvider);
        ViewModel.TokenColorMap = tokenizationRegistry.GetColorMap();
        tokenizationRegistry.Changed += (_, e) =>
        {
            if (e.ChangedColorMap)
            {
                ViewModel.TokenColorMap = tokenizationRegistry.GetColorMap();
            }
        };
        ViewModel.EventDispatcher.OnEvent += OnViewModelEvent;
        _coreCommands = CoreCommandRegistration.Register(Commands, ViewModel);
    }

    /// <inheritdoc />
    public IEditorScheduler Scheduler { get; }

    /// <inheritdoc />
    public CommandRegistry Commands { get; } = new();

    /// <inheritdoc />
    public TextModel Model { get; }

    internal SyntaxHighlighter Highlighter { get; }

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

    /// <summary>Stops the background tokenizer and releases the grammars.</summary>
    public void Dispose()
    {
        Highlighter.Dispose();
        _coreCommands.Dispose();
    }

    private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
    {
        if (e is CursorStateChangedEvent)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
