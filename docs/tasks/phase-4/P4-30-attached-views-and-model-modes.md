# P4-30 — `TextModel` tokenization (2/2): attached views, size limits, `model.modes.test.ts`

- **Status:** done
- **Depends on:** P4-29
- **Size:** M

## Goal

Views attached to a model report their visible lines, and the model tokenizes those lines first (heuristically when
the background has not reached them). Very large models are not tokenized. Monaco's `model.modes.test.ts` passes.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/common/model/tokens/abstractSyntaxTokenBackend.ts` | 24–136 | `AttachedViews` (`attachView`, `detachView`), `AttachedViewState`, `AttachedViewImpl` (`setVisibleLines`), `AttachedViewHandler` (`update` rate-limited, `handleStateChange`) |
| vscode: `src/vs/editor/common/tokens/common.ts` | 6–22 | `RateLimiter` |
| vscode: `src/vs/editor/common/model/tokens/tokenizerSyntaxTokenBackend.ts` | 185–188, 223–256 | `handleDidChangeAttached`, `refreshAllVisibleLineTokens`, `refreshRanges`, `refreshRange` |
| vscode: `src/vs/editor/common/model.ts` | 1452–1459 | `IAttachedView` |
| vscode: `src/vs/editor/common/model/textModel.ts` | 188–190, 338–347, 610, 619 | `LARGE_FILE_SIZE_THRESHOLD`, `LARGE_FILE_LINE_COUNT_THRESHOLD`, `isTooLargeForTokenization`, `onBeforeAttached` / `onBeforeDetached` |
| vscode: `src/vs/editor/test/common/model/model.modes.test.ts` | 19–269 | both suites |

## Files to create or change

- `src/LoomEditor.Core/Model/Tokens/AttachedViews.cs`, `RateLimiter.cs`, `IAttachedView.cs` — create
- `src/LoomEditor.Core/Model/Tokens/TokenizerSyntaxTokenBackend.cs`, `TokenizationTextModelPart.cs` — change
- `src/LoomEditor.Core/Model/TextModel.Tokenization.cs` — change (`AttachView`, `DetachView`, `IsTooLargeForTokenization`)
- `tests/LoomEditor.Core.Tests/Model/Tokens/ModelModesTests.cs` — create

## Design decisions (already made — do not change)

- `AttachView()` returns an `IAttachedView` (`SetVisibleLines(IReadOnlyList<(int StartLineNumber, int EndLineNumber)> ranges, bool stabilized)`, `IDisposable`).
- `RateLimiter` uses `Stopwatch`; the delayed refresh of `AttachedViewHandler` goes through the model's
  `ITokenizationScheduler.Post` (P4-28), never a timer of its own.
- `isTooLargeForTokenization` is computed once at creation from the length and the line count, as Monaco.

## Tests

- [x] Port: `'Editor Model - Model Modes 1'` › `'model calls syntax highlighter 1'`, `'… 2'`, `'model caches states'`,
      `'model invalidates states for one line insert'`, `'… for many lines insert'`, `'… for one new line'`,
      `'… for one line delete'`, `'… for many lines delete'`
- [x] Port: `'Editor Model - Model Modes 2'` › `'getTokensForInvalidLines one text insert'`, `'… two text insert'`,
      `'… one multi-line text insert, one small text insert'`, `'… one delete text'`, `'… one line delete text'`,
      `'… multiple lines delete text'`
- [x] New: `SetVisibleLines_TokenizesVisibleLinesFirst`
- [x] New: `TooLargeModel_IsNotTokenized`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map and ported-tests list updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The view model calling `SetVisibleLines` (P4-43).

## Suggested commit message

`feat(core): tokenize the visible lines first`

## Notes (done)

- `AttachedViewHandler` replaces Monaco's 50 ms `RunOnceScheduler` by `ITokenizationScheduler.Post` (a newer state
  supersedes the pending refresh); without a scheduler it refreshes at once. `RateLimiter` is ported as the card says
  but nothing uses it yet.
- `LineTokens.ConvertToEndOffset` returns on an empty array (the model-modes supports return no tokens; JS ignores the
  write at index -2).
- `TextModel.IsAttachedToEditor` now counts attached views; the P4-29 flag on the part is gone.
