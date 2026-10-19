# P4-32 — Port `model.line.test.ts` (2/2) and the token regressions of `textModelWithTokens.test.ts`

- **Status:** todo
- **Depends on:** P4-31
- **Size:** M (test code; fix the port until green)

## Goal

The rest of Monaco's token-editing tests passes (deletes inside tokens, replace, split, append), plus the four token
regression tests of `textModelWithTokens.test.ts`.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/test/common/model/model.line.test.ts` | 754–1276 | the tests listed below, with `testLineSplitTokens` (1062) and `testLineAppendTokens` (1153) |
| vscode: `src/vs/editor/test/common/model/textModelWithTokens.test.ts` | 544–711 | suite `'TextModelWithTokens regression tests'` with its helpers `assertViewLineTokens` (549) and `createViewLineToken` (614) |

## Files to create or change

- `tests/LoomEditor.Core.Tests/Model/Tokens/ModelLinesTokensTests.cs` — change
- `tests/LoomEditor.Core.Tests/Model/Tokens/TextModelWithTokensRegressionTests.cs` — create
- production code under `src/LoomEditor.Core/` — fix what the tests reveal

## Design decisions (already made — do not change)

- Same as P4-31. The regression tests that register languages use the test's own `LanguageIdCodec` and
  `TokenizationRegistry` (no language service).
- The other suites of `textModelWithTokens.test.ts` (brackets, indent guides) are phase 5: not ported here.

## Tests

- [ ] Port: `'delete second token 2'`, `'insert right before second token'`, `'delete first char'`,
      `'delete 2nd and 3rd chars'`, `'delete first token'`, `'delete second token'`,
      `'delete second token + a bit of the third one'`, `'delete second and third token'`, `'delete everything'`,
      `'noop'`, `'equivalent to deleting first two chars'`, `'equivalent to deleting from 5 to the end'`,
      `'updates tokens on replace 1'`, `'… 2'`, `'split at the beginning'`, `'split at the end'`,
      `'split inthe middle 1'`, `'split inthe middle 2'`, `'append empty 1'`, `'append empty 2'`, `'append 1'` … `'append 3'`
- [ ] Port (exact names from the file; they start like this): `'microsoft/monaco-editor#122…'` (line 548),
      `'microsoft/monaco-editor#133…'` (623), `'issue #11856…'` (651) **only if** it needs no bracket matching
      (otherwise list it as phase 5), `'issue #63822: Wrong embedded language detected for empty lines'` (674)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: ported tests, tests left for phase 5, bugs found
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Bracket and indent-guide suites (phase 5).

## Suggested commit message

`test(core): port the model line token tests (2/2) and token regressions`
