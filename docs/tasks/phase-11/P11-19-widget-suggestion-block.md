# P11-19 — The widget (4/4): the "Suggested change" block and **Apply suggestion**

- **Status:** todo
- **Depends on:** P11-18
- **Size:** M

## Goal

A ` ```suggestion ` block in a comment (and in the Preview) is drawn as a small red / green diff of the commented line under a "Suggested change" header; in a posted comment an **Apply suggestion** button edits the document (one undo step) and raises `SuggestionApplied`. When the line changes, the block redraws.

## Sources

Sources: none (specified in this card; ADR 0014 decision 6). Read first: `SuggestionExtractor.cs`, `SuggestionEdit.cs` (P11-06), `SuggestionDiff.cs` (P11-07), `CommentThreadsController.ApplySuggestion` (P11-10), the manager's `ThreadLineTextChanged` event (P11-09), `MarkdownElementBuilder` `CustomCodeBlock` hook (P11-15), the diff colors of the theme (`diffEditor.insertedLineBackground`, `diffEditor.removedLineBackground`, `diffEditor.insertedTextBackground`, `diffEditor.removedTextBackground` — grep `EditorColorDefaults`), `src/LoomEditor.WinUI/Controls/DiffEditor/` for how the diff colors reach WinUI.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/SuggestionBlockView.cs` — create (`Border`-based element)
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Comments.cs`, `CommentThreadWidget.Preview.cs` — change: pass `CustomCodeBlock` to the builder
- `src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.cs` — change: route the Apply click to the controller, subscribe the widget to `ThreadLineTextChanged`
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-19"

## Design decisions (already made — do not change)

- **`CustomCodeBlock`:** for `MarkdownCodeBlock { Language: "suggestion" }` return a `SuggestionBlockView`; every other block returns `null` (default drawing). The block's index among suggestion blocks of the body is counted by the callback: the counter is reset before each `Build` call for a body, the builder calls the hook in document order for top-level blocks (check it; if it also calls it for code blocks inside a quote, skip those, as `SuggestionExtractor` does), so it matches `SuggestionExtractor`'s `Index`.
- **View** (`SuggestionBlockView(SuggestionDiffResult diff, bool canApply, Action? apply)`):
  - A header row: `CommentStrings.SuggestedChange` (semi-bold) and, right-aligned, the button `CommentStrings.ApplySuggestion` when `apply != null`. A no-op shows `CommentStrings.NoChanges` in the header's place and **no button**; a deletion shows the removed row only and the button.
  - One row per `SuggestionDiffRow`: a 20 px marker column (`-`, `+`, blank; `Marker`), then the text in the editor's code font (the control's `FontFamily` and `FontSize`); an empty row is still one line high. Row background: removed = `diffEditor.removedLineBackground`, added = `diffEditor.insertedLineBackground`, context = transparent. The text colors are the theme's editor foreground (the background carries the meaning, **and** the `-` / `+` markers, so color is never the only signal). Long lines scroll horizontally inside the block (`ScrollViewer`, `HorizontalScrollBarVisibility = Auto`), never wrap.
  - The line text used for the diff is `controller.GetLineText(thread)` **at build time**; the view remembers it (`RenderedLineText`).
- **Apply:** click → `ApplySuggestionResult r = controller.ApplySuggestion(thread, commentId, index, renderedLineText)` (P11-10). `Applied` → nothing else (the edit raises `ThreadLineTextChanged`, which redraws). `Stale` → the block is redrawn against the current line and the button's `ToolTip` becomes `CommentStrings.ApplyFailedStale`; `ReadOnly` → `ToolTip` = `CommentStrings.ApplyFailedReadOnly`; `NoOp` / `NotFound` → redraw only. No timer, no dialog.
- **Enabled only when** the editor is not read-only (`LoomEditor.IsReadOnly`; the original side of a diff editor is read-only unless `OriginalEditable`), with the read-only tooltip when disabled; in the **Preview** the block has `apply == null` (no button).
- **Redraw:** the widget rebuilds the comment rows on `ThreadLineTextChanged` (the manager raises it when the content of the thread's line changes: after Apply, or when the user edits the line). While the user types in the editor the rebuild is coalesced to one per dispatcher tick.
- **Accessibility text:** `AutomationProperties.Name` of the block = `CommentStrings.SuggestedChange`, `AutomationProperties.FullDescription` = `diff.Describe()` (P11-20 completes the rest).
- Theme: colors resolved in `ApplyTheme`, with a fallback (soft red `#33FF0000`-like, soft green `#3300AA00`-like) when the theme has no diff colors.

## Tests

- [ ] Automated: none new (the apply results are tested in P11-10, the rows in P11-07).
- [ ] Manual (section "P11-19"; sample: a comment whose body is the text `Use a constant:` followed by a fenced block with the info string `suggestion` containing `const int Max = 10;`, on the line `int max = 10;`): (1) a "Suggested change" block with one red `-` row and one green `+` row; (2) **Apply suggestion** replaces the line, `Ctrl+Z` in the editor restores it, the block then reads "No changes"; (3) an **empty** suggestion shows the removed row only and deletes the line; (4) a two-line suggestion adds a line; (5) edit the commented line by hand: the block redraws against the new text; (6) a read-only editor: the button is disabled with its tooltip; (7) in Preview the block is drawn without a button; (8) in high contrast the `-` / `+` markers are still readable; (9) a very long line scrolls inside the block.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The manual section is written; the card result says it was not run by the agent
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Intra-line highlights, multi-line ranges, "commit suggestion", applying several suggestions at once, marking the thread resolved after Apply (the host decides).

## Suggested commit message

`feat(winui): draw suggestion blocks as a red and green diff with an apply button`
