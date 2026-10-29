# P11-08 — Thread view model: draft, preview, submit, cancel and Escape

- **Status:** done
- **Depends on:** P11-01, P11-05
- **Size:** S

## Goal

The state of one comment box lives in `CommentThreadViewModel` (draft text, composing, preview, can-submit, collapsed, title) with the submit / cancel / Escape rules of ADR 0014 decisions 2 and 7. The WinUI widget only reads it and calls its methods.

## Sources

Sources: none (specified in this card). Read first: `CommentThread.cs` (P11-01), `CommentStrings.cs` (P11-05), `src/LoomEditor.View/DiffEditor/DiffEditorViewModel.cs` (the plain-event style of a view model in this code base: no observables, no `INotifyPropertyChanged`).

## Files to create or change

- `src/LoomEditor.View/Comments/CommentThreadViewModel.cs`, `CancelOutcome.cs` — create
- `tests/LoomEditor.View.Tests/Comments/CommentThreadViewModelTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class CommentThreadViewModel : IDisposable`, `<remarks>Threading: UI thread only.</remarks>`; ctor `(CommentThread thread, Func<string> getLineText)` — `getLineText` returns the **current** content of the thread's line (the suggestion seed). Subscribes to `thread.Changed`; `Dispose()` unsubscribes (idempotent).
- **State** (read-only properties): `Thread`; `Title` = `thread.Title ?? CommentStrings.ThreadTitle(thread.LineNumber)`; `IsComposing`; `Draft` (always `\n` line breaks, never `null`); `IsPreview`; `CanSubmit` = `IsComposing && !string.IsNullOrWhiteSpace(Draft)`; `IsCollapsed` = `thread.State == Resolved && !_expandedWhileResolved`; `ReplyVisible` = `!IsComposing && thread.State == Open`; `SuggestionSeed` = `getLineText()`.
- **Methods:**
  - `StartComposing()` — sets `IsComposing`; keeps an existing draft; also expands a collapsed thread.
  - `SetDraft(string text)` — `ArgumentNullException`; normalizes `\r\n` and `\r` to `\n`; raises `Changed` only when the value changed.
  - `TogglePreview()` — only while composing; otherwise ignored.
  - `string? Submit()` — `null` and nothing raised when `!CanSubmit`; otherwise returns the draft with trailing whitespace and newlines trimmed (`TrimEnd()`), raises `SubmitRequested` with that text, and **keeps the draft** (the host may fail to post).
  - `CancelOutcome Cancel()` — discards the draft, `IsComposing = false`, `IsPreview = false`; returns `RemoveThread` when the thread has no comments, else `Closed`; raises `CancelRequested` with the outcome. When not composing it still returns the outcome (the Cancel button is only visible while composing; keep it simple).
  - `bool TryEscape()` — composing with a draft that is empty or whitespace → `Cancel()` and `true`; composing with a non-blank draft → `false`, nothing changes; not composing → `false`.
  - `ToggleCollapsed()` — only when `thread.State == Resolved`; flips `_expandedWhileResolved`.
- **Thread changes:** `CommentAdded` while composing → clear the draft, `IsComposing = false`, `IsPreview = false` (the host posted the comment). `StateChanged` → `_expandedWhileResolved = false`. Every thread change raises `Changed` (title, line number and state are shown).
- **Events:** `Changed` (`EventHandler`), `SubmitRequested` (`EventHandler<string>`), `CancelRequested` (`EventHandler<CancelOutcome>`). `internal enum CancelOutcome { Closed, RemoveThread }`.
- No UI types, no timers, no dispatcher.

## Tests

- [x] New: `Title_NoTitle_UsesLine`, `Title_Set_UsesIt`, `Title_LineMoved_RaisesChanged`, `CanSubmit_BlankDraft_False`, `CanSubmit_NotComposing_False`, `SetDraft_NormalizesCr`, `SetDraft_Same_RaisesNothing`, `Submit_Blank_ReturnsNullAndRaisesNothing`, `Submit_KeepsDraft`, `Submit_TrimsTrailingWhitespace`, `CommentAdded_WhileComposing_ClearsDraftAndCloses`, `CommentAdded_NotComposing_KeepsState`, `Cancel_NoComments_RemoveThread`, `Cancel_WithComments_Closed`, `Cancel_DiscardsDraftAndPreview`, `TryEscape_EmptyDraft_Cancels`, `TryEscape_WhitespaceDraft_Cancels`, `TryEscape_NonBlankDraft_DoesNothing`, `TryEscape_NotComposing_False`, `Resolved_IsCollapsed`, `ToggleCollapsed_Resolved_Expands`, `StateChange_ResetsExpanded`, `StartComposing_ExpandsCollapsed`, `TogglePreview_NotComposing_Ignored`, `ReplyVisible_ResolvedThread_False`, `SuggestionSeed_ReadsCurrentLine`, `Dispose_Unsubscribes`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Any WinUI element, anchoring to a line (P11-09), the controller (P11-10).

## Suggested commit message

`feat(view): add the comment thread view model`
