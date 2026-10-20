# P5-32 — Bracket pairs: legacy `MatchBracket` and `FindMatchingBracketUp` (no tree)

- **Status:** done
- **Depends on:** P5-31
- **Size:** M

## Goal

For documents too large for the bracket pair tree (more than 5,000,000 characters), `MatchBracket` and
`FindMatchingBracketUp` work with Monaco's legacy regex search over the syntax tokens, with its time limit. The
bracket matching tests of `textModelWithTokens.test.ts` are ported and run on both paths.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `common/model/bracketPairsTextModelPart/bracketPairsImpl.ts`.

| Lines | What to port |
|---|---|
| 156–173 | `findMatchingBracketUp`, the `else` branch |
| 199–203 | `matchBracket`, the `else` branch |
| 206–237 | `_establishBracketSearchOffsets` |
| 239–312 | `_matchBracket` |
| 314–334 | `_matchFoundBracket` |
| 336–421 | `_findMatchingBracketUp` |
| 423–509 | `_findMatchingBracketDown` |
| 847–871 | `ContinueBracketSearchPredicate`, `createTimeBasedContinueBracketSearchPredicate`, `BracketSearchCanceled`, `stripBracketSearchCanceled` |

Tests: `test/common/model/textModelWithTokens.test.ts`:

| Lines | Suite › tests |
|---|---|
| 23–33, 162–172 | helpers `createTextModelWithBrackets`, `assertIsNotBracket`, `assertIsBracket` |
| 174–279 | `TextModelWithTokens - bracket matching` › `bracket matching 1`, `bracket matching 2` |
| 281–541 | `TextModelWithTokens 2` › `bracket matching 3`, `bracket matching 4`, `issue #95843: Highlighting of closing braces is indicating wrong brace when cursor is behind opening brace`, ``issue #88075: TypeScript brace matching is incorrect in `${}` strings`` |
| 623–672 | `TextModelWithTokens regression tests` › `microsoft/monaco-editor#133: Error: Cannot read property 'modeId' of undefined`, `issue #11856: Bracket matching does not work as expected if the opening brace symbol is contained in the closing brace symbol` (the two tests left by P4-32) |

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/BracketPairsTextModelPart.Legacy.cs` — create (partial: the methods above)
- `src/LoomEditor.Core/Model/BracketPairs/BracketPairsTextModelPart.cs` — change: the two `else` branches
- `tests/LoomEditor.Core.Tests/Model/Tokens/TextModelWithTokensRegressionTests.cs` — change: add the two regression tests, remove the "phase 5" comment
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/TextModelWithTokensBracketMatchingTests.cs` — create (the two suites above)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/TestHelpers/BracketAssert.cs` — create (`AssertIsNotBracket`, `AssertIsBracket`; the latter sorts both pairs with `Range.CompareUsingStarts` like Monaco)

## Design decisions (already made — do not change)

- The legacy code uses step B's `RichEditBrackets` (`ResolvedLanguageConfiguration.Brackets`, may be `null`):
  `MaxBracketLength`, `ForwardRegex`, `ReversedRegex`, `TextIsBracket`, `TextIsOpenBracket` (dictionaries keyed by the
  lower-case bracket text), `RichEditBracket` (`LanguageId`, `Index`, `Open`, `Close`, `ForwardRegex`,
  `ReversedRegex`, `IsOpen`, `IsClose`), `BracketsUtils.FindNextBracketInRange` / `FindPrevBracketInRange`, and
  `ScopedLineTokens.IgnoreBracketsInToken(StandardTokenType)` (P5-10; supports.ts 94–100). `textIsBracket[text]` returning
  `undefined` is a failed `TryGetValue` → `null`.
- **The time limit**: `Date.now()` is replaced by `Stopwatch.GetTimestamp()` / `Stopwatch.GetElapsedTime(start)`
  (phase-4 README trap 8, ADR 0009 decision 10: no `DateTime`). `ContinueBracketSearchPredicate` is a
  `Func<bool>?`; `undefined` → a predicate that always returns `true`, as Monaco.
- `BracketSearchCanceled` is not a class: methods that return `Range | null | BracketSearchCanceled` (and
  `[Range, Range] | null | BracketSearchCanceled`) return a private `readonly record struct SearchResult<T>(T? Value,
  bool Canceled)`; `stripBracketSearchCanceled` maps `Canceled` to `null`.
- `_bracket.toLowerCase()` and `text.toLowerCase()` → `ToLowerInvariant()`.
- `lineTokens.getLanguageId(i)` and `getStandardTokenType(i)` are the phase 4 `LineTokens` members.
- **Both paths are tested** with the same expectations: each ported test runs once with the default part and once with
  `model.BracketPairs.MaxSupportedDocumentLength = 0` (a `[Theory]` with `bool useTree`; the comment says so). Monaco
  only runs them on the tree path: if a test fails on the legacy path only, keep the legacy port exactly as Monaco and
  restrict that test to the tree path with a comment saying why (and list it in the card notes); never change an
  expected value.
- The multi-language tests (`issue #95843`, `issue #88075`) use a hand-written `ITokenizationSupport` returning the
  tokens of the test (`EncodedTokenizationResult` of phase 4) registered in the model's `TokenizationRegistry`, and
  the language configuration of the test registered for each language.

## Tests

- [x] Port suite `TextModelWithTokens - bracket matching` › `bracket matching 1`, `bracket matching 2`
- [x] Port suite `TextModelWithTokens 2` › `bracket matching 3`, `bracket matching 4`,
      `issue #95843: Highlighting of closing braces is indicating wrong brace when cursor is behind opening brace`,
      ``issue #88075: TypeScript brace matching is incorrect in `${}` strings``
- [x] Port suite `TextModelWithTokens regression tests` › `microsoft/monaco-editor#133: Error: Cannot read property
      'modeId' of undefined`, `issue #11856: Bracket matching does not work as expected if the opening brace symbol is
      contained in the closing brace symbol`
- [x] New: `FindMatchingBracketUp_Legacy_FindsOpeningBracket` and `FindMatchingBracketUp_Tree_FindsOpeningBracket`
      (`"{\n  (\n  )\n}"`, `FindMatchingBracketUp("}", (4, 1))` → `(1, 1, 1, 2)`)
- [x] New: `MatchBracket_Legacy_MaxDurationZero_ReturnsNull` (`"("`, 2,000 lines of `x`, then `")"`; matching the last
      `)` with the time limit `TimeSpan.Zero` is canceled at the 100th call and gives `null`; without a limit it finds
      the pair)
- [x] New: `MatchBracket_Legacy_BracketInComment_IsIgnored`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `bracketPairsImpl.ts` 🔶 (P5-31, P5-32); tests row `textModelWithTokens.test.ts`
      10 / 29 (see P5-33 for the last bracket test; the 18 `TextModel.getLineIndentGuide` tests are guides: phase 6);
      the "Tests ported in phase 4" row `2 / 4` points to this card
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Legacy `FindPrevBracket`, `FindNextBracket`, `FindEnclosingBrackets` (P5-33).

## Suggested commit message

`feat(core): port the legacy bracket matching of the bracket pairs part`
