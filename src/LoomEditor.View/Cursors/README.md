# Cursors

**Phase 3.** Multi-cursor state and movements (word by word, line by line, column selection…).

Monaco sources: `src/vs/editor/common/cursor/` (`cursor.ts`, `cursorMoveOperations.ts`, `cursorWordOperations.ts`…).

What is here so far:

- P3-12: the value types the cursor is built from — `SingleCursorState`, `CursorState` (and its partial forms),
  `EditOperationType`, `SelectionStartKind`, `CursorChangeReason`, `ColumnSelectData`, `EditOperationResult`,
  `ICursorSimpleModel` (read by the move operations) and `TextModelCursorSimpleModel`. The identity coordinates
  converter is in `Layout/`. `Commands/IEditCommand` is an empty declaration until P3-14.
