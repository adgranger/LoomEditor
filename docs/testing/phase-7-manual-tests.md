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
