# P6-39 — Find, highlights and bracket matching mark the overview ruler

- **Status:** todo
- **Depends on:** P6-38
- **Size:** S

## Goal

The features that Monaco marks in the overview ruler do so in LoomEditor: find matches (with the approximate decorations
above 1000 matches), the current find match, word and selection highlights, and the matching bracket.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/find/browser/findDecorations.ts` | 19, 28, 39, 47 | the `_overviewRulerApproximateDecorations` field, its reset and its disposal |
| same | 161–197, 209–210 | the approximate overview decorations when there are more than 1000 matches (merging line ranges) |
| same | 276 | `getAllDecorations` includes them |
| same | 286–351 | the overview ruler parts of `_CURRENT_FIND_MATCH_DECORATION`, `_FIND_MATCH_DECORATION`, `_FIND_MATCH_ONLY_OVERVIEW_DECORATION` (`Center` lane) |
| `src/vs/editor/contrib/wordHighlighter/browser/highlightDecorations.ts` | 20–22, 24–95 | the three ruler colors and the `overviewRuler` parts of the word / selection highlight options (`Center` lane) |
| `src/vs/editor/contrib/bracketMatching/browser/bracketMatching.ts` | 27, 300–312 | `editorOverviewRuler.bracketMatchForeground` and `_DECORATION_OPTIONS_WITH_OVERVIEW_RULER` |
| `src/vs/platform/theme/common/colors/editorColors.ts` | 436–442 | `editorOverviewRuler.findMatchForeground`, `editorOverviewRuler.selectionHighlightForeground` defaults |

## Files to create or change

- `src/LoomEditor.View/Features/Find/FindDecorations.cs` — change (remove the "phase 6 brings the ruler" remarks)
- `src/LoomEditor.View/Features/WordHighlighter/HighlightDecorations.cs`, `Features/MultiCursor/SelectionHighlightDecorations.cs` — change
- `src/LoomEditor.View/Features/BracketMatching/` — change: the options with the overview ruler (find the file that ports
  `bracketMatching.ts` options: `grep -rln "bracket-match" src/LoomEditor.View/Features`)
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: the five ruler colors with their upstream defaults
- the feature tests of each changed file — change: assert the new options

## Design decisions (already made — do not change)

- Colors are `DecorationColor.FromThemeColor("editorOverviewRuler.…")` (P6-36), lane `OverviewRulerLane.Center` as upstream.
- The `minimap` parts of the same options are **not** added here (P6-54).
- Bracket matching: Monaco chooses the option with or without ruler by the `matchBrackets` setting (`'always'` vs `'near'`);
  port that choice exactly as upstream reads it (lines 280–312); if LoomEditor's port has no `matchBrackets` option, use the
  "with overview ruler" options (Monaco's default `matchBrackets: 'always'`).

## Tests

- [ ] New: `FindDecorations_Matches_HaveTheRulerColor`
- [ ] New: `FindDecorations_MoreThan1000Matches_AddsApproximateRulerDecorations` (merged line ranges; `GetAllDecorations` lists them)
- [ ] New: `WordHighlight_Options_HaveTheRulerColor`; `SelectionHighlight_Options_HaveTheRulerColor`
- [ ] New: `BracketMatching_Options_HaveTheRulerColor`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: the rows of `findDecorations.ts`, `highlightDecorations.ts`, `bracketMatching.ts` no longer say "no overview ruler";
      roadmap: nothing to tick (the overview ruler is not a roadmap line; add "Overview ruler" as a ticked line to
      `docs/roadmap/phase-6-view-extras.md`)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Markers / diagnostics marks (phase 8), the minimap.

## Suggested commit message

`feat(view): mark find matches, highlights and brackets in the overview ruler`
