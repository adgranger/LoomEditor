# Undo

Undo/redo stack and grouping of edits (typing several characters = one undo step). Ported in phase 1.

Monaco sources: `src/vs/editor/common/model/editStack.ts`, `src/vs/platform/undoRedo/common/undoRedoService.ts`,
`src/vs/editor/common/core/textChange.ts`.

- `TextChange` and `TextChangeCompressor.CompressConsecutiveTextChanges`: a text change and the merging of
  consecutive changes (without Monaco's binary serialization).
- `SingleModelEditStackData`, `SingleModelEditStackElement`: one undo step of one model.
- `EditStack`: opens and closes undo steps for a `TextModel` (`PushEditOperations`, `PushEol`, `PushStackElement`).
- `UndoRedoStack`: a small per-model stack that replaces Monaco's workspace-wide `IUndoRedoService`.

Used by `TextModel.PushEditOperations`, `PushEol`, `Undo` and `Redo`. See docs/PORTING.md for the differences.
