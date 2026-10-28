# FontZoom

**Role:** adds `editor.action.fontZoomIn`, `fontZoomOut` and `fontZoomReset`, which change the zoom level of the editor
(`EditorContext.Zoom`, one `EditorZoomState` per editor). They have no default key.

**Monaco source:** `src/vs/editor/contrib/fontZoom/browser/fontZoom.ts`, `src/vs/editor/common/config/editorZoom.ts`.

**Files:**
- `FontZoomFeature.cs` — registers the commands.
- `FontZoomCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/FontZoom/`, `tests/LoomEditor.View.Tests/Layout/EditorZoomStateTests.cs`.

**Known limits:** the zoom is not shared between editors and is not persisted.
