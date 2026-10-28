# P9-18 — View zones: gaps with a purpose, anchored to a model line

- **Status:** done
- **Depends on:** P9-17
- **Size:** M

## Goal

An editor can hold **view zones** (Monaco `changeViewZones`): vertical gaps that are anchored after a model line, have a height, survive edits and line mapping changes (hidden areas, word wrap), and carry a *kind* the renderer will paint. Zones sit on the existing `ViewLayout` whitespaces.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/viewZones/viewZones.ts` | 40–60, 68–97 | `ViewZones` state; `_recomputeWhitespacesProps` (called on line mapping change and on line height change) |
| same | 148–197 | `_computeWhitespaceProps` (model position of the zone → view line, `isInHiddenArea`, `showInHiddenAreas`, height from `heightInPx` / `heightInLines`, `minWidthInPx`) |
| same | 193–230, 232–330 | `changeViewZones`, `_addZone`, `_removeZone`, `_layoutZone`, `shouldSuppressMouseDown`, `_heightInPixels`, `_minWidthInPixels`, `_getZoneOrdinal` |
| `src/vs/editor/common/editorCommon.ts` or `src/vs/editor/browser/editorBrowser.ts` | search `interface IViewZone` | the members of `IViewZone` and `IViewZoneChangeAccessor` (`addZone`, `removeZone`, `layoutZone`) |

## Files to create or change

- `src/LoomEditor.View/Layout/ViewZone.cs` (`ViewZone`, `ViewZoneKind`), `ViewZones.cs`, `IViewZoneChangeAccessor.cs` — create
- `src/LoomEditor.View/Layout/ViewModel.cs` (+ `ViewModel.ViewZones.cs`) — change: owns `ViewZones ViewZones { get; }`; calls `ViewZones.RecomputeWhitespaceProps()` at the three places that call `_decorations.OnLineMappingChanged()` (`ViewModel.cs` ~267, `ViewModel.Cursors.cs` ~54, `ViewModel.HiddenAreas.cs` ~47) and when the line height changes
- `src/LoomEditor.View/Layout/ViewportData.cs` — change only if the renderer needs zones next to `WhitespaceViewportData` (see P9-19; leave it to that card)
- `tests/LoomEditor.View.Tests/Layout/ViewZonesTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class ViewZone` (mutable; the owner changes the fields and calls `LayoutZone(id)`): `int AfterLineNumber` (model line; 0 = before the first line), `int? AfterColumn`, `int? HeightInPx`, `double? HeightInLines`, `int? MinWidthInPx`, `int? Ordinal`, `bool ShowInHiddenAreas`, `bool SuppressMouseDown`, `ViewZoneKind Kind`, `object? Data` (what the renderer needs for the kind, P9-19 / P9-24), `Action<int>? OnComputedHeight`. `ViewZone.Id` (string, assigned by `AddZone`, read-only for the owner).
  `enum ViewZoneKind { Empty, DeletedLines, Collapsed }` (ADR 0012 decision 6). Not ported: `domNode`, `marginDomNode`, `onDomNodeTop`, `suppressMouseDown` stays.
- `IViewZoneChangeAccessor { string AddZone(ViewZone zone); void RemoveZone(string id); void LayoutZone(string id); }`; `ViewZones.Change(Action<IViewZoneChangeAccessor> callback) → bool` (true when something changed). The accessor is **invalidated** after the callback (calls throw `InvalidOperationException`, as Monaco's `invalidFunc`). An exception thrown by the callback propagates after the accessor is invalidated and after the whitespace accessor of `ViewLayout.ChangeWhitespace` is closed (the layout stays consistent).
  Internally one `ViewLayout.ChangeWhitespace` call per `Change`. `AddZone` computes the props (below), `ordinal = zone.Ordinal ?? zone.AfterColumn ?? 10000`.
- Props (`_computeWhitespaceProps`): `AfterLineNumber == 0` → view line 0, visible; else `afterModelPosition = ValidatePosition(afterLine, afterColumn ?? max column)`, `beforeModelPosition` = the position right after it (next line start when the zone is after the last column), view line = `CoordinatesConverter.ConvertModelPositionToViewPosition(afterModelPosition).LineNumber`, `IsInHiddenArea = !(ShowInHiddenAreas || ModelPositionIsVisible(beforeModelPosition))`, height `0` when hidden. `ICoordinatesConverter.ModelPositionIsVisible` already exists. `bool ShouldSuppressMouseDown(string id)` (port of line ~308) is part of this class.
- `RecomputeWhitespaceProps()`: as the TypeScript: for each zone, if the whitespace's line or height differs from the new props, `ChangeOneWhitespace` and call `OnComputedHeight`; raises `ZonesChanged` when something changed.
- Events: `event EventHandler? ZonesChanged` (raised when `Change` changed something and after a recompute that changed something); the control (P9-21) invalidates the canvas on it. `IReadOnlyList<ViewZone> GetZones()` in `ViewLayout` order is not needed: `ViewZone? GetZone(string id)` and `IEnumerable<ViewZone> Zones`.
- **Edits.** A zone keeps its model `AfterLineNumber`; when lines are deleted above it Monaco does **not** move it (owners re-layout): same here (no tracking). The diff editor re-creates its zones on every diff / option change (P9-23).
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [x] New: `AddZone_InsertsWhitespaceWithHeight`, `AddZone_HeightInLines_UsesLineHeight`, `AddZone_AfterLineZero_AtTop`, `AddZone_Ordinal_OrdersZonesAfterTheSameLine`, `RemoveZone_RemovesWhitespace`, `LayoutZone_AfterHeightChange_UpdatesLayout`, `Change_Accessor_InvalidAfterCallback_Throws`, `Change_CallbackThrows_LayoutStillConsistent`, `HiddenArea_ZoneHasZeroHeight_UnlessShowInHiddenAreas`, `HiddenArea_Changes_ZoneRecomputed` (use `ViewModel.SetHiddenAreas`), `WordWrap_LineMappingChange_ZoneMovesToWrappedLine` (a zone after a wrapped model line is anchored after its last view line), `OnComputedHeight_CalledWithHeight`, `ZonesChanged_RaisedOnceForABatch`, `LineHeightChange_RecomputesHeightInLinesZones`, `ShouldSuppressMouseDown_ZoneWithFlag_True_UnknownId_False`.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `viewZones.ts` (model part: no DOM nodes), `IViewZone`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Painting (P9-19), hit testing of zones, mouse suppression (P9-21), `getWhitespaces` on the control (P9-21).

## Suggested commit message

`feat(view): add view zones on top of the layout whitespaces`
