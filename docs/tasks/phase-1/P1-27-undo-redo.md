# P1-27 — Undo / redo

- **Status:** done
- **Depends on:** P1-26
- **Size:** M

## Goal

`TextModel.Undo()` / `Redo()` with Monaco's grouping rules: edits pushed with `PushEditOperations` between two
`PushStackElement()` calls form one undo step; EOL changes are undoable; each step remembers the cursor state
before and after, so the editor can restore selections.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/editStack.ts` | 24–143 | `SingleModelEditStackData` — **without** `serialize`/`deserialize` and the `_write*`/`_read*` helpers |
| same | 149–241 | `SingleModelEditStackElement` — keep the data form only (no serialized buffer) |
| same | 368–376, 384–451 | `getModelEOL`, `EditStack` |
| `src/vs/editor/common/model/textModel.ts` | 1269–1339 | `pushStackElement`, `popStackElement`, `pushEOL` |
| same | 1340–1440 | `pushEditOperations`, `_pushEditOperations` |
| same | 1441–1482 | `_applyUndo`, `_applyRedo` |
| same | 1604–1620 | `undo`, `canUndo`, `redo`, `canRedo` |

Monaco delegates the actual stack to `IUndoRedoService` (`src/vs/platform/undoRedo/common/undoRedoService.ts`, ~1,500
lines, workspace-wide undo). **Do not port it.** Write a small per-model stack instead (below).

## Files to create or change

`src/LoomEditor.Core/Undo/` (all `internal`):
- `SingleModelEditStackData.cs`, `SingleModelEditStackElement.cs`, `EditStack.cs`
- `UndoRedoStack.cs` — replaces `IUndoRedoService` for one model: `Push(element)`, `GetLastElement()` (the open
  element, if any), `Undo()`, `Redo()`, `CanUndo`, `CanRedo`, `Clear()`. Pushing clears the redo list.

`src/LoomEditor.Core/Model/TextModel.Undo.cs` — public API:
- `void PushStackElement()`, `void PopStackElement()`
- `IReadOnlyList<Selection>? PushEditOperations(IReadOnlyList<Selection>? beforeCursorState, IReadOnlyList<TextEdit> edits, Func<IReadOnlyList<TextEdit>, IReadOnlyList<Selection>?>? cursorStateComputer)`
  — the computer receives the **inverse** edits, as in Monaco
- `void PushEol(EndOfLineSequence eol)`
- `IReadOnlyList<Selection>? Undo()` / `IReadOnlyList<Selection>? Redo()` — return the cursor state to restore (null if nothing happened)
- `bool CanUndo`, `bool CanRedo`
- `SetValue` clears the undo stack (Monaco does)

Tests: `tests/LoomEditor.Core.Tests/Undo/UndoRedoStackTests.cs`, `tests/LoomEditor.Core.Tests/Model/TextModelUndoTests.cs`.
Run `./eng/Update-PublicApi.ps1 src/LoomEditor.Core`.

## Design decisions (already made — do not change)

- `ApplyEdits` (P1-22) does **not** record undo (same as Monaco); only `PushEditOperations` and `PushEol` do.
- Undo/redo events: `ContentChanged` with `IsUndoing` / `IsRedoing` set; `AlternativeVersionId` goes back to the value
  it had at that point (that is what Monaco's `_overwriteAlternativeVersionId` is for — "is the document back to saved?").
- Monaco's `editStack.test.ts` only tests binary serialization: not applicable, do not port.

## Tests

All new:
- [x] Two `PushEditOperations` without `PushStackElement` between them → one `Undo()` reverts both
- [x] With `PushStackElement()` between them → two undo steps
- [x] `Undo` then `Redo` → text and `AlternativeVersionId` identical to before the undo
- [x] New edit after an undo clears redo (`CanRedo` false)
- [x] `PushEol(CRLF)` is undoable
- [x] Cursor state: `Undo()` returns the before-state, `Redo()` the after-state given by `cursorStateComputer`
- [x] Surrogate pairs survive undo/redo (`'👁'` case from Monaco issue #47733, done with `PushEditOperations`)
- [x] Seeded random: 100 random `PushEditOperations` batches with random `PushStackElement`, then undo everything →
  original text; redo everything → final text

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `editStack.ts` ported (data form only) + "Behavior differences": `UndoRedoStack` replaces `IUndoRedoService`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Multi-model (workspace) undo. Keyboard shortcuts for undo (phase 3).

## Suggested commit message

`feat(core): add undo and redo to TextModel`
