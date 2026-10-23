// Ported from microsoft/vscode: src/vs/editor/contrib/stickyScroll/browser/stickyScrollController.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.StickyScroll;

/// <summary>
/// Sticky scroll: on scroll, resize, token and candidate changes, finds the lines that stick at the top of the editor
/// (at most 25 % of its height and <see cref="EditorStickyScrollOptions.MaxLineCount"/>) and publishes them in
/// <see cref="ViewModel.StickyScroll"/> for the renderer (Monaco: <c>StickyScrollController</c>).
/// </summary>
/// <remarks>
/// Behavior differences: the state is computed synchronously (no promise); the widget, the context keys, the focus, the
/// hover, the context menu and the relative line numbers are not ported; the rebuild-from-line bookkeeping is left to the
/// renderer (P6-59), which redraws what <see cref="ViewModel.StickyScroll"/> says. The options are read from
/// <see cref="EditorLayoutOptions.StickyScroll"/> when <see cref="ViewModel.OptionsChanged"/> is raised. Threading: UI thread only.
/// </remarks>
internal sealed partial class StickyScrollFeature : IEditorFeature
{
    private ViewModel? _viewModel;
    private TextModel? _model;
    private EditorContext? _editorContext;
    private StickyLineCandidateProvider? _provider;
    private EditorStickyScrollOptions _options = new();
    private StickyScrollWidgetState _widgetState = StickyScrollWidgetState.Empty;
    private int _maxStickyLines = int.MaxValue;
    private int? _showEndForLine;
    private bool _enabled;
    private IDisposable? _toggleRegistration;

    /// <inheritdoc />
    public string Id => "loom.stickyScroll";

    /// <summary>What the widget shows now (Monaco: <c>stickyScrollWidgetState</c>).</summary>
    internal StickyScrollWidgetState WidgetState => _widgetState;

    /// <summary>The candidate provider (Monaco: <c>stickyScrollCandidateProvider</c>).</summary>
    internal StickyLineCandidateProvider? CandidateProvider => _provider;

    /// <summary>The folding model the folding icons are read from; <see langword="null"/> when folding is off.</summary>
    internal FoldingModel? FoldingModel { get; private set; }

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
        _editorContext = context as EditorContext;
        if (_editorContext is not null)
        {
            _editorContext.StickyScrollFeature = this;
        }

        _options = viewModel.LayoutOptions.StickyScroll;
        _provider = new StickyLineCandidateProvider(_model, viewModel, context.Scheduler, () => _viewModel!.LayoutOptions.StickyScroll);
        _toggleRegistration = context.Commands.Register(new EditorCommand(StickyScrollCommands.ToggleStickyScroll, Toggle));
        viewModel.LayoutInfoChanged += OnLayoutInfoChanged;
        viewModel.OptionsChanged += OnOptionsChanged;
        ReadConfiguration();
        OnDidResize();
    }

    // Monaco: ToggleStickyScroll flips `editor.stickyScroll.enabled`; here the view model owns the option.
    private void Toggle()
    {
        ViewModel viewModel = _viewModel!;
        EditorLayoutOptions layout = viewModel.LayoutOptions;
        viewModel.SetOptions(layout with { StickyScroll = layout.StickyScroll with { Enabled = !layout.StickyScroll.Enabled } }, viewModel.ViewLayoutOptions);
    }

    /// <summary>Monaco: <c>findScrollWidgetState</c>.</summary>
    internal StickyScrollWidgetState FindScrollWidgetState()
    {
        ViewModel viewModel = _viewModel!;
        int maxNumberStickyLines = Math.Min(_maxStickyLines, viewModel.LayoutOptions.StickyScroll.MaxLineCount);
        double scrollTop = viewModel.ViewLayout.GetCurrentScrollTop();
        int lastLineRelativePosition = 0;
        var startLineNumbers = new List<int>();
        var endLineNumbers = new List<int>();
        List<Range> visibleRanges = viewModel.GetVisibleRanges();
        if (visibleRanges.Count != 0)
        {
            var fullVisibleRange = new StickyRange(visibleRanges[0].StartLineNumber, visibleRanges[^1].EndLineNumber);
            foreach (StickyLineCandidate range in _provider!.GetCandidateStickyLinesIntersecting(fullVisibleRange))
            {
                int start = range.StartLineNumber;
                int end = range.EndLineNumber;
                int topOfElement = range.Top;
                int bottomOfElement = topOfElement + range.Height;
                double topOfBeginningLine = GetTopForLineNumber(start) - scrollTop;
                double bottomOfEndLine = GetBottomForLineNumber(end) - scrollTop;
                if (topOfElement > topOfBeginningLine && topOfElement <= bottomOfEndLine)
                {
                    startLineNumbers.Add(start);
                    endLineNumbers.Add(end + 1);
                    if (bottomOfElement > bottomOfEndLine)
                    {
                        lastLineRelativePosition = (int)(bottomOfEndLine - bottomOfElement);
                    }
                }

                if (startLineNumbers.Count == maxNumberStickyLines)
                {
                    break;
                }
            }
        }

        return new StickyScrollWidgetState(startLineNumbers, endLineNumbers, lastLineRelativePosition, _showEndForLine);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.LayoutInfoChanged -= OnLayoutInfoChanged;
        _viewModel.OptionsChanged -= OnOptionsChanged;
        StopListening();
        _toggleRegistration?.Dispose();
        _toggleRegistration = null;
        _provider!.Dispose();
        _provider = null;
        ResetState();
        _viewModel = null;
        _model = null;
        if (_editorContext is not null)
        {
            _editorContext.StickyScrollFeature = null;
        }

        _editorContext = null;
    }

    // Monaco: getTopForLineNumber / getBottomForLineNumber take model line numbers.
    private int GetTopForLineNumber(int modelLineNumber) =>
        _viewModel!.ViewLayout.GetVerticalOffsetForLineNumber(ToViewLineNumber(modelLineNumber));

    private int GetBottomForLineNumber(int modelLineNumber)
    {
        int viewLineNumber = ToViewLineNumber(modelLineNumber);
        ViewLayout layout = _viewModel!.ViewLayout;
        return layout.GetVerticalOffsetForLineNumber(viewLineNumber) + layout.GetLineHeightForLineNumber(viewLineNumber);
    }

    private int ToViewLineNumber(int modelLineNumber) =>
        _viewModel!.CoordinatesConverter.ConvertModelPositionToViewPosition(new Position(modelLineNumber, 1)).LineNumber;

    private void OnLayoutInfoChanged(object? sender, EventArgs e) => OnDidResize();

    private void OnOptionsChanged(object? sender, EventArgs e)
    {
        EditorStickyScrollOptions options = _viewModel!.LayoutOptions.StickyScroll;
        if (options != _options)
        {
            _options = options;
            _provider!.ReadConfiguration();
            ReadConfiguration();
            OnDidResize();
        }
    }

    // Monaco: _readConfiguration, without the relative line numbers.
    private void ReadConfiguration()
    {
        if (!_options.Enabled)
        {
            StopListening();
            ResetState();
            return;
        }

        if (!_enabled)
        {
            _viewModel!.ViewLayout.Scroll += OnScroll;
            _model!.TokensChanged += OnTokensChanged;
            _provider!.StickyScrollChanged += OnStickyScrollChanged;
            _enabled = true;
        }
    }

    private void StopListening()
    {
        if (_enabled)
        {
            _viewModel!.ViewLayout.Scroll -= OnScroll;
            _model!.TokensChanged -= OnTokensChanged;
            _provider!.StickyScrollChanged -= OnStickyScrollChanged;
            _enabled = false;
        }
    }

    private void OnScroll(object? sender, ScrollEvent e)
    {
        if (e.ScrollTopChanged)
        {
            _showEndForLine = null;
            RenderStickyScroll();
        }
    }

    private void OnStickyScrollChanged(object? sender, EventArgs e)
    {
        _showEndForLine = null;
        RenderStickyScroll();
    }

    private void OnTokensChanged(object? sender, ModelTokensChangedEventArgs e)
    {
        if (_viewModel!.StickyScroll is { } data
            && data.LineNumbers.Any(line => e.Ranges.Any(range => line >= range.FromLineNumber && line <= range.ToLineNumber)))
        {
            RenderStickyScroll();
        }
    }

    private void OnDidResize()
    {
        // Make sure sticky scroll doesn't take up more than 25% of the editor
        double theoreticalLines = (double)_viewModel!.LayoutInfo.Height / _viewModel.FontInfo.LineHeight;
        _maxStickyLines = (int)Math.Round(theoreticalLines * .25, MidpointRounding.AwayFromZero);
        RenderStickyScroll();
    }

    private void RenderStickyScroll()
    {
        if (!_enabled)
        {
            return;
        }

        if (_model!.IsTooLargeForTokenization)
        {
            ResetState();
            return;
        }

        // The sticky model of an older version of the text is stale: wait for the provider to catch up.
        int? providerVersion = _provider!.GetVersionId();
        if (providerVersion is null || providerVersion == _model.VersionId)
        {
            UpdateState();
        }
    }

    private void UpdateState()
    {
        FoldingModel = _editorContext?.FoldingFeature?.FoldingModel;
        _widgetState = FindScrollWidgetState();
        _viewModel!.StickyScroll = StickyScrollRenderData.From(_widgetState, _viewModel);
    }

    private void ResetState()
    {
        FoldingModel = null;
        _widgetState = StickyScrollWidgetState.Empty;
        _viewModel!.StickyScroll = null;
    }
}
