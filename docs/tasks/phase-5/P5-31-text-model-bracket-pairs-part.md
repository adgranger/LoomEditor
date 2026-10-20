# P5-31 — `TextModel.BracketPairs`: the bracket pairs part (tree path)

- **Status:** todo
- **Depends on:** P5-30
- **Size:** M

## Goal

`TextModel.BracketPairs` exists and answers Monaco's bracket API from the bracket pair tree: `GetBracketPairsInRange`,
`GetBracketPairsInRangeWithMinIndentation`, `GetBracketsInRange`, `HasUnmatchedClosingBracketAfter`,
`FindMatchingBracketUp`, `MatchBracket`, `FindPrevBracket`, `FindNextBracket`, `FindEnclosingBrackets`, and raises
`Changed`. The tree is built lazily on the first request and kept up to date on text edits, token changes,
background tokenization, language, options and language configuration changes.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/textModelBracketPairs.ts` | 13–73 | `IBracketPairsTextModelPart` (the members, as the API of the part; no interface in C#) |
| `common/model/bracketPairsTextModelPart/bracketPairsImpl.ts` | 21–133 | `BracketPairsTextModelPart`: `bracketPairsTree`, `onDidChange`, `canBuildAST`, `bracketsRequested`, constructor, the `handle…` methods, `updateBracketPairsTree`, `getBracketPairsInRange`, `getBracketPairsInRangeWithMinIndentation`, `getBracketsInRange`, `hasUnmatchedClosingBracketAfter` |
| same | 135–156, 176–198, 511–518, 597–604, 684–697 | the **tree branches** (`if (this.canBuildAST)`) of `findMatchingBracketUp`, `matchBracket`, `findPrevBracket`, `findNextBracket`, `findEnclosingBrackets` |
| same | 840–845 | `createDisposableRef` (not ported: see below) |
| `common/model/textModel.ts` | 296–297, 357, 408–411, 472–473, 713 | where the part is created, and where language configuration, content and option changes reach it |
| `common/model/tokens/tokenizationTextModelPart.ts` | 92–94, 165–170, 382 | where background tokenization, token and language changes reach it |
| `test/common/model/bracketPairColorizer/getBracketPairsInRange.test.ts` | 20–593 | the 10 tests and the helpers `createTextModelWithColorizedBracketPairs`, `bracketPairToJSON`, `PositionOffsetTransformer`, `AnnotatedDocument` |

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/BracketPairsTextModelPart.cs` — create
- `src/LoomEditor.Core/Model/TextModel.BracketPairs.cs` — change: `internal BracketPairsTextModelPart BracketPairs`, created in the constructors
- `src/LoomEditor.Core/Model/TextModel.cs` — change: create the part before the tokenization part (Monaco's order, textModel.ts 357–365)
- `src/LoomEditor.Core/Model/TextModel.DeferredEmit.cs` — change: `Deliver` calls `_bracketPairs.HandleDidChangeContent(content)` right after `_tokenization.HandleDidChangeContent(content)`
- `src/LoomEditor.Core/Model/TextModel.Tokenization.cs` — change: `RaiseTokensChanged` calls `BracketPairs.HandleDidChangeTokens(e)` first
- `src/LoomEditor.Core/Model/Tokens/TokenizationTextModelPart.cs` — change: background state change → `HandleDidChangeBackgroundTokenizationState()`; `SetLanguageId` → `HandleDidChangeLanguage(e)` before `RaiseLanguageChanged`
- `src/LoomEditor.Core/Model/TextModel.Options.cs` — change: `UpdateOptions` calls `HandleDidChangeOptions(e)` before `OptionsChanged`
- `src/LoomEditor.Core/Model/TextModel.*.cs` where P5-15 subscribes the model to `LanguageConfigurationRegistry.Changed` — change: also call `BracketPairs.HandleLanguageConfigurationServiceChange(e)` (textModel.ts 408–411: for every change, before the model's own `LanguageConfigurationChanged`)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/GetBracketPairsInRangeTests.cs` — create
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/TestHelpers/AnnotatedDocument.cs` — create (`AnnotatedDocument`, `PositionOffsetTransformer` of the test file, 536–593)

## Design decisions (already made — do not change)

- **When the tree is used (checked in the pinned source).** Monaco does **not** tie the tree to colorization: it uses
  the tree whenever `canBuildAST` (text length ≤ `50_000 * 100` = 5,000,000 characters), and builds it lazily on the
  first request (`bracketsRequested`). The legacy regex search runs only for larger documents. Keep exactly this.
  The limit is an `internal int MaxSupportedDocumentLength { get; set; } = 50_000 * 100;` of the part (instance, not
  static) so tests can force the legacy path (P5-32, P5-33).
- Until P5-32 and P5-33, the `else` (legacy) branches return `null` (`FindMatchingBracketUp`, `MatchBracket`,
  `FindPrevBracket`, `FindNextBracket`, `FindEnclosingBrackets`), with a comment naming the card that ports them.
- `MutableDisposable<IReference<BracketPairsTree>>` and `createDisposableRef` become a nullable field
  `BracketPairsTree? _bracketPairsTree` plus the subscription to its `Changed` event (unsubscribe on clear). `clear()`
  = unsubscribe and set to `null`.
- The language configuration comes from step B: the part takes the model's `LanguageConfigurationRegistry` and calls
  `GetLanguageConfiguration(languageId)`. `LanguageConfigurationServiceChangeEvent` is P5-14's `LanguageConfigurationChangedEventArgs(string LanguageId)` (Monaco's `!e.languageId` = an empty id).
  `getLanguageIdAtPosition` is `Tokenization.GetLanguageIdAtPosition`.
- API shapes: `IPosition` parameters are `Position` (validated with `ValidatePosition`, as Monaco); `IRange` is
  `Range`; `[Range, Range] | null` is `(Range, Range)?` keeping Monaco's element order; `maxDuration?: number` (ms) is
  `TimeSpan? maxDuration = null` (used only by the legacy paths). `CallbackIterable.empty` is
  `CallbackIterable<T>.Empty`. `onDidChange` is `internal event EventHandler? Changed`.
- `matchBracket` tree branch: `filter(...).findLastMaxBy(compareBy(item => …, Range.compareRangesUsingStarts))` →
  `Filter(...).FindLastMaxBy((a, b) => Range.CompareUsingStarts(Select(a), Select(b)))` (see P5-29 for the
  "strictly greater" rule). `findEnclosingBrackets`: `item.range.strictContainsRange(range)` is the existing
  `Range.StrictContains(Range)`.
- **Event order is Monaco's**: content changes reach the part after the tokenization part and before any
  `ContentChanged` listener; token changes reach it before `TokensChanged` listeners; option changes before
  `OptionsChanged`; language changes before `LanguageChanged`.
- The step D cut points that need this part (auto-closing bracket balance, electric characters: `HasUnmatchedClosingBracketAfter`, `FindMatchingBracketUp`) are resolved by P5-46 (step D, after the bracket pair cards).

## Tests

- [ ] Port suite `Bracket Pair Colorizer - getBracketPairsInRange` › `Basic 1`, `Basic 2`, `Basic Empty`,
      `unmatched closing brackets after position`, `unmatched closing bracket query waits for background tokenization`,
      `unmatched closing brackets are language scoped`, `Basic All`, `getBracketsInRange`, `Test Error Brackets`,
      `colorizedBracketsVSBrackets` (`disposeOnReturn` → `using`/nothing; `TokenizedDocument` of P5-28;
      `createBackgroundTokenizer` returning a do-nothing tokenizer → the phase 4 `ITokenizationSupport` member
      `CreateBackgroundTokenizer` returning a no-op `IBackgroundTokenizer`; `bracketPairToJSON` → an anonymous record
      compared with `Assert.Equal`)
- [ ] New: `BracketPairs_FirstRequest_BuildsTree_AndRaisesChanged`
- [ ] New: `BracketPairs_LanguageChanged_RebuildsTree`
- [ ] New: `BracketPairs_ConfigurationOfOtherLanguage_DoesNotRebuild` (`DidLanguageChange` false)
- [ ] New: `MatchBracket_InsideBracket_ReturnsOpenThenClose`
- [ ] New: `FindEnclosingBrackets_ReturnsInnermostClosedPair`
- [ ] New: `MaxSupportedDocumentLength_Exceeded_TreeIsDropped_AndChangedRaised`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (phase 1–4 model tests untouched)
- [ ] `docs/PORTING.md` map: `bracketPairsImpl.ts` 🔶 (tree path, P5-31), `textModelBracketPairs.ts` ✅; tests row
      `getBracketPairsInRange.test.ts` 10 / 10
- [ ] Card status and `docs/tasks/README.md` updated
- [ ] `docs/tasks/phase-5/README.md` section 5: row for `AnnotatedDocument` (created by P5-31)

## Out of scope

The legacy paths (P5-32, P5-33), the cursor cut points (P5-46), colorization decorations (P5-34), guides
(`guidesTextModelPart.ts`, phase 6).

## Suggested commit message

`feat(core): add the bracket pairs part of the text model`
