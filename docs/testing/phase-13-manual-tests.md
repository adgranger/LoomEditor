# Phase 13 â€” manual test list

What unit tests cannot check: the host API seen from an app â€” drawing, scroll sync, zones with content, gutter clicks. Fill the **Result** line of each check
(`pass` / `fail` + a note, or `pending`).

**Machine:** Windows 11, a mouse with a wheel.

**Gallery setup:** the Gallery controls of this phase are added by P13-09.

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Date and tester:

## P13-04 â€” Decorations from the host

1. **Conflict colors** (Gallery, P13-09): lines 10â€“12 get the built-in `line-insert` background and lines 14â€“16 the `line-delete` background.
   Result:
2. After typing a new line above line 10, the tint moves down one line and does not grow when typing at the end of line 12.
   Result:
3. **Clear decorations** removes every tint.
   Result:
4. The overview ruler shows one mark per decorated block, in the host color.
   Result:
5. The same on the modified side of the diff demo (`Modified.CreateDecorationsCollection()`).
   Result:

## P13-05 — Scroll and layout sync

1. The **blame column** (Gallery, P13-09) shows one row per line; each row's top is within 1 px of its line while scrolling with the wheel, the scroll bar and `Ctrl+End`.
   Result:
2. After `Ctrl+=` (zoom) the rows have the new line height.
   Result:
3. With word wrap on, a wrapped line's row is as tall as its rows.
   Result:
4. Folding a region removes its rows and the next rows move up.
   Result:
5. A view zone (P13-08) pushes the rows below it down by its height.
   Result:
6. Resizing the window keeps the rows aligned.
   Result:

## P13-06 — Line numbers and ResetText

1. **Line numbers** check box (Gallery, P13-09) off: no number is drawn, the gutter is narrower by the numbers' width, the folding chevrons and the decorations still show; on: the numbers come back.
   Result:
2. **Reload same text** (Gallery, P13-09) after typing restores the original text (the typed characters are gone) although `Text` was set to the same string before.
   Result:
3. In the diff demo, setting `ModifiedText` to its current value after an edit does nothing, while `Modified.ResetText(...)` resets it and the diff updates.
   Result:

## P13-07 — Hidden areas and gutter clicks

1. **Hide unchanged** (Gallery, P13-09) hides lines 20–40 (line 19 is followed by line 41; the blame column has no rows for them); **Show all** brings them back.
   Result:
2. Folding a region while host areas are set, then **Show all**: the folded region stays folded.
   Result:
3. A left click on the line decorations of line 12 in the Gallery's staging demo toggles its "staged" tint and does **not** move the caret.
   Result:
4. A click on the line number of line 12 (the handler does not set `Handled`) selects the line as before and the log shows `GutterClicked line 12 LineNumbers`.
   Result:
5. A right click raises nothing; a click below the last line raises nothing.
   Result:
6. The folding chevron still folds when the handler does not set `Handled`.
   Result:
