# P6-37 — `ViewModel.GetAllOverviewRulerDecorations`: groups per color

- **Status:** todo
- **Depends on:** P6-36
- **Size:** S

## Goal

The view model turns the overview ruler decorations into groups of view line ranges per resolved color (merging
adjacent ranges of the same lane), ready to be drawn, with colors resolved against the current theme.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 941–961 | `getAllOverviewRulerDecorations` |
| same | 963–971 | `_invalidateDecorationsColorCache` (informative: see decisions) |
| same | 1360–1388 | `class OverviewRulerDecorations` (`accept` merges into the previous range of the same color and lane) |
| `src/vs/editor/common/viewModel.ts` | 410–448 | `OverviewRulerDecorationsGroup` (`compareByRenderingProps`, `equals`, `equalsArr`) |
| `src/vs/editor/common/model/textModel.ts` | 2370–2405 | `ModelDecorationOverviewRulerOptions.getColor` / `_resolveColor` (the `darkColor` rule for dark themes) |

## Files to create or change

- `src/LoomEditor.View/Layout/OverviewRulerDecorationsGroup.cs` — create (header `viewModel.ts`)
- `src/LoomEditor.View/Layout/OverviewRulerDecorations.cs` — create (header `viewModelImpl.ts`)
- `src/LoomEditor.View/Layout/DecorationColorResolver.cs` — create (header `textModel.ts (getColor)`): resolves a
  `DecorationColor` against an `EditorTheme`
- `src/LoomEditor.View/Layout/ViewModel.Decorations.cs` — change: `GetAllOverviewRulerDecorations(EditorTheme theme)`
- `tests/LoomEditor.View.Tests/Layout/OverviewRulerDecorationsTests.cs` — create

## Design decisions (already made — do not change)

- `OverviewRulerDecorationsGroup(ThemeColor Color, int ZIndex, List<int> Data)` (`internal sealed class`, `Data` encodes
  `lane, start, end` triples like Monaco). `compareByRenderingProps` compares `ZIndex`, then the color: Monaco compares CSS strings;
  LoomEditor compares the ARGB value as a `uint`. Behavior difference (PORTING): when two groups with the same z-index
  overlap, the one drawn last can differ from Monaco's; the order stays deterministic.
- The groups are keyed by resolved color in a `Dictionary<ThemeColor, …>` plus a `List` for insertion order (Monaco: object map + array).
- `DecorationColorResolver.Resolve(DecorationColor? color, DecorationColor? darkColor, EditorTheme theme)` → `ThemeColor?`:
  dark themes (`ThemeType.Dark` and `HighContrastDark`, Monaco's `isDark`) use `darkColor` when set; a theme color id goes
  through `EditorTheme.GetColor` (internal, visible to View through `InternalsVisibleTo` — check it is; if not, use the public
  path the renderer uses for other theme colors); a value is parsed with `ThemeColor.Parse`. An unknown id → `null` (Monaco: `''`),
  and a `null` color skips the decoration.
- No color cache in Core (Monaco caches `_resolvedColor` on the options and invalidates it on theme change): the resolver keeps a
  `ConditionalWeakTable<EditorTheme, Dictionary<…>>` cache instead, so `_invalidateDecorationsColorCache` is not needed. Say so in
  PORTING "Behavior differences".
- `filterOutValidation` / `filterFontDecorations`: pass `false` / `false` (LoomEditor has no `renderValidationDecorations` option and no
  font decorations).
- Lane `0` is skipped (Monaco `if (lane === 0) continue`).

## Tests

- [ ] New: `GetAllOverviewRulerDecorations_GroupsByColor`
- [ ] New: `GetAllOverviewRulerDecorations_MergesAdjacentRangesOfTheSameLane`; `…_DoesNotMergeDifferentLanes`
- [ ] New: `GetAllOverviewRulerDecorations_UsesViewLineNumbers` (a hidden area above shifts them)
- [ ] New: `DecorationColorResolver_DarkTheme_PrefersDarkColor`; `…_ThemeColorId_IsResolved`
- [ ] New: `CompareByRenderingProps_OrdersByZIndexThenColor`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `viewModelImpl.ts`, `viewModel.ts` rows; behavior differences (color cache)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing (P6-38), feeding features (P6-39).

## Suggested commit message

`feat(view): group the overview ruler decorations`
