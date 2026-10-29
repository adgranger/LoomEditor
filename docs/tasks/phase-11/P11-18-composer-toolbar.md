# P11-18 — The widget (3/4): the Markdown toolbar of the composer and its shortcuts

- **Status:** todo
- **Depends on:** P11-17
- **Size:** S

## Goal

The twelve toolbar buttons above the text box (heading, bold, italic, underline, strikethrough, inline code, code block, link, bulleted list, numbered list, quote, suggest a change) apply `MarkdownToolbar` to the text box's text and selection; `Ctrl+B`, `Ctrl+I` and `Ctrl+K` do the same as their buttons. The toolbar is one tab stop with arrow-key navigation.

## Sources

Sources: none (specified in this card; ADR 0014 decision 7). Read first: `MarkdownToolbar.cs`, `MarkdownToolbar.Inline.cs`, `MarkdownToolbar.Blocks.cs`, `ToolbarSelection.cs`, `ToolbarEdit.cs` (P11-03, P11-04), `CommentStrings.ToolbarName` (P11-05), `CommentThreadWidget.Composer.cs` (P11-17: the reserved toolbar slot, the `\r` / `\n` conversion).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentComposerToolbar.cs` — create
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Composer.cs` — change: put the toolbar in its slot, the three shortcuts
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-18"

## Design decisions (already made — do not change)

- `internal sealed class CommentComposerToolbar : StackPanel` (horizontal), ctor `(Action<MarkdownToolbarAction> apply)`; one `Button` per `MarkdownToolbarAction` value, in enum order. Content is a short **text** label (no icon font, no image): `H`, `B` (bold), `I` (italic), `U` (underlined), `S` (struck), `<>`, `{ }`, `Link`, `•`, `1.`, `"`, `±`. `ToolTipService.ToolTip` and `AutomationProperties.Name` = `CommentStrings.ToolbarName(action)`. 28 × 28, no border at rest (`BorderThickness = 0`, transparent background).
- **Roving focus:** only the first button has `IsTabStop = true`; the others `false`. In the toolbar's `KeyDown`: `Left` / `Right` move the focus to the previous / next button (wrapping), `Home` / `End` to the ends; the button that gets the focus becomes the only tab stop (so `Shift+Tab` back into the toolbar lands on it). Mark those four keys handled.
- **Apply (one method in the composer):** `string text = box.Text.Replace('\r', '\n')`; `var sel = new ToolbarSelection(box.SelectionStart, box.SelectionStart + box.SelectionLength)`; `ToolbarEdit edit = MarkdownToolbar.Apply(action, text, sel, viewModel.SuggestionSeed)`; `box.Text = edit.Text.Replace('\n', '\r')`; `box.Select(edit.Selection.Start, edit.Selection.Length)`; `box.Focus(FocusState.Programmatic)`. Known limit, written in an XML doc remark: assigning `Text` clears the text box's own undo history.
- **Disabled** while Preview is on (`IsEnabled = !viewModel.IsPreview`).
- **Shortcuts** in the text box's `KeyDown` (with P11-17's `Escape` / `Ctrl+Enter`): `Ctrl+B` → `Bold`, `Ctrl+I` → `Italic`, `Ctrl+K` → `Link`; handled. No other shortcut. (`Ctrl+I` in a WinUI `TextBox` does nothing by default; `Ctrl+B` neither: no conflict to resolve.)
- `Suggestion` uses `viewModel.SuggestionSeed` (the current content of the thread's line).

## Tests

- [ ] Automated: none for WinUI (the edits are tested in P11-03 / P11-04).
- [ ] Manual (section "P11-18"): for **each** of the twelve buttons, with a word selected and with an empty selection, the result matches the expected text written in the plan (the plan lists the twelve × 2 expected texts, copied from the P11-03 / P11-04 tests); `Ctrl+B` / `Ctrl+I` / `Ctrl+K` match their buttons; **Suggest a change** on line `int max = 10;` inserts a `suggestion` block containing that line, with the line selected; `Tab` enters the toolbar once and leaves it on the next `Tab`; arrows move between buttons; the toolbar is disabled in Preview; with Narrator each button reads its name.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The manual section is written; the card result says the agent did **not** run it
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Icons, keeping the text box's undo history across a toolbar action, more shortcuts.

## Suggested commit message

`feat(winui): add the Markdown toolbar to the comment composer`
