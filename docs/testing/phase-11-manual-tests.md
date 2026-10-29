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

## P11-14 — Focus and keys between the editor and the widget's text box

Click **Start composing on line 5**: a thread opens on line 5 with a temporary text box and **Cancel** / **Comment** buttons (P11-17 replaces them),
and the box has the focus. The status line counts `CommentSubmitted` events (the Gallery adds each submitted comment to its thread).

| # | Check | Result |
|---|---|---|
| 1 | Type `abc` and `Backspace` in the box: the box shows `ab`; the document is unchanged (after leaving the box, `Ctrl+Z` does nothing) and its caret does not blink | pending |
| 2 | `Tab` goes box → Cancel → Comment → the next control after the editor; `Shift+Tab` goes back; the document gets no tab | pending |
| 3 | `Enter` adds a line in the box; `Ctrl+Enter` submits (`CommentSubmitted` goes up by exactly 1) | pending |
| 4 | `Ctrl+A`, `Ctrl+Z`, `Ctrl+C` / `Ctrl+V`, `Ctrl+F`, `F12`, `Ctrl+D` in the box act on the box or do nothing; **no** editor command runs (no find widget, no selection change in the document) | pending |
| 5 | `Escape` with an empty box: the composer closes, the focus is in the document, the caret on line 5 blinks. `Escape` with text: nothing happens | pending |
| 6 | Click in the document while the box has text, then click back in the box: the text is still there | pending |
| 7 | **Remove all** while the box has the focus: the focus lands in the document (not on the window); typing works at once. Clicking the button moves the focus to it first, so also check the same path from inside the widget: on a thread with no comment, click **Cancel** (the thread is removed while its button has the focus) | pending |
| 8 | A Japanese or Chinese IME composes in the box, its candidate window is next to the box; `Win+.` inserts an emoji in the box | pending |
| 9 | Narrator follows the focus into the box and back | pending |

## P11-15 — The shared Markdown element builder

The hover's Markdown now comes from `MarkdownElementBuilder`; nothing should look different. **Before** checking out this card's commit, take screenshots
of the popups below with the previous build and keep them out of the repository; then compare with this build.

| # | Check | Result |
|---|---|---|
| 1 | The hover of a marker ("Problems" sample, **Add sample problems**): message, dimmed source / code, separators look the same as before | pending |
| 2 | A Markdown hover with code, a list, a heading, a link and bold text (Gallery **Demo providers: on** or a language server): same fonts, margins, bullet glyph, heading size and link color as before | pending |
| 3 | The completion documentation (details pane of the suggest widget) looks the same as before | pending |

## Findings

| # | Section / check | What happened | Card or fix |
|---|---|---|---|
