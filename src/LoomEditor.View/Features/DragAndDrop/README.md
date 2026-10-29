# DragAndDrop

**Role:** moves (Ctrl: copies) the selected text when the user drags it with the mouse. The mouse operation (P10-17) reports
the drag as view events; `DragAndDropController` shows the drop target (an empty `dnd-target` decoration, drawn as a 2 px bar),
sets `EditorContext.MouseStyle` (move / copy) and runs `DragAndDropCommand` on drop. A click inside a selection collapses it.

**Monaco source:** `src/vs/editor/contrib/dnd/browser/dnd.ts`, `dragAndDropCommand.ts`, `dnd.css`.

**Files:**
- `DragAndDropFeature.cs` — attaches the controller.
- `DragAndDropController.cs` — the state machine (`dnd.ts`).
- `DragAndDropCommand.cs` — the edit and the resulting selection.

**Tests:** `tests/LoomEditor.View.Tests/Features/DragAndDrop/`.

**Known limits:** solid drop marker (Monaco: dotted); no copy cursor (WinUI 3 has none); Ctrl is the only trigger modifier
(no macOS Alt); no drop from other applications.
