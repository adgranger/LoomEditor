# P7-24 — Gallery: accessibility samples and the in-process UIA self-check

- **Status:** todo
- **Depends on:** P7-23
- **Size:** M
- **Recommended model:** Sonnet; Opus to diagnose any failing self-check (bug in the providers vs. error in the README §4 spec)

## Goal

The Gallery has the switches for every phase 7 option, a long-line sample for screen reader mode, and a **"UIA self-check"** button that exercises the editor's UI Automation providers from inside the process (ADR 0010 decision 2) and prints
pass / fail per check, on the normal and the Native AOT build.

## Sources

None to port. WinUI API: `FrameworkElementAutomationPeer.CreatePeerForElement`, `AutomationPeer.GetPattern`, `PatternInterface`, `ITextProvider`, `ITextProvider2`, `ITextRangeProvider`, `IValueProvider`, `TextUnit`, `TextPatternRangeEndpoint`.

## Files to create or change

- `samples/LoomEditor.Gallery/UiaSelfCheck.cs` — create
- `samples/LoomEditor.Gallery/MainWindow.xaml` — change: header text "phase 7: accessibility"; a new `StackPanel` row with the controls below; a `TextBlock x:Name="SelfCheckText"` (monospace, selectable) for the results
- `samples/LoomEditor.Gallery/MainWindow.xaml.cs` — change: the handlers
- `docs/testing/phase-7-manual-tests.md` — change: section 7 below

## Design decisions (already made — do not change)

- **Controls** (one new row, `Grid.Row` shifted like the other rows were; read `MainWindow.xaml` first and follow its style): a `ComboBox x:Name="ScreenReaderBox"` "Screen reader mode" (`Auto`, `On`, `Off` → `Editor.AccessibilitySupport`),
  a `ToggleSwitch x:Name="TabFocusSwitch"` "Tab moves focus" (`Editor.TabMovesFocus`; also updated when the user presses `Ctrl+M`: subscribe to the property with `RegisterPropertyChangedCallback(LoomEditor.TabMovesFocusProperty, …)`),
  a `ToggleSwitch x:Name="SystemHighContrastSwitch"` "Follow system high contrast" (`IsOn="True"`, `Editor.UseSystemHighContrast`), a `Button` "Long-line sample" and a `Button` "UIA self-check". No new `FeatureSamples` entry.
- **"Long-line sample" button**: loads one line of 12 000 characters (the word `lorem` followed by a space, repeated, built with `string.Concat(Enumerable.Repeat("lorem ", 2000))`) followed by 20 short lines `"line N"`. This text is "dominated by long lines"
  (a line of at least 10 000 characters holding more than half of the characters), which is what screen reader mode needs to force wrapping. Set `Editor.LanguageId = "plaintext"` and `Editor.WordWrap = Off` (and `WordWrapSwitch.IsOn = false`) when loading it.
- **`UiaSelfCheck.Run(LoomEditor editor)`** (`internal static`): returns `IReadOnlyList<(string Name, bool Passed, string Detail)>`. It must **not throw**: each check is wrapped so that an exception becomes a failed check with the exception message. It sets `editor.Text = "one two\nthree four\nfive"`,
  `editor.SetSelections([new Selection(1, 1, 1, 4)])`, gets `AutomationPeer peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor)` and runs, in this order (each check a lambda):
  1. `Peer_IsEditControl`: `peer.GetAutomationControlType() == AutomationControlType.Edit`
  2. `Patterns_Text_Value_Text2`: `GetPattern(PatternInterface.Text) is ITextProvider`, `Value` is `IValueProvider`, `TextPattern2` is `ITextProvider2`
  3. `Document_Text`: `DocumentRange.GetText(-1) == "one two\nthree four\nfive"`
  4. `Selection_IsOne`: `GetSelection()` has one range with text `"one"`
  5. `Expand_Word`: a clone of the selection range, `ExpandToEnclosingUnit(TextUnit.Word)` → text `"one "`
  6. `Move_Word`: `Move(TextUnit.Word, 1)` returns `1` and the text is `"two\n"`
  7. `Expand_Line_Paragraph`: on a clone of the document range `ExpandToEnclosingUnit(TextUnit.Line)` → `"one two\n"`; `Move(TextUnit.Paragraph, 1)` returns `1` → `"three four\n"`
  8. `MoveEndpointByUnit`: on a clone, `MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Word, 1)` returns `1` and grows the range by one word
  9. `Compare`: `Clone().Compare(original)` is true; `CompareEndpoints(Start, original, Start) == 0`
  10. `FindText`: on the document range `FindText("four", false, true)` → text `"four"`; `FindText("zzz", false, true)` → `null`
  11. `Select_MovesTheCaret`: the `"four"` range's `Select()` → `editor.Selections[0]` is `Selection(2, 7, 2, 11)`
  12. `AddToSelection`: after `AddToSelection()` of a `"five"` range, `editor.Selections.Count == 2`
  13. `RemoveFromSelection`: after removing it, `Count == 1`
  14. `GetVisibleRanges`: not empty
  15. `Bounding_Rectangles`: `GetBoundingRectangles()` of the document range has a length that is a non-zero multiple of 4 **if the editor is loaded and visible** (else the check is reported as passed with detail "skipped: not in a window")
  16. `RangeFromPoint`: with the first rectangle of check 15, `RangeFromPoint(new Point(x + 1, y + 1))` returns a range (not null)
  17. `Value_Pattern`: `Value == editor.Model.GetValue(EndOfLinePreference.LF)`; `IsReadOnly == editor.IsReadOnly`
  18. `Caret_Range`: `GetCaretRange(out bool active)` returns a degenerate range (`GetText(-1) == ""`)
  19. `ScrollIntoView_DoesNotThrow`
  It restores the previous text and selections at the end (`finally`).
- The button handler runs `UiaSelfCheck.Run(Editor)` and writes one line per check to `SelfCheckText` (`PASS  Name — Detail` / `FAIL  Name — Detail`) plus a summary `"N of M passed"`; the `StatusText` shows the summary too.
- Internals: the Gallery already has `InternalsVisibleTo` for Core and View; `Selection` and `Position` are public. The self-check uses only public WinUI and Core types.
- No change to the editor, the model or the tests.

## Tests

Manual (`docs/testing/phase-7-manual-tests.md` section 7 "Gallery self-check", result column; Native AOT build, README §6):

- [ ] 7.1 Click **UIA self-check** with the editor visible → every check passes ("19 of 19 passed")
- [ ] 7.2 Same on the Native AOT published build → "19 of 19 passed"; any failure is copied into the commit body and the card stays `in progress`
- [ ] 7.3 **Long-line sample** + **Screen reader mode = On** → the 12 000-character line wraps; **Off** → it does not; **Auto** with Narrator running → wraps
- [ ] 7.4 **Tab moves focus** switch follows `Ctrl+M`
- [ ] 7.5 **Follow system high contrast** off with a contrast theme on → the editor keeps its theme

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` (README §6) — result reported in the commit body, with the self-check result
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

An automated run of the self-check in CI, a UIA **client** based test, any change to the providers (a failing check is fixed in a follow-up card, not here).

## Suggested commit message

`feat(gallery): add the accessibility switches and the UI Automation self-check`
