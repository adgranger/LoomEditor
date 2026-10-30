# Phase 13 — manual test list

What unit tests cannot check: the host API seen from an app — drawing, scroll sync, zones with content, gutter clicks. Fill the **Result** line of each check
(`pass` / `fail` + a note, or `pending`).

**Machine:** Windows 11, a mouse with a wheel.

**Gallery setup:** run the Gallery, click **Host API demo** (Phase 9 expander, under "Diff editor window"). The window shows, from top to bottom: the buttons
**Conflict colors**, **Clear decorations**, **Hide unchanged**, **Show all**, **Add conflict header**, **Add text box zone**, **Remove zones**, **Reload same text**
and the **Line numbers** check box (checked); a **File name** box (`Conflicts.cs`) with **Apply language**; the 220-DIP **blame column** left of the editor
(a 66-line C# file with a conflict block on lines 9–17); the event log (newest first, 50 lines). The language is picked from the file name on load.

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Date and tester:

## P13-01 — Language by file name

1. **File name** = `Program.cs` → **Apply language**: the text is highlighted as C# and the log shows `language csharp`.
   Result:
2. **File name** = `notes.unknown` → **Apply language**: the log shows `language null` and the text becomes plain.
   Result:

## P13-04 — Decorations from the host

1. **Conflict colors**: lines 10–12 get the built-in `line-insert` background and lines 14–16 the `line-delete` background; lines 9, 13 and 17 get a colored band.
   Result:
2. After typing a new line above line 10, the tint moves down one line and does not grow when typing at the end of line 12.
   Result:
3. **Clear decorations** removes every tint.
   Result:
4. The overview ruler shows one mark per decorated block, in the host color.
   Result:
5. API check (no Gallery control): the same on the modified side of a diff (`Modified.CreateDecorationsCollection()`).
   Result:

## P13-05 — Scroll and layout sync

1. The **blame column** shows one row per line; each row's top is within 1 px of its line while scrolling with the wheel, the scroll bar and `Ctrl+End`.
   Result:
2. After `Ctrl+=` (zoom) the rows have the new line height.
   Result:
3. With word wrap on, a wrapped line's row is as tall as its rows.
   Result:
4. Folding a region removes its rows and the next rows move up.
   Result:
5. **Add conflict header** (a zone after line 8) pushes the rows below it down by its height.
   Result:
6. Resizing the window keeps the rows aligned.
   Result:

## P13-06 — Line numbers and ResetText

1. **Line numbers** check box off: no number is drawn, the gutter is narrower by the numbers' width, the folding chevrons and the decorations still show; on: the numbers come back.
   Result:
2. Type a few characters, then **Reload same text**: the original text is restored (the typed characters are gone) although `Text` was set to the same string first; the log shows `ResetText`.
   Result:
3. API check (no Gallery control): in a diff, setting `ModifiedText` to its current value after an edit does nothing, while `Modified.ResetText(...)` resets it and the diff updates.
   Result:

## P13-07 — Hidden areas and gutter clicks

1. **Hide unchanged** hides lines 20–40 (line 19 is followed by line 41; the blame column has no rows for them); **Show all** brings them back.
   Result:
2. Fold a region, **Hide unchanged**, then **Show all**: the folded region stays folded.
   Result:
3. A left click on the line decorations of line 12 toggles its green "staged" bar and does **not** move the caret; clicking three lines gives three bars, each toggles back on a second click.
   Result:
4. A click on the line number of line 12 (the handler does not set `Handled`) selects the line as before and the log shows `GutterClicked line 12 LineNumbers`.
   Result:
5. A right click raises nothing; a click below the last line raises nothing.
   Result:
6. The folding chevron still folds when the handler does not set `Handled` (the log shows its click too).
   Result:

## P13-08 — Host view zones

1. **Add conflict header**: a 1-line-high bar with **Accept current** and **Accept incoming** appears between lines 8 and 9; the text below moves down by one line height.
   Result:
2. The bar scrolls with the text and is clipped at the top and bottom edges.
   Result:
3. Clicking **Accept current** logs `Accept current` and does not move the caret.
   Result:
4. Typing a new line above line 8 moves the bar down with its line.
   Result:
5. **Remove zones** removes it and the text closes the gap; the log shows `RemoveViewZone -> True`, then `RemoveViewZone (again) -> False`.
   Result:
6. **Add text box zone** (after line 30, two lines high) and the P11-14 checks (a)–(f), one line each: typing in the box inserts nothing in the editor.
   Result:
   `Tab` leaves the box by XAML focus navigation.
   Result:
   The caret of the editor stops blinking while the box has the focus.
   Result:
   Clicking in the box does not move the editor caret; clicking the text takes the focus back.
   Result:
   Keyboard shortcuts typed in the box (`Ctrl+Z`, `Ctrl+A`) act on the box, not on the editor.
   Result:
   An IME composition in the box does not reach the editor.
   Result:
7. **Remove zones** while the `TextBox` has the focus: the editor has the focus.
   Result:
8. Not reachable from the demo (zone after line 0; a zone next to a comment thread): covered by unit tests.
   Result:

## End-to-end A — Blame

1. Open **Host API demo**: the blame column has one row per visible line, three background shades by age, and a new commit id every 7 lines.
   Result:
2. Scroll with the wheel, drag the scroll bar, `Ctrl+End`: every row stays within 1 px of its line.
   Result:
3. Zoom with `Ctrl+=` / `Ctrl+-`, turn word wrap on, fold a region: the column stays aligned (rows follow the new line height, wrapped rows are taller, folded lines have no row).
   Result:
4. Uncheck **Line numbers**: the numbers disappear, the column stays aligned; check it again: they come back.
   Result:

## End-to-end B — Conflicts

1. **Conflict colors**: lines 10–12 and 14–16 are tinted, the marker lines 9, 13, 17 are banded.
   Result:
2. **Add conflict header**: the bar appears between lines 8 and 9, the blame rows below it move down by one line height.
   Result:
3. Click **Accept current**: the log shows `Accept current`.
   Result:
4. **Clear decorations**: every tint is gone and the bar stays.
   Result:
5. **Remove zones**: the bar is gone, the text closes the gap, the log shows `True` then `False`.
   Result:

## End-to-end C — Staging

1. **Hide unchanged**: lines 20–40 disappear, the blame column has no rows for them.
   Result:
2. Click the line decorations of three visible lines: each gets a green bar, the caret does not move, the log shows three `GutterClicked ... LineDecorations` lines; a second click on one removes its bar.
   Result:
3. **Show all**: the hidden lines come back and the bars are still on their lines.
   Result:

## Self-check

The scripted check of the host API (`LoomEditor.Gallery.exe --hostapi-selfcheck=<file>`) is added by P13-10; this plan covers what it cannot.
