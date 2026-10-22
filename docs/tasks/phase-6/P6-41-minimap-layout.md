# P6-41 — `ComputeMinimapLayout` and the minimap in the editor layout

- **Status:** todo
- **Depends on:** P6-40
- **Size:** M

## Goal

`EditorLayoutInfoComputer.ComputeLayout` computes the minimap layout (size, scale, sampling, side) and makes room for it,
like Monaco; the 8 minimap tests and the 2 word wrap tests of `editorLayoutProvider.test.ts` pass, which completes that file.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/config/editorOptions.ts` | 2605–2617 | `EditorLayoutInfoComputerEnv`: `memory`, `viewLineCount` (LoomEditor lacks them) |
| same | 2726–2744 | `computeContainedMinimapLineCount` |
| same | 2746–2920 | `_computeMinimapLayout` (with the `memory` stabilization for `fit` and `fill`) |
| same | 2922–3062 | `computeLayout`: the call (2993–3007), the left-side shift (3009–3015), `contentWidth = remainingWidth - minimapWidth` and the `minimap` member of the result |
| `src/vs/editor/test/browser/config/editorLayoutProvider.test.ts` | 52–123 | `computeLayoutInfo`, `doTest` (helpers) |
| same | 815–1373 | `EditorLayoutProvider 9 - render minimap`, `… pixelRatio = 2`, `… pixelRatio = 4`, `10 - render minimap to left`, `11 – 14` (cover / contain, with and without sampling) |
| same | 1375–1502 | `issue #31312: When wrapping, leave 2px for the cursor`, `leaves the rightmost column for the word wrap indicator when wrapping to the viewport` |

## Files to create or change

- `src/LoomEditor.View/Layout/EditorLayoutInfoComputer.cs` — change (or a new partial `EditorLayoutInfoComputer.Minimap.cs` to stay
  under ~400 lines): `ComputeContainedMinimapLineCount`, `ComputeMinimapLayout`, the integration; replace the `// Minimap: P6-…`
  comment left by P6-02
- `EditorLayoutInfoComputerEnv` — change: add `int ViewLineCount`, `ComputeOptionsMemory? Memory`, `bool ScrollBeyondLastLine`,
  `int PaddingTop`, `int PaddingBottom` (LoomEditor has no padding option: the view model passes 0)
- `src/LoomEditor.View/Layout/EditorLayoutInfo.cs` — change: add `EditorMinimapLayoutInfo Minimap`; update the remarks (nothing
  of Monaco's layout is missing except `isWordWrapMinified`)
- `src/LoomEditor.View/Layout/ViewModel.cs` — change: pass the view line count, a `ComputeOptionsMemory` field, scroll-beyond-last-line
  (from `ViewLayoutOptions`) when computing the layout, and recompute the layout when the view line count changes and the minimap is on
  (Monaco recomputes `layoutInfo` when `viewLineCount` changes)
- `src/LoomEditor.View/Layout/ViewLayout.cs` — change: replace the `// Minimap: P6-41` placeholder of P6-20 (`MinimapOnTheRight` in the
  wrapped content width) with `layoutInfo.Minimap.RenderMinimap != RenderMinimap.None && options.Minimap.Side == MinimapSide.Right`
  (Monaco reads `minimap.enabled && minimap.side === 'right'`: use the options' `Enabled` and `Side`)
- `src/LoomEditor.Rendering/OverviewRulerSettings.cs` — change, only if P6-38 is done: replace its `// Minimap: P6-41` placeholder the same way
- `tests/LoomEditor.View.Tests/Layout/EditorLayoutInfoComputerTests.cs` — change: port the 10 tests

## Design decisions (already made — do not change)

- `Math.floor`, `Math.ceil`, `Math.max`, `Math.min` on doubles: use `Math.Floor`/`Math.Ceiling` on `double` and cast exactly where
  Monaco's result is an integer (the layout records use `int` for DIP values that Monaco keeps integral; `minimapScale`,
  `minimapLineHeight` and the canvas widths can be fractional in Monaco: make those `double` in `EditorMinimapLayoutInfo` if a test
  expects a fraction — check every expected literal of the 10 tests before choosing each member's type).
- The test helper maps Monaco's input (`showGlyphMargin`, `lineDecorationsWidth`, `minimap`, `minimapSide`, `minimapRenderCharacters`,
  `minimapMaxColumn`, `minimapSize`, `wordWrap`, `wordWrapColumn`, `wordWrapIndicator`, …) to `EditorLayoutOptions` / `Env` and compares
  the whole `EditorLayoutInfo` record (Monaco's `deepStrictEqual`). Folding is off and padding is 0 in Monaco's helper (lines 56–58).
- The minimap does not draw yet: the control keeps `Minimap.Enabled = false` (P6-40), so nothing visible changes.

## Tests

- [ ] Port: `Editor ViewLayout - EditorLayoutProvider` › `EditorLayoutProvider 9 - render minimap`, `… with pixelRatio = 2`,
      `… with pixelRatio = 4`, `EditorLayoutProvider 10 - render minimap to left`, `11 - minimap mode cover without sampling`,
      `12 - minimap mode cover with sampling`, `13 - minimap mode contain without sampling`, `14 - minimap mode contain with sampling`
- [ ] Port: `issue #31312: When wrapping, leave 2px for the cursor`;
      `leaves the rightmost column for the word wrap indicator when wrapping to the viewport`
- [ ] New: `ComputeLayout_MinimapEnabledChangesContentWidth`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `editorLayoutProvider.test.ts` is 20 / 20; the `editorOptions.ts` row lists the minimap layout
- [ ] No `// Minimap: P6-41` placeholder is left in the code of the cards that are done (`grep -rn "Minimap: P6-41" src`)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing the minimap (P6-52).

## Suggested commit message

`feat(view): compute the minimap layout`
