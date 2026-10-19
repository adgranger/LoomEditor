# P4-40 — Themes: load a VS Code color theme (`colorThemeData.ts` subset)

- **Status:** todo
- **Depends on:** P4-39
- **Size:** M

## Goal

A VS Code theme JSON file (with comments, `include`s, `colors` and `tokenColors`) becomes a loaded theme: its type,
its editor colors, its token color rules in VS Code's order (rule 0 = default foreground/background), and its token
color map.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/workbench/services/themes/common/colorThemeData.ts` | 104–171 | `get tokenColors()` (rule 0 from `editor.foreground` / `editor.background`, then the theme rules, `defaultThemeColors` when no `token.info-token` rule), `getColor` (custom colors not ported) |
| vscode: same file | 269–306, 336–338 | `getTokenColorIndex` (without semantic colors), `tokenColorMap`, `getDefault` (→ P4-39) |
| vscode: same file | 589–617, 744–760 | `ensureLoaded` / `load`, `fromExtensionTheme` (label, `uiTheme` default `vs-dark`) |
| vscode: same file | 774–833 | `_loadColorTheme` — the JSON branch: `include` first (787 the `settings` form → `convertSettings`), `colors` (`"default"` deletes an inherited key), `tokenColors` (an array; a string path is **rejected**, ADR 0008 decision 8); not `semanticTokenColors` (817–829) |
| vscode: same file | 853–878, 993–1037, 1076–1106 | `defaultThemeColors`, `TokenColorIndex`, `normalizeColor` |
| vscode: `src/vs/workbench/services/themes/common/themeCompatibility.ts` | 12–68 | `settingToColorIdMapping`, `addSettingMapping`, `convertSettings` (not the `terminal.ansi*` mappings, 70–76) |

## Files to create or change

- `src/LoomEditor.Themes/VsCodeThemeLoader.cs` (+ `VsCodeThemeLoader.Compatibility.cs`), `TokenColorIndex.cs`, `ColorNormalization.cs` — create (internal)
- `src/LoomEditor.Themes/TokenColorRule.cs` — create (**public** `sealed record TokenColorRule(IReadOnlyList<string> Scopes, ThemeColor? Foreground, ThemeColor? Background, string? FontStyle)` with XML docs; empty `Scopes` = the default rule)
- `src/LoomEditor.Themes/ThemeColor.cs` — change: `Parse` also accepts `#RGBA` (VS Code's `Color.fromHex`); update its doc and tests
- `tests/LoomEditor.TextMate.Tests/Themes/VsCodeThemeLoaderTests.cs` — create

## Design decisions (already made — do not change)

- JSON is read with `JsonNode.Parse` and `JsonDocumentOptions { CommentHandling = Skip, AllowTrailingCommas = true }`
  (VS Code's tolerant `Json.parse`). No serializer.
- Includes are read through `Func<string, string?> readFile` (path relative to the including file, `/` separators);
  a missing include or a parse error throws `FormatException` with the path (VS Code rejects the theme).
- `normalizeColor` returns `#RRGGBB` or `#RRGGBBAA` upper case and drops an `FF` alpha, as upstream; the color map is
  `IReadOnlyList<ThemeColor>` with index 0 unused (`TokenColorIndex.AsArray`).
- `fontStyle` strings are kept as written (vscode-textmate parses them, P4-08).

## Tests

No upstream unit tests. New (inline JSON in the tests):

- [ ] `Load_Include_ParentRulesComeFirst`
- [ ] `Load_ColorDefault_RemovesInheritedColor`
- [ ] `Load_CommentsAndTrailingCommas_AreAccepted`
- [ ] `TokenColors_FirstRuleIsTheDefaultForegroundAndBackground`
- [ ] `TokenColors_NoInfoTokenRule_AppendsDefaultThemeColors`
- [ ] `TokenColorIndex_SameColorTwice_SameId_AndIdsStartAtOne`
- [ ] `Load_SettingsForm_ConvertsScopelessSettingsToColors`
- [ ] `Load_TokenColorsAsPath_IsRejected`
- [ ] `ThemeColor_Parse_FourDigits`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `./eng/Update-PublicApi.ps1 src/LoomEditor.Themes` run, diff checked
- [ ] `docs/PORTING.md` map updated; "Behavior differences": no `.tmTheme`, no semantic token colors
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The built-in theme files and `EditorTheme`'s new API (P4-41).

## Suggested commit message

`feat(themes): load VS Code color themes`
