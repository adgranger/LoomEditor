# Snippet

**Role:** inserts snippets (TextMate syntax, `LoomEditor.Core/Snippets`) and moves between their tab stops. `OneSnippet` tracks
one inserted snippet with one model decoration per placeholder (classes `snippet-placeholder`, `finish-snippet-placeholder`).

**Monaco source:** `src/vs/editor/contrib/snippet/browser/snippetSession.ts` (`OneSnippet`), `snippetSession.css`,
`snippetController2.ts` (later cards).

**Files:**
- `OneSnippet.cs`, `OneSnippet.Move.cs`, `OneSnippet.Merge.cs` — one snippet: decorations, moving, nested snippets.
- `SnippetSession*.cs`, `SnippetSessionInsertOptions.cs` — inserts a template at every cursor, merge, next/prev, selection queries.
- `SnippetFeature.cs`, `SnippetCommands.cs` — placeholders until the controller (P10-25).

**Tests:** `tests/LoomEditor.View.Tests/Features/Snippet/`.

**Known limits:** the decorations belong to the editor, not to a feature owner id; the 2 px minimum width of an empty placeholder
is not drawn; `Placeholder.Index` is an `int`, so the fractional indices of a merge live in a local table until the renumbering.
