# WordHighlighter

**Role:** highlights every occurrence of the word under the cursor (or of the single word a selection is inside) 50 ms
after the cursor moves, and removes the highlights when the text changes, the cursor leaves the word or the editor loses
the focus. Adds `editor.action.wordHighlight.next` (F7), `editor.action.wordHighlight.prev` (Shift+F7) and
`editor.action.wordHighlight.trigger` (no key).

**Monaco sources:** `src/vs/editor/contrib/wordHighlighter/browser/` (`wordHighlighter.ts`, `textualHighlightProvider.ts`,
`highlightDecorations.ts`, `highlightDecorations.css`).

**Files:**
- `WordHighlighterFeature.cs` — the contribution: creates the highlighter, registers the commands.
- `WordHighlighter.cs` — the controller: timers, query, decorations, `MoveNext` / `MoveBack`.
- `TextualDocumentHighlightProvider.cs`, `DocumentHighlight.cs` — the textual occurrences.
- `HighlightDecorations.cs` — the decoration options by highlight kind (the CSS rows are in `DecorationStyles.cs`).
- `EditorWordHighlightOptions.cs` — `occurrencesHighlight`, `occurrencesHighlightDelay`.
- `WordHighlighterCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/WordHighlighter/`.

**Known limits:** textual provider only (semantic read / write highlights come with the language features of phase 8);
no multi-file highlighting, no screen reader alert, no overview ruler or minimap marks.
