# P11-05 — `CommentStrings`, relative time and avatar initials

- **Status:** todo
- **Depends on:** P11-03
- **Size:** S

## Goal

Three small pure helpers that the later cards use: every English string of the feature in one class, `RelativeTime.Format` ("2 hours ago"), and the initials and color index of an author's avatar.

## Sources

Sources: none (specified in this card; ADR 0014 decisions 8 and 10). Read first: `src/LoomEditor.View/Comments/MarkdownToolbarAction.cs` (P11-03).

## Files to create or change

- `src/LoomEditor.View/Comments/CommentStrings.cs`, `RelativeTime.cs`, `AvatarInitials.cs` — create
- `tests/LoomEditor.View.Tests/Comments/RelativeTimeTests.cs`, `AvatarInitialsTests.cs`, `CommentStringsTests.cs` — create

## Design decisions (already made — do not change)

- All three are `internal static class`es in `LoomEditor.View.Comments`, `<remarks>Threading: pure and thread-safe.</remarks>`.
- **`CommentStrings`** — `const string` fields and `static string` formatting methods (invariant culture):
  - titles and buttons: `ThreadTitle(int line)` → `"Comment on line {line}"`, `Resolved` `"Resolved"`, `Reply` `"Reply"`, `Comment` `"Comment"`, `Cancel` `"Cancel"`, `Preview` `"Preview"`, `CommentPlaceholder` `"Leave a comment"`, `NothingToPreview` `"Nothing to preview"`, `CommentCount(int n)` → `"1 comment"` / `"{n} comments"`, `Edited` `" (edited)"`;
  - suggestion: `SuggestedChange` `"Suggested change"`, `ApplySuggestion` `"Apply suggestion"`, `NoChanges` `"No changes"`, `ApplyFailedStale` `"The line changed. Review the new suggestion and try again."`, `ApplyFailedReadOnly` `"The editor is read-only."`, and the `Describe` pieces used by P11-07: `"Removed: "`, `"Added: "`, `"Line deleted."`, `"(empty)"`;
  - automation: `ThreadAutomationName(int line)` → `"Comment thread on line {line}"`, `CommentAutomationName(string author, string time)` → `"{author}, {time}"`, `CommentText` `"Comment text"`, `AddCommentOnLine(int line)` → `"Add a comment on line {line}"`, `ToolbarName(MarkdownToolbarAction action)` → `Heading`, `Bold`, `Italic`, `Underline`, `Strikethrough`, `Inline code`, `Code block`, `Link`, `Bulleted list`, `Numbered list`, `Quote`, `Suggest a change` (a `switch` expression; an unknown value throws `ArgumentOutOfRangeException`);
  - announcements: `ThreadAdded(int line)` → `"Comment thread added on line {line}"`, `CommentPosted` `"Comment posted"`, `SuggestionApplied` `"Suggestion applied"`, `CommentDiscarded` `"Comment discarded"`.
  - Later cards **add** their strings here; nothing else in the feature contains a user-visible English literal.
- **`RelativeTime.Format(DateTimeOffset when, DateTimeOffset now)`**: `now − when < 1 minute`, or `when` in the future → `"just now"`; `< 60 minutes` → `"N minutes ago"` (`"1 minute ago"`); `< 24 hours` → `"N hours ago"` (`"1 hour ago"`); otherwise, when `when`'s calendar date (in `when`'s offset) is the day before `now`'s date converted to the same offset → `"yesterday"`; `< 30 days` → `"N days ago"`; otherwise `when.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)`. Whole units, truncated (59 s → "just now", 119 s → "1 minute ago").
- **`AvatarInitials`**: `string Get(string name)` — the first letter of the first two words (split on whitespace), upper-cased with `ToUpperInvariant`; one word → one letter; blank → `"?"`; a word starting with a surrogate pair keeps the **pair** (never half of it). `int PaletteIndex(string name, int paletteSize)` — a stable hash: `sum over chars of (c * 31^i)` in unchecked `int` arithmetic, then `(int)((uint)hash % (uint)paletteSize)` (`string.GetHashCode` is randomized per process: do not use it). `paletteSize < 1` → `ArgumentOutOfRangeException`.

## Tests

- [ ] `RelativeTime`: `Format_UnderMinute_JustNow`, `Format_Future_JustNow`, `Format_OneMinute_Singular`, `Format_Minutes`, `Format_OneHour_Singular`, `Format_Hours`, `Format_PreviousDay_Yesterday`, `Format_YesterdayAcrossMidnight`, `Format_Days`, `Format_OverThirtyDays_IsoDate`
- [ ] `AvatarInitials`: `Get_TwoWords_TwoLetters`, `Get_ThreeWords_FirstTwo`, `Get_OneWord_OneLetter`, `Get_Blank_QuestionMark`, `Get_LowerCase_UpperCased`, `Get_SurrogatePair_NotSplit`, `PaletteIndex_SameName_SameIndex`, `PaletteIndex_InRange` (100 names), `PaletteIndex_ZeroSize_Throws`
- [ ] `CommentStrings`: `ThreadTitle_FormatsLine`, `CommentCount_SingularAndPlural`, `ToolbarName_EveryActionHasAName` (all enum values), `ToolbarName_Unknown_Throws`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Localization, timers that refresh relative times, avatar images.

## Suggested commit message

`feat(view): add the comment strings, relative time and avatar initials`
