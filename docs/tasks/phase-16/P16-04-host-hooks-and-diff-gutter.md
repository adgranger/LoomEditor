# P16-04 — Split WinUI `LoomEditor.HostHooks` like Avalonia; share the diff gutter and scroll hooks

- **Status:** todo
- **Depends on:** P16-03
- **Size:** M

**Recommended model:** Sonnet

## Goal

The WinUI `LoomEditor.HostHooks.cs` (350 lines) is split into the same partial files as the Avalonia control (`LoomEditor.DiffGutter.cs`,
`LoomEditor.ViewZonePointer.cs`, `LoomEditor.HostHooks.cs`). Then the diff gutter partial and the scroll hooks exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs` and the Avalonia `LoomEditor.HostHooks.cs`, `LoomEditor.DiffGutter.cs`, `LoomEditor.ViewZonePointer.cs` (the target split).

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/LoomEditor.DiffGutter.cs` | 20+49 | `GlyphMarginLaneCount`, `DiffGutter`, `SetDiffGutterFactory`, `SetGutterLayer`, `CreateDiffGutter` | WinUI `LoomEditor.HostHooks.cs` 148+49 |
| `src/LoomEditor.Avalonia/Controls/LoomEditor.HostHooks.cs` | 40+28 | `GetVerticalOffsetForViewLine`, `GetScrollHeight`, `SetScrollPositionImmediate` | WinUI `LoomEditor.HostHooks.cs` 80+28 |

## Files to create or change

1. WinUI only, pure move (no code change): the diff gutter members → `src/LoomEditor.WinUI/Controls/LoomEditor.DiffGutter.cs`; the view zone pointer members, `ViewZonePointerPhase` and
   `ViewZonePointerEventArgs` → `src/LoomEditor.WinUI/Controls/LoomEditor.ViewZonePointer.cs`. Same members per file as the Avalonia control. Build.
2. `src/LoomEditor.WinUI/Controls/LoomEditor.DiffGutter.cs` → `git mv` to `src/LoomEditor.HostShared/Controls/LoomEditor.DiffGutter.cs`; the Avalonia twin — delete; host differences → `LoomEditor.DiffGutter.Host.cs`.
3. The three scroll hook members → new shared `src/LoomEditor.HostShared/Controls/LoomEditor.HostHooks.cs`; the rest of each host's `LoomEditor.HostHooks.cs` → `git mv` to `LoomEditor.HostHooks.Host.cs`.

## Design decisions (already made — do not change)

- Step 1 is a separate build-green step inside the card; it changes no line, only the file it lives in.
- `LoomEditor.ViewZonePointer.cs` is shared in P16-05, not here.

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The view zone pointer logic (P16-05). Any member not listed above.

## Suggested commit message

`refactor(hosts): split the WinUI host hooks like Avalonia; share the diff gutter and scroll hooks`
