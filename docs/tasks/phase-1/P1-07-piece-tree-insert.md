# P1-07 — `PieceTreeBase`: insert

- **Status:** todo
- **Depends on:** P1-06
- **Size:** L in Monaco lines, but mechanical — do it in one card, split into partial files

## Goal

Insert text at any offset, including the tricky cases where a `\r` and a `\n` end up next to each other and must
become one `\r\n` line break.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts`:

| Lines | What to port |
|---|---|
| 849–945 | `insert` |
| 1020–1073 | `insertContentToNodeLeft`, `insertContentToNodeRight` |
| 1158–1162 | `deleteNodes` |
| 1164–1247 | `createNewPieces` |
| 1374–1396 | `deleteNodeTail` (used by `insert`) |
| 1455–1493 | `appendToNode` |
| 1625–1773 | `shouldCheckCRLF`, `startWithLF`, `endWithCR`, `validateCRLFWithPrevNode`, `validateCRLFWithNextNode`, `fixCRLF`, `adjustCarriageReturnFromNext` |

## Files to create or change

- `src/LoomEditor.Core/Model/PieceTree/PieceTreeBase.Insert.cs` — `Insert`, `InsertContentToNodeLeft/Right`, `AppendToNode`, `CreateNewPieces`
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeBase.Crlf.cs` — the CRLF helpers (1625–1773)
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeBase.Nodes.cs` — `DeleteNodes`, `DeleteNodeTail` (P1-08 adds more here)
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeBaseInsertTests.cs` — create

## Design decisions (already made — do not change)

- Signature: `void Insert(int offset, string value, bool eolNormalized = false)`.
- `startWithLF` / `endWithCR` take `string | TreeNode` in Monaco: write two overloads (`string` and `TreeNode`).
- Change buffer appends use `StringBuffer.Append` and `StringBuffer.LineStarts` (`List<int>`): Monaco's
  `lineStarts.concat(x.slice(1))` → `LineStarts.AddRange(x.Skip(1))` (or a loop); `pop()` → `RemoveAt(Count - 1)`.
- Monaco's `this._buffers[0].buffer += '_' + text` (a placeholder char before text) must be kept as is.
- After every insert, call `_searchCache.Validate(offset)` exactly where Monaco does.

## Tests

Use `TestPieceTrees.FromChunks` and the `PieceTreeAssert` helpers (P1-06). After each operation, assert: full text
equals the same edit applied to a `string`, `PieceTreeAssert.LineStarts`, `PieceTreeAssert.LinesContent`,
`TreeInvariants.AssertValid`.

- [ ] Insert into an empty tree; at start, middle, end of a one-piece tree
- [ ] Many consecutive inserts at the end (typing) → still one change-buffer piece appended (check node count stays small)
- [ ] Insert text longer than `AverageBufferSize` (65,535) → split into several buffers; content correct
- [ ] `"\r"` then `"\n"` inserted right after it → one line break (line count does not double)
- [ ] Insert `"\n"` right after an existing `"\r"` at the end of a piece, and `"\r"` right before an existing `"\n"`
- [ ] Seeded random inserts (200 operations, short random strings including `\r` and `\n`) on several seeds

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`delete` (P1-08). Porting the Monaco test suites (P1-10…P1-12).

## Suggested commit message

`feat(core): add insert to PieceTreeBase`
