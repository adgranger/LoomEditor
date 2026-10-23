# P7-26 — Close phase 7

- **Status:** todo
- **Depends on:** all phase 7 cards
- **Size:** S (docs)

## Goal

The documentation describes the code as it is at the end of phase 7, the roadmap is ticked, and the gaps left for later phases are listed.

## Files to create or change

- `docs/ARCHITECTURE.md` — a section "Accessibility (phase 7)" after "View lines with word wrap and injected text": `TextModel` + `ViewModel` → `AutomationTextDocument` / `AutomationTextRange` / units / geometry (`LoomEditor.View/Accessibility/`) → `LoomEditorAutomationPeer` and
  `LoomEditorTextRangeProvider` (`LoomEditor.WinUI/Accessibility/`) → UIA clients; the events path (`EditorContext.SelectionChanged` / `Model.ContentChanged` → `AutomationEventCoalescer` → `AutomationEventSink` → `RaiseAutomationEvent`) and the announcements path
  (`IEditorAnnouncer` → `AutomationAnnouncer` → `RaiseNotificationEvent`); screen reader mode (`EditorAccessibilityOptions`, forced wrapping); tab focus mode; high contrast (`SystemHighContrastThemeFactory`, `editor.selectionForeground`).
  "Where do I find…?" rows: UIA text pattern / ranges / units; announcements; accessibility options and wrapping; tab focus mode; high contrast themes; the UIA self-check (Gallery). Update the layer table row of `LoomEditor.WinUI` ("accessibility") if it is out of date.
- `docs/PORTING.md` — "Done in phase 7 (accessibility)" section (like phase 6); the map complete for everything ported in phase 7 (`AccessibilitySupport`, `isDominatedByLongLines`, the four announcement messages, tab focus mode, `editor.selectionForeground` locations);
  the behavior differences of phase 7 (search the cards for "behavior difference" and for "Not ported"); the row "Accessibility-driven wrapping (`accessibilitySupport`)" of "Cut points for later phases" marked done, with what is **not** ported (`wrappingStrategy: advanced`,
  `accessibilityPageSize`, the paged screen reader strategy, `allowVariableFontsInAccessibilityMode`, `ariaLabel` options, the accessibility help dialog); "Tests ported in phase 7": all phase 7 tests are `New:` (no upstream test), say so in one line with the count of tests per test file
- `docs/roadmap/phase-7-accessibility.md` — every item ticked, with the card ids; the "Done when" line gets the date and "not verified: the manual plan (`docs/testing/phase-7-manual-tests.md`) was not run by the agent" (the maintainer runs it); items that moved go to a "Moved" list
- `docs/adr/0010-accessibility.md` — change **only** to correct a decision the cards had to change (write the correction and the reason in a "Changes during implementation" section at the end); otherwise untouched
- `src/LoomEditor.WinUI/Accessibility/README.md`, `src/LoomEditor.View/Accessibility/README.md` — match the code (files, one line each)
- `docs/tasks/README.md` — phase 7 statuses; a "Deferred on purpose" line for phase 7 (from ADR 0010 "Not in phase 7"); in the phase 6 "Deferred on purpose" line change only "accessibility-driven wrapping (phase 7)" to "accessibility-driven wrapping (done in phase 7)"

## Design decisions (already made — do not change)

- No code change in this card. A problem found while writing the docs becomes a follow-up card in the index (status `todo`, in a "Follow-ups" step of phase 7), not a fix.
- Test counts are counted from the test files, not copied from the cards (`grep -c "\[Fact\]\|\[Theory\]"` per file, theories counted once).
- "Not verified" items are listed honestly: the Native AOT publish results reported by the WinUI cards (copy them from the commit bodies: `git log --grep="phase 7"` is not enough, read the card statuses), the manual plan, the UIA behavior under Narrator / NVDA.
- Phase 7 open risks written into the roadmap file, from ADR 0010 decision 11 and the cards: the UIA calls are assumed to arrive on the UI thread (`Guard.Assert` in the providers); `DependencyProperty.UnsetValue` as "attribute not supported";
  the Narrator `Move` semantics follow README §4, which came from the documentation and may need tuning against real Narrator behavior (a follow-up card per finding of the manual plan).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — result stated in the roadmap file (`succeeded`, or `NOT verified` with the reason), never ticked if it was not run
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Phase 8 cards, running the manual plan, any code change.

## Suggested commit message

`docs: close phase 7`
