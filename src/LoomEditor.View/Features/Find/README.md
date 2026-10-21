# Find

**Role:** find and replace in the document: the find widget, matches, replace, and "find in selection". The shared state, the find model, the controller and the commands exist (P5-71); the widget comes with the next cards.
shared state exists; the find model, the controller, the widget and the commands come with the next cards.

**Commands:** the ids are in `FindCommands.cs` (same as Monaco). Not registered in phase 5: `editor.actions.findWithArgs` (commands
have no arguments) and `editor.action.goToMatchFindAction` (it needs a quick input UI).

**Monaco sources:** `src/vs/editor/contrib/find/browser/` (`findState.ts`, `findModel.ts`, `findController.ts`),
`common/config/editorOptions.ts` (`EditorFind`).

**Files:**
- `FindFeature.cs` — the feature (creates the `FindController`, registers the 16 commands); `FindCommands.cs` — command ids; `FindConstants.cs` — `MatchesLimit` (19999), `ResearchDelay` (240 ms).
- `FindReplaceState.cs`, `NewFindReplaceState.cs`, `FindReplaceStateChangedEventArgs.cs`, `FindOptionOverride.cs` — the state every find part shares and its change event.
- `EditorFindOptions.cs` — the `editor.find` options with VS Code's defaults (no public option in phase 5).
- `ReplacePattern.cs`, `ReplacePattern.Parse.cs` — the parsed replace string (`$1`, `\n`, `\u`…); `FindDecorations.cs` — the model decorations of matches, current match, its line and the scope.
- `FindModelBoundToEditorModel.cs`, `FindModelBoundToEditorModel.Navigation.cs` — searches when the state changes, decorates matches, keeps "n of m", moves to the next / previous match, selects all matches; `FindModelBoundToEditorModel.Replace.cs` and `ReplaceAllCommand.cs` — replace and replace all.
- `FindController.cs`, `FindActions.cs`, `FindStartOptions.cs`, `IFindWidget.cs` — the controller (state + find model while the widget is revealed), the find actions as static methods (Ctrl+F, Ctrl+H, F3…) and the small interface a widget implements.
- `SelectionSearchString.cs` — what Ctrl+F puts in the find box (`getSelectionSearchString`).

**Tests:** `tests/LoomEditor.View.Tests/Features/Find/`.

**Known limits:** no search history, no `globalFindClipboard`, no notebook filters.

**Keys (Windows, `DefaultKeybindings.cs`):** Ctrl+F find, Ctrl+H replace, F3 / Shift+F3 next / previous match (Enter / Shift+Enter in the find box),
Ctrl+F3 / Ctrl+Shift+F3 next / previous match of the selection, Esc close, Alt+C / W / R / L / P toggle case / whole word / regex / in selection /
preserve case, Ctrl+Shift+1 or Enter in the replace box replace one, Ctrl+Alt+Enter replace all, Alt+Enter select all matches.
