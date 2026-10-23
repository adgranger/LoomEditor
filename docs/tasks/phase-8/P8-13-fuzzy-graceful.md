# P8-13 — The graceful fuzzy scorers

- **Status:** todo
- **Depends on:** P8-12
- **Size:** S

## Goal

`Fuzzy.ScoreGraceful` and `Fuzzy.ScoreGracefulAggressive` match a pattern that has a typo (swapped neighbouring letters), like VS Code's `editor.suggest.filterGraceful` (on by default).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/base/common/filters.ts` | 921–979 | `fuzzyScoreGracefulAggressive`, `fuzzyScoreGraceful`, `fuzzyScoreWithPermutations`, `nextTypoPermutation` |
| `src/vs/base/test/common/filters.test.ts` | 559–570 (`fuzzyScoreGraceful`), 606–609 (`Suggestion is not highlighted #85826`) | tests |

## Files to create or change

- `src/LoomEditor.Core/Filters/Fuzzy.Graceful.cs` — create (`partial` of `Fuzzy`)
- `tests/LoomEditor.Core.Tests/Filters/FuzzyGracefulTests.cs` — create (it shares the `assertMatches` helper of P8-12: move it to `tests/LoomEditor.Core.Tests/Filters/TestHelpers/FuzzyAssert.cs` as `internal static class FuzzyAssert { public static void Matches(...) }` and make both test files use it; the P8-12 tests must still pass unchanged otherwise)

## Design decisions (already made — do not change)

- `public static int[]? ScoreGraceful(string pattern, string lowPattern, int patternPos, string word, string lowWord, int wordPos, FuzzyScoreOptions? options = null)` → `ScoreWithPermutations(…, aggressive: false, options)`; `ScoreGracefulAggressive(…)` → `aggressive: true`. A delegate type
  `internal delegate int[]? FuzzyScorer(string pattern, string lowPattern, int patternPos, string word, string lowWord, int wordPos, FuzzyScoreOptions? options);` in `FuzzyScore.cs`'s folder (Monaco `FuzzyScorer`), so `CompletionModel` (P8-15) can hold either scorer in a variable.
- `ScoreWithPermutations` is Monaco's `fuzzyScoreWithPermutations` line by line: `top = Score(…)`; `if (top != null && !aggressive) return top;` `if (pattern.Length >= 3)` try at most `tries = Math.Min(7, pattern.Length - 1)` permutations: for `movingPatternPos` from `patternPos + 1` while `< tries`: `newPattern = NextTypoPermutation(pattern, movingPatternPos)`; if not null, `candidate = Score(newPattern, newPattern.ToLowerInvariant(), patternPos, word, lowWord, wordPos, options)`;
  if not null: `candidate[0] -= 3` (the permutation penalty) and `if (top == null || candidate[0] > top[0]) top = candidate`. Return `top`. `NextTypoPermutation`: null when `patternPos + 1 >= pattern.Length` or the two characters are equal; else the pattern with the two characters swapped (`string.Concat(pattern.AsSpan(0, patternPos), pattern[patternPos + 1], pattern[patternPos], pattern.AsSpan(patternPos + 2))`-style: keep it a plain string concatenation).

## Tests

- [ ] Port (comment `// Monaco: '<name>'`): `fuzzyScoreGraceful` and `Suggestion is not highlighted #85826` (both calls: `fuzzyScore` and `fuzzyScoreGracefulAggressive`). Expected strings unchanged. Search the Monaco test file for `Graceful`: no other test uses it.
- [ ] New: `ScoreGraceful_ReturnsPlainScoreWhenItMatches` (same array contents as `Score` for `"cno"` / `"console"`), `ScoreGracefulAggressive_PatternShorterThanThree_NoPermutations` (`"cn"` gives the same as `Score`), `NextTypoPermutation_EqualNeighbours_Null` (through an `internal` test hook: make `NextTypoPermutation` `internal static`),
      `NextTypoPermutation_SwapsNeighbours` (`"cnoso"`, 1 → `"conso"`), `NextTypoPermutation_AtEnd_Null`, `ScoreGraceful_PermutationPenalty` (for `"rlut"` / `"result"`: the score of the permuted match is the `Score` of the permuted pattern minus 3)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: the graceful functions of `filters.ts`; tests ported with counts (P8-12 + P8-13 together = the fuzzy tests of the file)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Using the scorers (P8-15).

## Suggested commit message

`feat(core): port the graceful fuzzy scorers`
