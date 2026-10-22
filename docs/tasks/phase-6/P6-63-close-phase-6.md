# P6-63 — Close phase 6

- **Status:** todo
- **Depends on:** all phase 6 cards
- **Size:** S (docs)

## Goal

The documentation describes the code as it is at the end of phase 6, and the gaps left for later phases are listed.

## Files to create or change

- `docs/ARCHITECTURE.md` — a section "View lines with word wrap and injected text (phase 6)": model line → `LineInjectedText` →
  line-break computer → `ModelLineProjectionData` → `WrappedModelLineProjection` → `ViewModelLinesFromProjectedModel` → view lines; where the
  guides, the overview ruler, the minimap (`LoomEditor.View/Minimap/` + `TextViewRenderer.Minimap.cs`) and sticky scroll (feature +
  `TextViewRenderer.StickyScroll.cs`) live; "Where do I find…?" rows for each. Update the line that says word wrap comes in phase 6.
- `docs/PORTING.md` — "Done in phase 6" section (like phase 5); the map complete for everything ported in phase 6; the behavior
  differences of phase 6 (search the cards for "behavior difference"); "Tests ported in phase 6" complete (ported / total / why not, one
  row per upstream test file, counted from the `// Monaco:` comments); every row of "Cut points for phase 6" marked resolved or moved;
  a new section **"Cut points for later phases"**: outline-based sticky scroll and the 4 `stickyScroll.test.ts` tests (phase 8),
  click-link gesture in sticky scroll (phase 8), keyboard navigation of sticky lines, minimap section headers, the word wrap indicator
  drawing, `renderWhitespace`, the DOM line-break strategy (`wrappingStrategy: advanced`, not planned), accessibility-driven wrapping
  (phase 7), the generic `OverviewRuler` / `overviewZoneManager.ts` (phase 9, diff editor)
- `docs/roadmap/phase-6-view-extras.md` — every item ticked or moved (render whitespace is **not** done in phase 6: move it to a later
  phase with a note), "Done when" with the date
- `src/LoomEditor.View/Features/StickyScroll/README.md`, `src/LoomEditor.View/Layout/README.md` — match the code
- `docs/tasks/README.md` — phase 6 statuses

## Design decisions (already made — do not change)

- No code change in this card. A problem found while writing the docs becomes a follow-up card in the index, not a fix.
- Test counts are counted from the test files (`// Monaco:` comments), not copied from the cards.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — succeeds
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Phase 7 cards.

## Suggested commit message

`docs: close phase 6`
