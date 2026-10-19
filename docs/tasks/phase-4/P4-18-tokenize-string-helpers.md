# P4-18 — Grammar (5/6): `tokenizeString.ts` rule and injection matching

- **Status:** done
- **Depends on:** P4-17
- **Size:** S

## Goal

The matching functions that the tokenization loop calls exist: match the current rule at a position, match the
injections, choose between them, and prepare the compiled rule for a search (with the `\A` / `\G` variants).

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/grammar/tokenizeString.ts` | 12–17 | `TokenizeStringResult` |
| textmate: `src/grammar/tokenizeString.ts` | 413–542 | `matchRuleOrInjections`, `IMatchResult`, `matchRule`, `matchInjections`, `IMatchInjectionsResult` |
| textmate: `src/grammar/tokenizeString.ts` | 544–573 | `prepareRuleSearch`, `prepareRuleWhileSearch`, `getFindOptions` (kept, unused: `UseOnigurumaFindOptions = false`) |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/TokenizeString.Match.cs` — create (`static partial class TokenizeString`; P4-19 adds the other parts)
- `tests/LoomEditor.TextMate.Tests/Grammar/TokenizeStringMatchTests.cs` — create

## Design decisions (already made — do not change)

- A `static partial class TokenizeString` (upstream's module).
- `matchInjections` keeps upstream's comparison exactly: best start position first, then priority (`-1` for `L:`
  wins a tie, `1` for `R:` loses it). `matchRuleOrInjections` takes the injection when it starts strictly before the
  rule's match, or at the same position with `priorityMatch`.
- `prepareRuleSearch` and `prepareRuleWhileSearch` call `compileAG(allowA, allowG)` with `FindOption.None`.

## Tests

No upstream unit tests (vscode-textmate tests them through tokenization). New, with `TestGrammars`:

- [x] `MatchRule_ReturnsCaptureIndicesAndRuleId`
- [x] `MatchRule_NoMatch_ReturnsNull`
- [x] `MatchRuleOrInjections_LeftPriorityInjection_WinsAtSamePosition`
- [x] `MatchRuleOrInjections_RightPriorityInjection_LosesAtSamePosition`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

While conditions, captures and the main loop (P4-19): they call each other (`handleCaptures` recurses into
`_tokenizeString`), so they are ported together.

## Suggested commit message

`feat(textmate): port rule and injection matching`
