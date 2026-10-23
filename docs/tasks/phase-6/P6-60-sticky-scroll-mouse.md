# P6-60 — Sticky scroll pointer input: reveal, shift-click, folding chevron, hover

- **Status:** done
- **Depends on:** P6-59
- **Size:** S

## Goal

Clicking a sticky line reveals and selects it (at the clicked column), shift-click reveals the end of its scope, clicking the
chevron folds / unfolds the scope and keeps the view stable, and hovering highlights the line; clicks on the sticky widget
never reach the text below it.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/contrib/stickyScroll/browser/stickyScrollController.ts`.

| Lines | What to port |
|---|---|
| 228–244 | `_revealPosition`, `_revealLineInCenterIfOutsideViewport`, `_reveaInEditor` (reveal, set the selection, focus) |
| 280–322 | the click listener (modifiers ignored, left button only, shift-click → end line, folding icon → toggle, sticky line → reveal the position under the pointer or column 1) |
| 436–453 | `_toggleFoldingRegionForLine` (`toggleCollapseState`, the scroll-top correction, re-render from that line) |

`src/vs/editor/contrib/stickyScroll/browser/stickyScrollWidget.ts` 363–417 (`getEditorPositionFromNode`, `getLineNumberFromChildDomNode`,
`isInStickyLine`, `isInFoldingIconDomNode`) — informative: LoomEditor hit-tests geometry instead of DOM nodes.

## Files to create or change

- `src/LoomEditor.View/Features/StickyScroll/StickyScrollFeature.Mouse.cs` — create (partial): `HandleClick(int lineIndex, StickyScrollHitPart part, int? column, bool shift)`
  with `enum StickyScrollHitPart { LineNumberGutter, FoldingIcon, Content }`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: before the text mouse handling, if the pointer is in the sticky
  widget (P6-59 helpers), route the press to the feature and mark the event handled; pointer moves update the hovered index
- `tests/LoomEditor.View.Tests/Features/StickyScroll/StickyScrollMouseTests.cs` — create

## Design decisions (already made — do not change)

- `ClickLinkGesture` (ctrl-click "go to definition", hover underline) is phase 8: not ported; modifier clicks do nothing (Monaco returns).
- The column of a click on the content comes from the line layout hit test of the drawn sticky line (P6-59 provides it); Monaco uses the
  character mapping of the rendered span.
- `toggleCollapseState` exists in `FoldingModelFunctions` (P5-86); the folding model comes from `GetFoldingFeature()?.FoldingModel`.
- `revealLineInCenterIfOutsideViewport(…, Smooth)` and `revealPosition` → the view model's reveal API used by the find feature.

## Tests

- [x] New: `Click_OnContent_RevealsAndSelectsThePosition`
- [x] New: `ShiftClick_RevealsTheEndOfTheScope`
- [x] New: `CtrlClick_DoesNothing`
- [x] New: `Click_OnChevron_TogglesTheFoldAndAdjustsScrollTop`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `stickyScrollController.ts` row (mouse; no click-link gesture)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Keyboard focus navigation in the sticky widget (`focus`, `focusNext`, `goToFocused`): not ported in this phase.

## Suggested commit message

`feat(sticky-scroll): click sticky lines to reveal and fold`
