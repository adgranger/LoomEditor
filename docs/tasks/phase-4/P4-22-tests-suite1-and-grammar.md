# P4-22 — Port the vscode-textmate tests: `suite1`, `whileTests`, the rest of `grammar.test.ts`

- **Status:** done
- **Depends on:** P4-21
- **Size:** M (test code; fix the port until green)

## Goal

The remaining vscode-textmate tokenization tests pass: the 20 cases of `suite1/tests.json`, the 9 cases of
`suite1/whileTests.json` (plist grammars included), and the tests of `grammar.test.ts` not ported yet.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/tests/tokenization.test.ts` | 110–112 | the two other suites (same runner as P4-21) |
| textmate: `src/tests/grammar.test.ts` | 109–144 | `test.skip('Shadowed rules are resolved correctly')` — port it **skipped**, with upstream's comment |
| textmate: `src/tests/grammar.test.ts` | 145–196 | **not ported**: `'Fonts are correctly set'`, `'… 2'` (ADR 0008 decision 3) |
| textmate: `test-cases/suite1/` | — | cases and fixtures (copied in P4-06) |

## Files to create or change

- `tests/LoomEditor.TextMate.Tests/Grammar/Suite1Tests.cs`, `WhileTests.cs` — create (use `TokenizationSuiteRunner`)
- `tests/LoomEditor.TextMate.Tests/Grammar/GrammarRegressionTests.cs` — create (the skipped test)
- production code under `src/LoomEditor.TextMate/` — fix what the tests reveal

## Design decisions (already made — do not change)

- Same as P4-21 (no change to the JSON files; engine-version differences: stop and report before skipping).
- The skipped test keeps upstream's expected value and is `[Fact(Skip = "Skipped upstream: known vscode-textmate bug …")]`.

## Tests

- [x] Port: the 20 `suite1` cases and the 9 `whileTests` cases
- [x] Port (skipped as upstream): `'Shadowed rules are resolved correctly'`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: tests ported and not ported, bugs found and fixed
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Theme data-driven tests (not ported, P4-09).

## Suggested commit message

`test(textmate): port suite1, while tests and grammar tests`
