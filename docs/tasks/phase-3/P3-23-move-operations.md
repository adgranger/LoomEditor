# P3-23 — `MoveOperations`: left, right, up, down, line and buffer edges

- **Status:** todo
- **Depends on:** P3-22
- **Size:** M

## Goal

Port the elementary cursor moves: one grapheme left or right, one line up or down keeping the visible column (with the
"leftover" visible columns that remember where the cursor wanted to be), to the previous/next blank line, to the start
or end of a line or of the buffer. They are pure functions over `CursorConfiguration`, `ICursorSimpleModel` and
`SingleCursorState`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/cursor/cursorMoveOperations.ts` (356 lines). Port the whole file.

| Lines | What to port |
|---|---|
| 15–27 | `class CursorPosition` (`lineNumber`, `column`, `leftoverVisibleColumns`) |
| 30–155 | horizontal: `leftPosition`, `leftPositionAtomicSoftTabs`, `left`, `moveLeft`, `clipPositionColumn`, `clipRange`, `rightPosition`, `rightPositionAtomicSoftTabs`, `right`, `moveRight` |
| 157–280 | vertical: `vertical`, `down`, `moveDown`, `translateDown`, `up`, `moveUp`, `translateUp` |
| 282–355 | `_isBlankLine`, `moveToPrevBlankLine`, `moveToNextBlankLine`, `moveToBeginningOfLine`, `moveToEndOfLine`, `moveToBeginningOfBuffer`, `moveToEndOfBuffer` |
| `src/vs/base/common/uint.ts` | search `MAX_SAFE_SMALL_INTEGER` | `Constants.MAX_SAFE_SMALL_INTEGER = 1 << 30` |

## Files to create or change

- `src/LoomEditor.View/Cursors/CursorPosition.cs` — create
- `src/LoomEditor.View/Cursors/MoveOperations.cs` — create (`static partial class`; split as
  `MoveOperations.Horizontal.cs` / `MoveOperations.Vertical.cs` / `MoveOperations.Lines.cs` to stay under ~400 lines each)
- `tests/LoomEditor.View.Tests/Cursors/MoveOperationsTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first — especially §4 (JavaScript traps).
- `internal static partial class MoveOperations`, `internal readonly record struct CursorPosition(int LineNumber, int Column, int LeftoverVisibleColumns)`.
- `position.delta(undefined, -n)` → `position.Delta(deltaColumn: -n)`.
- `strings.prevCharLength` / `nextCharLength` → `Strings.PrevCharLength` / `NextCharLength` (P3-02): moves are by
  **grapheme**. Never step by one `char`.
- `MaxSafeSmallInteger = 1 << 30` is a `const int` in `MoveOperations`. "Sticky" end of line stores
  `MaxSafeSmallInteger - maxColumn` in `LeftoverVisibleColumns`; when it is added to a visible column later
  (`vertical`), the sum stays below `int.MaxValue` (2^30 + a line length): no overflow, but **do not** change the type.
- `moveDown` contains a loop that retries up to 10 times when normalization moves backwards (lines ~216–224). It only
  matters with injected text; port it as it is.
- `getLineFirstNonWhitespaceColumn` returns 0 for a blank line: `moveToBeginningOfLine` uses `|| minColumn` → write
  `firstNonWs == 0 ? minColumn : firstNonWs` (conventions §4.5); `_isBlankLine` tests `== 0`.
- `vertical(...)` has the `allowMoveOnEdgeLine` behavior (first line → min column, last line → max column) and resets
  the leftover columns when starting at the first/last buffer position (`wasAtEdgePosition`): port exactly.
- Atomic soft tabs use `AtomicTabMoveOperations` (P3-22) when `config.StickyTabStops` is true.

## Tests

The Monaco tests of these functions go through the core commands (`cursor.test.ts`, P3-27). Here, write direct tests
with `CursorTestContext` (P3-16):

- [ ] New: `MoveLeft` from `(1,3)` of `"a😀b"` goes to `(1,2)`; `MoveRight` from `(1,2)` goes to `(1,4)` (the emoji is
      one grapheme of 2 UTF-16 units)
- [ ] New: `MoveLeft` at `(2,1)` goes to the end of line 1; `MoveRight` at the end of a line goes to `(next,1)`
- [ ] New: `MoveDown` from `(1,10)` of `["aaaaaaaaaaaa", "bb", "cccccccccccc"]` → `(2,3)` with leftover 7; again →
      `(3,10)` (the leftover visible columns are used)
- [ ] New: with tab size 4, `MoveDown` from `(1,5)` of `["    x", "\tx"]` lands at `(2,2)` (visible column 4)
- [ ] New: `MoveUp` from line 1 goes to `(1,1)`; `MoveDown` from the last line goes to its max column
- [ ] New: `MoveToEndOfLine(sticky: true)` sets `LeftoverVisibleColumns == (1 << 30) - maxColumn`, and a following
      `MoveDown` lands at the end of the next line
- [ ] New: `MoveToNextBlankLine` / `MoveToPrevBlankLine` on `["a", "", "b", "", "c"]` from `(1,1)`, three times → `(2,1)`, `(4,1)`, `(5,1)` (the last line, column 1); `MoveToPrevBlankLine` from `(5,1)` → `(4,1)`
- [ ] New: with `StickyTabStops = true` and 4 spaces of indentation, `MoveRight` from `(1,1)` goes to `(1,5)`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `cursorMoveOperations.ts` (✅, P3-23)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`CursorMoveCommands` (P3-24, P3-25), word moves (P3-29), column selection (P3-28).

## Suggested commit message

`feat(view): port the elementary cursor moves`
