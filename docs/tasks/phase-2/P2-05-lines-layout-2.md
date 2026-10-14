# P2-05 — `LinesLayout` (2/2): viewport data

- **Status:** done
- **Depends on:** P2-04
- **Size:** M

## Goal

Finish `LinesLayout`: given the top and bottom of the viewport (in DIPs), find the visible lines, their offsets
relative to the viewport, the centered line, and the whitespaces in view. The renderer draws from this
(`IPartialViewLinesViewportData`) on every frame.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewLayout/linesLayout.ts` | 606–729 | `getLinesViewportData` (including `bigNumbersDelta`) |
| same | 795–865 | `getWhitespaceAtVerticalOffset`, `getWhitespaceViewportData` |
| `src/vs/editor/common/viewModel.ts` | 179–220 | `IPartialViewLinesViewportData`, `IViewWhitespaceViewportData` |
| same | 222–236 | `Viewport` |
| `src/vs/editor/test/common/viewLayout/linesLayout.test.ts` | 384–712 | `LinesLayout getCenteredLineInViewport`, `LinesLayout getLinesViewportData 1`, `LinesLayout getLinesViewportData 2 & getWhitespaceViewportData`, `LinesLayout getWhitespaceAtVerticalOffset` |

## Files to create or change

- `src/LoomEditor.View/Layout/LinesLayout.Viewport.cs` — create (`partial` of `LinesLayout`)
- `src/LoomEditor.View/Layout/PartialViewLinesViewportData.cs`, `ViewWhitespaceViewportData.cs`, `Viewport.cs` — create
- `tests/LoomEditor.View.Tests/Layout/LinesLayoutViewportTests.cs` — create

## Design decisions (already made — do not change)

- `PartialViewLinesViewportData` is an `internal sealed record` with `int[] RelativeVerticalOffset` (Monaco's
  `number[]`); `ViewWhitespaceViewportData` and `Viewport` are `internal readonly record struct`s with `int` fields.
- Keep `bigNumbersDelta` and the `STEP_SIZE = 500000` logic exactly as in Monaco, even though it was written for an IE
  limit. The renderer (P2-14) uses `relativeVerticalOffset`, which stays small. That is exactly what float-based
  drawing needs (ADR 0005).
- Keep the loop structure and variable names of `getLinesViewportData`. It is the hottest function of scrolling: do not
  "simplify" it.

## Tests

- [x] Port: `LinesLayout getCenteredLineInViewport`, `LinesLayout getLinesViewportData 1`,
      `LinesLayout getLinesViewportData 2 & getWhitespaceViewportData`, `LinesLayout getWhitespaceAtVerticalOffset`
      (`linesLayout.test.ts` is now 12 / 12)
- [x] New: on a 2,500,000-line layout, `GetLinesViewportData(47_000_000, 47_000_800)` returns a non-zero
      `BigNumbersDelta` and relative offsets below 1,000,000

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `linesLayout.ts` ✅ ported; `linesLayout.test.ts` 12 / 12
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`ViewLayout` (P2-08), anything that draws.

## Suggested commit message

`feat(view): port LinesLayout viewport data`
