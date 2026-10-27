# P9-21 — Control hooks for a host: zones, scroll, hidden areas and decorations

- **Status:** todo
- **Depends on:** P9-19, P9-20
- **Size:** S

## Goal

The `LoomEditor` control exposes, to code in the same assembly (the diff editor), what `LoomDiffEditor` needs from each of its two editors: view zones, scroll position and scroll events, hidden areas, decorations, line geometry, layout changes, and a switch that turns the control's own overview ruler off.

## Monaco sources

Sources: none (specified in this card). Monaco's counterparts are the `ICodeEditor` members used by `DiffEditorEditors` (`src/vs/editor/browser/widget/diffEditor/components/diffEditorEditors.ts`, 24–224): `changeViewZones`, `getScrollTop` / `setScrollTop` / `setScrollLeft` / `onDidScrollChange`, `setHiddenAreas`, `createDecorationsCollection`, `getTopForLineNumber`, `getWhitespaces`, `onDidLayoutChange` / `getLayoutInfo`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs` — create (`partial class LoomEditor`; all members `internal`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: a mouse down on a view zone with `SuppressMouseDown` does nothing (no caret move, no selection start): find where the hit test result is used on pointer pressed and add the check through `ViewModel.ViewZones.ShouldSuppressMouseDown(id)` (the method exists since P9-18)
- `src/LoomEditor.View/Layout/ViewportData.cs` — change: `ViewZoneViewportData.HitTest` (see Tests)
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change only to invalidate the canvas on `ViewZones.ZonesChanged`

## Design decisions (already made — do not change)

- Members (all `internal`, XML-documented, threading note "UI thread only"):
  `ViewZones ViewZones { get; }` (the View object); `double LineHeight { get; }`; `IReadOnlyList<IEditorWhitespace> GetWhitespaces()` (the layout's whitespaces: the diff code needs the zones that are not its own, as `getAdditionalLineHeights` does); `double GetTopForLineNumber(int modelLineNumber, bool includeViewZones = false)` (Monaco: `getTopForLineNumber`: the vertical offset of a model line in scroll coordinates, counting its wrapped view lines before it; `includeViewZones` adds the zones above, as in Monaco); `double GetScrollHeight()`; `double ScrollLeft { get; }` plus the existing `ScrollTop`;
  `event EventHandler<HostScrollEventArgs>? HostScrollChanged` with `HostScrollEventArgs(bool ScrollTopChanged, bool ScrollLeftChanged, bool ScrollHeightChanged, double ScrollTop, double ScrollLeft)` raised from `OnViewLayoutScroll` / `OnViewLayoutContentSizeChanged`; `void SetScrollPositionImmediate(double? scrollLeft, double? scrollTop)` (no smooth scrolling; Monaco: `ScrollType.Immediate`; it must not raise a feedback loop: the **caller** guards re-entrancy, the control does not);
  `void SetHiddenAreas(IReadOnlyList<Range> ranges, object source)` (forwards to `ViewModel.SetHiddenAreas`, existing since P5-81); `EditorDecorationsCollection CreateDecorationsCollection(IReadOnlyList<ModelDeltaDecoration>? decorations = null)` (the context's existing method); `event EventHandler? HostLayoutChanged` (forwards `_viewModel.LayoutInfoChanged`) and `Layout.EditorLayoutInfo LayoutInfo { get; }` (existing property of the view model, read-only);
  `bool OverviewRulerEnabled { get; set; }` (default `true`; `false` sets the decorations overview ruler lanes to 0 in the layout options through the existing option object — find how `LoomEditor.Minimap.cs` changes `EditorLayoutOptions`; used by the diff editor, which draws its own ruler in P9-28);
  `bool HideLineNumbers { get; set; }` (default `false`; for the original side of the inline view; uses the existing `lineNumbers` layout option if one exists — `grep -rn "LineNumbers" src/LoomEditor.View/Layout/EditorLayoutOptions.cs`; if there is none, **do not add this member**, P9-24 does not need it).
- **Zone pointer events** (needed by P9-25): `internal event EventHandler<ViewZonePointerEventArgs>? ViewZonePointer` with `ViewZonePointerEventArgs(ViewZone Zone, ViewZonePointerPhase Phase, double X, double YInZone, bool IsLeftButton)` and `enum ViewZonePointerPhase { Pressed, Moved, Released }`. The control hit-tests the pointer against the zones of the viewport (`ViewportData`'s zone data from P9-19; a zone is hit when the point is in the content area at the zone rectangle); on press over a zone it raises `Pressed` and, if the zone has `SuppressMouseDown`, returns before the editor's own handling (no caret move, no selection start); **if a handler calls `CapturePointerForZone()`** (an internal method of the args that makes the control remember the zone and capture the pointer), the following `Moved` and `Released` are raised for that zone even outside it (a drag), and ended by `Released` or pointer capture loss. Hovering raises `Moved` for the zone under the pointer when no capture is active. Mouse wheel scrolling is unchanged.
- Nothing here is public (no `PublicAPI` change). The WinUI assembly's `InternalsVisibleTo` is not needed: `LoomDiffEditor` lives in the same assembly.
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [ ] New (`tests/LoomEditor.View.Tests/Layout/ViewZoneHitTestTests.cs`; the pure part lives in View: add `ViewZoneViewportData.HitTest(double x, double y, double contentLeft, double contentWidth)` returning the zone and the y offset inside it, to the class P9-19 created): `HitTest_InsideZone_ReturnsZoneAndOffset`, `HitTest_BetweenLines_Null`, `HitTest_LeftOfContent_Null`, `HitTest_ZoneWithZeroHeight_Null`, `HitTest_TwoZones_ReturnsTheOneUnderThePoint`.
- [ ] The rest is covered by build and by later cards; run `dotnet build` and open the Gallery once to check that nothing regressed (the existing editor still scrolls, clicks and types).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The diff control (P9-22 …), public API.

## Suggested commit message

`feat(winui): expose host hooks of the editor control for the diff editor`
