# P5-96 — Close phase 5

- **Status:** todo
- **Depends on:** all phase 5 cards
- **Size:** S (docs)

## Goal

The documentation describes the code as it is at the end of phase 5, and phase 6 (word wrap, guides, minimap, sticky
scroll) can start from a clear list.

## Files to create or change

- `docs/ARCHITECTURE.md` — a section "How a feature reacts to the editor (phase 5)" after "Extension model: features":
  model events and outgoing view-model events → the feature → `IEditorScheduler` (`RunOnceScheduler` / `Delayer`) →
  model decorations (`DeltaDecorations`, `EditorDecorationsCollection`) → view-model decorations in view lines →
  `DecorationStyles` → the renderer; folding as the example (ranges → `FoldingModel` → `HiddenRangeModel` →
  `ViewModel.SetHiddenAreas` → `ViewModelLinesFromProjectedModel` → view lines). "Where do I find…?" rows: decorations,
  decoration styles, language configuration, bracket pairs, find (logic and widget), folding, hidden areas / view lines,
  word highlight, the scheduler, chords
- `docs/PORTING.md` — "Done in phase 5" section (like phases 1–4, with ADR 0009); the map complete for everything
  ported in phase 5; the behavior differences of phase 5; "Tests ported in phase 5" (ported / total / why not, one row per
  upstream test file); every phase 3 cut point of "Cut points for phases 4–5" marked resolved or moved; a new section
  **"Cut points for phase 6"**: the line-break projection and word wrap (`ModelLineProjection`, `ViewModelLinesFromProjectedModel`
  wrapping members, `modelLineProjection.test.ts` 3 tests), indent and bracket guides (`'Invalid line numbers'` guide
  assertions), injected text, the minimap and overview ruler parts of decorations, sticky scroll (can now use the
  folding model), `viewModelImpl.test.ts` tests that need word wrap or injected text
- `docs/roadmap/phase-5-features.md` — tick the items (add line operations, find-based multi-cursor, word highlight if
  missing), "Done when" with the date
- `docs/how-to/add-a-feature.md` — up to date with what phase 5 features use: `IEditorContext.Scheduler`, decorations,
  the language configuration, chords in `DefaultKeybindings`, a `README.md` per feature
- `src/LoomEditor.View/Features/*/README.md` — every phase 5 feature folder has one and it matches the code
  (`Find`, `Folding`, `BracketMatching`, `Comment`, `Indentation`, `LinesOperations`, `WordHighlighter`, `MultiCursor`,
  `Scheduling`)
- `NOTICE.md` — the bundled `language-configuration.json` files (VS Code `extensions/<language>/`, MIT, pinned commit)
  are listed
- `docs/tasks/README.md` — phase 5 statuses (and the temporary card ids replaced by the final ones, if not done yet)

## Design decisions (already made — do not change)

- No code change in this card. A problem found while writing the docs becomes a follow-up card in the index, not a fix.
- Test counts are counted from the test files (`// Monaco:` comments), not copied from the cards.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Phase 6 cards (written when phase 6 starts).

## Suggested commit message

`docs: close phase 5`
