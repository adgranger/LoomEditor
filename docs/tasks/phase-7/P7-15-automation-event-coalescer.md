# P7-15 — `AutomationEventCoalescer`: selection and text change events

- **Status:** done
- **Depends on:** P7-14
- **Size:** S

## Goal

Caret / selection moves and text changes become **at most one** automation event of each kind per dispatcher turn, delivered to a sink, so that Narrator and NVDA are not flooded
(typing one character changes the text and the caret; a `Replace all` makes thousands of edits).

## Sources

None (specified in ADR 0010 decision 6). The editor events it listens to: `IEditorContext.SelectionChanged` (raised for any cursor change, `EditorContext.OnViewModelEvent`) and `TextModel.ContentChanged`.

## Files to create or change

- `src/LoomEditor.View/Accessibility/IAutomationEventSink.cs` — create
- `src/LoomEditor.View/Accessibility/AutomationEventCoalescer.cs` — create
- `tests/LoomEditor.View.Tests/Accessibility/TestHelpers/RecordingAutomationEventSink.cs` — create
- `tests/LoomEditor.View.Tests/Accessibility/AutomationEventCoalescerTests.cs` — create

## Design decisions (already made — do not change)

- ```csharp
  internal interface IAutomationEventSink
  {
      void RaiseTextChanged();        // UIA TextPatternOnTextChanged
      void RaiseSelectionChanged();   // UIA TextPatternOnTextSelectionChanged
  }
  ```
- `AutomationEventCoalescer(IEditorContext context, IAutomationEventSink sink)` : `IDisposable`. It subscribes to `context.SelectionChanged` and `context.Model.ContentChanged` in the constructor and unsubscribes in `Dispose`.
  Either event sets a flag (`_textChanged`, `_selectionChanged`); if no flush is scheduled it schedules one with `context.Scheduler.Schedule(TimeSpan.Zero, Flush)` (the handle is kept so that `Dispose` cancels it).
  `Flush` clears the pending handle, reads and clears both flags, then calls `sink.RaiseTextChanged()` first (if the text flag was set) and `sink.RaiseSelectionChanged()` second (if the selection flag was set): UIA clients expect the text change before the caret move.
- A sink that throws must not break the editor: wrap each of the two sink calls separately in `try { … } catch (Exception) { }` with the comment `// A faulty automation client must not break typing.` If the build then reports CA1031, add a local
  `#pragma warning disable CA1031` / `restore` around those two statements (the only `#pragma` this card may add; `CLAUDE.md` forbids others).
- Disposing twice is safe. After `Dispose` no sink call is made, even for an already scheduled flush.
- A `Model` replaced on the context is not handled: `EditorContext.Model` is fixed for its life.
- `internal sealed`, XML doc + `Threading: UI thread only` remark. The class is created by the control (P7-19); no feature is involved.
- `RecordingAutomationEventSink`: `List<string> Events` with `"text"` / `"selection"` appended by the two methods.

## Tests

(use `new EditorContext(manualScheduler)` with `ManualEditorScheduler` from `tests/LoomEditor.View.Tests/Features/TestHelpers/`; `scheduler.RunAll()`-style member: read the helper for its exact API)

- [x] New: `NoEvents_WhenNothingChanges`
- [x] New: `SelectionChange_RaisesOneSelectionEventOnFlush` (nothing is raised before the scheduler runs)
- [x] New: `ManySelectionChanges_RaiseOneEvent`
- [x] New: `TextChange_RaisesTextThenSelection` (typing a character through `context.ViewModel.Type("a")`: events `["text", "selection"]` in that order)
- [x] New: `TextChangeOnly_RaisesOnlyText` (an edit through `Model.PushEditOperations` that leaves the cursor untouched — if the cursor always reports a change after an edit, assert `["text", "selection"]` and rename the test `EditAlsoMovesTheCaret`)
- [x] New: `EventsAfterAFlush_ScheduleANewFlush`
- [x] New: `Dispose_CancelsAPendingFlush`, `Dispose_Twice_IsSafe`, `Dispose_UnsubscribesFromTheContext`
- [x] New: `SinkThrowing_DoesNotBreakTheEditorOrTheOtherEvent` (a sink whose `RaiseTextChanged` throws: `RaiseSelectionChanged` is still called, and `context` keeps working)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Raising the real UIA events (P7-19), focus events (WinUI raises them), `LiveRegionChanged`.

## Suggested commit message

`feat(view): coalesce the automation text and selection events`
