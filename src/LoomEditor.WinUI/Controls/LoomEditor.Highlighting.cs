using LoomEditor.Themes;
using LoomEditor.View.Events;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Syntax highlighting: the language and the theme. The tokens come from the background tokenizer through the
// SyntaxHighlighter of the editor context; the control only redraws when they change.
public sealed partial class LoomEditor
{
    private const string PlainTextLanguage = "plaintext";

    /// <summary>Identifies the <see cref="LanguageId"/> dependency property.</summary>
    public static readonly DependencyProperty LanguageIdProperty = DependencyProperty.Register(
        nameof(LanguageId), typeof(string), typeof(LoomEditor), new PropertyMetadata(PlainTextLanguage, OnLanguageIdChanged));

    private bool _highlightingStopped;
    private EditorTheme _theme = EditorTheme.Dark;

    /// <summary>
    /// The language of the text, one of <c>BuiltInLanguages.Ids</c> (<c>csharp</c>, <c>typescript</c>…). An unknown
    /// language is plain text. Defaults to <c>plaintext</c>.
    /// </summary>
    public string LanguageId
    {
        get => (string)GetValue(LanguageIdProperty);
        set => SetValue(LanguageIdProperty, value);
    }

    /// <summary>
    /// The colors of the editor and of the tokens. Defaults to <see cref="EditorTheme.Dark"/>. A plain property, not a
    /// dependency property: a managed object cannot cross the XAML boundary under Native AOT (it can still be set from
    /// code and by <c>x:Bind</c>).
    /// </summary>
    public EditorTheme Theme
    {
        get => _theme;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _theme = value;
            ApplyTheme(value);
        }
    }

    private void InitializeHighlighting() => _viewModel.EventDispatcher.AddViewEventHandler(new TokensChangedHandler(this));

    // The worker thread only exists while the control is loaded.
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

    private static void OnLanguageIdChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        editor._context.Highlighter.SetLanguage((string?)e.NewValue ?? PlainTextLanguage);
        editor._canvas?.Invalidate();
    }

    private void ApplyTheme(EditorTheme theme)
    {
        if (_renderer is { } renderer)
        {
            renderer.Theme = theme;
        }

        _context.Highlighter.SetTheme(theme);
        _canvas?.Invalidate();
    }

    // CanvasControl.Invalidate draws once per frame, however many events come in one dispatcher turn.
    private sealed class TokensChangedHandler(LoomEditor editor) : IViewEventHandler
    {
        public void HandleEvents(IReadOnlyList<ViewEvent> events)
        {
            if (events.Any(e => e is ViewTokensChangedEvent))
            {
                editor._canvas?.Invalidate();
            }
        }
    }
}
