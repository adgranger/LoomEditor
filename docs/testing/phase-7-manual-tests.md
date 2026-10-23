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

## 4. UIA text pattern (navigate)

Tools: Accessibility Insights text pattern explorer, or the Gallery self-check of P7-24 once it exists.

| # | Steps | Expected | Result |
|---|---|---|---|
| 4.1 | Degenerate range at a word, `ExpandToEnclosingUnit(Word)`, `GetText` | The word and its trailing spaces | |
| 4.2 | `Move(Word, 1)` and `Move(Word, -1)` | They walk the words | |
| 4.3 | `Move(Line, 1)` on a wrapped line (word wrap on) | The next **view** line | |
| 4.4 | `Move(Paragraph, 1)` | The next model line | |
| 4.5 | `FindText("foo", false, true)`, then `Select` it | The range is returned and the selection appears | |
| 4.6 | `Select` on a range | The caret moves and the editor does not lose the focus | |
| 4.7 | `AddToSelection` on a range, then `RemoveFromSelection` on it | A second cursor appears, then goes away | |
| 4.8 | `ScrollIntoView(true)` on a range far below | It becomes the first visible line | |
| 4.9 | `MoveEndpointByUnit(End, Word, 1)` on a selection | The selection grows by a word | |

## 5. Events and announcements

Tools: Accessibility Insights → Events, Narrator (Win+Ctrl+Enter), NVDA.

| # | Steps | Expected | Result |
|---|---|---|---|
| 5.1 | Accessibility Insights → Events, listen to the editor: `Right`; type a letter; hold `Right` | `TextSelectionChanged`; `TextChanged` then `TextSelectionChanged`; the editor does not freeze | |
| 5.2 | Narrator on, arrow keys | It reads the character (`Right`/`Left`), the word (`Ctrl+Right`/`Ctrl+Left`) and the line (`Down`/`Up`), the **view** line when word wrap is on | |
| 5.3 | Narrator on, `Ctrl+F`, type a word present in the text; then an absent word; `F3` without matches | "N of M found for 'word'" (line text before it); "No results found for 'word'"; the message is repeated | |
| 5.4 | Narrator on, cursor on a word that occurs twice, `F7` | "line text, 2 of 2 for 'word'" | |
| 5.5 | Narrator on, `Ctrl+M` twice | The two tab focus mode messages are spoken | |
| 5.6 | Narrator off, no other client: repeat the actions above | No exception, normal speed (the peer does not exist) | |
| 5.7 | `ITextProvider2.GetCaretRange` in Accessibility Insights | `isActive` is `true` with the focus, `false` after clicking another control; the range is at the caret | |
| 5.8 | Repeat 5.1 – 5.5 on the Native AOT build | Same results | |

## 6. High contrast

Windows 11: Settings → Accessibility → Contrast themes; `Left Alt + Left Shift + Print Screen` toggles the last high contrast theme.

| # | Steps | Expected | Result |
|---|---|---|---|
| 6.1 | Gallery open with a C# sample and a selection; turn on **Aquatic** | Within a second the editor background and text become the scheme's, the selection uses the scheme's highlight colors with readable text, no restart needed | |
| 6.2 | Repeat with **Desert**, **Dusk**, **Night sky** | Text, selection, line numbers readable, caret visible, the active line is outlined | |
| 6.3 | Turn high contrast off | The editor returns to its `Theme` (Dark Modern by default) | |
| 6.4 | `UseSystemHighContrast = false` (from code) with high contrast on; then back to `true` | The editor keeps its `Theme`; then the scheme's colors | |
| 6.5 | Open the find widget (`Ctrl+F`) in a high contrast scheme | Input, buttons, toggles and result count are readable; a toggled option (match case) is distinguishable from an untoggled one | |
| 6.6 | Look at scrollbars, minimap, sticky scroll lines and folding chevrons in each scheme | All visible (note any that vanish) | |
| 6.7 | `Theme = EditorTheme.BuiltIn[8]` ("Dark High Contrast") with system high contrast **off** | Black background, white text, black text on the white selection | |
| 6.8 | Native AOT build: repeat 6.1 and 6.3 | Same results | |

## 7. Gallery self-check

The Gallery runs the 19 UI Automation checks in its own process (**UIA self-check** button, `UiaSelfCheck.cs`).

| # | Steps | Expected | Result |
|---|---|---|---|
| 7.1 | Click **UIA self-check** with the editor visible | "19 of 19 passed" | 19 of 19 passed (Release build) |
| 7.2 | Same on the Native AOT published build (README §6) | "19 of 19 passed" | 19 of 19 passed (`dotnet publish -r win-x64 -p:PublishAot=true -warnaserror`) |
| 7.3 | **Long-line sample** + **Screen reader mode = On** | The 12 000-character line wraps; **Off**: it does not; **Auto** with Narrator running: it wraps | |
| 7.4 | Toggle **Tab moves focus**, and press `Ctrl+M` in the editor | The switch follows `Ctrl+M` | |
| 7.5 | **Follow system high contrast** off with a contrast theme on | The editor keeps its theme | |
