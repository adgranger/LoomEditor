# P1-10 — Port tests: inserts and deletes, prefix sums, offset → position, search offset cache

- **Status:** todo
- **Depends on:** P1-09
- **Size:** M (tests only, plus the bug fixes they reveal)

## Goal

Port the first four Monaco piece tree test suites. If a test fails, the bug is in our port: fix the production code
by comparing it line by line with Monaco — **never change the test's expected values**.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`:

| Lines | Suite | Tests |
|---|---|---|
| 215–576 | `inserts and deletes` | 14 |
| 578–829 | `prefix sum for line feed` | 11 |
| 831–857 | `offset 2 position` | 1 |
| 1838–1955 | `search offset cache` | 5 |

## Files to create or change

`tests/LoomEditor.Core.Tests/Model/PieceTree/`:
- `PieceTreeInsertsAndDeletesTests.cs`, `PieceTreePrefixSumTests.cs`, `PieceTreeOffsetToPositionTests.cs`,
  `PieceTreeSearchOffsetCacheTests.cs`
- Production files only if a test reveals a bug.

## Design decisions (already made — do not change)

- One C# test class per Monaco suite; one `[Fact]` per Monaco `test`, with `// Monaco: '<exact test name>'` above it.
- Buffers are created with `TestTextBuffers.Create(...)` (P1-09) then `.GetPieceTree()`, as in Monaco.
- `assertTreeInvariants(pieceTable)` → `TreeInvariants.AssertValid(pieceTable.Tree)`;
  `testLinesContent` / `testLineStarts` → `PieceTreeAssert.LinesContent` / `PieceTreeAssert.LineStarts` (P1-06).
- Despite their names (`random test 1`…), these tests are **recorded sequences**: they contain no randomness.
- `ensureNoDisposablesAreLeakedInTestSuite` / `ds.add(...)`: not applicable, drop them.

## Tests

- [ ] `inserts and deletes` — 14 tests
- [ ] `prefix sum for line feed` — 11 tests
- [ ] `offset 2 position` — 1 test
- [ ] `search offset cache` — 5 tests

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Every bug fix is listed in the PR description (Monaco line vs. our line)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The other suites (P1-11…P1-15).

## Suggested commit message

`test(core): port piece tree insert/delete test suites`
