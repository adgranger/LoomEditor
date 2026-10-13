# P1-15 — Randomized tests against a naive reference buffer

- **Status:** todo
- **Depends on:** P1-10, P1-11, P1-12, P1-13, P1-14
- **Size:** S (tests only, plus the bug fixes they reveal)

## Goal

Thousands of random edits compared with a trivially correct implementation, so that bugs the hand-written tests
missed are caught. This is the safety net for every future change to the piece tree.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`:

| Lines | Suite | Tests |
|---|---|---|
| 19–45 | helpers | `randomChar`, `randomInt`, `randomStr` |
| 1592–1740 | `random is unsupervised` | 4 (`splitting large change buffer`, `random insert delete`, `random chunks`, `random chunks 2`) |

## Files to create or change

`tests/LoomEditor.Core.Tests/Model/PieceTree/`:
- `TestHelpers/RandomText.cs` — port of `randomChar`/`randomInt`/`randomStr`, taking a `Random` instance (never `Random.Shared`)
- `TestHelpers/NaiveTextBuffer.cs` — reference: a `StringBuilder` with `Insert(offset, text)`, `Delete(offset, count)`,
  `GetLines()`, `GetOffsetAt(line, column)`, `GetPositionAt(offset)` implemented in the most obvious way
- `PieceTreeRandomTests.cs` — the 4 ported tests
- `PieceTreePropertyTests.cs` — new property test

## Design decisions (already made — do not change)

- Every random test takes its seed from `[Theory]` data (e.g. 10 fixed seeds) and puts it in every assertion message,
  so a failure can be replayed.
- The property test, for each seed: 2,000 operations, each one an insert (random text of 0–20 chars drawn from
  `"abc\r\n"` plus one non-BMP character) or a delete, at a random offset; after **each** operation compare with
  `NaiveTextBuffer`: full text, line count, every line, `GetOffsetAt`/`GetPositionAt` for 20 random offsets, and
  `TreeInvariants.AssertValid`.
- Keep the total run time of this test class under ~10 seconds in Release.

## Tests

- [ ] `random is unsupervised` — 4 tests
- [ ] Property test (10 seeds)
- [ ] `NaiveTextBuffer` has its own 3–4 sanity tests

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated; mark `pieceTreeBase.ts` as ported in `docs/PORTING.md` (except search → phase 5)

## Out of scope

Performance (P1-19).

## Suggested commit message

`test(core): add randomized piece tree tests`
