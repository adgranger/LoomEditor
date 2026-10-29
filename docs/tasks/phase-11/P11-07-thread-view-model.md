# P11-07 — Thread view model: draft, preview, submit and cancel; strings and relative time

- **Status:** todo
- **Depends on:** P11-01, P11-04, P11-06
- **Size:** M

## Goal

The state of one open comment box lives in `CommentThreadViewModel` (draft text, composing, preview, can-submit, collapsed, the title), with the submit / cancel / escape rules of ADR 0014 decision 7. `CommentStrings` holds every English string of the feature and `RelativeTime` formats "2 hours ago". The WinUI widget (P11-13 … P11-15) only binds to this.

## Sources

Sources: none (specified in this card; ADR 0014 decisions 3, 7 and 10). Read first: `CommentThread.cs` (P11-01), `SuggestionDiff.cs` (P11-06, `Describe` literals move to `CommentStrings` here), `src/LoomEditor.View/DiffEditor/DiffEditorViewModel.cs` (the event style of a view model in this code base).

## Files to create or change

- `src/LoomEditor.View/Comments/CommentStrings.cs`, `RelativeTime.cs`, `CommentThreadViewModel.cs` — create
- `src/LoomEditor.View/Comments/SuggestionDiff.cs` — change: use `CommentStrings` for the `Describe` text
- `tests/LoomEditor.View.Tests/Comments/CommentThreadViewModelTests.cs`, `RelativeTimeTests.cs` — create

## Design decisions (already made — do not change)

- `internal static class CommentStrings`: `const string`s (and a few `static string Format…(…)` helpers): `ThreadTitle(int line)` → `"Comment on line {n}"`, `Resolved` `"Resolved"`, `Reply` `"Reply"`, `Comment` `"Comment"`, `Cancel` `"Cancel"`, `Preview` `"Preview"`, `CommentPlaceholder` `"Leave a comment"`, `SuggestedChange` `"Suggested change"`, `ApplySuggestion` `"Apply suggestion"`, `NoChanges` `"No changes"`, `NothingToPreview` `"Nothing to preview"`, `ThreadAutomationName(int line)` `"Comment thread on line {n}"`, the twelve toolbar names (`Heading`, `Bold`, `Italic`, `Underline`, `Strikethrough`, `Inline code`, `Code block`, `Link`, `Bulleted list`, `Numbered list`, `Quote`, `Suggest a change`) as `ToolbarName(MarkdownToolbarAction)`, `CommentText` `"Comment text"`, and the announcement strings (`"Comment thread added on line {n}"`, `"Comment posted"`, `"Suggestion applied"`, `"Comment discarded"`). Later cards add what they need here.
- `internal static class RelativeTime` with `static string Format(DateTimeOffset when, DateTimeOffset now)`: `< 1 minute` or in the future → `"just now"`; `< 60 minutes` → `"N minutes ago"` (`"1 minute ago"`); `< 24 hours` → `"N hours ago"`; the previous calendar day (compare the dates of `when` and `now` in `when`'s own offset) → `"yesterday"`; `< 30 days` → `"N days ago"`; otherwise `when.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)`. Singular forms for 1. No culture-dependent text.
- `internal sealed class CommentThreadViewModel`:
  - ctor `(CommentThread thread, Func<string> getLineText)`; `getLineText` returns the **current** content of the thread's line (used as the suggestion seed). Subscribes to `thread.Changed`; `Dispose()` unsubscribes.
  - Read-only state: `Thread`, `Title` (`thread.Title ?? CommentStrings.ThreadTitle(thread.LineNumber)`), `IsComposing`, `Draft` (always `\n` line breaks), `IsPreview`, `CanSubmit` (`IsComposing && !string.IsNullOrWhiteSpace(Draft)`), `IsCollapsed` (see below), `SuggestionSeed => getLineText()`, `ReplyVisible` (`!IsComposing && thread.State == Open`).
  - Commands (methods): `StartComposing()` (sets `IsComposing`, keeps a previous draft; leaves collapsed state), `SetDraft(string text)` (normalizes `\r\n` / `\r` to `\n`; raises `Changed` only when different), `TogglePreview()` (only while composing), `string? Submit()` (returns the draft with trailing blanks / newlines trimmed and raises `SubmitRequested`; **does not clear the draft**; `null` and nothing raised when `!CanSubmit`), `CancelOutcome Cancel()` (always discards the draft, sets `IsComposing = false`, `IsPreview = false`; returns `RemoveThread` when the thread has no comments, else `Closed`; raises `CancelRequested`), `bool TryEscape()` (when composing and the draft is **empty or blank** behaves as `Cancel()` and returns `true`; when composing with a non-blank draft returns `false` and changes nothing; when not composing returns `false`), `ToggleCollapsed()` (only meaningful when `thread.State == Resolved`).
  - **Draft survives a failed post:** when `thread.Changed` reports `CommentAdded` while composing, the draft is cleared and `IsComposing = false`, `IsPreview = false`. Any other thread change only raises `Changed` (title, line, state).
  - `IsCollapsed` = `thread.State == Resolved && !_expandedWhenResolved`; a state change to `Open` resets the flag; a change to `Resolved` collapses.
  - Events: `Changed` (any state above changed), `SubmitRequested` (`EventHandler<string>`), `CancelRequested` (`EventHandler<CancelOutcome>`). `enum CancelOutcome { Closed, RemoveThread }`.
  - Pure: no UI types, no timers. UI thread only (`<remarks>`).

## Tests

- [ ] `RelativeTime`: `Format_UnderMinute_JustNow`, `Format_Future_JustNow`, `Format_OneMinute_Singular`, `Format_Minutes`, `Format_Hours`, `Format_PreviousDay_Yesterday`, `Format_Days`, `Format_OverThirtyDays_IsoDate`, `Format_YesterdayAcrossMidnight`
- [ ] View model: `Title_NoTitle_UsesLine`, `Title_Set_UsesIt`, `CanSubmit_BlankDraft_False`, `SetDraft_NormalizesCr`, `Submit_Blank_ReturnsNullAndRaisesNothing`, `Submit_KeepsDraft`, `Submit_TrimsTrailingBlanks`, `CommentAdded_WhileComposing_ClearsDraftAndCloses`, `Cancel_NoComments_RemoveThread`, `Cancel_WithComments_Closed`, `Cancel_DiscardsDraft`, `TryEscape_EmptyDraft_Cancels`, `TryEscape_NonEmptyDraft_DoesNothing`, `TryEscape_NotComposing_False`, `Resolved_CollapsesAndToggleExpands`, `StateOpen_ResetsExpandedFlag`, `SuggestionSeed_ReadsCurrentLine`, `Dispose_Unsubscribes`, `TogglePreview_NotComposing_Ignored`, `ReplyVisible_ResolvedThread_False`
- [ ] `SuggestionDiffTests` from P11-06 still pass (the text now comes from `CommentStrings`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Any WinUI element, the anchoring of the thread to a line (P11-08), timers that refresh "2 minutes ago", localization.

## Suggested commit message

`feat(view): add the comment thread view model, strings and relative time`
