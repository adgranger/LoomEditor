# P9-27 — `OverviewZoneManager`: zones of a generic overview ruler

- **Status:** done
- **Depends on:** P9-26
- **Size:** S

## Goal

The geometry of a generic overview ruler exists in `LoomEditor.View`: zones (a line range and a color) become sorted, pixel-snapped color bands that can be drawn on a strip of any height, independent of the editor's own decorations ruler.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/overviewZoneManager.ts` | 6–36 | `Constants.MINIMUM_HEIGHT = 4`, `ColorZone` (`compare`) |
| same | 37–84 | `OverviewRulerZone` (`compare`, `setColorZone`, `getColorZones`) |
| same | 85–247 | `OverviewZoneManager` (`getId2Color`, `setZones`, `setLineHeight`, `setPixelRatio`, `setDOMWidth`, `setDOMHeight`, `setOuterHeight`, `resolveColorZones`) |

## Files to create or change

- `src/LoomEditor.View/Layout/OverviewZoneManager.cs` (`ColorZone`, `OverviewRulerZone`, `OverviewZoneManager`) — create
- `tests/LoomEditor.View.Tests/Layout/OverviewZoneManagerTests.cs` — create

## Design decisions (already made — do not change)

- Colors are `LoomEditor.Themes.ThemeColor` (the existing public record struct; the manager keeps `Dictionary<ThemeColor, int> _color2Id` and `List<ThemeColor> _id2Color` with `_id2Color[0]` unused as Monaco's `colorId` starts at 1). `OverviewRulerZone.Compare`: Monaco compares color **strings** (`a.color < b.color`); compare the packed ARGB value `(A << 24 | R << 16 | G << 8 | B)` as `uint` instead — any total order gives the same drawing because color zones are sorted by `colorId` afterwards. (No behavior difference visible; note it in the XML doc.)
- `Func<int, int> getVerticalOffsetForLine` is passed to the constructor (Monaco: a view layout callback), `int` pixels like `ViewLayout`. `heightInLines == 0` means "up to `EndLineNumber`".
- The manager is **single threaded and stateful** like Monaco's: `setZones` sorts a copy of the list; `resolveColorZones()` returns `IReadOnlyList<ColorZone>` sorted by `ColorZone.Compare` and caches per zone until something invalidates (line height, pixel ratio, DOM width / height, outer height). `Math.floor` → `(int)Math.Floor`. Keep the `ycenter` / `halfHeight` clamping as written (lines 195–215).
- `pixelRatio` is a `double` (DPI scale), canvas sizes are `DomSize * pixelRatio` as `int`.
- `<remarks>Threading: UI thread only; not thread-safe.</remarks>`

## Tests

- [x] New (expected numbers computed by hand from the formulas): `ResolveColorZones_SingleZone_ProportionalPosition` (outer height 1000, canvas 100, a zone of 10 lines at line 11 with line height 10), `ResolveColorZones_ZoneShorterThanMinimum_GrownToFourPixels`, `ResolveColorZones_ZoneAtTopEdge_ClampedInsideCanvas`, `ResolveColorZones_ZoneAtBottomEdge_ClampedInsideCanvas`, `ResolveColorZones_HeightInLinesZero_UsesEndLine`, `ResolveColorZones_SameColorsSortedByFrom`, `ResolveColorZones_ColorIdsAssignedOncePerColor`, `ResolveColorZones_CachedUntilInvalidated` (a counter in the offset callback), `SetLineHeight_Same_ReturnsFalse`, `SetDomHeight_Change_InvalidatesCache`, `SetPixelRatio_ScalesCanvas`, `ColorZone_Compare_ByColorThenFromThenTo`, `OverviewRulerZone_Compare_Order`.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated: `overviewZoneManager.ts`; the row "The generic `OverviewRuler` / `overviewZoneManager.ts` (diff editor) | phase 9" of the cut points becomes ✅ P9-27 / P9-28
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing (P9-28), replacing the existing decorations ruler (it keeps its own implementation).

## Suggested commit message

`feat(view): port OverviewZoneManager`
