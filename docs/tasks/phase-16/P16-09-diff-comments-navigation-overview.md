# P16-09 — Share the diff editor's comment threads, navigation, overview ruler and gutter controllers

- **Status:** done
- **Depends on:** P16-08
- **Size:** M

**Recommended model:** Sonnet

## Goal

`LoomDiffEditor.CommentThreads.cs`, `LoomDiffEditor.Navigation.cs`, `LoomDiffEditor.Overview.cs` and `DiffGutterControllers.cs` exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). The four WinUI files under `src/LoomEditor.WinUI/Controls/DiffEditor/` and their Avalonia twins.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.CommentThreads.cs` | 9+157 | whole file | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.Navigation.cs` | 11+44 | `GoToNextChange`, `GoToPreviousChange`, `RevealFirstChangeAsync`, `GoToChange`, `GoTo` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.Overview.cs` | 15+152 | whole file but the wheel handler | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/DiffEditor/DiffGutterControllers.cs` | 11+36 | whole file | WinUI same path |

## Files to create or change

- `git mv` each WinUI file to `src/LoomEditor.HostShared/Controls/DiffEditor/…`; delete the Avalonia twins.
- Host members: `private void FocusModified()` (WinUI `_modified.Focus(FocusState.Programmatic)`, Avalonia `_modified.Focus()`) in `LoomDiffEditor.Navigation.Host.cs`;
  `OnOverviewWheel` (framework event args) in `LoomDiffEditor.Overview.Host.cs`. Avalonia's `#pragma warning disable CA1001` stays in the Avalonia host part that declares the class
  base, or in the shared file if the analyzer asks there (pragmas are allowed in shared files).

## Design decisions (already made — do not change)

- `LoomDiffEditor.Overview.cs` uses `LoomEditor.<Host>.Controls.DiffEditor` types: their `using` goes into the header block.

## Tests

- [x] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).
- [x] `DiffCommentThreadsTests`, `DiffOptionsTests`, `LoomDiffEditorTests` pass unchanged.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`LoomDiffEditor.cs`, `.Layout`, `.Options` (P16-10). Block actions and line selection (P16-12, P16-13).

## Suggested commit message

`refactor(hosts): share the diff editor's comment threads, navigation and overview`
