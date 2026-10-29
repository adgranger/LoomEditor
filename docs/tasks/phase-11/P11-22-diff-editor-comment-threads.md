# P11-22 — Comment threads in the diff editor (original and modified side)

- **Status:** todo
- **Depends on:** P11-21
- **Size:** M

## Goal

`LoomDiffEditor.AddCommentThread(DiffSide side, int lineNumber, …)` puts a thread under a line of the original or the modified text. In side-by-side view the other side gets the same extra height, so the lines stay level and the scroll positions stay equal; the four events reach the host from either side.

## Sources

Sources: none (specified in this card; ADR 0014 decisions 2 and 4; ADR 0012 decisions 6–8). Read first, completely:
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.cs` (`Original`, `Modified`, `DiffChanged`, where the two editors are created), `LoomDiffEditor.Layout.cs` (lines 49–92: the view zones object is re-created when side-by-side / inline changes; in the inline view `original.Visibility = Collapsed`), `LoomDiffEditor.Options.cs` (lines 296–299: `original.IsReadOnly = !OriginalEditable`; `ApplyText` for `OriginalText` / `ModifiedText`);
- `DiffEditorViewZones.cs` (`_originalOwn` / `_modifiedOwn`, `MaxPasses`, the coalesced recompute) and `src/LoomEditor.View/DiffEditor/AdditionalLineHeights.cs` (`Compute(whitespaces, ignoredIds, …)`: every whitespace whose id is **not** in `ignoredIds` counts — a comment zone is such a foreign whitespace);
- `src/LoomEditor.View/DiffEditor/DiffEditorViewModel.cs` (`EnsureOriginalLineIsVisible`, `EnsureModifiedLineIsVisible` with `RevealPreference`; used by `HideUnchangedRegionsController.cs` lines 186–190);
- `tests/LoomEditor.View.Tests/DiffEditor/AdditionalLineHeightsTests.cs`; `docs/how-to/use-the-diff-editor.md`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.CommentThreads.cs` — create (partial: API, event forwarding)
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffSide.cs` — create (`public enum DiffSide { Original, Modified }`)
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffAddCommentRequestedEventArgs.cs` — create
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffEditorViewZones.cs` — change **only** if the tests or the manual check show a comment zone is not padded
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `tests/LoomEditor.View.Tests/DiffEditor/AdditionalLineHeightsCommentZoneTests.cs` — create
- `docs/how-to/use-the-diff-editor.md` — change: a short "Comment threads" paragraph
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-22"

## Design decisions (already made — do not change)

- **API** (ADR 0014 decision 2): `AddCommentThread(DiffSide side, int lineNumber, string? title = null, bool startComposing = false)` → `Original` / `Modified`.`AddCommentThread`; `RemoveCommentThread(thread)` tries both; `CommentThreads` = original's then modified's; `GetSide(thread)` (`ArgumentException` for a thread of neither side); events `CommentSubmitted`, `CommentCancelled`, `SuggestionApplied` with the **same** args classes as `LoomEditor` (the host calls `GetSide(args.Thread)`), and `AddCommentRequested` with its own `public sealed class DiffAddCommentRequestedEventArgs(DiffSide side, int lineNumber) : EventArgs` (no inheritance from `AddCommentRequestedEventArgs`, which stays sealed). `sender` is the diff editor. Subscribe to both editors once, where they are created; unsubscribe where they are released.
- `Original.AddCommentThread` / `Modified.AddCommentThread` stay valid and their threads are part of the diff editor's list and events (the forwarding is per editor, not per call).
- **Alignment:** no new code is expected. The test proves it: `AdditionalLineHeights.Compute` with a whitespace list that contains a 100 px comment zone after view line 5 and an `ignoredIds` set without it returns `(5, 100)`; with the zone id in `ignoredIds` it does not. Then the manual check (below) confirms the other side is padded. If the manual check fails, the fix is in `DiffEditorViewZones` (it must pass only its own ids as `ignoredIds`); report what you changed.
- **Heights that change while typing** go through the existing coalesced recompute of `DiffEditorViewZones` (`ZonesChanged` already triggers it). No new scheduling.
- **Inline view:** the original editor is `Collapsed`, so its zone-widget layer and its boxes are not shown; original-side threads come back in side-by-side view with their drafts (the view models are not touched). Nothing to code; the manual test checks it.
- **Hidden unchanged regions:** a comment zone has `ShowInHiddenAreas = false`, so a thread inside a collapsed region is hidden with it. `AddCommentThread(..., startComposing: true)` first calls `_viewModel.EnsureOriginalLineIsVisible(line, RevealPreference.FromCloserSide)` / `EnsureModifiedLineIsVisible` for its side (only when hiding unchanged regions is on), then adds the thread.
- **Read-only original:** `ApplySuggestion` returns `ReadOnly` there (P11-10); the button shows the read-only tooltip (P11-19). Posting comments works on both sides.
- `CommentGutterButtonEnabled` is **not** added to `LoomDiffEditor`: the host sets it on `Original` and `Modified` (the how-to says so).
- **Text replaced** through `OriginalText` / `ModifiedText` (`ApplyText`): threads clamp to the new line count (P11-09 rule); state it in the how-to.

## Tests

- [ ] Automated (View): `CommentZone_NotIgnored_CountsAsAdditionalHeight`, `CommentZone_Ignored_NotCounted`, `TwoCommentZonesSameLine_HeightsAdd`, `CommentZoneAndWrappedLine_Combine` (a zone plus wrapping on the same model line: one combined entry)
- [ ] Manual (section "P11-22"; Gallery diff window, buttons added by this card in `DiffDemoWindow`: **Add on original at caret**, **Add on modified at caret**; P11-23 finalizes them): (1) a thread on original line 10: the modified side shows blank space of the same height after the matching line, and the first line below the box is at the same y on both sides (screenshot, ±1 px); (2) the same from the modified side; (3) scroll: both sides stay level; (4) type ten lines in a composer on each side: the sides stay level after each line; (5) inline view: the modified thread stays, the original thread is not shown; side-by-side again: it is back with its draft; (6) with hidden unchanged regions, `startComposing` on a hidden line expands it and opens the box; (7) Apply suggestion works on the modified side; on the original side (not editable) the button is disabled with the read-only tooltip; (8) the Gallery log shows each event once with the right `GetSide` / `Side`.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `PublicAPI.Unshipped.txt` lists exactly `DiffSide`, `DiffAddCommentRequestedEventArgs`, and the new `LoomDiffEditor` members
- [ ] The manual section is written; the card result says the agent did **not** run it
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Original-side threads in the inline view, threads that follow moved blocks, one thread spanning both sides, a diff-level gutter property, reverting a change from a thread.

## Suggested commit message

`feat(winui): add comment threads to the diff editor`
