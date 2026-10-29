# P10-26 — Completion items that are snippets

- **Status:** done
- **Depends on:** P10-25
- **Size:** M

## Goal

A completion item with `InsertTextRules.InsertAsSnippet` (and every LSP item with `insertTextFormat: 2`) is inserted as a snippet when accepted: tab stops, placeholders and variables work, at **every** cursor, with the item's replace range and additional edits. Plain items behave exactly as before.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/languages.ts` | 534–547 | `CompletionItemInsertTextRule` (`None`, `KeepWhitespace = 0b001`, `InsertAsSnippet = 0b100`) |
| `src/vs/editor/contrib/suggest/browser/suggestController.ts` | 308–345 | the start of `_insertSuggestion`: the snippet controller lookup, `getOverwriteInfo` |
| `src/vs/editor/contrib/suggest/browser/suggestController.ts` | 345–470 | the rest of `_insertSuggestion`: `adjustWhitespace: !(insertTextRules & KeepWhitespace)`, `overwriteBefore` / `overwriteAfter`, the snippet branch (`SnippetController2.insert(insertText, options)`) versus the plain edit branch (437), additional text edits applied before / after |
| `src/vs/editor/contrib/suggest/browser/suggestController.ts` | 573–590 | `getOverwriteInfo` (overwriteBefore / After from the item's edit range and the position) |

## Files to create or change

- `src/LoomEditor.Core/Languages/Features/CompletionItem.cs` — change: `InsertTextRules InsertTextRules { get; set; }` (new `[Flags] public enum InsertTextRules { None = 0, KeepWhitespace = 1, InsertAsSnippet = 4 }` in `InsertTextRules.cs`, with XML docs; public API: `./eng/Update-PublicApi.ps1 src/LoomEditor.Core`); update the XML doc that says "snippets are not supported" and the `<remarks>` "Not ported: `insertTextRules`"
- `src/LoomEditor.View/Suggest/SuggestController.Accept.cs` — change: the snippet branch (below)
- `src/LoomEditor.Lsp/Editor/LspConvert.Completion.cs` — change: `InsertTextFormat == Snippet` sets `InsertTextRules.InsertAsSnippet` and keeps the **raw** text; delete the call to `LspSnippet.ToPlainText`; delete `LspSnippet.cs` and its tests **only if** nothing else uses it (`grep -rn "LspSnippet" src tests`); otherwise leave it and say why
- `tests/LoomEditor.View.Tests/Suggest/…` (the existing accept tests: `grep -rln "SuggestController" tests/LoomEditor.View.Tests`) and `tests/LoomEditor.Lsp.Tests/…` (the existing completion conversion tests) — change / create
- `tests/LoomEditor.Lsp.FakeServer/` — change: one completion item with `insertTextFormat: 2` and a body such as `for (${1:i} = 0; $1 < ${2:n}; $1++) {\n\t$0\n}`; `tests/LoomEditor.View.Tests/Lsp/…` end-to-end completion test — add a case that accepts it and checks the document and the active placeholder

## Design decisions (already made — do not change)

- **Snippet branch of accept:** when `InsertTextRules.InsertAsSnippet` is set, the controller (P10-25) inserts `item.InsertText` with `overwriteBefore` / `overwriteAfter` computed like `getOverwriteInfo` (the range of the item; the `replace` vs `insert` choice that the accept code already makes), `adjustWhitespace = !KeepWhitespace`, `undoStopBefore: false`, `undoStopAfter: false` (the accept code pushes its own stops as Monaco's does at lines 345–470: read them), and the item's **additional text edits** are applied **before** the snippet exactly as for plain items (Monaco applies them in `_insertSuggestion` regardless of the kind). **Every cursor** gets the snippet (the plain-text accept edits only the primary one: that remains for plain items and is stated in the file header; snippets use the snippet session which handles all the cursors).
- A snippet item that Monaco treats specially while the widget is open (`lines 195`, `623`: it is not "commit on typing a commit character" when it has additional edits) is **not** ported: out of scope.
- `CompletionItem.InsertText` of a snippet item is the template, so `FilterText` / the label are unaffected.
- The `LspSnippet.ToPlainText` fallback goes away: LSP clients now declare `snippetSupport: true` (check `LspClient` capabilities: `grep -rn "snippetSupport" src/LoomEditor.Lsp`; it must be `true` after this card, and the initialize test that asserts the capabilities is updated).
- Items of `CompletionItemKind.Snippet` without the rule stay plain text.

## Tests

- [x] Port: none (the Monaco suggest tests do not run without the widget)
- [x] New (View, no server): `Accept_SnippetItem_InsertsTemplateText_AndEntersSnippetMode`, `Accept_SnippetItem_MultiCursor_AllGetSnippet`, `Accept_SnippetItem_ReplacesTypedPrefix` (overwriteBefore), `Accept_SnippetItem_KeepWhitespace_DoesNotReindent`, `Accept_SnippetItem_AdditionalTextEdits_Applied`, `Accept_PlainItem_Unchanged`, `Accept_SnippetItem_OneUndoStep`
- [x] New (Lsp): `ToCompletionItem_SnippetFormat_SetsInsertAsSnippetAndKeepsRawText`, `Initialize_DeclaresSnippetSupport`, end-to-end (`tests/LoomEditor.View.Tests/Lsp`): `Completion_SnippetItem_EndToEnd`
- [x] Manual (section "P10-26", with the Gallery language server demo of phase 8): accept the snippet item of the fake server, Tab through its placeholders.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `suggestController.ts` row (snippet branch), `languages.ts` (`CompletionItemInsertTextRule`); ADR 0011's "Not delivered: snippets" line gets a "(delivered in phase 10)" note
- [x] Public API file updated; card status and `docs/tasks/README.md` updated

## Out of scope

Choice lists, snippet text edits from LSP (`textEdit` with `insertTextFormat 2` **is** covered: the item's text edit becomes the range and the text the template), snippet completion from a snippet library (application concern), `snippetSuggestions` ordering changes.

## Suggested commit message

`feat(suggest): insert completion items as snippets`
