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
| `StringBuffer` | Growable `char[]` instead of a `string` | Monaco's `buffer += text` on the change buffer would be quadratic in .NET |
| `RedBlackTree` | One sentinel per tree instead of a global mutable `SENTINEL`; `TreeNode.IsDetached` replaces `parent === null`; `next`/`prev` are tree methods | Monaco's `resetSentinel` writes to the global sentinel: two trees on two threads would corrupt each other |
| `PieceTreeBase.NodeAt`, `NodeAt2` | Throw `ArgumentOutOfRangeException` for an offset or position outside the document (Monaco returns `null` and fails later) | Fail fast with a clear message |
| `PieceTreeBase.GetCharCode`, `GetLineCharCode` | Return `char` (`'\0'` at the end of the document) instead of a number (`0` or `NaN`) | Same UTF-16 unit; no `NaN` in C# |
| `PieceTreeBase.Insert` | Inserting an empty string is a no-op (Monaco splits the piece; inside a CRLF pair this made `GetLineCount` count one line too many) | Found by the P1-15 property test; Monaco callers never insert empty text |
| `PieceTreeTextBufferFactory.Create` | Returns the `PieceTreeTextBuffer` directly, not a `{ textBuffer, disposable }` pair; `PieceTreeTextBuffer` is not `IDisposable` and has no `onDidChangeContent` | Nothing to dispose in this port |
| `ITextBuffer` | `IReadonlyTextBuffer` and `ITextBuffer` are one interface; no `onDidChangeContent`, not `IDisposable`, no `findMatchesLineByLine` yet (phase 5) | Simpler contract; search is deferred |

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
| `editor/common/model/pieceTreeTextBuffer/*` | `LoomEditor.Core/Model/PieceTree/` | ⏳ phase 1 — see docs/tasks |
| `editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts` | `LoomEditor.Core/Model/PieceTree/RedBlackTree*.cs`, `TreeNode.cs`, `NodeColor.cs` | ✅ ported (P1-03; `rbInsertLeft/Right` and `NodePosition` from `pieceTreeBase.ts`) |
| `editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` | `LoomEditor.Core/Model/PieceTree/LineStarts.cs`, `PieceTreeBase*.cs` (+ more) | ✅ ported (P1-01, P1-05 to P1-08, P1-14, tests P1-10 to P1-15) except search (`findMatchesInNode`…), which comes in phase 5 |
| `editor/common/model/pieceTreeTextBuffer/pieceTreeTextBufferBuilder.ts` | `LoomEditor.Core/Model/PieceTree/PieceTreeTextBufferBuilder.cs`, `PieceTreeTextBufferFactory.cs` | ✅ ported (P1-09) |
| `editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts` | `LoomEditor.Core/Model/PieceTree/PieceTreeTextBuffer.cs` | ⏳ in progress: P1-09 (shell), P1-16 (read queries, `ITextBuffer`); `applyEdits` in P1-18 |
| `editor/common/model.ts` (`EndOfLinePreference`, `DefaultEndOfLine`, `EndOfLineSequence`, `IReadonlyTextBuffer` + `ITextBuffer` merged, without search) | `LoomEditor.Core/Model/` | ✅ ported (P1-09, P1-16); `applyEdits` added in P1-18 |
| `base/common/strings.ts` (`containsRTL`, `isBasicASCII`, `containsUnusualLineTerminators`, BOM, `firstNonWhitespaceIndex`, `lastNonWhitespaceIndex`) | `LoomEditor.Core/Model/StringClassification.cs` | ✅ ported (P1-09, P1-16) |
| `editor/test/common/model/linesTextBuffer/linesTextBufferBuilder.test.ts` | `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeTextBufferBuilderTests.cs` | ✅ all tests ported |
| `editor/common/model/textModel.ts` | `LoomEditor.Core/Model/TextModel.cs` | ⏳ phase 1 |
| `editor/common/model/editStack.ts` | `LoomEditor.Core/Undo/` | ⏳ phase 1 |
| `editor/common/model/intervalTree.ts` | `LoomEditor.Core/Decorations/` | ⏳ phase 1–2 |
| `editor/common/cursor/*` | `LoomEditor.View/Cursors/` | ⏳ phase 3 |
| `editor/common/viewModel/*` | `LoomEditor.View/Layout/` | ⏳ phase 2 / 6 |
| `microsoft/vscode-textmate` | `LoomEditor.TextMate/Grammar/` | ⏳ phase 4 |
| `editor/contrib/<feature>/` | `LoomEditor.View/Features/<Feature>/` | ⏳ phase 5 |

## Plan: phase 1 (piece tree and text model)

The step-by-step plan is in **[docs/tasks/](tasks/README.md)**: 30 small task cards (P1-01 … P1-30), each with the
exact Monaco lines to port, the files to create, the design decisions and the tests.

Summary of the main design decisions taken there:

- `PieceTreeBase` is split into `partial` files by concern (`.Lines`, `.Positions`, `.Insert`, `.Delete`, `.Crlf`,
  `.Snapshot`, `.Eol`) and **owns** a `RedBlackTree` (composition) instead of mixing tree and text code.
- **One sentinel node per tree** instead of Monaco's global mutable `SENTINEL` (thread safety, parallel tests).
- `StringBuffer` is a growable `char[]`, not a `string` (appending to the change buffer must not be quadratic).
- `PieceTreeTextBuffer` / `ITextBuffer` stay `internal`; the public API is `TextModel`, which uses `TextEdit` for edits.
- Search in the piece tree is deferred to phase 5 (Find); decorations and injected text to phase 2+.
- Monaco's workspace-wide `IUndoRedoService` is replaced by a per-model `UndoRedoStack`.
