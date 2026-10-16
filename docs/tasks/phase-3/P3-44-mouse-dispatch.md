# P3-44 — Mouse clicks to cursor commands (`ViewController.dispatchMouse`)

- **Status:** done
- **Depends on:** P3-43
- **Size:** M

## Goal

Port the decision table that turns a click into a cursor command: click → move; Shift+click → extend; Alt+click → add
or remove a cursor; double-click → select the word; triple-click → select the line; four clicks → select all;
clicking the line numbers → select lines; Alt+Shift+drag or middle button → column selection. Port the mouse core
commands it uses (`_moveTo`, `_createCursor`, `_wordSelect`, `_lineSelect`…).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/view/viewController.ts` | 21–40, 101–133 | `_validateViewColumn`, `_hasMulticursorModifier`, `_hasNonMulticursorModifier` |
| same | 140–215 | `_trySelectBracketContent`, `_trySelectStringContent` — **cut points** (need brackets and tokens): return `null` |
| same | 217–311 | `dispatchMouse` (the whole decision table) |
| same | 313–396 | `_usualArgs`, `moveTo`, `_moveToSelect`, `_columnSelect`, `_createCursor`, `_lastCursorMoveToSelect`, `_wordSelect`, `_wordSelectDrag`, `_lastCursorWordSelect`, `_lineSelect`, `_lineSelectDrag`, `_lastCursorLineSelect`, `_lastCursorLineSelectDrag`, `_select`, `_selectAll`, `_convertViewToModelPosition` |
| `src/vs/editor/browser/editorBrowser.ts` | search `interface IMouseDispatchData` | the dispatch data fields |
| `src/vs/editor/browser/coreCommands.ts` | 896–983 | `CreateCursor` (`_createCursor`, toggles a cursor), `LastCursorMoveToSelect` |
| same | 1631–1773 | `WordCommand` (`_wordSelect`, `_wordSelectDrag`), `LastCursorWordSelect`, `LineCommand` (`_lineSelect`, `_lineSelectDrag`), `LastCursorLineCommand` (`_lastCursorLineSelect`, `_lastCursorLineSelectDrag`) |
| `src/vs/editor/common/config/editorOptions.ts` | 3629, 6511–6514 | `multiCursorModifier` (default `alt`), `doubleClickSelectsBlock` (default `true`) |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | 2462, 2478, 2491, 6369, 6396, 6416 | `issue #33788`, `issue #12887`, `Double-click on punctuation should select the character, not adjacent space`, `issue #7100`, `issue #112039`, `issue #158236` |

## Files to create or change

- `src/LoomEditor.View/Controller/ViewController.cs` — create: `DispatchMouse(MouseDispatchData data)` and the helpers
- `src/LoomEditor.View/Controller/MouseDispatchData.cs` — create
- `src/LoomEditor.View/Commands/CoreNavigationCommands.Mouse.cs` — create (partial): the mouse commands
- `src/LoomEditor.View/Cursors/EditorCursorOptions.cs` — change: `MultiCursorModifier` (`Alt`), `DoubleClickSelectsBlock` (`true`)
- `tests/LoomEditor.View.Tests/Controller/ViewControllerTests.cs`, `tests/LoomEditor.View.Tests/Commands/MouseCommandTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `MouseDispatchData` (Monaco: `IMouseDispatchData`, `viewController.ts` lines 21–40) =
  `internal sealed record MouseDispatchData(Position Position, int MouseColumn, NavigationCommandRevealType RevealType, bool StartedOnLineNumbers, bool InSelectionMode, int MouseDownCount, bool AltKey, bool CtrlKey, bool MetaKey, bool ShiftKey, bool LeftButton, bool MiddleButton, bool OnInjectedText)`.
  `NavigationCommandRevealType` is Monaco's enum (`coreCommands.ts`, search `NavigationCommandRevealType`: `Regular`, `Minimal`, `None`).
- `ViewController(ViewModel viewModel)`. Monaco's `this.commandService.executeCommand(id, args)` for the core
  commands becomes a direct call `CoreNavigationCommands.X.RunCoreEditorCommand(viewModel, args)`.
- `multiCursorModifier`: `Alt` (default) means Alt is the multi-cursor modifier; `CtrlCmd` means Ctrl. Keep Monaco's
  helper logic (`_hasMulticursorModifier` / `_hasNonMulticursorModifier`) with this option.
- `doubleClickSelectsBlock` is `true` by default, but `_trySelectBracketContent` / `_trySelectStringContent` always
  return `null` in phase 3 (cut points: bracket pairs and tokens), so a double-click selects the word.
- `onInjectedText` is always `false`. The Linux selection clipboard (`middleButton && !…`) is Linux only: write the
  condition with `isLinux = false`.
- `_createCursor` toggles: clicking inside an existing secondary cursor's selection removes it (coreCommands 919–944) — port.
- The mouse core commands are not registered in the `CommandRegistry` (they need a position argument; Monaco registers
  them but no key is bound).

## Tests

- [x] Port the 6 `cursor.test.ts` tests listed above (they call `CoreNavigationCommands.WordSelect.RunCoreEditorCommand(viewModel, …)`,
      `MoveToSelect`, `LineSelectDrag`… directly)
- [x] New (`ViewControllerTests`): `DispatchMouse` with `MouseDownCount = 2` → word selected; `3` → line selected; `4` → all;
      `AltKey` → a second cursor; `AltKey` on an existing secondary cursor → removed; `ShiftKey` (`InSelectionMode`) → extended;
      `AltKey + ShiftKey` + `InSelectionMode` → column selection; `MiddleButton` → column selection;
      `StartedOnLineNumbers` → line selection

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows `editor/browser/view/viewController.ts` (🔶 `dispatchMouse`, P3-44), `coreCommands.ts`
      (+ mouse commands); behavior difference "double-click never selects bracket or string content in phase 3";
      tests table `cursor.test.ts` (+6)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Click counting, drags and drag scrolling (P3-45), pointer events (P3-46), drag-and-drop of selected text (ADR 0006,
decision 12).

## Suggested commit message

`feat(view): port the mouse click decision table and mouse commands`
