# Phase 15 — manual test list

What the headless Avalonia tests cannot check: the control seen from an app on each operating system, real fonts, real DPI changes, real IME windows and
screen readers. Fill the **Result** line of each check (`pass` / `fail` + a note, or `pending`). Agents write these checks; they do not run them.

**Machines:** Windows 11, macOS 15 or later, Ubuntu 24.04.

**Gallery setup:** run the Avalonia Gallery (P15-31) on each machine.

- Build (Debug / Release / Native AOT):
- Operating system and display scale:
- Date and tester:

## P15-08 — Template, draw loop, font, zoom

1. The Gallery shows a C# file in the editor; the text is drawn on the theme background and the first line is not clipped.
   **Result:** pending
2. The font is the default monospace of the operating system (Cascadia Mono / Consolas on Windows, Menlo on macOS, DejaVu Sans Mono on Linux).
   **Result:** pending
3. Move the window to a display with a 200 % scale (or change the scale in the system settings): the text is re-rendered and stays crisp.
   **Result:** pending
4. Change the font size and the zoom level from the Gallery controls: the line height follows and the text is not blurry.
   **Result:** pending

## P15-09 — Scrolling, caret, highlighting, read-only, reveal

1. Open a C# file of 200 lines or more: the vertical scroll bar appears (auto-hide on macOS and Linux), dragging it scrolls the text, and the text scrolls the bar.
   **Result:** pending
2. Mouse wheel: one notch scrolls about 3 lines; on a macOS trackpad the scroll is continuous and follows the fingers.
   **Result:** pending
3. Click in the editor: the caret blinks about twice per second and stops blinking while the window is in the background.
   **Result:** pending
4. Set the language to `csharp` and the theme to light, then to dark: the tokens are colored and the colors follow the theme.
   **Result:** pending
5. Turn `IsReadOnly` on: the caret does not blink as an edit caret would, and the text cannot be changed (once P15-11 is in).
   **Result:** pending

## P15-11 — Keyboard, macOS modifiers, clipboard

1. Type text, press Enter, Tab, Backspace, Delete and the arrow keys: each does what it does in VS Code.
   **Result:** pending
2. Ctrl+A, Ctrl+C, Ctrl+X, Ctrl+V (Cmd on macOS) select, copy, cut and paste; paste text copied from another application.
   **Result:** pending
3. On macOS the Control key does not act as Cmd: Ctrl+A moves to the start of the line (emacs style is not ported: nothing must happen) and Cmd+A selects all.
   **Result:** pending
4. Ctrl+Z and Ctrl+Shift+Z (Cmd on macOS) undo and redo; Ctrl+Tab moves the focus out of the editor, Tab does not.
   **Result:** pending
5. Type an accented character with a dead key (for example ^ then e) and an emoji from the system picker: the text is inserted once.
   **Result:** pending

## P15-12 — Pointer

1. Click in the text: the caret goes where the pointer is; double click selects a word, triple click the line; Shift+click extends the selection.
   **Result:** pending
2. Drag from the middle of a line to another line, then keep the button down below the last visible line: the editor scrolls and the selection follows.
   **Result:** pending
3. Alt+click adds a cursor (Option on macOS); Ctrl+Alt+drag (Cmd+Option on macOS) makes a column selection.
   **Result:** pending
4. Select some text, press inside the selection and drag: the text moves (Ctrl or Option copies); Escape while dragging cancels.
   **Result:** pending
5. The pointer is an I-beam over the text and an arrow over the gutter, the minimap and the scroll bar; click a line number selects the line.
   **Result:** pending
6. With `MouseWheelZoom` on, Ctrl+wheel (Cmd+wheel on macOS) zooms; a trackpad pinch does nothing.
   **Result:** pending

## P15-13 — IME

1. Windows, Microsoft Pinyin: type `nihao`, then Space: the pinyin is underlined while composing, the candidate window opens at the caret, and `你好` is inserted once.
   **Result:** pending
2. Windows, Microsoft Japanese IME: type `sennsei`, Space to convert, Enter to commit: `先生` is inserted once; Escape during a composition removes the composed text.
   **Result:** pending
3. macOS, Pinyin – Simplified and Japanese – Romaji: same as 1 and 2; the candidate window follows the caret after a scroll.
   **Result:** pending
4. macOS, dead key `⌥e` then `e`: `é` is inserted once (the accent is shown underlined in between).
   **Result:** pending
5. Ubuntu, IBus Pinyin or Fcitx5: same as 1.
   **Result:** pending
6. All: the emoji panel (Win+. on Windows, Ctrl+Cmd+Space on macOS, Ctrl+. on Ubuntu with IBus) inserts the emoji once at the caret.
   **Result:** pending
7. All: make the editor read-only (Gallery option) during a composition: the composition ends; typing with the IME afterwards changes nothing and goes to no other control.
   **Result:** pending
