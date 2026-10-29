# P12-04 — Strings for ranges and a multi-line "Suggest a change" seed

- **Status:** done
- **Depends on:** P12-03
- **Size:** S

**Recommended model:** Sonnet

## Goal

`CommentStrings` can name a range of lines ("Comment on lines 12 to 15"), and the toolbar's **Suggest a change** action accepts a seed of several lines (the range text)
instead of throwing.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 9 and 10). Read first: `src/LoomEditor.View/Comments/CommentStrings.cs`,
`MarkdownToolbar.cs`, `MarkdownToolbar.Blocks.cs` (`InsertSuggestion`, `InsertBlock`, `Fence`), `tests/LoomEditor.View.Tests/Comments/CommentStringsTests.cs`,
`MarkdownToolbarBlockTests.cs` (`Suggestion_SeedWithNewline_Throws`, `Suggestion_SeedWithFence_UsesLongerFence`).

## Files to create or change

- `src/LoomEditor.View/Comments/CommentStrings.cs` — change
- `src/LoomEditor.View/Comments/MarkdownToolbar.Blocks.cs` — change: `InsertSuggestion` only
- `src/LoomEditor.View/Comments/MarkdownToolbar.cs` — change: the XML doc of `suggestionSeed` ("may hold several lines separated by <c>\n</c>")
- `tests/LoomEditor.View.Tests/Comments/CommentStringsTests.cs`, `MarkdownToolbarBlockTests.cs` — change

## Design decisions (already made — do not change)

- New members of `CommentStrings` (all `string.Create(CultureInfo.InvariantCulture, …)` like the existing ones). Each **range** overload returns the phase 11 single-line
  string when `start == end`:
  - `ThreadTitle(int start, int end)` → `"Comment on lines {start} to {end}"`
  - `ThreadAutomationName(int start, int end)` → `"Comment thread on lines {start} to {end}"`
  - `ThreadAdded(int start, int end)` → `"Comment thread added on lines {start} to {end}"`
  - `AddCommentOnLines(int start, int end)` → `"Add a comment on lines {start} to {end}"` (single line: `AddCommentOnLine(start)`)
  - `public const string ApplyFailedStaleLines = "The lines changed. Review the new suggestion and try again.";`
  - (`DescribeLinesDeleted` already exists since P12-03.)
- `InsertSuggestion`: remove the `\n` check and its `ArgumentException`. The fence is three backticks, or, when **any line** of the seed starts with a backtick run of length
  ≥ 3, one backtick longer than the longest such run (a 3-backtick line gives the phase 11 result `````` ```` `````` — the existing test keeps passing). The seed is inserted as
  is (its `\n` stay `\n`) and stays **selected**, as today (`head.Length`, `seed.Length`).
- Replace the test `Suggestion_SeedWithNewline_Throws` by `Suggestion_MultiLineSeed_InsertsAllLinesSelected` (the old behavior is intentionally gone; say so in the
  test's comment: `// Phase 12: a range seed has several lines.`).

## Tests

- [x] Strings: `ThreadTitle_Range_FormatsLines`, `ThreadTitle_RangeOfOne_EqualsSingleLine`, `ThreadAutomationName_Range_FormatsLines`, `ThreadAdded_Range_FormatsLines`, `AddCommentOnLines_Range_FormatsLines`, `AddCommentOnLines_OneLine_EqualsSingleLine`
- [x] Toolbar: `Suggestion_MultiLineSeed_InsertsAllLinesSelected`, `Suggestion_SecondSeedLineHasFence_UsesLongerFence`, `Suggestion_SeedWithFiveBackticks_UsesSix`
- [x] All other existing toolbar and string tests unchanged and green

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Using the strings (P12-07, P12-11, P12-12), localization.

## Suggested commit message

`feat(view): add range strings and accept a multi-line suggestion seed`
