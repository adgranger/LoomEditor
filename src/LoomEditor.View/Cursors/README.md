# Cursors

**Phase 3.** Multi-cursor state, movements, and every edit the cursor makes (typing, deleting, indenting, paste, cut,
composition). Monaco sources: `src/vs/editor/common/cursor*` and `src/vs/editor/common/cursorCommon.ts`.

How it fits together (the entry point is `CursorsController`, owned by `ViewModel`):

- **State**: `SingleCursorState` / `CursorState`, `CursorCollection` (one `Cursor` per selection, normalized by
  `CursorCollection.Normalize`), `CursorConfiguration` (what the operations read: options, font, layout).
- **Movement**: `MoveOperations`, `AtomicTabMoveOperations`, `WordOperations`, `ColumnSelection`; `CursorMoveCommands`
  and `CursorMove` turn them into cursor state changes.
- **Editing**: `TypeOperations` / `TypeEditOperations*` (typing, Enter, Tab), `DeleteOperations`, `ShiftCommand`
  (in `Commands/`), `PasteOperations`, `CompositionState` (IME).
- **Executing**: an operation gives `IEditCommand`s; `CommandExecutor` runs them with `TextModel.PushEditOperations`
  and tracks the resulting selections (`ResultingSelection`).

Cut points for phases 4–5 (auto-closing, auto-indent, surround, electric characters) are listed in
`docs/PORTING.md`, "Cut points for phases 4–5".
