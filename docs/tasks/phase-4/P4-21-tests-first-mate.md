# P4-21 — Port the vscode-textmate tokenization tests: `first-mate` suite

- **Status:** done
- **Depends on:** P4-20
- **Size:** M (test code; fix the port until green)

## Goal

The 64 cases of `test-cases/first-mate/tests.json` run against our tokenizer, exactly like vscode-textmate's
`tokenization.test.ts`, and pass.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/tests/tokenization.test.ts` | 1–112 | the data-driven runner: load the grammars of a case, pick the grammar by `grammarScopeName` or `grammarPath`, add `grammarInjections`, tokenize each line with the previous state, round-trip the state through `diffStateStacksRefEq` / `applyStateStackDiff`, compare `{value, scopes}`; the workaround of lines 91–97 (drop empty expected tokens on a non-empty line) |
| textmate: `src/tests/resolver.ts` | 1–139 | the test resolver (grammar paths → raw grammars, injections) — only what the runner uses |
| textmate: `test-cases/first-mate/tests.json` | — | the cases (copied in P4-06) |

## Files to create or change

- `tests/LoomEditor.TextMate.Tests/Grammar/TokenizationSuiteRunner.cs` (shared with P4-22), `TestHelpers/TestResolver.cs` — create
- `tests/LoomEditor.TextMate.Tests/Grammar/FirstMateTests.cs` — create (an xUnit theory, one row per `desc`)
- production code under `src/LoomEditor.TextMate/` — fix what the tests reveal

## Design decisions (already made — do not change)

- The cases JSON is read with `JsonDocument` in the test (no serializer).
- Each case is a theory row named by its `desc` (`// vscode-textmate: '<desc>'` above the theory, the row data carries
  the name). A failure message shows the line, the expected and the actual tokens.
- Never change `tests.json`. A case that cannot pass because of the engine version (ADR 0007, decision 6) is skipped
  with `Skip = "Oniguruma 6.9.10 vs 6.9.8: <reason>"` **only after** showing the maintainer the difference: stop and
  report it first.

## Tests

- [x] Port: the 64 `first-mate` cases (`tokenization.test.ts` › `test(tst.desc)`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: tests ported, bugs found and fixed (one line each)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`suite1` and `whileTests` (P4-22).

## Suggested commit message

`test(textmate): port the first-mate tokenization suite`
