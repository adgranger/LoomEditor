# P5-90 — Folding controls in the gutter: room, chevrons, mouse-over

- **Status:** todo
- **Depends on:** P5-89
- **Size:** M

## Goal

The gutter has room for the folding controls (16 DIPs, as Monaco's layout), and the renderer draws the chevrons of
the `codicon-folding-*` first-line decorations: an expanded region shows a down chevron when the mouse is over the
gutter (`showFoldingControls: mouseover`, VS Code's default) or always (`always`); a folded region always shows a right
chevron. The folded line shows `⋯` and the fold background (P5-08 rows, nothing new here).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/config/editorOptions.ts` | 2954–2960 | `lineDecorationsWidth += 16` when `folding` and `showFoldingControls !== 'never'` (already in `EditorLayoutInfoComputer`) |
| `contrib/folding/browser/folding.css` | 5–30, 50–55 | chevron visibility: hidden (`opacity: 0`) unless the margin is hovered, the region is collapsed, or `alwaysShowFoldIcons`; color `editorGutter.foldingControlForeground`; `margin-left: 2px`, centered, `font-size: 140%` |
| `browser/viewParts/linesDecorations/linesDecorations.ts` | search `firstLineDecorationClassName` | `.cldr` decorations: drawn in the line-decorations area (`decorationsLeft`, `decorationsWidth`), first line of the decoration only |
## Files to create or change

- `src/LoomEditor.View/Layout/EditorLayoutOptions.cs` — change: `Folding` defaults to `true` (Monaco's default); fix the remark
- `src/LoomEditor.View/Layout/DecorationStyles.cs` (P5-08) — change: rows `codicon-folding-expanded`,
  `codicon-folding-collapsed`, `codicon-folding-manual-expanded`, `codicon-folding-manual-collapsed`, `alwaysShowFoldIcons`
  (a new `GutterIcon` part of a style: `None`, `ChevronDown`, `ChevronRight`, and `AlwaysVisible`)
- `src/LoomEditor.Rendering/IRenderSurface.cs`, `Win2D/Win2DRenderSurface.cs` — change: `DrawLine(float x1, float y1, float x2, float y2, ThemeColor color, float strokeWidth)`
- `tests/LoomEditor.Rendering.Tests/TestHelpers/RecordingRenderSurface.cs` — change: record `DrawLine`
- `src/LoomEditor.Rendering/TextViewRenderer.GutterDecorations.cs` (P5-09) — change: a lines-decorations entry whose
  style has a `GutterIcon` draws it (after its background / border, in A09's order); `bool IsMarginHovered { get; set; }`
  on the renderer
- `src/LoomEditor.Themes/EditorTheme.cs` — change: `FoldingControlForeground` (`editorGutter.foldingControlForeground`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: `UpdatePointerShape` sets `renderer.IsMarginHovered`
  (`x < ContentLeft`), `OnPointerExited` clears it; redraw when it changes
- tests whose expected numbers assume no folding room — change: set `Folding = false` explicitly with a comment
  (`EditorLayoutInfoComputerTests`, `MouseTargetFactoryTests`, renderer tests; only where a number changes)
- `tests/LoomEditor.Rendering.Tests/TextViewRendererFoldingControlsTests.cs` — create

## Design decisions (already made — do not change)

- The class name string `alwaysShowFoldIcons codicon codicon-folding-expanded` is resolved by P5-08's
  `DecorationStyles.Get` (space-separated list); this card only adds the rows. `codicon` has no row.
- **Where:** the icon box is the line-decorations area of the line: `x` from `DecorationsLeft + 2` (CSS `margin-left: 2px`)
  to `DecorationsLeft + DecorationsWidth`, `y` the line box. The chevron is centered in that box.
- **How:** no codicon font. A chevron is two strokes of 1.5 DIPs in `FoldingControlForeground`: down = from
  `(cx - 4, cy - 2)` to `(cx, cy + 2)` to `(cx + 4, cy - 2)`; right = from `(cx - 2, cy - 4)` to `(cx + 2, cy)` to
  `(cx - 2, cy + 4)` (DIPs; an 8-DIP glyph, about Monaco's 140 % codicon at 14 px). Manual variants look the same
  (Monaco maps them to the same codicons).
- **When:** an icon with `AlwaysVisible` (collapsed icons, `alwaysShowFoldIcons`) is always drawn; the others only
  while `IsMarginHovered`. The 0.5 s opacity transition is not ported (behavior difference).
- `ShowFoldingControls` (P5-87/G11) drives which options the regions get; `never` gives options without icons and
  the layout has no folding room (`ShowFoldingDecoration = false`): `FoldingFeature.SetOptions` updates
  `EditorLayoutOptions` through `ViewModel.SetOptions` so layout and feature agree.
- The decorations come from the view-model decorations of the viewport (P5-07), already in view lines (P5-81).

## Tests

New (renderer with `RecordingRenderSurface` and `FakeTextMeasurer`):

- [ ] `Layout_FoldingOn_AddsSixteenDipsToTheDecorationsWidth` (View: default options → `DecorationsWidth == 26`)
- [ ] `ExpandedRegion_MarginNotHovered_DrawsNoChevron`; `…_MarginHovered_DrawsADownChevron` at the expected points
- [ ] `CollapsedRegion_AlwaysDrawsARightChevron`
- [ ] `ShowFoldingControlsAlways_ExpandedRegion_DrawsWithoutHover`
- [ ] `ShowFoldingControlsNever_DrawsNothingAndNoRoom`
- [ ] `RegionOnHiddenLine_DrawsNothing` (hidden lines are not drawn)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: row `contrib/folding/browser/folding.css` (drawn by `TextViewRenderer.GutterDecorations.cs`,
      P5-90); behavior differences "chevrons are strokes, not codicons; no fade"
- [ ] Card status and `docs/tasks/README.md` updated
- [ ] Manual check (maintainer, Gallery, C# sample): chevrons appear when the mouse enters the gutter and disappear
      when it leaves; a folded line keeps its right chevron, shows `⋯` and the fold background, in Dark and Light Modern

## Out of scope

Clicking the chevrons and the `⋯` (P5-91).

## Suggested commit message

`feat(rendering): draw the folding controls in the gutter`
