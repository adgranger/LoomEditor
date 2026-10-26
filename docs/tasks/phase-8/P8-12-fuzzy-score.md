# P8-12 — The fuzzy filter of completion: `FuzzyScore`, `fuzzyScore`, `anyScore`, `createMatches`

- **Status:** done
- **Depends on:** P8-11
- **Size:** M

## Goal

`Fuzzy.Score(pattern, word, …)` answers "does the typed text match this label, how well, and which characters" exactly like VS Code's completion list (`filters.ts`, the fuzzy part), so the list ranks and highlights like Monaco. The graceful variants are P8-13.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/base/common/filters.ts` | 12–28 (`IMatch`), 487–519 (`anyScore`, `createMatches`), 521–548 (`_maxLen`, `initTable`, `initArr`, the static tables), 578–660 (`isSeparatorAtPos`, `isWhitespaceAtPos`, `isUpperCaseAtPos`, `isPatternInWord`, `Arrow`), 656–681 (`FuzzyScore`, `FuzzyScoreOptions`, `FuzzyScorer`), 683–919 (`fuzzyScore`, `_fillInMaxWordMatchPos`, `_doScore`) | the algorithm |
| `src/vs/base/common/strings.ts` | 753–764 | `isEmojiImprecise` |
| `src/vs/base/test/common/filters.test.ts` | 273–290 (`assertMatches`), 293–557 and 571–653 (all the tests except `'fuzzyScoreGraceful'` at 559 and `'Suggestion is not highlighted #85826'` at 606, which are P8-13) | tests |

(The list of tests to port is in the Tests section; the `Graceful` ones are P8-13.)

## Files to create or change

- `src/LoomEditor.Core/Filters/FuzzyScore.cs` — create (`internal static class FuzzyScore`: `Default`, `IsDefault`; `internal sealed record FuzzyScoreOptions`)
- `src/LoomEditor.Core/Filters/Fuzzy.cs` — create (`internal static partial class Fuzzy`: `Score`, `IsPatternInWord`, `AnyScore`, `CreateMatches`); `src/LoomEditor.Core/Filters/Fuzzy.DoScore.cs` — create (the table fill, `_doScore`)
- `src/LoomEditor.Core/Filters/FilterMatch.cs` — create (`internal readonly record struct FilterMatch(int Start, int End)`, Monaco `IMatch`)
- `src/LoomEditor.Core/Filters/CharacterClasses.cs` — create (`IsSeparatorAtPos`, `IsWhitespaceAtPos`, `IsEmojiImprecise`)
- `tests/LoomEditor.Core.Tests/Filters/FuzzyScoreTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.Core.Filters`, everything `internal` (View reaches it through `InternalsVisibleTo`). Header on every file: `// Ported from microsoft/vscode: src/vs/base/common/filters.ts (…)`.
- **Representation.** Monaco's `FuzzyScore` is the JavaScript array `[score, wordStart, ...matchPositions]` and callers mutate its first element (`candidate[0] -= 3`, `item.score[0] = match[0]`). Keep it: a `FuzzyScore` is an **`int[]`**, same layout (`score`, `wordStart`, then match positions in the order Monaco stores them: last match first).
  `FuzzyScore.Default` is `static readonly int[] Default = [-100, 0]` (callers must treat it as read-only; document it), `FuzzyScore.IsDefault(int[]? score)` ports `isDefault` (`score is null || (score.Length == 2 && score[0] == -100 && score[1] == 0)`).
  `FuzzyScoreOptions(bool FirstMatchCanBeWeak, bool BoostFullMatch)` is a `sealed record` with `static readonly Default = new(FirstMatchCanBeWeak: false, BoostFullMatch: true)` (Monaco `FuzzyScoreOptions.default = { boostFullMatch: true, firstMatchCanBeWeak: false }`).
- **API** (all `public static` on `internal static partial class Fuzzy`): `int[]? Score(string pattern, string patternLow, int patternStart, string word, string wordLow, int wordStart, FuzzyScoreOptions? options = null)` (port of `fuzzyScore`; `options ?? FuzzyScoreOptions.Default`),
  `bool IsPatternInWord(string patternLow, int patternPos, int patternLen, string wordLow, int wordPos, int wordLen, bool fillMinWordPosArr = false)`, `int[] AnyScore(string pattern, string lowPattern, int patternPos, string word, string lowWord, int wordPos)` (returns a **new** array each call: `[0, wordPos]` when nothing matches),
  `IReadOnlyList<FilterMatch> CreateMatches(int[]? score)` (null → empty list).
- **The tables are static and shared** (`_minWordMatchPos`, `_maxWordMatchPos`, `_diag`, `_table`, `_arrows`, sizes as Monaco: `_maxLen = 128`, arrays of `2 * _maxLen`): `Score` is **not thread-safe**; the `<remarks>` say "UI thread only". The `_debug` printing is not ported.
  `Arrow` becomes `enum Arrow : byte { Diag = 1, Left = 2, LeftLeft = 3 }` and `_arrows` a `Arrow[][]`; `_table` and `_diag` are `int[][]`; `initTable` creates `_maxLen + 1` rows of `_maxLen + 1` ints, exactly as Monaco's `initTable`.
- **JavaScript traps** (the card exists to port a numeric algorithm exactly, so read these):
  1. JavaScript strings are UTF-16 and `s[i]` is a UTF-16 unit: C# `string` indexing is the same; every comparison `patternLow[patternPos] === wordLow[wordPos]` is `char == char` (ordinal).
  2. `value.codePointAt(index)` (in `isSeparatorAtPos`) returns the **full code point** at a surrogate pair: in C# `char.IsHighSurrogate(value[index]) && index + 1 < value.Length ? char.ConvertToUtf32(value[index], value[index + 1]) : value[index]`.
     `isEmojiImprecise(code)` is then called with that code point: port the ranges of `strings.ts` 756–764 exactly (decimal and hex mixed in the source: keep the same numbers).
  3. `isUpperCaseAtPos(pos, word, wordLow)` is `word[pos] !== wordLow[pos]` (a character differs from its lower-cased form): `word[pos] != wordLow[pos]`. `wordLow` / `patternLow` are produced by the **caller** with `ToLowerInvariant()` (Monaco `toLowerCase()`); document it, and use `ToLowerInvariant` in the tests' `assertMatches`.
  4. `Math.min` / `Math.max` of mixed `number`s are `int` here; the score arithmetic stays in `int` (Monaco's scores are small integers; no `double`).
  5. `undefined` returns are `null`; `FuzzyScore | undefined` is `int[]?`.
  6. The `const enum Constants`-like numeric literals in `_doScore` (1, 5, 8, …) are copied unchanged; do not "improve" them.
- No XML-doc-free public member (everything is internal, but the `<summary>` of each method says which Monaco function it is).

## Tests

- [x] Port (each test keeps its Monaco name in a comment `// Monaco: 'fuzzyScore, #23215'`; Monaco's `assertMatches(pattern, word, decoratedWord, filter, opts)` becomes a helper in the test file with the same decoration format (`^` before each matched character, built from `Fuzzy.CreateMatches`), calling `Fuzzy.Score(pattern, pattern.ToLowerInvariant(), opts.PatternPos, word, word.ToLowerInvariant(), opts.WordPos, new FuzzyScoreOptions(opts.FirstMatchCanBeWeak, BoostFullMatch: true))`;
      `decoratedWord == null` means "no match"): `fuzzyScore, #23215`, `fuzzyScore, #23332`, `fuzzyScore, #23190`, `fuzzyScore, #23581`, `fuzzyScore, #23458`, `fuzzyScore, #23746`, `fuzzyScore`, `fuzzyScore (first match can be weak)`, `fuzzyScore, many matches`,
      `Freeze when fjfj -> jfjf, https://github.com/microsoft/vscode/issues/91807`, `fuzzyScore, issue #26423`, `Fuzzy IntelliSense matching vs Haxe metadata completion, #26995`, `Separator only match should not be weak #79558`, `Cannot set property '1' of undefined, #26511`,
      `Vscode 1.12 no longer obeys 'sortText' in completion items (from language server), #26096`, `patternPos isn't working correctly #79815`, `topScore - fuzzyScore`, `Unexpected suggestion scoring, #28791`, `HTML closing tag proposal filtered out #38880`,
      `List highlight filter: Not all characters from match are highlighterd #66923`, `Autocompletion is matched against truncated filterText to 54 characters #74133`, `Wrong highlight after emoji #113404`, `IntelliSense completion not correctly highlighting text in front of cursor #115250`,
      `anyScore should not require a strong first match`, `configurable full match boost`, `Unexpected suggest highlighting ignores whole word match in favor of matching first letter#147423`
      plus `'"Go to Symbol" with the exact method name doesn't work as expected #84787'` (it calls `fuzzyScore` directly). In `assertMatches`, `anyScore` is passed as the scorer: wrap `Fuzzy.AnyScore` in a lambda that ignores the options.
      **Not ported here** (P8-13): `fuzzyScoreGraceful` and `Suggestion is not highlighted #85826` (it also calls `fuzzyScoreGracefulAggressive`). Search the file for `Graceful` to double-check.
- [x] New: `Default_IsMinus100`, `IsDefault_NullAndDefault`, `AnyScore_NoMatch_ReturnsZeroAndWordPos`, `CreateMatches_Null_Empty`, `CreateMatches_MergesAdjacentPositions` (`[score, 0, 2, 1, 0]`-style input → one match `[0, 3)`; build the arrays by hand following Monaco's layout), `Score_PatternLongerThanWord_Null`, `Score_EmptyPattern_Null` (pattern start >= length), `Score_LongWord_UsesFirst128Characters`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green (**no ported test may be weakened or have its expected string changed**; if one fails, the port is wrong: re-read the Monaco function)
- [x] `docs/PORTING.md` map: the ported part of `filters.ts` (what is not ported: `matchesPrefix`, `matchesCamelCase`, `matchesWords`, `matchesFuzzy`, `or`, the graceful variants until P8-13) and `strings.isEmojiImprecise`; tests ported with counts
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`fuzzyScoreGraceful*` (P8-13), the other filters of `filters.ts`, using the scorer (P8-15).

## Suggested commit message

`feat(core): port the fuzzy score of the completion filter`
