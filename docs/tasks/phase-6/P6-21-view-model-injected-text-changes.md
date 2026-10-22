# P6-21 — The view model and the cursors follow injected text changes

- **Status:** done
- **Depends on:** P6-20, P6-06
- **Size:** S

## Goal

When injected text is added, moved or removed, the view model re-projects the affected lines (their line breaks change)
and the cursors recompute their view positions, like Monaco's `ModelInjectedTextChangedEvent` path.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 322–333 | `onDidChangeContentOrInjectedText`: an injected-text event has `changes` and **no** version id (`versionId = null`) |
| same | 437–460 | the end of the method (`acceptVersionId` only when `versionId !== null`; line mapping changed → cursor + decorations) |
| same | 465–472 | `emitContentChangeEvent` (the cursor gets the injected-text event too) |
| same | 855–857 | `getInjectedTextAt` |
| `src/vs/editor/common/cursor/cursor.ts` | 230–250 | `CursorsController.onModelContentChanged`: the `ModelInjectedTextChangedEvent` branch with `_isHandling` |

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModel.cs` — change: subscribe to `TextModel.InjectedTextChanged`; factor the body of
  `OnModelRawContentChanged` so both events share it (`OnDidChangeContentOrInjectedText(IReadOnlyList<ModelRawChange> changes, int? versionId)`)
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: add `GetInjectedTextAt(Position viewPosition)` and `ModifyPosition`
  if missing (Monaco 983–987, uses `TextModel.ModifyPosition`, which exists)
- `src/LoomEditor.View/Cursors/CursorsController.cs` — change: `OnModelInjectedTextChanged(ViewModelEventsCollector)` with the
  `_isHandling` guard; update the remark that says the branch is not ported
- `tests/LoomEditor.View.Tests/Layout/ViewModelInjectedTextTests.cs` — create

## Design decisions (already made — do not change)

- The existing `OnModelLineChanged(int? versionId, …)` already treats `null` as "not stale" (P5-79): pass `null` for injected
  text changes. `AcceptVersionId` is called only for content changes (Monaco: `if (versionId !== null)`).
- The injected-text handler filters nothing by owner: Monaco reprojects every reported line; the line-breaks computer reads
  only this editor's injected text (P6-17).
- Cursor: Monaco's `onModelContentChanged(eventsCollector, e)` takes a union; LoomEditor adds a separate method
  `OnModelInjectedTextChanged(ViewModelEventsCollector)` (C# has no union): it runs `SetStates(…, "modelChange", CursorChangeReason.NotSet, GetCursorStates())`
  inside `_isHandling = true … finally false`, and returns early if `_isHandling` is already set.
- The view model calls it from a second `EmitViewEvent` after the line update (Monaco's `emitContentChangeEvent` order).

## Tests

- [x] New: `AddingInjectedText_ThatWraps_AddsViewLines` (word wrap on, an `After` long enough to wrap the line)
- [x] New: `RemovingInjectedText_RemovesTheViewLines`
- [x] New: `InjectedTextBeforeTheCursor_MovesItsViewColumn` (cursor at model (1, 5); `Before` of 3 chars at column 3 → view column 8)
- [x] New: `InjectedTextOfAnotherEditor_DoesNotChangeTheViewLines`
- [x] New: `GetInjectedTextAt_ReturnsTheOptions`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `cursor.ts` row (the injected-text branch is ported); `viewModelImpl.ts` row
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing injected text and hit testing over it (P6-25).

## Suggested commit message

`feat(view): follow injected text changes in the view model and cursors`
