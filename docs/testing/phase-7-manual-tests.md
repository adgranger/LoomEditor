# Phase 7 — manual test list

What unit tests cannot check: screen reader behavior under a real UI (Narrator, NVDA, Accessibility Insights), and the Native AOT build.
Fill the **Result** column (`pass` / `fail` + a note) and the **Findings** table at the end.

- Windows build:
- Narrator version:
- NVDA version:
- Accessibility Insights for Windows version:
- Display scale:
- Date and tester:

## Setup

- Run the **Native AOT** build of the Gallery (README §6), on Windows 11, with a C# sample loaded:

  ```powershell
  dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64
  ```

- Narrator: `Win + Ctrl + Enter` toggles it (Settings → Accessibility → Narrator). The Narrator key is `Caps Lock` (or `Insert`).
- NVDA (free, <https://www.nvaccess.org/>): start it; `NVDA menu → Tools → Speech viewer` shows what is spoken.
- Accessibility Insights for Windows: <https://accessibilityinsights.io/> (FastPass, Live inspect, Events).
- Run section 8 with **Narrator only** and section 9 with **NVDA only** (never both at once). Speech is compared with the
  *Expected speech* column: the wording may differ slightly, judge by content and order.
- Unless a row says otherwise the editor holds `class Foo {\n    void Bar() {}\n}`.

## 1. Screen reader mode

Use the Gallery's **Long-line sample** button (one line of 12 000 characters plus a few short lines).

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

## 8. Narrator

Narrator on, Gallery focused. Rows about wrapping use **Word wrap** on.

| # | Steps | Expected speech | Result |
|---|---|---|---|
| 8.1 | Press `Tab` until the focus enters the editor | "Code editor, edit" (name and control type) | |
| 8.2 | Caret at the start, press `Right` | The next character ("l") | |
| 8.3 | Press `Down` | The next line, in full ("    void Bar() {}") | |
| 8.4 | Press `Ctrl+Right` | The next word | |
| 8.5 | Press `Home`, then `End` | The character at the new caret each time | |
| 8.6 | Press `Shift+Right` three times | "selected" followed by the selected text | |
| 8.7 | Type `x` | "x" | |
| 8.8 | Press `Backspace` | The deleted character | |
| 8.9 | Move onto an empty line (add one if needed) | "blank" | |
| 8.10 | Narrator "read current line" (`Caps+L`), then "read current word" (`Caps+K`) | The line, then the word at the caret | |
| 8.11 | Word wrap on, a long line (Gallery **Long-line sample**), press `Down` | The **visual** (wrapped) line, not the whole model line | |
| 8.12 | Press `Ctrl+F` | The find widget is announced; its controls are named ("Find", "Match Case", "Previous Match", "Next Match", …) | |
| 8.13 | In the find widget, type a word present, then an absent word, then press `F3` | "N of M found for 'word'" (line text before it); "No results found for 'word'"; the message is repeated (P7-13) | |
| 8.14 | Caret on a word that occurs twice, press `F7` | "line text, 2 of 2 for 'word'" (P7-14) | |
| 8.15 | Press `Ctrl+M` twice | "Pressing Tab will now move focus to the next focusable element", then "Pressing Tab will now insert the tab character" (P7-16) | |
| 8.16 | Press `Ctrl+Alt+Down` (second cursor) | Narrator does not crash and keeps reading the primary caret | |
| 8.17 | Type a word, press `Ctrl+Z` | The restored text is read | |
| 8.18 | Toggle scan mode (`Caps+Space`) on and off, then type | The editor receives the typing after leaving scan mode (the keyboard is not trapped) | |
| 8.19 | Narrator's caret browsing | Not required: note what happens, no pass/fail | |
| 8.20 | Fold `{ … }` of `Foo`, press `Down` over the folded line | Check and record: the folded lines are skipped by `Down`, but exist in the document text; hidden text is not read until expanded | |

## 9. NVDA

NVDA on, Narrator off, Gallery focused. Keep the speech viewer open.

| # | Steps | Expected speech | Result |
|---|---|---|---|
| 9.1 | `Tab` into the editor | "edit, multi line, Code editor" | |
| 9.2 | `Right` / `Left` | The character | |
| 9.3 | `Down` / `Up` | The line | |
| 9.4 | `Ctrl+Right` / `Ctrl+Left` | The word | |
| 9.5 | `NVDA+Up` (say line), then `NVDA+Down` (say all) | The current line; then everything from the caret to the end | |
| 9.6 | `Shift+Arrow` | Selection changes are announced ("selected", "unselected") | |
| 9.7 | Type a few words | Characters / words are echoed per NVDA's typing echo settings | |
| 9.8 | Review cursor (numpad 7/8/9, or the laptop layout): lines, words, characters | It reads the document text without moving the caret; it can reach all the text | |
| 9.9 | Find widget: present word, absent word, `F3` | As 8.13 (P7-13) | |
| 9.10 | `F7` on a repeated word | As 8.14 (P7-14) | |
| 9.11 | `Ctrl+M` twice | As 8.15 (P7-16) | |
| 9.12 | Word wrap on, long line, `Down` | The visual line | |
| 9.13 | Focus the editor | NVDA stays in focus mode: browse mode is not entered (it is an edit control) | |
| 9.14 | Hold `Right` for 3 seconds with the speech viewer open | No repeated or empty announcements (the coalescer, P7-15) | |
| 9.15 | `NVDA+F1` (developer info) | The Text pattern is listed as supported (or the UIA class is `LoomEditor`) | |

## 10. Accessibility Insights for Windows

| # | Steps | Expected | Result |
|---|---|---|---|
| 10.1 | FastPass on the Gallery window | No failure on the editor (expected issues: see "Known limits") | |
| 10.2 | Live inspect on the editor | Text, Value and TextPattern2 patterns present; `IsKeyboardFocusable` is true | |
| 10.3 | Events recorder, type in the editor and move the caret | Text changed and text selection changed events arrive (P7-19) | |
| 10.4 | Tab through the Gallery | Order: Gallery controls → editor → status | |
| 10.5 | Color contrast tool on the editor in Dark Modern, Light Modern and the two bundled high contrast themes | Record the foreground / background ratio of each; compare with the P7-20 test floors | |
| 10.6 | Keyboard only: reach and operate every Gallery control and the find widget; leave the editor with `Ctrl+M` then `Tab` | Everything is reachable and usable without a mouse | |

## 11. Regression

| # | Steps | Expected | Result |
|---|---|---|---|
| 11.1 | Screen reader **off**: re-run the phase 3 – 6 lists for typing, IME composition (Microsoft Japanese IME), the find widget and the minimap; on "Large C# (20 000 lines)" hold `Right` and scroll | Nothing slowed down; record the observed frame rate / lag | |
| 11.2 | Narrator **on**, "Large C# (20 000 lines)": scroll and type | No freeze; record the observed frame rate / lag | |

## Known limits

- Colors and font styles are reported as uniform, and there are no annotations (ADR 0010, "Not in phase 7").
- The screen reader mode (`AccessibilitySupport = Auto`) is detected when the editor gets the focus, not while it stays focused (P7-09).
- `Intl.Segmenter` words (CJK) are runs of characters, not dictionary words (P7-17).
- Hidden folded text belongs to the line before it in the UIA text (P7-17); row 8.20 records the real behavior — to verify.
- Injected text (inlay hints, ghost text) is not in the UIA text (P7-17).
- The standard `Ctrl+Arrow` word moves are used in screen reader mode; Monaco's `cursorWordAccessibility*` commands are not bound (P7-16).
- There is no accessibility help dialog and no verbosity settings (ADR 0010, "Not in phase 7").
- There is no reduced motion support: smooth scrolling and caret blinking ignore the Windows animation setting (ADR 0010, "Not in phase 7").
- There is no UIA text edit pattern (`ITextEditProvider`) (ADR 0010, "Not in phase 7").

## Findings

| # | Row | What happened | Severity | Follow-up card |
|---|---|---|---|---|
| | | | | |
