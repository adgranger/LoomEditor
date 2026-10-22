// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (Minimap)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Minimap;

/// <summary>The model side of Monaco's <c>Minimap</c> view part: <see cref="IMinimapModel"/> over a <see cref="ViewModel"/>.</summary>
/// <remarks>
/// <para>Threading: UI thread only.</para>
/// <para>
/// Behavior differences: there are no <c>ViewLines*</c>/<c>ViewFlushed</c> view events, so a content or injected text
/// change of the model, and a change of the layout (word wrap, hidden areas), is treated as <c>onFlushed</c>: all the minimap
/// lines are redrawn instead of only the changed ones. The options are rebuilt by <see cref="UpdateOptions"/> (called by the
/// renderer each frame) instead of on configuration events. The selections start as the current view selections, not empty.
/// The section header members are not ported. Monaco's <c>prepareRender</c> is <see cref="PrepareRender"/>.
/// </para>
/// </remarks>
internal sealed class MinimapModel : IMinimapModel, IViewEventHandler, IDisposable
{
    private readonly ViewModel _viewModel;
    private readonly IMinimapRenderTarget _target;
    private readonly Func<int, string, MinimapCharRenderer> _charRendererFactory;
    private EditorTheme _theme;
    private IReadOnlyList<Selection> _selections;
    private IReadOnlyList<Selection>? _minimapSelections;
    private MinimapSamplingState? _samplingState;
    private bool _shouldCheckSampling;

    /// <summary>Starts following <paramref name="viewModel"/>; <see cref="Dispose"/> stops.</summary>
    public MinimapModel(
        ViewModel viewModel, IMinimapRenderTarget target, EditorTheme theme, Func<int, string, MinimapCharRenderer> charRendererFactory)
    {
        _viewModel = viewModel;
        _target = target;
        _theme = theme;
        _charRendererFactory = charRendererFactory;
        TokensColorTracker = new MinimapTokensColorTracker(theme);
        _selections = viewModel.GetViewSelections();
        Options = CreateOptions();
        _samplingState = MinimapSamplingState.Compute(Options, viewModel.GetLineCount(), null).State;

        viewModel.EventDispatcher.AddViewEventHandler(this);
        viewModel.Model.RawContentChanged += OnModelContentChanged;
        viewModel.Model.InjectedTextChanged += OnModelContentChanged;
        viewModel.LayoutInfoChanged += OnModelContentChanged;
    }

    /// <inheritdoc/>
    public MinimapTokensColorTracker TokensColorTracker { get; private set; }

    /// <inheritdoc/>
    public MinimapOptions Options { get; private set; }

    /// <summary>Whether the minimap shows a sample of the view lines.</summary>
    public bool IsSampling => _samplingState is not null;

    /// <summary>Maps a view line to its minimap line (identity when not sampling). Monaco: <c>modelLineToMinimapLine</c>.</summary>
    public int ViewLineToMinimapLine(int viewLineNumber) =>
        _samplingState?.ModelLineToMinimapLine(viewLineNumber) ?? viewLineNumber;

    public void Dispose()
    {
        _viewModel.EventDispatcher.RemoveViewEventHandler(this);
        _viewModel.Model.RawContentChanged -= OnModelContentChanged;
        _viewModel.Model.InjectedTextChanged -= OnModelContentChanged;
        _viewModel.LayoutInfoChanged -= OnModelContentChanged;
    }

    private MinimapOptions CreateOptions() => new(
        _viewModel.LayoutInfo, _viewModel.LayoutOptions.Minimap, _viewModel.FontInfo,
        _viewModel.ViewLayoutOptions.ScrollBeyondLastLine, _theme, TokensColorTracker, _charRendererFactory);

    /// <summary>
    /// Rebuilds the options from the view model and <paramref name="theme"/>; when they changed, recreates the sampling and
    /// returns <c>true</c>. Monaco: <c>_onOptionsMaybeChanged</c> (and <c>onThemeChanged</c>).
    /// </summary>
    public bool UpdateOptions(EditorTheme theme)
    {
        if (!ReferenceEquals(theme, _theme))
        {
            _theme = theme;
            TokensColorTracker = new MinimapTokensColorTracker(theme);
            _target.OnThemeChanged();
        }

        MinimapOptions options = CreateOptions();
        if (Options.EqualsOptions(options))
        {
            return false;
        }

        Options = options;
        RecreateLineSampling();
        _target.OnDidChangeOptions();
        return true;
    }

    /// <summary>Recreates the sampling if a change asked for it. Monaco: <c>prepareRender</c>.</summary>
    public void PrepareRender()
    {
        if (_shouldCheckSampling)
        {
            _shouldCheckSampling = false;
            RecreateLineSampling();
        }
    }

    // Monaco: onFlushed
    private void OnModelContentChanged(object? sender, EventArgs e)
    {
        if (_samplingState is not null)
        {
            _shouldCheckSampling = true;
        }

        _target.OnFlushed();
    }

    /// <inheritdoc/>
    public void HandleEvents(IReadOnlyList<ViewEvent> events)
    {
        foreach (ViewEvent e in events)
        {
            switch (e)
            {
                case ViewCursorStateChangedEvent cursor:
                    _selections = cursor.Selections;
                    _minimapSelections = null;
                    _target.OnSelectionChanged();
                    break;
                case ViewDecorationsChangedEvent { AffectsMinimap: true }:
                    _target.OnDecorationsChanged();
                    break;
                case ViewTokensChangedEvent tokens:
                    OnTokensChanged(tokens);
                    break;
            }
        }
    }

    private void OnTokensChanged(ViewTokensChangedEvent e)
    {
        if (_samplingState is null)
        {
            _target.OnTokensChanged(e.Ranges);
            return;
        }

        var ranges = new List<ViewLineRange>();
        foreach (ViewLineRange range in e.Ranges)
        {
            if (_samplingState.ModelLineRangeToMinimapLineRange(range.FromLineNumber, range.ToLineNumber) is var (from, to))
            {
                ranges.Add(new ViewLineRange(from, to));
            }
        }

        if (ranges.Count > 0)
        {
            _target.OnTokensChanged(ranges);
        }
    }

    private void RecreateLineSampling()
    {
        _minimapSelections = null;

        bool wasSampling = _samplingState is not null;
        (MinimapSamplingState? samplingState, IReadOnlyList<SamplingStateEvent> events) =
            MinimapSamplingState.Compute(Options, _viewModel.GetLineCount(), _samplingState);
        _samplingState = samplingState;

        if (wasSampling && _samplingState is not null)
        {
            // was sampling, is sampling
            foreach (SamplingStateEvent e in events)
            {
                switch (e)
                {
                    case SamplingStateLinesDeleted deleted:
                        _target.OnLinesDeleted(deleted.DeleteFromLineNumber, deleted.DeleteToLineNumber);
                        break;
                    case SamplingStateLinesInserted inserted:
                        _target.OnLinesInserted(inserted.InsertFromLineNumber, inserted.InsertToLineNumber);
                        break;
                    case SamplingStateFlush:
                        _target.OnFlushed();
                        break;
                }
            }
        }
    }

    // Maps a 1-based minimap line to the view line it shows.
    private int ViewLine(int minimapLineNumber) => _samplingState?.MinimapLines[minimapLineNumber - 1] ?? minimapLineNumber;

    /// <inheritdoc/>
    public int GetLineCount() => _samplingState?.MinimapLines.Count ?? _viewModel.GetLineCount();

    /// <inheritdoc/>
    public int GetRealLineCount() => _viewModel.GetLineCount();

    /// <inheritdoc/>
    public string GetLineContent(int lineNumber) => _viewModel.GetLineContent(ViewLine(lineNumber));

    /// <inheritdoc/>
    public int GetLineMaxColumn(int lineNumber) => _viewModel.GetLineMaxColumn(ViewLine(lineNumber));

    /// <inheritdoc/>
    public ViewLineData?[] GetMinimapLinesRenderingData(int startLineNumber, int endLineNumber, bool[] needed)
    {
        if (_samplingState is null)
        {
            return _viewModel.GetMinimapLinesRenderingData(startLineNumber, endLineNumber, needed).Data;
        }

        var result = new ViewLineData?[Math.Max(0, endLineNumber - startLineNumber + 1)];
        for (int lineIndex = 0; lineIndex < result.Length; lineIndex++)
        {
            if (needed[lineIndex])
            {
                int viewLineNumber = ViewLine(startLineNumber + lineIndex);
                result[lineIndex] = _viewModel.GetMinimapLinesRenderingData(viewLineNumber, viewLineNumber, [true]).Data[0];
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Selection> GetSelections()
    {
        if (_minimapSelections is null)
        {
            if (_samplingState is null)
            {
                _minimapSelections = _selections;
            }
            else
            {
                var minimapSelections = new List<Selection>(_selections.Count);
                foreach (Selection selection in _selections)
                {
                    (int start, int end) = _samplingState.DecorationLineRangeToMinimapLineRange(selection.Range.StartLineNumber, selection.Range.EndLineNumber);
                    minimapSelections.Add(new Selection(start, selection.Range.StartColumn, end, selection.Range.EndColumn));
                }

                _minimapSelections = minimapSelections;
            }
        }

        return _minimapSelections;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ViewModelDecoration> GetMinimapDecorationsInViewport(int startLineNumber, int endLineNumber)
    {
        int viewStart = ViewLine(startLineNumber);
        int viewEnd = ViewLine(endLineNumber);
        var visibleRange = new Range(viewStart, 1, viewEnd, _viewModel.GetLineMaxColumn(viewEnd));
        IReadOnlyList<ViewModelDecoration> decorations = _viewModel.GetMinimapDecorationsInRange(visibleRange);
        if (_samplingState is null)
        {
            return decorations;
        }

        var result = new List<ViewModelDecoration>();
        foreach (ViewModelDecoration decoration in decorations)
        {
            if (decoration.Options.Minimap is null)
            {
                continue;
            }

            Range range = decoration.Range;
            result.Add(new ViewModelDecoration(
                new Range(
                    _samplingState.ModelLineToMinimapLine(range.StartLineNumber), range.StartColumn,
                    _samplingState.ModelLineToMinimapLine(range.EndLineNumber), range.EndColumn),
                decoration.Options));
        }

        return result;
    }

    /// <inheritdoc/>
    public TextModelOptions GetOptions() => _viewModel.Model.Options;

    /// <inheritdoc/>
    public void RevealLineNumber(int lineNumber)
    {
        lineNumber = ViewLine(lineNumber);
        _viewModel.RevealRange("mouse", false, new Range(lineNumber, 1, lineNumber, 1), VerticalRevealType.Center, ScrollType.Smooth);
    }

    /// <inheritdoc/>
    public void SetScrollTop(double scrollTop) =>
        _viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(ScrollTop: scrollTop), ScrollType.Immediate);
}
