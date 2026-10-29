# P10-27 — `MarkerList`: the sorted problems of a model and the "next one" arithmetic

- **Status:** done
- **Depends on:** P9-33
- **Size:** S

## Goal

A `MarkerList` over the editor's `MarkerService` holds the errors, warnings and infos sorted like Monaco's, says which one is "next" / "previous" from a position (wrapping around), and tells when the set of markers changes. No UI and no command yet (P10-28).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/gotoError/browser/markerNavigationService.ts` | 21–28 | `MarkerCoordinate` |
| `src/vs/editor/contrib/gotoError/browser/markerNavigationService.ts` | 29–212 | `MarkerList` (the two sort orders, `updateMarker` with the "unchanged" comparison, `onDidChange`, `selected`, `_initIdx`, `resetIndex`, `move`, `find`). **Single resource**: every `resource` / `uri` / `matches` / `resourceFilter` part is dropped (a LoomEditor `MarkerService` belongs to one model) |
| `src/vs/editor/contrib/gotoError/browser/markerNavigationService.ts` | 214–254 | `IMarkerNavigationService`: **not ported** (no provider registry; the feature builds the list itself) |

## Files to create or change

- `src/LoomEditor.View/Features/GotoError/MarkerList.cs`, `MarkerCoordinate.cs`, `MarkerSortOrder.cs` — create (headers: ported from `markerNavigationService.ts`)
- `tests/LoomEditor.View.Tests/Features/GotoError/MarkerListTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class MarkerList : IDisposable` with the constructor `(MarkerService markers, MarkerSortOrder order = MarkerSortOrder.Severity)`; it subscribes to `MarkerService.MarkersChanged` and re-reads with `markers.Read(severities: Error | Warning | Info)` (use the severity flags type of `MarkerService.Read`; `Hint` is excluded, as in Monaco). `enum MarkerSortOrder { Severity, Position }`: Monaco's `problems.sortOrder` setting, default `severity` (so errors come first); the C# comparison is `MarkerSeverities.Compare` (more severe first: it is `(int)b - (int)a`) and `Range.CompareRangesUsingStarts` (find the existing method or write it: start line, start column, end line, end column).
- `Changed` is an `EventHandler`; it fires only when the **new sorted list differs** from the old one by (range, severity, message) — the comparison of lines 63–72 — and then `_nextIdx` is reset to `-1`.
- `Selected` (`MarkerCoordinate?`: marker, 1-based index, total), `ResetIndex()`, `bool Move(bool forward, TextModel model, Position position)`, `MarkerCoordinate? Find(Position position)` as Monaco, with the single resource simplification: `_initIdx` without the "different file" branch: look for the first marker (in list order) whose range (an empty range is widened to the word at its start, `model.GetWordAtPosition`) contains the position or starts at / after it; `found = false` → first (forward) or last (backward) marker; `wentPast && !forward` → one back; a negative index wraps to the last.
- `Dispose` unsubscribes.
- `<remarks>Threading: UI thread only.</remarks>`.

## Tests

- [x] Port: none (no upstream test of `MarkerList` exists)
- [x] New: `Constructor_SortsBySeverityThenPosition`, `Constructor_PositionOrder_SortsByPositionThenSeverity`, `Constructor_ExcludesHints`, `Move_Forward_FromPositionBeforeFirst_SelectsFirst`, `Move_Forward_Repeatedly_WrapsAround`, `Move_Backward_FromPositionAfterAll_SelectsLast`, `Move_Backward_Wraps`, `Move_PositionInsideMarker_ForwardSelectsThatMarker`, `Move_Backward_WentPast_GoesOneBack`, `Move_EmptyMarkerRange_UsesWordAtPosition`, `Move_NoMarkers_ReturnsFalse`, `Selected_HasIndexAndTotal`, `Find_ReturnsMarkerContainingPosition`, `Changed_FiresWhenMarkersChange_AndResetsIndex`, `Changed_DoesNotFireWhenSameMarkersReSet`, `Dispose_StopsListening`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `markerNavigationService.ts` 🔶 (single resource, no service)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Several resources, `IMarkerNavigationService`, the widget, commands (P10-28).

## Suggested commit message

`feat(view): add MarkerList (sorted problems and next / previous arithmetic)`
