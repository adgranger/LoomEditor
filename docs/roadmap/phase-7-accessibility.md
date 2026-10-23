# Phase 7 — Full UI Automation accessibility

**Done when:** Narrator reads and navigates the content.

**Status (2026-10-23):** all code items are done and tested (build 0 warnings, 1684 tests green, Native AOT publish of the Gallery `succeeded`, in-process UIA self-check 19 of 19 on the P7-24 run). **Manual verification pending — not verified:** the manual plan ([docs/testing/phase-7-manual-tests.md](../testing/phase-7-manual-tests.md)) was not run by the agent (the maintainer runs it), so "Narrator reads and navigates the content" is **not** confirmed.

Task cards: [docs/tasks/phase-7/](../tasks/phase-7/README.md). Design: [ADR 0010](../adr/0010-accessibility.md).

- [x] `ITextProvider` / `ITextRangeProvider` on `LoomEditorAutomationPeer` — P7-05 … P7-11 (the UI-free text model: document, ranges, units, move, find, geometry), P7-17 … P7-18 (the peer and the range provider), `ITextProvider2` in P7-19
- [x] Caret and selection events, live regions for important changes — P7-15 (event coalescing), P7-19 (UIA events), P7-12 … P7-14 (announcements: find results, word highlight, tab focus mode, sent as UIA notifications)
- [x] High contrast themes — P7-20 (contrast of the bundled themes), P7-21 (`editor.selectionForeground`), P7-22 … P7-23 (the editor follows the Windows high contrast colors)
- [x] Manual test plan with Narrator and NVDA written — P7-25 (and the in-process UIA self-check of P7-24). **Running it with Narrator and NVDA: manual verification pending** (not ticked as a result, see below)

Also in phase 7 (they were not on the original checklist but are part of an accessible editor and of Monaco's accessibility behavior):

- [x] Screen reader mode: `accessibilitySupport`, wrapping indent `None` and forced wrapping of long-line models — P7-01 … P7-04 (moved here from phase 6, "accessibility-driven wrapping")
- [x] Tab focus mode (`Ctrl+M`), the keyboard trap exit — P7-16

## Manual verification pending (for the maintainer)

- Narrator and NVDA reading and navigating the editor (the "Done when" line), by the plan in `docs/testing/phase-7-manual-tests.md`.
- The unticked manual checks of the cards: P7-04 (1.1 – 1.3), P7-17 (3.3 – 3.7), P7-23 (6.1 – 6.8, high contrast schemes, also on the Native AOT build), P7-24 (7.3 – 7.5).
- Each finding of the manual plan becomes a follow-up card, not a change to a closed one.

## Moved

- Accessibility-driven wrapping came from phase 6 (done here, P7-01 … P7-04).
- Not planned in phase 7 (ADR 0010 "Not in phase 7"): accessibility help dialog, `ariaLabel` / `ariaRole` options, reduced motion, the UIA text edit pattern and annotations, per-token text attributes, the `cursorWordAccessibility*` keybindings, localization of announcements, a UIA client based automated test, the accessible view of the diff editor (phase 9).
- Not ported from Monaco's accessibility code: `wrappingStrategy: advanced`, `accessibilityPageSize` and the paged screen reader strategy, `allowVariableFontsInAccessibilityMode`.

## Build and publish results

- `dotnet build LoomEditor.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test --solution LoomEditor.slnx -c Release`: 1684 tests passed.
- `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` (Native AOT): succeeded (2026-10-23). Running the published exe was **not** done by the agent for this card; the P7-17, P7-19 and P7-24 cards report their own AOT runs, P7-23's AOT high contrast check (6.8) is **not verified**.

## Open risks

- The UIA calls are assumed to arrive on the UI thread (`Guard.Assert` in the providers).
- `DependencyProperty.UnsetValue` stands for "attribute not supported".
- The Narrator `Move` semantics follow `docs/tasks/phase-7/README.md` §4, which came from the documentation and may need tuning against real Narrator behavior (one follow-up card per finding of the manual plan).
