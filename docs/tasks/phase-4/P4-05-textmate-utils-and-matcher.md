# P4-05 — vscode-textmate helpers (`utils.ts`) and scope selectors (`matcher.ts`)

- **Status:** done
- **Depends on:** P4-04
- **Size:** S

## Goal

The small leaves of vscode-textmate exist: the string and regex-source helpers, and the scope-selector parser used by
injections and balanced-bracket selectors.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/utils.ts` | 46–55, 59–91, 96–176 | `basename`, `RegexSource` (`hasCaptures`, `replaceCaptures`), `OrMask` (as a comment: C# uses `[Flags]` enums), `strcmp`, `strArrCmp`, `isValidHexColor`, `escapeRegExpCharacters`, `CachedFn` |
| textmate: `src/utils.ts` | 7–44, 189–205 | **not ported**: `clone`/`mergeObjects` (raw-grammar cloning is a method of the raw types, P4-06), `containsRTL` (unused by the tokenizer) |
| textmate: `src/matcher.ts` | 5–106 | `MatcherWithPriority`, `Matcher`, `createMatchers`, `isIdentifier`, `newTokenizer` |
| textmate: `src/tests/matcher.test.ts` | 8–57 | the 27 inline cases and `nameMatcher` |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/RegexSource.cs`, `Strings.cs` (`strcmp`, `strArrCmp`, `basename`, `isValidHexColor`, `escapeRegExpCharacters`), `CachedFn.cs` — create
- `src/LoomEditor.TextMate/Grammar/Matcher.cs` — create
- `tests/LoomEditor.TextMate.Tests/Grammar/MatcherTests.cs`, `RegexSourceTests.cs` — create

## Design decisions (already made — do not change)

- Regexes of the port use `[GeneratedRegex]` with ASCII classes (phase-4/README.md, trap 4). The `RegexSource`
  capture regex is `\$(\d+)|\${(\d+):\/(downcase|upcase)}` in JavaScript: `\d` becomes `[0-9]`.
- `strcmp` is ordinal (`string.CompareOrdinal` sign: -1, 0, 1).
- `Matcher<T>` is `Func<T, bool>`; `MatcherWithPriority<T>` is a `readonly record struct (Func<T,bool> Matcher, int Priority)`;
  priority: `-1` for `L:`, `1` for `R:`, `0` otherwise.
- `CachedFn<TKey, TValue>` uses a `Dictionary` (single-threaded, like the grammar that owns it).

## Tests

- [x] Port: `'Matcher Test #' + i` for every case of `matcher.test.ts` as an xUnit theory (`// vscode-textmate: 'Matcher Test #<i>'`)
- [x] New: `RegexSource_ReplaceCaptures_DowncaseAndUpcase`, `RegexSource_HasCaptures_IsAsciiDigitsOnly`
- [x] New: `IsValidHexColor_AcceptsThreeFourSixEightDigits`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map updated (vscode-textmate section created)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Raw grammars (P4-06), theme (P4-08).

## Suggested commit message

`feat(textmate): port vscode-textmate utils and scope matchers`
