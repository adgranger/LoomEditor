# P9-28 — The generic `OverviewRuler` and the diff overview

- **Status:** done
- **Depends on:** P9-27, P9-23
- **Size:** M

## Goal

The diff editor shows an overview strip on the right (two 15 DIP rulers: removed lines of the original, added lines of the modified), with a slider that shows the visible part of the modified editor; clicking or dragging the strip scrolls both editors, the wheel scrolls them.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/overviewRuler/overviewRuler.ts` | 100–172 | `OverviewRuler.setLayout`, `setZones`, `_render`, `_renderOneLane` (the merge of adjacent same-color zones) |
| `src/vs/editor/browser/widget/diffEditor/features/overviewRulerFeature.ts` | 24–70 | constants (`ONE_OVERVIEW_WIDTH = 15`, `ENTIRE_DIFF_OVERVIEW_WIDTH = 30`), the colors (`diffEditorOverview.insertedForeground` ?? `diffEditor.insertedTextBackground` ?? default, `.transparent(2)`; same for removed), the `diffViewport` element |
| same | 71–110 | zones are rebuilt when the diff, the editors' zones or hidden areas change |
| same | 111–158 | `createZones` (view line of the range start and end, `lineCount = end - start`) |
| same | 159–174 | the layout (`originalOverviewRuler` at `right: freeSpace + 15`, `modified` at `right: 0`) and the viewport slider (`ScrollbarState`: `getSliderPosition()` / `getSliderSize()`) |
| `src/vs/base/browser/ui/scrollbar/scrollbarState.ts` | 9–243 | `ScrollbarState` (slider size and position, `getDesiredScrollPositionFromOffset`; minimum slider 20) |

## Files to create or change

- `src/LoomEditor.View/DiffEditor/DiffOverviewZones.cs` — create (pure: `DiffState` + ranges → `OverviewRulerZone` lists, the color rule)
- `src/LoomEditor.View/Layout/ScrollbarState.cs` — create (port, pure)
- `src/LoomEditor.Rendering/OverviewRulerPainter.cs` — create (`internal sealed class OverviewRulerPainter(OverviewZoneManager manager)`: `Draw(IRenderSurface surface, float left, float top, float width, float height)`)
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffOverviewRuler.cs` — create (a `Control` / `CanvasControl` host that owns two managers and painters and draws the slider; pointer handling) and `LoomDiffEditor.Overview.cs`
- `src/LoomEditor.WinUI/Themes/Generic.xaml` — change: a `PART_Overview` element in the diff template (30 DIPs wide, at the right of the modified editor, `Visibility` from `RenderOverviewRuler`); the modified editor gets `Margin.Right = 0` (it already reserved room in P9-22)
- `tests/LoomEditor.View.Tests/DiffEditor/DiffOverviewZonesTests.cs`, `tests/LoomEditor.View.Tests/Layout/ScrollbarStateTests.cs`, `tests/LoomEditor.Rendering.Tests/OverviewRulerPainterTests.cs` — create

## Design decisions (already made — do not change)

- `DiffOverviewZones.Create(IReadOnlyList<LineRange> ranges, Func<int, int> modelToViewLine, ThemeColor color)` → zones: ranges with `Length > 0`; `startView = modelToViewLine(range.StartLineNumber)`, `endView = modelToViewLine(range.EndLineNumberExclusive)`, `OverviewRulerZone(startView, endView, heightInLines: endView - startView, color)` (Monaco comment: "by computing the lineCount, we won't ask the view model later for the bottom vertical position"). `modelToViewLine` is the editor's coordinates converter (`ConvertModelPositionToViewPosition(new Position(line, 1)).LineNumber`; the control passes a lambda). The color rule is a static `DiffOverviewZones.GetColors(EditorTheme theme)` → `(ThemeColor Insert, ThemeColor Remove)`: the theme's `diffEditorOverview.*Foreground` if set, else `diffEditor.*TextBackground` if set, else `rgba(155,185,85,0.2)` / `rgba(255,0,0,0.2)`, **then alpha × 2 capped at 1** (`transparent(2)`: Monaco `Color.transparent(factor)` multiplies the alpha by `factor`; check `colorUtils` / `color.ts` `transparent`: copy its exact rounding).
- `ScrollbarState`: constructor `(arrowSize, scrollbarSize, oppositeScrollbarSize, visibleSize, scrollSize, scrollPosition)` as Monaco, `GetSliderSize()`, `GetSliderPosition()`, `GetDesiredScrollPositionFromOffset(double offset)` (a click centers the slider on the pointer), `GetDesiredScrollPositionFromDelta(double delta)` (drag). Numbers `double`; the Monaco `Math.round` / `Math.floor` calls stay.
- Painting: one strip per ruler: `OverviewRulerPainter.Draw` resolves the zones with the manager (`SetDomWidth`, `SetDomHeight`, `SetOuterHeight(scrollHeight)`, `SetLineHeight`, `SetPixelRatio(rasterizationScale)`) and fills one rectangle per merged band exactly as `_renderOneLane` (adjacent or overlapping same-color zones are merged into one rectangle). Rectangles are in DIPs: `y / pixelRatio`. The slider is a translucent rectangle (`scrollbarSlider.background`, hover `scrollbarSlider.hoverBackground`: use whatever ids the theme table has; fall back to `#79797966`) over both rulers, full 30 DIP width, `y = GetSliderPosition()`, `height = GetSliderSize()`.
- The overview redraws when: the diff changes, `ViewZones.ZonesChanged` / hidden areas change of either editor, the scroll position or scroll height changes (`HostScrollChanged`), the size changes, the theme changes.
- Pointer: on pointer pressed in the strip: `Modified.SetScrollPositionImmediate(null, state.GetDesiredScrollPositionFromOffset(y))`, then dragging uses `GetDesiredScrollPositionFromDelta` (capture the pointer); the original follows through the P9-23 synchronization. Mouse wheel over the strip scrolls the modified editor by the wheel delta (use `MouseWheelScrolling` of View, as the editor control does: copy how `LoomEditor.Scrolling.cs` handles `PointerWheelChanged`, one notch = the same pixels as over the editor).
- Both editors have `OverviewRulerEnabled = false` (P9-21) so there is no second ruler; their own overview ruler decorations (find matches…) are therefore not shown in the diff editor (listed in `PORTING.md`).
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [x] New (`DiffOverviewZonesTests`): `Create_SkipsEmptyRanges`, `Create_MapsModelToViewLines`, `Create_HeightInLinesIsViewLineDelta`, `GetColors_ThemeValues_Used_AlphaDoubled`, `GetColors_Defaults_WhenThemeHasNone`, `GetColors_AlphaCappedAtOne`.
- [x] Port-style (`ScrollbarStateTests`, expected values from the formulas): `SliderSize_ProportionalToVisibleOverScroll`, `SliderSize_MinimumTwenty`, `SliderSize_NotNeeded_FullTrack`, `SliderPosition_AtTop_Zero`, `SliderPosition_AtBottom_TrackMinusSlider`, `GetDesiredScrollPositionFromOffset_CentersSlider`, `GetDesiredScrollPositionFromDelta_Proportional`, `SetScrollSize_Change_Recomputes`.
- [x] New (`OverviewRulerPainterTests`, recording surface): `Draw_NoZones_NoRectangles`, `Draw_TwoZonesSameColorAdjacent_OneRectangle`, `Draw_TwoColors_TwoRectangles`, `Draw_ScaledByHeightRatio`.
- [ ] Manual (P9-31's plan): red / green marks line up with the changes, the slider follows the scroll, clicking and dragging scrolls both sides, the wheel over the strip scrolls.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `overviewRuler.ts` (generic part), `overviewRulerFeature.ts`, `scrollbarState.ts` (used for the slider only)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Replacing the decorations overview ruler of the single editor by the generic one, cursor marks.

## Suggested commit message

`feat: add the generic overview ruler and the diff overview`
