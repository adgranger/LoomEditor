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

## 3. UIA text pattern (read)

Tools: Accessibility Insights for Windows (Live inspect) or `inspect.exe` from the Windows SDK, on the Gallery with a C# sample.

| # | Steps | Expected | Result |
|---|---|---|---|
| 3.1 | Hover the editor | ControlType `Edit`, Name `Code editor`, patterns list contains **Text** and **Value** (and no error) | |
| 3.2 | Text pattern pane | `DocumentRange.GetText(-1)` equals the editor text; `SupportedTextSelection` = Multiple | |
| 3.3 | Select some text, press "Get selection" in the tool; then add a second cursor (`Ctrl+Alt+Down`) | The selected text; with two cursors two ranges | |
| 3.4 | `GetVisibleRanges`, scroll, repeat | The text on screen | |
| 3.5 | `RangeFromPoint` over a word | A degenerate range at that character | |
| 3.6 | `GetBoundingRectangles` of a selection, at 100 % and 200 % display scale | Rectangles that sit on the highlighted text | |
| 3.7 | `IsReadOnly = true`, then `false` | Value pattern `IsReadOnly` is `true` and `SetValue` fails; then `SetValue("abc")` replaces the text | |
| 3.8 | `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64`, repeat 3.1 – 3.3 | Same result under Native AOT (no crash) | |
