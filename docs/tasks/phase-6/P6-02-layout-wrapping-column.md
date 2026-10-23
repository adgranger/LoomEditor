# P6-02 — Layout: `IsViewportWrapping` and `WrappingColumn`

- **Status:** done
- **Depends on:** P6-01
- **Size:** S

## Goal

`EditorLayoutInfoComputer.ComputeLayout` computes Monaco's `isViewportWrapping` and `wrappingColumn` from the word-wrap
options and the size of the control, and `EditorLayoutInfo` carries them. Nothing consumes them yet (P6-20).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/config/editorOptions.ts`.

| Lines | What to port |
|---|---|
| 2932–2937 | how `computeLayout` reads `wordWrap`, `wordWrapColumn`, `wordWrapIndicator` (skip the two overrides: they are always `inherit`) |
| 2979–2992 | the `isViewportWrapping` / `wrappingColumn` decision (skip the accessibility branch and `isWordWrapMinified`) |
| 3019–3031 | the actual `wrappingColumn` for viewport wrapping (`Math.max(1, viewportColumn - (wordWrapIndicator ? 1 : 0))`, `bounded` takes the minimum with `wordWrapColumn`) |
| 5670–5698 | `EditorWrappingInfo`, `EditorWrappingInfoComputer` (informative: LoomEditor reads the two fields from `EditorLayoutInfo` directly) |

## Files to create or change

- `src/LoomEditor.View/Layout/EditorLayoutOptions.cs` — change: add `public EditorWrappingOptions Wrapping { get; init; } = new();`
- `src/LoomEditor.View/Layout/EditorLayoutInfo.cs` — change: add `bool IsViewportWrapping` and `int WrappingColumn`
  after `ViewportColumn` (update every `new EditorLayoutInfo(…)` / pattern in the code base: build errors tell where)
- `src/LoomEditor.View/Layout/EditorLayoutInfoComputer.cs` — change
- `tests/LoomEditor.View.Tests/Layout/EditorLayoutInfoComputerTests.cs` — change (add tests; fix the expected records of the
  existing tests with `IsViewportWrapping: false, WrappingColumn: -1`)

## Design decisions (already made — do not change)

- `WordWrap.On` and `WordWrap.Bounded` → `IsViewportWrapping = true`; `WordWrap.WordWrapColumn` → `WrappingColumn =
  WordWrapColumn`, `IsViewportWrapping = false`; `Off` → `WrappingColumn = -1`.
- When `IsViewportWrapping`, `WrappingColumn = Math.Max(1, ViewportColumn - (WordWrapIndicator ? 1 : 0))`, and for
  `Bounded` `Math.Min(WrappingColumn, WordWrapColumn)`.
- `isWordWrapMinified` and `isDominatedByLongLines` are **not** ported (accessibility support, phase 7): update the
  `<remarks>` of `EditorLayoutInfo` and `EditorLayoutInfoComputer` (they now list only the minimap as missing).
- The minimap (P6-41) will subtract its width from `ContentWidth` before `ViewportColumn` is computed; the code stays in the
  order Monaco has (minimap block between the two computations) so that card only has to fill it in. Leave a comment
  `// Minimap: P6-41` where Monaco's `_computeMinimapLayout` call is.
- The two Monaco word-wrap layout tests (`issue #31312`, `leaves the rightmost column…`) use the minimap: they are ported in the
  minimap layout card (P6-41), not here.

## Tests

- [x] New: `ComputeLayout_WordWrapOff_HasNoWrappingColumn` (`IsViewportWrapping` false, `WrappingColumn` -1)
- [x] New: `ComputeLayout_WordWrapColumn_UsesTheColumn`
- [x] New: `ComputeLayout_WordWrapOn_WrapsAtTheViewportColumn` (check `WrappingColumn == ViewportColumn`)
- [x] New: `ComputeLayout_WordWrapOnWithIndicator_LeavesTheRightmostColumn`
- [x] New: `ComputeLayout_Bounded_UsesTheMinimumOfViewportAndColumn` (both orders)
- [x] New: `ComputeLayout_WordWrapOn_NeverBelowOne` (tiny `OuterWidth`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map row of `editorOptions.ts` mentions the wrapping fields
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The minimap layout (P6-41), feeding the wrapping column to the view lines and the wrapped content width in `ViewLayout`
(P6-20), the control's property (P6-26).

## Suggested commit message

`feat(view): compute the wrapping column in the editor layout`
