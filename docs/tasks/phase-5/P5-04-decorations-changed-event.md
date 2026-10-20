# P5-04 — `TextModel.DecorationsChanged` and the decorations-and-editing tests

- **Status:** done
- **Depends on:** P5-03
- **Size:** S

## Goal

The model raises `DecorationsChanged` when decorations are added, changed or removed and when an edit moves them,
once per operation (deferred like the content events, and after them). The 59 `Decorations and editing` tests check
how every stickiness reacts to inserts, deletes and replaces.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/model/textModel.ts` unless said otherwise.

| Lines | What to port |
|---|---|
| 2588–2698 | `DidChangeDecorationsEmitter`: `beginDeferredEmit`, `endDeferredEmit`, `checkAffectedAndFire`, `fire`, `tryFire`, `doFire` — **not** `recordLineAffectedByInjectedText`, `…LineHeightChange`, `…FontChange`, `handleBeforeFire` |
| 225–226 | `_onDidChangeDecorations`, `onDidChangeDecorations` |
| 1283, 1291 | `pushEOL`: begin / end around the edit |
| 1342, 1347 | `pushEditOperations`: begin / end (outside the content deferral, so the decorations event comes after the content event) |
| 1467, 1479 | `_applyUndoRedoEdits`: begin / end |
| 1492, 1499 | `applyEdits`: begin / end |
| 1530 | `_doApplyEdits`: `fire()` once per content change |
| 1673, 1676 | `changeDecorations`: begin / end |
| 1733, 1736 | `deltaDecorations`: begin / end |
| 1920, 1945–1946, 1985, 2021, 2059, 2076 | the `checkAffectedAndFire` / begin / end calls of `_changeDecorationImpl`, `_changeDecorationOptionsImpl`, `_deltaDecorationsImpl` (honoring `suppressEvents`) |
| `src/vs/editor/common/textModelEvents.ts` 129–134 | `IModelDecorationsChangedEvent` |
| `src/vs/editor/test/common/model/modelDecorations.test.ts` 230–295, 445–1137 | the tests listed below |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModel.DecorationsEmitter.cs` — create (partial: the emitter state and methods; Monaco's
  class becomes fields of the model, like `TextModel.DeferredEmit.cs` did for the content emitter)
- `src/LoomEditor.Core/Model/Events/ModelDecorationsChangedEventArgs.cs` — create
- `src/LoomEditor.Core/Model/TextModel.Decorations.cs` — change: the emitter calls listed above
- `src/LoomEditor.Core/Model/TextModel.Edits.cs` — change: begin / end in `DoApplyEdits`, `Fire()` per change
- `src/LoomEditor.Core/Model/TextModel.Undo.cs` — change: begin / end in `PushEditOperations` (internal overload),
  `ApplyUndoRedoEdits`, `PushEol`
- `src/LoomEditor.Core/Model/TextModel.Edits.cs` — change: `internal void ApplyEdits(IReadOnlyList<IdentifiedEditOperation> edits)`
  (Monaco's `applyEdits` with `forceMoveMarkers`; the tests need it)
- `tests/LoomEditor.Core.Tests/Model/ModelDecorationsTests.cs` — change (5 event tests)
- `tests/LoomEditor.Core.Tests/Model/DecorationsAndEditingTests.cs` — create

## Design decisions (already made — do not change)

- `internal event EventHandler<ModelDecorationsChangedEventArgs>? DecorationsChanged` on `TextModel`.
  `ModelDecorationsChangedEventArgs` (internal sealed class, `EventArgs`) has Monaco's four flags `AffectsMinimap`,
  `AffectsOverviewRuler`, `AffectsGlyphMargin`, `AffectsLineNumber`; `checkAffectedAndFire` only sets
  `AffectsGlyphMargin` (from `GlyphMarginClassName`) because the options behind the other three are not ported, `fire()`
  sets the three Monaco sets.
- Private methods `BeginDecorationsDeferredEmit()`, `EndDecorationsDeferredEmit()`, `CheckAffectedAndFireDecorations(ModelDecorationOptions)`,
  `FireDecorationsChanged()`. They are separate from the content emitter of `TextModel.DeferredEmit.cs` (two emitters,
  like Monaco).
- **Order.** Every public edit path wraps the decorations deferral **outside** the content deferral, in Monaco's order:
  begin decorations, begin content, …, end content, end decorations. `DoApplyEdits` begins / ends the decorations
  deferral itself so that the public `ApplyEdits` overloads and `ApplyEditsForUndo` are covered; the nesting counter
  makes the outer calls win.
- `SetValue` and `SetEol` (alone) do not raise `DecorationsChanged`, like Monaco (the view handles the flush).
- The event is raised synchronously at the end of the outermost deferral; a handler that changes decorations is fine
  (the counter is back to 0).

## Tests

Port (`modelDecorations.test.ts`):

- [x] `Editor Model - Model Decorations` (5): `decorations emit event on add`, `decorations emit event on change`,
  `decorations emit event on remove`, `decorations emit event when inserting one line text before it`,
  `decorations do not emit event on no-op deltaDecorations`
- [x] `Decorations and editing` › `insert` › `collapsed dec`: `before`, `equal`, `after`; › `non-collapsed dec`: `before`,
  `start`, `inside`, `end`, `after`
- [x] `Decorations and editing` › `delete` › `collapsed dec`: `edit.end < range.start`, `edit.end <= range.start`,
  `edit.start < range.start && edit.end > range.end`, `edit.start >= range.end`, `edit.start > range.end`;
  › `non-collapsed dec`: `edit.end < range.start`, `edit.end <= range.start`, `edit.start < range.start && edit.end < range.end`,
  `edit.start < range.start && edit.end == range.end`, `edit.start < range.start && edit.end > range.end`,
  `edit.start == range.start && edit.end < range.end`, `edit.start == range.start && edit.end == range.end`,
  `edit.start == range.start && edit.end > range.end`, `edit.start > range.start && edit.start < range.end && edit.end < range.end`,
  `edit.start > range.start && edit.start < range.end && edit.end == range.end`,
  `edit.start > range.start && edit.start < range.end && edit.end > range.end`, `edit.start == range.end`, `edit.start > range.end`
- [x] `Decorations and editing` › `replace short`: the same 5 `collapsed dec` and 12 `non-collapsed dec` names as `delete`
- [x] `Decorations and editing` › `replace long`: the same 5 + 12 names
  (59 tests in this suite. Use one `[Fact]` per Monaco test; the C# method name is the suite path plus a short name,
  e.g. `Delete_NonCollapsed_EditStartEqualsRangeEnd`, with the Monaco name in the `// Monaco:` comment. `_runTest` and
  `runTest` (lines 449–478) are private helpers; the edit goes through the new internal `ApplyEdits` with
  `ForceMoveMarkers`.)
- [x] New: `DecorationsChanged_PushEditOperations_RaisedOnceAfterContentChanged`
- [x] New: `DecorationsChanged_UndoAndRedo_RaisedOncePerStep`
- [x] New: `DecorationsChanged_SetTrackedRange_NotRaised` (`suppressEvents`)
- [x] New: `DecorationsChanged_GlyphMarginClassName_SetsAffectsGlyphMargin`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `modelDecorations.test.ts` row in the tests table (ported / total, and why 3 are not)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The view model's reaction to the event (P5-07).

## Suggested commit message

`feat(core): raise DecorationsChanged from the text model`
