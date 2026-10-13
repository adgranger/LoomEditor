# Porting Monaco to C#

LoomEditor ports `microsoft/vscode` → `src/vs/editor` (MIT license). This page explains **how** to port a file,
and keeps the **map** from Monaco files to LoomEditor files.

## Reference commit

All ports currently target **`0c9c0a4295a9222e12a605537738719455ae3056`** (vscode `main`, 2026-10-13).
When you port a file, read it at that commit:
`https://github.com/microsoft/vscode/blob/0c9c0a4295a9222e12a605537738719455ae3056/<path>`.
If you deliberately port from a newer commit, write that commit in the file header.

## Method, step by step

1. **Read the Monaco file and its tests first.** Tests are in `src/vs/editor/test/...` with the same path.
2. **Create the C# file in the matching LoomEditor folder** (see the map below) with this header:
   ```csharp
   // Ported from microsoft/vscode: src/vs/editor/common/core/range.ts
   // Upstream commit: <sha>  —  Copyright (c) Microsoft Corporation. MIT License.
   ```
   Use `// Inspired by microsoft/vscode: ...` when the design is adapted rather than translated.
   (`ConventionTests.PortedFiles_StateTheUpstreamCommit` checks the header.)
3. **Port the tests first, then the code.** Keep each Monaco test name in a comment:
   ```csharp
   // Monaco: 'swap start and stop'
   [Fact]
   public void Constructor_EndLineBeforeStartLine_SwapsPositions() { ... }
   ```
4. **Keep Monaco's concepts and behavior; use C# conventions.**

   | TypeScript (Monaco) | C# (LoomEditor) |
   |---|---|
   | `class Position { constructor(public readonly lineNumber, ...) }` | `readonly record struct Position` with properties |
   | `range.containsPosition(p)` | `range.Contains(p)` (Monaco name in `<remarks>`) |
   | `static Range.plusRange(a, b)` | instance method `a.Plus(b)` when it reads better |
   | `IDisposable`, `Disposable`, `DisposableStore` | `IDisposable`, `List<IDisposable>` |
   | `Emitter<T>` / `onDidChange` | .NET `event EventHandler<T>? Changed` |
   | `number` used as an int | `int` (check for overflow: JS numbers are 53-bit) |
   | `string.charCodeAt(i)` | `string[i]` (UTF-16 code unit — identical semantics) |
   | `Uint32Array`, typed arrays | `int[]`, `List<int>`, `Span<int>` |
   | `undefined` / `null` | nullable (`T?`) — enable nullable reasoning, don't use `!` to silence it |
   | regular expressions | `[GeneratedRegex]` (never `new Regex(...)` in hot paths) |
5. **Don't port what .NET already has** (`strings.ts` helpers, `arrays.ts`…): use the BCL, and say so in a comment.
6. **Don't optimize while porting.** First a faithful port with green tests; then benchmarks; then optimizations,
   each with a comment explaining *why* and the benchmark that proves it.
7. **Update the map below** in the same pull request.

## Behavior differences we accept

Any deliberate difference from Monaco must be documented in the XML doc of the member *and* listed here.

| Where | Difference | Why |
|---|---|---|
| `Position`, `Range`, `Selection` constructors | Throw `ArgumentOutOfRangeException` for values < 1 (Monaco accepts anything) | Fail fast; Monaco's own model validates positions anyway |
| `Selection` | Not a subclass of `Range` (exposes a `Range` property) | C# record structs cannot inherit |
| `CommandRegistry` | One per editor instead of a global registry | Two editors on a page never share state |

## Map: Monaco → LoomEditor

| Monaco file | LoomEditor file | Status |
|---|---|---|
| `editor/common/core/position.ts` | `LoomEditor.Core/Text/Position.cs` | ✅ ported |
| `editor/common/core/range.ts` | `LoomEditor.Core/Text/Range.cs` | ✅ ported |
| `editor/common/core/selection.ts` | `LoomEditor.Core/Text/Selection.cs` | ✅ ported |
| `editor/common/core/editOperation.ts` | `LoomEditor.Core/Text/TextEdit.cs` | ✅ ported (`ISingleEditOperation` + `EditOperation`) |
| `editor/test/common/core/range.test.ts` | `tests/LoomEditor.Core.Tests/Text/RangeTests.cs` | ✅ all tests ported |
| `platform/commands/common/commands.ts` | `LoomEditor.View/Commands/CommandRegistry.cs` | ✅ adapted (per editor) |
| `editor/common/editorCommon.ts` (`IEditorContribution`) | `LoomEditor.View/Features/IEditorFeature.cs` | ✅ adapted |
| `editor/common/cursor/cursorMoveCommands.ts` (`selectAll`) | `LoomEditor.View/Features/SelectAll/` | ✅ ported |
| `editor/common/model/pieceTreeTextBuffer/*` | `LoomEditor.Core/Model/PieceTree/` | ⏳ phase 1 — plan below |
| `editor/common/model/textModel.ts` | `LoomEditor.Core/Model/TextModel.cs` | ⏳ phase 1 |
| `editor/common/model/editStack.ts` | `LoomEditor.Core/Undo/` | ⏳ phase 1 |
| `editor/common/model/intervalTree.ts` | `LoomEditor.Core/Decorations/` | ⏳ phase 1–2 |
| `editor/common/cursor/*` | `LoomEditor.View/Cursors/` | ⏳ phase 3 |
| `editor/common/viewModel/*` | `LoomEditor.View/Layout/` | ⏳ phase 2 / 6 |
| `microsoft/vscode-textmate` | `LoomEditor.TextMate/Grammar/` | ⏳ phase 4 |
| `editor/contrib/<feature>/` | `LoomEditor.View/Features/<Feature>/` | ⏳ phase 5 |

## Plan: porting the piece tree (phase 1)

**Monaco sources** (`src/vs/editor/common/model/pieceTreeTextBuffer/`, line counts at the reference commit):

| File | Lines | Content |
|---|---|---|
| `rbTreeBase.ts` | 425 | red-black tree node (`TreeNode`), sentinel, rotations, insert/delete fix-ups, metadata updates |
| `pieceTreeBase.ts` | 1882 | `StringBuffer`, `Piece`, line starts, `PieceTreeSearchCache`, `PieceTreeBase` (insert, delete, offset ↔ position, line content, snapshots, search) |
| `pieceTreeTextBuffer.ts` | 629 | `PieceTreeTextBuffer`: the `ITextBuffer` API used by `TextModel` (apply edits, EOL, BOM, line/column queries) |
| `pieceTreeTextBufferBuilder.ts` | 188 | building a buffer from chunks (file loading), EOL detection |

**C# classes** (all `internal`, in `LoomEditor.Core/Model/PieceTree/`, one class per file):

| C# file | From | Notes |
|---|---|---|
| `NodeColor.cs`, `TreeNode.cs` | `rbTreeBase.ts` | node with `Left/Right/Parent`, `SizeLeft`, `LineFeedCountLeft`; the sentinel is a static singleton |
| `RedBlackTree.cs` | `rbTreeBase.ts` | `LeftRotate`, `RightRotate`, `RbDelete`, `FixInsert`, `UpdateTreeMetadata` as static methods |
| `StringBuffer.cs` | `pieceTreeBase.ts` | text + `LineStarts` (`List<int>`, append-only) |
| `LineStarts.cs` | `createLineStarts*` | counts `\r`, `\n`, `\r\n`; returns line starts |
| `BufferCursor.cs`, `Piece.cs` | `pieceTreeBase.ts` | `readonly record struct`s |
| `PieceTreeSearchCache.cs` | `pieceTreeBase.ts` | small LRU of recent node lookups |
| `PieceTreeBase.cs` | `pieceTreeBase.ts` | the tree itself; likely > 400 lines — allowed for a direct port, split with `partial` by concern (`.Insert.cs`, `.Delete.cs`, `.Query.cs`, `.Search.cs`) |
| `PieceTreeSnapshot.cs` | `pieceTreeBase.ts` | read-only snapshot for saving / background work |
| `PieceTreeTextBuffer.cs` | `pieceTreeTextBuffer.ts` | public-facing buffer used by `TextModel` |
| `PieceTreeTextBufferBuilder.cs` | `pieceTreeTextBufferBuilder.ts` | chunked loading, EOL normalization, BOM |

`ITextBuffer` (the contract) goes to `LoomEditor.Core/Model/ITextBuffer.cs`, so `TextModel` does not depend on
the piece tree directly (and a naive `StringBuilder` buffer can implement it for tests).

**Tests to port** (`src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`, 87 tests,
2125 lines), suite by suite, into `tests/LoomEditor.Core.Tests/Model/PieceTree/`:
`inserts and deletes`, `prefix sum for line feed`, `offset 2 position`, `get text in range`, `CRLF`,
`centralized lineStarts with CRLF`, `random is unsupervised`, `buffer api`, `search offset cache`, `snapshot`,
`chunk based search`. Then `textModel.test.ts`, `model.line.test.ts`, `editableTextModel.test.ts` and
`editableTextModelAuto.test.ts` once `TextModel` exists.

**Additional tests (LoomEditor):**
- **Property/fuzz test**: thousands of random inserts/deletes (random seeds, logged on failure) applied both to the
  piece tree and to a naive `StringBuilder` reference; after each step compare full text, line count, every line,
  and `offset ↔ position` round-trips. Include CRLF splits (`\r` and `\n` inserted separately).
- **Red-black invariants** checked after each operation in Debug builds (`Guard.Assert`): root black, no red-red,
  equal black height, `SizeLeft`/`LineFeedCountLeft` consistent.

**Benchmarks** (`tests/LoomEditor.Benchmarks/`): open a 100 MB file; 10,000 random edits; `GetLineContent` on
random lines. Results go to `docs/performance.md`.

**Order of work:** `LineStarts` → `StringBuffer`/`Piece` → `TreeNode`/`RedBlackTree` (+ invariant checker) →
`PieceTreeBase` insert → delete → queries → fuzz test → snapshot/search → builder → `PieceTreeTextBuffer` →
`TextModel`. Each step is one small pull request with its tests.
