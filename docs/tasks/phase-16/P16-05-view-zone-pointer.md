# P16-05 — Share the view zone pointer

- **Status:** todo
- **Depends on:** P16-04
- **Size:** S

**Recommended model:** Sonnet

## Goal

Hit testing, move and release routing of host view zones (`ViewZonePointer`) exist once; only the pointer capture stays per host.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/LoomEditor.ViewZonePointer.cs` (after P16-04) and its Avalonia twin.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/LoomEditor.ViewZonePointer.cs` | 27+14, 57+99 | `HitTestZone`, `HandleZoneMove`, `EndZoneCapture`, `ViewZonePointerPhase`, `ViewZonePointerEventArgs`, `CaptureRequested` | WinUI `LoomEditor.HostHooks.cs` 216+, 246+ (now `LoomEditor.ViewZonePointer.cs`) |

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.ViewZonePointer.cs` → `git mv` to `src/LoomEditor.HostShared/Controls/LoomEditor.ViewZonePointer.cs`; the Avalonia twin → `git mv` to `src/LoomEditor.Avalonia/Controls/LoomEditor.ViewZonePointer.Host.cs`
  and reduced to the host members; a new `src/LoomEditor.WinUI/Controls/LoomEditor.ViewZonePointer.Host.cs`.
- `HandleZonePress(EditorMouseEvent, <framework pointer>)` splits: the shared `bool HandleZonePress(EditorMouseEvent mouseEvent)` (hit test, event, suppress decision)
  and a host member `CapturePointerForZone()` that captures the pointer saved by the host's pointer handler (WinUI `Pointer`, Avalonia `IPointer`), as both controls do today.

## Design decisions (already made — do not change)

- The capture/release order and the `double.NaN` default of `ViewZonePointerEventArgs.y` stay as in WinUI.

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The rest of the pointer code (P16-07).

## Suggested commit message

`refactor(hosts): share the view zone pointer routing`
