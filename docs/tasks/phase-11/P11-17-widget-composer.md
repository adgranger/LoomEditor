# P11-17 — The widget (2/4): the composer — text box, Preview, Cancel / Comment, Reply

- **Status:** done
- **Depends on:** P11-16
- **Size:** M

## Goal

The composer of the reference screenshot without its toolbar: a multi-line text box with a placeholder, a **Preview** toggle that shows the rendered Markdown, **Cancel** and **Comment** buttons, and a **Reply** button on an existing thread. It replaces the temporary controls of P11-14 and keeps their key behavior. The toolbar comes in P11-18.

## Sources

Sources: none (specified in this card; ADR 0014 decisions 2 and 7). Read first: `CommentThreadWidget*.cs` (P11-16) and its temporary composer panel (P11-14), `CommentZoneHost.FocusComposer` / `ReturnFocusToEditor` (P11-14), `CommentThreadViewModel.cs` (P11-08), `CommentThreadsController.cs` (P11-10), `MarkdownElementBuilder.cs` (P11-15), `Find/FindWidget.xaml` and `FindWidget.Theme.cs` (how a text box and buttons are styled inside the control).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Composer.cs`, `CommentThreadWidget.Preview.cs` — create
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs`, `CommentThreadWidget.Theme.cs` — change: remove the temporary controls, show the composer panel
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-17"

## Design decisions (already made — do not change)

- **Composer panel** (visible when `viewModel.IsComposing`), top to bottom: a reserved slot for the toolbar (an empty `StackPanel` named for P11-18), the text box / preview, the button row.
- **Text box:** `AcceptsReturn = true`, `TextWrapping = Wrap`, `MinHeight = 96`, `MaxHeight = 280` (it scrolls inside), `PlaceholderText = CommentStrings.CommentPlaceholder`, `IsSpellCheckEnabled = false`, `IsTextPredictionEnabled = false`. `TextChanged` → `viewModel.SetDraft(text)`. WinUI's `TextBox.Text` uses a single `\r` per line break, the view model uses `\n`: convert `\r` → `\n` when reading and `\n` → `\r` when writing; offsets are unchanged (one character each). When the view model clears the draft (comment posted) set `Text = ""`.
- **Preview:** a `ToggleButton` "Preview" (`viewModel.TogglePreview()`); on → the text box is collapsed and a panel shows `MarkdownElementBuilder` output for the draft (extended parser); an empty draft shows `CommentStrings.NothingToPreview`, dimmed. The preview panel's `MinHeight` is the text box's last `ActualHeight` (no jump). The preview re-renders only when shown and the draft changed. Off → the text box comes back with its selection.
- **Buttons** right-aligned: **Cancel** (`viewModel.Cancel()` → `ReturnFocusToEditor`; the controller removes the thread when the outcome is `RemoveThread`, P11-10) and **Comment** (accent style; `IsEnabled = viewModel.CanSubmit`, refreshed on `Changed`; click → `viewModel.Submit()`). While the host has not added the comment the composer stays as it is (no spinner).
- **Keys** (the text box's `KeyDown`, kept from P11-14): `Escape` → `TryEscape()` (+ `ReturnFocusToEditor` when it closed), `Ctrl+Enter` → `Submit()`; handled only when they did something.
- **Reply:** a button "Reply" (`CommentStrings.Reply`) under the comments, visible when `viewModel.ReplyVisible`; click → `viewModel.StartComposing()` then `host.FocusComposer(thread)`. A thread created with `startComposing: true` and no comment shows only the title row and the composer.
- **Theme** (`ApplyTheme`): text box `input.background`, `input.border`, `input.foreground`; Comment button `button.background` / `button.foreground`; other buttons transparent with the widget's foreground. Missing ids fall back to the WinUI defaults (do not set the brush).
- The composer is part of the widget's measured height: P11-13 already follows it (growing text box up to 280 px).

## Tests

- [x] Automated: none for WinUI (the rules are in `CommentThreadViewModel`, P11-08).
- [x] Manual (section "P11-17"): (1) **Reply** opens the composer and the caret is in the box; (2) the placeholder shows when empty; (3) Comment is disabled for an empty or blank box and enabled after one letter; (4) `**bold** and [a link](http://x)` in Preview shows bold text and a link; the box height does not jump when toggling; back to the box the caret is where it was; (5) Comment and `Ctrl+Enter` each raise exactly one `CommentSubmitted` (Gallery log); after the Gallery adds the comment the composer closes and the box is empty; (6) Cancel on a new thread removes it and the focus is in the document on its line; on a thread with comments it only closes the composer; (7) type 30 lines: the box stops growing at 280 px and scrolls; the gap under the line follows (no overlap with the next line); (8) `Escape` rules of P11-14 still hold.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] No temporary control of P11-14 remains
- [x] The manual section is written; the card result says the agent did **not** run it
- [x] Card status and `docs/tasks/README.md` updated

## Result

- `CommentThreadWidget.Composer.cs` now holds the real composer (toolbar slot `_toolbarSlot`, text box, preview, Preview / Cancel / Comment, Reply);
  `CommentThreadWidget.Preview.cs` renders the draft. The temporary controls of P11-14 are gone. `CommentThreadWidget.cs` needed no change (it
  already stacked the rows and the composer area).
- Keys: `Escape` and `Ctrl+Enter` are read in the text box's `PreviewKeyDown`, not `KeyDown` (kept from P11-14: the WinUI `TextBox` handles
  `Enter` itself, so `KeyDown` never sees `Ctrl+Enter`).
- Reply calls the widget's own `FocusComposer()` (what `CommentZoneHost.FocusComposer` calls). A composer just made visible is not laid out yet
  and can refuse the focus: `FocusComposer` then retries once on the dispatcher.
- Comment button: `AccentButtonStyle` from the application resources when present. `button.background` / `button.foreground` are not in the built-in
  defaults, so the accent style stays unless a theme defines them; `input.*` ids exist. A missing id clears the brush (WinUI default).
- The manual section "P11-17" is written; the agent did **not** run it.

## Out of scope

The toolbar and its shortcuts (P11-18), the suggestion block (P11-19), automation names beyond `PlaceholderText` (P11-20), a "Write / Preview" tab pair, editing an existing comment.

## Suggested commit message

`feat(winui): add the comment composer with preview`
