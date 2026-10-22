# P6-58 — The `StickyScroll` feature: widget state and re-render triggers

- **Status:** todo
- **Depends on:** P6-57
- **Size:** M

## Goal

A new `StickyScroll` feature computes, on scroll, resize, token and candidate changes, which lines stick at the top of the
editor (`findScrollWidgetState`, max 25 % of the editor height and `maxLineCount`), and publishes that state for the
renderer. The reveal logic leaves room for the sticky lines.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/stickyScroll/browser/stickyScrollController.ts` | 50–165 | the fields and the constructor (what is created and listened to; skip context keys, focus, hover, context menu) |
| same | 455–499 | `_readConfiguration` (enable / disable, scroll, layout, tokens, candidate listeners; the relative line numbers branch is not ported: no relative line numbers) |
| same | 501–548 | `_readConfigurationChange`, `_needsUpdate`, `_onTokensChange`, `_onDidResize` (25 %) |
| same | 550–614 | `_renderStickyScroll`, `_updateAndGetMinRebuildFromLine`, `_updateState`, `_resetState` (without the focus branch) |
| same | 616–647 | `findScrollWidgetState` |
| `src/vs/editor/contrib/stickyScroll/browser/stickyScrollWidget.ts` | 155–175 | `_findRenderingData` |
| `src/vs/editor/browser/viewParts/viewLines/viewLines.ts` | 126–128, 171–173, 206–208, 727 | `_stickyScrollEnabled`, `_maxNumberStickyLines` and their use in the reveal (`surroundingLines = max(cursorSurroundingLines, sticky ? maxLineCount : 0)`) |

## Files to create or change

- `src/LoomEditor.View/Features/StickyScroll/StickyScrollFeature.cs` — create (header `stickyScrollController.ts`): `IEditorFeature`, id `"loom.stickyScroll"`
- `src/LoomEditor.View/Features/StickyScroll/StickyScrollRenderData.cs` — create (header `stickyScrollWidget.ts (_findRenderingData)`):
  `internal sealed record StickyScrollRenderData(IReadOnlyList<int> LineNumbers, int LastLineRelativePosition)` and the computation
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: add `() => new StickyScrollFeature()` **after** `FoldingFeature`
- `src/LoomEditor.View/Features/EditorContext.cs`, `EditorContextExtensions.cs` — change: expose the folding feature (set by
  `FoldingFeature.Attach`, like `FindController`) through `GetFoldingFeature()`
- `src/LoomEditor.View/Layout/ViewModel.cs` (or a new partial `ViewModel.StickyScroll.cs`) — change: `internal StickyScrollRenderData? StickyScroll { get; set; }`
  and `EditorStickyScrollOptions StickyScrollOptions` in `EditorLayoutOptions` (`StickyScroll { get; init; } = new()`)
- `src/LoomEditor.View/Layout/RevealCalculator.cs` — change: the sticky lines in `surroundingLines` (replace the "Phase 4–5 cut point"
  comments at lines 20 and 197)
- `tests/LoomEditor.View.Tests/Features/StickyScroll/StickyScrollFeatureTests.cs` — create

## Design decisions (already made — do not change)

- The feature publishes `ViewModel.StickyScroll` (the renderer reads it in P6-59); `null` when disabled or empty.
- `getVisibleRanges()` → `ViewModel.GetVisibleRanges()` (P5-81); `getTopForLineNumber` / `getBottomForLineNumber` → `ViewLayout`
  vertical offsets of the **model** lines converted to view lines (Monaco's editor methods take model line numbers).
- Scroll: subscribe to the view layout scroll changes (the event `ViewLayout` raises; find it in `ViewLayout.Scrollable.cs`); tokens:
  `TextModel.TokensChanged`; layout: `ViewModel.LayoutInfoChanged`.
- `_maxStickyLines = Math.Round(height / lineHeight * 0.25, MidpointRounding.AwayFromZero)`.
- The candidate provider's version check (`getVersionId() === model.getVersionId()`) is kept.
- `FoldingModel` for folding icons is read through `GetFoldingFeature()?.FoldingModel` (may be null).

## Tests

New, porting the **scenarios** of `issue #157180` and `issue #159271` (`stickyScroll.test.ts` 161–212, 312–362) with the indentation
model (expected line numbers recomputed for it, explained in a comment):

- [ ] `FindScrollWidgetState_AtTop_NoStickyLine`
- [ ] `FindScrollWidgetState_InsideABlock_StickTheBlockStart`
- [ ] `FindScrollWidgetState_NestedBlocks_StickBoth`
- [ ] `FindScrollWidgetState_LastLinePushedUp_HasANegativeRelativePosition`
- [ ] `FindScrollWidgetState_MaxLineCount_IsRespected`
- [ ] `Disabled_PublishesNull`
- [ ] `Reveal_WithStickyScroll_KeepsMaxLineCountLinesAbove` (`RevealCalculator`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Architecture tests green (the feature folder has a README and is listed in `EditorFeatures.cs`)
- [ ] `docs/PORTING.md`: `stickyScrollController.ts` (🔶), `viewLines.ts` reveal row; the `Layout/RevealCalculator.cs` rows of the cut point
      tables are resolved
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing (P6-59), mouse and commands (P6-60, P6-61), focus navigation of sticky lines.

## Suggested commit message

`feat(sticky-scroll): compute the sticky lines`
