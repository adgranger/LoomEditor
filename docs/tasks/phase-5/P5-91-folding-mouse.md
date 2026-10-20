# P5-91 — Clicking the folding controls and the `⋯`

- **Status:** todo
- **Depends on:** P5-90
- **Size:** M

## Goal

A click on a chevron folds or unfolds its region (Shift or middle button: recursively; Alt: the surrounding regions),
and a click on the `⋯` of a folded line unfolds it, as in VS Code. Features receive the editor's mouse-down and
mouse-up events in **model** coordinates (Monaco's `ViewUserInputEvents`).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `browser/view/viewUserInputEvents.ts` | 55–62, 80–103 | `emitMouseDown`, `emitMouseUp`, `_convertViewToModelMouseEvent`, `convertViewToModelMouseTarget` (no view zones) |
| `browser/controller/mouseHandler.ts` | 303–308, 310–354 | where the events are emitted: mouse-up with a fresh target; mouse-down **after** the mouse-down operation started |
| `contrib/folding/browser/folding.ts` | 396–510 | `onEditorMouseDown`, `onEditorMouseUp` (`iconClicked`, `gutterOffsetX < 4`, `unfoldOnClickAfterEndOfLine`, Alt / Shift / middle button) |

## Files to create or change

- `src/LoomEditor.View/Controller/ViewUserInputEvents.cs` — create: `MouseDown`, `MouseUp` events of
  `EditorMouseEventArgs(EditorMouseEvent Event, MouseTarget Target)`; `EmitMouseDown/Up` convert the target's
  `Position` and `Range` with the view model's `CoordinatesConverter`
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `internal ViewUserInputEvents UserInputEvents { get; }`
- `src/LoomEditor.View/Features/EditorContextExtensions.cs` — change: `GetUserInputEvents()`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: emit mouse-down at the end of `OnPointerPressed`
  (also for gutter targets) and mouse-up in `OnPointerReleased` (new hit test), even when no mouse-down operation runs
- `src/LoomEditor.Rendering/TextViewRenderer.HitTest.cs` — change: `GetLineWidth` includes the after-content text of the
  line (the `⋯`), and a point inside it returns the line's max column (see decisions)
- `src/LoomEditor.View/Features/Folding/FoldingFeature.Mouse.cs` — create (partial): `OnEditorMouseDown`, `OnEditorMouseUp`
- `tests/LoomEditor.View.Tests/Controller/ViewUserInputEventsTests.cs`, `tests/LoomEditor.View.Tests/Features/Folding/FoldingMouseTests.cs` — create
- `tests/LoomEditor.Rendering.Tests/TextViewRendererHitTestTests.cs` — change: after-content case

## Design decisions (already made — do not change)

- Monaco's DOM puts the `::after` content inside the line, so a click on `⋯` is `CONTENT_TEXT` at the line's max column.
  LoomEditor matches this geometrically: the hit tester's line width is `text width + after-content width` (the width
  P5-08 draws), and an offset in the after-content part returns `Position(line, maxColumn)`. Beyond it, the target is
  `ContentEmpty` as before.
- `gutterOffsetX` = `event.X - layoutInfo.DecorationsLeft` (Monaco: `offsetX - element.offsetLeft` of the `.cldr`
  element); `< 4` → not an icon click.
- Only left and middle buttons (Monaco: `leftButton || middleButton`). The feature handles the events only when a
  hidden range model exists (folding enabled).
- The fold-on-click goes through `GetFoldingModel()` (P5-88), toggles, then `reveal({ lineNumber, column: 1 })`.
- The control still does not move focus or the cursor for a gutter click (P3-46 behavior); the folding handler runs
  on the mouse-**up** as Monaco.

## Tests

No Monaco test covers the mouse handlers. New (the events are raised with hand-made `EditorMouseEvent`s and targets from
`MouseTargetFactory` on a `TestCodeEditor`):

- [ ] `EmitMouseDown_ConvertsTheTargetToModelCoordinates` (a click below a hidden area reports the model line)
- [ ] `ClickOnExpandedChevron_FoldsTheRegion`; `ClickOnCollapsedChevron_UnfoldsIt`
- [ ] `ClickInTheFirstFourDipsOfTheDecorationsArea_DoesNothing`
- [ ] `ShiftClickOnChevron_FoldsChildrenFirst_ThenTheRegion` (Monaco's "first only collapse all children")
- [ ] `AltClickOnChevron_TogglesTheSurroundingRegions`
- [ ] `ClickOnEllipsis_UnfoldsTheRegion`; `ClickAfterTheEllipsis_DoesNothing` (`unfoldOnClickAfterEndOfLine` false)
- [ ] `MouseUpOnAnotherLine_DoesNothing`
- [ ] Rendering: `HitTest_InsideAfterContent_ReturnsMaxColumn`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: rows `browser/view/viewUserInputEvents.ts` (🔶 mouse down / up, P5-91), `folding.ts`
      (mouse, P5-91); behavior difference "after-content is hit-tested as part of the line"
- [ ] Card status and `docs/tasks/README.md` updated
- [ ] Manual check (maintainer, Gallery): click a chevron → the region folds; click the `⋯` → it unfolds;
      Shift+click folds the children first; the caret does not move on a chevron click

## Out of scope

Hover tooltips (`linesDecorationsTooltip`), the context menu, mouse move events for features.

## Suggested commit message

`feat(folding): fold and unfold with the mouse`
