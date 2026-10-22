# P6-38 — Drawing the decorations overview ruler

- **Status:** todo
- **Depends on:** P6-37, P6-33
- **Size:** M

## Goal

The renderer draws Monaco's decorations overview ruler in the vertical scrollbar area: one mark per decoration group and
lane (with the 6-DIP minimum height and the merge of close marks), the cursor marks and the border, with the lanes and the
options of Monaco.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/viewParts/overviewRuler/decorationsOverviewRuler.ts`.

| Lines | What to port |
|---|---|
| 22–101 | `class Settings` (constructor: line height, pixel ratio, lanes, border, cursor colors at 70 %, background rule, position, canvas size) |
| 103–188 | `_initLanes` (lane x / width tables for 1, 2, 3 lanes) |
| 190–210 | `Settings.equals` |
| 213–233 | `Constants.MIN_DECORATION_HEIGHT`, `OverviewRulerLane`, `ShouldRenderValue` |
| 311–322 | `onCursorStateChanged` (cursor colors: single, or primary / secondary with several cursors; sorted) |
| 357–519 | `_render` (background, decoration groups, cursors, border) |

Options and colors: `src/vs/editor/common/config/editorOptions.ts` 233–238, 310 (`overviewRulerLanes` 3 (0–3),
`overviewRulerBorder` true, `hideCursorInOverviewRuler` false; registered at 6449–6451 and 6584–6595);
`src/vs/editor/common/core/editorColorRegistry.ts` 61–62 (`editorOverviewRuler.border`, `editorOverviewRuler.background`).

## Files to create or change

- `src/LoomEditor.View/Layout/EditorOverviewRulerOptions.cs` — create: `internal sealed record EditorOverviewRulerOptions(int Lanes = 3, bool Border = true, bool HideCursor = false)`
- `src/LoomEditor.View/Layout/EditorLayoutOptions.cs` — change: `public EditorOverviewRulerOptions OverviewRuler { get; init; } = new();`
- `src/LoomEditor.Rendering/OverviewRulerSettings.cs` — create (header `decorationsOverviewRuler.ts (Settings)`): settings + `_initLanes`
- `src/LoomEditor.Rendering/TextViewRenderer.OverviewRuler.cs` — create (header `decorationsOverviewRuler.ts`): `DrawOverviewRuler(...)`,
  called **last** in `Render` (it sits on top of the text, under the XAML scrollbar)
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: `editorOverviewRuler.border`, `editorOverviewRuler.background` (null default),
  and `editorMultiCursor.primary.foreground` / `.secondary.foreground` if missing
- `src/LoomEditor.WinUI/Themes/Generic.xaml` — change only if the vertical `ScrollBar` paints an opaque track over the ruler: make
  its track background transparent
- `tests/LoomEditor.Rendering.Tests/TextViewRendererOverviewRulerTests.cs`, `OverviewRulerSettingsTests.cs` — create

## Design decisions (already made — do not change)

- The renderer draws every frame, so Monaco's `ShouldRenderValue` / "rendered decorations" diffing is **not** ported (one comment).
- Coordinates: Monaco draws on a canvas of `domWidth * pixelRatio`; LoomEditor draws in DIPs directly on the surface at
  `layoutInfo.OverviewRuler` (`Top`, `Right`, `Width`, `Height`), with `pixelRatio = 1` in the settings (Win2D scales). Keep the
  integer truncations (`| 0`) on the DIP values: `(int)`.
- `heightRatio = rulerHeight / ViewLayout scroll height`; `getVerticalOffsetForLineNumber` → `ViewLayout.GetVerticalOffsetForLineNumber`.
- Background: `editorOverviewRuler.background` if set; else the editor background **only** when the minimap is on the right
  (always false until the minimap exists: write `false` with a `// Minimap: P6-41` comment; P6-41 replaces it with
  `layoutInfo.Minimap.RenderMinimap != None && side == Right`); else nothing.
- Cursor colors: `editorCursor.foreground` (single), `editorMultiCursor.primary.foreground` / `.secondary.foreground` (several), all
  at 70 % alpha (`ThemeColor` alpha × 0.7, helper from P6-33). The cursor positions are the view positions of the selections, sorted.
- `Lanes == 0` draws nothing.

## Tests

- [ ] New (`OverviewRulerSettingsTests`): `InitLanes_ThreeLanes_SplitsInThirds`; `…_TwoLanes`; `…_OneLane`
- [ ] New: `DrawOverviewRuler_DecorationMark_HasTheMinimumHeight`
- [ ] New: `DrawOverviewRuler_CloseMarksOfTheSameLane_AreMerged`
- [ ] New: `DrawOverviewRuler_Cursor_DrawnInTheFullLane`; `…_HideCursor_NotDrawn`
- [ ] New: `DrawOverviewRuler_Border_DrawnWhenEnabled`
- [ ] New: `DrawOverviewRuler_LanesZero_DrawsNothing`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: new row `browser/viewParts/overviewRuler/decorationsOverviewRuler.ts`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`overviewRuler.ts` / `overviewZoneManager.ts` (the generic ruler of the diff editor: phase 9); features that add marks (P6-39).

## Suggested commit message

`feat(rendering): draw the decorations overview ruler`
