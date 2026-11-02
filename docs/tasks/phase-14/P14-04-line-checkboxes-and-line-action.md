# P14-04 — Line checkboxes and the line action

- **Status:** todo
- **Depends on:** P14-03
- **Size:** M

**Recommended model:** Sonnet

## Goal

With `LineSelectionEnabled`, every changed line in side-by-side has a checkbox in the first glyph lane (deleted lines on the left, added lines on the right): click
toggles one line, Ctrl+click the pair, Shift+click extends. A per-line hover button (when `LineActionGlyph` is set) and a key (`LineActionKey`, S by default) raise
`LineActionInvoked`.

## Sources

Sources: none (specified in this card; [phase-14/README.md](README.md) decisions 1–4). Read first: `DiffBlockActionsController.cs`, `DiffGutterLanes.cs` (P14-03),
`DiffLineSelection.cs`, `LoomDiffEditor.LineSelection.cs` (P14-02), `CommentGutterController.cs`, `LoomEditor.cs` (`OnKeyDown`).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffLineCheckBoxesController.cs` — create (one per side editor)
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.LineSelection.cs` — change: DPs, keys, event
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffLineActionInvokedEventArgs.cs` — create
- `src/LoomEditor.View/DiffEditor/DiffStrings.cs` — change: checkbox and line-action names
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt`; `docs/testing/phase-14-manual-tests.md` — section "P14-04"

## Design decisions (already made — do not change)

- Checkbox control: `ToggleButton` (UIA Toggle pattern), square `Math.Min(16, LineHeight - 4)`, a `FontIcon` check mark (``) shown only when checked,
  `IsTabStop = false`, colors from `GutterButtonTheme.Apply`. The model is the truth: in `Click`, read the modifiers
  (`InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control/Shift)` has `Down`), call `ExtendLineSelection` (Shift), `ToggleLinePair` (Ctrl) or `ToggleLine`,
  then re-sync every visible box's `IsChecked` from the model (the pair partner is on the other editor: `SelectionChanged` re-syncs both controllers).
- Visible changed lines only, pooled exactly as P14-03's buttons. Automation name: `"Select deleted line {n}"` / `"Select added line {n}"` (`DiffStrings`).
- Line action hover button: shown in the line-action lane on the hovered **changed** line of either editor (gutter or text hover), one at a time;
  `ToolTip` = `LineActionToolTip`; automation name `"{tooltip}, line {n}"`. Click: `Lines` = the selection if the hovered line is selected, else that single line.
- Key: in the diff editor's `PreviewKeyDown` (handledEventsToo is not needed: preview runs first), when `LineSelectionEnabled`, side-by-side, no modifier down, the
  key equals `LineActionKey`, and the focused editor is **read-only** (`IsReadOnly`, or the original side with `OriginalEditable` off): `Lines` = the selection if not
  empty, else the caret line of the focused editor if it is a changed line, else nothing (no event, key not handled). When raised, `e.Handled = true`. An editable
  side never loses the S key.
- `LineActionInvoked` args: `IReadOnlyList<DiffLine> Lines` (never empty; same order as `SelectedLines`). `LineSelectionEnabled` turning off clears the selection.
- DPs: `LineSelectionEnabled` (false), `LineActionGlyph` / `LineActionToolTip` (null), `LineActionKey` (`VirtualKey.S`).

## Tests

- [ ] New (View): `DiffStrings_SelectLine_Deleted` / `_Added`
- [ ] Manual "P14-04" (written, not run): (1) a modified pair 7/7: click the right box → `SelectedLines` = `Modified 7` only; (2) click the left box instead →
  `Original 7` only; (3) Ctrl+click either → both; Ctrl+click again → none; (4) Shift+click from added line 3 to added line 20 across two blocks: only changed lines
  in between; (5) a pure insertion and a pure deletion each select alone; (6) press S with the caret on an added line and no selection → `LineActionInvoked` with that
  line; with a selection → the selection; on an unchanged line → nothing; (7) `IsReadOnly = false`: S types an "s" in the modified editor; (8) hover button on a
  changed line → the event with that line; (9) edit the text (host sets new `ModifiedText`) → selection cleared; toggle `IgnoreTrimWhitespace` (same text) → kept;
  (10) word wrap on/off, dark/light, 50 000 lines: same checks as P14-03; (11) Narrator reads "Select added line 7, toggle button, off".

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Manual section written; the card's Result section says it was not run
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drag-to-select over checkboxes, a background highlight of selected lines, keyboard focus on the checkboxes.

## Suggested commit message

`feat(winui): line checkboxes and the line action in LoomDiffEditor`
