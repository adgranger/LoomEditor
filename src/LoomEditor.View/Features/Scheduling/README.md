# Scheduling

**Role:** infrastructure, not a feature (ADR 0009, decision 2): it has no `SchedulingFeature.cs` and is not in
`EditorFeatures.cs`. Features delay work with `IEditorContext.Scheduler` (`IEditorScheduler`), the control backs it with
a `DispatcherQueueTimer`, tests with `ManualEditorScheduler`.

**Monaco source:** `src/vs/base/common/async.ts` — `RunOnceScheduler`, `Delayer`, `TimeoutTimer`.

**Cuts:** `Delayer` has no promises (a cancelled task never runs); `MicrotaskDelay`, `ThrottledDelayer`,
`IntervalTimer` and `createCancelablePromise` are not ported. `TimeoutTimer` has no runner constructor.
