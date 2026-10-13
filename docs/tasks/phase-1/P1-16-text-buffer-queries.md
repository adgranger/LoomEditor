# P1-16 — `ITextBuffer` and `PieceTreeTextBuffer` read queries

- **Status:** done
- **Depends on:** P1-15
- **Size:** M

## Goal

The `ITextBuffer` contract that `TextModel` will use, and every read-only method of `PieceTreeTextBuffer`
(everything except `ApplyEdits`).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model.ts` | 1530–1601 | `IReadonlyTextBuffer` + `ITextBuffer` (as one C# interface, without search members) |
| `src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts` | 54–241 | `equals` … `setEOL` (all methods in that range) |
| `src/vs/base/common/strings.ts` | 284–325 | `firstNonWhitespaceIndex`, `lastNonWhitespaceIndex` |
| same | 503–513 | `isHighSurrogate`, `isLowSurrogate` (or use `char.IsHighSurrogate` / `char.IsLowSurrogate` — same thing) |

## Files to create or change

- `src/LoomEditor.Core/Model/ITextBuffer.cs` — `internal interface ITextBuffer`
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeTextBuffer.cs` — implement `ITextBuffer` (except `ApplyEdits`, added in P1-18)
- `src/LoomEditor.Core/Model/StringClassification.cs` — add `FirstNonWhitespaceIndex`, `LastNonWhitespaceIndex`
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeTextBufferQueryTests.cs` — create

## Design decisions (already made — do not change)

- `ITextBuffer` is `internal`: `TextModel` is the public API, the buffer is an implementation detail.
- Leave out `findMatchesLineByLine` (search, phase 5) and `applyEdits` (P1-18) for now; P1-18 adds `ApplyEdits` to the interface.
- `EndOfLinePreference` (P1-09) is a parameter with default `EndOfLinePreference.TextDefined`, like Monaco.
- `GetEol()` returns `string` (`"\n"` or `"\r\n"`); `SetEol(string)` validates its argument.
- `createSnapshot(preserveBOM)` → `CreateSnapshot(bool preserveBom)` returning `ITextSnapshot` (P1-13).

## Tests

All new (Monaco tests these through `TextModel`; P1-20 ports those):

- [x] `GetRangeAt(offset, length)` for ranges inside a line and across lines
- [x] `GetValueInRange` with `TextDefined`, `LF` and `CRLF` on a CRLF buffer
- [x] `GetValueLengthInRange` equals `GetValueInRange(...).Length` for several ranges and EOL preferences
- [x] `GetCharacterCountInRange` counts a surrogate pair as 1 (e.g. `"a😀b"`)
- [x] `GetLineMinColumn` = 1, `GetLineMaxColumn` = length + 1
- [x] `GetLineFirstNonWhitespaceColumn` / `GetLineLastNonWhitespaceColumn`: `"  ab  "` → 3 / 5; whitespace-only line → 0
- [x] `MightContainRtl`, `MightContainNonBasicAscii`, `MightContainUnusualLineTerminators` reflect the initial text

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`ApplyEdits` (P1-17, P1-18).

## Suggested commit message

`feat(core): add ITextBuffer and PieceTreeTextBuffer queries`
