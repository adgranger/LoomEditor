# P11-15 — The widget (3/3): the "Suggested change" block and **Apply suggestion**

- **Status:** todo
- **Depends on:** P11-14
- **Size:** M

## Goal

A ` ```suggestion ` block in a comment (and in the Preview) is drawn as a small red / green diff of the commented line under a "Suggested change" header; in a posted comment an **Apply suggestion** button edits the document (one undo step) and raises `SuggestionApplied`. When the line changes, the block redraws.

## Sources

Sources: none (specified in this card; ADR 0014 decision 6). Read first: `SuggestionExtractor.cs`, `SuggestionEdit.cs` (P11-05), `SuggestionDiff.cs` (P11-06), `CommentThreadsController.ApplySuggestion` (P11-09), the manager's `ThreadLineTextChanged` event (P11-08), `MarkdownElementBuilder` `CustomCodeBlock` hook (P11-12), the diff colors of the theme (`diffEditor.insertedLineBackground`, `diffEditor.removedLineBackground`, `diffEditor.insertedTextBackground`, `diffEditor.removedTextBackground` — grep `EditorColorDefaults`), `src/LoomEditor.WinUI/Controls/DiffEditor/` for how the diff colors reach WinUI.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/SuggestionBlockView.cs` — create (`Border`-based element)
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Comments.cs`, `CommentThreadWidget.Preview.cs` — change: pass `CustomCodeBlock` to the builder
- `src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.cs` — change: route the Apply click to the controller, subscribe the widget to `ThreadLineTextChanged`
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-15"

## Design decisions (already made — do not change)

- **`CustomCodeBlock`:** for `MarkdownCodeBlock { Language: "suggestion" }` return a `SuggestionBlockView`; every other block returns `null` (default drawing). The block's index among suggestion blocks of the body is counted by the callback (in order of calls for that body) and matches `SuggestionExtractor`'s `Index`.
- **View** (`SuggestionBlockView(SuggestionDiffResult diff, bool canApply, Action? apply)`):
  - A header row: `CommentStrings.SuggestedChange` (semi-bold) and, right-aligned, the button `CommentStrings.ApplySuggestion` when `apply != null`. A no-op shows `CommentStrings.NoChanges` in the header's place and **no button**; a deletion shows the removed row only and the button.
  - One row per `SuggestionDiffRow`: a 20 px marker column (`-`, `+`, blank; `Marker`), then the text in the **editor's font** (`FontInfo` family and size via the editor, so it aligns with the code the user sees), a trailing empty row text shows as a visible `⏎`-free blank row at the line height. Row background: removed = `diffEditor.removedLineBackground`, added = `diffEditor.insertedLineBackground`, context = transparent. The text colors are the theme's editor foreground (the background carries the meaning, **and** the `-` / `+` markers, so color is never the only signal). Long lines scroll horizontally inside the block (`ScrollViewer`, `HorizontalScrollBarVisibility = Auto`), never wrap.
  - The line text used for the diff is `controller.GetLineText(thread)` **at build time**; the view remembers it (`RenderedLineText`).
- **Apply:** click → `controller.ApplySuggestion(thread, commentId, index, renderedLineText)`. On `true` nothing else (the event, the edit and the redraw come from the model change). On `false` the button stays and gets a `ToolTip` with the reason (`"The line changed. Review the new suggestion and try again."` for stale, `"The editor is read-only."` for read-only; the reasons come from a small `enum ApplyFailure { None, ReadOnly, Stale, Unknown, NoOp }` that `ApplySuggestion` returns through an `out` parameter: add it to P11-09's method in this card, with its tests), and a stale block is redrawn against the current line text. No timer, no message box.
- **Enabled only when** the editor is not read-only (`IsReadOnly`) and the thread belongs to an editor whose text is editable; in the **Preview** the block has `apply == null` (no button).
- **Redraw:** the widget rebuilds the comment rows on `ThreadLineTextChanged` (the manager raises it when the content of the thread's line changes: after Apply, or when the user edits the line). While the user types in the editor the rebuild is coalesced to one per dispatcher tick.
- **Accessibility text:** `AutomationProperties.Name` of the block = `CommentStrings.SuggestedChange`, `AutomationProperties.FullDescription` = `diff.Describe()` (P11-16 completes the rest).
- Theme: colors resolved in `ApplyTheme`, with a fallback (soft red `#33FF0000`-like, soft green `#3300AA00`-like) when the theme has no diff colors.

## Tests

- [ ] Automated: `CommentThreadsControllerTests` gains `ApplySuggestion_Stale_ReportsStaleFailure`, `ApplySuggestion_ReadOnly_ReportsReadOnlyFailure`, `ApplySuggestion_Success_ReportsNone` for the new `out ApplyFailure` parameter. The rest is WinUI (manual).
- [ ] Manual (section "P11-15"; sample: a comment whose body is the text `Use a constant:` followed by a fenced block with the info string `suggestion` containing `const int Max = 10;`, on the line `int max = 10;`): (1) a "Suggested change" block with one red `-` row and one green `+` row; (2) **Apply suggestion** replaces the line, `Ctrl+Z` in the editor restores it, the block then reads "No changes"; (3) an **empty** suggestion shows the removed row only and deletes the line; (4) a two-line suggestion adds a line; (5) edit the commented line by hand: the block redraws against the new text; (6) a read-only editor: the button is disabled with its tooltip; (7) in Preview the block is drawn without a button; (8) in high contrast the `-` / `+` markers are still readable; (9) a very long line scrolls inside the block.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The manual section is written; the card result says it was not run by the agent
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Intra-line highlights, multi-line ranges, "commit suggestion", applying several suggestions at once, marking the thread resolved after Apply (the host decides).

## Suggested commit message

`feat(winui): draw suggestion blocks as a red and green diff with an apply button`
