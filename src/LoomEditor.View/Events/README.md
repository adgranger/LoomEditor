# Events

**Phase 3.** The events the cursor sends to the view (redraw, reveal a range) and to the outside world (the
selections changed, an edit was attempted on a read-only editor), with the dispatcher that collects them during an
operation and delivers them at its end.

Monaco sources: `src/vs/editor/common/viewModelEventDispatcher.ts`, `src/vs/editor/common/viewEvents.ts`.

Only what phase 3 needs is ported: two view events (`ViewCursorStateChangedEvent`, `ViewRevealRangeRequestEvent`) and
two outgoing events (`CursorStateChangedEvent`, `ReadOnlyEditAttemptEvent`). Monaco has about 20 view events and about
10 outgoing events; they are added when a feature needs them. The control keeps its own .NET events for scrolling,
configuration and line changes.
