# Phase 7 — Full UI Automation accessibility

**Done when:** Narrator reads and navigates the content.

Task cards: [docs/tasks/phase-7/](../tasks/phase-7/README.md). Design: [ADR 0010](../adr/0010-accessibility.md).

- [ ] `ITextProvider` / `ITextRangeProvider` on `LoomEditorAutomationPeer` — P7-05 … P7-11 (the UI-free text model: document, ranges, units, move, find, geometry), P7-17 … P7-18 (the peer and the range provider), `ITextProvider2` in P7-19
- [ ] Caret and selection events, live regions for important changes — P7-15 (event coalescing), P7-19 (UIA events), P7-12 … P7-14 (announcements: find results, word highlight, tab focus mode, sent as UIA notifications)
- [ ] High contrast themes — P7-20 (contrast of the bundled themes), P7-21 (`editor.selectionForeground`), P7-22 … P7-23 (the editor follows the Windows high contrast colors)
- [ ] Manual test plan with Narrator and NVDA — P7-25 (and the in-process UIA self-check of P7-24)

Also in phase 7 (they were not on the original checklist but are part of an accessible editor and of Monaco's accessibility behavior):

- [ ] Screen reader mode: `accessibilitySupport`, wrapping indent `None` and forced wrapping of long-line models — P7-01 … P7-04 (moved here from phase 6, "accessibility-driven wrapping")
- [ ] Tab focus mode (`Ctrl+M`), the keyboard trap exit — P7-16
