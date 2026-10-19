# P4-31 — Port `model.line.test.ts` › `ModelLinesTokens` (1/2): deletes, inserts, updates on insertion

- **Status:** done
- **Depends on:** P4-30
- **Size:** M (test code; fix the port until green)

## Goal

The first half of Monaco's token-editing tests passes: tokens follow single and multiple deletes and inserts.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/test/common/model/model.line.test.ts` | 24–47, 76–151, 153–200, 495–511 | helpers: `assertLineTokens`, `TestToken`, `ManualTokenizationSupport`, `LineState`, `testApplyEdits`, `testLineEditTokens` |
| vscode: same file | 201–751 | the tests listed below |

## Files to create or change

- `tests/LoomEditor.Core.Tests/Model/Tokens/TestHelpers/ManualTokenizationSupport.cs` — create
- `tests/LoomEditor.Core.Tests/Model/Tokens/ModelLinesTokensTests.cs` — create (P4-32 adds the second half)
- production code under `src/LoomEditor.Core/` — fix what the tests reveal

## Design decisions (already made — do not change)

- `ManualTokenizationSupport` sets tokens per line from the test (upstream 103–141); it is registered in a
  `TokenizationRegistry` built by the test, under a test language id.
- `createTextModel` is the existing test helper of phase 3 (or `new TextModel(...)` with the registry overload of P4-29).
- Suite `'ModelLine - getIndentLevel'` (49–75) is not about tokens: not ported here.

## Tests

- [x] Port: `'single delete 1'` … `'single delete 5'`, `'multi delete 6'` … `'multi delete 9'`,
      `'single insert 1'` … `'single insert 5'`, `'multi insert 6'` … `'multi insert 9'`,
      `'insertion on empty line'`, `'updates tokens on insertion 1'` … `'updates tokens on insertion 10'`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: ported tests and bugs found listed
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The second half (P4-32).

## Suggested commit message

`test(core): port the model line token tests (1/2)`

## Notes (done)

- 29 tests ported, all green on the first run: no bug found in the port.
- `assertLineTokens` compares the foreground of each token instead of Monaco's `getClassName` (CSS class, not ported;
  it only encodes the foreground here).
