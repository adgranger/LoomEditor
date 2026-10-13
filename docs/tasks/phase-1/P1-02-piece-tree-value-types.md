# P1-02 — Piece tree value types

- **Status:** todo
- **Depends on:** P1-01
- **Size:** S

## Goal

The small types the piece tree is made of: the text buffers, a cursor inside a buffer, a piece, and a node position.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` | 14 | `AverageBufferSize` constant |
| same | 101–114 | `NodePosition` |
| same | 116–125 | `BufferCursor` |
| same | 127–141 | `Piece` |
| same | 143–157 | `StringBuffer` |

Read lines 1164–1247 (`createNewPieces`) and 1455–1493 (`appendToNode`) to see how `StringBuffer` is used — do not port them.

## Files to create or change

All in `src/LoomEditor.Core/Model/PieceTree/` (namespace `LoomEditor.Core.Model.PieceTree`), all `internal`:

- `PieceTreeConstants.cs` — `internal static class PieceTreeConstants { public const int AverageBufferSize = 65535; }`
- `BufferCursor.cs` — `readonly record struct BufferCursor(int Line, int Column)` (0-based line index in the buffer's
  line starts, and column = offset from that line start)
- `Piece.cs` — `sealed record Piece(int BufferIndex, BufferCursor Start, BufferCursor End, int LineFeedCount, int Length)`
- `StringBuffer.cs` — see below
- `NodePosition.cs` — **create in P1-03** (it references `TreeNode`); skip it here
- `tests/LoomEditor.Core.Tests/Model/PieceTree/StringBufferTests.cs` — create

## Design decisions (already made — do not change)

- **`StringBuffer` must support cheap appends.** Monaco does `buffer += text` on the change buffer; in .NET that copies
  the whole string each time (quadratic). Implement `StringBuffer` over a growable `char[]`:
  - `StringBuffer(string text, List<int> lineStarts)`
  - `int Length`, `char this[int index]`, `string Substring(int start, int length)`, `override string ToString()`
  - `void Append(string text)` — amortized O(text length) (double the capacity when full)
  - `List<int> LineStarts { get; set; }` — mutable list: the change buffer appends to it and Monaco sometimes
    removes the last entry or replaces the list.
  - XML doc: explain *why* it is not a `string` (the comment is mandatory for a non-obvious optimization).
- `Piece` is immutable (a `sealed record` class, not a struct: tree nodes swap whole pieces, and Monaco's snapshot
  relies on pieces being shared immutable objects).
- Names: Monaco `lineFeedCnt` → `LineFeedCount`, `bufferIndex` → `BufferIndex`.

## Tests

- [ ] `StringBuffer`: indexer, `Substring`, `ToString`, `Length` on initial text
- [ ] `Append` many times (e.g. 10,000 × "ab") → content and `Length` correct
- [ ] `Append("")` changes nothing
- [ ] `Substring` with out-of-range arguments throws `ArgumentOutOfRangeException`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` "Behavior differences": add the `StringBuffer` row (growable char array instead of `string`)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Tree nodes (P1-03), anything that inserts or deletes.

## Suggested commit message

`feat(core): add piece tree buffers and pieces`
