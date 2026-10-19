# Phase 3 — manual test list

What unit tests cannot check: keyboard layouts, the mouse, the clipboard with other applications, IMEs, display scales.
Run it on the **Native AOT** build of the Gallery, on Windows 11:

```powershell
dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64
```

and fill the **Result** column (`pass` / `fail` + a note). Nothing has been run yet.

- Windows build: _to fill_
- Keyboard layouts: _to fill_
- IME versions (Japanese, Pinyin, Korean): _to fill_
- Date and tester: _to fill_

Every step starts from the Gallery with the **Reset** button, unless it says otherwise.

## 1. Keyboard

| # | Steps | Expected | Result |
|---|---|---|---|
| 1.1 | US layout: type `Hello, world!` at the end of the text, then Enter and `x` | The text appears once, the caret follows, Enter makes a new line | |
| 1.2 | French layout: type `é`, `è`, `à`, `ù`, `^` then `e` (dead key), `¨` then `u`, AltGr+`é` (`~`), AltGr+`à` (`@`), AltGr+`)` (`]`) | `é è à ù ê ü ~ @ ]`; no character is doubled or lost | |
| 1.3 | Arrows, Home, End, Ctrl+Left/Right, Ctrl+Home/End, PageUp/PageDown | The caret moves like in VS Code; the view scrolls to keep the caret visible | |
| 1.4 | Backspace, Delete, Ctrl+Backspace, Ctrl+Delete, Tab, Shift+Tab on a selection | Text is deleted / indented / outdented like in VS Code | |

## 2. Mouse

| # | Steps | Expected | Result |
|---|---|---|---|
| 2.1 | Click in the text, between two characters, after the end of a line, below the last line | The caret goes to the nearest position | |
| 2.2 | Double-click a word, triple-click a line | The word / the line is selected | |
| 2.3 | Drag from the middle of a line to another line, then past the bottom edge of the editor | The selection grows; the view scrolls while the pointer is outside | |
| 2.4 | Click a line number; click and drag over several line numbers | The line(s) are selected | |
| 2.5 | Alt+click, then Alt+Shift+drag | A cursor is added; a column (box) selection is made | |

## 3. Multi-cursor

| # | Steps | Expected | Result |
|---|---|---|---|
| 3.1 | Click **Sample: multi-cursor**; the status line says 3 cursors. Type `X` | `X` is typed at the three cursors (the third replaces its selection) | |
| 3.2 | Ctrl+Alt+Down / Ctrl+Alt+Up from a single cursor | A cursor is added on the line below / above | |
| 3.3 | Backspace, Enter, Tab with the three cursors | Each cursor acts; the result matches VS Code | |

## 4. Clipboard (with Notepad)

| # | Steps | Expected | Result |
|---|---|---|---|
| 4.1 | Select two lines, Ctrl+C, paste into Notepad | The text is pasted with the line breaks | |
| 4.2 | Copy in Notepad, Ctrl+V in the Gallery, on one cursor and on three cursors (three lines copied) | One cursor: all the text. Three cursors with three lines: one line per cursor | |
| 4.3 | Ctrl+X on a selection, and with an empty selection | The selection / the whole line goes to the clipboard and is removed | |

## 5. Undo and redo

| # | Steps | Expected | Result |
|---|---|---|---|
| 5.1 | Type a word, press Ctrl+Z, then Ctrl+Y (or Ctrl+Shift+Z) | The word disappears, then comes back; the cursors are restored | |
| 5.2 | With three cursors, type a character, Ctrl+Z | The character disappears at the three cursors in one step | |

## 6. Read-only

| # | Steps | Expected | Result |
|---|---|---|---|
| 6.1 | Turn **Read-only** on; type, Backspace, paste, Ctrl+X | The text does not change | |
| 6.2 | Same state: arrows, mouse selection, Ctrl+C | The caret moves, text is selected and copied | |

## 7. IME — Japanese (Microsoft IME)

| # | Steps | Expected | Result |
|---|---|---|---|
| 7.1 | Type `nihongo`, Space, Enter | `日本語` once, caret after it; a thin underline while typing, a thick one on the converted segment, none after Enter | |
| 7.2 | Ctrl+Z after 7.1 | `日本語` is removed in one step | |
| 7.3 | Type `nihongo`, then Esc | The composition is cancelled and leaves no text | |
| 7.4 | Start a composition, then click elsewhere in the editor | The composition is committed once, not doubled | |
| 7.5 | Scroll with the wheel during a composition; move the window | The candidate window is not left at a stale position | |

## 8. IME — Chinese (Microsoft Pinyin)

| # | Steps | Expected | Result |
|---|---|---|---|
| 8.1 | Type `nihao`, choose `你好` with Space | `你好` is inserted once | |
| 8.2 | Type `zhongwen`, Enter | The pinyin letters are inserted as typed | |

## 9. IME — Korean

| # | Steps | Expected | Result |
|---|---|---|---|
| 9.1 | Type `안녕` (`dkssud`) | The syllables commit correctly; a new composition can start right after the previous one | |

## 10. Emoji panel

| # | Steps | Expected | Result |
|---|---|---|---|
| 10.1 | Win + `.`, pick an emoji | The emoji is inserted at the cursor | |
| 10.2 | Same with three cursors | The emoji is inserted at every cursor | |
| 10.3 | With three cursors, a Japanese composition | It is typed at the three cursors | |

## 11. Display scales

| # | Steps | Expected | Result |
|---|---|---|---|
| 11.1 | At 100 % scale: run section 7 (candidate window under the composed text) | Correct place | |
| 11.2 | At 150 % scale: same | Correct place | |
| 11.3 | Two monitors with different scales: drag the window from one to the other during a composition, then continue | The candidate window follows | |

## 12. Native AOT build

| # | Steps | Expected | Result |
|---|---|---|---|
| 12.1 | Run sections 1 to 11 on the published `LoomEditor.Gallery.exe` | The same results as the Debug build; no crash | |
| 12.2 | Windows 10, if available | The editor starts and plain typing works; there is no IME (one debug line says so) | |

## Phase 3 "done when"

Rows 7.1 and 8.1 (Japanese and Chinese IME input works) are the phase 3 acceptance. Record their result here and in the
P3-54 card. If one fails, set P3-54 to `blocked` in `docs/tasks/README.md` and open a follow-up card.
