// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/folding.ts (FoldingController)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using LoomEditor.View.Events;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Folding;

/// <summary>
/// Folding: computes the ranges of the text with the indentation provider after each edit, keeps the
/// <see cref="FoldingModel"/> and the <see cref="HiddenRangeModel"/>, hides the folded lines in the view model and unfolds
/// a region when a cursor ends up inside it (Monaco: <c>FoldingController</c>).
/// </summary>
/// <remarks>
/// Behavior differences: the update delay is fixed (300 ms the first time, then 200 ms; Monaco measures the compute time
/// and adapts it); the range provider is synchronous, so there is no promise and no cancellation; only the indentation
/// strategy exists (no syntax providers, no imports folding, no view-state memento). The commands, the gutter and the
/// mouse come with later cards. Threading: UI thread only.
/// </remarks>
internal sealed partial class FoldingFeature : IEditorFeature
{
    private static readonly TimeSpan FirstUpdateDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan UpdateDelay = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan CursorChangedDelay = TimeSpan.FromMilliseconds(200);

    private readonly RangesLimitReporter _limitReporter;
    private EditorFoldingOptions _options = EditorFoldingOptions.Default;
    private IEditorScheduler? _scheduler;
    private ViewModel? _viewModel;
    private TextModel? _model;
    private FoldingDecorationProvider? _decorationProvider;
    private FoldingModel? _foldingModel;
    private HiddenRangeModel? _hiddenRangeModel;
    private IndentRangeProvider? _rangeProvider;
    private Delayer? _updateScheduler;
    private RunOnceScheduler? _cursorChangedScheduler;
    private bool _isModelBound;

    /// <summary>Creates the feature.</summary>
    public FoldingFeature() => _limitReporter = new RangesLimitReporter(() => _options.FoldingMaximumRegions);

    /// <inheritdoc />
    public string Id => "loom.folding";

    internal EditorFoldingOptions Options => _options;

    internal RangesLimitReporter LimitReporter => _limitReporter;

    internal HiddenRangeModel? HiddenRangeModel => _hiddenRangeModel;

    /// <summary>The current folding model, without running a pending update; <see langword="null"/> when folding is off.</summary>
    internal FoldingModel? FoldingModel => _foldingModel;

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ViewModel? viewModel = context.GetViewModel();
        if (viewModel is null)
        {
            return;
        }

        _viewModel = viewModel;
        _model = context.Model;
        _scheduler = context.Scheduler;
        _decorationProvider = new FoldingDecorationProvider(viewModel)
        {
            ShowFoldingControls = _options.ShowFoldingControls,
            ShowFoldingHighlights = _options.FoldingHighlight,
        };
        RegisterCommands(context);
        OnModelChanged();
    }

    internal void SetOptions(EditorFoldingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        EditorFoldingOptions old = _options;
        _options = options;
        if (_decorationProvider is null)
        {
            return;
        }

        if (options.Folding != old.Folding)
        {
            OnModelChanged();
        }

        if (options.FoldingMaximumRegions != old.FoldingMaximumRegions)
        {
            OnModelChanged();
        }

        if (options.Folding != old.Folding || options.ShowFoldingControls != old.ShowFoldingControls)
        {
            // The gutter has room for the controls only when they can be shown.
            _viewModel!.SetOptions(
                _viewModel.LayoutOptions with { Folding = options.Folding, ShowFoldingDecoration = options.ShowFoldingControls != ShowFoldingControls.Never },
                _viewModel.ViewLayoutOptions);
        }

        if (options.ShowFoldingControls != old.ShowFoldingControls || options.FoldingHighlight != old.FoldingHighlight)
        {
            _decorationProvider.ShowFoldingControls = options.ShowFoldingControls;
            _decorationProvider.ShowFoldingHighlights = options.FoldingHighlight;
            TriggerFoldingModelChanged();
        }
    }

    /// <summary>
    /// The folding model, after running a pending update at once; <see langword="null"/> when folding is off.
    /// (Monaco returns the promise of the pending update.)
    /// </summary>
    internal FoldingModel? GetFoldingModel()
    {
        if (_updateScheduler is { IsTriggered: true })
        {
            _updateScheduler.Cancel();
            ComputeAndUpdate();
        }

        return _foldingModel;
    }

    internal void TriggerFoldingModelChanged() => _updateScheduler?.Trigger(ComputeAndUpdate);

    internal void Reveal(Position position) =>
        _viewModel!.RevealRange(
            "api",
            true,
            _viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(Range.Collapsed(position)),
            VerticalRevealType.CenterIfOutsideViewport,
            ScrollType.Smooth);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        ClearModelBinding();
        _decorationProvider = null;
        _viewModel = null;
        _model = null;
        _scheduler = null;
    }

    private void OnModelChanged()
    {
        ClearModelBinding();

        if (!_options.Folding || _model!.IsTooLargeForTokenization)
        {
            // huge files get no view model, so they cannot support hidden areas
            return;
        }

        _foldingModel = new FoldingModel(_model, _decorationProvider!);
        _hiddenRangeModel = new HiddenRangeModel(_foldingModel);
        _hiddenRangeModel.Changed += OnHiddenRangesChanged;
        _updateScheduler = new Delayer(_scheduler!, FirstUpdateDelay);
        _cursorChangedScheduler = new RunOnceScheduler(_scheduler!, RevealCursor, CursorChangedDelay);
        _model.LanguageChanged += OnLanguageChanged;
        _model.LanguageConfigurationChanged += OnLanguageConfigurationChanged;
        _model.ContentChanged += OnModelContentChanged;
        _viewModel!.EventDispatcher.OnEvent += OnViewModelEvent;
        _isModelBound = true;
        TriggerFoldingModelChanged();
    }

    // Monaco: the localToDispose store, in the order the things were added.
    private void ClearModelBinding()
    {
        if (_isModelBound)
        {
            _isModelBound = false;
            _model!.LanguageChanged -= OnLanguageChanged;
            _model.LanguageConfigurationChanged -= OnLanguageConfigurationChanged;
            _model.ContentChanged -= OnModelContentChanged;
            _viewModel!.EventDispatcher.OnEvent -= OnViewModelEvent;
        }

        _foldingModel?.Dispose();
        if (_hiddenRangeModel is not null)
        {
            _hiddenRangeModel.Changed -= OnHiddenRangesChanged;
            _hiddenRangeModel.Dispose();

            // Dispose raises Changed with no ranges only if there were some, and the handler is already detached.
            _viewModel?.SetHiddenAreas([], this);
        }

        _updateScheduler?.Dispose();
        _cursorChangedScheduler?.Dispose();
        _rangeProvider?.Dispose();
        _foldingModel = null;
        _hiddenRangeModel = null;
        _updateScheduler = null;
        _cursorChangedScheduler = null;
        _rangeProvider = null;
    }

    private void OnFoldingStrategyChanged()
    {
        _rangeProvider?.Dispose();
        _rangeProvider = null;
        TriggerFoldingModelChanged();
    }

    private void OnLanguageChanged(object? sender, ModelLanguageChangedEventArgs e) => OnFoldingStrategyChanged();

    private void OnLanguageConfigurationChanged(object? sender, EventArgs e) => OnFoldingStrategyChanged();

    private IndentRangeProvider GetRangeProvider(TextModel editorModel) =>
        _rangeProvider ??= new IndentRangeProvider(editorModel, editorModel.LanguageConfigurationRegistry, _limitReporter);

    private void OnModelContentChanged(object? sender, ModelContentChangedEventArgs e)
    {
        _hiddenRangeModel?.NotifyChangeModelContent(e);
        TriggerFoldingModelChanged();
    }

    private void ComputeAndUpdate()
    {
        FoldingModel? foldingModel = _foldingModel;
        if (foldingModel is null)
        {
            // null if the editor has been disposed, or folding turned off
            return;
        }

        FoldingRegions? foldingRanges = GetRangeProvider(foldingModel.TextModel).Compute();
        if (foldingRanges is not null)
        {
            // some cursors might have moved into hidden regions, make sure they are in expanded regions
            foldingModel.Update(foldingRanges, SelectedLines.From(_viewModel!.GetSelections()));
        }

        if (_updateScheduler is not null)
        {
            _updateScheduler.DefaultDelay = UpdateDelay;
        }
    }

    private void OnHiddenRangesChanged(object? sender, IReadOnlyList<Range> hiddenRanges)
    {
        if (_hiddenRangeModel is not null && hiddenRanges.Count > 0)
        {
            Selection[] selections = [.. _viewModel!.GetSelections()];
            if (_hiddenRangeModel.AdjustSelections(selections))
            {
                _viewModel.SetSelections("api", selections);
            }
        }

        _viewModel!.SetHiddenAreas(hiddenRanges, this);
    }

    private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
    {
        // Monaco: onDidChangeCursorPosition
        if (e is CursorStateChangedEvent && _hiddenRangeModel is { } hidden && hidden.HasRanges())
        {
            _cursorChangedScheduler!.Schedule();
        }
    }

    private void RevealCursor()
    {
        FoldingModel? foldingModel = GetFoldingModel();
        if (foldingModel is null)
        {
            return;
        }

        List<Selection> selections = _viewModel!.GetSelections();
        if (selections.Count == 0)
        {
            return;
        }

        var toToggle = new List<FoldingRegion>();
        foreach (Selection selection in selections)
        {
            int lineNumber = selection.SelectionStartLineNumber;
            if (_hiddenRangeModel is { } hidden && hidden.IsHidden(lineNumber))
            {
                toToggle.AddRange(foldingModel.GetAllRegionsAtLine(lineNumber, (r, _) => r.IsCollapsed && lineNumber > r.StartLineNumber));
            }
        }

        if (toToggle.Count > 0)
        {
            foldingModel.ToggleCollapseState(toToggle);
            Reveal(selections[0].Position);
        }
    }
}
