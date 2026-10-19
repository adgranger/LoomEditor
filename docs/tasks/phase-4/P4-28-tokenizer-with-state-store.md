# P4-28 — Core model tokens: `TokenizerWithStateStore(AndTextModel)` and `DefaultBackgroundTokenizer`

- **Status:** done
- **Depends on:** P4-27
- **Size:** L — split: `TokenizerWithStateStore.cs`, `TokenizerWithStateStoreAndTextModel.cs`, `DefaultBackgroundTokenizer.cs`

## Goal

A tokenization support can be run over a model: line by line from the first invalid line, heuristically for the
viewport (guessing a start state from indentation), and in idle-time slices with Monaco's default background
tokenizer.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/common/model/textModelTokens.ts` | 23–47 | `Constants` (`CHEAP_TOKENIZATION_LENGTH_LIMIT = 2048`), `TokenizerWithStateStore` |
| vscode: same file | 49–209 | `TokenizerWithStateStoreAndTextModel` (`updateTokensUntilLine`, `getTokenTypeIfInsertingCharacter`, `tokenizeLinesAt`, `hasAccurateTokensForLine`, `isCheapToTokenize`, `tokenizeHeuristically`, `guessStartState`), `findLikelyRelevantLines` |
| vscode: same file | 426–559 | `safeTokenize`, `DefaultBackgroundTokenizer` (`handleChanges`, `_beginBackgroundTokenization`, `_backgroundTokenizeWithDeadline`, `_backgroundTokenizeForAtLeast1ms`, `_hasLinesToTokenize`, `_tokenizeOneInvalidLine`, `checkFinished`, `requestTokens`) |

## Files to create or change

- `src/LoomEditor.Core/Model/Tokens/TokenizerWithStateStore.cs`, `TokenizerWithStateStoreAndTextModel.cs`, `DefaultBackgroundTokenizer.cs` — create
- `src/LoomEditor.Core/Model/Tokens/ITokenizationScheduler.cs` — create (replaces `runWhenGlobalIdle` / `setTimeout0`)
- `src/LoomEditor.Core/Tokens/IBackgroundTokenizer.cs`, `IBackgroundTokenizationStore.cs` — create (moved from P4-29; `languages.ts` 124–159, without `setFontInfo` and `reportMismatchingTokens`; `IBackgroundTokenizer : IDisposable`). `CreateBackgroundTokenizer` on `ITokenizationSupport` stays in P4-29 (it needs `TextModel`).
- `src/LoomEditor.Core/Model/Tokens/ITokenizationModelInfo.cs` — create (see the decisions)
- `tests/LoomEditor.Core.Tests/Model/Tokens/TestHelpers/ManualTokenizationScheduler.cs` — create
- `tests/LoomEditor.Core.Tests/Model/Tokens/TokenizerWithStateStoreTests.cs`, `DefaultBackgroundTokenizerTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0008 decision 5: `ITokenizationScheduler` has `IDisposable RunWhenIdle(Action<IdleDeadline> callback)` and
  `void Post(Action callback)` (Monaco's `setTimeout0`). `IdleDeadline.TimeRemaining()` returns milliseconds.
  The control (P4-45) never uses `DefaultBackgroundTokenizer` (it has the background thread); tests drive it with
  `ManualTokenizationScheduler`.
- `safeTokenize` catches exceptions of the support (Monaco calls `onUnexpectedError`): `Debug.WriteLine` and the
  null tokens.
- `TokenizerWithStateStoreAndTextModel` reads the model through the members `TextModel` already has (`GetLineContent`,
  `GetLineCount`, `GetLineFirstNonWhitespaceColumn` or the indent helper of P3-09). Search for them; add nothing to
  `TextModel` except what is missing, and say so.
- Time is measured with `Stopwatch` (trap 8).
- `TextModel` has no language id and no "attached to an editor" flag yet (P4-29 / P4-30). The tokenizers read them through `ITokenizationModelInfo` (`string LanguageId`, `bool IsAttachedToEditor`), passed to the constructor; P4-29 makes `TextModel` provide it. Text is read from `TextModel` directly.

## Tests

No upstream unit tests. New (with a tiny `ITokenizationSupport` in the test that marks each line with its number):

- [x] `UpdateTokensUntilLine_TokenizesFromFirstInvalidLine`
- [x] `TokenizeHeuristically_UsesGuessedStartState_WhenFarFromValidStates`
- [x] `SafeTokenize_SupportThrows_ReturnsNullTokens`
- [x] `DefaultBackgroundTokenizer_RunsInIdleSlices_UntilFinished`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated; "Behavior differences": scheduler abstraction
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Plugging this into `TextModel` (P4-29).

## Suggested commit message

`feat(core): port the stateful tokenizers and the default background tokenizer`
