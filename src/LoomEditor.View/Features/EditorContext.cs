using LoomEditor.View.Cursors;
using LoomEditor.Core.Decorations;
using System.Globalization;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Core.Threading;
using LoomEditor.Core.Text;
using LoomEditor.TextMate.Grammars;
using LoomEditor.TextMate.Tokenization;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Commands;
using LoomEditor.View.Controller;
using LoomEditor.View.Events;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Features.StickyScroll;
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
    // Monaco: EDITOR_ID (the first id is 1: owner 0 means "no owner" in the model)
    private static int _nextEditorId;

    // Keeps the registration of the core commands (cursorLeft, cursorHome...) alive for the life of the editor.
    private readonly IDisposable _coreCommands;

    private readonly LanguageRegistry _languages;

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
        Id = Interlocked.Increment(ref _nextEditorId);
        var tokenizationRegistry = new TokenizationRegistry();
        var languages = new LanguageRegistry();
        _languages = languages;
        BuiltInLanguages.RegisterAll(languages);
        Model = new TextModel(string.Empty, DefaultEndOfLine.LF, null, tokenizationRegistry, languages.Codec, NoopTokenizationScheduler.Instance, new LanguageConfigurationRegistry());
        Highlighter = new SyntaxHighlighter(Model, tokenizationRegistry, languages, Model.LanguageConfigurationRegistry, dispatcher);
        FontInfo fontInfo = FontInfo.Default;
        ViewModel = new ViewModel(
            Model,
            fontInfo,
            new EditorLayoutOptions(),
            new ViewLayoutOptions(fontInfo.LineHeight),
            scheduleAtNextAnimationFrame,
            timeProvider,
            Id);
        UserInputEvents = new ViewUserInputEvents(ViewModel.CoordinatesConverter);
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
        UiDispatcher = dispatcher ?? InlineUiDispatcher.Instance;
        _documentUri = "inmemory://model/" + Id.ToString(CultureInfo.InvariantCulture);
        MarkerDecorations = new MarkerDecorations(Model, Markers, Id);
    }

    /// <summary>The providers of completion, hover and definition of this editor.</summary>
    public LanguageFeaturesService LanguageFeatures { get; } = new();

    /// <summary>The zoom level of this editor (Monaco: the global EditorZoom; here one per editor).</summary>
    public EditorZoomState Zoom { get; } = new();

    /// <summary>The markers of this editor; each one becomes a squiggle decoration.</summary>
    public MarkerService Markers { get; } = new();

    /// <summary>The URI of the document (default <c>inmemory://model/&lt;id&gt;</c>), read when a language server session opens it.</summary>
    /// <exception cref="ArgumentException">The value is null or empty.</exception>
    public string DocumentUri
    {
        get => _documentUri;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            _documentUri = value;
        }
    }

    private string _documentUri;

    /// <summary>Runs an action on the UI thread; inline when the editor has no dispatcher.</summary>
    internal IUiDispatcher UiDispatcher { get; }

    /// <summary>Turns <see cref="Markers"/> into decorations of <see cref="Model"/>; hover reads it.</summary>
    internal MarkerDecorations MarkerDecorations { get; }

    /// <inheritdoc />
    public IEditorScheduler Scheduler { get; }

    /// <inheritdoc />
    public CommandRegistry Commands { get; } = new();

    /// <inheritdoc />
    public TextModel Model { get; }

    /// <summary>The id of this editor; it owns the decorations the editor creates (Monaco: <c>getId</c>).</summary>
    internal int Id { get; }

    internal void RegisterLanguage(string languageId) =>
        _languages.Register(new LanguageDefinition(languageId, [], [], [], null, []));

    internal SyntaxHighlighter Highlighter { get; }

    internal ViewModel ViewModel { get; }

    /// <summary>The mouse events of the view, in model coordinates (Monaco: <c>ViewUserInputEvents</c>).</summary>
    internal ViewUserInputEvents UserInputEvents { get; }

    /// <summary>The find controller, set by <c>FindFeature</c> while it is attached (Monaco: <c>CommonFindController.get(editor)</c>).</summary>
    internal FindController? FindController { get; set; }

    /// <summary>Announces messages to assistive technology; assigned by the control (P7-19).</summary>
    internal IEditorAnnouncer Announcer { get; set; } = NullEditorAnnouncer.Instance;

    /// <summary>Whether Tab and Shift+Tab move the keyboard focus instead of indenting (Monaco: <c>TabFocus.getTabFocusMode()</c>). Per editor.</summary>
    internal bool TabMovesFocus
    {
        get => _tabMovesFocus;
        set
        {
            if (_tabMovesFocus != value)
            {
                _tabMovesFocus = value;
                TabMovesFocusChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Raised when <see cref="TabMovesFocus"/> really changes.</summary>
    internal event EventHandler? TabMovesFocusChanged;

    private bool _tabMovesFocus;

    /// <summary>The comment threads of the editor; set by the control (P11-11), null until then.</summary>
    internal LoomEditor.View.Comments.CommentThreadsController? CommentThreads { get; set; }

    /// <summary>The folding feature, set by <c>FoldingFeature</c> while it is attached (Monaco: <c>FoldingController.get(editor)</c>).</summary>
    internal FoldingFeature? FoldingFeature { get; set; }

    /// <summary>The hover popup; assigned by the control (P8-11), the null presenter until then.</summary>
    internal LoomEditor.View.Hover.IHoverPresenter HoverPresenter { get; set; } = LoomEditor.View.Hover.NullHoverPresenter.Instance;

    /// <summary>The completion list; assigned by the control (P8-22), the null presenter until then.</summary>
    internal LoomEditor.View.Suggest.ISuggestPresenter SuggestPresenter { get; set; } = LoomEditor.View.Suggest.NullSuggestPresenter.Instance;

    /// <summary>True while a hover is shown (Monaco: <c>editorHoverVisible</c>); set by the hover controller.</summary>
    internal bool HoverVisible { get; set; }

    /// <summary>True while the completion list is on screen (Monaco: <c>suggestWidgetVisible</c>); set by the suggest feature.</summary>
    internal bool SuggestWidgetVisible { get; set; }

    /// <summary>True while the completion list has several items (Monaco: <c>suggestWidgetMultipleSuggestions</c>); set by the suggest feature.</summary>
    internal bool SuggestWidgetMultipleSuggestions { get; set; }

    /// <summary>The snippet controller, set by <c>SnippetFeature</c> while it is attached (Monaco: <c>SnippetController2.get(editor)</c>).</summary>
    internal LoomEditor.View.Features.Snippet.SnippetController? SnippetController { get; set; }

    /// <summary>Supplies the file, workspace and clipboard variables of snippets; set by the host.</summary>
    internal LoomEditor.Core.Snippets.ISnippetVariableHost SnippetVariableHost { get; set; } = LoomEditor.Core.Snippets.ISnippetVariableHost.None;

    /// <summary>True while a snippet with tab stops is active (Monaco: <c>inSnippetMode</c>); set by the snippet controller.</summary>
    internal bool InSnippetMode { get; set; }

    /// <summary>True while the active snippet has a next tab stop (Monaco: <c>hasNextTabstop</c>); set by the snippet controller.</summary>
    internal bool HasNextTabstop { get; set; }

    /// <summary>True while the active snippet has a previous tab stop (Monaco: <c>hasPrevTabstop</c>); set by the snippet controller.</summary>
    internal bool HasPrevTabstop { get; set; }

    /// <summary>The go to problem controller, set by <c>GotoErrorFeature</c> while it is attached (Monaco: <c>MarkerController.get(editor)</c>).</summary>
    internal LoomEditor.View.Features.GotoError.MarkerController? MarkerController { get; set; }

    /// <summary>True while a go to problem session is active (Monaco: <c>markersNavigationVisible</c>); set by the marker controller.</summary>
    internal bool MarkersNavigationVisible { get; set; }

    /// <summary>True while the selection anchor exists (Monaco: <c>selectionAnchorSet</c>); set by the selection anchor feature.</summary>
    internal bool SelectionAnchorSet { get; set; }

    /// <summary>Opens the definitions that are in another document; null (the default) announces them instead.</summary>
    public LoomEditor.View.Features.GotoDefinition.ILocationOpener? LocationOpener { get; set; }

    /// <summary>The sticky scroll feature, set by <c>StickyScrollFeature</c> while it is attached (Monaco: <c>StickyScrollController.get(editor)</c>).</summary>
    internal StickyScrollFeature? StickyScrollFeature { get; set; }

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

    /// <summary>Why the selections changed last, for a host that must tell a text reset from a user move.</summary>
    internal CursorChangeReason LastSelectionChangeReason { get; private set; }

    /// <inheritdoc />
    public event EventHandler? SelectionChanged;

    /// <inheritdoc />
    public bool IsReadOnly
    {
        get => ViewModel.IsReadOnly;
        set
        {
            if (ViewModel.IsReadOnly != value)
            {
                ViewModel.SetCursorOptions(ViewModel.CursorOptions with { ReadOnly = value });
                ReadOnlyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler? ReadOnlyChanged;

    /// <summary>Whether typing inserts or overwrites (Monaco: the global <c>InputMode</c>; here one per editor).</summary>
    public EditorInputMode InputMode
    {
        get => ViewModel.CursorOptions.InputMode;
        set
        {
            if (ViewModel.CursorOptions.InputMode != value)
            {
                ViewModel.SetCursorOptions(ViewModel.CursorOptions with { InputMode = value });
                InputModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Raised when <see cref="InputMode"/> really changes.</summary>
    public event EventHandler? InputModeChanged;

    /// <summary>The mouse cursor over the text (Monaco: <c>updateOptions({ mouseStyle })</c>).</summary>
    internal EditorMouseStyle MouseStyle
    {
        get => _mouseStyle;
        set
        {
            if (_mouseStyle != value)
            {
                _mouseStyle = value;
                MouseStyleChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Raised when <see cref="MouseStyle"/> really changes.</summary>
    internal event EventHandler? MouseStyleChanged;

    private EditorMouseStyle _mouseStyle;

    /// <summary>
    /// Replaces the whole text. Accepts <c>\r\n</c>, <c>\n</c> and <c>\r</c> line breaks.
    /// The cursor is reset to the start of the document.
    /// </summary>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Model.SetValue(text);
        if (DetectIndentation)
        {
            // Monaco: EDITOR_MODEL_DEFAULTS, not the current options, so a previous detection does not leak
            Model.DetectIndentation(defaultInsertSpaces: true, defaultTabSize: 4);
        }
    }

    /// <summary>Whether <see cref="SetText"/> guesses the indentation (Monaco: the <c>detectIndentation</c> option).</summary>
    internal bool DetectIndentation { get; set; } = true;

    /// <summary>Creates a decorations collection owned by this editor (Monaco: <c>createDecorationsCollection</c>).</summary>
    internal EditorDecorationsCollection CreateDecorationsCollection(IReadOnlyList<ModelDeltaDecoration>? decorations = null) =>
        new(Model, Id, decorations);

    /// <summary>Changes the decorations of this editor with an accessor (Monaco: <c>changeDecorations</c>).</summary>
    internal T ChangeDecorations<T>(Func<IModelDecorationsChangeAccessor, T> callback) => Model.ChangeDecorations(Id, callback);

    /// <inheritdoc cref="ChangeDecorations{T}(Func{IModelDecorationsChangeAccessor, T})"/>
    internal void ChangeDecorations(Action<IModelDecorationsChangeAccessor> callback) => Model.ChangeDecorations(Id, callback);

    /// <summary>The decorations of this editor that touch a line (Monaco: <c>getLineDecorations</c>).</summary>
    internal IReadOnlyList<ModelDecoration> GetLineDecorations(int lineNumber) =>
        // Monaco passes filterValidationDecorations(options) and filterFontDecorations(options): both are false here
        // (no validation decorations and no font decorations exist, and the renderValidationDecorations option is not ported).
        Model.GetLineDecorations(lineNumber, Id, filterOutValidation: false, filterFontDecorations: false);

    /// <summary>The decorations of this editor that intersect a range (Monaco: <c>getDecorationsInRange</c>).</summary>
    internal IReadOnlyList<ModelDecoration> GetDecorationsInRange(Range range) =>
        Model.GetDecorationsInRange(range, Id, filterOutValidation: false, filterFontDecorations: false);

    /// <summary>Replaces decorations of this editor (Monaco: the deprecated <c>deltaDecorations</c>); with both lists empty it returns <paramref name="oldDecorationIds"/>.</summary>
    internal IReadOnlyList<string> DeltaDecorations(IReadOnlyList<string> oldDecorationIds, IReadOnlyList<ModelDeltaDecoration> newDecorations)
    {
        if (oldDecorationIds.Count == 0 && newDecorations.Count == 0)
        {
            return oldDecorationIds;
        }

        return Model.DeltaDecorations(Id, oldDecorationIds, newDecorations);
    }

    /// <summary>Removes decorations by id (Monaco: <c>removeDecorations</c>).</summary>
    internal void RemoveDecorations(IReadOnlyList<string> decorationIds)
    {
        if (decorationIds.Count == 0)
        {
            return;
        }

        // Monaco: model.changeDecorations(callback) with the default owner 0: only the old ids matter here.
        Model.ChangeDecorations(0, accessor => accessor.DeltaDecorations(decorationIds, []));
    }

    /// <summary>Stops the background tokenizer and releases the grammars.</summary>
    public void Dispose()
    {
        CommentThreads?.Dispose();
        MarkerDecorations.Dispose();
        Highlighter.Dispose();
        _coreCommands.Dispose();
    }

    private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
    {
        if (e is CursorStateChangedEvent cursorEvent)
        {
            LastSelectionChangeReason = cursorEvent.Reason;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
