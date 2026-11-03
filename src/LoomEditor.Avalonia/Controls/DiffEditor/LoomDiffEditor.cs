// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/diffEditorWidget.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using LoomEditor.Avalonia.Controls.DiffEditor;
using LoomEditor.Avalonia.Threading;
using LoomEditor.Core.Diff;
using LoomEditor.Themes;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.Avalonia.Controls;

/// <summary>
/// A diff editor: the original and the modified text side by side, with the changes decorated (Monaco: <c>DiffEditorWidget</c>).
/// </summary>
/// <example>
/// <code language="xml">
/// &lt;loom:LoomDiffEditor OriginalText="a" ModifiedText="b" LanguageId="csharp" /&gt;
/// </code>
/// </example>
/// <remarks>
/// <para>
/// Subset of Monaco's widget: this part has the two editors, the sash and the decorations; the options, navigation, alignment of the two sides, the overview ruler,
/// hidden unchanged regions, comments, the accessible viewer and the actions are added by the following cards.
/// The editors are available as <see cref="Original"/> and <see cref="Modified"/>.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
[TemplatePart(OriginalPartName, typeof(LoomEditor))]
[TemplatePart(ModifiedPartName, typeof(LoomEditor))]
[TemplatePart(SashPartName, typeof(DiffEditorSash))]
[TemplatePart(OverviewPartName, typeof(DiffOverviewRuler))]
#pragma warning disable CA1001 // Controls are not IDisposable: disposable fields are released in OnUnloaded.
public sealed partial class LoomDiffEditor : TemplatedControl
#pragma warning restore CA1001
{
    private const string OriginalPartName = "PART_Original";
    private const string ModifiedPartName = "PART_Modified";
    private const string SashPartName = "PART_Sash";
    private const string OverviewPartName = "PART_Overview";

    /// <summary>Identifies the <see cref="OriginalText"/> styled property.</summary>
    public static readonly StyledProperty<string> OriginalTextProperty =
        AvaloniaProperty.Register<LoomDiffEditor, string>(nameof(OriginalText), string.Empty);

    /// <summary>Identifies the <see cref="ModifiedText"/> styled property.</summary>
    public static readonly StyledProperty<string> ModifiedTextProperty =
        AvaloniaProperty.Register<LoomDiffEditor, string>(nameof(ModifiedText), string.Empty);

    /// <summary>Identifies the <see cref="LanguageId"/> styled property.</summary>
    public static readonly StyledProperty<string> LanguageIdProperty =
        AvaloniaProperty.Register<LoomDiffEditor, string>(nameof(LanguageId), "plaintext");

    private readonly DiffEditorOptions _options = new();
    private readonly SashLayout _sashLayout = new(0, 0.5, true);
    private TaskCompletionSource _viewModelReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private LoomEditor? _original;
    private LoomEditor? _modified;
    private DiffEditorSash? _sash;
    private DiffEditorViewModel? _viewModel;
    private DiffDecorations? _decorations;
    private DiffEditorViewZones? _viewZones;
    private HideUnchangedRegionsController? _hideUnchangedRegions;
    private bool _viewZonesInline;
    private EditorDecorationTarget? _originalTarget;
    private EditorDecorationTarget? _modifiedTarget;
    private EditorTheme _theme = EditorTheme.Dark;
    private bool _isSyncingText;
    private int _decoratedOriginalLines;
    private int _decoratedModifiedLines;
    private IReadOnlyList<DiffChangeBlock> _changes = [];
    private DiffState? _changesDiff;

    static LoomDiffEditor()
    {
        OriginalTextProperty.Changed.AddClassHandler<LoomDiffEditor>(static (editor, e) => editor.ApplyText(editor._original, (string?)e.NewValue));
        ModifiedTextProperty.Changed.AddClassHandler<LoomDiffEditor>(static (editor, e) => editor.ApplyText(editor._modified, (string?)e.NewValue));
        LanguageIdProperty.Changed.AddClassHandler<LoomDiffEditor>(static (editor, _) => editor.ApplyLanguageAndTheme());
        LineSelectionEnabledProperty.Changed.AddClassHandler<LoomDiffEditor>(OnLineSelectionEnabledChanged);
        LineActionGlyphProperty.Changed.AddClassHandler<LoomDiffEditor>(static (editor, _) => editor.UpdateGutterLanes());
        LineActionToolTipProperty.Changed.AddClassHandler<LoomDiffEditor>(static (editor, _) => editor.UpdateGutterLanes());
        BlockActionsVisibilityProperty.Changed.AddClassHandler<LoomDiffEditor>(static (editor, _) => editor.UpdateGutterLanes());
        GlyphFontFamilyProperty.Changed.AddClassHandler<LoomDiffEditor>(static (editor, _) => editor.UpdateGutterLanes());
    }

    /// <summary>Creates the control.</summary>
    public LoomDiffEditor()
    {
        _options.Changed += OnOptionsChanged;
        SizeChanged += OnSizeChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        GotFocus += (_, _) => RefreshScreenReader();
        AddHandler(KeyDownEvent, OnLineActionKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>Raised when a new diff was published, or <see cref="IsDiffUpToDate"/> changed.</summary>
    public event EventHandler? DiffChanged;

    /// <summary>The editor of the original text. Available once the template is applied.</summary>
    /// <exception cref="InvalidOperationException">The template is not applied yet.</exception>
    public LoomEditor Original => _original ?? throw NotReady();

    /// <summary>The editor of the modified text. Available once the template is applied.</summary>
    /// <exception cref="InvalidOperationException">The template is not applied yet.</exception>
    public LoomEditor Modified => _modified ?? throw NotReady();

    /// <summary>The original text. Defaults to an empty string.</summary>
    public string OriginalText
    {
        get => GetValue(OriginalTextProperty);
        set => SetValue(OriginalTextProperty, value);
    }

    /// <summary>The modified text; an edit of the user in <see cref="Modified"/> is reflected here. Defaults to an empty string.</summary>
    public string ModifiedText
    {
        get => GetValue(ModifiedTextProperty);
        set => SetValue(ModifiedTextProperty, value);
    }

    /// <summary>The language of both texts, see <see cref="LoomEditor.LanguageId"/>. Defaults to <c>plaintext</c>.</summary>
    public string LanguageId
    {
        get => GetValue(LanguageIdProperty);
        set => SetValue(LanguageIdProperty, value);
    }

    /// <summary>
    /// The theme of both editors, see <see cref="LoomEditor.Theme"/> (a plain property for the same reason). Defaults to
    /// <see cref="EditorTheme.Dark"/>.
    /// </summary>
    public new EditorTheme Theme
    {
        get => _theme;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _theme = value;
            ApplyLanguageAndTheme();
        }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        DetachParts();
        _original = GetTemplatePart<LoomEditor>(e, OriginalPartName);
        _modified = GetTemplatePart<LoomEditor>(e, ModifiedPartName);
        _sash = GetTemplatePart<DiffEditorSash>(e, SashPartName);
        _sash.GetSashLeft = () => _sashLayout.SashLeft;
        _sash.SashMoved += OnSashMoved;
        _sash.ResetRequested += OnSashReset;
        ConnectOverview(GetTemplatePart<DiffOverviewRuler>(e, OverviewPartName));
        ConnectViewerHost(GetTemplatePart<Panel>(e, ViewerHostPartName));
        ConfigureEditors();
        ApplyLanguageAndTheme();
        ApplyText(_original, OriginalText);
        ApplyText(_modified, ModifiedText);
        _original.Model.ContentChanged += OnOriginalContentChanged;
        _modified.Model.ContentChanged += OnModifiedContentChanged;
        AttachCommentEvents();
        AttachBlockActions();
        ApplyLayout();
        if (IsLoaded)
        {
            EnsureViewModel();
        }
    }

    private static T GetTemplatePart<T>(TemplateAppliedEventArgs e, string name) where T : class =>
        e.NameScope.Find(name) as T
            ?? throw new InvalidOperationException($"The LoomDiffEditor template must contain a {typeof(T).Name} named '{name}'.");

    private static InvalidOperationException NotReady() => new("The LoomDiffEditor template is not applied yet.");

    private void DetachParts()
    {
        DestroyAccessibleViewer();
        DisconnectOverview();
        DetachCommentEvents();
        DetachBlockActions();
        if (_sash is not null)
        {
            _sash.SashMoved -= OnSashMoved;
            _sash.ResetRequested -= OnSashReset;
        }

        if (_original is not null)
        {
            _original.Model.ContentChanged -= OnOriginalContentChanged;
        }

        if (_modified is not null)
        {
            _modified.Model.ContentChanged -= OnModifiedContentChanged;
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) => EnsureViewModel();

    // Loaded can come before the template is applied: OnApplyTemplate calls this again.
    private void EnsureViewModel()
    {
        if (_viewModel is not null || _original is null || _modified is null)
        {
            return;
        }

        _originalTarget = new EditorDecorationTarget(_original.CreateModelDecorationsCollection());
        _modifiedTarget = new EditorDecorationTarget(_modified.CreateModelDecorationsCollection());
        _decorations = new DiffDecorations(_originalTarget, _modifiedTarget);
        RefreshScreenReader();
        _options.SetWidth(Bounds.Width);
        _viewModel = new DiffEditorViewModel(
            _original.Model,
            _modified.Model,
            _options,
            new DocumentDiffProvider(),
            new DispatcherUiDispatcher(),
            new DispatcherEditorScheduler());
        _viewModel.DiffChanged += OnViewModelDiffChanged;
        _hideUnchangedRegions = new HideUnchangedRegionsController(
            _original, _modified, _viewModel, () => !_options.RenderSideBySideEffective, () => _options.HideUnchangedRegions.RevealLineCount);
        UpdateViewZones();
        _viewModelReady.TrySetResult();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        DestroyAccessibleViewer();
        if (_viewModel is null)
        {
            return;
        }

        _hideUnchangedRegions?.Dispose();
        _hideUnchangedRegions = null;
        _viewZones?.Dispose();
        _viewZones = null;
        _viewModel.DiffChanged -= OnViewModelDiffChanged;
        _viewModel.Dispose();
        _viewModel = null;
        _changes = [];
        _changesDiff = null;
        UpdateBlockActionBlocks(_changes);
        _viewModelReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _originalTarget?.Clear();
        _modifiedTarget?.Clear();
        _decorations = null;
    }

    // An edit made in an editor (user or code) goes back to the property, without applying it to the model again.
    private void ReflectText(LoomEditor editor, StyledProperty<string> property)
    {
        if (_isSyncingText)
        {
            return;
        }

        _isSyncingText = true;
        try
        {
            SetValue(property, editor.Model.GetValue());
        }
        finally
        {
            _isSyncingText = false;
        }
    }

}
