# P4-50 — Close phase 4

- **Status:** todo
- **Depends on:** all phase 4 cards
- **Size:** S (docs)

## Goal

The documentation describes the code as it is at the end of phase 4, and phase 5 can start from a clear list.

## Files to create or change

- `docs/ARCHITECTURE.md` — a section "How a line gets its colors (phase 4)" (grammar → tokens on the worker → model
  token store → view model → renderer; viewport first on the UI thread), the "Where do I find…?" rows for tokens,
  grammars, themes, the worker
- `docs/PORTING.md` — "Done in phase 4" section (like phases 1–3), the map complete for the three repositories, the
  behavior differences, the tests ported and not ported, and **"Cut points for phase 5"** updated: which phase 3 cut
  points can now read tokens (`StandardTokenType`, language id per token) and what is still missing (language
  configuration)
- `docs/roadmap/phase-4-textmate-themes.md` — tick the items, "Done when" with the date and the golden-test result
- `docs/how-to/add-a-language.md` — remove "Status: phase 4 — not available yet"; the final steps (P4-35, P4-47)
- `src/LoomEditor.TextMate/*/README.md`, `src/LoomEditor.Themes` README if any — up to date (`Json/README.md` says JSON is read with `JsonNode`)
- `NOTICE.md` — check every bundled grammar, theme and test-data source is listed with its license
- `docs/tasks/README.md` — phase 4 statuses

## Design decisions (already made — do not change)

- No code change in this card. A problem found while writing the docs becomes a follow-up card in the index, not a fix.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Phase 5 cards (written when phase 5 starts).

## Suggested commit message

`docs: close phase 4`
