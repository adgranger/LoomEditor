# SelectionAnchor

**Role:** adds `editor.action.setSelectionAnchor` (`Ctrl+K Ctrl+B`), `goToSelectionAnchor`, `selectFromAnchorToCursor`
(`Ctrl+K Ctrl+K`) and `cancelSelectionAnchor` (`Escape`). The anchor is one empty model decoration (class
`selection-anchor`, drawn as a 2 px blue bar); the context key `selectionAnchorSet` is `EditorContext.SelectionAnchorSet`.

**Monaco source:** `src/vs/editor/contrib/anchorSelect/browser/anchorSelect.ts`, `anchorSelect.css`.

**Files:**
- `SelectionAnchorFeature.cs` — the anchor and the four commands.
- `SelectionAnchorCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/SelectionAnchor/`.

**Known limits:** no hover message. The `Escape` rule has the weight of `cancelSelection` (the lowest `Escape` rule), so the
find widget, the hover and the suggest widget close first.
