# Suggest

**Role:** completion from the keyboard. Typing opens the list (P8-17), `Down` / `Up` / `PageDown` / `PageUp` move in it, `Tab` or
`Enter` accept, `Esc` closes it, `Ctrl+Space` (or `Ctrl+I`) opens it on demand. The feature keeps the context flags
`SuggestWidgetVisible` and `SuggestWidgetMultipleSuggestions` of the editor up to date for the keybinding conditions.

**Monaco sources:** `src/vs/editor/contrib/suggest/browser/suggestController.ts` (the commands and their keys), `suggest.ts` (the context keys).

**Files:**
- `SuggestFeature.cs` — creates the `SuggestController`, registers the nine commands, updates the context flags.
- `SuggestCommands.cs` — command ids.
- The logic is in `src/LoomEditor.View/Suggest/`.

**Tests:** `tests/LoomEditor.View.Tests/Features/Suggest/`.

**Known limits:** plain-text insertion only (no snippets), one cursor, no details pane; `acceptSuggestionOnEnter` is always `on`;
the list itself is the control's `ISuggestPresenter` (P8-22).
