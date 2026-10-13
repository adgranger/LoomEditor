# P1-28 — `TextModel.CreateSnapshot`

- **Status:** done
- **Depends on:** P1-22
- **Size:** S

## Goal

Read the whole document chunk by chunk without building one giant string — used to save files and to hand the text
to background work.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/textModel.ts` | 815–818 | `createSnapshot` |
| same | 100–109 | `createTextBufferFactoryFromSnapshot` (to build a model back from a snapshot) |
| `src/vs/editor/test/common/model/textModel.test.ts` | 1176–1241 | suite `TextModel.createSnapshot` (5 tests) |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModel.Snapshot.cs` — `public ITextSnapshot CreateSnapshot(bool preserveBom = false)`,
  `public static TextModel FromSnapshot(ITextSnapshot snapshot, DefaultEndOfLine defaultEol = DefaultEndOfLine.LF)`
- `tests/LoomEditor.Core.Tests/Model/TextModelSnapshotTests.cs`
- Run `./eng/Update-PublicApi.ps1 src/LoomEditor.Core`

## Design decisions (already made — do not change)

- Reuse `PieceTreeSnapshot` (P1-13) through `ITextBuffer.CreateSnapshot`.

## Tests

- [x] `TextModel.createSnapshot` — 5 tests (`empty file`, `file with BOM`, `regular file`, `large file`, `issue #119632: invalid range`)
- [x] New: `FromSnapshot(model.CreateSnapshot())` has the same value as `model`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Reading from streams/files (later).

## Suggested commit message

`feat(core): add TextModel snapshots`
