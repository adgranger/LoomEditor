# P6-61 — Toggle sticky scroll: command, control property, gallery

- **Status:** done
- **Depends on:** P6-60
- **Size:** S

## Goal

Sticky scroll can be turned on and off: the `editor.action.toggleStickyScroll` command, a public `StickyScrollEnabled`
property on the control (default `true`, Monaco's default), and a gallery switch.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/stickyScroll/browser/stickyScrollActions.ts` | 18–55 | `ToggleStickyScroll` (id, the toggle, focus kept) |
| same | 57–150 | the focus / navigation commands (informative: not ported, see out of scope) |
| `src/vs/editor/common/config/editorOptions.ts` | 3173–3180 | the doc of `enabled` and `maxLineCount` (XML docs) |

## Files to create or change

- `src/LoomEditor.View/Features/StickyScroll/StickyScrollCommands.cs` — create: the command id constant and its registration in
  the feature's `Attach` (follow `FoldingCommands.cs`); no default keybinding (Monaco has none)
- `src/LoomEditor.WinUI/Controls/LoomEditor.StickyScroll.cs` — create: `public bool StickyScrollEnabled` (default `true`),
  `public int StickyScrollMaxLineCount` (default `5`, 1–20, out of range throws like `TabSize`); they set `EditorLayoutOptions.StickyScroll`
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — update (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `samples/LoomEditor.Gallery/MainWindow.xaml(.cs)` — change: a "Sticky scroll" toggle; `docs/testing/phase-6-manual-tests.md` gets the checks (create the file as P6-26 describes if it does not exist yet)
- `tests/LoomEditor.View.Tests/Features/StickyScroll/StickyScrollCommandsTests.cs` — create

## Design decisions (already made — do not change)

- The command toggles the editor's `EditorLayoutOptions.StickyScroll.Enabled` through the view model (there is no configuration
  service); the control's property reflects it (the command raises the change, the control updates its dependency property without
  re-entering).
- Command id: `editor.action.toggleStickyScroll` (Monaco's).

## Tests

- [x] New: `ToggleStickyScroll_DisablesThenEnables`
- [x] Manual (`phase-6-manual-tests.md`): sticky lines follow the scroll in a long C# file, nested scopes stack, a click reveals, the chevron folds,
      the toggle hides the widget

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — succeeds (NOT verified: XamlCompiler WMC1007, known environment issue)
- [x] `docs/roadmap/phase-6-view-extras.md`: tick "Sticky scroll"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`focusStickyScroll`, `selectNextStickyScrollLine`, `selectPreviousStickyScrollLine`, `goToFocusedStickyScrollLine`, `selectEditor`
(keyboard navigation inside the widget: a follow-up), the outline model (phase 8).

## Suggested commit message

`feat(sticky-scroll): toggle sticky scroll`
