// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelImpl.ts (visibleLinesStabilized, _handleVisibleLinesChanged, onDidChangeTokens, getViewLineData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Events;

namespace LoomEditor.View.Layout;

// Tokens: the view model is the one view attached to the model. View lines are model lines (ADR 0005, decision 6), so
// no coordinate conversion is needed.
internal sealed partial class ViewModel
{
    private IAttachedView? _attachedView;
    private Func<Action, IDisposable>? _scheduleAtNextAnimationFrame;
    private IDisposable? _pendingStabilize;

    private void AttachToModelTokens(Func<Action, IDisposable> scheduleAtNextAnimationFrame)
    {
        _scheduleAtNextAnimationFrame = scheduleAtNextAnimationFrame;
        _attachedView = _model.AttachView();
        _model.TokensChanged += OnModelTokensChanged;
        ViewLayout.Scroll += OnViewLayoutScroll;
        LayoutInfoChanged += OnLayoutInfoChangedForTokens;
    }

    private void DetachFromModelTokens()
    {
        _model.TokensChanged -= OnModelTokensChanged;
        ViewLayout.Scroll -= OnViewLayoutScroll;
        LayoutInfoChanged -= OnLayoutInfoChangedForTokens;
        _pendingStabilize?.Dispose();
        _pendingStabilize = null;
        _attachedView?.Dispose();
        _attachedView = null;
    }

    /// <summary>The tokens of a line, ready for the renderer (<c>getViewLineData</c>: <c>getLineTokens(…).inflate()</c>).</summary>
    public IViewLineTokens GetViewLineTokens(int lineNumber) => _model.Tokenization.GetLineTokens(lineNumber).Inflate();

    /// <summary>
    /// <c>visibleLinesStabilized</c>: the visible lines will not change for a while, so the model may tokenize them
    /// right away. The view model calls it itself, one animation frame after the viewport changed.
    /// </summary>
    public void VisibleLinesStabilized() => SetVisibleLines(stabilized: true);

    private void HandleVisibleLinesChanged()
    {
        SetVisibleLines(stabilized: false);
        _pendingStabilize?.Dispose();
        _pendingStabilize = _scheduleAtNextAnimationFrame!(() =>
        {
            _pendingStabilize = null;
            VisibleLinesStabilized();
        });
    }

    private void SetVisibleLines(bool stabilized)
    {
        if (_disposed || _attachedView is null)
        {
            return;
        }

        PartialViewLinesViewportData viewport = ViewLayout.GetLinesViewportData();
        _attachedView.SetVisibleLines([(viewport.StartLineNumber, viewport.EndLineNumber)], stabilized);
    }

    private void OnViewLayoutScroll(object? sender, ScrollEvent e)
    {
        if (e.ScrollTopChanged || e.HeightChanged)
        {
            HandleVisibleLinesChanged();
        }
    }

    private void OnLayoutInfoChangedForTokens(object? sender, EventArgs e) => HandleVisibleLinesChanged();

    private void OnModelTokensChanged(object? sender, ModelTokensChangedEventArgs e)
    {
        EventDispatcher.EmitSingleViewEvent(new ViewTokensChangedEvent([.. e.Ranges.Select(r => new ViewLineRange(r.FromLineNumber, r.ToLineNumber))]));
    }
}
