# P4-16 — Grammar (3/6): the token accumulator (`LineTokens` of vscode-textmate)

- **Status:** todo
- **Depends on:** P4-15
- **Size:** M

## Goal

While a line is tokenized, tokens are collected in two forms: scope names (`IToken[]`, for `tokenizeLine`) and binary
metadata pairs (`uint[]`, for `tokenizeLine2`), with vscode-textmate's merging and token-type rules.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/grammar/grammar.ts` | 958–1125 | `LineTokens` (constructor, `produce`, `produceFromScopes`, `getResult`, `getBinaryResult`) |
| textmate: `src/main.ts` | 219–252, 284–288 | `ITokenizeLineResult`, `ITokenizeLineResult2` (without `fonts`), `IToken` |
| textmate: `src/grammar/grammar.ts` | 1127–1190 | **not ported**: `FontInfo`, `LineFonts` (ADR 0008 decision 3) |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/LineTokens.cs` — create (namespace `LoomEditor.TextMate.Grammar`; see phase-4/README.md section 3)
- `src/LoomEditor.TextMate/Grammar/TokenizeLineResult.cs` (`IToken` as `sealed record Token(int StartIndex, int EndIndex, IReadOnlyList<string> Scopes)`, `TokenizeLineResult`, `TokenizeLineResult2` with `uint[] Tokens`) — create
- `tests/LoomEditor.TextMate.Tests/Grammar/LineTokensTests.cs` — create

## Design decisions (already made — do not change)

- `getBinaryResult` returns `uint[]` (trap 1). The "merge consecutive tokens with the same metadata" rule and the
  last-token fix-up at the end of the line are upstream's.
- `_emitBinaryTokens` / `_tokenTypeOverrides` / `balancedBracketSelectors` behave like upstream; `DebugFlags.InDebugMode`
  is always false (no debug logging).
- The `lineText` is used only in debug mode upstream: keep the parameter, do not store more.

## Tests

No upstream unit tests. New:

- [ ] `GetResult_LastTokenIsExtendedToLineLength`
- [ ] `GetBinaryResult_ConsecutiveTokensWithSameMetadata_AreMerged`
- [ ] `Produce_TokenTypeOverride_SetsStandardTokenType`
- [ ] `Produce_BalancedBracketSelector_SetsBracketBit`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The grammar class (P4-17).

## Suggested commit message

`feat(textmate): port the line token accumulator`
