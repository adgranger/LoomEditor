# P5-33 — Bracket pairs: legacy `FindPrevBracket`, `FindNextBracket`, `FindEnclosingBrackets` (no tree)

- **Status:** done
- **Depends on:** P5-32
- **Size:** M

## Goal

The last three legacy paths of the bracket pairs part are ported, so every bracket query works on documents too large
for the tree. After this card `bracketPairsImpl.ts` is complete and the bracket tests of `textModelWithTokens.test.ts`
are all ported.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `common/model/bracketPairsTextModelPart/bracketPairsImpl.ts`.

| Lines | What to port |
|---|---|
| 520–595 | `findPrevBracket`, the legacy part after the tree branch |
| 606–682 | `findNextBracket`, the legacy part |
| 699–818 | `findEnclosingBrackets`, the legacy part (`savedCounts`, `resetCounts`, `searchInRange`) |
| 820–837 | `_toFoundBracket` |

Test: `test/common/model/textModelWithTokens.test.ts` 35–160, suite `TextModelWithTokens` › `brackets1` (with its
helpers `testBrackets` and `toRelaxedFoundBracket`).

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/BracketPairsTextModelPart.LegacyFind.cs` — create (partial)
- `src/LoomEditor.Core/Model/BracketPairs/BracketPairsTextModelPart.cs` — change: the three `else` branches
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/TextModelWithTokensBracketsTests.cs` — create

## Design decisions (already made — do not change)

- Same mappings as P5-32 (`RichEditBrackets`, `BracketsUtils`, `IgnoreBracketsInToken`, `SearchResult<T>`, the
  `Stopwatch`-based predicate, `ToLowerInvariant`).
- The legacy search uses the regexes of `RichEditBrackets` (`ForwardRegex`, `ReversedRegex`) per token language, and
  `_toFoundBracket` uses `BracketsNew.GetBracketInfo(text)` (step B); it returns `FoundBracket?`.
- `savedCounts: Map<string, number[]>` is a `Dictionary<string, int[]>` (lookups only); `counts` is the array of the
  current language, shared by reference as in Monaco (`counts = savedCounts.get(languageId)!`).
- `findEnclosingBrackets` checks the time predicate every 100 calls (`++totalCallCount % 100 === 0`), like P5-32.
- The test `brackets1` runs on both paths (`[Theory]` with `bool useTree`, the legacy run sets
  `MaxSupportedDocumentLength = 0`). `toRelaxedFoundBracket` compares `Range.ToString()` and the bracket info
  (reference equality of the `BracketKind` instances returned by `BracketsNew.GetBracketInfo`, as Monaco's
  `deepStrictEqual` on the same objects). The assertion message is Monaco's (`"findPrevBracket of " + line + ", " +
  column`).

## Tests

- [x] Port suite `TextModelWithTokens` › `brackets1` (tree and legacy)
- [x] New: `FindEnclosingBrackets_Legacy_NestedPairs_ReturnsInnermost` (same input as P5-31's
      `FindEnclosingBrackets_ReturnsInnermostClosedPair`; the legacy result is (closing, opening) as in Monaco)
- [x] New: `FindNextBracket_Legacy_SkipsBracketsInStrings`
- [x] New: `FindPrevBracket_Legacy_TwoLanguages_UsesLanguageOfEachToken`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `bracketPairsImpl.ts` ✅ (P5-31 … P5-33); tests row `textModelWithTokens.test.ts`
      11 / 29 — not ported: the 18 tests of `TextModel.getLineIndentGuide` (`guidesTextModelPart.ts`, phase 6)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`fixBrackets.ts` (only used by inline completions: not ported). Guides (phase 6).

## Suggested commit message

`feat(core): port the legacy bracket search of the bracket pairs part`
