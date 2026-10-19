# P4-33 — `TextMateTokenizationSupport` and `TokenizationSupportWithLineLimit`

- **Status:** done
- **Depends on:** P4-32
- **Size:** S

## Goal

A vscode-textmate grammar is a Monaco `ITokenizationSupport`: it tokenizes a line into encoded tokens with a 500 ms
limit, keeps the start state when it stops early, reports embedded languages, and gives null tokens for lines of
20 000 characters or more.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/workbench/services/textMate/browser/tokenizationSupport/textMateTokenizationSupport.ts` | 14–94 | `TextMateTokenizationSupport` (`onDidEncounterLanguage`, constructor, `getInitialState`, `tokenize` throws, `createBackgroundTokenizer`, `tokenizeEncoded`) — **not** the time telemetry (51–60) |
| vscode: `src/vs/workbench/services/textMate/browser/tokenizationSupport/tokenizationSupportWithLineLimit.ts` | 13–54 | `TokenizationSupportWithLineLimit` |
| vscode: `src/vs/editor/common/config/editorConfigurationSchema.ts` | 94–98 | `editor.maxTokenizationLineLength` default `20_000` |

## Files to create or change

- `src/LoomEditor.TextMate/Tokenization/TextMateTokenizationSupport.cs`, `TokenizationSupportWithLineLimit.cs` — create
- `src/LoomEditor.TextMate/Grammar/StateStack.cs` — change: `StateStack : LoomEditor.Core.Tokens.IState`
- `tests/LoomEditor.TextMate.Tests/Tokenization/TextMateTokenizationSupportTests.cs` — create

## Design decisions (already made — do not change)

- The time limit is a constant `500` ms (`textMateTokenizationSupport.ts` 54); the line limit is a constructor
  argument (default 20 000), not an observable.
- `onDidEncounterLanguage` → `event EventHandler<int>? EncounteredLanguage` (encoded language id), raised once per
  language, as upstream.
- When `stoppedEarly`, `Debug.WriteLine` the warning and return the tokens with the **start** state (62–66).
- The start state object is returned when `state.Equals(endState)` (84–90): keep it, it lets the state store see "no change".

## Tests

New (with `TestGrammars` and a theme):

- [x] `TokenizeEncoded_ReturnsMetadataWithThemeColors`
- [x] `TokenizeEncoded_EndStateEqualToStart_ReturnsSameObject`
- [x] `TokenizeEncoded_EmbeddedLanguage_RaisesEncounteredLanguageOnce`
- [x] `TokenizeEncoded_StoppedEarly_KeepsStartState`
- [x] `LineLimit_LongLine_ReturnsNullTokens`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (workbench section created)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Creating grammars from languages (P4-34), the background tokenizer (P4-37, P4-38).

## Suggested commit message

`feat(textmate): adapt TextMate grammars to Monaco's tokenization support`

## Notes (done)

- `LoomEditor.Core` now exposes its internals to `LoomEditor.TextMate` and `LoomEditor.TextMate.Tests` (the support
  implements Core's internal `ITokenizationSupport` / `IState`).
- `StateStack` extends `IState` through default interface members; `Clone` is now `new StateStack Clone()`.
- The constructor of `TextMateTokenizationSupport` has no telemetry parameters (not ported).
