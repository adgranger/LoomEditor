# P8-18 — `SuggestListState`: selection and scrolling of the completion list, and the initial selection

- **Status:** todo
- **Depends on:** P8-17
- **Size:** S

## Goal

The completion list has a pure state object: which items it shows, which one is selected, which rows are visible (a window of at most 12 rows), how Up / Down / PageUp / PageDown / Home / End and the wheel move it, and which item is selected when the list opens (Monaco's "first of the best matches, or the preselected one").
The WinUI list (P8-22) draws this state; the controller (P8-19) drives it.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/suggest/browser/suggestMemory.ts` | 24–41 | `Memory.select` (the base implementation: no recency memory) |
| `src/vs/editor/contrib/suggest/browser/suggestWidget.ts` | the `_list` focus calls: search `focusNext`, `focusPrevious`, `focusNextPage`, `focusPreviousPage`, `focusFirst`, `focusLast`, `selectNextPageSuggestion` and `maxVisibleSuggestions` / `_applyStyles` row count (`const maxSuggestions = 12` style constant: search `rows`) | the movement semantics |
| `src/vs/base/browser/ui/list/listWidget.ts` | search `focusNextPage`, `focusPreviousPage`, `focusNext(n, loop)` | page and loop semantics (the rules are written out below: read the Monaco code to check them) |

## Files to create or change

- `src/LoomEditor.View/Suggest/SuggestListState.cs`, `SuggestSelector.cs` — create (`internal`)
- `tests/LoomEditor.View.Tests/Suggest/SuggestListStateTests.cs`, `SuggestSelectorTests.cs` — create

## Design decisions (already made — do not change)

- **`SuggestSelector`** (`internal static class`): `static int SelectInitialIndex(IReadOnlyList<SuggestItem> items)` — port of `Memory.select`: empty → 0; `topScore = items[0].Score[0]`; for `i` in order: stop (leave the loop) when `items[i].Score[0] != topScore`; return `i` when `items[i].Completion.Preselect`; after the loop return 0.
  (The recency / prefix memories of `suggestMemory.ts` are not ported: `editor.suggestSelection` is always `first` in LoomEditor; document in the README.)
- **`SuggestListState`** (`internal sealed class`, UI thread only; no UI types): `const int MaxVisibleRows = 12` (Monaco: the list shows at most 12 rows).
  State: `IReadOnlyList<SuggestItem> Items`, `int SelectedIndex` (`-1` when empty), `int FirstVisibleIndex`, `bool IsFrozen`, `int VisibleRowCount => Math.Min(Items.Count, MaxVisibleRows)`, `int LastVisibleIndex => FirstVisibleIndex + VisibleRowCount - 1`, `SuggestItem? SelectedItem`, `bool IsEmpty`.
  `event EventHandler? Changed` (raised once per mutating call that changed something) and `event EventHandler<SuggestItem?>? SelectionChanged` (raised when the selected **item** changed, including to `null`).
- `void Reset(IReadOnlyList<SuggestItem> items, int selectedIndex, bool isFrozen)`: stores the items; `selectedIndex` is clamped to `[0, Count - 1]` (`-1` when there are no items); `FirstVisibleIndex` is chosen by `Reveal`; raises both events as appropriate (a `Reset` that shows the same items with the same selection raises nothing).
- `void Clear()`: no items, `SelectedIndex = -1`, `FirstVisibleIndex = 0`, `IsFrozen = false`.
- **`Reveal(int index)`** (private, also used by every move): if `index < FirstVisibleIndex` → `FirstVisibleIndex = index`; else if `index > LastVisibleIndex` → `FirstVisibleIndex = index - VisibleRowCount + 1`; then clamp `FirstVisibleIndex` to `[0, Count - VisibleRowCount]`. (A freshly opened list with the selection at index 20 shows rows 9–20.)
- **Moves** (all return `true` when the selection changed; no-op and `false` when empty): `SelectNext()`: `SelectedIndex + 1`, **wrapping** to 0 after the last (Monaco `focusNext(1, true)`); `SelectPrevious()`: wraps to the last before the first; `SelectFirst()` and `SelectLast()` (no wrap);
  `SelectNextPage()`: if `SelectedIndex < LastVisibleIndex` → select `LastVisibleIndex`, else select `Math.Min(SelectedIndex + VisibleRowCount - 1, Count - 1)` and `Reveal` it (so the window slides by a page); `SelectPreviousPage()`: mirror image (`SelectedIndex > FirstVisibleIndex` → select `FirstVisibleIndex`, else `Math.Max(SelectedIndex - (VisibleRowCount - 1), 0)`). No wrap for pages.
- `bool ScrollBy(int rows)` (mouse wheel): moves `FirstVisibleIndex` by `rows` clamped to `[0, Count - VisibleRowCount]` **without** changing the selection; returns whether it moved. `bool Select(int index)` (a click or hover on a row): clamps to a valid index, `Reveal`s it, returns whether the selection changed.
- `IReadOnlyList<SuggestItem> VisibleItems` = the rows `[FirstVisibleIndex, LastVisibleIndex]`.
- `<remarks>Threading: UI thread only.</remarks>`; header `// Inspired by microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestWidget.ts (list focus)` on `SuggestListState`; `// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestMemory.ts (Memory.select)` on `SuggestSelector`.

## Tests

- [ ] Port (`SuggestSelectorTests`; the items come from `CreateSuggestItem` of P8-15's helper with scores set by hand: `item.Score = [score, 0]`): `Select_Empty_Zero`, `Select_NoPreselect_Zero`, `Select_PreselectInTopGroup_ItsIndex`, `Select_PreselectOutsideTopGroup_Zero`, `Select_FirstOfTopGroupPreselect_Zero` (`// Monaco: Memory.select`)
- [ ] New (`SuggestListStateTests`): `Reset_SelectsAndClamps`, `Reset_Empty_SelectionMinusOne`, `Reset_SelectionBeyondWindow_Reveals` (30 items, index 20 → first visible 9), `Reset_SameContent_RaisesNothing`, `SelectNext_Wraps`, `SelectPrevious_Wraps`, `SelectNext_BelowWindow_Scrolls` (selection at the last visible row: window moves by one),
      `SelectFirst_SelectLast`, `SelectNextPage_ToLastVisibleThenSlides` (30 items: 0 → 11 → 22 → 29), `SelectPreviousPage_ToFirstVisibleThenSlides`, `SelectNextPage_FewItems` (5 items: goes to 4), `ScrollBy_DoesNotChangeSelection`, `ScrollBy_Clamps`, `Select_ByIndex_RevealsAndRaises`, `Select_Invalid_Clamps`,
      `Clear_Resets`, `Changed_RaisedOncePerCall`, `SelectionChanged_RaisedOnlyWhenTheItemChanges`, `VisibleItems_MatchWindow`, `Moves_WhenEmpty_ReturnFalse`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `suggestMemory.ts` (`Memory.select` only), the list focus behavior of `suggestWidget.ts` (what is not ported: details pane, status bar, icons toggle, resizing)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The controller (P8-19), the WinUI list (P8-22), recency memory, the details side pane.

## Suggested commit message

`feat(view): add the completion list state and the initial selection`
