# P16-15 — Share the comment gutter controller and the comment zone host

- **Status:** todo
- **Depends on:** P16-03
- **Size:** M

**Recommended model:** Sonnet

## Goal

The "+" gutter logic and the comment zone bookkeeping (threads added and removed, line text changes, read-only, height passes) exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/Comments/CommentGutterController.cs`, `CommentZoneHost.cs`, `CommentZoneHost.Height.cs` and their twins. The `sonar-fix` work may have touched the Avalonia `CommentZoneHost.cs`: start from the current files.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/Comments/CommentGutterController.cs` | 33+23, 128+25, 155+39 | constructor, `LineOf`, `Update`, `TargetOf`, `HasThreadOn`, `OnThreadsChanged` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Comments/CommentZoneHost.cs` | 34+20, 131+30, 177+45 | constructor part, `Diagnostics`, `Dispose`, `OnThreadAdded`, `OnReadOnlyChanged`, `OnThreadLineTextChanged`, `OnThreadRemoving`, `Add` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Comments/CommentZoneHost.Height.cs` | 9+45 | `OnFontChanged`, `MarkAllDirty`, `BoxWidth`, `MarkDirty`, `ScheduleHeightPass` | WinUI same path |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/Comments/CommentGutterController.cs` — shared part; `CommentGutterController.Host.cs` per host: button creation and placement calls.
- `src/LoomEditor.HostShared/Controls/Comments/CommentZoneHost.cs`, `src/LoomEditor.HostShared/Controls/Comments/CommentZoneHost.Height.cs` — shared parts; `CommentZoneHost.Host.cs` per host: measuring a widget, focus return,
  automation peer, dispatching through `HostDispatcher`.

## Design decisions (already made — do not change)

- Height pass scheduling keeps its priority (the WinUI one): use `HostDispatcher.Post` or `PostLow` to match.

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).
- [ ] `CommentGutterTests` and `CommentZoneTests` pass unchanged.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`CommentThreadWidget` (P16-16).

## Suggested commit message

`refactor(hosts): share the comment gutter controller and comment zone host`
