# P6-26 — The control's word wrap properties and a gallery switch

- **Status:** done
- **Depends on:** P6-25
- **Size:** S

## Goal

An app turns word wrap on through the public `LoomEditor` control (`WordWrap`, `WordWrapColumn`, `WrappingIndent`), and the
gallery has a switch to try it.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/config/editorOptions.ts` | 335–366 | the public doc of `wordWrap`, `wordWrapColumn`, `wrappingIndent` (copy the meaning into the XML docs) |

## Files to create or change

- `src/LoomEditor.WinUI/Controls/EditorWordWrap.cs` — create: `public enum EditorWordWrap { Off, On, WordWrapColumn, Bounded }`
- `src/LoomEditor.WinUI/Controls/EditorWrappingIndent.cs` — create: `public enum EditorWrappingIndent { None, Same, Indent, DeepIndent }`
- `src/LoomEditor.WinUI/Controls/LoomEditor.WordWrap.cs` — create (partial of the control): three dependency properties
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — update with `./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`
- `samples/LoomEditor.Gallery/MainWindow.xaml`, `MainWindow.xaml.cs` — change: a `ToggleSwitch` "Word wrap" next to "Smooth scrolling"
- `docs/testing/phase-6-manual-tests.md` — create (same shape as `docs/testing/phase-5-manual-tests.md`, a result column for the
  maintainer) with the word wrap checks (see Tests); P6-35, P6-45, P6-54, P6-61 and P6-62 add their checks to it

## Design decisions (already made — do not change)

- The public enums mirror the internal `WordWrap` / `WrappingIndent` (P6-01) and are mapped with a `switch` (no casts between
  unrelated enums, no `Enum.Parse`). Defaults: `Off`, `80`, `Same` (Monaco's).
- `WordWrapColumn` below 1 throws `ArgumentOutOfRangeException` and keeps the previous value, like `TabSize` does
  (`LoomEditor.Font.cs`).
- Each property change calls `_viewModel.SetOptions(_viewModel.LayoutOptions with { Wrapping = … }, _viewModel.ViewLayoutOptions)`
  (P6-20 reacts to it). The other wrapping options (`WordBreak`, break characters, escaped line feeds, two-cell characters) are
  not exposed yet: internal defaults.
- The gallery switch maps on/off to `EditorWordWrap.On` / `Off`.

## Tests

- [x] Manual (in `phase-6-manual-tests.md`, for the maintainer): word wrap on wraps long lines at the window width;
      resizing rewraps; typing at the end of a wrapped line keeps the caret visible; Home/End and Up/Down move by view line;
      folding a region whose first line wraps; line numbers only on the first view line
- [x] No unit test (WinUI control; covered by the View tests of P6-20)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — succeeds (Native AOT: new public API on the control) (NOT verified: fails with XamlCompiler WMC1007 on the unmodified HEAD too, environment issue)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Guides, minimap, sticky scroll options (their own cards).

## Suggested commit message

`feat(winui): expose word wrap on the editor control`
