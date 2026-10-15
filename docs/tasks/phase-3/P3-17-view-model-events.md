# P3-17 — View model events (dispatcher subset)

- **Status:** todo
- **Depends on:** P3-16
- **Size:** S

## Goal

The cursor reports what happened through two kinds of events, collected during an operation and delivered at its
end: **view events** for the view (the cursor moved: redraw; please reveal this range: scroll) and **outgoing events**
for the outside world (the selections changed; someone tried to edit a read-only editor). Port Monaco's dispatcher and
the four events phase 3 needs.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModelEventDispatcher.ts` | 15–155 | `class ViewModelEventDispatcher` (nested begin/end, outgoing event merge and postponing, view event queue) |
| same | 157–174 | `class ViewModelEventsCollector` |
| same | 387–444 | `class CursorStateChangedEvent` (`isNoOp`, `attemptToMerge`) |
| same | 446–463 | `class ReadOnlyEditAttemptEvent` |
| `src/vs/editor/common/viewEvents.ts` | 61–70 | `class ViewCursorStateChangedEvent` |
| same | 184–230 | `enum VerticalRevealType`, `class ViewRevealRangeRequestEvent` |

## Files to create or change

- `src/LoomEditor.View/Events/ViewModelEventDispatcher.cs`, `ViewModelEventsCollector.cs` — create
- `src/LoomEditor.View/Events/ViewEvent.cs` (abstract base), `ViewCursorStateChangedEvent.cs`,
  `ViewRevealRangeRequestEvent.cs`, `VerticalRevealType.cs` — create
- `src/LoomEditor.View/Events/OutgoingViewModelEvent.cs` (abstract base with `Kind`, `IsNoOp()`, `AttemptToMerge`),
  `CursorStateChangedEvent.cs`, `ReadOnlyEditAttemptEvent.cs` — create
- `src/LoomEditor.View/Events/README.md` — create (what the folder is, Monaco source)
- `tests/LoomEditor.View.Tests/Events/ViewModelEventDispatcherTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first. Namespace `LoomEditor.View.Events`, everything `internal`.
- Only the event types listed above exist in phase 3. Write in the README that Monaco has ~20 view events and ~10
  outgoing events, added when a feature needs them.
- View event handlers: Monaco's `ViewEventHandler` objects become an interface
  `internal interface IViewEventHandler { void HandleEvents(IReadOnlyList<ViewEvent> events); }` with
  `AddViewEventHandler` / `RemoveViewEventHandler`. Outgoing events are a .NET event
  `event EventHandler<OutgoingViewModelEvent>? OnEvent` (Monaco: `onEvent`).
- Port the postponing rules exactly: outgoing events are not delivered while a collector is open **or** while view
  events are being consumed; they are merged by kind (`AttemptToMerge`) and dropped when `IsNoOp()`. Nested
  `BeginEmitViewEvents` / `EndEmitViewEvents` share one collector.
- `CursorStateChangedEvent`: `IReadOnlyList<Selection>? OldSelections`, `IReadOnlyList<Selection> Selections`,
  `int OldModelVersionId`, `int ModelVersionId`, `string Source`, `CursorChangeReason Reason`, `bool ReachedMaxCursorCount`.
  Selections are compared with `==` (Monaco `equalsSelection`).
- `ViewRevealRangeRequestEvent`: `string? Source`, `bool MinimalReveal`, `Range? Range`, `IReadOnlyList<Selection>? Selections`,
  `VerticalRevealType VerticalType`, `bool RevealHorizontal`, `ScrollType ScrollType` (the existing
  `LoomEditor.View.Layout.ScrollType`). `VerticalRevealType { Simple = 0, Center = 1, CenterIfOutsideViewport = 2, Top = 3, Bottom = 4, NearTop = 5, NearTopIfOutsideViewport = 6 }`.
- Add `ViewModelEventDispatcher.EmitSingleViewEvent(ViewEvent)` and `EmitOutgoingEvent(OutgoingViewModelEvent)` as in Monaco.

## Tests

No Monaco test file. New tests:

- [ ] New: inside `BeginEmitViewEvents`/`EndEmitViewEvents`, two `CursorStateChangedEvent`s are merged into one
      (old selections of the first, new selections of the second) and delivered once, at the end
- [ ] New: a `CursorStateChangedEvent` whose old and new selections and versions are equal is not delivered
- [ ] New: nested begin/end deliver only when the outermost `End` is called
- [ ] New: a view event handler that emits a new view event while handling: the new event is delivered after the
      current batch, to every handler (queue), and outgoing events wait until the queue is empty
- [ ] New: two `ReadOnlyEditAttemptEvent`s in one collector are delivered once

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `viewModelEventDispatcher.ts` and `viewEvents.ts` (🔶 subset, P3-17)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Other view events (scroll, configuration, lines changed…): the control keeps its current .NET events for those.

## Suggested commit message

`feat(view): add the view model event dispatcher and cursor events`
