# Phase 7 — read this before any card

Phase 7 brings the full UI Automation accessibility of the roadmap ([phase-7-accessibility.md](../../roadmap/phase-7-accessibility.md)):
the UIA text pattern (`ITextProvider` / `ITextRangeProvider`) so that Narrator and NVDA can read and navigate the text, caret and
selection events, announcements ("live regions") for important changes, the screen reader mode (accessibility-driven wrapping),
the tab focus mode, high contrast, and a manual test plan. The design is in [ADR 0010](../../adr/0010-accessibility.md): read it first.
This page lists the conventions that **every** phase 7 card relies on; a card never repeats them. The conventions of
[phase-3/README.md](../phase-3/README.md) (sections 1, 3, 4, 5 and 6), [phase-5/README.md](../phase-5/README.md) (sections 1–5) and
[phase-6/README.md](../phase-6/README.md) (sections 1 and 2) still apply.

## Order of the cards

Take the cards in table order of [../README.md](../README.md) (each card's "Depends on" is satisfied by the cards above it). Steps:

- **A — Screen reader mode** (P7-01 … P7-04): the options, the wrapping indent, long-line models, the control property and the screen reader detection.
- **B — The UIA text model** (P7-05 … P7-11): document, ranges, the seven units, expand / move, find, geometry. All in `LoomEditor.View`, all unit tested.
- **C — Announcements, events, tab focus** (P7-12 … P7-16): the announcer and Monaco's messages, find and word highlight announcements, the event coalescer, the tab focus mode.
- **D — The UIA peer** (P7-17 … P7-19): `ITextProvider`, `ITextRangeProvider`, `IValueProvider` (tracer bullet, publishes with Native AOT), the navigation members, events, notifications, `ITextProvider2`.
- **E — High contrast** (P7-20 … P7-23): contrast ratio, `editor.selectionForeground`, the system colors theme, the control follows Windows.
- **F — Prove it and close** (P7-24 … P7-26): Gallery switches and the in-process UIA self-check, the manual plan, close the phase.

P7-04, P7-16, P7-17, P7-18, P7-19, P7-23 and P7-24 have manual checks for the maintainer (`docs/testing/phase-7-manual-tests.md`). Steps A, B, C and E do not depend on each other except through the table order; D needs B and C.

## Out of scope for phase 7

Accessibility help dialog and verbosity settings; `ariaLabel` / `ariaRole` options; reduced motion (`UISettings.AnimationsEnabled`); the UIA text edit pattern and annotations; per-token text attributes (colors, bold: reported as uniform);
the `cursorWordAccessibility*` keybindings (ADR 0010 decision 8); localization of the announcements; a UIA client based automated test; the accessible view of the diff editor (phase 9); JAWS and Magnifier specific work.
Anything the manual plan finds is a follow-up card, not a change to a closed one.

## 1. Upstream sources

Monaco has no UI Automation (it mirrors the text into a hidden `<textarea>`). Cards that port something give the file and lines at
`microsoft/vscode` `0c9c0a4295a9222e12a605537738719455ae3056` (paths are full, `src/vs/…`; the function **name** is authoritative if a line
number looks off). Cards that have no Monaco counterpart say **"Sources: none (specified in §4)"**: for them this README is the spec,
and the tests are written by the card (`New:`), not ported. A C# file that ports (or is inspired by) a Monaco file starts with the two-line header of `CLAUDE.md`
("Ported from microsoft/vscode: <path>" / "Upstream commit: …"; the architecture test checks it); a file with no Monaco counterpart has no such header.

## 2. Where things go

| What | Where |
|---|---|
| Accessibility options, effective wrapping | `LoomEditor.View/Layout/` (`EditorAccessibilityOptions`, next to `EditorWrappingOptions`) |
| `TextModel.IsDominatedByLongLines` | `LoomEditor.Core/Model/TextModel.LongLines.cs` |
| UIA text model (document, ranges, units, move, find, geometry), announcer, event coalescer | `LoomEditor.View/Accessibility/` (namespace `LoomEditor.View.Accessibility`; **not** a feature, no `XxxFeature.cs`) |
| Tab focus mode | the feature `LoomEditor.View/Features/TabFocus/` (listed in `EditorFeatures.cs`, with a README) |
| High contrast themes and system colors | `LoomEditor.Themes/` (`HighContrast/` folder) |
| `editor.selectionForeground` drawing | `LoomEditor.Rendering/TextViewRenderer.cs` (the style overrides of a line) |
| UIA peer, range provider, event sink, announcer, screen reader and high contrast detection | `LoomEditor.WinUI/Accessibility/` (+ `Interop/NativeMethods.cs`) |
| Public properties (`AccessibilitySupport`, `TabMovesFocus`, `UseSystemHighContrast`) | `LoomEditor.WinUI/Controls/LoomEditor.Accessibility.cs` + `PublicAPI.Unshipped.txt` |
| Tests of the View classes | `tests/LoomEditor.View.Tests/Accessibility/` (mirrors `src/`) |

## 3. Monaco concepts → LoomEditor (new in phase 7)

| Monaco | LoomEditor |
|---|---|
| `accessibilitySupport` (`'auto' \| 'on' \| 'off'`), `AccessibilitySupport` (`Unknown`, `Disabled`, `Enabled`) | `EditorAccessibilityOptions` on `EditorLayoutOptions.Accessibility`, enum `AccessibilitySupport` (P7-01); control property `AccessibilitySupport` of type `EditorAccessibilitySupport` (`Auto`, `On`, `Off`) (P7-04) |
| `IAccessibilityService.isScreenReaderOptimized()` | `ScreenReaderDetector` (WinUI, P7-04): `SPI_GETSCREENREADER` |
| `textModel.isDominatedByLongLines()` | `TextModel.IsDominatedByLongLines()` (P7-02) |
| `alert(...)` of `base/browser/ui/aria/aria.ts` | `IEditorAnnouncer.Announce(string)` (P7-12) → UIA notification |
| `TabFocus` + `editor.action.toggleTabFocusMode` | `EditorContext.TabMovesFocus` + the `TabFocus` feature (P7-16) |
| `editor.selectionForeground` + `inline-selected-text` | `EditorTheme.SelectionForeground` (P7-21) |
| hidden textarea / `ISimpleModel` (`screenReaderSimpleModel.ts`) | `AutomationTextDocument` (P7-05) |

## 4. The UIA text semantics (the spec of P7-05 … P7-11)

Monaco has nothing to port here; these rules are final. They follow the UIA documentation of `ITextProvider` / `ITextRangeProvider` and the
behavior Narrator and NVDA rely on, and decide what the documentation leaves open.

**Text and positions.** The document text is the model lines joined with `"\n"` (never `"\r\n"`, whatever the model's EOL). Every position
is a **model** `Position`. `DocumentStart = (1, 1)`; `DocumentEnd = (lineCount, maxColumn(lineCount))`. A range is a `Range` with
`Start <= End`; a range with `Start == End` is *degenerate* (an insertion point). The line break between two lines is **one** character of the
text (it belongs to the position `(line, maxColumn)`).

**Starts of a unit.** Every unit `U` has an ascending set `S(U)` of positions, the *starts*; `DocumentStart ∈ S(U)` always. Three helpers
(used by every operation):

- `StartAtOrBefore(U, p)` = the greatest `s ∈ S(U)` with `s <= p`;
- `NextStart(U, p)` = the smallest `s ∈ S(U)` with `s > p`, or `DocumentEnd` when there is none;
- `PreviousStart(U, p)` = the greatest `s ∈ S(U)` with `s < p`, or `null` when there is none.

`S(U)`:

| Unit (UIA `TextUnit`) | `S(U)` | Notes |
|---|---|---|
| `Character` | every grapheme start of every line (`Strings.NextCharLength`), plus `(line, maxColumn)` for every line except the last (the line break) | `DocumentStart` also for the empty document |
| `Format` | same as `Word` | UIA "run of same formatting": the editor has no per-run formatting that is exposed, a word is the closest |
| `Word` | per line: the start column of each *token*, see below; an empty line has one start `(line, 1)` | |
| `Line` | the start of each **view line** (a wrapped model line is several): the model position of `(viewLine, viewMinColumn)`; text hidden by folding belongs to the view line before it | uses the `ViewModel` |
| `Paragraph` | `(line, 1)` for every model line | |
| `Page` | the start of every `N`-th view line (`1`, `N + 1`, `2N + 1`, …) where `N = max(1, number of completely visible view lines)` | uses the `ViewModel` |
| `Document` | `{ DocumentStart }` | |

**Word tokens** (per model line, on UTF-16 units, with `WordCharacterClassifier(WordHelper.UsualWordSeparators, [])`): the line is cut in
*tokens*; a token is a maximal run of `Regular` characters **or** a maximal run of `WordSeparator` characters (any mix of separators), followed by the
`Whitespace` run that follows it. White space at the start of a line is a token of its own. A line made only of white space is one token. The line
break belongs to the last token of its line. (`Intl.Segmenter` is not used, like in `WordCharacterClassifier`: a run of CJK characters is one word.)
Examples: `foo.bar` → `foo`, `.`, `bar`; `foo  bar` → `foo  `, `bar`; `  x` → `  `, `x`; `a\nb` → `a\n`, `b`.

**Operations** (`R` = range, `U` = unit):

- `ExpandToEnclosingUnit(R, U)`: `start = StartAtOrBefore(U, R.Start)`, `end = NextStart(U, start)`; the result is `[start, end)`. (Exactly one unit, whether `R`
  was smaller or larger.) At `DocumentEnd` the result is the last unit (`DocumentEnd` is not a start, except for `Line`, `Paragraph`, `Page` when the last line is empty:
  then the result is the empty last line).
- `Move(R, U, count)`: (1) expand `R` to its unit as above, `cur = start`; (2) `count > 0`: up to `count` times, `s = NextStart-strict` = the smallest `s ∈ S(U)`
  with `s > cur`; if there is none, stop; else `cur = s` and `moved++`; (3) `count < 0`: up to `|count|` times, `s = PreviousStart(U, cur)`; if `null`, stop; else
  `cur = s`, `moved--`; (4) `R = [cur, NextStart(U, cur))`; return `moved` (the signed number of units really moved; `count == 0` → `0`, `R` is only expanded).
- `MoveEndpointByUnit(R, endpoint, U, count)`: `count == 0` → `0`, nothing changes. `e` = the endpoint. `count > 0`: up to `count` times, `s` = the smallest `s ∈ S(U)`
  with `s > e`; if there is one, `e = s`, `moved++`; else if `e < DocumentEnd`, `e = DocumentEnd`, `moved++` and stop; else stop. `count < 0`: up to `|count|` times,
  `s = PreviousStart(U, e)`; if `null` stop; else `e = s`, `moved--`. Then if `endpoint` is `Start` and `Start > End`: `End = Start`; if `endpoint` is `End` and `End < Start`: `Start = End`.
  Returns `moved`.
- `MoveEndpointByRange(A, aEndpoint, B, bEndpoint)`: `A.aEndpoint = B.bEndpoint`, then the same repair as above (a crossed range collapses to the moved endpoint).
- `CompareEndpoints(A, aEndpoint, B, bEndpoint)`: `-1`, `0` or `1` comparing the two positions. `Compare(A, B)`: `Start` and `End` equal.
- `GetText(R, maxLength)`: the text of `R`; `maxLength == -1` → no limit; otherwise at most `maxLength` UTF-16 units, never ending between the two halves of a surrogate
  pair (the high surrogate is dropped); `maxLength < -1` → `ArgumentOutOfRangeException`.
- `FindText(R, text, backward, ignoreCase)`: the first (`backward == false`) or last match of `text` **inside** `R` (ordinal; `OrdinalIgnoreCase` when `ignoreCase`), as a range;
  `null` when `text` is empty or there is no match.
- `Select` / `AddToSelection` / `RemoveFromSelection`: see P7-18 (a selection is `anchor = R.Start`, `position = R.End`, left to right).
- `GetBoundingRectangles` / `RangeFromPoint` / `GetVisibleRange`: see P7-11 (editor-relative DIPs; converted to screen pixels only in WinUI).
- Ranges are snapshots: before every use a range is *validated* against the current model (`TextModel.ValidateRange`); a model edit never throws in a range operation.

## 5. Test helpers shared by many cards

| Helper | Created by | What |
|---|---|---|
| `tests/LoomEditor.View.Tests/TestHelpers/TestCodeEditor.cs` (existing): `TestCodeEditorOptions` gains `Accessibility` | P7-01 | passes `EditorLayoutOptions.Accessibility` |
| `tests/LoomEditor.View.Tests/Accessibility/TestHelpers/AutomationTestHelpers.cs` (`WithDocument(text, callback, options?)` over `TestCodeEditor.WithTestCodeEditor`, `Rng(sl, sc, el, ec)`, `Pos(line, column)`) | P7-05 | runs a test callback with an `AutomationTextDocument` built on a `TestCodeEditor` |
| `tests/LoomEditor.View.Tests/Accessibility/TestHelpers/RecordingAnnouncer.cs` | P7-12 | an `IEditorAnnouncer` that records the messages |
| `tests/LoomEditor.View.Tests/Accessibility/TestHelpers/RecordingAutomationEventSink.cs` | P7-15 | an `IAutomationEventSink` that records the events |
| `ManualEditorScheduler` (existing, `tests/LoomEditor.View.Tests/Features/Scheduling/` or `TestHelpers/`: find it with `grep -rn "class ManualEditorScheduler" tests`) | — | runs scheduled actions when the test says so |

A card that needs a helper missing from this table creates it in the `TestHelpers/` folder next to its tests and adds a row here.

## 6. Manual checks

WinUI cards add their manual checks to `docs/testing/phase-7-manual-tests.md` (created by P7-04, written in full by P7-25).
The Native AOT publish of the Gallery is attempted by the WinUI cards that say so, as in P6-62 (`dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:UseOptimizedPublish=false`
if the plain command fails with XamlCompiler `WMC1007`, see ADR 0008); report the result honestly in the card, never tick it if it was not run.

## 7. Commands and commit

Same as every card (see `CLAUDE.md`). Public API members go in `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`);
everything else is `internal`.
