# P1-09 — Text buffer builder, factory and `PieceTreeTextBuffer` shell

- **Status:** done
- **Depends on:** P1-08
- **Size:** M

## Goal

Build a piece tree the way Monaco does when it loads a file: chunk by chunk, detecting the BOM, counting line-break
kinds, choosing the EOL and normalizing it. After this card, tests can create buffers exactly like Monaco's
`createTextBuffer` test helper.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBufferBuilder.ts` | 13–188 | `PieceTreeTextBufferFactory`, `PieceTreeTextBufferBuilder` (all) |
| `src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts` | 34–83, 528–530 | `PieceTreeTextBuffer`: fields, constructor, `mightContain*`, `getBOM`, `getEOL`, `getPieceTree` only |
| `src/vs/editor/common/model.ts` | 456–498 | `EndOfLinePreference`, `DefaultEndOfLine`, `EndOfLineSequence` |
| `src/vs/base/common/strings.ts` | 670–702 | `containsRTL` (+ `makeContainsRtl`), `isBasicASCII`, `containsUnusualLineTerminators` |
| same | 903–907 | `UTF8_BOM_CHARACTER`, `startsWithUTF8BOM` |
| `src/vs/editor/test/common/model/linesTextBuffer/linesTextBufferBuilder.test.ts` | all (71 lines) | suite `ModelBuilder` (12 tests) |
| `src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` | 155–162 | helper `createTextBuffer` |

## Files to create or change

- `src/LoomEditor.Core/Model/EndOfLinePreference.cs`, `DefaultEndOfLine.cs`, `EndOfLineSequence.cs` — **public** enums (same values as Monaco)
- `src/LoomEditor.Core/Model/StringClassification.cs` — `internal static class` with `ContainsRtl`, `IsBasicAscii`,
  `ContainsUnusualLineTerminators`, `Utf8Bom` (const char `'﻿'`), `StartsWithUtf8Bom`
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeTextBufferBuilder.cs` — builder (`AcceptChunk`, `Finish`)
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeTextBufferFactory.cs` — factory (`Create(DefaultEndOfLine)`, `GetFirstLineText`)
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeTextBuffer.cs` — shell, `internal sealed`
- `tests/LoomEditor.Core.Tests/Model/PieceTree/TestHelpers/TestTextBuffers.cs` — `Create(string[] chunks, bool normalizeEol = true)` = Monaco `createTextBuffer`
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeTextBufferBuilderTests.cs` — ported suite
- Run `./eng/Update-PublicApi.ps1 src/LoomEditor.Core` for the three public enums

## Design decisions (already made — do not change)

- `containsRTL`: copy Monaco's regular expression (strings.ts line 674) **character for character** into a
  `[GeneratedRegex]`. It already uses UTF-16 surrogate pairs (`\uD802…`) and no JS flags, so .NET reads it the same
  way. Test with one RTL character inside the BMP (Hebrew `א`) and one outside (e.g. U+10800 written as `"𐠀"`).
- `isBasicASCII` and `containsUnusualLineTerminators` are simple: use explicit loops or `[GeneratedRegex]`.
- `Factory.Create(DefaultEndOfLine)` returns the `PieceTreeTextBuffer` directly (no `{ textBuffer, disposable }` pair:
  nothing is disposable in our port). `PieceTreeTextBuffer` is not `IDisposable`.
- `PieceTreeTextBuffer` constructor takes the same arguments as Monaco (chunks, BOM, eol, containsRTL,
  containsUnusualLineTerminators, isBasicASCII, eolNormalized) and creates the `PieceTreeBase`.
- Monaco mutates the last chunk in `_finish` (`lastChunk.buffer += ...`): use `StringBuffer.Append` and replace `LineStarts`.

## Tests

- [x] Port suite `ModelBuilder` (12 tests: `t1`, `t2`, …) — the Monaco helper `createTextBufferFactory(text)` = builder with one chunk + `Finish()`
- [x] New: chunk ending with `\r` followed by a chunk starting with `\n` → one CRLF, line count correct
- [x] New: chunk ending with a high surrogate followed by its low surrogate → the pair is never split
- [x] New: BOM in the first chunk is removed from the text and reported by `GetBom()`; a BOM in a later chunk is kept as text
- [x] New: EOL choice: no line break → default EOL; mostly CRLF → `\r\n`; otherwise `\n`; with `normalizeEol` the text is rewritten

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: rows for `pieceTreeTextBufferBuilder.ts` (ported) and `pieceTreeTextBuffer.ts` (in progress)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`ApplyEdits` and the other `PieceTreeTextBuffer` queries (P1-16…P1-18). Streams (`createTextBufferFactoryFromStream`).

## Suggested commit message

`feat(core): port the piece tree text buffer builder`
