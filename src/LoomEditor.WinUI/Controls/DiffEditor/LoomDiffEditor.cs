// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/diffEditorWidget.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff;
using LoomEditor.View.DiffEditor;
using LoomEditor.WinUI.Controls.DiffEditor;
using LoomEditor.WinUI.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.WinUI.Controls;

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
/// Subset of Monaco's widget: this part has the two editors, the sash, the decorations and the alignment of the two sides (view zones and
/// synchronized scrolling; word wrap must be the same on both, wrapping is off by default); the inline view, hidden unchanged regions, navigation and the overview ruler are added by the following cards.
/// The editors are available as <see cref="Original"/> and <see cref="Modified"/>.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
[TemplatePart(Name = OriginalPartName, Type = typeof(LoomEditor))]
[TemplatePart(Name = ModifiedPartName, Type = typeof(LoomEditor))]
[TemplatePart(Name = SashPartName, Type = typeof(DiffEditorSash))]
[TemplatePart(Name = OverviewPartName, Type = typeof(DiffOverviewRuler))]
#pragma warning disable CA1001 // WinUI controls are not IDisposable: disposable fields are released in OnUnloaded.
public sealed partial class LoomDiffEditor : Control
#pragma warning restore CA1001
{
    private const string OriginalPartName = "PART_Original";
    private const string ModifiedPartName = "PART_Modified";
    private const string SashPartName = "PART_Sash";
    private const string OverviewPartName = "PART_Overview";

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
    private bool _isSyncingText;
    private int _decoratedOriginalLines;
    private int _decoratedModifiedLines;
    private IReadOnlyList<DiffChangeBlock> _changes = [];
    private DiffState? _changesDiff;

    /// <summary>Creates the control.</summary>
    public LoomDiffEditor()
    {
        DefaultStyleKey = typeof(LoomDiffEditor);
        _options.Changed += OnOptionsChanged;
        SizeChanged += OnSizeChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        GotFocus += (_, _) => RefreshScreenReader();
    }

    /// <summary>Raised when a new diff was published, or <see cref="IsDiffUpToDate"/> changed.</summary>
    public event EventHandler? DiffChanged;

    /// <summary>The editor of the original text. Available once the template is applied.</summary>
    /// <exception cref="InvalidOperationException">The template is not applied yet.</exception>
    public LoomEditor Original => _original ?? throw NotReady();

    /// <summary>The editor of the modified text. Available once the template is applied.</summary>
    /// <exception cref="InvalidOperationException">The template is not applied yet.</exception>
    public LoomEditor Modified => _modified ?? throw NotReady();

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachParts();
        _original = GetTemplatePart<LoomEditor>(OriginalPartName);
        _modified = GetTemplatePart<LoomEditor>(ModifiedPartName);
        _sash = GetTemplatePart<DiffEditorSash>(SashPartName);
        _sash.GetSashLeft = () => _sashLayout.SashLeft;
        _sash.SashMoved += OnSashMoved;
        _sash.ResetRequested += OnSashReset;
        ConnectOverview(GetTemplatePart<DiffOverviewRuler>(OverviewPartName));
        ConnectViewerHost(GetTemplatePart<Grid>(ViewerHostPartName));
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

    private T GetTemplatePart<T>(string name) where T : class =>
        GetTemplateChild(name) as T
            ?? throw new InvalidOperationException($"The LoomDiffEditor template must contain a {typeof(T).Name} named '{name}'.");

    private static InvalidOperationException NotReady() => new("The LoomDiffEditor template is not applied yet.");

    private void DetachParts()
    {
        DestroyAccessibleViewer();
        DetachCommentEvents();
        DetachBlockActions();
        DisconnectOverview();
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

    private void OnLoaded(object sender, RoutedEventArgs e) => EnsureViewModel();

    // Loaded also fires for a collapsed control, before its template is applied: OnApplyTemplate calls this again.
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
        _options.SetWidth(ActualWidth);
        _viewModel = new DiffEditorViewModel(
            _original.Model,
            _modified.Model,
            _options,
            new DocumentDiffProvider(),
            new DispatcherQueueUiDispatcher(DispatcherQueue),
            new DispatcherQueueEditorScheduler(DispatcherQueue));
        _viewModel.DiffChanged += OnViewModelDiffChanged;
        _hideUnchangedRegions = new HideUnchangedRegionsController(
            _original, _modified, _viewModel, () => !_options.RenderSideBySideEffective, () => _options.HideUnchangedRegions.RevealLineCount);
        UpdateViewZones();
        _viewModelReady.TrySetResult();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => DestroyViewModel();

    // An edit made in an editor (user or code) goes back to the property, without applying it to the model again.
    private void ReflectText(LoomEditor editor, DependencyProperty property)
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
