# Phase 15 — manual test list

What the headless Avalonia tests cannot check: the control seen from an app on each operating system, real fonts, real DPI changes, real IME windows and
screen readers. Fill the **Result** line of each check (`pass` / `fail` + a note, or `pending`). Agents write these checks; they do not run them.

**Machines:** Windows 11, macOS 15 or later, Ubuntu 24.04.

**Gallery setup:** run the Avalonia Gallery (`samples/LoomEditor.Gallery.Avalonia`, P15-31) on each machine:
`dotnet run --project samples/LoomEditor.Gallery.Avalonia -c Release` (or the Native AOT exe once P15-32 publishes it). The main window has the editor with the
language, theme, font, display and accessibility options, the document samples and "Open file…"; "Diff editor window" and "Host API demo" open the two other
windows; the "Language server" section drives the LSP demo. The `--text-selfcheck=<file>`, `--diff-selfcheck=<file>`, `--comments-selfcheck=<file>`,
`--hostapi-selfcheck=<file>` and `--lsp-selfcheck=<file>` flags write a PASS / FAIL report and exit with 0 or 1 (`--uia-selfcheck` prints that it is not available).

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

## P15-14 — Accessibility and high contrast

1. Windows, Narrator and NVDA: tab into the editor: it is read as an edit control named "Code editor" (or the host's `AutomationProperties.Name`) with its text as value.
   **Result:** pending
2. Windows: with a screen reader running, the editor behaves as the WinUI control in screen reader mode; without one, `AccessibilitySupport = Auto` stays off.
   **Result:** pending
3. All: Ctrl+M announces "Pressing Tab will now move focus to the next focusable element"; Tab then moves the focus out of the editor; Ctrl+M again restores Tab.
   **Result:** pending
4. All: the Insert key announces "Overtype mode" / "Insert mode"; Find with no match announces "No results".
   **Result:** pending
5. macOS, VoiceOver; Ubuntu, Orca: the editor is found and its value read; the announcements of 3 and 4 are spoken (as far as Avalonia supports live regions there).
   **Result:** pending
6. Windows high contrast (Aquatic, Desert…) and macOS "Increase contrast": the editor switches to the high contrast theme live and back when it is turned off; `UseSystemHighContrast = false` keeps the app theme.
   **Result:** pending

## P15-17 — Find and replace widget

1. All: Ctrl+F (Cmd+F on macOS) opens the widget at the top right with the focus in the find box; Ctrl+H (Cmd+Option+F on macOS) also shows the replace row; Escape closes it and the editor has the focus.
   **Result:** pending
2. All: type a word that occurs several times: the matches are highlighted in the text and the label reads "1 of N"; Enter / Shift+Enter move to the next / previous match and the editor scrolls to it.
   **Result:** pending
3. All: the match case, whole word and regex toggles (and Alt+C, Alt+W, Alt+R) change the matches live; an invalid regex shows "No results" in red without an error dialog.
   **Result:** pending
4. All: Replace and Replace All change the text; the icons of the buttons are drawn crisp (no missing glyph box), also on a 200 % display.
   **Result:** pending
5. All: the widget colors follow the editor theme (dark and light); the text boxes show the focus border color while focused, the toggles show the active colors when on.
   **Result:** pending
6. All: clicking and dragging inside the widget never moves the editor caret or starts a selection in the text.
   **Result:** pending

## P15-18 — Markdown rendering and the hover widget

1. All: hover a symbol that has a hover provider (Gallery C# sample): after about 300 ms the popup shows under the word, or above it when there is no room below; headings, bold, italic, inline code, code blocks and lists look like the WinUI control.
   **Result:** pending
2. All: move the pointer into the popup: it stays; move it out: it goes. Text in the popup can be selected with the mouse.
   **Result:** pending
3. All: an error marker shows its message with the `source(code)` detail dimmed; the popup colors follow the editor theme in dark and light.
   **Result:** pending
4. Windows, macOS, Ubuntu: the prose font falls back to the system font where Segoe UI is missing and no text is clipped.
   **Result:** pending

## P15-19 — The suggest widget

1. All: Ctrl+Space (also while typing an identifier with a completion provider) shows the list under the word, or above it when there is no room; the matched letters of each label are highlighted.
   **Result:** pending
2. All: Up / Down move the selection and scroll the list; Enter and Tab insert the item; Escape closes the list; the editor keeps the keyboard focus the whole time (the caret keeps blinking).
   **Result:** pending
3. All: clicking a row inserts it; the mouse wheel over the list scrolls it by three rows.
   **Result:** pending
4. All: the detail of the selected item shows under the list; deprecated items are struck through; the colors follow the editor theme in dark and light.
   **Result:** pending

## P15-20 — Comment threads: gutter "+"

1. All: with `CommentGutterButtonEnabled = true`, hover the line numbers: a 22 px "+" appears on the hovered line, centered on it, and follows the pointer from line to line; moving the pointer into the text hides it.
   **Result:** pending
2. All: clicking the "+" raises `AddCommentRequested` for that line (the Gallery logs it); with a selection, hovering a selected line asks for the selected lines or text.
   **Result:** pending
3. All: the "+" has no focus ring and is skipped by Tab; a screen reader reads "Add a comment on line N".
   **Result:** pending
4. All: the "+" colors follow `button.background` / `button.foreground` of the theme, and the accent look when the theme has none.
   **Result:** pending
5. All: scroll with the mouse wheel while the pointer rests on the line numbers: the "+" follows the line under the pointer.
   **Result:** pending

## P15-22 — Comment composer, toolbar and suggestions

1. All: in a comment thread click Reply: the box opens with the focus in the text field; typing, selecting and the context menu (copy, paste) work; Ctrl+Enter (Cmd+Enter on macOS) submits, Escape on an empty box cancels, Escape on a non-empty box does nothing.
   **Result:** pending
2. All: the toolbar buttons wrap the selection (Bold, Italic, Link...); Ctrl+B / Ctrl+I / Ctrl+K do the same (Cmd on macOS); the arrow keys, Home and End move between the buttons and Tab leaves the toolbar after one stop.
   **Result:** pending
3. Windows, macOS, Ubuntu: IME composition in the comment box (Japanese or Chinese): the preedit text shows inline, the candidate window sits at the caret, the committed text lands once, and Ctrl+Enter during a composition does not submit.
   **Result:** pending
4. All: Preview shows the rendered draft, a suggestion block with the diff and no Apply button; switching back keeps the selection and the text.
   **Result:** pending
5. All: a posted comment with a suggestion block shows "Suggested change" with - / + rows and an Apply button; Apply replaces the line, a changed line shows the stale tooltip, a read-only editor disables the button.
   **Result:** pending

## P15-23 — Comment zones

1. All: `AddCommentThread(line)` opens a box between that line and the next; the lines below move down, and removing the thread closes the box and moves them back.
   **Result:** pending
2. All: adding a comment, a reply box or a long Markdown comment grows the box to fit its content; resizing the editor (a narrower width wraps the text) changes the height to match, without flicker or a gap.
   **Result:** pending
3. All: scroll the thread half out of the editor: the box is clipped at the editor edge and does not draw over the line numbers or the scroll bar.
   **Result:** pending
4. Windows (Narrator), macOS (VoiceOver), Ubuntu (Orca): the thread is read as a group named "Comment thread on line N", and its comments are inside it.
   **Result:** pending
5. All: `AddCommentThread(line, startComposing: true)` puts the focus in the comment box; submitting or cancelling returns the focus to the editor with the caret on that line.
   **Result:** pending

## P15-24 — LoomDiffEditor shell, layout and sash

1. All: `<loom:LoomDiffEditor OriginalText="..." ModifiedText="..." />` in a window wider than 900 DIPs shows two editors side by side with a thin line between them; inserted lines are tinted green in the modified side, deleted lines red in the original side.
   **Result:** pending
2. All: drag the sash with the mouse: the original side grows or shrinks and the cursor is a west-east arrow; each side keeps at least 100 DIPs; double-click the sash: it returns to the middle.
   **Result:** pending
3. All: narrow the window below 900 DIPs: the original side and the sash disappear (inline fallback, the deleted lines come with P15-26); widen it again: they come back at the dragged position.
   **Result:** pending
4. All: type in the modified side: `ModifiedText` follows, and the decorations update after a short delay (`DiffChanged`).
   **Result:** pending

## P15-28 — Accessible diff viewer

1. Windows (Narrator), macOS (VoiceOver), Ubuntu (Orca): in the diff editor press F7: the viewer opens over the editors and the first changed row is focused and read; Shift+F7 opens it on the previous group.
   **Result:** pending
2. Same: Down and Up move one row and read it (line numbers, sign, text, and "group N of M"); with F7 again the next group opens; Space or Enter reveals the row in the editor behind.
   **Result:** pending
3. Same: Escape (or the Close button) closes the viewer and puts the focus in the modified editor; while it is open the two editors are not read by the screen reader.
   **Result:** pending
4. All: the rows use the editor font and the inserted and removed line colors of the theme.
   **Result:** pending

## P15-30 — Diff line checkboxes and the line action

1. All: with `LineSelectionEnabled = true` in a side-by-side diff, every deleted line (original side) and added line (modified side) has a checkbox in the glyph margin; a click toggles it and `SelectedLines` follows.
   **Result:** pending
2. Windows, Ubuntu: Ctrl+click a checkbox selects the line and its partner on the other side; macOS: Cmd+click does. Shift+click adds the changed lines from the last clicked one on the same side.
   **Result:** pending
3. All: with `LineActionGlyph` set (and a `GlyphFontFamily` that has the glyph), hovering a changed line shows the line action button in its lane; clicking raises `LineActionInvoked` with the selection, or the line.
   **Result:** pending
4. All: with the modified side read-only and focused, pressing `LineActionKey` (default S, no modifier) raises `LineActionInvoked`; in an editable side the key types the letter.
   **Result:** pending
5. All: the checkboxes and buttons use the `button.background` colors of the theme, and are skipped by Tab; a screen reader reads "Select line N" for a checkbox.
   **Result:** pending
