# P1-13 — Snapshots, equality and buffer API tests

- **Status:** done
- **Depends on:** P1-09
- **Size:** S

## Goal

Read-only snapshots of a piece tree (used later to save files and to give text to background work without
copying everything), tree equality, and the "buffer api" Monaco tests.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` | 159–199 | `PieceTreeSnapshot` |
| same | 365–393 | `createSnapshot`, `equal` |
| `src/vs/editor/common/model.ts` | 693–695 | `ITextSnapshot` |
| `src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` | 1742–1836 | suite `buffer api` (7 tests) |
| same | 1957–2065 | helper `getValueInSnapshot` + suite `snapshot` (4 tests) |

## Files to create or change

- `src/LoomEditor.Core/Model/ITextSnapshot.cs` — **public** interface: `string? Read()` (returns `null` at the end)
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeSnapshot.cs` — `internal sealed`
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeBase.Snapshot.cs` — `CreateSnapshot(string bom)`, `Equal(PieceTreeBase other)`
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeBufferApiTests.cs`, `PieceTreeSnapshotTests.cs`
- Run `./eng/Update-PublicApi.ps1 src/LoomEditor.Core`

## Design decisions (already made — do not change)

- Keep Monaco's `Read()` protocol: first call returns BOM + first piece, then one piece per call, then `null`.
  An empty tree returns the BOM once (possibly `""`), then `null`.
- The snapshot copies the **list of pieces** at creation (pieces are immutable); later edits must not change what the
  snapshot returns — that is exactly what the `snapshot` suite checks.
- Name `Equal` (not `Equals`) to avoid overriding `object.Equals` semantics.

## Tests

- [x] `buffer api` — 7 tests (`equal`, `equal with more chunks`, `equal 2, empty buffer`, `equal 3, empty buffer`, `getLineCharCode - issue #45735`, `getLineCharCode - issue #47733`, `getNearestChunk`)
- [x] `snapshot` — 4 tests

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`TextModel.CreateSnapshot` (P1-27).

## Suggested commit message

`feat(core): add piece tree snapshots and equality`
