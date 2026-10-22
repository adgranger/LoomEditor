# P6-47 — `MinimapLayout`: rendered line range and slider position

- **Status:** done
- **Depends on:** P6-46
- **Size:** S

## Goal

For a scroll position and a viewport, the minimap knows which lines it renders, where its slider is and how big it is,
and how a slider drag or a click converts back into a scroll position — Monaco's `MinimapLayout`, with its previous-layout
stabilization.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/viewParts/minimap/minimap.ts`.

| Lines | What to port |
|---|---|
| 214–278 | `class MinimapLayout` (fields, `getDesiredScrollTopFromDelta`, `getDesiredScrollTopFromTouchLocation`, `intersectWithViewport`, `getYForLineNumber`) |
| 280–414 | `MinimapLayout.create` (the `minimapHeightIsEditorHeight` branch and the proportional branch with `previousLayout`, `consideringStartLineNumber`, the slider computations) |

## Files to create or change

- `src/LoomEditor.View/Minimap/MinimapLayout.cs` — create (header `minimap.ts (MinimapLayout)`)
- `tests/LoomEditor.View.Tests/Minimap/MinimapLayoutTests.cs` — create

## Design decisions (already made — do not change)

- `MinimapLayout.Create(MinimapOptions options, int viewportStartLineNumber, int viewportEndLineNumber, double viewportStartLineNumberVerticalOffset, int viewportHeight, bool viewportContainsWhitespaceGaps, int lineCount, int realLineCount, double scrollTop, double scrollHeight, MinimapLayout? previousLayout)`
  — copy the parameter list from line 280 onward and keep every name; types: line numbers `int`, pixel values `double` (Monaco
  numbers), with `Math.Floor`/`Math.Round` where Monaco has them (`Math.round` → `MidpointRounding.AwayFromZero`).
- `intersectWithViewport` returns `(int Start, int End)?`.
- `getYForLineNumber` keeps Monaco's unary `+` as a plain expression.

## Tests

No upstream unit tests. New, with a `MinimapOptions` built by a test helper (P6-46 constructor, fake layout values):

- [x] `Create_EditorHeightMode_SliderCoversTheViewportRatio`
- [x] `Create_Proportional_DocumentShorterThanMinimap_NoSampling_SliderAtScrollTop`
- [x] `Create_Proportional_LongDocument_StartLineFollowsTheScroll`
- [x] `Create_WithPreviousLayout_KeepsTheStartLineWhenPossible` (the stabilization branch)
- [x] `GetDesiredScrollTopFromDelta_InvertsTheSliderRatio`
- [x] `IntersectWithViewport_OutsideReturnsNull`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `minimap.ts` row (+ `MinimapLayout`)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Sampling (P6-48), drawing (P6-52), mouse (P6-53).

## Suggested commit message

`feat(view): compute the minimap layout and slider`
