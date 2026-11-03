# P16-18 — Move the shared text layout helpers into `LoomEditor.Rendering`

- **Status:** todo
- **Depends on:** P15-34
- **Size:** S

**Recommended model:** Sonnet

## Goal

The cluster hit-test helpers and the `wght` parsing that `Win2DLineLayout` / `AvaloniaLineLayout` and `Win2DTextMeasurer` / `AvaloniaTextMeasurer` both contain
live once in `LoomEditor.Rendering`, with tests. (Both sides are new code on PR #26: the Win2D files moved in phase 15.)

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Rendering/Win2D/Win2DLineLayout.cs`, `Win2DTextMeasurer.cs`, `src/LoomEditor.Avalonia/Rendering/AvaloniaLineLayout.cs`, `AvaloniaTextMeasurer.cs`.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Rendering/AvaloniaLineLayout.cs` | 77+44 | `IsSplittableCluster`, `NearestOffset` | `Win2DLineLayout.cs` 61+44 |
| `src/LoomEditor.Avalonia/Rendering/AvaloniaTextMeasurer.cs` | 189+24 | `TryGetWght`, the weight arithmetic of `ResolveBaseWeight` | `Win2DTextMeasurer.cs` 156+24 |

## Files to create or change

- `src/LoomEditor.Rendering/LineLayoutClusters.cs` — create: `internal static class LineLayoutClusters` with the two algorithms as static methods over the data they
  read today (the line text and the cluster/caret-stop arrays or delegates the two layouts already have).
- `src/LoomEditor.Rendering/FontVariationSettings.cs` — create: `internal static bool TryGetWght(string settings, out int value)` and the numeric part of the base-weight
  resolution (returns an `int`; each host converts to its `FontWeight`).
- The four host files call them. `ResolveInstalledFamily` (uses `FontManager` / DirectWrite) stays per host.
- `tests/LoomEditor.Rendering.Tests/LineLayoutClustersTests.cs`, `FontVariationSettingsTests.cs` — create

## Design decisions (already made — do not change)

- Rendering is `net11.0` and platform neutral: the helpers take no Win2D or Avalonia type. Behavior identical to the WinUI version.

## Tests

- [ ] New: `TryGetWght_ValidSetting_ReturnsValue`, `TryGetWght_NoWght_ReturnsFalse`, `TryGetWght_Malformed_ReturnsFalse`; `NearestOffset_LeftHalf_ReturnsStart`,
  `NearestOffset_RightHalf_ReturnsEnd`, `IsSplittableCluster_SurrogatePair_IsFalse` (or the closest real cases of the extracted code)
- [ ] `AvaloniaLineLayoutTests` pass unchanged

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The rest of the text measurers.

## Suggested commit message

`refactor(rendering): share the cluster hit-test and wght helpers of the two text layouts`
