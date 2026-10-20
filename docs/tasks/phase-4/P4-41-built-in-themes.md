# P4-41 — Themes: the ten built-in VS Code themes and `EditorTheme`'s public API

- **Status:** done
- **Depends on:** P4-40
- **Size:** M

## Goal

LoomEditor ships VS Code's ten default themes. `EditorTheme` carries the theme type and the token color rules,
`EditorTheme.Load` loads any VS Code theme JSON, `EditorTheme.BuiltIn` lists the defaults, and `EditorTheme.Dark` /
`EditorTheme.Light` are Dark Modern / Light Modern.

## Sources

| File | What to copy or port |
|---|---|
| vscode: `extensions/theme-defaults/themes/*.json` | copy verbatim: `dark_vs.json`, `dark_plus.json`, `dark_modern.json`, `2026-dark.json`, `light_vs.json`, `light_plus.json`, `light_modern.json`, `2026-light.json`, `hc_black.json`, `hc_light.json` |
| vscode: `extensions/theme-defaults/package.json` | `contributes.themes`: id, label (the English text of `package.nls.json`), `uiTheme`, path |
| vscode: `extensions/theme-defaults/` license | MIT (already in `NOTICE.md` as part of VS Code: check, add a line naming the theme files) |

## Files to create or change

- `src/LoomEditor.Themes/BuiltIn/*.json` — copy; `EmbeddedResource` items with `LogicalName`s (P4-01 findings); `.gitattributes` `-text`
- `src/LoomEditor.Themes/BuiltInThemes.cs` — create (internal: the ten entries and the resource reader)
- `src/LoomEditor.Themes/EditorTheme.cs` — change (public API below)
- `src/LoomEditor.Themes/PublicAPI.Unshipped.txt` — update with the script
- `tests/LoomEditor.TextMate.Tests/Themes/BuiltInThemesTests.cs`, `EditorThemeTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0008 decisions 8 and 10. New **public** members of `EditorTheme`:
  - `ThemeType Type { get; init; }` (default `Dark`)
  - `IReadOnlyList<TokenColorRule> TokenColors { get; init; }` (default empty)
  - `static EditorTheme Load(string json, string uiTheme, string name, Func<string, string?>? readInclude = null)`
  - `static IReadOnlyList<EditorTheme> BuiltIn { get; }` (the ten, in `package.json` order)
  - `Dark` = "Dark Modern", `Light` = "Light Modern" (loaded once, `static readonly` lazy, immutable)
- The existing properties (`Background`, `Foreground`, …) are filled from the theme's `colors` with the P4-39 defaults.
- The token color map (P4-40) is an **internal** property computed once per instance (lazy).
- Existing tests that compare `EditorTheme.Dark` values keep passing if the values are the same; a test that checks
  the old name `"Dark"` is updated (it is not a ported test).

## Tests

New:

- [x] `BuiltIn_TenThemes_AllLoad` (theory: each has a name, a type, a background and token rules)
- [x] `DarkModern_Background_Is1F1F1F` and `LightModern_Background_IsFFFFFF` (from the JSON files)
- [x] `DarkModern_InheritsDarkPlusTokenColors` (a keyword rule from `dark_plus.json` is present)
- [x] `Load_CustomTheme_WithInclude`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `./eng/Update-PublicApi.ps1 src/LoomEditor.Themes` run, diff checked
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Applying the theme to grammars (P4-42) and to the control (P4-45).

## Suggested commit message

`feat(themes): ship VS Code's default themes`
