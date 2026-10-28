# P10-25 — `SnippetController`: the feature, the keys and `InsertSnippet`

- **Status:** todo
- **Depends on:** P10-24
- **Size:** M

## Goal

Snippets work in the editor: `LoomEditor.InsertSnippet("for (${1:i} = 0; …$0")` inserts a snippet at the cursors, `Tab` / `Shift+Tab` move between its tab stops, `Escape` leaves snippet mode, typing in a placeholder replaces its default text and updates the mirrored ones, and snippet mode ends by itself when the caret leaves the placeholders or the snippet is undone.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/snippet/browser/snippetController2.ts` | 30–50 | `ISnippetInsertOptions`, `_defaultOptions` (`overtypingCapturer`, `reason` not ported) |
| `src/vs/editor/contrib/snippet/browser/snippetController2.ts` | 51–131 | the context keys (`inSnippetMode`, `hasNextTabstop`, `hasPrevTabstop`; **not** `inSnippetChoice`), constructor, `dispose`, `insert` (the string overload only; `apply(edits)` is not ported) |
| `src/vs/editor/contrib/snippet/browser/snippetController2.ts` | 132–260 | `_doInsert` (undo stops, `_modelVersionId`, merge into an existing session, the three listeners) and `_updateState` (cancel on undo to the "before" version, on plain-text snippets, at the last placeholder, when the selection leaves the placeholders) — **without** the choice completion provider block of 148–206 |
| `src/vs/editor/contrib/snippet/browser/snippetController2.ts` | 290–340 | `finish`, `cancel(resetSelection)`, `prev`, `next`, `isInSnippet`, `getSessionEnclosingRange` |
| `src/vs/editor/contrib/snippet/browser/snippetController2.ts` | 344–392 | the commands and keys: `jumpToNextSnippetPlaceholder` (`Tab`, when `inSnippetMode && hasNextTabstop && textInputFocus`, weight `EditorContrib + 30`), `jumpToPrevSnippetPlaceholder` (`Shift+Tab`, same with `hasPrevTabstop`), `leaveSnippet` (`Escape` and `Shift+Escape`, when `inSnippetMode`), `acceptSnippet` (no key) |
| `src/vs/editor/contrib/snippet/test/browser/snippetController2.test.ts` | whole file (783 lines, 41 tests) | the tests that do not need the suggest widget, the choice provider or `apply(edits)` (the rest are skipped with a comment) |

## Files to create or change

- `src/LoomEditor.View/Features/Snippet/SnippetFeature.cs`, `SnippetController.cs`, `SnippetCommands.cs`, `SnippetInsertOptions.cs`, `README.md` — create (headers: ported from `snippetController2.ts`); add to `EditorFeatures.Default`
- `src/LoomEditor.View/Commands/KeybindingContext.cs`, `DefaultKeybindings.cs` — change: the three fields (`InSnippetMode`, `HasNextTabstop`, `HasPrevTabstop`; fill them where the control fills the others, `grep -rn "SuggestWidgetVisible" src`) and the four rules above
- `src/LoomEditor.View/Features/IEditorContext.cs` / `EditorContext.cs` — change only if the controller needs one more accessor (clipboard text, language id); prefer using what exists
- `src/LoomEditor.WinUI/Controls/LoomEditor.Snippets.cs` — create: `public void InsertSnippet(string template)` and `public void InsertSnippet(string template, int overwriteBefore, int overwriteAfter)` (public API), the `ISnippetVariableHost` implementation that gives `ReadClipboardText` from the existing clipboard service (`WindowsClipboardService` through `IClipboardService`: read how `ClipboardCommands` gets the text) and `null` for the rest
- `tests/LoomEditor.View.Tests/Features/Snippet/SnippetControllerTests.cs` — create
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change

## Design decisions (already made — do not change)

- `SnippetController.Insert(string template, SnippetInsertOptions? options = null)`: `overwriteBefore`, `overwriteAfter`, `adjustWhitespace` (default `true`), `undoStopBefore` / `undoStopAfter` (default `true`), `clipboardText`. The controller is the object other features call (the suggest controller in P10-26 does): expose it as `SnippetController.Get(IEditorContext)`-style accessor like the other features expose theirs (see `FindController` and how `SuggestFeature` obtains `FindController`/`HoverController`: `grep -rn "FindController" src/LoomEditor.View/Features`).
- **Choices:** a choice placeholder shows its first option (P10-19) and Tab moves on; the choice list, the `inSnippetChoice` key and the extra `Shift+Tab` rule of lines 371–378 are **not** ported (listed in the README as "later").
- `Escape` / `Shift+Escape` (`leaveSnippet` → `Cancel(resetSelection: true)`): weight `EditorContrib + 30`, **lower than** the suggest widget's and the find widget's `Escape` rules (the suggest widget must close first, as in Monaco, where the suggest widget has a higher weight).
- `Tab` while the suggest widget is visible accepts the suggestion: the existing suggest rule keeps priority because of its weight; do not change it (check by test: `Tab_WithSuggestWidgetVisible_AcceptsSuggestion`).
- `_modelVersionId` is `TextModel.AlternativeVersionId` (find the C# name: `grep -rn "AlternativeVersionId" src/LoomEditor.Core/Model`); if it does not exist, report instead of inventing a version scheme.
- The three listeners: content change with the **flush** flag (full replacement) → `Cancel`; model replaced → `Cancel`; selection change → `UpdateState`. They are removed during the insertion (`_snippetListener.clear()` in `_doInsert`).
- `Cancel(resetSelection)` resets the selection to the primary cursor when asked (lines 296–315), removes the session, resets the three keys.
- Typing inside a placeholder needs **no** code here: the `AlwaysGrowsWhenTypingAtEdges` decoration of the active group (P10-23) and the mirroring of identical placeholders are done by the model decorations plus `OneSnippet`; verify with a test (`Typing_InPlaceholder_ReplacesDefaultText`, `Typing_InPlaceholder_UpdatesMirroredPlaceholders`). If mirroring needs a hook that Monaco has in `OneSnippet`/the cursor (it does not: Monaco's `$1` mirrors are updated by re-running the `Move` transform on leaving the placeholder and, for plain mirrors, by `_placeholderGroups` edits in `move`; read lines 93–190 again), say so in the commit body and make the test match Monaco's observable behavior.
- `<remarks>Threading: UI thread only.</remarks>`.

## Tests

- [ ] Port: the tests of `snippetController2.test.ts` that use `insert` / `next` / `prev` / `cancel` / `finish` and the context keys; titles in `// Monaco:` comments. Skipped (with a comment): choice completion, `apply(edits)`, anything needing the suggest widget
- [ ] New: `Insert_PlainText_NoSnippetMode`, `Insert_WithTabstops_EntersSnippetMode_SelectsFirstPlaceholder`, `Tab_JumpsToNextPlaceholder_ThenLeavesAtFinal`, `ShiftTab_GoesBack`, `Escape_LeavesSnippet_KeepsText`, `Undo_AfterInsert_LeavesSnippetMode`, `CaretOutsidePlaceholders_LeavesSnippetMode`, `Tab_NotInSnippetMode_InsertsTab`, `Tab_WithSuggestWidgetVisible_AcceptsSuggestion`, `ContextKeys_HasNextHasPrev`, `Insert_MultiCursor_AllGetPlaceholders`, `Insert_ReadOnly_DoesNothing`, `Insert_Nested_MergesIntoActivePlaceholder`, `ClipboardVariable_UsesClipboardService`
- [ ] Manual (section "P10-25", needs P10-29's Gallery button): insert `for (${1:i} = 0; $1 < ${2:n}; $1++) {\n\t$0\n}`, type to replace `i` (the second and third `i` follow after Tab), Tab, Tab, Tab ends at `$0`; Shift+Tab goes back; Escape leaves; a snippet with `${TM_SELECTED_TEXT}` wraps the selection.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `snippetController2.ts` 🔶 (choices, `apply` not ported)
- [ ] Public API file updated; card status and `docs/tasks/README.md` updated

## Out of scope

Choice list, snippet edits (`apply`), registering user snippets (a snippet "library" is an application concern), completion integration (P10-26).

## Suggested commit message

`feat(view): add snippet mode (SnippetController, Tab stops, InsertSnippet)`
