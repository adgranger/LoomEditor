// Ported from microsoft/vscode: src/vs/editor/contrib/stickyScroll/browser/stickyScrollProvider.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using LoomEditor.View.Events;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.StickyScroll;

/// <summary>
/// Keeps the sticky model up to date (debounced on edits, at once when the hidden areas change) and answers which sticky lines
/// intersect a range, skipping the folded ones (Monaco: <c>StickyLineCandidateProvider</c>).
/// </summary>
/// <remarks>
/// Behavior differences: the update is synchronous (no promise, no cancellation); one model per editor, so Monaco's
/// <c>onDidChangeModel</c> path is the constructor; the document symbol listener comes with phase 8; the line height is the
/// default one (custom heights are not ported). Threading: UI thread only.
/// </remarks>
internal sealed class StickyLineCandidateProvider : IDisposable
{
    private static readonly TimeSpan UpdateDelay = TimeSpan.FromMilliseconds(50);

    private readonly TextModel _model;
    private readonly ViewModel _viewModel;
    private readonly Func<EditorStickyScrollOptions> _options;
    private readonly RunOnceScheduler _updateSoon;
    private StickyModelProvider? _stickyModelProvider;
    private StickyModel? _stickyModel;
    private bool _listening;

    /// <summary>Creates the provider and, when sticky scroll is enabled, computes the first model.</summary>
    public StickyLineCandidateProvider(TextModel model, ViewModel viewModel, IEditorScheduler scheduler, Func<EditorStickyScrollOptions> options)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(options);

        _model = model;
        _viewModel = viewModel;
        _options = options;
        _updateSoon = new RunOnceScheduler(scheduler, Update, UpdateDelay);
        ReadConfiguration();
    }

    /// <summary>Raised after the sticky model was recomputed (Monaco: <c>onDidChangeStickyScroll</c>).</summary>
    public event EventHandler? StickyScrollChanged;

    /// <summary>The version of the sticky model, or <see langword="null"/> when there is none (Monaco: <c>getVersionId</c>).</summary>
    public int? GetVersionId() => _stickyModel?.Version;

    /// <summary>Applies the sticky scroll options again; call it when they changed (Monaco: <c>readConfiguration</c>).</summary>
    public void ReadConfiguration()
    {
        StopListening();
        if (!_options().Enabled)
        {
            return;
        }

        _model.RawContentChanged += OnRawContentChanged;
        _viewModel.EventDispatcher.OnEvent += OnViewModelEvent;
        _listening = true;
        // Phase 8: the document symbol provider listener
        _stickyModelProvider = new StickyModelProvider(_model, new RangesLimitReporter(() => 5000));
        Update();
    }

    /// <summary>Recomputes the sticky model now (Monaco: <c>update</c>).</summary>
    public void Update()
    {
        _stickyModel = _stickyModelProvider is null || _model.IsTooLargeForTokenization ? null : _stickyModelProvider.Update();
        StickyScrollChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Gets the sticky line candidates intersecting <paramref name="range"/>, without the folded ones.</summary>
    public IReadOnlyList<StickyLineCandidate> GetCandidateStickyLinesIntersecting(StickyRange range)
    {
        if (_stickyModel?.Element is not { } element)
        {
            return [];
        }

        var candidates = new List<StickyLineCandidate>();
        GetCandidatesFromStickyModel(range, element, candidates, 0, -1);
        return FilterHiddenRanges(candidates);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        StopListening();
        _updateSoon.Dispose();
    }

    private void StopListening()
    {
        if (_listening)
        {
            _model.RawContentChanged -= OnRawContentChanged;
            _viewModel.EventDispatcher.OnEvent -= OnViewModelEvent;
            _listening = false;
        }

        _stickyModelProvider?.Dispose();
        _stickyModelProvider = null;
    }

    private void OnRawContentChanged(object? sender, ModelRawContentChangedEventArgs e) => _updateSoon.Schedule();

    private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
    {
        if (e is HiddenAreasChangedEvent)
        {
            Update();
        }
    }

    private void GetCandidatesFromStickyModel(
        StickyRange range, StickyElement outlineModel, List<StickyLineCandidate> result, int top, int lastStartLineNumber)
    {
        if (outlineModel.Children.Count == 0)
        {
            return;
        }

        int lastLine = lastStartLineNumber;
        var childrenStartLines = new List<int>();
        foreach (StickyElement child in outlineModel.Children)
        {
            if (child.Range is not null)
            {
                childrenStartLines.Add(child.Range.StartLineNumber);
            }
        }

        int lowerBound = UpdateIndex(childrenStartLines.BinarySearch(range.StartLineNumber));
        int upperBound = UpdateIndex(childrenStartLines.BinarySearch(range.EndLineNumber));

        for (int i = lowerBound; i <= upperBound; i++)
        {
            if (i >= outlineModel.Children.Count || outlineModel.Children[i].Range is not { } childRange)
            {
                continue;
            }

            StickyElement child = outlineModel.Children[i];
            int startLineNumber = childRange.StartLineNumber;
            int endLineNumber = childRange.EndLineNumber;
            if (endLineNumber > startLineNumber + 1
                && range.StartLineNumber <= endLineNumber + 1
                && startLineNumber - 1 <= range.EndLineNumber
                && startLineNumber != lastLine
                && _model.IsValidRange(new Range(startLineNumber, 1, endLineNumber, 1)))
            {
                lastLine = startLineNumber;
                int lineHeight = GetLineHeight(startLineNumber);
                result.Add(new StickyLineCandidate(startLineNumber, endLineNumber - 1, top, lineHeight));
                GetCandidatesFromStickyModel(range, child, result, top + lineHeight, startLineNumber);
            }
        }
    }

    private int GetLineHeight(int modelLineNumber)
    {
        Position viewPosition = _viewModel.CoordinatesConverter.ConvertModelPositionToViewPosition(new Position(modelLineNumber, 1));
        return _viewModel.ViewLayout.GetLineHeightForLineNumber(viewPosition.LineNumber);
    }

    private List<StickyLineCandidate> FilterHiddenRanges(List<StickyLineCandidate> candidates)
    {
        IReadOnlyList<Range> hiddenRanges = _viewModel.GetHiddenAreas();
        return candidates.FindAll(candidate => !hiddenRanges.Any(hiddenRange =>
            candidate.StartLineNumber >= hiddenRange.StartLineNumber && candidate.EndLineNumber <= hiddenRange.EndLineNumber + 1));
    }

    // Monaco's binarySearch encoding: a miss returns -(insertion point) - 1, which is what List.BinarySearch returns too.
    private static int UpdateIndex(int index)
    {
        if (index == -1)
        {
            return 0;
        }

        return index < 0 ? -index - 2 : index;
    }
}
