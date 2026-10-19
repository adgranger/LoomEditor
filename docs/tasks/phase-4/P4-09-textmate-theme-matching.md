# P4-09 — vscode-textmate theme (2/2): resolving rules and matching scopes

- **Status:** done
- **Depends on:** P4-08
- **Size:** M

## Goal

`Theme.CreateFromRawTheme(rawTheme, colorMap)` builds vscode-textmate's trie, and `Theme.Match(scopePath)` returns the
style attributes (font style, foreground id, background id) of the most specific rule. The theme unit tests of
vscode-textmate pass.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/theme.ts` | 7–63 | `Theme` (`createFromRawTheme`, `createFromParsedTheme`, `getColorMap`, `getDefaults`, `match` with its cache) |
| textmate: `src/theme.ts` | 392–463 | `resolveParsedThemeRules` |
| textmate: `src/theme.ts` | 512–787 | `ThemeTrieElementRule`, `ITrieChildrenMap`, `ThemeTrieElement` (`_cmpBySpecificity`, `match`, `insert`, `_doInsertHere`) |
| textmate: `src/tests/themes.test.ts` | 187–346, 420–828 | the matching and resolving tests (list below) |

## Files to create or change

- `src/LoomEditor.TextMate/Theme/Theme.cs`, `ThemeTrieElement.cs`, `ThemeTrieElementRule.cs` — create
- `src/LoomEditor.TextMate/Theme/ThemeParser.cs` — change: `ResolveParsedThemeRules`
- `tests/LoomEditor.TextMate.Tests/Theme/ThemeMatchingTests.cs`, `ThemeResolvingTests.cs` — create

## Design decisions (already made — do not change)

- `resolveParsedThemeRules` sorts with a **stable** sort (`OrderBy`), phase-3 trap 3.
- `Theme.Match` caches by scope path in a `Dictionary` (single-threaded, remarks), like upstream's `CachedFn`.
- The trie children map is a `Dictionary<string, ThemeTrieElement>` (`ITrieChildrenMap`); `match` splits the scope at
  the first `.` exactly as upstream.

## Tests

- [x] Port: `'Theme matching gives higher priority to deeper matches'`, `'Theme matching gives higher priority to parent matches 1'`, `'… 2'`
- [x] Port: suite `'Theme matching can match'` (`simpleMatch1` … `simpleMatch25`, `defaultMatch1` … `3`, `multiMatch1`, `multiMatch2`)
- [x] Port: `'Theme matching Microsoft/vscode#23460'`
- [x] Port: every `'Theme resolving …'` test of lines 420–828 (always has defaults, respects incoming defaults 1–5,
      can merge incoming defaults, defaults are inherited, same rules get merged, rules are inherited 1–2, rules with
      parent scopes, child combinator, deeper scopes #233, issue #38 invalid colors, issue #35 trailing comma)
- [x] Not ported (write why in a comment in the test file): the data-driven theme tests (73–123) and the timing tests
      (125, 129) — they need 11 MB of test data and older grammars; the golden tests (P4-48) cover the same path.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map and the list of tests not ported updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Encoding the attributes into token metadata (P4-10).

## Suggested commit message

`feat(textmate): resolve and match TextMate theme rules`
