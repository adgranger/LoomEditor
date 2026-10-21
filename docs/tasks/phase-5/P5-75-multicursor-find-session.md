# P5-75 — Find-based multi-cursor: `MultiCursorSession` and Ctrl+D

- **Status:** done
- **Depends on:** P5-74
- **Size:** M

## Goal

Ctrl+D adds the next occurrence of the selection (or of the word under the cursor) as a new cursor, Ctrl+K Ctrl+D moves
the last selection to the next occurrence, Ctrl+Shift+L and Ctrl+F2 select all occurrences, with Monaco's session rules
(find widget search vs selection search, `selectedTextMatchMode`, temporary find option overrides). The first 16 deferred
tests of `multicursor.test.ts` pass.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/multicursor/browser/multicursor.ts` | 267–291 | `MultiCursorSessionResult`, `isFindWidgetSearch`, `getSelectionSearchOptions` |
| same | 293–476 | `MultiCursorSession` |
| same | 478–698 | `MultiCursorSelectionController` |
| same | 700–844 | `MultiCursorSelectionControllerAction` and the six actions (ids, `kbOpts`, `precondition`; menus not ported) |
| same | 1100–1118 | `modelRangesContainSameText`, `getValueInRange` |
| `common/config/editorOptions.ts` | 6726–6738 | `selectedTextMatchMode` (default `findOptions`) |
| `common/viewModel/viewModelImpl.ts` | 246–251 | `setHasFocus` (the outgoing `FocusChangedEvent`) |
| `common/viewModelEventDispatcher.ts` | 247–269 | `FocusChangedEvent` |
| `contrib/multicursor/test/browser/multicursor.test.ts` | 84–298 | `fromRange`, `testMulticursor`, `testAddSelectionToNextFindMatchAction`, `setTextFocus` |

## Files to create or change

- `src/LoomEditor.View/Features/MultiCursor/MultiCursorSession.cs`, `MultiCursorSelectionController.cs` — create
- `src/LoomEditor.View/Features/MultiCursor/MultiCursorOptions.cs` — create (`SelectedTextMatchMode`; P5-77 adds the
  `selectionHighlight*` options)
- `src/LoomEditor.View/Features/MultiCursor/MultiCursorFeature.cs`, `MultiCursorCommands.cs`, `README.md` — change: the six ids,
  create the controller in `Attach` when `context.GetFindController()` is not `null`
- `src/LoomEditor.View/Events/OutgoingViewModelEvent.cs` — change: `FocusChangedEvent` (+ `OutgoingViewModelEventKind.FocusChanged`)
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `HasFocus` getter; `SetHasFocus` emits `FocusChangedEvent(!hasFocus, hasFocus)` like Monaco
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: the rules below
- `src/LoomEditor.WinUI/Controls/Find/FindWidget.Keyboard.cs` — change: add the four keyed ids to `RoutedCommandIds`
  (Ctrl+D etc. work from the find box in VS Code: their `kbExpr` is `editorFocus`)
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — via the script (new constants of `MultiCursorCommands`)
- `tests/LoomEditor.View.Tests/Features/MultiCursor/MultiCursorSelectionTests.cs` — create
- `tests/LoomEditor.View.Tests/Features/MultiCursor/TestHelpers/MultiCursorTestHelpers.cs` — create (`TestMulticursor`,
  `SetTextFocus`, `FromRange`; a `TestFindWidget` (P5-70) is attached to the controller so `HighlightFindOptionsCount`
  replaces Monaco's `sinon.spy`)

## Design decisions (already made — do not change)

- `new MultiCursorSelectionController(ViewModel viewModel, MultiCursorOptions options)`; actions are methods taking the
  `FindController` (Monaco passes it). `MultiCursorSelectionControllerAction.run` without a find controller creates a
  temporary `FindController` and disposes it after the action, like Monaco.
- Editor events: `onDidChangeCursorSelection` → `CursorStateChangedEvent`; `onDidBlurEditorText` / `onDidFocusEditorText` →
  the new `FocusChangedEvent` (`HasFocus` false / true); `onDidChangeConfiguration` with `selectedTextMatchMode` →
  `MultiCursorSelectionController.SetOptions(options)` raising `OptionsChanged` (old, new) when the value changes;
  `editor.hasTextFocus()` → `viewModel.HasFocus`. `onDidChangeModel` is not ported (one model per editor).
- `editor.getConfiguredWordAtPosition` → `WordOperations.GetWordAtPosition(model, config.WordSeparators, config.WordSegmenterLocales, position)`
  (the word separators option, not the language word pattern of P5-15 — Monaco uses the option here).
- `findController.getState().change(…)` with overrides uses `NewFindReplaceState { WholeWordOverride = FindOptionOverride.True, … }` (P5-65).
- `revealRangeInCenterIfOutsideViewport` as in P5-68. `announceCursorChange` is not ported (no screen reader announcement).
- `getValueInRange(...).replace(/\r\n/g, '\n')` → `GetValueInRange(range, EndOfLinePreference.LF)`; `toLowerCase` → `ToLowerInvariant`.
- Commands (`MultiCursorCommands`, Monaco ids): `AddSelectionToNextFindMatch`, `AddSelectionToPreviousFindMatch`,
  `MoveSelectionToNextFindMatch`, `MoveSelectionToPreviousFindMatch`, `SelectHighlights`, `ChangeAll` (`canExecute`:
  writable for `changeAll`, Monaco's precondition).
- Keybindings (EditorContrib, `when` = `WhenClauses.EditorFocus` of P5-71 unless said): Ctrl+D → `addSelectionToNextFindMatch`;
  Ctrl+K Ctrl+D → `moveSelectionToNextFindMatch` with the chord support of **P5-47** (do not add another chord
  mechanism); Ctrl+Shift+L → `selectHighlights`; Ctrl+F2 → `changeAll` (`when` = `editorTextFocus` and writable).

## Tests

`multicursor.test.ts`, suite `Multicursor selection` (and its nested suite `Find state disassociation`):

- [x] Port (11): `issue #8817: Cursor position changes when you cancel multicursor`,
      `issue #5400: "Select All Occurrences of Find Match" does not select all if find uses regex`,
      `AddSelectionToNextFindMatchAction can work with multiline`,
      `issue #6661: AddSelectionToNextFindMatchAction can work with touching ranges`,
      `issue #23541: Multiline Ctrl+D does not work in CRLF files`,
      `AddSelectionToNextFindMatchAction starting with single collapsed selection`,
      `AddSelectionToNextFindMatchAction starting with two selections, one being collapsed 1)`,
      `AddSelectionToNextFindMatchAction starting with two selections, one being collapsed 2)`,
      `AddSelectionToNextFindMatchAction starting with all collapsed selections`,
      `AddSelectionToNextFindMatchAction starting with all collapsed selections on different words`,
      `issue #20651: AddSelectionToNextFindMatchAction case insensitive`
- [x] Port suite `Find state disassociation` (5): `caret-started sessions temporarily override and restore Find options in every matching mode`,
      `changing Find options ends a default caret-started session without reverting user preferences`, `enters mode`,
      `leaves mode when selection changes`, `Select Highlights respects mode ` (trailing space is in Monaco's name)
- [x] New: `SetHasFocus_EmitsFocusChangedEvent`, `Attach_WithoutFindFeature_RegistersOnlyThePhase3Commands`,
      `CtrlD_Keybinding_ResolvesToAddSelectionToNextFindMatch`, `CtrlK_CtrlD_Chord_ResolvesToMoveSelectionToNextFindMatch`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` of `LoomEditor.View`: only the new `MultiCursorCommands` constants
- [x] `docs/PORTING.md`: map rows `multicursor.ts` (+ Find-based commands, P5-75), `viewModelEventDispatcher.ts` (+ `FocusChangedEvent`);
      tests table `multicursor.test.ts` 18 / 76
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The `Selected text occurrence matching` tests (P5-76), `SelectionHighlighter` (P5-77).

## Suggested commit message

`feat(view): add Find-based multi-cursor (Ctrl+D, select all occurrences)`
