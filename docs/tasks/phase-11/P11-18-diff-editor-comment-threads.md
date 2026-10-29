# P11-18 — Comment threads in the diff editor (original and modified side)

- **Status:** todo
- **Depends on:** P11-17
- **Size:** M

## Goal

`LoomDiffEditor.AddCommentThread(DiffSide side, int lineNumber, …)` puts a thread under a line of the original or the modified text. In side-by-side view the other side gets the same extra height so the lines stay level and the two scroll positions stay equal; the four events reach the host from either side.

## Sources

Sources: none (specified in this card; ADR 0014 decisions 2 and 4; ADR 0012 decisions 6–8). Read first, completely: `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.cs`, `LoomDiffEditor.Layout.cs`, `DiffEditorViewZones.cs` (what is "own" and what is "foreign": `_originalOwn` / `_modifiedOwn`, `GetAdditionalLineHeights`, `MaxPasses`), `src/LoomEditor.View/DiffEditor/AdditionalLineHeights.cs`, `LineRangeAlignment.cs`, `InlineViewZones.cs`, `HideUnchangedRegionsController.cs` (can a line be revealed? is there a "reveal line" or "show all" for one region), `docs/how-to/use-the-diff-editor.md`, `tests/LoomEditor.View.Tests/DiffEditor/AdditionalLineHeightsTests.cs` (and neighbours).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.CommentThreads.cs` — create (partial class: the public API, the event forwarding)
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffSide.cs` — create (`public enum DiffSide { Original, Modified }`)
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffEditorViewZones.cs`, `src/LoomEditor.View/DiffEditor/AdditionalLineHeights.cs` — change **only** if the verification below shows a comment zone is not treated as a foreign zone
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `tests/LoomEditor.View.Tests/DiffEditor/AdditionalLineHeightsCommentZoneTests.cs` — create
- `docs/how-to/use-the-diff-editor.md` — change: a short "Comments" paragraph (full how-to is P11-21)
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-18"

## Design decisions (already made — do not change)

- **API** (ADR 0014 decision 2), on `LoomDiffEditor`: `CommentThread AddCommentThread(DiffSide side, int lineNumber, string? title = null, bool startComposing = false)`, `bool RemoveCommentThread(CommentThread thread)`, `IReadOnlyList<CommentThread> CommentThreads` (original's then modified's), `DiffSide GetSide(CommentThread thread)` (throws `ArgumentException` for a thread of no side), the events `CommentSubmitted`, `CommentCancelled`, `AddCommentRequested`, `SuggestionApplied` forwarded from **both** editors (subscribe once when the editors are created; the `sender` is the diff editor). Three of them carry a thread: the host calls `GetSide(args.Thread)`. `AddCommentRequested` has no thread, so the diff editor raises a **subclass** `DiffAddCommentRequestedEventArgs : AddCommentRequestedEventArgs` with a `DiffSide Side` property (make `AddCommentRequestedEventArgs` non-sealed in P11-09's file for this; the event is declared as `EventHandler<DiffAddCommentRequestedEventArgs>` on the diff editor).
- `Original` and `Modified` already expose `LoomEditor`, so the single-editor API works on each; the diff API is the convenience over both, and `Modified.AddCommentThread` / `Original.AddCommentThread` stay valid.
- **Alignment (verify first):** a `Widget` zone is not in `_originalOwn` / `_modifiedOwn`, so `DiffEditorViewZones` must count its height as an **additional line height** of its side and pad the other side by the same amount at the same aligned position. Write the View test (`AdditionalLineHeightsCommentZoneTests`) with a real zone list: a `Widget` zone of 100 px after original line 5 gives the modified side a 100 px pad after the aligned line. If the existing code already does it the test is the proof and no production change is made; if not, fix the smallest thing and say what in the card result.
- **Heights change often** (typing in the composer grows the box): the existing coalescing and the pass limit of `DiffEditorViewZones` must absorb it without drift; the manual test types ten lines into a composer on each side.
- **Inline view** (one visible editor, the modified): threads of the **original** side are not shown (ADR 0014 decision 4). The hidden original editor's widgets must not appear: verify that collapsing / hiding the original editor hides its overlay (the widgets are children of its own `PART_OverlayCanvas`); if the original editor stays in the visual tree with `Visibility.Collapsed` that is enough. A thread on the modified side behaves as in a single editor.
- **Hidden unchanged regions:** a thread whose line is inside a collapsed region is not shown (its zone is not `ShowInHiddenAreas`). When `startComposing: true` is passed for such a line, reveal that region if the controller offers a way to (grep `HideUnchangedRegionsController` for a reveal / show-all of one region); if it does not, throw nothing, add the thread, and document the limit in the how-to (the thread appears when the region is expanded).
- **Read-only original:** the original editor is read-only by default (phase 9): Apply suggestion is refused there (P11-09's rule) and its button is disabled with the read-only tooltip (P11-15). Posting comments works on both sides.
- `LoomDiffEditor` is a different control from `LoomEditor`: it must **not** gain `CommentGutterButtonEnabled`; the host sets the property on `Original` / `Modified` (the how-to says so). Do not add a diff-level property.
- Models are replaced by `OriginalText` / `ModifiedText` setters? **Read how they set the text** (`SetValue` flush): threads clamp to the new line count (P11-08); the card result states what you saw.

## Tests

- [ ] Automated (View): `CommentZoneOnOriginal_PadsModifiedSide`, `CommentZoneOnModified_PadsOriginalSide`, `TwoCommentZonesSameBlock_PadsSum`, `CommentZoneGrowing_UpdatesPad`, `CommentZoneInUnchangedRegion_IsIgnoredOrHidden` (as the code behaves; assert what the card decided)
- [ ] Manual (section "P11-18"; Gallery diff window with the temporary buttons of P11-10 extended by P11-19): (1) a thread on an original line: the modified side gets matching blank space, the lines below stay level; (2) the same on the modified side; (3) scroll both sides: they stay level with threads open; (4) type ten lines in the composer on each side: no drift, no flicker; (5) switch to inline view: the modified thread stays, the original thread disappears; back to side-by-side: it returns with its text intact; (6) collapse unchanged regions: a thread inside one is hidden and returns when expanded; (7) Apply suggestion works on the modified side and is disabled on the original side; (8) events reach one handler on the diff editor with the right `GetSide`.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `PublicAPI.Unshipped.txt` lists exactly the public members above (`DiffSide`, the four events, the four methods / properties, the args subclass)
- [ ] The manual section is written; the card result says what the agent could not run
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Original-side threads in the inline view, threads that follow moved blocks, one thread spanning both sides, a diff-level gutter button property, reverting changes from a thread.

## Suggested commit message

`feat(winui): add comment threads to the diff editor`
