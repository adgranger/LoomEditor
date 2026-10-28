// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/diffEditorViewModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff;
using LoomEditor.Core.Model;
using LoomEditor.Core.Threading;
using LoomEditor.View.Features.Scheduling;

namespace LoomEditor.View.DiffEditor;

/// <summary>
/// Watches two models and the diff options, recomputes the diff off the UI thread (200 ms after an edit, at once at start) and
/// publishes a <see cref="DiffState"/> and the unchanged regions (Monaco: <c>DiffEditorViewModel</c>, subset: no edit mapping,
/// no moved text selection, no observables: see <see cref="DiffChanged"/> and <see cref="UnchangedRegionsChanged"/>).
/// </summary>
/// <remarks>Threading: UI thread only; the diff itself runs on a pool thread (DocumentDiffProvider).</remarks>
internal sealed partial class DiffEditorViewModel : IDisposable
{
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(200);

    private readonly TextModel _original;
    private readonly TextModel _modified;
    private readonly DiffEditorOptions _options;
    private readonly IDocumentDiffProvider _provider;
    private readonly IUiDispatcher _dispatcher;
    private readonly RunOnceScheduler _debouncer;
    private CancellationTokenSource _cts = new();
    private bool _lastComputeMoves;
    private bool _isDisposed;
    private List<TaskCompletionSource> _waiters = [];

    /// <summary>Starts a first computation immediately.</summary>
    public DiffEditorViewModel(
        TextModel original, TextModel modified, DiffEditorOptions options, IDocumentDiffProvider provider, IUiDispatcher dispatcher, IEditorScheduler scheduler)
    {
        _original = original;
        _modified = modified;
        _options = options;
        _provider = provider;
        _dispatcher = dispatcher;
        _debouncer = new RunOnceScheduler(scheduler, Recompute, Debounce);
        _original.ContentChanged += OnContentChanged;
        _modified.ContentChanged += OnContentChanged;
        _options.Changed += OnOptionsChanged;
        Recompute();
    }

    /// <summary>The last diff, kept while a new one is computed; null before the first result.</summary>
    public DiffState? Diff { get; private set; }

    /// <summary>False from an edit until its diff is published.</summary>
    public bool IsDiffUpToDate { get; private set; }

    /// <summary>A new <see cref="DiffState"/> was published, or <see cref="IsDiffUpToDate"/> changed.</summary>
    public event EventHandler? DiffChanged;

    /// <summary>Completes when <see cref="IsDiffUpToDate"/> is true.</summary>
    public Task WaitForDiffAsync(CancellationToken token = default)
    {
        if (IsDiffUpToDate)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiters.Add(tcs);
        token.Register(static state => ((TaskCompletionSource)state!).TrySetCanceled(), tcs);
        return tcs.Task;
    }

    /// <summary>Stops the computation and the decorations.</summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _cts.Cancel();
        _debouncer.Dispose();
        _original.ContentChanged -= OnContentChanged;
        _modified.ContentChanged -= OnContentChanged;
        _options.Changed -= OnOptionsChanged;
        RemoveDecorations();
    }

    private void OnContentChanged(object? sender, EventArgs e)
    {
        MarkStale();
        MergeTouchingRegions();
        _debouncer.Schedule();
    }

    private void OnOptionsChanged(object? sender, DiffEditorOptionsChangedEventArgs e)
    {
        var names = e.ChangedOptions;
        if (names.Contains(nameof(DiffEditorOptions.HideUnchangedRegions)))
        {
            if (!_options.HideUnchangedRegions.Enabled)
            {
                foreach (var region in _regions)
                {
                    region.CollapseAll();
                }
            }

            UnchangedRegionsChanged?.Invoke(this, EventArgs.Empty);
            Recompute();
        }
        else if (names.Contains(nameof(DiffEditorOptions.IgnoreTrimWhitespace))
            || names.Contains(nameof(DiffEditorOptions.MaxComputationTimeMs))
            || _options.ShowMovesEffective != _lastComputeMoves)
        {
            Recompute();
        }
    }

    private void MarkStale()
    {
        if (IsDiffUpToDate)
        {
            IsDiffUpToDate = false;
            DiffChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Recompute()
    {
        _debouncer.Cancel();
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        MarkStale();
        _lastComputeMoves = _options.ShowMovesEffective;

        Task<DocumentDiff> task;
        try
        {
            task = _provider.ComputeDiffAsync(
                _original, _modified, new LinesDiffComputerOptions(_options.IgnoreTrimWhitespace, _options.MaxComputationTimeMs, _lastComputeMoves), token);
        }
        catch (Exception ex)
        {
            task = Task.FromException<DocumentDiff>(ex);
        }

        // On error the old state is kept and the diff stays not up to date (Monaco rejects the autorun the same way).
        UiContinuation.Then(task, _dispatcher, OnDiffComputed, static _ => { }, token);
    }

    private void OnDiffComputed(DocumentDiff result)
    {
        result = result.Normalize(_original.LineCount, _original.GetLineMaxColumn, _modified.LineCount, _modified.GetLineMaxColumn);
        UpdateUnchangedRegions(result);
        Diff = DiffState.FromDiffResult(result);
        IsDiffUpToDate = true;
        DiffChanged?.Invoke(this, EventArgs.Empty);

        var waiters = _waiters;
        _waiters = [];
        foreach (var waiter in waiters)
        {
            waiter.TrySetResult();
        }
    }
}
