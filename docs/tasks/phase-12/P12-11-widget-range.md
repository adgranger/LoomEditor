# P12-11 — The widget for a range: title, accessibility and an N-line suggestion block

- **Status:** done
- **Depends on:** P12-10
- **Size:** S

**Recommended model:** Sonnet

## Goal

The comment box of a range thread says "Comment on lines 12 to 15" (visually and to UI Automation), its "Suggested change" block shows the red / green diff of all the lines,
and **Apply suggestion** sends back the range text it was drawn against.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 8–10). Read first: `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs` (`Rebuild`:
the automation name), `CommentThreadWidget.Header.cs` (the title comes from `_viewModel.Title`, already range-aware since P12-07), `CommentThreadWidget.Suggestions.cs`
(`SuggestionOptions`, `SuggestionView`, `OnApply`), `SuggestionBlockView.cs` (`RenderedLineText`), `CommentZoneHost.cs` (the `ApplySuggestion` delegate, line ~199),
`CommentThreadWidget.Composer.cs` (the toolbar seed, line ~158), `src/LoomEditor.View/Comments/SuggestionDiff.cs` (P12-03).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs` — change: one line in `Rebuild`
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Suggestions.cs` — change
- `src/LoomEditor.WinUI/Controls/Comments/SuggestionBlockView.cs` — change: rename `RenderedLineText` → `RenderedRangeText` (and its doc)
- `src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.cs` — change: the lambda parameter name only (`rangeText`)
- `docs/testing/phase-12-manual-tests.md` — change: section "P12-11"

## Design decisions (already made — do not change)

- Automation name: `CommentStrings.ThreadAutomationName(thread.StartLineNumber, thread.LineNumber)`. It is set in `Rebuild`, which already runs on every thread change
  (a range change raises `LineChanged` → the view model raises `Changed`), so it stays current.
- `SuggestionOptions` reads `string rangeText = _viewModel.SuggestionSeed` (the range text, P12-07) and passes it on; `SuggestionView` computes
  `SuggestionDiff.Compute(rangeText.Split('\n'), block)` (the list overload of P12-03) and sets `RenderedRangeText = rangeText`; `OnApply` passes `rangeText` to the host
  delegate (whose third parameter is now the range text).
- Stale tooltip: `CommentStrings.ApplyFailedStaleLines` when the thread is multi-line, else `ApplyFailedStale` (phase 11 text kept for one line).
- The composer's **Suggest a change** button already passes `_viewModel.SuggestionSeed`: with P12-04 a multi-line seed works; no change in `Composer.cs`.
- `ThreadLineTextChanged` already fires for any change inside the range (P12-05): the block redraws through the existing `OnLineTextChanged`. No change.
- The box stays under the last line; nothing in `CommentZoneHost` positioning changes.
- Manual section "P12-11" (each with a measurable expectation): (1) title and Narrator name "Comment thread on lines 3 to 6"; (2) **Suggest a change** in the composer of a
  4-line thread inserts a block with the 4 lines selected; (3) a 4-line suggestion that changes line 2 shows `3 context + 1 removed + 1 added` rows in that order; (4) Apply
  replaces the 4 lines, one `Ctrl+Z` restores them; (5) editing a line inside the range then pressing an old Apply shows the "lines changed" tooltip and changes nothing;
  (6) an empty suggestion block on a range shows all lines removed and Narrator's description ends with "Lines deleted."

## Tests

- [x] None automated (no WinUI test project; the logic is covered by P12-03 and P12-07).
- [x] Manual: section "P12-11" written (not run by the agent).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] The manual section is written; the card's Result section says it was not run
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Intra-line highlights, a "show the lines" toggle in the box, any change to the box position or height logic.

## Result

Build and tests green. The manual section "P12-11" in `docs/testing/phase-12-manual-tests.md` was written but **not run** (agents do not run the Gallery UI).

## Suggested commit message

`feat(winui): show the range of a comment thread and its N-line suggestions`
