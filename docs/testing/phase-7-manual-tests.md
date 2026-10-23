# Phase 7 — manual test list

What unit tests cannot check: screen reader behavior under a real UI (Narrator, NVDA), and the Native AOT build.
Run it on the **Native AOT** build of the Gallery, on Windows 11:

```powershell
dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64
```

and fill the **Result** column (`pass` / `fail` + a note).

- Windows build:
- Date and tester:

## 1. Screen reader mode

Until the Gallery's "Long-line sample" button exists (P7-24), set `Editor.Text` from code to one line of 12 000 characters
of `"lorem "` repeated, plus a few short lines.

| # | Steps | Expected | Result |
|---|---|---|---|
| 1.1 | `AccessibilitySupport = On`, a long line, word wrap off | The line wraps at the window width; with `Off` it does not | |
| 1.2 | `AccessibilitySupport = On`, `WrappingIndent = Indent`, `WordWrap = On` | Continuation lines are not indented; with `Off` they are | |
| 1.3 | `Auto` and Narrator started (Win+Ctrl+Enter), then click into the editor | Behaves like `On`; after stopping Narrator and clicking out and in again, like `Off` | |

## 2. Tab focus mode

| # | Steps | Expected | Result |
|---|---|---|---|
| 2.1 | Click in the editor, press `Tab` | A tab is inserted | |
| 2.2 | Press `Ctrl+M`, then `Tab` | The focus leaves the editor (to the next control of the Gallery) and nothing is inserted | |
| 2.3 | Press `Shift+Tab` | The focus comes back to the editor | |
| 2.4 | Narrator on, `Ctrl+M` | Narrator says "Pressing Tab will now move focus to the next focusable element" | |
| 2.5 | `Ctrl+M` again | Narrator says "Pressing Tab will now insert the tab character" | |
| 2.6 | Set `TabMovesFocus = true` from code | Behaves like 2.2 | |
