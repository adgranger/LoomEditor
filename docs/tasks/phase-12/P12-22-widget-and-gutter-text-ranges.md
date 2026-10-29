# P12-22 — The box tells screen readers the commented text; the gutter "+" requests the selected text

- **Status:** todo
- **Depends on:** P12-21
- **Size:** S

**Recommended model:** Sonnet

## Goal

The comment box of an exact-text thread exposes "Commented text: …" to UI Automation, kept current while the text is edited. With a word (or any partial text) selected,
clicking the gutter **+** on a line of the selection requests a thread on that exact text, and the button's name says so. Whole-line behavior is unchanged.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 15, 20 and 22). Read first: `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs`
(`Rebuild`: the automation name, line ~96), `CommentThreadWidget.Suggestions.cs` (`OnLineTextChanged`), `CommentZoneHost.cs` (`OnThreadLineTextChanged` calls it),
`CommentGutterController.cs` (`Update`, `TargetOf`, `OnClick`, `_requestStartLine`, `_requestLine`), `CommentGutterButton.cs` (`SetLines`),
`src/LoomEditor.View/Comments/CommentRange.cs`, `CommentStrings.cs` (P12-17), `CommentThreadViewModel.cs` (`CommentedText`, P12-20),
`docs/tasks/phase-12/P12-11-widget-range.md` and `P12-12-gutter-button-selection.md`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs` — change
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Suggestions.cs` — change: one call in `OnLineTextChanged`
- `src/LoomEditor.WinUI/Controls/Comments/CommentGutterController.cs` — change
- `src/LoomEditor.WinUI/Controls/Comments/CommentGutterButton.cs` — change: `SetLines` → `SetTarget`
- `docs/testing/phase-12-manual-tests.md` — change: section "P12-22", after "P12-21"

## Design decisions (already made — do not change)

- **Widget.** New private `void UpdateCommentedText()` in `CommentThreadWidget.cs`:
  `AutomationProperties.SetHelpText(this, _viewModel.CommentedText is { Length: > 0 } text ? CommentStrings.CommentedText(text) : string.Empty);`. Called at the end of
  `Rebuild` (next to the automation name) and at the start of `OnLineTextChanged` (an edit inside the text raises `ThreadLineTextChanged`, because the covering line changed —
  P12-18). The automation **name** and the title are unchanged (README decision 20).
- **Gutter.** Replace the fields `_requestStartLine`, `_requestLine` by one `CommentRange _request`. `TargetOf` returns a `CommentRange`:
  ```csharp
  Selection selection = _editor.Selections[0];
  if (!selection.IsEmpty)
  {
      CommentRange range = CommentRange.FromSelection(_editor.Model, selection);
      if (range.StartLineNumber <= hoveredLine && hoveredLine <= range.LineNumber) { return range; }
  }
  return CommentRange.Lines(hoveredLine, hoveredLine);
  ```
  (braces on separate lines). A one-line whole-line selection on the hovered line gives the same request as no selection — fine. Computed only when the shown line changes,
  as today. `OnClick` calls `_controller.RequestAdd(_request)`.
- Where the button appears is unchanged (hovered line; `HasThreadOn` compares `thread.LineNumber`).
- **Button.** `SetLines(int, int)` becomes `SetTarget(CommentRange range)`: automation name `range.IsWholeLines ? CommentStrings.AddCommentOnLines(range.StartLineNumber,
  range.LineNumber) : CommentStrings.AddCommentOnSelectedText(range.StartLineNumber, range.LineNumber)`. Its only caller is `Update`.
- `CommentLineRange` is no longer used by `src/` after this card (still used by the Gallery until P12-23).
- Manual section `## P12-22 — Exact text: box and gutter` (controls of P12-23): (1) on the box of a thread on `value`, Accessibility Insights for Windows shows the name
  "Comment thread on line N" and the `HelpText` "Commented text: value"; (2) type inside the word: the `HelpText` follows; (3) select `value`, hover the gutter of that line, click **+**: the log shows `AddCommentRequested, line N, text N:a–N:b`; the button's name
  is "Add a comment on the selected text on line N"; (4) select lines 3–6 with `Shift+Down`, click **+** on line 4: request `lines 3–6`, no `text` (P12-12 unchanged);
  (5) hover a line outside the selection: request for that line only.

## Tests

- [ ] None automated (no WinUI test project; `CommentRange` is tested by P12-17, `CommentedText` by P12-20).
- [ ] Manual: section "P12-22" written (not run by the agent).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `grep -rn CommentLineRange src` finds only `src/LoomEditor.View/Comments/CommentLineRange.cs`
- [ ] The manual section is written; the card's Result section says it was not run
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

A quote of the commented text drawn in the box, columns in the title or the automation name, a selection-changed listener for the gutter button, Gallery changes (P12-23).

## Suggested commit message

`feat(winui): describe the commented text and let the gutter button request it`
