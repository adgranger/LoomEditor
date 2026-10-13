# P1-11 — Port tests: get text in range and line content

- **Status:** done
- **Depends on:** P1-09 (can be done in parallel with P1-10)
- **Size:** S (tests only, plus the bug fixes they reveal)

## Goal

Port the Monaco suite that checks `GetValueInRange`, `GetLineContent` and `GetLinesContent` after edits.
If a test fails, fix the production code — never the expected values.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`:

| Lines | Suite | Tests |
|---|---|---|
| 859–1052 | `get text in range` | 10 (`getContentInRange`, `random test value in range`, `random test value in range exception`, `random tests bug 1`, `random tests bug 2`, `get line content`, `get line content basic`, `get line content after inserts/deletes`, `random 1`, `random 2`) |

## Files to create or change

- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeTextInRangeTests.cs` — create
- Production files only if a test reveals a bug.

## Design decisions (already made — do not change)

Same as P1-10 (one `[Fact]` per Monaco test, `// Monaco:` comment, `TestTextBuffers.Create`, seeded `Random`).
Monaco's `new Range(...)` → `new Range(...)` (our `LoomEditor.Core.Text.Range`).

## Tests

- [x] `get text in range` — all tests of the suite

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Other suites.

## Suggested commit message

`test(core): port piece tree text-in-range test suite`
