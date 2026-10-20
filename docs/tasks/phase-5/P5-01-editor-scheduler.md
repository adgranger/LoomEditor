# P5-01 — Editor scheduler: `IEditorScheduler`, `RunOnceScheduler`, `Delayer`, `TimeoutTimer`

- **Status:** todo
- **Depends on:** P4-50
- **Size:** M

## Goal

Features can run work after a delay without timers of their own: `IEditorContext.Scheduler` schedules an action on
the UI thread, and `Features/Scheduling/` has the ports of Monaco's `RunOnceScheduler`, `Delayer` and `TimeoutTimer`
on top of it (ADR 0009, decision 10). The control schedules with a `DispatcherQueueTimer`; tests schedule with the
new `ManualEditorScheduler`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/base/common/async.ts` | 1190–1263 | `RunOnceScheduler` (constructor, `dispose`, `cancel`, `schedule`, `delay`, `isScheduled`, `flush`, `onTimeout`, `doRun`) |
| `src/vs/base/common/async.ts` | 365–382, 422–492 | `IScheduledLater`, `timeoutDeferred` (only the `isTriggered` semantics), `Delayer` (`trigger`, `isTriggered`, `cancel`, `cancelTimeout`, `dispose`) — without promises (see below) |
| `src/vs/base/common/async.ts` | 1104–1156 | `TimeoutTimer` (`dispose`, `cancel`, `cancelAndSet`, `setIfNotSet`; not the constructor with a runner) |
| `src/vs/base/test/common/async.test.ts` | 386–668 | the `Delayer` tests listed below |

## Files to create or change

- `src/LoomEditor.View/Features/Scheduling/IEditorScheduler.cs` — create, **public**
- `src/LoomEditor.View/Features/Scheduling/NullEditorScheduler.cs` — create (internal)
- `src/LoomEditor.View/Features/Scheduling/RunOnceScheduler.cs`, `Delayer.cs`, `TimeoutTimer.cs` — create (internal)
- `src/LoomEditor.View/Features/Scheduling/README.md` — create (Monaco sources, the cuts below)
- `src/LoomEditor.View/Features/IEditorContext.cs` — change: **public** `IEditorScheduler Scheduler { get; }`
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `Scheduler` property; the internal constructor gets a last
  parameter `IEditorScheduler? scheduler = null`; new internal constructor `EditorContext(IEditorScheduler scheduler)`
  (no-op animation frames, `TimeProvider.System`, no dispatcher) for tests
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — `./eng/Update-PublicApi.ps1 src/LoomEditor.View`
- `src/LoomEditor.WinUI/Threading/DispatcherQueueEditorScheduler.cs` — create (internal)
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: pass `new DispatcherQueueEditorScheduler(DispatcherQueue)` to the `EditorContext`
- `tests/LoomEditor.Architecture.Tests/ConventionTests.cs` — change: `FeatureFolders` skips the `Scheduling` folder
- `tests/LoomEditor.View.Tests/Features/TestHelpers/ManualEditorScheduler.cs` — create (shared helper, phase-5 README section 5)
- `tests/LoomEditor.View.Tests/Features/Scheduling/DelayerTests.cs`, `RunOnceSchedulerTests.cs`, `TimeoutTimerTests.cs`, `ManualEditorSchedulerTests.cs` — create
- `tests/LoomEditor.View.Tests/Features/EditorContextTests.cs` — change (new tests below)

## Design decisions (already made — do not change)

- **`IEditorScheduler`** (public, namespace `LoomEditor.View.Features.Scheduling`):
  `IDisposable Schedule(TimeSpan delay, Action action)`. The action runs once on the UI thread, after at least
  `delay`. Disposing the result before it runs cancels it; disposing after is a no-op. `delay < TimeSpan.Zero` throws
  `ArgumentOutOfRangeException`. XML doc with a threading note: call it on the UI thread only.
- **`IEditorContext.Scheduler`** is public (ADR 0009, decision 10 puts it on the public interface). Nothing else of this
  card is public.
- **`EditorContext()`** (public, no control) uses `NullEditorScheduler`: scheduled actions never run, like the animation
  frames of that constructor today. Say so in the XML doc of the constructor. Tests that need delayed work to run
  build the context with `new EditorContext(new ManualEditorScheduler())`.
- **`DispatcherQueueEditorScheduler(DispatcherQueue queue)`**: one `DispatcherQueueTimer` per call
  (`queue.CreateTimer()`, `Interval = delay`, `IsRepeating = false`); `Tick` stops the timer, then runs the action.
  The returned `IDisposable` stops the timer and removes the `Tick` handler.
- **`RunOnceScheduler(IEditorScheduler scheduler, Action runner, TimeSpan delay)`**: Monaco's `timeoutToken` is the
  `IDisposable` returned by `Schedule`. Members: `Delay { get; set; }`, `Schedule(TimeSpan? delay = null)`,
  `Cancel()`, `IsScheduled`, `Flush()`, `Dispose()`. After `Dispose` the runner is dropped and never runs.
- **`Delayer(IEditorScheduler scheduler, TimeSpan defaultDelay)`** without promises (`ITask<T | Promise<T>>` → `Action`):
  `Trigger(Action task, TimeSpan? delay = null)` keeps the **last** task and restarts the delay; when the delay
  elapses the last task runs once and `IsTriggered` becomes false. `Cancel()` drops the task and the timer (Monaco
  rejects the promise with `CancellationError`: here the task simply never runs). `Dispose()` = `Cancel()`. A caller
  that used `trigger(...).then(x => …)` puts the continuation inside the task. `MicrotaskDelay` is not ported.
- **`TimeoutTimer(IEditorScheduler scheduler)`**: `CancelAndSet(Action runner, TimeSpan timeout)`,
  `SetIfNotSet(Action runner, TimeSpan timeout)`, `Cancel()`, `Dispose()`. After `Dispose`, `CancelAndSet` and
  `SetIfNotSet` throw `InvalidOperationException` (Monaco: `BugIndicatingError`).
- **`ManualEditorScheduler`** (test helper, `IEditorScheduler`): a fake clock. `TimeSpan Now` (starts at 0),
  `int PendingCount`, `AdvanceBy(TimeSpan delta)` runs every pending action whose due time is ≤ `Now + delta`, in due
  time order then scheduling order, moving `Now` to each due time before running it (an action scheduled by an action
  runs in the same call if it is due), then sets `Now` to the end; `RunAll()` runs until nothing is pending. Never uses
  `DateTime`, `Task.Delay` or threads.
- **Architecture test.** `Features/Scheduling/` is infrastructure, not a feature (ADR 0009, decision 2 puts it there):
  it has no `SchedulingFeature.cs` and is not in `EditorFeatures.cs`. `ConventionTests.FeatureFolders` skips it by
  name (one `Where`, with a comment pointing to ADR 0009).

## Tests

- [ ] Port (`async.test.ts` › `Async` › `Delayer`, adapted: `Action` instead of promises, `ManualEditorScheduler`
  instead of `setTimeout`; "the promise resolves with N" becomes "after `AdvanceBy(TimeSpan.Zero)` the last task ran
  once"; "the promise is rejected" becomes "the task never ran"):
  `simple`, `simple cancel`, `cancel should cancel all calls to trigger`, `trigger, cancel, then trigger again`,
  `last task should be the one getting called`, `cancel releases the pending task`, `dispose releases the pending task`
  (the last two are the `${release} releases the pending task` loop).
  Not ported: `microtask delay simple`, `simple cancel microtask`, `cancel after the delay elapsed does not run the task`,
  `cancel and trigger after the delay elapsed settles both triggers` (microtasks), the `ThrottledDelayer` suite.
- [ ] New (`RunOnceSchedulerTests`): `Schedule_DelayElapsed_RunsRunnerOnce`, `Schedule_Twice_RestartsTheDelay`,
  `Schedule_WithDelay_UsesThatDelay`, `Cancel_RunnerDoesNotRun`, `Flush_WhenScheduled_RunsNow`,
  `Flush_WhenNotScheduled_DoesNothing`, `IsScheduled_AfterRun_IsFalse`, `Dispose_RunnerNeverRuns`
- [ ] New (`TimeoutTimerTests`): `CancelAndSet_ReplacesThePendingRunner`, `SetIfNotSet_WhenSet_KeepsTheFirstRunner`,
  `Cancel_RunnerDoesNotRun`, `CancelAndSet_AfterDispose_Throws`
- [ ] New (`ManualEditorSchedulerTests`): `AdvanceBy_RunsDueActionsInDueTimeOrder`,
  `AdvanceBy_ActionScheduledByAnAction_RunsWhenDue`, `Dispose_BeforeDue_CancelsTheAction`, `Schedule_NegativeDelay_Throws`
- [ ] New (`EditorContextTests`): `Scheduler_PublicConstructor_NeverRunsActions`,
  `Scheduler_InternalConstructor_IsTheGivenScheduler`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (`async.ts` subset → `Features/Scheduling/`); "Behavior differences": `Delayer`
      without promises, `EditorContext()` never runs scheduled actions
- [ ] Card status and `docs/tasks/README.md` updated
- [ ] No manual check: no feature schedules anything yet (the first is bracket matching, step C)

## Out of scope

`ThrottledDelayer`, `IntervalTimer`, `createCancelablePromise`, `MicrotaskDelay`. Any feature that uses the scheduler.

## Suggested commit message

`feat(view): add the editor scheduler and Monaco's RunOnceScheduler, Delayer and TimeoutTimer`
