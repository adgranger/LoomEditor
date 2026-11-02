# Phase 14 — manual test list

What unit tests cannot check: the diff gutter seen from an app — lanes, buttons, hover, theme, zoom, scrolling, accessibility. Fill the **Result** line of
each check (`pass` / `fail` + a note, or `pending`).

**Machine:** Windows 11, a mouse with a wheel.

**Gallery setup:** run the Gallery and open the phase 14 diff demo (added by P14-05): a side-by-side `LoomDiffEditor` with a C# file of 5 change blocks (one of
them a pure deletion, one a pure insertion), controls for `BlockActionsVisibility`, the primary and secondary glyphs and tooltips, `RenderSideBySide`,
`LineHeight` and the theme, a **Large file** button (50 000 lines, 2 000 blocks) and the event log (newest first). The demo sets the primary action to glyph
`U+E73E` (CheckMark), tooltip `Stage block`, and logs `BlockActionInvoked {Action} {block}` with the block's `ToString()`.

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Date and tester:

## P14-03 — Glyph lanes and block buttons

1. The 5-block file, `BlockActionsVisibility = Always`, primary glyph set: exactly 5 primary buttons, one per block, each in the glyph margin at the first line of
   its block; both gutters have the same width (one lane, one line height wide).
   Result:
2. Click the button of the 3rd block: the log shows one line `BlockActionInvoked Primary o,n -> m,k` equal to `Changes[2].ToString()` (for example
   `12,3 -> 12,4`); the text and the selection do not change.
   Result:
3. The pure deletion's button is in the left (original) editor, at the first deleted line; no button for it on the right.
   Result:
4. `OnHover`: no button until the pointer is over a block's lines (gutter or text) on the side that shows its buttons; then only that block's buttons; moving to
   an unchanged line hides them; hovering the other editor shows nothing.
   Result:
5. Set a secondary glyph and tooltip: a second lane appears right of the first on both editors; its buttons log `BlockActionInvoked Secondary ...`. Clearing the
   primary glyph removes its lane and the secondary buttons move to the first lane.
   Result:
6. With `CommentGutterButtonEnabled` on for the modified editor: the comment "+" still appears on hover right of the glyph margin, and a click still raises
   `AddCommentRequested`.
   Result:
7. Switch the theme dark ↔ light: the block buttons take the new `button.background` / `button.foreground` colors at once.
   Result:
8. Ctrl+wheel zoom in and out, then `LineHeight = 30`: the lanes widen with the line height and the buttons stay centered in their lane and on their line
   (at most 22 × 22).
   Result:
9. Word wrap on (both editors), a long first line in a block: its button sits on the first row of that line.
   Result:
10. **Large file** (50 000 lines, 2 000 blocks), `Always`: scrolling with the wheel and dragging the scrollbar shows no visible lag; buttons appear only for visible
    blocks.
    Result:
11. `RenderSideBySide = false`: no glyph margin, no button; back to `true`: they come back. Same with a window narrow enough for the inline fallback.
    Result:
12. Narrator on a button: it reads the tooltip and the lines, e.g. `Stage block, lines 12 to 15` (`line 12` for a one-line block). The buttons are not tab stops.
    Result:
