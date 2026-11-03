# P15-22 — Comment composer, toolbar and suggestion blocks

- **Status:** todo
- **Depends on:** P15-21
- **Size:** M (~550 lines; mechanical, allowed above 400)

**Recommended model:** Sonnet

## Goal

A user can write, edit, submit and cancel a comment, use the composer toolbar, and see and apply suggestion blocks, in the Avalonia comment widget.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Composer.cs`, `CommentThreadWidget.Suggestions.cs`, `CommentComposerToolbar.cs`,
`SuggestionBlockView.cs`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/Comments/` — the same 4 files (twins)
- `src/LoomEditor.Avalonia/Themes/Icons.axaml` — the toolbar icons (bold, italic, code, link, suggestion; text labels where WinUI uses text)
- `tests/LoomEditor.Avalonia.Tests/Controls/CommentComposerTests.cs` — create
- `docs/testing/phase-15-manual-tests.md` — section "P15-22" (IME in the composer on the three OSes)

## Design decisions (already made — do not change)

- The composer is an Avalonia `TextBox` (`AcceptsReturn = true`, `TextWrapping = Wrap`). Ctrl+Enter submits (Cmd+Enter on macOS through `KeyMapping.ToModifiers`), as WinUI.
- `SuggestionBlockView` shows the diff of a suggestion with the same colors and the same "Apply" button behavior.

## Tests

- [ ] New: `Composer_CtrlEnter_RaisesCommentSubmitted`; `Composer_Escape_Cancels`; `Toolbar_Bold_WrapsSelection`; `Suggestion_Apply_RaisesSuggestionApplied`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The zone host (P15-23).

## Suggested commit message

`feat(avalonia): comment composer, toolbar and suggestions`
