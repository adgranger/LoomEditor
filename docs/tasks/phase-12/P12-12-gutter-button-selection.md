# P12-12 — The gutter "+" button requests the selected lines

- **Status:** todo
- **Depends on:** P12-11
- **Size:** S

**Recommended model:** Sonnet

## Goal

With several lines selected, clicking the gutter **+** on any of those lines requests a thread on the whole selected range (GitHub: select lines, then click **+**). Elsewhere
the button works exactly as in phase 11.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decision 1). Read first: `src/LoomEditor.WinUI/Controls/Comments/CommentGutterController.cs`
(`Update`, `OnClick`, `_shownLine`), `CommentGutterButton.cs` (`SetLine`, the automation name), `src/LoomEditor.View/Comments/CommentLineRange.cs` (P12-08),
`LoomEditor` members that give the primary selection (`Selections[0]`, used by the Gallery), `docs/tasks/phase-11/P11-21-gutter-add-comment-button.md`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentGutterController.cs` — change
- `src/LoomEditor.WinUI/Controls/Comments/CommentGutterButton.cs` — change: `SetLine(int line)` → `SetLines(int startLine, int line)`
- `docs/testing/phase-12-manual-tests.md` — change: section "P12-12"

## Design decisions (already made — do not change)

- Where the button appears does **not** change (hovered line, one box per last line: `HasThreadOn` still compares `thread.LineNumber`).
- New private `(int Start, int Line) TargetOf(int hoveredLine)`: `var (s, e) = CommentLineRange.FromSelection(_editor.Selections[0]);` when `s < e && s <= hoveredLine && hoveredLine <= e`
  return `(s, e)`, else `(hoveredLine, hoveredLine)`. Computed when the button is shown for a line (in `Update`, only when `_shownLine` changes — no work per pointer move).
- `OnClick` calls `_controller.RequestAdd(start, line)`.
- `CommentGutterButton.SetLines(start, line)`: automation name `CommentStrings.AddCommentOnLines(start, line)` (equals the phase 11 string for one line). The selection
  changing while the button is visible re-targets on the next `Update` (pointer move or `Arrange` after a draw) — acceptable, no selection listener.
- Manual section "P12-12": (1) select lines 3–6 (mouse drag in the text), hover the gutter of line 4, click **+**: the log shows `AddCommentRequested, lines 3–6` and the box opens
  under line 6; (2) select lines 3–6 with `Shift+Down` from line 3 (the selection ends at column 1 of line 7): request is 3–6, not 3–7; (3) hover line 9 (outside the
  selection), click: request is line 9 only; (4) no selection: phase 11 behavior; (5) Narrator reads "Add a comment on lines 3 to 6" on the button inside the selection.

## Tests

- [ ] None automated (the rule is `CommentLineRange`, tested in P12-08).
- [ ] Manual: section "P12-12" written (not run by the agent).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The manual section is written; the card's Result section says it was not run
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Dragging from the button to select lines, shift-click in the gutter, showing the button on every line of the selection, a selection-changed listener.

## Suggested commit message

`feat(winui): let the gutter button request the selected lines`
