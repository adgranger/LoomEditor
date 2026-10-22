# P6-33 — The colors of indent and bracket pair guides

- **Status:** done
- **Depends on:** P6-27
- **Size:** S

## Goal

The themes know the 24 guide colors with Monaco's defaults, and the view resolves the effective color of an indent guide
level and of a bracket guide level (active or not), with Monaco's fallbacks.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/core/editorColorRegistry.ts` | 32–47 | `editorIndentGuide.background` / `.activeBackground` (deprecated ones) and `editorIndentGuide.background1…6`, `editorIndentGuide.activeBackground1…6` |
| same | 88–101 | `editorBracketPairGuide.background1…6`, `editorBracketPairGuide.activeBackground1…6` |
| `src/vs/editor/browser/viewParts/indentGuides/indentGuides.ts` | 254–345 | `transparentToUndefined` and the theming participant: the effective colors (a bracket guide falls back to the bracket color at 30 % / 100 %; levels without both colors are dropped; CSS rules cycle over the remaining ones) |

## Files to create or change

- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: add the 26 rows (2 deprecated + 24), each with its
  `// editorColorRegistry.ts:N` comment and the four defaults (dark, light, hcDark, hcLight) **as written upstream** (some
  defaults reference other colors, e.g. `deprecatedEditorIndentGuides`: use the reference mechanism the table already has
  for `contrastBorder`-like references; if a default is a `transparent(…)` expression, check how existing rows encode it)
- `src/LoomEditor.View/Layout/GuideColors.cs` — create (header `indentGuides.ts`): `internal static class GuideColors`
- `tests/LoomEditor.Themes.Tests` or the existing theme defaults tests — change: one test per new default kind
- `tests/LoomEditor.View.Tests/Layout/GuideColorsTests.cs` — create

## Design decisions (already made — do not change)

- `GuideColors.GetIndentGuideColor(EditorTheme theme, int level, bool active)` and
  `GuideColors.GetBracketGuideColor(EditorTheme theme, int level, bool active)` return `ThemeColor?`; the lists of effective
  colors are cached per theme with a `ConditionalWeakTable` like `BracketPairColorizationColors` does.
- The level index cycles over the **effective** list (`level % count`), like the generated CSS (`lvl-${i}` rules repeat
  every `count`); `null` when the list is empty.
- `bracketColor.transparent(0.3)` → multiply the alpha by 0.3 (look for an existing `ThemeColor` helper; add `WithAlphaFactor`
  to `ThemeColor` if missing, with a test).
- If the themes project has no tests project, put the defaults tests in `LoomEditor.View.Tests` next to the other theme tests
  (find where `EditorColorDefaults` is tested with `grep -rln EditorColorDefaults tests`).

## Tests

- [x] New: `IndentGuideColor_DarkTheme_UsesTheDefaults` (level 0 and 5, active and not)
- [x] New: `BracketGuideColor_NoGuideColor_FallsBackToTheBracketColorAt30Percent`
- [x] New: `BracketGuideColor_TransparentLevelsAreSkipped`
- [x] New: `IndentGuideColor_LevelCyclesOverTheEffectiveColors`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `editorColorRegistry.ts` row lists the guide colors; new row `indentGuides.ts` (🔶 colors only)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Computing and drawing the guides (P6-34, P6-35).

## Suggested commit message

`feat(themes): add the indent and bracket guide colors`
