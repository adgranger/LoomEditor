# P15-15 — Overlay layer and host view zones

- **Status:** todo
- **Depends on:** P15-14
- **Size:** M

**Recommended model:** Sonnet

## Goal

Controls can be placed over the text (the overlay layer every widget uses), and a host can put its own Avalonia `Control` between lines (`AddViewZone` / `RemoveViewZone`).

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/Overlay/OverlayHost.cs`, `Controls/LoomEditor.Overlay.cs`, `Controls/ViewZones/HostViewZoneLayer.cs`,
`Controls/LoomEditor.ViewZones.cs`. Also read [ADR 0015](../../adr/0015-public-host-api.md) and `docs/tasks/phase-13/P13-08-host-view-zones.md`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/Overlay/OverlayHost.cs`, `Controls/LoomEditor.Overlay.cs`, `Controls/ViewZones/HostViewZoneLayer.cs`, `Controls/LoomEditor.ViewZones.cs` —
  create (twins)
- `tests/LoomEditor.Avalonia.Tests/Controls/OverlayHostTests.cs`, `HostViewZoneTests.cs` — create
- `PublicAPI.Unshipped.txt`, `AvaloniaParityPending.txt`

## Design decisions (already made — do not change)

- Placement on the `PART_OverlayCanvas` / `PART_ZoneWidgetCanvas` `Canvas`es with `Canvas.SetLeft/SetTop`, `IsVisible` for hide. This is the same pooling and the same
  `Arrange` after each draw as WinUI.
- `AddViewZone(…, Control content)` — the WinUI `UIElement` parameter becomes `Control` ([README](README.md) decision 3). Focus rules: as the twin (the zone content is focusable,
  the editor gets focus back on Escape).

## Tests

- [ ] New: `Overlay_Show_PlacesAtCoordinates`; `Overlay_Hide_CollapsesElement`; `AddViewZone_PushesNextLineDown` (`GetTopForLineNumber(5)` grows by the zone height);
  `AddViewZone_ContentIsArrangedInGap` (the content's bounds are inside the gap); `RemoveViewZone_RestoresLayout`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

Comment zones (P15-23), diff zones (P15-26).

## Suggested commit message

`feat(avalonia): overlay layer and host view zones`
