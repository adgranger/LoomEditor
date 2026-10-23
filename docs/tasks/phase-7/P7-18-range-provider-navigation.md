# P7-18 — The range provider: navigation, find, selection and scrolling

- **Status:** done
- **Depends on:** P7-17
- **Size:** M

## Goal

`ITextRangeProvider` is complete: Narrator can expand a range to a word or line, move by units, find text, select a range (which moves the caret) and scroll a range into view.

## Sources

None to port: the members delegate to `AutomationTextRange` (P7-09, P7-10) and to the view model.
View model members used: `ViewModel.SetSelections(string? source, IReadOnlyList<Selection> selections, CursorChangeReason reason = NotSet)`, `ViewModel.GetSelections()`,
`ViewModel.RevealRange(string? source, bool revealHorizontal, Range viewRange, VerticalRevealType verticalType, ScrollType scrollType)` (`src/LoomEditor.View/Layout/ViewModel.Cursors.cs`),
`CoordinatesConverter.ConvertModelRangeToViewRange`.

## Files to create or change

- `src/LoomEditor.WinUI/Accessibility/LoomEditorTextRangeProvider.cs` — change (or add a `LoomEditorTextRangeProvider.Navigation.cs` `partial` file if it passes ~400 lines): the nine members that threw `NotImplementedException`
- `src/LoomEditor.View/Accessibility/AutomationTextSelection.cs` — create (pure helpers for the selection members, so that they are tested: see decisions)
- `tests/LoomEditor.View.Tests/Accessibility/AutomationTextSelectionTests.cs` — create
- `docs/testing/phase-7-manual-tests.md` — change: section 4 below

## Design decisions (already made — do not change)

- Mappings: `TextUnit` → `AutomationTextUnit` and `TextPatternRangeEndpoint` → `AutomationTextEndpoint` by `(AutomationTextUnit)(int)unit` (same integers, README §4 / P7-05); an undefined value throws `ArgumentOutOfRangeException` from the View model.
- `ExpandToEnclosingUnit(unit)` → `_range.ExpandToEnclosingUnit(unit)`. `Move(unit, count)` → `_range.Move(...)`. `MoveEndpointByUnit(endpoint, unit, count)` → `_range.MoveEndpointByUnit(...)`.
  `MoveEndpointByRange(endpoint, other, otherEndpoint)` → `other` must be a `LoomEditorTextRangeProvider`, else `ArgumentException`; delegates to `_range.MoveEndpointByRange(...)`.
  `FindText(text, backward, ignoreCase)` → `_range.FindText(...)` wrapped in a new provider, or `null`.
- The three selection members use the pure helpers of `AutomationTextSelection` (`internal static class`, takes and returns `IReadOnlyList<Selection>`; the control applies the result with `viewModel.SetSelections("automation", result)`):
  - `Select(IReadOnlyList<Selection> current, Range range)` → `[new Selection(range.StartLineNumber, range.StartColumn, range.EndLineNumber, range.EndColumn)]` (anchor at the start, position at the end);
  - `Add(IReadOnlyList<Selection> current, Range range)` → `current` followed by the new selection (the cursor model merges overlapping ones);
  - `Remove(IReadOnlyList<Selection> current, Range range)` → if no selection of `current` has exactly `range` as its range, `current` unchanged; else `current` without that selection (the **first** equal one); if that would leave none, the single selection collapsed
    to its own position: `[Selection.Collapsed(selection.Position)]`.
- `Select()`, `AddToSelection()`, `RemoveFromSelection()` never move the keyboard focus (UIA `Select` on a text range must not steal it); they only call `SetSelections`.
- `ScrollIntoView(bool alignToTop)`: `viewRange = CoordinatesConverter.ConvertModelRangeToViewRange(_range.Range)`; `viewModel.RevealRange("automation", true, viewRange, alignToTop ? VerticalRevealType.Top : VerticalRevealType.Bottom, ScrollType.Immediate)`.
  (`ViewModel.RevealRange` is the one the find and word highlighter features use; keep their `source` string style.)
- All members keep the `Guard.Assert(_owner.DispatcherQueue.HasThreadAccess)` line of P7-17. Delete the `NotImplementedException` stubs and their comments.

## Tests

- [x] New (`AutomationTextSelectionTests`): `Select_ReplacesEverything`, `Select_UsesTheStartAsAnchor` (the selection's start is `range.Start`, its position `range.End`), `Add_AppendsTheRange`, `Remove_AnEqualRange_RemovesIt`, `Remove_NoEqualRange_ReturnsTheSameSelections`,
      `Remove_TheOnlySelection_CollapsesIt` (to the `Position` of the selection), `Remove_FirstOfTwoEqual_RemovesOnlyOne`, `Remove_ASelectionWithTheOppositeDirection_StillMatches` (selection stored right-to-left, same range)
- [x] New: `Add_ThroughTheViewModel_MergesOverlappingSelections` (apply `Add` with `viewModel.SetSelections` in a `TestCodeEditor`: two overlapping ranges end as one selection — documents the merge the decision relies on)
- [x] Manual, section 4 "UIA text pattern (navigate)" (Accessibility Insights text pattern explorer, or the Gallery self-check of P7-24 once it exists):
      4.1 degenerate range at a word → `ExpandToEnclosingUnit(Word)` → `GetText` = the word and its trailing spaces; 4.2 `Move(Word, 1)` / `Move(Word, -1)` walk the words; 4.3 `Move(Line, 1)` on a wrapped line (word wrap on) → the next **view** line;
      4.4 `Move(Paragraph, 1)` → the next model line; 4.5 `FindText("foo", false, true)` returns the range (`Select` it: the selection appears); 4.6 `Select` on a range moves the caret and the editor does not lose the focus;
      4.7 `AddToSelection` adds a second cursor; `RemoveFromSelection` on it removes it; 4.8 `ScrollIntoView(true)` on a range far below → it becomes the first visible line; 4.9 `MoveEndpointByUnit(End, Word, 1)` grows a selection by a word

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] No `NotImplementedException` left in `src/LoomEditor.WinUI/Accessibility/` (`grep -rn NotImplementedException src/LoomEditor.WinUI/Accessibility` is empty)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Events (P7-19), the self-check (P7-24), per-token attributes.

## Suggested commit message

`feat(winui): implement the range navigation, find, selection and scrolling of the UIA text range`
