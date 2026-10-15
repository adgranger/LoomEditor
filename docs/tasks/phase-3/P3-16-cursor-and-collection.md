# P3-16 — `Cursor`, `CursorContext` and `CursorCollection`

- **Status:** todo
- **Depends on:** P3-15
- **Size:** M

## Goal

One `Cursor` holds a model state and a view state and keeps its selection in a tracked range of the model, so it can
recover its place after an edit. `CursorCollection` holds the primary cursor and the secondary cursors, adds and
removes them, and merges cursors whose selections overlap (`normalize`). `CursorContext` bundles what they read: the
model, the "simple model" of the view, the coordinates converter and the configuration.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorContext.ts` | 10–24 | `class CursorContext` |
| `src/vs/editor/common/cursor/oneCursor.ts` | 16–165 | `class Cursor` (whole file): tracked range, `readSelectionFromMarkers`, `ensureValidState`, `setState`, `_validatePositionWithCache`, `_validateViewState`, `_setState` |
| `src/vs/editor/common/cursor/cursorCollection.ts` | 15–252 | `class CursorCollection` (whole file), including `normalize` (161–251) |

## Files to create or change

- `src/LoomEditor.View/Cursors/CursorContext.cs`, `Cursor.cs`, `CursorCollection.cs` — create
  (`CursorCollection.Normalize.cs` partial if the file passes ~400 lines)
- `tests/LoomEditor.View.Tests/Cursors/CursorTests.cs`, `CursorCollectionTests.cs` — create
- `tests/LoomEditor.View.Tests/Cursors/TestHelpers/CursorTestContext.cs` — create: builds a `CursorContext` for a text
  (`TextModel` + `TextModelCursorSimpleModel` + `IdentityCoordinatesConverter` + `CursorConfiguration`). The
  `FontInfo` is built with `FontMeasurements.Read` and a fixed-width `ICharacterWidthReader`, as in
  `tests/LoomEditor.View.Tests/Layout/FontMeasurementsTests.cs`; the `EditorLayoutInfo` with `EditorLayoutInfoComputer`
  for an 800 × 600 editor

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `CursorContext` is an `internal sealed class` with `TextModel Model`, `ICursorSimpleModel ViewModel`,
  `ICoordinatesConverter CoordinatesConverter`, `CursorConfiguration CursorConfig` (Monaco's names).
- `Cursor`:
  - public mutable properties `SingleCursorState ModelState` and `ViewState` (Monaco assigns them in `_setState`);
  - the tracked range uses `TextModel.SetTrackedRange(id, selection.Range, TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges)`
    (P3-08) — Monaco passes the `Selection` as a range: use `selection.Range`;
  - `Dispose(CursorContext)`, `StartTrackingSelection`, `StopTrackingSelection`, `AsCursorState()`,
    `ReadSelectionFromMarkers(CursorContext)`, `EnsureValidState(CursorContext)`, `SetState(CursorContext, SingleCursorState? modelState, SingleCursorState? viewState)`.
  - `_validateViewState` and `_setState`: port line by line, including the position cache of `_validatePositionWithCache`
    and the adjustment of the leftover visible columns by the column delta (lines ~108–110).
- `CursorCollection`:
  - `internal sealed class CursorCollection : IDisposable`-like, but Monaco's `dispose()` takes no context: keep
    `void Dispose()` and implement `IDisposable`.
  - `List<Cursor> _cursors` (index 0 = primary), `int _lastAddedCursorIndex`.
  - `lastAddedCursorIndex`: the Monaco comment says it indexes secondary cursors, but `_addSecondaryCursor` stores
    `cursors.length - 1` (a full-array index). **Port the code, not the comment.**
  - `Normalize()` (lines 161–251) uses `findFirstMin` / `findLastMax` and a sort: use a **stable** sort
    (conventions §4.3). It merges overlapping selections when `CursorConfig.MultiCursorMergeOverlapping` is true;
    port the "touching" rule exactly.
  - `GetAll()`, `GetViewPositions()`, `GetTopMostViewPosition()`, `GetBottomMostViewPosition()`, `GetSelections()`,
    `GetViewSelections()`, `SetSelections(IReadOnlyList<Selection>)`, `GetPrimaryCursor()`, `SetStates(IReadOnlyList<PartialCursorState>?)`,
    `KillSecondaryCursors()`, `GetLastAddedCursorIndex()`, `UpdateContext(CursorContext)`, `EnsureValidState()`,
    `ReadSelectionFromMarkers()`, `StartTrackingSelections()`, `StopTrackingSelections()`.

## Tests

No Monaco test targets these classes directly (they are covered by `cursor.test.ts` from P3-27 on). New tests:

- [ ] New: a new `Cursor` is at `(1,1)` in both states
- [ ] New: set a selection `(1,2) → (1,4)` on `"abcdef"`, insert `"xx"` at `(1,1)` through `TextModel.PushEditOperations`,
      `ReadSelectionFromMarkers` gives `(1,4) → (1,6)` with the same direction
- [ ] New: an empty cursor at `(1,3)`, someone inserts `"zz"` at `(1,2)-(1,4)` replacing `"bc"`: `ReadSelectionFromMarkers`
      returns an **empty** selection (collapsed to the end, lines 68–71)
- [ ] New: `SetState` with a model state outside the model clamps it to a valid position
- [ ] New: `CursorCollection.SetSelections` with 3 selections → 3 cursors, primary first; `KillSecondaryCursors` leaves 1
- [ ] New: `Normalize` merges `(1,1)→(1,5)` and `(1,3)→(1,8)` into one cursor `(1,1)→(1,8)`; it does not merge them when
      `MultiCursorMergeOverlapping` is false; two empty cursors at the same position become one
- [ ] New: `GetLastAddedCursorIndex` after adding two secondary cursors is 2

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `oneCursor.ts`, `cursorCollection.ts`, `cursorContext.ts` (✅, P3-16)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`CursorsController` (P3-18, P3-19), events (P3-17).

## Suggested commit message

`feat(view): port Cursor and CursorCollection`
