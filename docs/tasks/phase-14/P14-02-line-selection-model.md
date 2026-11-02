# P14-02 — The line selection model and its public facade

- **Status:** done
- **Depends on:** P14-01
- **Size:** M

**Recommended model:** Sonnet

## Goal

`LoomDiffEditor` keeps a selection of changed lines on either side (`SelectedLines`, `SetSelection`, `ClearSelection`, `SelectionChanged`), backed by a unit-tested
View model that implements click, Ctrl+click (pair) and Shift+click (range), and survives a re-diff of unchanged texts. No UI yet (P14-04 adds the checkboxes).

## Sources

Sources: none (specified in this card; [phase-14/README.md](README.md) decisions 2–3). Read first: `DiffChangeBlock.cs` (P14-01),
`src/LoomEditor.Core/Model/TextModel.cs` (`VersionId`), `LoomDiffEditor.cs` (`OnViewModelDiffChanged`, `OnUnloaded`), `DiffSide.cs`.

## Files to create or change

- `src/LoomEditor.View/DiffEditor/DiffLineSelection.cs` — create (internal)
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffLine.cs` — create: `public readonly record struct DiffLine(DiffSide Side, int LineNumber)`
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.LineSelection.cs` — create
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — `./eng/Update-PublicApi.ps1`
- `tests/LoomEditor.View.Tests/DiffEditor/DiffLineSelectionTests.cs` — create

## Design decisions (already made — do not change)

- View cannot see `DiffSide`: the model uses `bool modified` + `int line`. Internal API:
  - `SetBlocks(IReadOnlyList<DiffChangeBlock> blocks, bool keepSelection)` — `keepSelection` false clears; true removes lines that are no longer changed lines and
    keeps the anchor only if it is still a changed line. Returns whether the selection changed.
  - `bool IsChangedLine(bool modified, int line)`, `bool IsSelected(bool modified, int line)`.
  - `bool Toggle(bool modified, int line)` — flips one line; sets the anchor.
  - `bool TogglePair(bool modified, int line)` — the line and its partner (decision 3); if either is unselected both become selected, else both unselected; no
    partner → as `Toggle`. Sets the anchor to the clicked line.
  - `bool ExtendTo(bool modified, int line)` — no anchor, or the anchor on the other side → as `Toggle`. Else **adds** every changed line of that side between the
    anchor and `line` inclusive (unchanged lines between two blocks are skipped). The anchor does not move.
  - `bool Set(IEnumerable<(bool modified, int line)>)` — replaces; non-changed lines are **ignored** (the host may call it before the diff); clears the anchor.
  - `bool Clear()`.
  - `IReadOnlyList<(bool modified, int line)> Lines` — original side ascending, then modified side ascending; cached until the next change.
  - Every mutator returns `true` only when the set really changed (so `SelectionChanged` is not raised for nothing).
- Lookup: blocks are sorted; `IsChangedLine` and the pair partner use a binary search over the blocks of that side (`List.BinarySearch` with a comparer, or a manual
  loop) — **no per-line array** (tens of thousands of lines). Storage: two `HashSet<int>`.
- `LoomDiffEditor` (new partial `LoomDiffEditor.LineSelection.cs`): a `DiffLineSelection _lineSelection` created with the control (not with the view model, so
  `SetSelection` works before load). In `OnViewModelDiffChanged`, after `Changes` is rebuilt (P14-01) and before `DiffChanged`: `keepSelection` = both
  `Model.VersionId`s equal the ones stored at the previous rebuild; then store the new ids; raise `SelectionChanged` if `SetBlocks` returned true.
- Public members (XML doc each): `SelectedLines` (a new `DiffLine[]` from `Lines`), `SetSelection(IEnumerable<DiffLine>)` (throws `ArgumentNullException`; ignores
  lines that are not changed lines: say so), `ClearSelection()`, `event EventHandler? SelectionChanged`. Internal `ToggleLine`, `ToggleLinePair`, `ExtendLineSelection`
  (`DiffLine` in, raise the event) for P14-04.

## Tests

- [x] New: `Toggle_AddedLine_SelectsOnlyIt`, `Toggle_DeletedLine_SelectsOnlyIt`, `Toggle_Twice_Unselects`
- [x] New: `Toggle_UnchangedLine_ReturnsFalse`
- [x] New: `TogglePair_ModifiedPair_SelectsBoth`, `TogglePair_OneAlreadySelected_SelectsBoth`, `TogglePair_BothSelected_UnselectsBoth`
- [x] New: `TogglePair_LongerSide_NoPartner_TogglesOne` (block 2 deleted / 4 added: added line 3 and 4 have no partner)
- [x] New: `ExtendTo_SameSide_AddsChangedLinesOnly` (across two blocks; unchanged lines between them stay out)
- [x] New: `ExtendTo_OtherSide_ActsAsToggle`
- [x] New: `Lines_OriginalFirstThenModified_Ascending`
- [x] New: `SetBlocks_Keep_DropsLinesNoLongerChanged`, `SetBlocks_NotKeep_Clears`
- [x] New: `Set_IgnoresUnchangedLines`, `Mutators_NoChange_ReturnFalse`
- [x] New: `IsChangedLine_FiftyThousandLines_IsFast` — 10 000 blocks; 100 000 lookups under 200 ms (`Stopwatch`, generous bound; it guards against a linear scan)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Checkboxes, keys, mouse (P14-04); selection across a text edit (cleared by design).

## Suggested commit message

`feat(winui): add a changed-line selection to LoomDiffEditor`

## Result

Build (0 warnings) and tests green; 21 tests in `DiffLineSelectionTests` (the card's list plus `ExtendTo_Backwards`, `ExtendTo_NoAnchor`, `SetBlocks_Keep_SameLines`, `SetBlocks_NotKeep_ClearsTheAnchor`, `IsChangedLine_EmptySidesAndBounds`). `Mutators_NoChange_ReturnFalse` was corrected: after `Set` (which clears the anchor) its `ExtendTo` acted as a toggle and changed the selection; it now sets an anchor with `Toggle` first. No UI yet (P14-04).
