# P1-12 — Port tests: CRLF and centralized line starts with CRLF

- **Status:** done
- **Depends on:** P1-09 (can be done in parallel with P1-10/P1-11)
- **Size:** M (tests only, plus the bug fixes they reveal — expect some in `PieceTreeBase.Crlf.cs`)

## Goal

Port the two suites that stress `\r\n` handling: deleting half of a pair, inserting `\r` and `\n` separately,
chunks that end with `\r`. These are the hardest cases of the piece tree.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`:

| Lines | Suite | Tests |
|---|---|---|
| 1054–1292 | `CRLF` | 12 |
| 1294–1590 | `centralized lineStarts with CRLF` | 16 (incl. `random chunk bug 1…4`) |

## Files to create or change

- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeCrlfTests.cs`
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeCentralizedLineStartsTests.cs`
- Production files only if a test reveals a bug.

## Design decisions (already made — do not change)

- Same as P1-10.
- Monaco has two tests named `random bug 1` etc. in different suites: the `// Monaco:` comment is enough to tell
  them apart; C# method names just need to be unique in their class.
- Several tests call `createTextBuffer([...], false)` (no EOL normalization): keep the `false`.

## Tests

- [x] `CRLF` — 12 tests
- [x] `centralized lineStarts with CRLF` — 16 tests

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Other suites.

## Suggested commit message

`test(core): port piece tree CRLF test suites`
