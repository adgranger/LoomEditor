# P4-28 — Core model tokens: `TokenizerWithStateStore(AndTextModel)` and `DefaultBackgroundTokenizer`

- **Status:** todo
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

## Tests

No upstream unit tests. New (with a tiny `ITokenizationSupport` in the test that marks each line with its number):

- [ ] `UpdateTokensUntilLine_TokenizesFromFirstInvalidLine`
- [ ] `TokenizeHeuristically_UsesGuessedStartState_WhenFarFromValidStates`
- [ ] `SafeTokenize_SupportThrows_ReturnsNullTokens`
- [ ] `DefaultBackgroundTokenizer_RunsInIdleSlices_UntilFinished`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated; "Behavior differences": scheduler abstraction
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Plugging this into `TextModel` (P4-29).

## Suggested commit message

`feat(core): port the stateful tokenizers and the default background tokenizer`
