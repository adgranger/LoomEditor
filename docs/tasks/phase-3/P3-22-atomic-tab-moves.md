# P3-22 — Atomic soft tabs (`AtomicTabMoveOperations`)

- **Status:** todo
- **Depends on:** P3-21
- **Size:** S

## Goal

With the `stickyTabStops` option, indentation made of spaces behaves like tabs: ← / → jump over a whole indentation
step. Port the two pure functions that compute this. They are used by `MoveOperations` (P3-23).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorAtomicMoveOperations.ts` | 9–13 | `const enum Direction` (`Left`, `Right`, `Nearest`) |
| same | 15–159 | `class AtomicTabMoveOperations`: `whitespaceVisibleColumn`, `atomicPosition` |
| `src/vs/editor/test/common/controller/cursorAtomicMoveOperations.test.ts` | all (155 lines) | `Test whitespaceVisibleColumn`, `Test atomicPosition` |

## Files to create or change

- `src/LoomEditor.View/Cursors/AtomicTabMoveOperations.cs` — create (with `internal enum AtomicMoveDirection`)
- `tests/LoomEditor.View.Tests/Cursors/AtomicTabMoveOperationsTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- Monaco's `Direction` enum is named `AtomicMoveDirection` in C# (there are several `Direction` enums in Monaco; C#
  needs distinct names in one namespace). Values `Left = 0, Right = 1, Nearest = 2`.
- `whitespaceVisibleColumn` returns a JavaScript tuple `[prevTabStopPosition, prevTabStopVisibleColumn, visibleColumn]`:
  return a C# named tuple `(int PrevTabStopPosition, int PrevTabStopVisibleColumn, int VisibleColumn)`.
- **Positions and the return values are 0-based** and `-1` means "not applicable", exactly as in Monaco. Do not convert
  to 1-based columns.
- It uses `CursorColumns.NextRenderTabStop` (P3-03) only.
- The test file's suite is (mis)named `'Cursor move command test'` in Monaco: keep the test names `Test whitespaceVisibleColumn`
  and `Test atomicPosition` in the `// Monaco:` comments. Each Monaco test is a table: port it as one `[Fact]` that
  loops over the same table (copy every value; the failure message names the case). Every
  case and every expected array must be kept.

## Tests

- [ ] Port `cursorAtomicMoveOperations.test.ts` › `Test whitespaceVisibleColumn`
- [ ] Port `cursorAtomicMoveOperations.test.ts` › `Test atomicPosition`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `cursorAtomicMoveOperations.ts` (✅, P3-22); phase 3 tests table row
      `cursorAtomicMoveOperations.test.ts` 2 / 2
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Using it from the moves (P3-23).

## Suggested commit message

`feat(view): port the atomic soft tab moves`
