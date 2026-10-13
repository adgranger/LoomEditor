# P1-08 — `PieceTreeBase`: delete

- **Status:** todo
- **Depends on:** P1-07
- **Size:** M

## Goal

Delete any range of text, keeping `\r\n` pairs consistent when a deletion joins a `\r` and a `\n`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts`:

| Lines | What to port |
|---|---|
| 947–1018 | `delete` |
| 1398–1418 | `deleteNodeHead` |
| 1420–1453 | `shrinkNode` |

## Files to create or change

- `src/LoomEditor.Core/Model/PieceTree/PieceTreeBase.Delete.cs` — `Delete`
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeBase.Nodes.cs` — add `DeleteNodeHead`, `ShrinkNode`
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeBaseDeleteTests.cs` — create

## Design decisions (already made — do not change)

- Signature: `void Delete(int offset, int count)` (Monaco `cnt`). `count <= 0` does nothing (as in Monaco).
- `rbDelete(this, node)` → `_tree.Delete(node)`.

## Tests

Same assertions as P1-07 after each operation.

- [ ] Delete inside one piece; a whole piece; across several pieces; everything
- [ ] Delete that joins `\r` and `\n` (e.g. text `"a\r\nb"`: delete then re-insert around the pair; text `"\rx\n"`: delete `x`)
- [ ] Delete the `\r` of a `\r\n` pair → line count stays consistent
- [ ] Seeded random inserts **and** deletes (300 operations) on several seeds, compared with a `string`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Porting the Monaco suites (P1-10…P1-12).

## Suggested commit message

`feat(core): add delete to PieceTreeBase`
