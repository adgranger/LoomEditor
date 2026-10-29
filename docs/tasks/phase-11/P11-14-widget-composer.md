# P11-14 — The widget (2/3): the composer with the Markdown toolbar, the text box and Preview

- **Status:** todo
- **Depends on:** P11-13
- **Size:** M

## Goal

The composer of the reference screenshot: the title row, a toolbar of twelve buttons, a multi-line text box, a **Preview** toggle that shows the rendered Markdown, and **Cancel** / **Comment** buttons. On an existing thread a **Reply** button opens it. It replaces the temporary controls of P11-11 and keeps their key behavior.

## Sources

Sources: none (specified in this card; ADR 0014 decisions 3, 5, 7). Read first: `CommentThreadWidget*.cs` (P11-13), the temporary controls and `KeyDown` code of P11-11, `CommentThreadViewModel.cs` (P11-07), `MarkdownToolbar*.cs` (P11-03, P11-04), `MarkdownElementBuilder.cs` (P11-12), `Find/FindWidget*.cs` (how it styles a `TextBox` and its buttons inside the control).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Composer.cs`, `CommentComposerToolbar.cs`, `CommentThreadWidget.Preview.cs` — create
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs`, `CommentZoneHost.cs` — change: remove the temporary controls, wire the composer
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-14"

## Design decisions (already made — do not change)

- **Composer area** (a panel under the comment rows, `Visibility` follows `viewModel.IsComposing`):
  1. **Toolbar** (`CommentComposerToolbar`, a horizontal `StackPanel` of twelve `Button`s in the order of `MarkdownToolbarAction`): the content is a short text label (deliberately no icon font): `H`, `B` (bold weight), `I` (italic), `U` (underlined), `S` (struck), `<>`, `{ }`, `Link`, `•`, `1.`, `"`, `±`; each has a `ToolTip` and (P11-16) an automation name from `CommentStrings.ToolbarName`. The buttons are small (28 × 28, no border until hover), **`IsTabStop = false` except the first**; `Left` / `Right` move the focus between them, `Home` / `End` go to the ends (roving focus, ADR 0014 decision 7). They are disabled while **Preview** is on, and in a read-only text box.
  2. **Text box:** `AcceptsReturn = true`, `AcceptsTab = false`, `TextWrapping = Wrap`, `MinHeight = 96`, `MaxHeight = 280` (it scrolls inside), `PlaceholderText = CommentStrings.CommentPlaceholder`, `IsSpellCheckEnabled = false`, `IsTextPredictionEnabled = false`. `TextChanged` → `viewModel.SetDraft(text)` (the box holds `\r` for line breaks; the view model holds `\n`: convert both ways; the **offsets are unchanged** because WinUI uses a single `\r`).
  3. **Preview:** a `ToggleButton` "Preview" (`viewModel.TogglePreview()`); when on, the text box and the toolbar buttons are hidden / disabled and a panel shows the rendered draft (`MarkdownElementBuilder` with the extended parser; an empty draft shows `CommentStrings.NothingToPreview` dimmed). The preview is re-rendered when the draft changes while it is shown. Toggling keeps the text box's selection.
  4. **Buttons** right-aligned: `Cancel` (→ `viewModel.Cancel()`, then `CommentZoneHost.ReturnFocusToEditor`; when the outcome is `RemoveThread` the controller removes the thread) and `Comment` (accent style, `IsEnabled = viewModel.CanSubmit`, → `viewModel.Submit()` then `controller.RaiseSubmitted`). The draft stays until the host adds the comment (P11-07 rule): while it waits the widget changes nothing visually (no spinner in phase 11).
- **Toolbar action:** read `Text` (convert `\r` → `\n`), `SelectionStart`, `SelectionLength` → `MarkdownToolbar.Apply(action, text, selection, viewModel.SuggestionSeed)` → write back `edit.Text` (convert to `\r`) and `Select(start, length)`, focus the text box. A known limit, documented in the file: assigning `Text` clears the text box's own undo stack.
- **Keys in the text box** (in addition to P11-11's `Escape` and `Ctrl+Enter`): `Ctrl+B` bold, `Ctrl+I` italic, `Ctrl+K` link; handled only when the text box has the focus; mark `Handled` for those three only.
- **Reply:** a `Button` "Reply" (`CommentStrings.Reply`) shown when `viewModel.ReplyVisible`; click → `StartComposing()` + `FocusComposer`. A thread with no comment and `startComposing` opens directly in the composer with the title row "Comment on line N" (the title row is the header of P11-13).
- **Layout stability:** switching Preview must not make the widget jump: the preview panel gets `MinHeight` equal to the text box's current `ActualHeight`.
- Theme: the same colors as P11-13 (`ApplyTheme` extends to the new controls: toolbar button foreground, text box background / border from `input.background`, `input.border`, `input.foreground`, accent from `button.background`, `button.foreground`; missing ids fall back to the system accent as the find widget does).

## Tests

- [ ] Automated: none for WinUI. (The toolbar logic is covered by P11-03 / P11-04, the state rules by P11-07.)
- [ ] Manual (section "P11-14"): (1) **Reply** opens the composer, focus lands in the box; (2) each of the twelve buttons does what the tooltip says on a selection and with an empty selection; (3) `Ctrl+B` / `Ctrl+I` / `Ctrl+K` match the buttons; (4) type `**bold** and a [link](http://x)`: Preview shows it rendered, no layout jump, the toolbar is disabled in Preview, going back keeps the caret; (5) **Comment** is disabled for blank text; `Ctrl+Enter` and the button raise the event once; (6) after the host adds the comment the composer closes and the draft is gone; if it does not, the draft is still there; (7) **Cancel** on a thread with no comments removes it and puts the focus back in the editor; on a thread with comments only closes the composer; (8) the box grows to 280 px then scrolls; the widget height follows; (9) the **Suggest a change** button inserts a block containing the commented line, selected; (10) arrow keys move across the toolbar, `Tab` leaves it in one stop.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] No temporary control of P11-11 remains
- [ ] The manual section is written; the card result says it was not run by the agent
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The suggestion block's diff view (P11-15), automation names beyond the ones already set here (P11-16), icon glyphs, drag and drop into the box, a "Write / Preview" tab pair, edit of an existing comment.

## Suggested commit message

`feat(winui): add the comment composer with the Markdown toolbar and preview`
