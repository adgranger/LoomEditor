# P12-10 — Public API of `LoomDiffEditor`: a thread on a range of lines of one side

- **Status:** done
- **Depends on:** P12-09
- **Size:** S

**Recommended model:** Sonnet

## Goal

`diffEditor.AddCommentThread(DiffSide.Modified, 12, 15)` opens a thread on lines 12 to 15 of one side; `AddCommentRequested` of the diff editor carries the first line too.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 4 and 5). Read first: `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.CommentThreads.cs`
(`AddCommentThread`, `OnSideAddCommentRequested`), `DiffAddCommentRequestedEventArgs.cs`, `docs/tasks/phase-11/P11-22-diff-editor-comment-threads.md` (the reveal rule and the
alignment: a comment zone is a foreign zone, so the other side is padded — nothing changes for a range since the zone is still after one line).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.CommentThreads.cs` — change
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffAddCommentRequestedEventArgs.cs` — change
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change
- `docs/testing/phase-12-manual-tests.md` — change: section "P12-10"

## Design decisions (already made — do not change)

- New method: `public CommentThread AddCommentThread(DiffSide side, int startLineNumber, int endLineNumber, string? title = null, bool startComposing = false)`. Validation as
  README decision 5 against that side's `Model.LineCount`. With `startComposing && HideUnchangedRegions`, reveal **both** ends with the existing calls
  (`EnsureOriginalLineIsVisible` / `EnsureModifiedLineIsVisible(line, RevealPreference.FromCloserSide)`, first `startLineNumber`, then `endLineNumber`), then
  `editor.AddCommentThread(start, end, title, startComposing)`. The single-line method becomes `=> AddCommentThread(side, lineNumber, lineNumber, title, startComposing)`
  (same behavior: one reveal of that line — call the reveal once when start == end).
- RS0026 / RS0027: README decision 4.
- `DiffAddCommentRequestedEventArgs`: turn the primary constructor into explicit constructors: `(DiffSide side, int lineNumber)` (unchanged meaning, start = line) and
  `(DiffSide side, int startLineNumber, int lineNumber)` (`ArgumentOutOfRangeException` when start > line); new `public int StartLineNumber { get; }`. XML docs like P12-09.
- `OnSideAddCommentRequested` passes `e.StartLineNumber, e.LineNumber`.
- `SuggestionApplied` of the diff editor forwards the same args object (already the case): nothing to do.
- `PublicAPI.Unshipped.txt`: the method, the new constructor, `StartLineNumber.get`. The existing constructor line stays (same signature).
- Manual section "P12-10": (1) a range thread on the modified side (Gallery, P12-13): box under the last line, the lines highlighted, the original side padded by the box height
  (the line after the box is level on both sides, ±1 px); (2) the same on the original side in side-by-side; (3) inline view: an original-side range thread is not shown (phase 11
  rule); (4) with hidden unchanged regions, `startComposing` on a range whose start is hidden reveals it; (5) applying a suggestion on the modified side replaces the range, one
  `Ctrl+Z` restores it, the diff recomputes.

## Tests

- [x] None automated (no WinUI test project).
- [x] Manual: section "P12-10" written (not run by the agent).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` (WinUI) lists the three new members; no phase 11 line removed
- [x] The manual section is written; the card's Result section says it was not run
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Original-side threads in the inline view, any alignment change, the widget (P12-11).

## Result

Build and tests green. The manual section "P12-10" in `docs/testing/phase-12-manual-tests.md` was written but **not run** (agents do not run the Gallery UI).

## Suggested commit message

`feat(winui): add comment threads on a range of lines to the diff editor API`
