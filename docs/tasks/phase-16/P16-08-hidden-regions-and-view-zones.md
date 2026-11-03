# P16-08 — Share `HideUnchangedRegionsController` and `DiffEditorViewZones`

- **Status:** todo
- **Depends on:** P16-03
- **Size:** S

**Recommended model:** Sonnet

## Goal

The two largest duplicated controllers exist once. They post through `HostDispatcher` instead of taking a `DispatcherQueue`.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/DiffEditor/HideUnchangedRegionsController.cs`, `src/LoomEditor.WinUI/Controls/DiffEditor/DiffEditorViewZones.cs`, their Avalonia twins, their callers in `LoomDiffEditor.*`.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/DiffEditor/HideUnchangedRegionsController.cs` | 48+230, 280+44 | the whole class but the constructor's dispatcher argument | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/DiffEditor/DiffEditorViewZones.cs` | 50+53, 106+154 | the whole class but the dispatcher | WinUI same path |

## Files to create or change

- `src/LoomEditor.WinUI/Controls/DiffEditor/HideUnchangedRegionsController.cs` → `git mv` to `src/LoomEditor.HostShared/Controls/DiffEditor/HideUnchangedRegionsController.cs`; Avalonia twin — delete.
  `_dispatcher.TryEnqueue(DispatcherQueuePriority.Low, …)` → `HostDispatcher.PostLow(…)`; the `DispatcherQueue dispatcher` constructor parameter goes.
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffEditorViewZones.cs` → `git mv` to `src/LoomEditor.HostShared/Controls/DiffEditor/DiffEditorViewZones.cs`; Avalonia twin — delete.
  `_recomputePending = _dispatcher.TryEnqueue(() => …)` → `_recomputePending = HostDispatcher.Post(() => …)`; the parameter goes.
- WinUI callers (`LoomDiffEditor.cs`, `LoomDiffEditor.Layout.cs`, `LoomDiffEditor.Options.cs`): drop the `DispatcherQueue` argument.

## Design decisions (already made — do not change)

- No host part is needed: the two classes use only `LoomEditor` members that both controls have.

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).
- [ ] `HiddenRegionsTests` and `DiffAlignmentTests` pass unchanged.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Other diff editor files.

## Suggested commit message

`refactor(hosts): share the hidden-regions and view-zone controllers of the diff editor`
