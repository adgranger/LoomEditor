# P1-06 — `PieceTreeBase`: offsets, positions, ranges, characters

- **Status:** todo
- **Depends on:** P1-05
- **Size:** M

## Goal

Convert between document offsets and (line, column) positions, read the text of a range, and read single characters.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts`:

| Lines | What to port |
|---|---|
| 395–416 | `getOffsetAt` |
| 418–457 | `getPositionAt` |
| 459–513 | `getValueInRange` |
| 631–649 | `_getCharCode` |
| 651–667 | `getLineCharCode`, `getLineLength`, `getCharCode` |
| 669–688 | `getNearestChunk` |
| 1075–1121 | `positionInBuffer` |
| 1123–1151 | `getLineFeedCnt` |
| 1343–1358 | `getIndexOf` |
| 1495–1528 | `nodeAt` |
| 1530–1595 | `nodeAt2` |
| 1597–1623 | `nodeCharCodeAt`, `offsetOfNode` |

## Files to create or change

- `src/LoomEditor.Core/Model/PieceTree/PieceTreeBase.Positions.cs` — create (all methods above)
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeBasePositionsTests.cs` — create

## Design decisions (already made — do not change)

- Public-in-class (still `internal` type) API: `GetOffsetAt(int lineNumber, int column)`, `GetPositionAt(int offset)`
  returning `LoomEditor.Core.Text.Position`, `GetValueInRange(Range range, string? eol = null)`,
  `GetLineCharCode(int lineNumber, int index)` (returns `char`), `GetLineLength(int lineNumber)`,
  `GetCharCode(int offset)` (returns `char`), `GetNearestChunk(int offset)`.
- Monaco returns char codes as numbers; return `char` here (same UTF-16 unit).
- `positionInBuffer` has an optional out-parameter `ret`; port it as `BufferCursor PositionInBuffer(TreeNode node, int remainder)`
  (return the value; the "reuse an object" trick is a JS allocation optimization).
- `nodeAt` / `nodeAt2` return `NodePosition` (from P1-03) and use the search cache (P1-04) exactly like Monaco.

## Tests

Use `TestPieceTrees.FromChunks` (P1-05). Port now the read-only Monaco helpers:

- [ ] Port helper `testLineStarts` (test file lines 100–153) into `TestHelpers/PieceTreeAssert.cs` as `LineStarts(string text, PieceTreeBase tree)`
- [ ] Port helper `testLinesContent` (lines 78–98) and `trimLineFeed` (47–74) into the same file as `LinesContent(...)`
- [ ] Both helpers pass on: empty text, LF text, CRLF text (eol `\r\n`), multi-chunk text, text ending with a line break
- [ ] `GetPositionAt`/`GetOffsetAt` round-trip for every offset of a multi-chunk text
- [ ] `GetValueInRange` across chunks, and with `eol` replacement (`"\r\n"` text read with `eol: "\n"`)
- [ ] `GetLineCharCode`, `GetCharCode`, `GetLineLength` on a few lines
- [ ] `GetNearestChunk` returns the rest of the piece that contains the offset

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Editing. Search (`findMatchesInNode`, `findMatchesLineByLine`, `_findMatchesInLine`: lines 690–847) is phase 5.

## Suggested commit message

`feat(core): add offset and position queries to PieceTreeBase`
