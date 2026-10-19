// Ported from microsoft/vscode: src/vs/editor/common/model/tokens/abstractSyntaxTokenBackend.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>The visible lines of an attached view (Monaco: <c>AttachedViewState</c>); line ranges are end exclusive.</summary>
/// <param name="VisibleLineRanges">The visible line ranges (Monaco's <c>LineRange</c>s, here <see cref="OffsetRange"/>s of line numbers).</param>
/// <param name="Stabilized">See <see cref="IAttachedView.SetVisibleLines"/>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record AttachedViewState(IReadOnlyList<OffsetRange> VisibleLineRanges, bool Stabilized);

/// <summary>
/// The views attached to a model (Monaco: <c>AttachedViews</c>). Not ported: the <c>visibleLineRanges</c> observable
/// (used by tree-sitter only).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class AttachedViews
{
    private readonly HashSet<AttachedViewImpl> _views = [];

    /// <summary><c>onDidChangeVisibleRanges</c>: a view and its new state, or <see langword="null"/> when it was detached.</summary>
    public event Action<IAttachedView, AttachedViewState?>? VisibleRangesChanged;

    /// <summary><c>attachView</c>; <paramref name="onDispose"/> runs when the returned view is disposed (the model detaches it).</summary>
    public IAttachedView AttachView(Action<IAttachedView> onDispose)
    {
        AttachedViewImpl view = null!;
        view = new AttachedViewImpl(state => VisibleRangesChanged?.Invoke(view, state), () => onDispose(view));
        _views.Add(view);
        return view;
    }

    /// <summary><c>detachView</c>.</summary>
    public void DetachView(IAttachedView view)
    {
        _views.Remove((AttachedViewImpl)view);
        VisibleRangesChanged?.Invoke(view, null);
    }

    private sealed class AttachedViewImpl(Action<AttachedViewState> handleStateChange, Action onDispose) : IAttachedView
    {
        private bool _isDisposed;

        public void SetVisibleLines(IReadOnlyList<(int StartLineNumber, int EndLineNumber)> visibleLines, bool stabilized)
        {
            OffsetRange[] visibleLineRanges = [.. visibleLines.Select(line => new OffsetRange(line.StartLineNumber, line.EndLineNumber + 1))];
            handleStateChange(new AttachedViewState(visibleLineRanges, stabilized));
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                onDispose();
            }
        }
    }
}

/// <summary>
/// Refreshes the tokens of the visible lines of one view when they changed (Monaco: <c>AttachedViewHandler</c>). The
/// delayed refresh of an unstabilized view goes through <see cref="ITokenizationScheduler.Post"/> instead of Monaco's 50ms
/// <c>RunOnceScheduler</c>; a newer state replaces the pending refresh.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class AttachedViewHandler(Action refreshTokens, ITokenizationScheduler? scheduler)
{
    private IReadOnlyList<OffsetRange> _computedLineRanges = [];
    private int _version;

    /// <summary><c>lineRanges</c>.</summary>
    public IReadOnlyList<OffsetRange> LineRanges { get; private set; } = [];

    /// <summary><c>handleStateChange</c>: refreshes now when stabilized (or without a scheduler), else at the next post.</summary>
    public void HandleStateChange(AttachedViewState state)
    {
        LineRanges = state.VisibleLineRanges;
        int version = ++_version;
        if (state.Stabilized || scheduler is null)
        {
            Update();
        }
        else
        {
            scheduler.Post(() =>
            {
                if (version == _version)
                {
                    Update();
                }
            });
        }
    }

    /// <summary>Cancels the pending refresh (Monaco: <c>dispose</c>).</summary>
    public void Cancel() => _version++;

    private void Update()
    {
        if (_computedLineRanges.SequenceEqual(LineRanges))
        {
            return;
        }

        _computedLineRanges = LineRanges;
        refreshTokens();
    }
}
