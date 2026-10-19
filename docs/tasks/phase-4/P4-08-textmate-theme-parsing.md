# P4-08 — vscode-textmate theme (1/2): scope stacks, theme parsing, color map

- **Status:** todo
- **Depends on:** P4-07
- **Size:** M

## Goal

A raw theme (a list of `{scope, settings}` rules) is parsed into `ParsedThemeRule`s with vscode-textmate's rules, and
the color map gives each color an id.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/theme.ts` | 69–105 | `ScopeName`, `ScopePath`, `ScopePattern`, `IRawTheme`, `IRawThemeSetting` (without `fontFamily`, `fontSize`, `lineHeight`) |
| textmate: `src/theme.ts` | 107–225 | `ScopeStack`, `_scopePathMatchesParentScopes`, `_matchesScope`, `StyleAttributes` (without font attributes) |
| textmate: `src/theme.ts` | 230–387 | `parseTheme`, `ParsedThemeRule`, `FontStyle`, `fontStyleToString` |
| textmate: `src/theme.ts` | 465–510 | `ColorMap`, `emptyParentScopes` |
| textmate: `src/tests/themes.test.ts` | 347–411 | `'Theme parsing can parse'`, `'strcmp works'`, `'strArrCmp works'` |

## Files to create or change

- `src/LoomEditor.TextMate/Theme/RawTheme.cs` (`RawTheme`, `RawThemeSetting`, `RawThemeSettings`), `ScopeStack.cs`, `StyleAttributes.cs`, `ParsedThemeRule.cs`, `FontStyle.cs`, `ThemeParser.cs`, `ColorMap.cs` — create
- `tests/LoomEditor.TextMate.Tests/Theme/ThemeParsingTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0008, decision 3: no font attributes (`fontFamily`, `fontSize`, `lineHeight` are ignored when present).
- `RawThemeSetting.Scope` is `IReadOnlyList<string>?` plus the original single string (vscode-textmate accepts a
  string — split on `,` after removing leading/trailing commas — or an array).
- `FontStyle` is a `[Flags]` enum with `NotSet = -1`, `None = 0`, `Italic = 1`, `Bold = 2`, `Underline = 4`,
  `Strikethrough = 8`, as upstream.
- `ScopeStack` is immutable (a linked list: `Parent`, `ScopeName`), as upstream.
- `ColorMap` upper-cases colors and gives ids from 1 (id 0 is never used, upstream 465–508); with a given color map it
  becomes read-only (`_isFrozen`).
- The test helper that builds a raw theme from the tests' object literals is local to the test file.

## Tests

- [ ] Port: `'Theme parsing can parse'`, `'Theme resolving strcmp works'`, `'Theme resolving strArrCmp works'` (check the exact names in the file)
- [ ] New: `ScopeStack_Extends_And_GetSegments`
- [ ] New: `ColorMap_Frozen_ThrowsOnUnknownColor` (upstream throws `Missing color in color map`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated; "Behavior differences": no font attributes
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The trie and theme matching (P4-09). VS Code theme files (P4-40).

## Suggested commit message

`feat(textmate): parse TextMate themes`
