# P14-03 — Glyph lanes and the per-block action buttons

- **Status:** done
- **Depends on:** P14-02
- **Size:** M

**Recommended model:** Sonnet

## Goal

In side-by-side, every change block shows up to two host-defined action buttons in the glyph margin at its first line (always, or while the pointer is over the
block); a click raises `BlockActionInvoked` with the block and the action. A file with N blocks has N buttons per action.

## Sources

Sources: none (specified in this card; [phase-14/README.md](README.md) decisions 1, 4). Read first:
`src/LoomEditor.WinUI/Controls/Comments/CommentGutterController.cs` and `CommentGutterButton.cs` (the pattern to copy: overlay, hit test, `Arrange`, theme),
`LoomEditor.CommentThreads.cs` (how the controller is created and fed pointer moves), `LoomEditor.cs` (`Arrange` calls near line 283), `LoomEditor.HostHooks.cs`
(`ApplyHostLayoutOptions`), `src/LoomEditor.View/Layout/ViewModel.cs` (`GlyphMarginDecorationLaneCount: 1`), `EditorLayoutInfoComputer.cs` (glyph width = lanes × line height),
`LoomDiffEditor.Options.cs` (how a diff option DP is declared and forwarded), `LoomDiffEditor.cs`.

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModel.cs` — change: internal settable `GlyphMarginLaneCount` (default 1) used instead of the literal `1`; setting it re-lays out
- `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs` — change: internal `GlyphMarginLaneCount` (0 = no glyph margin; else `GlyphMargin = true` and that lane count)
- `src/LoomEditor.WinUI/Controls/Gutter/GutterButtonTheme.cs` — create: the `ApplyTheme` body of `CommentGutterButton`, shared (`static void Apply(Button, EditorTheme)`);
  `CommentGutterButton.ApplyTheme` calls it
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffGutterLanes.cs` — create: computes the lanes in use and each lane's index from the options (decision 1)
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffBlockActionsController.cs` — create (one per side editor)
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.BlockActions.cs` — create: DPs, event, wiring
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffBlockActionsVisibility.cs`, `DiffBlockAction.cs`, `DiffBlockActionInvokedEventArgs.cs` — create
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt`; `docs/testing/phase-14-manual-tests.md` — create (header from phase 13), section "P14-03"

## Design decisions (already made — do not change)

- Lanes: `DiffGutterLanes` is recomputed when any phase 14 DP or `RenderSideBySideEffective` changes; it sets `GlyphMarginLaneCount` on **both** editors (same count
  on both, so the two gutters keep the same width); 0 lanes when not side-by-side or nothing is on. Lane `k` is at `GlyphMarginLeft + k × LineHeight`.
- **Which editor shows a block's buttons:** the modified editor at `ModifiedStartLineNumber`; for a pure deletion (`ModifiedLineCount == 0`) the original editor at
  `OriginalStartLineNumber`. One set of buttons per block.
- `DiffBlockActionsController(LoomEditor editor, OverlayHost layer, bool modifiedSide, Func<double,double,MouseTarget> hitTest)`: `SetBlocks(IReadOnlyList<DiffChangeBlock>)`,
  `Configure(visibility, primary glyph/tooltip, secondary glyph/tooltip, primaryLane, secondaryLane)`, `OnPointerMoved` / `OnPointerLeft` (same calls as the
  comment controller), `Arrange()` after each draw, `ApplyTheme`, `Dispose`, `event Action<DiffChangeBlock, DiffBlockAction> Invoked`.
- **Visible blocks only:** find the first and last visible model line (from `ScrollTop`, the viewport height and `GetTopForLineNumber`/the view model's visible range
  — reuse what exists, add an internal helper on `LoomEditor` if needed); binary-search the blocks whose anchor line is in that range; skip an anchor hidden by a
  hidden unchanged region. Buttons come from a pool (`List<Button>`): grow when needed, hide the extra ones (`OverlayHost.Hide`); never one element per block.
- `OnHover`: the buttons of the block whose lines (on this editor's side; the anchor line for an empty side) contain the hovered line — pointer over gutter **or**
  text. A hover on the other editor does not show them (kept simple).
- Button: `Button` with `Content = new FontIcon { Glyph = glyph, FontSize = 12 }` (font from the `SymbolThemeFontFamily` resource), square of
  `Math.Min(22, LineHeight - 2)`, centered in its lane and on the first row of the line (word wrap: first row, as the comment "+"). `ToolTipService.ToolTip` = the
  tooltip; `AutomationProperties.Name` = `"{tooltip}, lines {s} to {e}"` (the modified range; the original range for a pure deletion) from a new
  `DiffStrings.BlockAction(tooltip, start, end)` in View (single-line form `"{tooltip}, line {s}"`). `IsTabStop = false`. Colors: `GutterButtonTheme.Apply`.
- A glyph set to null hides that action's lane. `BlockActionsVisibility` DP default `Off`; glyph/tooltip DPs default null.
- `BlockActionInvoked` args: `Block` (the `DiffChangeBlock` instance from `Changes`) and `Action`. Raised on the UI thread, synchronously from the click.
- The controllers receive `Changes` in `OnViewModelDiffChanged`; theme changes reach them through the editors' theme path (as the comment button).

## Tests

- [x] New (View): `DiffStrings_BlockAction_OneLine` / `_Lines`
- [x] Manual "P14-03" (written, not run): (1) a file with 5 blocks, `Always`: 5 buttons, one per block, at each block's first line; (2) click the 3rd: the Gallery log
  shows `BlockActionInvoked Primary 12,3 -> 12,4` matching `Changes[2]`; (3) a pure deletion's button is on the left side; (4) `OnHover`: buttons only on the hovered
  block; (5) secondary action: a second lane, `Secondary` in the log; (6) the comment "+" still appears and works; (7) dark/light theme switch recolors the buttons;
  (8) zoom (Ctrl+wheel) and `LineHeight = 30`: lanes and buttons grow; (9) word wrap on: buttons on the first row of the line; (10) a 50 000-line file with 2 000
  blocks scrolls without visible lag; (11) `RenderSideBySide = false`: no lane, no button; (12) Narrator reads the automation name.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Manual section written; the card's Result section says it was not run
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Line checkboxes and the line action (P14-04), keyboard access to the buttons, rendering glyph decorations, buttons in the inline view.

## Suggested commit message

`feat(winui): per-block action buttons in the LoomDiffEditor gutter`

## Result

Build (0 warnings) and tests green; 2 new View tests in `DiffStringsTests`. The 12 manual checks of `docs/testing/phase-14-manual-tests.md` (section P14-03) were written, not run (agents do not run the Gallery UI; the Gallery demo comes with P14-05). Notes: `LoomEditor` gained internal hooks (`GlyphMarginLaneCount`, `SetBlockActionsFactory`/`BlockActions`, `GetVisibleModelRanges`) and calls the controller from its draw, pointer and theme paths next to the comment `+`; the visible lines come from `ViewModel.GetModelVisibleRanges` (made public in the internal class), so an anchor in a hidden unchanged region gets no button. A null tooltip gives the automation name `Block action, lines s to e` (`DiffStrings.BlockActionFallback`). `DiffGutterLanes` already takes the P14-04 lanes; they are passed as off until P14-04.
