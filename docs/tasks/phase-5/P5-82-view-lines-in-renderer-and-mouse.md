# P5-82 — The renderer, the hit testing and the control draw view lines

- **Status:** todo
- **Depends on:** P5-81
- **Size:** M

## Goal

With hidden areas, what is drawn and clicked is in **view** lines: the renderer reads view lines and view selections,
the gutter shows **model** line numbers, the mouse hit testing returns view positions that the view controller already
converts to model positions, and the control redraws when the hidden areas change. A test hides lines and checks the
drawn lines, numbers and clicks.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `browser/viewParts/lineNumbers/lineNumbers.ts` | 73–80, 111–145 | `onCursorStateChanged` (the active **model** line), `_getLineRenderLineNumber` (`convertViewPositionToModelPosition(viewLine, 1)`; an empty number when the column is not 1) |
| `browser/controller/mouseTarget.ts` | 310–327, 732–822 | `getFullLineRangeAtCoord` and `_hitTestViewLines`: `viewModel.getLineCount()` / `getLineMaxColumn()` are view-line calls |
| `browser/controller/dragScrolling.ts` | 138, 153, 208 | `viewModel.getLineCount()` / `getLineMaxColumn()` (view) |
| `browser/controller/mouseHandler.ts` | 534–590 | `model = this._context.viewModel` in `_getPositionOutsideEditor` (view) |
| `browser/view/viewController.ts` | 394–396 | `_convertViewToModelPosition` (already ported in `ViewController.cs` 226: every click goes through it) |

## Files to create or change

- `src/LoomEditor.Rendering/TextViewRenderer.cs` — change: line text from `viewModel.GetLineContent(viewLine)` /
  `GetLineLength`, long-line cut with `viewModel.GetLineMaxColumn`; `UpdateMaxLineWidth` compares with `viewModel.GetLineCount()`
- `src/LoomEditor.Rendering/TextViewRenderer.Gutter.cs` — change: the drawn number is the model line of the view line;
  the active number is the model line of the primary cursor
- `src/LoomEditor.Rendering/TextViewRenderer.HitTest.cs`, `TextViewRenderer.Reveal.cs` — change: `GetLineLayout` and
  `TryGetHorizontalRange` read view lines (`viewModel.GetLineCount()`, `GetLineContent`, …)
- `src/LoomEditor.Rendering/TextViewRenderer.Composition.cs` — change: composition ranges (model) are converted with
  `CoordinatesConverter.ConvertModelRangeToViewRange` before drawing
- `src/LoomEditor.View/Controller/MouseTargetFactory.cs`, `DragScrolling.cs`, `MouseDownOperation.cs` — change:
  `viewModel.Model.LineCount / GetLineMaxColumn / GetLineLength / GetLineContent` → the `ViewModel` view-line members
  (the list in "Design decisions")
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: `Render(…, _viewModel.GetViewSelections(), …)`; redraw on
  the outgoing `HiddenAreasChangedEvent`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Ime.cs` — change: the IME text bounds use the primary **view** selection
  and `_viewModel.GetLineMaxColumn` (view)
- `tests/LoomEditor.Rendering.Tests/TextViewRendererHiddenAreasTests.cs` — create
- `tests/LoomEditor.View.Tests/Controller/MouseTargetFactoryTests.cs` — change: hidden-area cases

## Design decisions (already made — do not change)

- The renderer receives **view selections** (Monaco's view draws `_cursor.getViewSelections()`); `ViewportData.Selections`
  are view selections. `ViewModel.GetSelections()` (model) is unchanged for every other caller.
- Exactly these `viewModel.Model.` uses switch to view lines (Monaco reads the view model there):
  `MouseTargetFactory` (all: 107–108, 114, 122, 152–153, 158, 164, 168, 177), `DragScrolling` 171, 182, 238,
  `MouseDownOperation` 176, 194, and the renderer files above. These stay **model** lines, as in Monaco:
  `CoreNavigationCommands.Home` 72, `CoreNavigationCommands.Scroll` 72 and 84, `CursorMoveCommands*`,
  `CommandExecutor`, `MultiCursorFeature`, `DeleteWordCommand`, `WordPartOperationsFeature` (they work on model positions).
- The tab size of `GetVisibleColumnFromColumn` in `MouseTargetFactory` 168 stays `viewModel.Model.Options.TabSize`.
- No new drawing for folded lines here (the `⋯` and the chevrons are P5-90).

## Tests

No Monaco test covers these view parts. New:

- [ ] Rendering: `Render_WithHiddenLines_DrawsOnlyVisibleLines` — model lines 1–6, hide 3–4: drawn texts are lines
      1, 2, 5, 6 at consecutive tops; gutter numbers are "1", "2", "5", "6"
- [ ] Rendering: `Render_CursorBelowHiddenLines_HighlightsItsModelLineNumber` (active number "5")
- [ ] Rendering: `Render_SelectionAcrossHiddenLines_DrawsTheViewSelection`
- [ ] View: `CreateMouseTarget_BelowAHiddenArea_ReturnsTheViewPosition` and, through `ViewController`, the click
      selects the model line 5
- [ ] View: `CreateMouseTarget_AfterLastViewLine_UsesTheLastViewLine` (last model lines hidden)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `lineNumbers.ts` row (model line numbers of view lines, P5-82); `mouseTarget.ts` row (view lines)
- [ ] Card status and `docs/tasks/README.md` updated
- [ ] No manual check here: the folding checks of P5-94 cover it

## Out of scope

Folding (P5-83…), gutter controls (P5-90, P5-91).

## Suggested commit message

`feat(rendering): draw and hit-test view lines`
