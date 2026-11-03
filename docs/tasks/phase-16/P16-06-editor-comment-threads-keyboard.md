# P16-06 — Share the `LoomEditor` comment threads, keybinding context, selection API and screen reader refresh

- **Status:** todo
- **Depends on:** P16-03
- **Size:** M

**Recommended model:** Sonnet

## Goal

`LoomEditor.CommentThreads.cs` and the duplicated members of `LoomEditor.Keyboard.cs`, `LoomEditor.cs` and `LoomEditor.Accessibility.cs` exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). The four WinUI files and their Avalonia twins under `src/LoomEditor.WinUI/Controls` / `src/LoomEditor.Avalonia/Controls`.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/LoomEditor.CommentThreads.cs` | 33+87, 150+12 | `CommentThreads`, `AddCommentThread` (both overloads), `RemoveCommentThread`, the comment events, `Announce`, `InitializeCommentThreads` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/LoomEditor.Keyboard.cs` | 11+36 | `IsInDiffEditor`, `CreateKeybindingContext` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/LoomEditor.cs` | 139+29 | `Model`, `SetSelections`, `SelectionChanged`, `Commands` | WinUI `LoomEditor.cs` 121+29 |
| `src/LoomEditor.Avalonia/Controls/LoomEditor.Accessibility.cs` | 89+18 | `RefreshScreenReader` | WinUI same path |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/LoomEditor.CommentThreads.cs` — from the WinUI file (`git mv`); host parts `LoomEditor.CommentThreads.Host.cs` (comment zone host creation, announcements through
  the automation peer, `DispatcherQueue` → `HostDispatcher`).
- `src/LoomEditor.HostShared/Controls/LoomEditor.Keyboard.cs` — new, only `IsInDiffEditor` and `CreateKeybindingContext`; each host's `LoomEditor.Keyboard.cs` → `git mv` to `LoomEditor.Keyboard.Host.cs`.
- `src/LoomEditor.HostShared/Controls/LoomEditor.Selection.cs` — new, the four members of `LoomEditor.cs`; `src/LoomEditor.HostShared/Controls/LoomEditor.ScreenReader.cs` — new, `RefreshScreenReader`.
  The host files `LoomEditor.cs` and `LoomEditor.Accessibility.cs` keep their names and the rest.

## Design decisions (already made — do not change)

- New shared files take a name by concern when the host files keep most of their code (`LoomEditor.Selection.cs`, `LoomEditor.ScreenReader.cs`).
- Public members keep their XML doc and their order inside the new file.

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Pointer and scrolling (P16-07). The comment widgets (P16-15, P16-16).

## Suggested commit message

`refactor(hosts): share the comment thread, selection and keybinding-context members of LoomEditor`
