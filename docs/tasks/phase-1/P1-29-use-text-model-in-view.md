# P1-29 — Use `TextModel` in the editor (`IEditorContext.Model`)

- **Status:** todo
- **Depends on:** P1-27, P1-28
- **Size:** S

## Goal

Replace the temporary `IEditorContext.Lines` (a `string[]`) by the real `TextModel`, everywhere: View, Rendering,
WinUI and the Gallery. The editor stays read-only on screen (typing is phase 3), but its text now lives in the model.

## Monaco sources

None.

## Files to create or change

- `src/LoomEditor.View/Features/IEditorContext.cs` — replace `IReadOnlyList<string> Lines` by `TextModel Model { get; }`
- `src/LoomEditor.View/Features/EditorContext.cs` — own a `TextModel`; `SetText` → `Model.SetValue`; forward
  `Model.ContentChanged` as `TextChanged` (or remove `TextChanged` and let callers subscribe to the model — choose the
  latter, it is simpler)
- `src/LoomEditor.View/Features/SelectAll/SelectAllFeature.cs` — compute the selection from `Model.FullModelRange`
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — read lines through `Model.LineCount` / `Model.GetLineContent`
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — invalidate the canvas on `Model.ContentChanged`
- Tests in `tests/LoomEditor.View.Tests/` — update; `docs/how-to/add-a-feature.md` and `add-a-command.md` — update the examples
- PublicAPI files of View, Rendering, WinUI — update (`./eng/Update-PublicApi.ps1 src/<Project>`; remove obsolete lines by hand)

## Design decisions (already made — do not change)

- `SelectAllFeature.ComputeSelection` becomes `ComputeSelection(TextModel model)` and returns
  `Selection.FromRange(model.FullModelRange, SelectionDirection.LeftToRight)`.
- No other behavior change: same rendering, same Ctrl+A.

## Tests

- [ ] Existing View tests pass after the update
- [ ] New: `SelectAll` on a model edited with `ApplyEdits` selects the new full range
- [ ] Gallery: `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, and the app starts

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] AOT publish of the Gallery as above
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Typing, cursors (phase 3), view lines (phase 2).

## Suggested commit message

`refactor(view): use TextModel in the editor context`
