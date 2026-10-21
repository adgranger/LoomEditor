# P5-53 — Indentation feature: convert, change size, detect, reindent

- **Status:** done
- **Depends on:** P5-52
- **Size:** S

## Goal

The `Indentation` feature registers Monaco's eight indentation actions: convert indentation to spaces / tabs,
indent using tabs / spaces, change the tab display size, detect indentation from the content, reindent all lines,
reindent the selected lines. VS Code opens a quick pick for the size; LoomEditor has no UI for it (see below).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/indentation/browser/indentation.ts` | 31–65 | `IndentationToSpacesAction` |
| same | 67–101 | `IndentationToTabsAction` |
| same | 103–159 | `ChangeIndentationSizeAction.run` (the size choice and `model.updateOptions`, not the quick pick) |
| same | 161–207 | `IndentUsingTabs`, `IndentUsingSpaces`, `ChangeTabDisplaySize` (ids, `insertSpaces`, `displaySizeOnly`) |
| same | 209–235 | `DetectIndentation` |
| same | 237–264 | `ReindentLinesAction` |
| same | 266–320 | `ReindentSelectedLinesAction` |

## Files to create or change

- `src/LoomEditor.View/Features/Indentation/IndentationActions.cs` — create: one `internal static` method per action, taking the `ViewModel`
- `src/LoomEditor.View/Features/Indentation/IndentationFeature.cs` — change: register the eight commands
- `src/LoomEditor.View/Features/Indentation/README.md` — change
- `tests/LoomEditor.View.Tests/Features/Indentation/IndentationActionsTests.cs` — create

## Design decisions (already made — do not change)

- **No quick pick.** `ChangeIndentationSize(ViewModel viewModel, int tabSize, bool insertSpaces, bool displaySizeOnly)`
  takes the size as an argument (what the quick pick returns): `displaySizeOnly` → `UpdateOptions(new TextModelUpdateOptions(TabSize: n))`,
  otherwise `TabSize`, `IndentSize` and `InsertSpaces`. The registered commands
  `editor.action.indentUsingTabs`, `editor.action.indentUsingSpaces`, `editor.action.changeTabDisplaySize` have no
  argument (`CommandRegistry` has none in phase 5): they apply the item the quick pick pre-selects, i.e.
  `Math.min(tabSize - 1, 7) + 1` (line 135, `autoFocusIndex`), as if the user pressed Enter. Write this in the XML doc,
  the README and `docs/PORTING.md` (behavior difference). The 50 ms `setTimeout` is not ported.
- `DetectIndentation` uses Monaco's creation options: `TextModelOptions.Default.InsertSpaces` and
  `TextModelOptions.Default.TabSize` (`getCreationOptions` returns the configured defaults, which are those).
  `model.detectIndentation` → `TextModel.DetectIndentation(bool defaultInsertSpaces, int defaultTabSize)` (internal, P5-19).
  The `detectIndentation` **editor option** (apply it when the text is set) is not this card: P5-19 leaves it to a
  later card; this card only adds the command.
- Reindent actions call `ReindentOperations.GetReindentEditOperations` (P5-52) and run
  `viewModel.ExecuteEdits(id, edits, _ => null)` between two `PushStackElement` calls, only when there are edits.
  `ReindentSelectedLinesAction` keeps Monaco's line adjustments (`endColumn === 1`, `startLineNumber--`).
- `precondition: writable` (`indentationToSpaces`, `indentationToTabs`, both reindents) → `canExecute: () => !viewModel.CursorConfig.ReadOnly`.
  The four others have `precondition: undefined`: no `canExecute`.
- No keybinding: none of these actions has `kbOpts`.
- The language configuration registry is the model's (as in P5-51).

## Tests

- [x] New: `IndentationToSpaces_TabIndentedDocument_ConvertsAndSetsInsertSpaces` (and the Tabs mirror)
- [x] New: `ChangeIndentationSize_DisplaySizeOnly_ChangesOnlyTheTabSize`
- [x] New: `IndentUsingSpacesCommand_TabSize12_AppliesEight` (the pre-selected quick pick item is capped to 8)
- [x] New: `DetectIndentation_TwoSpaceDocument_SetsTabSizeTwoAndSpaces`
- [x] New: `ReindentLines_JavaScriptRules_FixesTheWholeDocumentInOneUndoStep`
- [x] New: `ReindentSelectedLines_SelectionEndingAtColumnOne_IgnoresTheLastLine`
- [x] New: `ReindentLines_ReadOnly_DoesNotRun`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `contrib/indentation/browser/indentation.ts` (🔶 actions + commands, no quick pick,
      P5-53); behavior difference "size actions without a quick pick"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`AutoIndentOnPaste` (P5-54), a status bar or a picker UI.

## Suggested commit message

`feat(view): add the indentation actions`
