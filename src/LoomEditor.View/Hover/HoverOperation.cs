// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/hoverOperation.ts (HoverOperation, Debouncer)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Threading;
using LoomEditor.View.Features.Scheduling;

namespace LoomEditor.View.Hover;

/// <summary>
/// Runs a hover computation with Monaco's timings. With a delay of 300 ms: at 150 ms the async computation starts (its results wait);
/// at 300 ms the sync computation runs and the results are raised; at 900 ms, if the async part is still pending, the loading flag is raised.
/// Difference from Monaco: the async part is one task of a list, not an async iterable.
/// </summary>
/// <remarks>Threading: UI thread only (the provider tasks complete anywhere; the results come back through the dispatcher).</remarks>
internal sealed class HoverOperation : IDisposable
{
    private enum State
    {
        Idle,
        FirstWait,
        SecondWait,
        WaitingForAsync,
        WaitingForAsyncShowingLoading,
    }

    // Monaco: Debouncer
    private sealed class Debouncer : IDisposable
    {
        private readonly RunOnceScheduler _scheduler;
        private HoverComputerOptions? _options;

        public Debouncer(IEditorScheduler scheduler, Action<HoverComputerOptions> runner) =>
            _scheduler = new RunOnceScheduler(scheduler, () => runner(_options!), TimeSpan.Zero);

        public void Schedule(HoverComputerOptions options, TimeSpan delay)
        {
            _options = options;
            _scheduler.Schedule(delay);
        }

        public void Cancel() => _scheduler.Cancel();

        public void Dispose() => _scheduler.Dispose();
    }

    private readonly IUiDispatcher _dispatcher;
    private readonly IHoverComputer _computer;
    private readonly Func<TimeSpan> _getHoverDelay;
    private readonly Debouncer _asyncScheduler;
    private readonly Debouncer _syncScheduler;
    private readonly Debouncer _loadingScheduler;
    private State _state = State.Idle;
    private CancellationTokenSource? _cts;
    private bool _asyncDone;
    private List<IHoverPart> _result = [];
    private TimeSpan _hoverTime;
    private bool _disposed;

    public HoverOperation(IEditorScheduler scheduler, IUiDispatcher dispatcher, IHoverComputer computer, Func<TimeSpan> getHoverDelay)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(computer);
        ArgumentNullException.ThrowIfNull(getHoverDelay);
        _dispatcher = dispatcher;
        _computer = computer;
        _getHoverDelay = getHoverDelay;
        _asyncScheduler = new Debouncer(scheduler, TriggerAsyncComputation);
        _syncScheduler = new Debouncer(scheduler, TriggerSyncComputation);
        _loadingScheduler = new Debouncer(scheduler, TriggerLoadingMessage);
    }

    /// <summary>Raised with the parts found so far, once the hover delay has passed.</summary>
    public event EventHandler<HoverResult>? Result;

    /// <summary>The options of the last <see cref="Start"/>; null after <see cref="Cancel"/>.</summary>
    public HoverComputerOptions? Options { get; private set; }

    private TimeSpan FirstWaitTime => _hoverTime / 2;

    private TimeSpan SecondWaitTime => _hoverTime - FirstWaitTime;

    private TimeSpan LoadingMessageTime => 3 * _hoverTime;

    /// <summary>Starts the computation; a delayed start is ignored unless the operation is idle.</summary>
    public void Start(HoverStartMode mode, HoverComputerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (_disposed)
        {
            return;
        }

        _hoverTime = _getHoverDelay();
        if (mode == HoverStartMode.Delayed)
        {
            if (_state == State.Idle)
            {
                SetState(State.FirstWait, options);
                _asyncScheduler.Schedule(options, FirstWaitTime);
                _loadingScheduler.Schedule(options, LoadingMessageTime);
            }

            return;
        }

        switch (_state)
        {
            case State.Idle:
                TriggerAsyncComputation(options);
                _syncScheduler.Cancel();
                TriggerSyncComputation(options);
                break;
            case State.SecondWait:
                _syncScheduler.Cancel();
                TriggerSyncComputation(options);
                break;
        }
    }

    /// <summary>Stops everything: the pending timers, the provider requests and the results.</summary>
    public void Cancel()
    {
        _asyncScheduler.Cancel();
        _syncScheduler.Cancel();
        _loadingScheduler.Cancel();
        CancelAsync();
        _result = [];
        Options = null;
        _state = State.Idle;
        _asyncDone = false;
    }

    /// <summary>Cancels and never raises <see cref="Result"/> again.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Cancel();
        _disposed = true;
        _asyncScheduler.Dispose();
        _syncScheduler.Dispose();
        _loadingScheduler.Dispose();
    }

    private void CancelAsync()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private void SetState(State state, HoverComputerOptions options)
    {
        Options = options;
        _state = state;
        FireResult(options);
    }

    private void TriggerAsyncComputation(HoverComputerOptions options)
    {
        SetState(State.SecondWait, options);
        _syncScheduler.Schedule(options, SecondWaitTime);

        _asyncDone = false;
        CancelAsync();
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;
        Task<IReadOnlyList<IHoverPart>>? task = _computer.ComputeAsync(options, token);
        if (task is null)
        {
            _asyncDone = true;
            return;
        }

        UiContinuation.Then(
            task,
            _dispatcher,
            parts =>
            {
                _result.AddRange(parts);
                OnAsyncDone(options);
            },
            _ => OnAsyncDone(options),
            token);
    }

    private void OnAsyncDone(HoverComputerOptions options)
    {
        _asyncDone = true;
        FireResult(options);
        if (_state is State.WaitingForAsync or State.WaitingForAsyncShowingLoading)
        {
            SetState(State.Idle, options);
        }
    }

    private void TriggerSyncComputation(HoverComputerOptions options)
    {
        _result.AddRange(_computer.ComputeSync(options));
        SetState(_asyncDone ? State.Idle : State.WaitingForAsync, options);
    }

    private void TriggerLoadingMessage(HoverComputerOptions options)
    {
        if (_state == State.WaitingForAsync)
        {
            SetState(State.WaitingForAsyncShowingLoading, options);
        }
    }

    private void FireResult(HoverComputerOptions options)
    {
        if (_disposed || _state is State.FirstWait or State.SecondWait)
        {
            // no results before the hover time
            return;
        }

        Result?.Invoke(this, new HoverResult([.. _result], _state == State.Idle, _state == State.WaitingForAsyncShowingLoading, options));
    }
}
