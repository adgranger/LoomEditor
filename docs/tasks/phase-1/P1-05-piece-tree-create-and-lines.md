# P1-05 — `PieceTreeBase`: construction and reading lines

- **Status:** done
- **Depends on:** P1-04
- **Size:** M

## Goal

A read-only `PieceTreeBase`: build it from text chunks, then read its length, line count, full text and any line.
No editing yet.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts`:

| Lines | What to port |
|---|---|
| 268–282 | fields and constructor |
| 284–318 | `create` |
| 355–357 | `getEOL` |
| 515–603 | `getLinesContent` |
| 605–611 | `getLength`, `getLineCount` |
| 613–629 | `getLineContent` (with the `_lastVisitedLine` one-line cache) |
| 1153–1156 | `offsetInBuffer` |
| 1249–1251 | `getLinesRawContent` |
| 1253–1323 | `getLineRawContent` |
| 1325–1341 | `computeBufferMetadata` |
| 1360–1372 | `getAccumulatedValue` |
| 1775–1813 | `iterate`, `getNodeContent`, `getPieceContent` |
| 1871–1882 | `getContentOfSubTree` |

## Files to create or change

`src/LoomEditor.Core/Model/PieceTree/` — `internal sealed partial class PieceTreeBase`, split by concern:
- `PieceTreeBase.cs` — fields, constructor, `Create`, `ComputeBufferMetadata`, `GetLength`, `GetLineCount`, `GetEol`
- `PieceTreeBase.Lines.cs` — `GetLinesContent`, `GetLineContent`, `GetLineRawContent`, `GetLinesRawContent`,
  `GetContentOfSubTree`, `Iterate`, `GetNodeContent`, `GetPieceContent`, `GetAccumulatedValue`, `OffsetInBuffer`

`tests/LoomEditor.Core.Tests/Model/PieceTree/`:
- `TestHelpers/TestPieceTrees.cs` — `static PieceTreeBase FromChunks(string eol, params string[] chunks)`: builds
  `StringBuffer`s with `LineStarts.CreateFast` and calls the constructor with `eolNormalized: true`.
  **Temporary**: P1-09 adds the real builder; keep this helper for these early tests.
- `PieceTreeBaseLinesTests.cs`

## Design decisions (already made — do not change)

- Constructor: `PieceTreeBase(IReadOnlyList<StringBuffer> chunks, string eol, bool eolNormalized)`; `eol` must be
  `"\n"` or `"\r\n"` (else `ArgumentException`). Keep Monaco's `string` EOL here; enums come with `TextModel`.
- `PieceTreeBase` **owns** a `RedBlackTree` (`private readonly RedBlackTree _tree`, exposed `internal RedBlackTree Tree`
  for tests). Monaco's `this.root` → `_tree.Root`; `SENTINEL` → `_tree.Sentinel`; `rbInsertRight` → `_tree.InsertRight`.
- `_buffers[0]` is the change buffer (empty, line starts `[0]`); original chunks follow — same as Monaco.
- `_lastVisitedLine` → a private `(int LineNumber, string Value)` field; `_lastChangeBufferPos` → `BufferCursor` field.
- Monaco's regex `/(\r\n|\r|\n)$/` in `getLineContent` → a `[GeneratedRegex]` or a small explicit helper; either is fine.
- `GetLinesContent` returns `string[]`.

## Tests

- [x] Empty tree (`FromChunks("\n", "")`): length 0, 1 line, line 1 is `""`
- [x] One chunk `"a\nbc\n\ndef"`: line count 4, each line correct, `GetLinesRawContent` equals the input
- [x] Several chunks where a line spans chunks (`"ab", "c\nd", "ef\n", "g"`) → lines `abc`, `def`, `g`
- [x] CRLF text with `eol = "\r\n"`: `GetLineContent` strips `\r\n`; `GetLineRawContent` keeps it
- [x] `GetLinesContent` equals `text.Split(["\r\n", "\n", "\r"])` for a few inputs (`[Theory]`)
- [x] Reading the same line twice returns the same value (exercises `_lastVisitedLine`)
- [x] `TreeInvariants.AssertValid(tree.Tree)` after construction

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Offsets/positions (P1-06), insert (P1-07), delete (P1-08), snapshots (P1-13), EOL changes (P1-14), search (phase 5).

## Suggested commit message

`feat(core): add read-only PieceTreeBase`
