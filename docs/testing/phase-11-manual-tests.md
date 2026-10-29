# Phase 11 — manual test list

What unit tests cannot check: the comment thread widgets under a real UI.
Fill the **Result** column (`pass` / `fail` + a note, or `pending`) and the **Findings** table at the end.

**Machine:** Windows 11, a mouse with a wheel and (optional) a touchpad. About 15 minutes for the sections written so far.
Pick the sample "Comment threads (P11-12)" in the Gallery "Features" picker; its buttons are in the **Comment threads (P11-12)** Expander, whose
status line shows `zones {n} visible {v} lastArrange {ms} ms` (refreshed every 500 ms).

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Date and tester:

## P11-12 — The zone-widget layer

A placeholder box (title and comments as plain text) covers each thread's 120 px gap. Use the Gallery's zoom at 100 % unless a check says otherwise.
The text-area origin is the origin of the zone-widget canvas (both fill the same template cell with no margin): check 1 verifies it.

| # | Check | Result |
|---|---|---|
| 1 | **Add 3 threads**: the box of line 5 has its top edge exactly at the bottom of line 5, and line 6 starts 120 px lower (screenshot ruler or line numbers) | pending |
| 2 | Scroll by wheel and by dragging the scroll bar for 10 s: in every screenshot taken during the scroll (Win+Shift+S) the box is never more than 1 px away from line 5 | pending |
| 3 | Scroll line 5 above the top edge: the box is cut at the editor's top edge, nothing is drawn above the editor; the same at the bottom edge and above the horizontal scroll bar | pending |
| 4 | `Ctrl+F` and scroll a box under the find widget: the find widget stays on top; a hover popup and the completion list are drawn above a box | pending |
| 5 | Insert a line above line 5: the three boxes move down one line | pending |
| 6 | Resize the window and zoom (`Ctrl` + wheel): the box keeps `ContentLeft + 4` and the content width; it never covers the line numbers, the minimap or the vertical scroll bar | pending |
| 7 | Word wrap on with a long line 5: the box sits under the **last** wrapped row of line 5 | pending |
| 8 | A click and a drag on a box do not move the caret or select text; the wheel over a box scrolls the editor | pending |
| 9 | **Add 200 threads**: scrolling stays smooth; `lastArrange` stays under 1 ms (write the measured value here) | pending |
| 10 | Collapse a fold that contains a thread's line: its box disappears (`visible` drops); expand: it returns | pending |

## P11-13 — The zone height follows the widget's content

Start from **Add 3 threads**. The diagnostics line now also shows `heightPasses {n} heightLimitHits {m}`. A new thread's gap shrinks from 120 px to its title row.

| # | Check | Result |
|---|---|---|
| 1 | **Add a comment to the first thread**: the box and its gap grow together; the line below the box touches its bottom edge (no gap, no overlap: 0 px in a screenshot) | pending |
| 2 | After check 1, `heightPasses` increased by 1 or 2 and `heightLimitHits` = 0 | pending |
| 3 | Narrow the window so the comment wraps on more rows: the gap follows within one frame after the resize stops | pending |
| 4 | **Toggle a long title** repeatedly (narrow window so the title wraps): no flicker, `heightLimitHits` stays 0 | pending |
| 5 | **Add 200 threads**, resize the window: no freeze longer than a visible hitch, `heightLimitHits` = 0 | pending |
| 6 | Zoom in and out (`Ctrl` + wheel, A− / A+): the heights follow the font size. The placeholder text uses the XAML default font, so until P11-16 only the text lines move; write what you see | pending |

## Findings

| # | Section / check | What happened | Card or fix |
|---|---|---|---|
