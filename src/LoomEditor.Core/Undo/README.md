# Undo

**Phase 1.** Undo/redo stack and grouping of edits (typing several characters = one undo step).

Monaco sources: `src/vs/editor/common/model/editStack.ts`, `src/vs/platform/undoRedo/common/undoRedoService.ts`,
`src/vs/editor/common/core/textChange.ts`.

Ported so far:

- `TextChange` and `TextChangeCompressor.CompressConsecutiveTextChanges` (P1-26), without Monaco's binary serialization.

- `SingleModelEditStackData`, `SingleModelEditStackElement`, `EditStack` and `UndoRedoStack` (P1-27), used by `TextModel.PushEditOperations`, `PushEol`, `Undo` and `Redo`.
