# PieceTree

The text buffer behind `TextModel`: a *piece tree* (a red-black tree of pieces pointing into
append-only string buffers), which makes edits in huge files fast. Ported in phase 1.

Monaco source: `src/vs/editor/common/model/pieceTreeTextBuffer/`.

| Type | Role |
|---|---|
| `RedBlackTree`, `TreeNode`, `NodeColor` | the balanced tree (one sentinel node per tree) |
| `StringBuffer`, `LineStarts`, `Piece` | append-only buffers and the pieces that point into them |
| `PieceTreeBase` (`.Lines`, `.Positions`, `.Insert`, `.Delete`, `.Crlf`, `.Eol`, `.Snapshot`) | the algorithms: lines, offsets, insert, delete, CRLF fix-ups |
| `PieceTreeTextBufferBuilder`, `PieceTreeTextBufferFactory` | build a buffer from chunks, detect the EOL |
| `PieceTreeTextBuffer` (`.ApplyEdits`, `.EditHelpers`) | the `ITextBuffer` used by `TextModel`: queries and `ApplyEdits` |

Not ported: search (`findMatchesLineByLine`), phase 5. See docs/PORTING.md for the differences from Monaco and
docs/performance.md for the benchmarks.
