# P4-39 — Themes: theme types and VS Code's default editor colors

- **Status:** done
- **Depends on:** P4-38
- **Size:** S

## Goal

`LoomEditor.Themes` knows the four theme types (dark, light, high-contrast dark, high-contrast light) and the default
value of every editor color that `EditorTheme` exposes, resolved like VS Code's color registry (a default may name
another color).

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/platform/theme/common/colorUtils.ts` | 75, 168–223, 352–366 | `isColorDefaults`, `registerColor` (as a static table, not a registry), `resolveDefaultColor`, `resolveColorValue` (null, `#hex`, color id; **not** the transforms `executeTransform` 262–317: none of our keys uses one) |
| vscode: `src/vs/platform/theme/common/colors/editorColors.ts` | 19, 23, 121, 141 | defaults of `editor.background`, `editor.foreground`, `editor.selectionBackground`, `editor.compositionBorder` |
| vscode: `src/vs/editor/common/core/editorColorRegistry.ts` | 14, 16, 22, 30, 49–50 | defaults of `editor.lineHighlightBackground`, `editor.lineHighlightBorder`, `editorCursor.foreground`, `editorLineNumber.foreground`, `editorActiveLineNumber.foreground` (deprecated id) and `editorLineNumber.activeForeground` |
| vscode: `src/vs/platform/theme/common/colors/baseColors.ts` | 13, 37, 41, 45 | `foreground`, `focusBorder`, `contrastBorder`, `contrastActiveBorder` (referenced by the defaults above) |
| vscode: `src/vs/workbench/services/themes/common/colorThemeData.ts` | 649–667 | `themeTypeSelector`, `type` (`vs` → light, `hc-black` → hcDark, `hc-light` → hcLight, otherwise dark) |

## Files to create or change

- `src/LoomEditor.Themes/ThemeType.cs` — create (**public** enum: `Dark`, `Light`, `HighContrastDark`, `HighContrastLight`)
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — create (internal: the table `id → (dark, light, hcDark, hcLight)` and `Resolve(id, ThemeType, Func<string, ThemeColor?> themeColors)`)
- `src/LoomEditor.Themes/PublicAPI.Unshipped.txt` — via `./eng/Update-PublicApi.ps1 src/LoomEditor.Themes`
- `src/LoomEditor.Themes/LoomEditor.Themes.csproj` — `InternalsVisibleTo` for `LoomEditor.View` and the test project
- `tests/LoomEditor.TextMate.Tests/Themes/EditorColorDefaultsTests.cs` — create (the Themes tests live in this project, as `ThemeColorTests`)

## Design decisions (already made — do not change)

- Only the keys above. The table entries are copied from the files (values and references), one line each, with the
  source line in a comment. `Color.white` / `Color.black` are `#FFFFFF` / `#000000`.
- A reference (`'editorActiveLineNumber.foreground'`, `'focusBorder'`…) is resolved through the theme's colors first,
  then through that id's own default — exactly `resolveColorValue` → `theme.getColor(id)` with `useDefault = true`.
- `null` defaults stay `null` (`editor.lineHighlightBackground`).

## Tests

No upstream unit tests. New:

- [x] `Defaults_DarkTheme_MatchVsCode` (each key, values from the files)
- [x] `Defaults_ReferenceToAnotherColor_UsesTheThemeValueFirst` (`editorLineNumber.activeForeground` with a theme that sets `editorActiveLineNumber.foreground`)
- [x] `ThemeType_FromUiTheme`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Loading theme files (P4-40).

## Suggested commit message

`feat(themes): add theme types and VS Code's default editor colors`
