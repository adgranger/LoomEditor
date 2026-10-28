# P10-08 — `renderWhitespace`: which characters of a line are rendered as whitespace (the logic)

- **Status:** done
- **Depends on:** P9-33
- **Size:** M

## Goal

A pure function answers, for one view line and one `renderWhitespace` mode (`none`, `boundary`, `selection`, `trailing`, `all`), which UTF-16 offsets of the line are whitespace characters that must be drawn as `·` or `→`. Nothing is drawn yet (P10-09).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewLayout/viewLineRenderer.ts` | 17–24 | `RenderWhitespace` (`None`, `Selection`, `Boundary`, `Trailing`, `All`) |
| `src/vs/editor/common/viewLayout/viewLineRenderer.ts` | 497–503 | the condition that decides whether `_applyRenderWhitespace` runs at all |
| `src/vs/editor/common/viewLayout/viewLineRenderer.ts` | 794–956 | `_applyRenderWhitespace`: **only** the per-character decision `isInWhitespace` (lines 829–881: leading / trailing whitespace, tab, space with the `boundary` rule, the `selection` filter, the `trailing` filter; `selectionsOnLine` is sorted by `Start` first, line 143) and the end-of-line rule of 927–940 (`continuesWithWrappedLine && onlyBoundary`, the single trailing space) |
| `src/vs/editor/browser/viewParts/viewLines/viewLine.ts` | 118–150 | how `selectionsOnLine` is built from the selections (ranges as `[startColumn - 1, endColumn - 1)`) |
| `src/vs/editor/common/core/misc/strings.ts` (search `firstNonWhitespaceIndex`, `lastNonWhitespaceIndex`) | — | the two helpers: use the existing `StringClassification.FirstNonWhitespaceIndex` / `LastNonWhitespaceIndex` of Core |
| `src/vs/editor/test/common/viewLayout/viewLineRenderer.test.ts` | the tests that pass `renderWhitespace: 'boundary'` / `'all'` (search `renderWhitespace:`, lines 215, 243, 389 and more) | adapt each: the expectation becomes a list of whitespace offsets instead of HTML |

## Files to create or change

- `src/LoomEditor.View/Layout/RenderWhitespace.cs` — create (the enum + `RenderWhitespaceComputer`, one file if under 250 lines)
- `tests/LoomEditor.View.Tests/Layout/RenderWhitespaceComputerTests.cs` — create

## Design decisions (already made — do not change)

- `internal enum RenderWhitespace { None, Selection, Boundary, Trailing, All }`.
- `internal static class RenderWhitespaceComputer` with
  `static IReadOnlyList<int> Compute(string content, int length, int fauxIndentLength, RenderWhitespace mode, bool continuesWithWrappedLine, IReadOnlyList<OffsetRange>? selectionsOnLine)` →
  the **sorted offsets** (`< length`) of every space or tab to draw. `length` is the number of characters that are rendered (Monaco's `len`: lines longer than the rendered length are cut by the renderer). Characters before `fauxIndentLength` (the wrapping indent: `ViewLineData.MinColumn - 1`) are never returned.
- The run condition of lines 497–503 is the first thing the method does (returns an empty list otherwise): `All`, `Boundary`, `Selection` **with** a non-null `selectionsOnLine`, `Trailing` **and** `!continuesWithWrappedLine`.
- **Do not port** what only exists to build DOM parts: `LinePart`s, `tokenType`, `generateLinePartForEachWhitespace`, `tmpIndent`, `useMonospaceOptimizations`, and the RTL rule of 871–881 (LoomEditor does not lay out RTL text specially). Keep the `isInWhitespace` logic and the order of the conditions **identical**, including the `wasInWhitespace` state that makes `boundary` render a run of spaces that starts at a boundary.
- A helper `static IReadOnlyList<OffsetRange>? SelectionsOnLine(IReadOnlyList<Selection> selections, int lineNumber, int minColumn, int maxColumn)` is the port of `viewLine.ts` 124–150 (non-empty selections that intersect the line; `null` when none). The `inline-selected-text` high contrast part is **not** ported.
- Full-width characters and `\t`: a tab is one offset (the renderer draws the arrow over the tab's width).
- `<remarks>Threading: pure, thread-safe.</remarks>`

## Tests

- [x] Port (names in `// Monaco:` comments): the `renderWhitespace` cases of `viewLineRenderer.test.ts`, as offsets: `boundary` leaves inner single spaces alone and renders leading, trailing and multiple-space runs; `all` renders every space and tab; `trailing` renders only trailing whitespace and a whitespace-only line; `selection` renders only inside the selections
- [x] New: `None_ReturnsEmpty`, `Selection_WithoutSelections_ReturnsEmpty`, `Trailing_ContinuesWithWrappedLine_ReturnsEmpty`, `Boundary_ContinuesWithWrappedLine_SingleTrailingSpace_NotRendered`, `Boundary_ContinuesWithWrappedLine_TwoTrailingSpaces_Rendered`, `FauxIndent_NeverRendered`, `Length_CutsTheLine`, `EmptyLine_ReturnsEmpty`, `WhitespaceOnlyLine_Trailing_AllRendered`, `SelectionsOnLine_MultiLineSelection_UsesMinAndMaxColumn`, `SelectionsOnLine_EmptySelection_Skipped`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `viewLineRenderer.ts` row (whitespace decision only) and the "Behavior differences" line (no RTL rule, no parts)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing (P10-09), the control property, the `experimentalWhitespaceRendering` option, RTL, the high contrast selection decoration.

## Suggested commit message

`feat(view): add the renderWhitespace computation`
