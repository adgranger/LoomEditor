# P3-21 — `EditorContext` and the control use the cursors

- **Status:** todo
- **Depends on:** P3-20
- **Size:** M

## Goal

Plug the cursor into the editor. `EditorContext` creates the `ViewModel` (and so the cursors) in its constructor
(ADR 0006, decision 3), `IEditorContext` gets multi-cursor members, `Selection` becomes the **primary** selection,
and the control stops creating its own `ViewModel`. Nothing changes for the user yet (Ctrl+A still selects all), but
from now on every selection lives in the cursor.

## Monaco sources

None (integration card). Monaco equivalents for reference: `ICodeEditor.getSelections` / `setSelections`
(`src/vs/editor/browser/editorBrowser.ts`), `onDidChangeCursorSelection`.

## Files to create or change

- `src/LoomEditor.View/Layout/FontInfo.cs` — change: `static FontInfo Default { get; }` (placeholder font, see below)
- `src/LoomEditor.View/Features/IEditorContext.cs`, `EditorContext.cs` — change
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — update with `./eng/Update-PublicApi.ps1 src/LoomEditor.View`
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs`, `LoomEditor.Scrolling.cs`, `LoomEditor.Font.cs`, `LoomEditor.Caret.cs` — change
- `tests/LoomEditor.View.Tests/Features/EditorContextTests.cs` — create (or extend if it exists)

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `FontInfo.Default` = Monaco's test font (`new FontInfo(1, "mockFont", "normal", 14, 19, 1.5, true, 10, 20, true, 10, 10, 10, 10)`)
  with `FontFamily = "Consolas"`. XML doc: "Placeholder used until the control measures the real font".
- `EditorContext`:
  - public constructor `EditorContext()` (unchanged signature) → creates the `ViewModel` with `FontInfo.Default`,
    `new EditorLayoutOptions()`, `new ViewLayoutOptions(FontInfo.Default.LineHeight)`, a scheduler that never runs
    (returns a no-op `IDisposable`) and `TimeProvider.System`;
  - new `internal EditorContext(Func<Action, IDisposable> scheduleAtNextAnimationFrame, TimeProvider timeProvider)`
    used by the control (smooth scrolling needs the real scheduler);
  - `internal ViewModel ViewModel { get; }`.
  - `Selection` getter → `ViewModel.GetSelection()`; setter → `ViewModel.SetSelections("api", [value])`
    (Monaco's `source` for `setSelection` from the API is `"api"`).
  - **Public, new:** `IReadOnlyList<Selection> Selections { get; }` → `ViewModel.GetSelections()`;
    `void SetSelections(IReadOnlyList<Selection> selections)` → `ViewModel.SetSelections("api", selections)`;
    throws `ArgumentException` when the list is empty.
  - `SelectionChanged` is raised when the dispatcher emits a `CursorStateChangedEvent` (subscribe to
    `ViewModel.EventDispatcher.OnEvent` in the constructor).
  - `SetText` only calls `Model.SetValue(text)`: the cursor resets itself to `(1,1)` on a flush (P3-18).
- `IEditorContext` gets `Selections` and `SetSelections` (public interface change, listed in `PublicAPI.Unshipped.txt`).
- The control (`LoomEditor.WinUI`):
  - creates `_context = new EditorContext(ScheduleAtNextAnimationFrame, TimeProvider.System)` in the constructor;
    `_viewModel` becomes `_context.ViewModel` (field kept for brevity, assigned once). The `ViewLayout` and
    `LayoutInfoChanged` subscriptions move to the constructor.
  - `CreateResources` no longer creates or disposes the view model: it creates the measurer and the renderer and calls
    `_viewModel.SetFontInfo(_measurer.FontInfo)` and `SetViewportSize`. `ReleaseResources` keeps the view model.
  - The pending scroll position of P2-17 can now be applied directly (the view model always exists). Keep the
    `ScrollTop` / `ScrollLeft` behavior identical; remove the pending fields only if no test or caller needs them.
  - Focus: `OnCaretFocusChanged` also calls `_viewModel.SetHasFocus(HasFocus)` (undo/redo restore selections only
    with focus, P3-18).
  - The renderer still draws the primary selection (`_context.Selection`); several cursors are drawn in P3-42.

## Tests

- [ ] New: `new EditorContext().Selection` is `(1,1)`; `SetSelections([a, b])` → `Selections` is `[a, b]`, `Selection == a`,
      `SelectionChanged` raised once
- [ ] New: `SetText("x\ny")` after a selection on line 2 resets to `(1,1)` and raises `SelectionChanged`
- [ ] New: `SetSelections([])` throws `ArgumentException`
- [ ] New: the existing `SelectAllFeature` tests still pass (Ctrl+A goes through `Selection =`)
- [ ] Manual (Gallery, Debug): the editor starts, Ctrl+A selects all, scrolling and font changes still work

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `PublicAPI.Unshipped.txt` of `LoomEditor.View`: only `IEditorContext.Selections`, `IEditorContext.SetSelections`,
      `EditorContext.Selections`, `EditorContext.SetSelections` added
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the app starts
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Keyboard, mouse and typing (later cards).

## Suggested commit message

`feat(view): keep the selections in the cursor and expose multi-cursor selections`
