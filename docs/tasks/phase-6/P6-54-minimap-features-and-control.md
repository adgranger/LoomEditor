# P6-54 — Features mark the minimap; the control's minimap properties

- **Status:** todo
- **Depends on:** P6-53, P6-39
- **Size:** S

## Goal

Find matches, word / selection highlights and folded regions are painted in the minimap like Monaco; the minimap is on by
default (Monaco's default) and the control exposes `MinimapEnabled`, `MinimapSide`, `MinimapRenderCharacters`; the gallery can
toggle it.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/find/browser/findDecorations.ts` | 286–334 | the `minimap` parts of `_CURRENT_FIND_MATCH_DECORATION` and `_FIND_MATCH_DECORATION` (`minimap.findMatchHighlight`, `Inline`) |
| `src/vs/editor/contrib/wordHighlighter/browser/highlightDecorations.ts` | 24–95 | the `minimap` parts (`minimap.selectionOccurrenceHighlight`, `Inline`) |
| `src/vs/editor/contrib/folding/browser/foldingDecorations.ts` | 26, 40–90 | `foldedBackgroundMinimap` (`editor.foldBackground`, `Inline`) on the collapsed decorations |
| `src/vs/editor/common/config/editorOptions.ts` | 3405–3482 | the doc of `enabled`, `side`, `renderCharacters` (XML docs of the properties) |

## Files to create or change

- `src/LoomEditor.View/Features/Find/FindDecorations.cs`, `Features/WordHighlighter/HighlightDecorations.cs`,
  `Features/MultiCursor/SelectionHighlightDecorations.cs`, `Features/Folding/FoldingDecorationProvider.cs` — change (remove the
  "no minimap" remarks)
- `src/LoomEditor.View/Layout/EditorLayoutOptions.cs` — change: the minimap is enabled by default (remove P6-40's temporary `Enabled = false`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Minimap.cs` — create: `public bool MinimapEnabled` (default `true`), `public EditorMinimapSide MinimapSide`
  (`public enum EditorMinimapSide { Right, Left }`, default `Right`), `public bool MinimapRenderCharacters` (default `true`)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — update (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `samples/LoomEditor.Gallery/MainWindow.xaml(.cs)` — change: a "Minimap" toggle
- the feature tests of each changed file — change; `docs/testing/phase-6-manual-tests.md` — add the minimap checks (create the file as P6-26 describes if it does not exist yet)

## Design decisions (already made — do not change)

- Colors are `DecorationColor.FromThemeColor(…)` (P6-36) with the ids above; their defaults exist (P6-46; `editor.foldBackground`
  exists since P5-87 — check).
- Tests that compare whole layouts (`EditorLayoutInfoComputerTests`, view model / renderer tests that assume no minimap) must pass
  explicit options: fix them by giving those tests `Minimap = new() { Enabled = false }`, not by changing the default back.

## Tests

- [ ] New: `FindDecorations_Matches_HaveTheMinimapColor`; `Highlights_HaveTheMinimapColor`; `CollapsedRegion_HasTheMinimapColor`
- [ ] New: `EditorLayoutOptions_MinimapEnabledByDefault`
- [ ] Manual (`phase-6-manual-tests.md`): minimap visible, click / drag, slider on hover, side left, blocks mode, find matches in the minimap

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — succeeds
- [ ] `docs/roadmap/phase-6-view-extras.md`: tick "Minimap"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Section headers (`showRegionSectionHeaders`, `showMarkSectionHeaders`), `minimap.size` / `scale` / `autohide` / `showSlider` control
properties (internal options only).

## Suggested commit message

`feat(winui): enable the minimap`
