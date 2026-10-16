# P3-25 — `CursorMoveCommands` (2/2): `simpleMove`, `viewportMove` and the `cursorMove` arguments

- **Status:** done
- **Depends on:** P3-24
- **Size:** M

## Goal

Port the general move: in a direction (left, right, up, down, blank lines, line start/center/end…) by a unit
(character, half line, line, wrapped line, folded line) a number of times, and the moves relative to the viewport
(top, center, bottom, "if outside"). Port the argument parser of Monaco's `cursorMove` command
(`{ to, by, value, select }`), which the arrow keys, Page Up/Down and the tests use.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/cursor/cursorMoveCommands.ts`. If a function is not exactly at the given line, search for its name.

| Lines | What to port |
|---|---|
| 273–355 | `simpleMove` |
| 357–415 | `viewportMove`, `findPositionInViewportIfOutside` |
| 417–438 | `_firstLineNumberInRange`, `_lastLineNumberInRange` |
| 440–486 | `_moveLeft`, `_moveHalfLineLeft`, `_moveRight`, `_moveHalfLineRight` |
| 488–522 | `_moveDownByViewLines`, `_moveDownByModelLines`, `_moveUpByViewLines`, `_moveUpByModelLines` |
| 524–630 | `_moveDownByFoldedLines`, `_moveUpByFoldedLines`, `_targetFoldedDown`, `_targetFoldedUp` (hidden areas: see below) |
| 639–692 | `_moveToViewMinColumn`, `_moveToViewFirstNonWhitespaceColumn`, `_moveToViewCenterColumn`, `_moveToViewMaxColumn`, `_moveToViewLastNonWhitespaceColumn` |
| 695–980 | `namespace CursorMove`: `RawDirection`, `RawUnit`, `RawArguments`, `parse`, `ParsedArguments`, `SimpleMoveArguments`, `Direction`, `Unit` — **not** `metadata` (the JSON schema, 727–782) |

## Files to create or change

- `src/LoomEditor.View/Cursors/CursorMoveCommands.Simple.cs`, `CursorMoveCommands.Viewport.cs` — create (partials)
- `src/LoomEditor.View/Cursors/CursorMove.cs` — create (`CursorMoveDirection`, `CursorMoveUnit`, `CursorMoveArguments`,
  `CursorMoveParsedArguments`, `CursorMove.Parse`)
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `GetCompletelyVisibleViewRange()`, `GetHiddenAreas()`,
  `GetTextDirection(int lineNumber)`
- `tests/LoomEditor.View.Tests/Cursors/CursorMoveTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first — especially `Math.round` (§4.1): half-line moves (lines ~458, ~482),
  the center column (~666) and the viewport center (~374) round with JavaScript semantics.
- `CursorMoveDirection` / `CursorMoveUnit` are C# enums with Monaco's members **in Monaco's order** (`Direction` and
  `Unit` of lines 929–980). `SimpleMoveDirection` / `ViewportDirection` are not separate types: they are subsets of
  `CursorMoveDirection`; add a comment.
- `CursorMoveArguments` is `internal sealed record CursorMoveArguments(string To, bool Select = false, string? By = null, int Value = 1, bool NoHistory = false)`.
  `RawDirection` / `RawUnit` become `internal static class` with `const string` fields (`Left = "left"`…), the exact
  strings of lines 784–815. `CursorMove.Parse(CursorMoveArguments args)` returns `CursorMoveParsedArguments?` (null
  for illegal arguments), port of lines 828–912.
- `ViewModel.GetCompletelyVisibleViewRange()` is Monaco's (viewModelImpl.ts 703–712): it uses
  `ViewLayout.GetLinesViewportData().CompletelyVisibleStartLineNumber/EndLineNumber`. The Monaco tests replace this
  method (`viewModel.getCompletelyVisibleViewRange = …`): add `internal Func<Range>? CompletelyVisibleViewRangeOverride { get; set; }`
  on `ViewModel`, used when not null, with an XML doc "test hook".
- `GetHiddenAreas()` returns an empty list (no folding until phase 5): the folded-line moves then behave like line moves.
  `GetTextDirection(int lineNumber)` returns `TextDirection.LTR` (new `internal enum TextDirection { LTR, RTL }`): the RTL
  swap of `_moveLeft`/`_moveRight` is kept but never taken. Document both in `docs/PORTING.md`.

## Tests

The Monaco test file `cursorMoveCommand.test.ts` runs through the `cursorMove` core command (P3-26). Direct tests here:

- [x] New: `CursorMove.Parse` — `{ to: "left" }` → Left / Character / 1; `{ to: "down", by: "wrappedLine", value: 3, select: true }`;
      `{ to: "nope" }` → null; `{ to: "left", value: 0 }` keeps Monaco's behavior (read `parse` and assert what it does)
- [x] New: `SimpleMove` Left by `HalfLine` on `"abcdefghi"` (9 characters) from `(1,10)` → `(1,5)`: it moves
      `Math.round(4.5) = 5` columns with JavaScript rounding (C# `Math.Round(4.5)` would give 4: this test catches it)
- [x] New: `ViewportMove` `ViewPortTop` with `CompletelyVisibleViewRangeOverride = () => new Range(10, 1, 20, 1)` → line 10
- [x] New: `FindPositionInViewportIfOutside` keeps a cursor that is inside the visible range and moves one that is above it

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `cursorMoveCommands.ts` (✅ except `word`, P3-25); behavior differences "no hidden areas
      before folding (phase 5)" and "text direction is always LTR in phase 3"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The core commands by id (P3-26), folding.

## Suggested commit message

`feat(view): port simpleMove, viewportMove and the cursorMove arguments`
