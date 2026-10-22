# P6-46 — `MinimapOptions`, the token color tracker and the minimap colors

- **Status:** todo
- **Depends on:** P6-44, P6-43
- **Size:** S

## Goal

The minimap's resolved settings for one frame exist (`MinimapOptions`: layout values, char renderer, background, foreground
alpha), with the token colors as `Rgba8` and the minimap theme colors with Monaco's defaults.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/minimap/minimap.ts` | 46–48 | `POINTER_DRAG_RESET_DISTANCE`, `GUTTER_DECORATION_WIDTH` |
| same | 50–212 | `class MinimapOptions` (constructor, `_getMinimapBackground`, `_getMinimapForegroundOpacity`, `equals`; **not** the section header members) |
| `src/vs/editor/common/viewModel/minimapTokensColorTracker.ts` | 13–75 | `MinimapTokensColorTracker` (`getColor`, `backgroundIsLight`) |
| `src/vs/platform/theme/common/colors/minimapColors.ts` | 17–60 | `minimap.findMatchHighlight`, `minimap.selectionOccurrenceHighlight`, `minimap.selectionHighlight`, `minimap.infoHighlight`, `minimap.warningHighlight`, `minimap.errorHighlight`, `minimap.background`, `minimap.foregroundOpacity`, `minimapSlider.background`, `minimapSlider.hoverBackground`, `minimapSlider.activeBackground` |

## Files to create or change

- `src/LoomEditor.View/Minimap/MinimapOptions.cs` — create (header `minimap.ts (MinimapOptions)`)
- `src/LoomEditor.View/Minimap/MinimapTokensColorTracker.cs` — create (header `minimapTokensColorTracker.ts`)
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: the 11 colors with their upstream defaults (some are `transparent(…)` of
  other colors: encode them the way existing rows encode derived defaults)
- `tests/LoomEditor.View.Tests/Minimap/MinimapOptionsTests.cs` — create

## Design decisions (already made — do not change)

- `MinimapTokensColorTracker` is **not** a singleton: `new MinimapTokensColorTracker(EditorTheme theme)` reads the theme's token color
  map (the same map `ViewModel.TokenColorMap` exposes; `ColorId.DefaultForeground` = 1, `DefaultBackground` = 2, like Monaco). A new
  tracker is created when the theme changes (Monaco listens to `TokenizationRegistry.onDidChange`).
- `MinimapOptions` is an `internal sealed class` with get-only properties built from `(EditorLayoutInfo layoutInfo, EditorMinimapOptions options, FontInfo fontInfo, bool scrollBeyondLastLine, EditorTheme theme, MinimapTokensColorTracker tracker, Func<int, string, MinimapCharRenderer> charRendererFactory)`;
  padding is 0 (no padding option); `minimapHeight = editorHeight = layoutInfo.Height`; `Equals(MinimapOptions)` like Monaco (no
  `Object.Equals` override needed: name it `EqualsOptions`).
- `charRenderer` is a `Lazy<MinimapCharRenderer>` (Monaco: `createSingleCallFunction`).
- `Math.round(255 * a)` → `Math.Round(255 * a, MidpointRounding.AwayFromZero)` (JavaScript's `Math.round` rounds .5 up for positives).

## Tests

- [ ] New: `MinimapOptions_NoMinimapBackground_UsesTheDefaultBackground`
- [ ] New: `MinimapOptions_ForegroundOpacity_FromTheTheme`
- [ ] New: `MinimapOptions_EqualsOptions_DetectsAChange`
- [ ] New: `TokensColorTracker_BackgroundIsLight_ForTheLightTheme`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `minimap.ts` (🔶 options), `minimapTokensColorTracker.ts`, `minimapColors.ts` rows
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`MinimapLayout` (P6-47), sampling (P6-48).

## Suggested commit message

`feat(view): resolve the minimap options and colors`
