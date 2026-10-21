# Find

**Role:** find and replace in the document: the find widget, matches, replace, and "find in selection". For now (P5-65) only the
shared state exists; the find model, the controller, the widget and the commands come with the next cards.

**Commands:** the ids are in `FindCommands.cs` (same as Monaco). Not registered in phase 5: `editor.actions.findWithArgs` (commands
have no arguments) and `editor.action.goToMatchFindAction` (it needs a quick input UI).

**Monaco sources:** `src/vs/editor/contrib/find/browser/` (`findState.ts`, `findModel.ts`, `findController.ts`),
`common/config/editorOptions.ts` (`EditorFind`).

**Files:**
- `FindFeature.cs` — the feature (does nothing yet); `FindCommands.cs` — command ids; `FindConstants.cs` — `MatchesLimit` (19999), `ResearchDelay` (240 ms).
- `FindReplaceState.cs`, `NewFindReplaceState.cs`, `FindReplaceStateChangedEventArgs.cs`, `FindOptionOverride.cs` — the state every find part shares and its change event.
- `EditorFindOptions.cs` — the `editor.find` options with VS Code's defaults (no public option in phase 5).
- `ReplacePattern.cs`, `ReplacePattern.Parse.cs` — the parsed replace string (`$1`, `\n`, `\u`…); `FindDecorations.cs` — the model decorations of matches, current match, its line and the scope.
- `FindModelBoundToEditorModel.cs`, `FindModelBoundToEditorModel.Navigation.cs` — searches when the state changes, decorates matches, keeps "n of m", moves to the next / previous match, selects all matches (replace comes with P5-69).
- `SelectionSearchString.cs` — what Ctrl+F puts in the find box (`getSelectionSearchString`).

**Tests:** `tests/LoomEditor.View.Tests/Features/Find/`.

**Known limits:** no search history, no `globalFindClipboard`, no notebook filters.
