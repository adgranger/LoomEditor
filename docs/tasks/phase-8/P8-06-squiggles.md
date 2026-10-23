# P8-06 — Squiggles: error, warning, info and hint underlines, and the ruler colors of markers

- **Status:** done
- **Depends on:** P8-05
- **Size:** M

## Goal

A marker (P8-03) shows in the text as Monaco does: a zigzag underline in the severity color (error red, warning yellow, info blue) and three dots for a hint, and a mark of the same color in the overview ruler.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/codeEditor/codeEditorWidget.ts` | 2524–2560 | the squiggle rules: `getSquigglySVGData` (6 × 3 zigzag), `squiggly-error` / `-warning` / `-info` use `editorError.foreground` / `editorWarning.foreground` / `editorInfo.foreground`, `squiggly-hint` uses `editorHint.foreground` (three dots) |
| `src/vs/editor/browser/widget/codeEditor/editor.css` | 81–119 | which class names exist (`squiggly-*`); the `border-bottom` / `::before` rules are **not** ported (see decisions) |
| `src/vs/editor/common/core/editorColorRegistry.ts` | 75–77 | `editorOverviewRuler.errorForeground`, `.warningForeground`, `.infoForeground` |
| `src/vs/platform/theme/common/colors/editorColors.ts` | 105–112 | `editorHint.foreground`, `editorHint.border` |

## Files to create or change

- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: the five new ids below (one line each with the upstream line in a comment, like the neighbours)
- `src/LoomEditor.View/Layout/DecorationStyle.cs` — change: `UnderlineStyle Underline = UnderlineStyle.None`, `string? UnderlineColorId = null`; new `enum UnderlineStyle { None, Squiggle, Dots }` in the same file
- `src/LoomEditor.View/Layout/DecorationStyles.cs` — change: the four rows; `ColorIds` also returns `UnderlineColorId`
- `src/LoomEditor.Rendering/UnderlineGeometry.cs` — create (`internal static`, pure)
- `src/LoomEditor.Rendering/TextViewRenderer.Decorations.cs` — change: `DrawDecorationBox` draws the underline
- `tests/LoomEditor.Rendering.Tests/UnderlineGeometryTests.cs` — create; `tests/LoomEditor.Rendering.Tests/TextViewRendererDecorationsTests.cs` — change
- `tests/LoomEditor.View.Tests/Layout/DecorationStylesTests.cs` — change (or create if absent: `grep -rn "DecorationStyles" tests/LoomEditor.View.Tests | head`)
- `docs/testing/phase-8-manual-tests.md` — change: section 1 below

## Design decisions (already made — do not change)

- **Theme colors** (`EditorColorDefaults`, values from the upstream lines): `editorHint.foreground` = dark `ColorValue.Transparent("#eeeeee", 0.7)`, light `"#6c6c6c"`, hcDark `null`, hcLight `null`; `editorHint.border` = null, null, `ColorValue.Transparent("#eeeeee", 0.8)`, `"#292929"`;
  `editorOverviewRuler.errorForeground` = dark and light `ColorValue.Transparent("#FF1212", 0.7)`, hcDark `"#FF3232"`, hcLight `"#B5200D"`; `editorOverviewRuler.warningForeground` = `"editorWarning.foreground"` (dark, light), `"editorWarning.border"` (hcDark, hcLight);
  `editorOverviewRuler.infoForeground` = `"editorInfo.foreground"` (dark, light), `"editorInfo.border"` (hcDark, hcLight). (`ColorValue` implicitly converts a string: a `#hex` or the id of another color; look at the neighbouring rows for the exact form.)
- `DecorationStyle` rows (`DecorationStyles.Table`, comment `// codeEditorWidget.ts 2524–2560`): `squiggly-error` → `new(Underline: UnderlineStyle.Squiggle, UnderlineColorId: "editorError.foreground")`, `squiggly-warning` → `editorWarning.foreground`, `squiggly-info` → `editorInfo.foreground`,
  `squiggly-hint` → `new(Underline: UnderlineStyle.Dots, UnderlineColorId: "editorHint.foreground")`. **No row** for `squiggly-unnecessary`, `squiggly-inline-unnecessary`, `squiggly-inline-deprecated` (Monaco fades or strikes the text; phase 8 draws nothing: behavior difference, listed in the card's PORTING entry).
  Monaco's `border-bottom: 4px double editorError.border` and the `::before` background (`editorError.background`) are not ported: both colors are `null` for the dark and light themes (they only exist in high contrast): behavior difference.
- **`UnderlineGeometry`** (pure, `internal static class`, no drawing API):
  - `IReadOnlyList<(float X, float Y)> SquigglePoints(float left, float width, float bottom)` — the polyline of a squiggle that starts at `left`, is `width` DIPs wide and sits on `bottom` (the bottom of the line box). Zigzag of period 6 DIPs; trough centre at `bottom - 0.7`, peak centre at `bottom - 2.3`
    (Monaco's 3-DIP-high SVG, whose stripe is 1.4 high, aligned bottom left); the trough is at `t = 1.8 + 6k` and the peak at `t = 4.8 + 6k` (`t = x - left`). The function is `Y(t)`: `p = ((t - 1.8) mod 6 + 6) mod 6`; `v = p <= 3 ? p / 3 : (6 - p) / 3` (0 at a trough, 1 at a peak); `y = (bottom - 0.7) + ((bottom - 2.3) - (bottom - 0.7)) * v`.
    Points: `(left, Y(0))`, then `(left + 1.8 + 3k, Y(1.8 + 3k))` for every `k >= 0` while `1.8 + 3k < width`, then `(left + width, Y(width))`. `width <= 0` → an empty list.
  - `IReadOnlyList<(float X, float Y, float Size)> DotPositions(float left, float bottom)` — the three dots of a hint: `for i in 0..2: (left + 3 * i, bottom - 2.5f, 1.5f)` (a dot is a square of `Size` DIPs whose top left is `(X, Y)`).
- **`DrawDecorationBox`** (existing method): after the background and the border, if `style.Underline != None` and `GetDecorationColor(style.UnderlineColorId)` is a color: `Squiggle` → draw `DrawLine` for each consecutive pair of `SquigglePoints(left, width, top + height)` with stroke width `1.2f`;
  `Dots` → `FillRectangle(x, y, size, size, color)` for each of `DotPositions(left, top + height)` (a hint range is two columns wide, so the dots are not clipped; they are **not** clipped to `width`). No new `IRenderSurface` member.
- Squiggles are `ClassName` decorations (not `InlineClassName`): they go through `DrawNormalDecorations` already (ordered by `ZIndex`, so an error draws above a warning, as in Monaco).
- `squiggly-*` are the `ClassName` of **validation** decorations: `DecorationsTrees.GetAll(…, filterOutValidation, …)` already knows the three class names? Check `grep -rn "filterOutValidation\|IsForValidation\|squiggly" src/LoomEditor.Core src/LoomEditor.View | head`. If the model code does **not** treat `squiggly-error` / `-warning` / `-info` as validation decorations, add them to that predicate (Monaco `intervalTree.ts` 205–214) and say so in the card's PORTING entry; if it already does, do nothing.

## Tests

- [x] New (`UnderlineGeometryTests`; comment `// Monaco: getSquigglySVGData (6 x 3 zigzag)`): `SquigglePoints_ZeroWidth_Empty`, `SquigglePoints_StartsAtLeftAndEndsAtRight`, `SquigglePoints_StaysBetweenPeakAndTrough` (every `Y` in `[bottom - 2.3, bottom - 0.7]`), `SquigglePoints_TroughAndPeak` (width 12: a vertex at `x = left + 1.8` with `y = bottom - 0.7`, one at `left + 4.8` with `bottom - 2.3`, one at `left + 7.8` with `bottom - 0.7`),
      `SquigglePoints_StartHeight` (`Y(0)`: `p = 4.2`, `v = 0.6` → `bottom - 0.7 - 1.6 * 0.6`), `DotPositions_ThreeDots`
- [x] New (`DecorationStylesTests`): `SquigglyClasses_HaveUnderlineRows` (error / warning / info Squiggle with the three ids, hint Dots), `UnnecessaryClasses_HaveNoRow`, `ColorIds_IncludeUnderlineColors`
- [x] New (`TextViewRendererDecorationsTests`, the file's helpers `CreateHarness`, `AddDecoration`, `RecordingRenderSurface`; add the five color ids to its `ThemeJson` only if a test needs them): `SquigglyError_DrawsZigzagLines` (a `squiggly-error` decoration on columns 2–5: `DrawLineCall`s with the `editorError.foreground` color, first point x = the left of column 2, all y within the bottom 3 DIPs of the line),
      `SquigglyHint_DrawsThreeDots` (three `FillRectangleCall`s of the hint color), `Squiggly_Warning_DrawnAboveError_WhenHigherZIndex` (z-index order of the calls), `Squiggly_EmptyDecoration_ShowIfCollapsed_DrawsSomething` (a marker decoration has `ShowIfCollapsed = true`: an empty range still draws a squiggle of width `TypicalHalfwidthCharacterWidth`)
- [x] Existing `DecorationStyles_EveryColorId_IsAKnownThemeColor` still passes (the new ids are known)
- [x] An end-to-end check that markers reach the renderer: `TextViewRendererDecorationsTests.MarkerDecorations_DrawASquiggle` — create a `MarkerService`, a `MarkerDecorations(harness.Model, service, harness.ViewModel.EditorId)`, `service.Change("t", [new MarkerData(MarkerSeverity.Error, "x", 1, 1, 1, 4)])`, render, assert a squiggle was drawn

## Manual checks (add to `docs/testing/phase-8-manual-tests.md` as "1. Squiggles and ruler")

| # | Steps | Expected |
|---|---|---|
| 1.1 | Gallery with a marker set from code (the Gallery's **Demo markers** button of P8-37, or the Setup line of the file) on `Console`, severities Error / Warning / Info / Hint on four words | Red zigzag, yellow zigzag, blue zigzag, three dots; the zigzag follows the text when you type before it |
| 1.2 | Look at the overview ruler | A red / yellow / blue mark at the lines of the markers |
| 1.3 | Switch the theme to a light one | The colors change with the theme |

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: the squiggle rows of `codeEditorWidget.ts` and `editor.css` (with the behavior differences: no `unnecessary` / `deprecated` rendering, no border or background, the squiggle is drawn with line segments and not an SVG)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Hover on markers (P8-09), `Unnecessary` fading, `Deprecated` strike-through, high contrast double underlines, the "next problem" navigation.

## Suggested commit message

`feat(rendering): draw marker squiggles and add the marker colors`
