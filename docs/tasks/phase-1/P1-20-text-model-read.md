# P1-20 — `TextModel`: creation and read API

- **Status:** done
- **Depends on:** P1-18
- **Size:** M

## Goal

The public `TextModel` class, read-only for now: create it from a string, read text, lines, lengths, offsets.
This is the class the rest of the editor (and library users) will use.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/textModel.ts` | 59–70, 111–132 | `createTextBufferFactory`, `createTextBuffer` |
| same | 186–456 (read only what you need) | `TextModel` fields and constructor — **keep only** the text buffer, EOL, version ids |
| same | 737–913 | `getVersionId`, `mightContainRTL`, `mightContainNonBasicASCII`, `getAlternativeVersionId`, `getOffsetAt`, `getPositionAt`, `getValue`, `getValueLength`, `getValueInRange`, `getValueLengthInRange`, `getCharacterCountInRange`, `getLineCount`, `getLineContent`, `getLineLength`, `getLinesContent`, `getEOL`, `getEndOfLineSequence`, `getLineMinColumn`, `getLineMaxColumn`, `getLineFirstNonWhitespaceColumn`, `getLineLastNonWhitespaceColumn` |
| same | 1168–1175 | `getFullModelRange` |
| `src/vs/editor/test/common/testTextModel.ts` | `createTextModel` | test helper |
| `src/vs/editor/test/common/model/textModel.test.ts` | 74–169 | suite `TextModelData.fromString` (5 tests) |
| same | 189–239, 1053–1096 | from suite `Editor Model - TextModel`: `getValueLengthInRange`, `getValueLengthInRange different EOL` (valid ranges only, see Tests), `getLineFirstNonWhitespaceColumn`, `getLineLastNonWhitespaceColumn` |
| same | 1142–1174 | suite `TextModel.mightContainRTL` — only `nope` and `yes` (the two `setValue` tests go to P1-22) |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModel.cs` — **public sealed partial class** (constructor, fields, read API)
- `tests/LoomEditor.Core.Tests/Model/TestHelpers/TestTextModels.cs` — `Create(string text)` (Monaco `createTextModel`)
- `tests/LoomEditor.Core.Tests/Model/TextModelReadTests.cs` — ported + new tests
- Run `./eng/Update-PublicApi.ps1 src/LoomEditor.Core`

## Design decisions (already made — do not change)

- Constructor: `public TextModel(string text, DefaultEndOfLine defaultEol = DefaultEndOfLine.LF)`; builds the buffer
  through `PieceTreeTextBufferBuilder` (normalize EOL: true), like Monaco's `createTextBuffer`.
- **Out of the port for now:** language, options (tab size…), decorations, tokens, injected text, attached editors,
  `isTooLargeForSyncing`, disposal. Do not add stubs for them.
- C# shape: methods for things that take arguments (`GetLineContent(int)`), properties for the rest:
  `LineCount`, `VersionId`, `AlternativeVersionId`, `Eol` (string), `EndOfLineSequence`, `MightContainRtl`,
  `MightContainNonBasicAscii`, `FullModelRange`.
- `GetValue(EndOfLinePreference eol = EndOfLinePreference.TextDefined, bool preserveBom = false)`.
- Methods that take a position/range accept `Position`/`Range` **as given** here; validation comes in P1-21 (then
  these methods validate their input like Monaco does).
- Threading: `<remarks>Threading: UI thread only. Not thread-safe.</remarks>`.
- `VersionId` and `AlternativeVersionId` start at 1.

## Tests

- [x] `TextModelData.fromString` — 5 tests (`one line text`, `multiline text`, `Non Basic ASCII 1`, `containsRTL 1`, `containsRTL 2`)
- [x] The 4 listed tests from `Editor Model - TextModel`. In `getValueLengthInRange` and `getValueLengthInRange different EOL`, port only the assertions whose range is inside the text; the assertions with out-of-bounds ranges (`1000`) move to P1-21, which adds `ValidateRange`. `#50471` also moves to P1-21.
- [x] `TextModel.mightContainRTL` › `nope`, `yes`
- [x] New: empty text → 1 line, `FullModelRange` = `[1,1 -> 1,1]`; BOM preserved only with `preserveBom: true`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `textModel.ts` in progress
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Validation (P1-21), edits/events (P1-22), undo (P1-27), snapshots (P1-28).

## Suggested commit message

`feat(core): add TextModel read API`
