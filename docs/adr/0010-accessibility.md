# ADR 0010 — Accessibility: UI Automation text pattern, screen reader mode, announcements, high contrast

- **Status:** accepted
- **Date:** 2026-10-23

## Context

Phase 7 makes the editor usable with Narrator and NVDA (roadmap: `ITextProvider` / `ITextRangeProvider`, caret and
selection events, live regions, high contrast, manual test plan). Monaco has no UI Automation: it mirrors the text into a
hidden `<textarea>` (`editContext/textArea/`, `editContext/native/screenReaderContent*.ts`) or an ARIA live region
and lets the browser expose it. A WinUI control has no such mirror: it must implement the UIA **text pattern** itself.
What Monaco does have, and what is portable, is the **screen reader mode** (`accessibilitySupport`: wrapping that matches
what the screen reader reads), the **announcements** (`alert(...)` for find results, word highlight navigation, tab focus
mode), the **tab focus mode** (`Ctrl+M`) and the **high contrast** behavior of the selection (`editor.selectionForeground`).

Three problems need a decision:

1. The UIA glue (`ITextProvider`, `ITextRangeProvider`, events) lives in `LoomEditor.WinUI` and cannot be unit tested
   without a UI host (there is no WinUI test project, and a UIA client in a test host is out of proportion).
2. UIA ranges, units (character, word, line, paragraph, page, document) and their `Move` semantics are **not** in Monaco:
   there is nothing to port, so the behavior must be specified here and tested on its own.
3. Narrator and NVDA read the caret through `ITextProvider.GetSelection` + `ExpandToEnclosingUnit` + `GetText`, and
   need events when the caret or the text changes.

## Decisions

1. **All the logic goes to `LoomEditor.View/Accessibility/`; WinUI only adapts.** The text range model
   (`AutomationTextDocument`, `AutomationTextRange`, units, `Move`, `FindText`, geometry) is plain C# over
   `ViewModel` / `TextModel`, tested in `LoomEditor.View.Tests`. `LoomEditor.WinUI/Accessibility/` holds the peer and the range provider
   classes, which **only** convert types and call the View model (no logic, no branches beyond argument checks). The
   architecture rules do not change (`WinUI → Rendering → View → Core`); `PlatformNeutralSourceTests` keeps View free of Windows APIs.

2. **No WinUI unit tests; an in-process UIA self-check instead.** The peer is exercised by a button of the Gallery
   (P7-24) that gets the peer with `FrameworkElementAutomationPeer.FromElement`, reads `ITextProvider` through
   `GetPattern(PatternInterface.Text)` and asserts the contract (document text, selection, unit navigation). The maintainer
   also runs the manual plan with Narrator, NVDA and Accessibility Insights for Windows (P7-25).

3. **The UIA document is the model text, positions are model positions.** `GetText` joins the model lines with `\n`
   regardless of the model's EOL (Monaco: `EndOfLinePreference.LF` in `screenReaderUtils.ts`). A range is a pair of model
   `Position`s, never an offset, so CRLF models need no special case. Text hidden by folding is part of the document (Monaco's
   screen reader content also reads the model); injected text is **not** part of it (it is not in the model). Ranges are
   snapshots: a range that no longer fits the model after an edit is clamped when it is next used (`AutomationTextDocument.Validate`).

4. **Units and `Move` are specified in `docs/tasks/phase-7/README.md` §4** (UIA text units, "starts" of a unit, expand and
   move rules). The rules were taken from the UIA documentation and from the behavior Narrator and NVDA rely on; where the
   documentation is silent, §4 decides. Character = grapheme (`Strings.NextCharLength`, ADR 0006 decision 7), word = Monaco's
   `WordCharacterClassifier` runs plus trailing white space, line = **view line** (a wrapped line is several), paragraph = model
   line, page = the visible view lines, document = everything.

5. **Selection support is `Multiple`.** The editor has multi-cursor (phase 5): `ITextProvider.GetSelection` returns one range
   per selection, primary first. `ITextProvider2.GetCaretRange` returns the primary cursor as a degenerate range.

6. **Events are raised only when somebody listens and at most once per tick.** `AutomationEventCoalescer` (View) merges
   selection and text changes through `IEditorScheduler` (delay 0) and calls an `IAutomationEventSink`; the control's sink calls
   `AutomationPeer.RaiseAutomationEvent(TextPatternOnTextSelectionChanged / TextPatternOnTextChanged)` only when
   `AutomationPeer.ListenerExists(...)` is true. Focus events come from WinUI.

7. **"Live regions" are UIA notifications.** The editor is one UIA element, so there is no live-region child.
   Announcements go through `IEditorAnnouncer` (internal, in `EditorContext`, default: does nothing); the control implements it
   with `AutomationPeer.RaiseNotificationEvent(AutomationNotificationKind.Other, AutomationNotificationProcessing.ImportantMostRecent, text, activityId)`.
   The texts are Monaco's (`findWidget.ts` `_getAriaLabel`, `wordHighlighter.ts` `moveNext`, `toggleTabFocusMode.ts`), in English, not localized.
   A feature announces only when the editor is in screen reader mode **or** a UIA client listens (the control's announcer decides; the
   features always call it).

8. **Screen reader mode is Monaco's `accessibilitySupport`.** `EditorAccessibilityOptions` has the setting (`Unknown` = auto,
   `Enabled`, `Disabled`) and the detected state. The control detects a screen reader with `SystemParametersInfo(SPI_GETSCREENREADER)`
   (Narrator and NVDA both set it) when the setting is auto. When enabled: wrapping indent is `None` and a model dominated by
   long lines wraps at the viewport (Monaco: `editorOptions.ts` 2983 and 3098). Not ported: `wrappingStrategy: advanced` (DOM only),
   `accessibilityPageSize` and the paged screen reader strategy (they exist because the browser needs a bounded textarea; UIA
   ranges are virtual), `allowVariableFontsInAccessibilityMode`, the help dialog (`accessibilityHelp`). The `accessibilityModeEnabled` key of the
   keybinding context stays `false`: Monaco uses it to *disable* the standard Ctrl+Arrow word moves because, in a browser, the screen reader's own
   textarea handling takes over; LoomEditor has no such fallback, so the standard word moves stay on and the `cursorWordAccessibility*` commands
   (already ported, not bound in Monaco's editor either) stay unbound. The manual plan checks word navigation with Narrator and NVDA (P7-25).

9. **Tab focus mode is Monaco's** (`toggleTabFocusMode.ts`, `tabFocus.ts`): `Ctrl+M` toggles whether `Tab` inserts a tab or moves
   the focus out of the editor (a keyboard trap otherwise). The state is per editor (Monaco's is global), exposed as the public
   `TabMovesFocus` property.

10. **High contrast has two layers.** (a) The two bundled VS Code themes `hc_black` / `hc_light` already exist and are given the
    behavior Monaco gives them: the selected text takes `editor.selectionForeground` (`viewLine.ts` 128-141). (b) When Windows high
    contrast is on (`AccessibilitySettings.HighContrast`), the control builds a theme from the **user's system colors** read with Win32 `GetSysColor`
    (`SystemHighContrastThemeFactory` in `LoomEditor.Themes`, pure, tested with sample palettes), because that is what the user chose and tuned.
    The control property `UseSystemHighContrast` (default `true`) switches it off. Contrast ratios (WCAG relative luminance, which `Color.GetRelativeLuminance` already computes)
    are asserted in tests.

11. **WinUI trade-offs that cannot be checked by tests** (the cards say how to verify them): the peer must implement
    `ITextProvider` / `ITextProvider2` / `IValueProvider` and return `ITextRangeProvider` objects under Native AOT (CsWinRT
    generates the marshalling for managed implementations of WinRT interfaces: the classes are `partial`); `PatternInterface.Text2`
    must be accepted by `GetPatternCore`; `ProviderFromPeer(this)` gives the `IRawElementProviderSimple` returned by `GetEnclosingElement`.
    P7-17 is a tracer bullet for all three and publishes with Native AOT before any further WinUI card builds on it.

## Not in phase 7

Accessibility help dialog and verbosity settings, `ariaLabel` / `ariaRole` options (the control's `AutomationProperties.Name` is used),
reduced motion (`UISettings.AnimationsEnabled` → smooth scrolling and caret blinking), UIA text edit pattern (`ITextEditProvider`),
annotations, per-token text attributes (colors and font styles are reported as uniform), a UI Automation client based test, localization of
announcements, the accessible view of the diff editor.
