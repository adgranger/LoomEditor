# P5-66 — `ReplacePattern` and case-preserving replace

- **Status:** todo
- **Depends on:** P5-65
- **Size:** M

## Goal

A replace string typed in the find widget becomes the text that replaces each match, with Monaco's rules: `$0`, `$&`,
`$1`…`$99` with JavaScript's "group exists" semantics, `\n`, `\t`, `\\`, the case operators `\u \U \l \L`, and "preserve
case" (`buildReplaceStringWithCasePreserved`). No `Regex.Replace` anywhere.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/find/browser/replacePattern.ts` | 1–347 | `ReplacePatternKind`, `StaticValueReplacePattern`, `DynamicPiecesReplacePattern`, `ReplacePattern` (`fromStaticValue`, `hasReplacementPatterns`, `buildReplaceString`, `_substitute`), `ReplacePiece`, `ReplacePieceBuilder`, `parseReplaceString` |
| `src/vs/base/common/search.ts` | 1–48 | `buildReplaceStringWithCasePreserved`, `validateSpecificSpecialCharacter`, `buildReplaceStringForSpecificSpecialCharacter` |
| `src/vs/base/common/strings.ts` | 947–957 | `containsUppercaseCharacter` |
| `contrib/find/test/browser/replacePattern.test.ts` | 11–254 | suite `Replace Pattern test` |

## Files to create or change

- `src/LoomEditor.View/Features/Find/ReplacePattern.cs` — create (`ReplacePattern`, `ReplacePiece`, `parseReplaceString` as
  `ReplacePattern.Parse`; `ReplacePieceBuilder` private nested; split into `ReplacePattern.Parse.cs` if > 400 lines)
- `src/LoomEditor.Core/Text/SearchStrings.cs` — create: `internal static class SearchStrings` with
  `BuildReplaceStringWithCasePreserved` (port of `base/common/search.ts`; Core/Text holds `base/common` ports)
- `src/LoomEditor.Core/Text/Strings.Regex.cs` — change: add `ContainsUppercaseCharacter(string target, bool ignoreEscapedChars = false)`
- `tests/LoomEditor.View.Tests/Features/Find/ReplacePatternTests.cs` — create
- `tests/LoomEditor.View.Tests/Features/Find/TestHelpers/RegexMatches.cs` — create: `string?[]? Exec(string target, string pattern, RegexOptions options = None)`
  — the JavaScript `regex.exec(target)` of the tests (first match, groups with `null` for groups that did not match, `null` when no match)

## Design decisions (already made — do not change)

- `matches: string[] | null` → `IReadOnlyList<string?>?` (the `FindMatch.Matches` of P5-62). `_substitute` treats a
  `null` group like Monaco treats `undefined` (empty string, issue #19740).
- `toUpperCase` / `toLowerCase` → `ToUpperInvariant` / `ToLowerInvariant`; `str[0]` / `substr(1)` on the UTF-16 string as
  Monaco does. Behavior difference (XML doc + PORTING): the few characters whose JavaScript upper case is longer
  (`ß` → `SS`) keep their length in .NET.
- `containsUppercaseCharacter` with `ignoreEscapedChars` removes `\\.` with a loop (no regex): drop every `\` and the
  character after it.
- `split(specialCharacter)` → `string.Split(char)` (same count, empty entries kept).
- `ReplacePattern` is `internal sealed class`; `Parse` and `FromStaticValue` are static factories.

## Tests

`replacePattern.test.ts`, suite `Replace Pattern test` — port all 8 with `// Monaco:` names:

- [ ] `parse replace string`, `parse replace string with case modifiers`
- [ ] `replace has JavaScript semantics` — Monaco computes the expected value with `'hi'.replace(...)` at run time; write
      the JavaScript results as literals: `"hello"`, `"\t"`, `"\n"`, `"\\t"`, `"\\n"`, `"hellohi"`, `"hellohi"`, `"hellohi1"`,
      `"hellohi1"`, `"hellohi0"` (`$10`, one group), `"hello"` (`$10`, ten groups), `"hellohi00"` (`$100`, one group),
      `"hello$20"` (`$20`, one group)
- [ ] `get replace string if given text is a complete match`, `get replace string if match is sub-string of the text`
- [ ] `issue #19740 Find and replace capture group/backreference inserts \`undefined\` instead of empty string`
- [ ] `buildReplaceStringWithCasePreserved test`, `preserve case`
- JavaScript regex literals become `.NET` patterns with the same text and `RegexOptions.CultureInvariant` (`/i` →
  `IgnoreCase`); all patterns in this file mean the same in both engines.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map rows `contrib/find/browser/replacePattern.ts` ✅ (P5-66), `base/common/search.ts` ✅,
      `strings.ts` (+ `containsUppercaseCharacter`); tests table `replacePattern.test.ts` 8 / 8; the `ß` difference
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Running a replace on the model (P5-69).

## Suggested commit message

`feat(view): port the find replace pattern and case-preserving replace`
