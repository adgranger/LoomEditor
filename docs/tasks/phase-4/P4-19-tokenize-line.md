# P4-19 — Grammar (6/6): the tokenization loop, captures, while conditions, `tokenizeLine(2)`

- **Status:** done
- **Depends on:** P4-18
- **Size:** L — split by concern: `TokenizeString.cs` (loop), `TokenizeString.Captures.cs`, `TokenizeString.While.cs`, `Grammar.Tokenize.cs`

## Goal

A grammar tokenizes a line: `Grammar.TokenizeLine(line, prevState, timeLimitMs)` returns scope tokens and the end
state, `TokenizeLine2` returns binary tokens (metadata) and the end state, with the time limit and `StoppedEarly`.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/grammar/tokenizeString.ts` | 31–338 | `_tokenizeString` (while-condition check, the `while (!STOP)` loop with the time limit, `scanNext`: no match, end rule and pop with the "pushed and popped without advancing" guard 149–161, push `BeginEndRule`, push `BeginWhileRule`, `MatchRule`, advance) |
| textmate: `src/grammar/tokenizeString.ts` | 345–411 | `_checkWhileConditions`, `IWhileCheckResult` |
| textmate: `src/grammar/tokenizeString.ts` | 575–664 | `handleCaptures` (nested captures, `retokenizeCapturedWithRuleId`), `LocalStackElement` |
| textmate: `src/grammar/grammar.ts` | 277–412 | `Grammar.tokenizeLine`, `tokenizeLine2`, `_tokenize` (initial state, root scope, the `\n` appended to the line, `disposeOnigString`) |
| textmate: `src/main.ts` | 200–217 | `IGrammar` |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/TokenizeString.cs`, `TokenizeString.Captures.cs`, `TokenizeString.While.cs` — create
- `src/LoomEditor.TextMate/Grammar/Grammar.Tokenize.cs`, `IGrammar.cs` — create
- `tests/LoomEditor.TextMate.Tests/Grammar/TokenizeLineTests.cs` — create

## Design decisions (already made — do not change)

- The time limit uses `Stopwatch.GetTimestamp()` (trap 8); `timeLimit = 0` means no limit, as upstream.
- `IGrammar` has `TokenizeLine` and `TokenizeLine2` only (no `fonts` in the result, ADR 0008 decision 3).
- `handleCaptures` slices captured text with `Substring(start, length)` (phase-3 trap 6) and keeps its `localStack`
  as a `List<LocalStackElement>`.
- The anchor position handling (`anchorPosition`, `isFirstLine`) is upstream's; do not simplify.

## Tests

New, with `TestGrammars` (the vscode-textmate suites come in P4-21 and P4-22):

- [x] `TokenizeLine_SimpleMatchRule_ProducesScopes`
- [x] `TokenizeLine_BeginEndAcrossLines_KeepsStateAndContentName`
- [x] `TokenizeLine_BeginWhile_EndsWhenWhileFails`
- [x] `TokenizeLine_NestedCaptures_ProduceNestedScopes`
- [x] `TokenizeLine_CaptureWithPatterns_IsRetokenized`
- [x] `TokenizeLine_EmptyMatchAtSamePosition_DoesNotLoopForever`
- [x] `TokenizeLine2_BinaryTokens_UseThemeColors` (a theme from P4-09)
- [x] `TokenizeLine_TimeLimitReached_StopsEarly` (a pathological grammar, time limit 1 ms; check `StoppedEarly`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The registry (P4-20), the vscode-textmate data-driven suites (P4-21, P4-22).

## Suggested commit message

`feat(textmate): tokenize lines with a TextMate grammar`
