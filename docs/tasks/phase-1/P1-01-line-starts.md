# P1-01 — Line starts

- **Status:** done
- **Depends on:** —
- **Size:** S

## Goal

A pure, tested function that finds where each line starts in a string (handling `\r`, `\n` and `\r\n`) and counts
each kind of line break. Every other piece tree class depends on it.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` | 27–35 | class `LineStarts` |
| same | 37–62 | `createLineStartsFast` |
| same | 64–99 | `createLineStarts` |

Do **not** port `createUintArray` (16–25): it only exists to save memory in JavaScript.

## Files to create or change

- `src/LoomEditor.Core/Model/PieceTree/LineStarts.cs` — create
- `tests/LoomEditor.Core.Tests/Model/PieceTree/LineStartsTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class LineStarts` with read-only properties: `IReadOnlyList<int> Starts`, `int CarriageReturnCount`
  (Monaco `cr`), `int LineFeedCount` (`lf`), `int CarriageReturnLineFeedCount` (`crlf`), `bool IsBasicAscii`.
- Two static methods:
  - `public static List<int> CreateFast(string text)` — Monaco `createLineStartsFast`. Always returns a new
    mutable `List<int>` (the change buffer appends to it later). Drop Monaco's `readonly` parameter.
  - `public static LineStarts Create(string text)` — Monaco `createLineStarts`. Drop Monaco's reusable `r` array
    parameter (a JS allocation optimization).
- Offsets are `int` (UTF-16 code units, like `string.Length`). The first entry is always `0`.
- "Basic ASCII" = every char that is not a line break is a tab or in `32..126` (same rule as Monaco).

## Tests

All new (Monaco tests these functions only indirectly):

- [x] `CreateFast("")` → `[0]`
- [x] `CreateFast("a\nb\r\nc\rd")` → `[0, 2, 5, 7]`
- [x] `\r\n` counts as ONE line break; `\n\r` counts as TWO
- [x] text ending with a line break → last start equals `text.Length`
- [x] `Create` counts: `"a\r\nb\nc\rd\r\n"` → `CarriageReturnLineFeedCount = 2`, `LineFeedCount = 1`, `CarriageReturnCount = 1`
- [x] `IsBasicAscii`: `"abc\tdef"` → true; `"é"` → false; `"\u0001"` → false; line breaks do not make it false
- [x] `Create(text).Starts` equals `CreateFast(text)` for a few inputs (`[Theory]`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: add a row for `pieceTreeBase.ts` (status "in progress, P1-01")
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Everything else in `pieceTreeBase.ts`.

## Suggested commit message

`feat(core): port piece tree line starts`
