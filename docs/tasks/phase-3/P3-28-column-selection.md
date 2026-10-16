# P3-28 — Column (box) selection

- **Status:** done
- **Depends on:** P3-27
- **Size:** S

## Goal

Select a rectangle of text: one cursor per line, all between the same two visible columns (so tabs and full-width
characters line up). Port `ColumnSelection` and the commands `columnSelect` (mouse: Alt+Shift+drag, middle button)
and `cursorColumnSelectLeft/Right/Up/Down/PageUp/PageDown` (keyboard: Ctrl+Shift+Alt+arrows).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorColumnSelection.ts` | 10–126 | `class ColumnSelection` (`columnSelect`, `columnSelectLeft`, `columnSelectRight`, `columnSelectUp`, `columnSelectDown`), `IColumnSelectResult` |
| `src/vs/editor/browser/coreCommands.ts` | 419–588 | `ColumnSelectCommand` (base), `ColumnSelect` (`columnSelect`), `CursorColumnSelectLeft` … `CursorColumnSelectPageDown`, `ColumnSelectUpCommand`, `ColumnSelectDownCommand` |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | 908, 978, 1007, 1065, 1110, 1125 | `column select 1`, `issue #4905 - column select is biased to the right`, `issue #20087: column select with mouse`, `issue #20087: column select with keyboard`, `issue #118062: Column selection cannot select first position of a line`, `column select with keyboard` |

## Files to create or change

- `src/LoomEditor.View/Cursors/ColumnSelection.cs` — create (with `internal sealed record ColumnSelectResult`)
- `src/LoomEditor.View/Commands/CoreNavigationCommands.ColumnSelect.cs` — create (partial)
- `src/LoomEditor.View/Commands/CoreCommandRegistration.cs` — change: register the six `cursorColumnSelect…` ids
- `tests/LoomEditor.View.Tests/Cursors/ColumnSelectionTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `IColumnSelectResult` → `internal sealed record ColumnSelectResult(IReadOnlyList<SingleCursorState> ViewStates, bool ReversedSelection, int FromLineNumber, int FromVisualColumn, int ToLineNumber, int ToVisualColumn)` (check the field names at lines 119–126).
- Visible columns go through `CursorConfiguration.VisibleColumnFromColumn` / `ColumnFromVisibleColumn` (P3-13), so
  tabs and full-width characters are handled like Monaco.
- `ColumnSelectCommandArgs(Position Position, Position ViewPosition, int MouseColumn, bool DoColumnSelect, string? Source = null)`
  for `columnSelect` (lines 446–452: `ColumnSelectCommandOptions`). The mouse passes it in P3-44.
- The commands store the result with `ViewModel.SetCursorColumnSelectData(new ColumnSelectData(IsReal: true, …))`,
  as in the base class `ColumnSelectCommand`.

## Tests

- [x] Port the 6 tests listed above (with the `CursorTestHelpers` of P3-27 and `TestCodeEditor`)
- [x] New: `ColumnSelect` over `["\tab", "    cd"]` (tab size 4) from visible column 4 to 6 selects `"ab"` on line 1
      and `"cd"` on line 2

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `cursorColumnSelection.ts` (✅, P3-28); tests table `cursor.test.ts` updated (+6)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The mouse gesture (P3-44, P3-46), the keybindings (P3-39), column selection paste (P3-35).

## Suggested commit message

`feat(view): port column selection`
