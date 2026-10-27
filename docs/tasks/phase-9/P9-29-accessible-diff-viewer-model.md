# P9-29 — Accessible diff viewer: the model

- **Status:** todo
- **Depends on:** P9-26
- **Size:** M

## Goal

The pure model of Monaco's accessible diff viewer exists: the diff is cut into groups of changes with 3 lines of context, each group is a list of rows (header, unchanged, deleted, added), a cursor moves through groups and rows with wrap-around, and every row has the exact screen reader label Monaco uses.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/components/accessibleDiffViewer.ts` | 245–292 | `computeViewElementGroups` (group by modified distance `< 2 * 3`, the full ranges with the 3 line margin, the unchanged / deleted / added rows between and around changes) |
| same | 294–339 | `LineType`, `ViewElementGroup`, `HeaderViewElement`, `DeletedLineViewElement`, `AddedLineViewElement`, `UnchangedLineViewElement` |
| same | 115–243 | `ViewModel`: group and element indexes, `nextGroup` / `previousGroup` (cyclic, only when more than one group), `goToNextLine` / `goToPreviousLine` (clipped to the group, only when more than one row), `goToLine`, the initial group (the first group that ends after the modified cursor line), `revealCurrentElementInEditor`, `close` (**without** the audio cues and the `modifiedSetSelection` side effect, which move to the UI card) |
| same | 42–62 | `IAccessibleDiffViewerModel` (what the viewer needs from the editors: reveal / focus / selection callbacks; ported as `IAccessibleDiffViewerHost`) |
| same | 63–114 | `AccessibleDiffViewer.next` / `prev` / `close` semantics: `next` opens the viewer and, if it was already open, goes to the next group; `prev` opens and goes to the previous group |
| same | 428–470 (`header` branch), 541–600 (`_createRow` aria labels) | the label strings: header `"Difference {0} of {1}: original line {2}, {3}, modified line {4}, {5}"` with `"no lines changed"` / `"1 line changed"` / `"{n} lines changed"`; the header text `"{i}/{n}: @@ -{origStart},{origLen} +{modStart},{modLen} @@"`; rows `"{content} unchanged line {n}"` (same line number on both sides) / `"{content} original line {o} modified line {m}"`, `"+ {content} modified line {m}"`, `"- {content} original line {o}"`, empty content → `"blank"`; the list label `"Accessible Diff Viewer. Use arrow up and down to navigate."` |
| `src/vs/base/common/arrays.ts` | `groupAdjacentBy` (search the name; it is in `arrays.ts`), `forEachAdjacent` (193–198) | the two helpers |
| `src/vs/editor/common/core/ranges/offsetRange.ts` | `ofLength`, `clip`, `clipCyclic` | exist after P9-02 (add if missing) |

## Files to create or change

- `src/LoomEditor.View/DiffEditor/AccessibleDiffViewerModel.cs`, `ViewElementGroup.cs` (the element types), `IAccessibleDiffViewerHost.cs` — create
- `src/LoomEditor.View/DiffEditor/ArrayHelpers.cs` (`GroupAdjacentBy`, `ForEachAdjacent`) — create (internal static; if a similar helper exists, reuse it)
- `src/LoomEditor.View/Accessibility/AccessibilityMessages.cs` — change: the strings above as methods (`DiffHeaderLabel(...)`, `DiffRowLabel(...)`, `DiffViewerListLabel`) — **English only**, like the existing messages
- `tests/LoomEditor.View.Tests/DiffEditor/AccessibleDiffViewerModelTests.cs` — create

## Design decisions (already made — do not change)

- `ViewElement` is a small class hierarchy (`abstract record ViewElement(LineType Type)`; `HeaderViewElement`, `UnchangedLineViewElement(int OriginalLineNumber, int ModifiedLineNumber)`, `DeletedLineViewElement(DetailedLineRangeMapping Diff, int OriginalLineNumber)`, `AddedLineViewElement(DetailedLineRangeMapping Diff, int ModifiedLineNumber)`); `ViewElementGroup(LineRangeMapping Range, IReadOnlyList<ViewElement> Lines)`. **Deliberate difference:** Monaco builds the group's `range` as `new LineRangeMapping(modifiedRange, originalRange)` (line 291) while the constructor takes `(original, modified)` and the header reads `r.original` as the original side: so Monaco's header announces the modified numbers as "original line"; it is a Monaco bug. Build it the right way round (`new LineRangeMapping(originalRange, modifiedRange)`), and record the difference in `PORTING.md` "Behavior differences". The tests pin the corrected labels.
- `AccessibleDiffViewerModel(IAccessibleDiffViewerHost host, bool canClose)`: `IReadOnlyList<ViewElementGroup> Groups`, `int CurrentGroupIndex`, `ViewElementGroup? CurrentGroup`, `ViewElement? CurrentElement`, `int CurrentElementIndex`; `void SetDiffs(IReadOnlyList<DetailedLineRangeMapping>? diffs)` (null → no groups; recomputes the groups from `host.OriginalLineCount` / `ModifiedLineCount`, picks the initial group from `host.ModifiedCursorLine`); `NextGroup()`, `PreviousGroup()` (reset the element index to 0), `GoToNextLine()`, `GoToPreviousLine()`, `GoToLine(ViewElement)`, `RevealCurrentElementInEditor()`, `Close()`; `event EventHandler? Changed` (any of the above changed the state).
- `IAccessibleDiffViewerHost`: `int OriginalLineCount`, `int ModifiedLineCount`, `int? ModifiedCursorLine`, `string GetOriginalLine(int)`, `string GetModifiedLine(int)`, `void OriginalReveal(Range)`, `void ModifiedReveal(Range?)`, `void ModifiedSetSelection(Range)`, `void ModifiedFocus()`, `void SetViewerVisible(bool)`.
- `RevealCurrentElementInEditor` / `Close` do nothing when `canClose` is false; Monaco's two `_setVisible(false)` calls become `host.SetViewerVisible(false)`.
- Selection side effect: Monaco selects the current element's modified line in the modified editor on every element change (`autorun` "select lines in editor", so that editor commands keep working): the model calls `host.ModifiedSetSelection(Range.FromPositions(new Position(line, 1)))` for every non-header current element with `line = element.ModifiedLineNumber ?? diff.Modified.StartLineNumber`.
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [ ] New (diffs built by hand, 20-line files): `Groups_TwoFarApartChanges_TwoGroups`, `Groups_TwoCloseChanges_OneGroup` (gap `< 6`), `Groups_ContextLinesLimitedToFileBounds`, `Group_Rows_HeaderThenUnchangedDeletedAddedInOrder`, `Group_Rows_UnchangedBetweenTwoChangesInGroup`, `NextGroup_Wraps`, `PreviousGroup_Wraps`, `NextGroup_SingleGroup_Stays`, `GoToNextLine_ClipsAtEnd`, `GoToPreviousLine_ClipsAtStart`, `InitialGroup_FirstGroupEndingAfterCursor`, `InitialGroup_CursorAfterAll_StaysFirst`, `CurrentElementChange_SelectsModifiedLine`, `CurrentElementChange_Deleted_SelectsDiffModifiedStart`, `CurrentElementChange_Header_NoSelection`, `Reveal_Deleted_CallsOriginalReveal`, `Reveal_Added_CallsModifiedReveal`, `Reveal_Header_ModifiedRevealWithNull`, `Close_CanCloseFalse_DoesNothing`, `Labels_Header_OriginalAndModifiedCounts` (the three plural forms), `Labels_HeaderText_Format` (`1/2: @@ -3,2 +3,4 @@`), `Labels_Rows_UnchangedSameNumber`, `Labels_Rows_UnchangedDifferentNumbers`, `Labels_Rows_AddedDeleted`, `Labels_Rows_BlankLine`, `Changed_RaisedOnNavigation`.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `accessibleDiffViewer.ts` (model and labels; UI in P9-30)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The UI (P9-30), the audio cues (not ported), localization.

## Suggested commit message

`feat(view): add the accessible diff viewer model`
