# P10-24 — `SnippetSession`: insert a snippet at every cursor, merge, next and previous

- **Status:** todo
- **Depends on:** P10-23
- **Size:** M

## Goal

`SnippetSession` inserts a snippet template at each cursor (replacing selections, adjusting indentation and line endings, resolving variables), keeps one `OneSnippet` per cursor, and exposes `Next`, `Prev`, `Merge` (a nested snippet inside the active placeholder) and the state queries the controller needs. It is driven by tests only; the keys and the feature are P10-25.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/snippet/browser/snippetSession.ts` | 403–424 | `ISnippetSessionInsertOptions` (`overwriteBefore`, `overwriteAfter`, `adjustWhitespace`, `clipboardText`; `overtypingCapturer` is **not** ported), `_defaultOptions` |
| `src/vs/editor/contrib/snippet/browser/snippetSession.ts` | 425–504 | `SnippetSession.adjustWhitespace` (normalizes `\r\n`, re-indents continuation lines with the line's leading whitespace, converts tabs to spaces when `insertSpaces`), `adjustSelection` |
| `src/vs/editor/contrib/snippet/browser/snippetSession.ts` | 505–588 | `createEditsAndSnippetsFromSelections` (per selection: overwrite ranges, the `EditOperation`, the variable resolvers `Composite[Selection, Clipboard, Model, Comment, Time, Workspace, Random]`, the edit offsets, `ensureFinalTabstop`) |
| `src/vs/editor/contrib/snippet/browser/snippetSession.ts` | 689–811 | `dispose`, `insert` (apply the edits, `initialize` each `OneSnippet` from the resulting text change, select the first placeholder), `merge` (+ `_templateMerges`), `next`, `prev`, `_move`, `isAtFirstPlaceholder`, `isAtLastPlaceholder`, `hasPlaceholder`, `hasChoice`, `activeChoice` |
| `src/vs/editor/contrib/snippet/browser/snippetSession.ts` | 812–898 | `isSelectionWithinPlaceholders`, `getEnclosingRange` |
| `src/vs/editor/contrib/snippet/test/browser/snippetSession.test.ts` | whole file | every test not already ported in P10-23 |

## Files to create or change

- `src/LoomEditor.View/Features/Snippet/SnippetSession.cs` (+ `SnippetSession.Edits.cs` for `adjustWhitespace`, `adjustSelection`, `createEditsAndSnippetsFromSelections`), `SnippetSessionInsertOptions.cs` — create
- `tests/LoomEditor.View.Tests/Features/Snippet/SnippetSessionTests.cs` (+ `.Whitespace.cs`) — create

## Design decisions (already made — do not change)

- **Only `createEditsAndSnippetsFromSelections`** is ported (a template string at the cursors). `ISnippetEdit` and `createEditsAndSnippetsFromEdits` (lines 419–424 and 589–688: snippet edits of workspace edits) are **not** (they belong to LSP snippet text edits: later).
- `SnippetSession(EditorContext editor, string template, SnippetSessionInsertOptions options, ISnippetVariableHost host, TimeProvider time, Random random)`: the last three are the injection points of P10-22 (the controller passes real ones; tests pass doubles). Language configuration comes from the model as P10-22's `CommentBasedVariableResolver` expects.
- `Insert()` applies the edits through `ViewModel.ExecuteEdits` (`source: "snippet"`, a cursor state computer that returns the `Selection`s of the **first placeholder of each snippet**), exactly one undo step. Edits are identified so only the snippet's own inverse edits generate the end cursors (Monaco `edit.identifier = { major: i, minor: 0 }`: use the identifier mechanism of `ExecuteEdits`; if the C# edit type has none, add an `int? Identifier` to the edit record — a few lines — and say so in the commit body).
- `adjustWhitespace` is a **pure static** `(TextModel model, Position position, bool adjustIndentation, TextmateSnippet snippet, ISet<SnippetMarker>? filter)` as in Monaco (lines 427–480). `Model.Options.InsertSpaces` / `TabSize` / `NormalizeIndentation` provide the whitespace settings; `StringClassification` provides leading whitespace.
- `Merge(string template, options)` implements `_templateMerges` bookkeeping and replays the merged template for each cursor as Monaco does (lines 728–766), using `OneSnippet.Merge`.
- `IsSelectionWithinPlaceholders` and `GetEnclosingRange` port the algorithms, including the "all the cursors must be inside placeholders of the same index group" rule.
- Read-only editors: `Insert` does nothing (the controller checks before; the session asserts with `Guard.Assert`).
- `<remarks>Threading: UI thread only.</remarks>`.

## Tests

- [ ] Port: all the remaining tests of `snippetSession.test.ts`, with titles in `// Monaco:` comments; tests that use `SnippetSession.createEditsAndSnippetsFromEdits` / `ISnippetEdit` are skipped with `// Monaco: <title> — snippet edits are out of scope (later)`; tests that use `OvertypingCapturer` likewise. `adjustWhitespace` tests are pure and run on a `TextModel`
- [ ] New: `Insert_AtEveryCursor_OneUndoStep`, `Insert_ReplacesSelection`, `Insert_OverwriteBeforeAfter`, `Insert_AdjustWhitespace_False_KeepsRawTemplate`, `Insert_ResolvesSelectionVariable`, `Insert_NoPlaceholders_EndsAtFinalPosition`, `Insert_WithFinalTabstop_CaretThere`, `Next_Prev_WalkPlaceholders`, `Merge_InsideActivePlaceholder_Nested`, `IsSelectionWithinPlaceholders_OutsideSelection_False`, `GetEnclosingRange_CoversAllSnippets`, `Dispose_RemovesAllDecorations`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `snippetSession.ts` ✅ except `createEditsAndSnippetsFromEdits`; "Behavior differences": no `OvertypingCapturer`, no snippet edits
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The controller, keys and context keys (P10-25), snippet edits, `OvertypingCapturer`.

## Suggested commit message

`feat(view): port SnippetSession (insert, merge, next and previous)`
