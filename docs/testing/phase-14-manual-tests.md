# Phase 14 — manual test list

What unit tests cannot check: the diff gutter seen from an app — lanes, buttons, hover, theme, zoom, scrolling, accessibility. Fill the **Result** line of
each check (`pass` / `fail` + a note, or `pending`).

**Machine:** Windows 11, a mouse with a wheel.

**Gallery setup:** run the Gallery and click **Diff editor window**. The phase 14 controls are in the **Staging** expander (`DiffDemoWindow.Actions.cs`), above
the editor; open it. They are: the `BlockActionsVisibility` combo (Off / OnHover / Always), the check boxes **Stage action** (primary action: glyph `U+E710`,
tooltip `Stage block`; on by default), **Discard action** (secondary action: glyph `U+E7A7`, tooltip `Discard block`), **LineSelectionEnabled**, **Line hover
button** (`LineActionGlyph` `U+E710`, tooltip `Stage lines`), **Word wrap** (both editors), **Line height 30** and **Light theme** (light ↔ dark), the buttons
**Staging sample**, **Large file** (50 000 lines, about 2 000 blocks), **Clear selection** and **Select all added lines**, the **Selected:** text (the
`SelectedLines`, e.g. `O7 M7`) and the event log (newest first). The toolbar above keeps **Side by side**, **Ignore trim whitespace**, **Original editable**,
**Read-only modified**, **Gutter "+" button** and **Reset texts**.

The log lines: `BlockActionInvoked {Action} {block}` (the block's `ToString()`, `o,n -> m,k`), `LineActionInvoked {lines}` (e.g. `O7 M7`),
`SelectionChanged {count}` and `DiffChanged {Changes.Count} blocks`.

**Staging sample** (C#, original with CRLF, modified with LF) has 5 blocks: `2,0 -> 2,1` (pure insertion, `using System.Linq;`), `4,1 -> 5,0` (pure deletion,
`// Obsolete helpers below.`), `7,1 -> 7,1` (a modified pair, `Greeter` → `Welcome`), `10,2 -> 10,4` (2 lines become 4) and `14,1 -> 16,1`.

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Date and tester:

## P14-03 — Glyph lanes and block buttons

1. **Staging sample**, `BlockActionsVisibility` = `Always`, **Stage action** on: exactly 5 primary buttons, one per block, each in the glyph margin at the first
   line of its block; both gutters have the same width (one lane, one line height wide).
   Result:
2. Click the button of the 3rd block (modified line 7, `public static class Welcome`): the log shows one line `BlockActionInvoked Primary 7,1 -> 7,1`; the text
   and the selection do not change.
   Result:
3. The pure deletion's button is in the left (original) editor, at original line 4 (`// Obsolete helpers below.`); no button for it on the right.
   Result:
4. `BlockActionsVisibility` = `OnHover`: no button until the pointer is over a block's lines (gutter or text) on the side that shows its buttons; then only that
   block's buttons; moving to an unchanged line hides them; hovering the other editor shows nothing.
   Result:
5. Back to `Always`, check **Discard action**: a second lane appears right of the first on both editors; its buttons log `BlockActionInvoked Secondary ...`.
   Uncheck **Stage action**: its lane goes away and the discard buttons move to the first lane.
   Result:
6. Check **Gutter "+" button**: the comment "+" still appears on hover right of the glyph margin of the modified editor, and a click still logs
   `AddCommentRequested` (in the toolbar's comment log).
   Result:
7. Toggle **Light theme** on and off: the block buttons take the new `button.background` / `button.foreground` colors at once.
   Result:
8. Ctrl+wheel zoom in and out, then check **Line height 30**: the lanes widen with the line height and the buttons stay centered in their lane and on their line
   (at most 22 × 22).
   Result:
9. **Reset texts**, check **Word wrap**, then narrow the window until the long line of the sample (the one with a renamed word) wraps: its block's button sits
   on the first row of that line.
   Result:
10. **Large file**, `Always`: scrolling with the wheel and dragging the scrollbar shows no visible lag; buttons appear only for visible blocks.
    Result:
11. Uncheck **Side by side**: no glyph margin, no button; check it again: they come back. Same with a window narrow enough for the inline fallback.
    Result:
12. Narrator on a button: it reads the tooltip and the lines, e.g. `Stage block, lines 10 to 13` on the 4th block (`Stage block, line 7` on the 3rd). The
    buttons are not tab stops.
    Result:

## P14-04 — Line checkboxes and the line action

Setup: **Staging sample**, **LineSelectionEnabled** and **Line hover button** checked, **Read-only modified** checked (the original side is read-only while
**Original editable** is unchecked). The modified pair is at original line 7 / modified line 7.

1. Click the right (added) checkbox of line 7: it shows a check mark; **Selected:** shows `M7` only and the log `SelectionChanged 1`; the left box of line 7
   stays empty.
   Result:
2. **Clear selection**, then click the left (deleted) checkbox of line 7 instead: **Selected:** shows `O7` only.
   Result:
3. Ctrl+click either box of line 7: both boxes checked, **Selected:** shows `O7 M7` (`SelectionChanged 2`); Ctrl+click again: both empty, nothing selected
   (`SelectionChanged 0`).
   Result:
4. Click added line 10, then Shift+click added line 16 (across two blocks): **Selected:** shows `M10 M11 M12 M13 M16`; the unchanged lines 14 and 15 have no box
   and are not selected; the original side is untouched.
   Result:
5. The pure insertion's line (modified line 2) has a box on the right only and selects alone (`M2`); the pure deletion's line (original line 4) has a box on the
   left only and selects alone (`O4`).
   Result:
6. Caret on an added line, no selection, press S: the log shows `LineActionInvoked` with that one line (e.g. `LineActionInvoked M7`). With a selection, press S:
   `LineActionInvoked` with the selection, in the order of **Selected:**. Caret on an unchanged line, no selection: no event. Shift+S or Ctrl+S: no event.
   Result:
7. Uncheck **Read-only modified**, caret in the modified editor: S types an `s` and raises nothing. Same in the original editor with **Original editable**
   checked.
   Result:
8. Hover a changed line (gutter or text) on either side: one line action button appears in its lane on that line only, with the tooltip `Stage lines`; click it:
   `LineActionInvoked` with that line, or with the whole selection if that line is selected. Hovering an unchanged line shows no button.
   Result:
9. With a selection, click **Reset texts** (the host sets new texts): the selection is cleared (`SelectionChanged 0`) and every box is empty. Back to **Staging
   sample**, select lines, then toggle **Ignore trim whitespace** (same texts): the selection is kept (minus lines that are no longer changed lines).
   Result:
10. **Word wrap** on/off, **Light theme** on/off, **Large file**: the boxes sit centered in the first lane on the first row of each line (at most 16 × 16),
    recolor with the theme, and scrolling shows no visible lag; unchecking **LineSelectionEnabled** removes both lanes and clears the selection.
    Result:
11. Narrator on a box reads `Select added line 7, toggle button, off` (`on` when checked; `Select deleted line 7` on the left); the line action button reads
    `Stage lines, line 7`. Neither is a tab stop.
    Result:

## P14-05 — The demo

1. Click **Staging sample**: the log shows `DiffChanged 5 blocks`: the CRLF original against the LF modified text adds no block of its own.
   Result:
2. **LineSelectionEnabled** on, click **Select all added lines**: **Selected:** shows `M2 M7 M10 M11 M12 M13 M16` and the log `SelectionChanged 7`; every right
   box of a changed line is checked, no left box is. **Clear selection**: **Selected:** is empty and the log shows `SelectionChanged 0`.
   Result:
3. **Large file**: the log shows `DiffChanged n blocks` with `n` close to 2 000.
   Result:
