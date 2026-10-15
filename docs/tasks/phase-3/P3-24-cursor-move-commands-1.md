# P3-24 — `CursorMoveCommands` (1/2): line, buffer, select all, add cursor, move to

- **Status:** todo
- **Depends on:** P3-23
- **Size:** M

## Goal

`CursorMoveCommands` turns a user intention ("go to the start of the line", "add a cursor below", "select this line",
"move to this position") into new cursor states for **all** cursors, on top of `MoveOperations`. This card ports the
first half of the class; P3-25 ports `simpleMove` / `viewportMove` and the `cursorMove` arguments.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/cursor/cursorMoveCommands.ts` (980 lines). If a function is not exactly at the given
line, search for its name.

| Lines | What to port |
|---|---|
| 16–46 | `addCursorDown`, `addCursorUp` (use `MoveOperations.translateDown/Up`) |
| 48–84 | `moveToBeginningOfLine`, `_moveToLineStart`, `_moveToLineStartByView`, `_moveToLineStartByModel` |
| 86–122 | `moveToEndOfLine`, `_moveToLineEnd`, `_moveToLineEndByView`, `_moveToLineEndByModel` |
| 124–176 | `expandLineSelection`, `moveToBeginningOfBuffer`, `moveToEndOfBuffer`, `selectAll` |
| 177–235 | `line` (line selection by mouse, with `SelectionStartKind.Line`) |
| 241–272 | `cancelSelection`, `moveTo` |
| 631–638 | `_moveToViewPosition`, `_moveToModelPosition` |

## Files to create or change

- `src/LoomEditor.View/Cursors/CursorMoveCommands.cs` — create (`internal static partial class`)
- `tests/LoomEditor.View.Tests/Cursors/CursorMoveCommandsTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- Monaco's functions take `viewModel: IViewModel`. In C# they take the concrete `ViewModel` (P3-20), which has the
  members they read: `CursorConfig`, `Model`, `CoordinatesConverter`, `GetLineMinColumn`… (the `ICursorSimpleModel`
  members). Add to `ViewModel` any small read-only member a function needs **only if** it is a one-line forward to the
  model or the layout (e.g. `GetLineFirstNonWhitespaceColumn`). Anything bigger: stop and ask.
- Return types: `PartialCursorState` / `CursorState` lists as in Monaco (`IReadOnlyList<PartialCursorState>`).
- The `ByView` and `ByModel` variants of line start/end give the same result while view lines are model lines.
  **Keep both code paths** (they diverge with word wrap in phase 6); do not merge them.
- `word` (lines 236–240) needs `WordOperations.word` and is ported in P3-30. Leave it out (no stub).
- `moveTo(viewModel, cursor, inSelectionMode, position, viewPosition)`: `viewPosition` can be `undefined` in Monaco:
  use `Position? viewPosition`.
- `selectAll` replaces nothing in `SelectAllFeature` yet; the core command `editor.action.selectAll` is rewired in P3-26.

## Tests

Most Monaco tests for these functions run through the core commands (P3-26, P3-27). Direct tests here, with
`TestCodeEditor` (P3-20):

- [ ] New: `MoveToBeginningOfLine` on `"   abc"` from `(1,6)` → `(1,4)` (first non-whitespace); again from `(1,4)` → `(1,1)`
- [ ] New: `MoveToEndOfLine` from `(1,1)` → max column; `sticky: true` sets the sticky leftover columns
- [ ] New: `AddCursorDown` with one cursor on `(1,3)` of 3 lines → 2 cursors `(1,3)` and `(2,3)`
- [ ] New: `Line` (line select) on line 2 of 3 → selection `(2,1) → (3,1)`, kind `Line`
- [ ] New: `ExpandLineSelection` twice from `(1,2)` → `(1,1) → (2,1)` then `(1,1) → (3,1)`
- [ ] New: `SelectAll` → `(1,1) → (last, max)`; `CancelSelection` collapses to the position
- [ ] New: `MoveTo(inSelectionMode: true)` keeps the selection start

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `cursorMoveCommands.ts` (🔶 part 1, P3-24)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`simpleMove`, `viewportMove`, `CursorMove` arguments (P3-25), `word` (P3-30), the commands registered by id (P3-26).

## Suggested commit message

`feat(view): port CursorMoveCommands line, buffer and cursor helpers`
