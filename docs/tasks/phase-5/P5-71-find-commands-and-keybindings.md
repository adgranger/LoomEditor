# P5-71 — Find commands, `when` clauses and VS Code's keybindings

- **Status:** done
- **Depends on:** P5-70
- **Size:** M

## Goal

`FindFeature` creates the editor's `FindController`, registers the 16 find commands, and the default keybindings give
them VS Code's Windows shortcuts (Ctrl+F, Ctrl+H, F3, Shift+F3, Ctrl+F3, Esc, Enter in the find box, Alt+C/W/R/L/P,
Ctrl+Shift+1, Ctrl+Alt+Enter, Alt+Enter). Without the widget (P5-72), Ctrl+F already reveals the state and F3 already
moves to the next match of the word under the cursor.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/find/browser/findModel.ts` | 28–58 | context keys `findWidgetVisible`, `findInputFocussed`, `replaceInputFocussed`; `Toggle*Keybinding` |
| `contrib/find/browser/findController.ts` | 566–578, 777–816, 964–1002, 1004–1015 | `kbOpts` and `precondition` of `StartFindAction`, `NextMatchFindAction`, `PreviousMatchFindAction`, `NextSelectionMatchFindAction`, `PreviousSelectionMatchFindAction`, `StartFindReplaceAction` |
| same | 1061–1209 | the `FindCommand` registrations (`closeFindWidget`, the toggles, `replaceOne` ×2, `replaceAll` ×2, `selectAllMatches`) |
| `common/editorContextKeys.ts` | 20 | `focus` (`editorFocus`): the `when` of most find keybindings |

## Files to create or change

- `src/LoomEditor.View/Features/Find/FindFeature.cs` — change: create the controller in `Attach`, register the commands,
  dispose everything in `Dispose`
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `internal FindController? FindController { get; set; }`
- `src/LoomEditor.View/Features/EditorContextExtensions.cs` — change: `GetFindController(this IEditorContext)`
- `src/LoomEditor.View/Commands/KeybindingContext.cs` — change: add `EditorFocus`, `FindWidgetVisible`, `FindInputFocused`,
  `ReplaceInputFocused` (all `= false`, docs with Monaco's key names)
- `src/LoomEditor.View/Commands/WhenClauses.cs` — change: `EditorFocus`, `FindWidgetVisible`, `FindInputFocused`, `ReplaceInputFocused` (below)
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: the find rules (below), in a `// findController.ts (Contrib)` block
- `src/LoomEditor.View/Commands/KeyCode.cs` — change: `Digit0` … `Digit9`
- `src/LoomEditor.WinUI/Input/KeyMapping.cs` — change: `VirtualKey.Number0 … Number9` → `KeyCode.Digit0 + n`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Keyboard.cs` — change: `EditorFocus: HasFocus`, `FindWidgetVisible: _context.FindController?.State.IsRevealed ?? false`
  (the widget adds its own focus in P5-73)
- `src/LoomEditor.View/Features/Find/README.md` — update (commands and keys table)
- `tests/LoomEditor.View.Tests/Features/Find/FindFeatureTests.cs`, `tests/LoomEditor.View.Tests/Commands/FindKeybindingsTests.cs` — create

## Design decisions (already made — do not change)

- `FindFeature.Attach`: `ViewModel? vm = context.GetViewModel()`; `null` → return (as `MultiCursorFeature`). Create
  `new FindController(vm, context.Scheduler)` (the `IEditorScheduler` of step A) and set `EditorContext.FindController`.
  `Dispose`: unregister, clear `EditorContext.FindController`, dispose the controller. Other features find the controller
  with `context.GetFindController()` (Monaco `CommonFindController.get(editor)`).
- Commands: `EditorCommand(id, execute, canExecute)`. A Monaco `precondition` becomes `canExecute`
  (`CONTEXT_FIND_WIDGET_VISIBLE` → `State.IsRevealed`; `StartFindReplace` checks read-only inside, like Monaco). Ids and
  targets: `actions.find` → `FindActions.StartFind`; `actions.findWithSelection` → `StartFindWithSelection`;
  `editor.action.nextMatchFindAction` / `previousMatchFindAction` / `nextSelectionMatchFindAction` /
  `previousSelectionMatchFindAction` / `startFindReplaceAction` → the `FindActions` of the same name; `closeFindWidget`,
  `toggleFindCaseSensitive`, `toggleFindWholeWord`, `toggleFindRegex`, `toggleFindInSelection`, `togglePreserveCase`,
  `editor.action.replaceOne`, `editor.action.replaceAll`, `editor.action.selectAllMatches` → the controller method.
- `when` clauses (Monaco `precondition AND kbExpr`):
  `EditorFocus(c) = c.EditorFocus`; `FindWidgetVisible(c) = c.EditorFocus && c.FindWidgetVisible`;
  `FindInputFocused(c) = c.EditorFocus && c.FindInputFocused`;
  `ReplaceInputFocused(c) = c.EditorFocus && c.FindWidgetVisible && c.ReplaceInputFocused`.
- Rules (Windows; weight `EditorContrib` = 100 unless `+5`):

  | Key | Command | When | Weight |
  |---|---|---|---|
  | Ctrl+F | `actions.find` | `null` | 100 |
  | F3 / Enter | `editor.action.nextMatchFindAction` | `EditorFocus` / `FindInputFocused` | 100 |
  | Shift+F3 / Shift+Enter | `editor.action.previousMatchFindAction` | `EditorFocus` / `FindInputFocused` | 100 |
  | Ctrl+F3 | `editor.action.nextSelectionMatchFindAction` | `EditorFocus` | 100 |
  | Ctrl+Shift+F3 | `editor.action.previousSelectionMatchFindAction` | `EditorFocus` | 100 |
  | Ctrl+H | `editor.action.startFindReplaceAction` | `null` | 100 |
  | Esc, Shift+Esc | `closeFindWidget` | `FindWidgetVisible` | 105 |
  | Alt+C / Alt+W / Alt+R / Alt+L / Alt+P | `toggleFindCaseSensitive` / `toggleFindWholeWord` / `toggleFindRegex` / `toggleFindInSelection` / `togglePreserveCase` | `EditorFocus` | 105 |
  | Ctrl+Shift+1 | `editor.action.replaceOne` | `FindWidgetVisible` | 105 |
  | Enter | `editor.action.replaceOne` | `ReplaceInputFocused` | 105 |
  | Ctrl+Alt+Enter | `editor.action.replaceAll` | `FindWidgetVisible` | 105 |
  | Alt+Enter | `editor.action.selectAllMatches` | `FindWidgetVisible` | 105 |

  `actions.findWithSelection` has no Windows key (Monaco: `primary: 0`, Cmd+E on macOS only). Ctrl+Enter in the replace box
  is macOS only. `Ctrl+F` / `Ctrl+H` use `null`: the control only receives keys when it or its widget has the focus, which
  is Monaco's `editorFocus || editorIsOpen` for one editor.
- Esc with the widget visible: `closeFindWidget` (105) wins over `removeSecondaryCursors` (1) and `cancelSelection` (0),
  as in VS Code.

## Tests

No upstream tests (keybinding tables are not unit-tested in Monaco). New:

- [x] `Attach_RegistersTheFindCommands_DisposeRemovesThemAndTheController`
- [x] `StartFind_ThroughCommandRegistry_RevealsTheState` (`new EditorContext()`, `Commands.Execute("actions.find")`)
- [x] `CloseFindWidget_WidgetHidden_CannotExecute`
- [x] Resolver: `Escape_FindWidgetVisible_ClosesFindWidget`, `Escape_FindWidgetHidden_CancelsSelection`,
      `Enter_FindInputFocused_NextMatch`, `Enter_ReplaceInputFocused_ReplaceOne`, `F3_EditorFocus_NextMatch`,
      `CtrlShift1_FindWidgetVisible_ReplaceOne`, `AltC_EditorFocus_ToggleCaseSensitive`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows `findController.ts` (+ commands and keybindings, P5-71), `editorContextKeys.ts`
      (+ `editorFocus`, find keys); the Keybindings difference row still says "no chords"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The widget and its focus (P5-72 … P5-74), the Find-based multi-cursor keys (P5-75).

## Suggested commit message

`feat(view): register the find commands and their keybindings`
