# P6-30 — `GuidesTextModelPart.GetLinesBracketGuides`

- **Status:** done
- **Depends on:** P6-29
- **Size:** S

## Goal

The text model computes the bracket pair guides of a range of lines (vertical guides, horizontal guides for the active
or every pair, the active pair class), from the bracket pair tree of phase 5.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/model/guidesTextModelPart.ts`.

| Lines | What to port |
|---|---|
| 274–459 | `getLinesBracketGuides` (keep the ASCII drawing comment of lines 320–345) |
| 461–469 | `getVisibleColumnFromPosition` |

## Files to create or change

- `src/LoomEditor.Core/Model/Guides/GuidesTextModelPart.Brackets.cs` — create (partial; same header)
- `tests/LoomEditor.Core.Tests/Model/Guides/GuidesTextModelPartBracketTests.cs` — create

## Design decisions (already made — do not change)

- `GetLinesBracketGuides(int startLineNumber, int endLineNumber, Position? activePosition, BracketGuideOptions options)` →
  `List<IndentGuide>[]` (one list per line).
- Bracket pairs: `TextModel.BracketPairs.GetBracketPairsInRangeWithMinIndentation(range)` and `GetBracketPairsInRange(range)`
  exist (P5-30/P5-31) and return `CallbackIterable<…>`; `.toArray()` → the existing helper that materializes a
  `CallbackIterable` (look in `CallbackIterable` for `ToArray` / `ToList`).
- `findLast(arr, pred)` → a backwards `for` loop (no LINQ allocation in the hot path is required, but LINQ `LastOrDefault` is
  acceptable).
- `Range.strictContainsPosition` → the existing `Range.StrictContainsPosition` (add it to `Core/Text/Range.cs` only if missing,
  ported from `range.ts`, with a test).
- `independentColorPoolPerBracketType` → `TextModel.BracketPairColorizationOptions.IndependentColorPoolPerBracketType`.
- `CursorColumns.visibleColumnFromColumn` exists in Core (`CursorColumns.VisibleColumnFromColumn`).
- `guides.sort((a, b) => a.visibleColumn - b.visibleColumn)`: JavaScript's sort is stable; use a **stable** sort
  (`List<T>` + `OrderBy(...).ToList()` or an insertion sort), not `List.Sort`.

## Tests

No upstream unit tests (Monaco tests this through the browser). New, with the TypeScript test language of P5 (brackets `{}`, `()`):

- [x] `GetLinesBracketGuides_MultiLinePair_HasVerticalGuidesInside`
- [x] `GetLinesBracketGuides_IncludeInactiveFalse_OnlyTheActivePair`
- [x] `GetLinesBracketGuides_HorizontalEnabledForActive_AddsHorizontalLinesForTheActivePairOnly`
- [x] `GetLinesBracketGuides_TextBeforeClosingBracket_RendersTheEndLineAtTheBottom`
- [x] `GetLinesBracketGuides_SingleLinePair_OnlyAHorizontalGuide`
- [x] `GetLinesBracketGuides_ActiveClassName_WhenHighlightActive`
- [x] `GetLinesBracketGuides_ActivePositionOutsideTheRange_QueriesItsPairs`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `guidesTextModelPart.ts` row ✅; the `Core/Model/TextModel*` row of "Cut points for phase 6" is resolved
      except the view part (keep it until P6-32)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The view model (P6-31, P6-32), drawing (P6-34).

## Suggested commit message

`feat(core): compute the bracket pair guides`
