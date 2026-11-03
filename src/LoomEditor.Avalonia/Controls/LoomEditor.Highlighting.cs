using Avalonia;
using LoomEditor.Themes;
using LoomEditor.View.Events;

namespace LoomEditor.Avalonia.Controls;

// Syntax highlighting: the language and the theme. The tokens come from the background tokenizer through the
// SyntaxHighlighter of the editor context; the control only redraws when they change.
public sealed partial class LoomEditor
{
    private const string PlainTextLanguage = "plaintext";

    /// <summary>Identifies the <see cref="LanguageId"/> styled property.</summary>
    public static readonly StyledProperty<string> LanguageIdProperty =
        AvaloniaProperty.Register<LoomEditor, string>(nameof(LanguageId), PlainTextLanguage);

    private bool _highlightingStopped;
    private EditorTheme _theme = EditorTheme.Dark;

    /// <summary>
    /// The language of the text, one of <c>BuiltInLanguages.Ids</c> (<c>csharp</c>, <c>typescript</c>…). An unknown
    /// language is plain text. Defaults to <c>plaintext</c>.
    /// </summary>
    public string LanguageId
    {
        get => GetValue(LanguageIdProperty);
        set => SetValue(LanguageIdProperty, value);
    }

    /// <summary>
    /// The colors of the editor and of the tokens. Defaults to <see cref="EditorTheme.Dark"/>. A plain property, not a
    /// styled property, as in the WinUI control. The getter returns what the application set; while the system high
    /// contrast scheme is followed (<see cref="UseSystemHighContrast"/>) the editor draws with the scheme's colors instead, and uses this theme again
    /// when high contrast goes off.
    /// </summary>
    public new EditorTheme Theme
    {
        get => _theme;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _theme = value;
            ApplyTheme(EffectiveTheme);
        }
    }

    private static void InitializeHighlightingProperties() =>
        LanguageIdProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnLanguageIdChanged((string?)e.NewValue ?? PlainTextLanguage));

    private void InitializeHighlighting()
    {
        _viewModel.EventDispatcher.AddViewEventHandler(new TokensChangedHandler(this));
        // The hidden areas change which model lines are drawn.
        _viewModel.EventDispatcher.OnEvent += (_, e) =>
        {
            if (e is HiddenAreasChangedEvent)
            {
                Invalidate();
            }
        };
    }

    // The worker thread only exists while the control is attached to a window.
    private void StartHighlighting()
    {
        if (_highlightingStopped)
        {
            _highlightingStopped = false;
            _context.Highlighter.StartWorker();
        }
    }

    private void StopHighlighting()
    {
        _highlightingStopped = true;
        _context.Highlighter.StopWorker();
    }

    private void OnLanguageIdChanged(string languageId)
    {
        _context.Highlighter.SetLanguage(languageId);
        Invalidate();
    }

    private void ApplyTheme(EditorTheme theme)
    {
        if (_renderer is { } renderer)
        {
            renderer.Theme = theme;
        }

        _context.Highlighter.SetTheme(theme);
        _findWidget?.ApplyTheme(theme);
        Invalidate();
    }

    // InvalidateVisual draws once per frame, however many events come in one dispatcher turn.
    private sealed class TokensChangedHandler(LoomEditor editor) : IViewEventHandler
    {
        public void HandleEvents(IReadOnlyList<ViewEvent> events)
        {
            if (events.Any(e => e is ViewTokensChangedEvent or ViewDecorationsChangedEvent))
            {
                editor.Invalidate();
            }
        }
    }
}
