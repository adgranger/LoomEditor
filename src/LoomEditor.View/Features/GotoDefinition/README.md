# GotoDefinition

**Role:** `F12` on a symbol, or `Ctrl+click` on it, asks the definition providers and moves the cursor to the first
definition in the same document; a definition in another document goes to the application's `ILocationOpener`;
"no definition" is announced.

**Monaco sources:** `src/vs/editor/contrib/gotoSymbol/browser/` (`goToSymbol.ts`, `referencesModel.ts`, `goToCommands.ts`,
`link/goToDefinitionAtPosition.ts`).

**Files:**
- `GotoDefinitionFeature.cs` — creates the controller, registers the command, listens to `Ctrl+click`.
- `GotoDefinitionCommands.cs` — command id (`editor.action.revealDefinition`).
- `GotoDefinitionController.cs` — runs a request, drops it when the text or the cursor changed, opens the result.
- `DefinitionProviders.cs`, `DefinitionResults.cs` — asking the providers; sorting and de-duplicating the links.
- `ILocationOpener.cs` — the public hook for definitions in other documents.

**Tests:** `tests/LoomEditor.View.Tests/Features/GotoDefinition/`.

**Known limits:** no peek view (several results go to the first), no side-by-side (`Ctrl+K F12`), no `Ctrl+hover` link
underline, no highlight flash, one cursor, `Ctrl+click` does not check the click count.
