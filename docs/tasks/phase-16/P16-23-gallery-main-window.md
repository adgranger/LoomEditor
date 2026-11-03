# P16-23 — Gallery: share the main window logic

- **Status:** todo
- **Depends on:** P16-20
- **Size:** S

**Recommended model:** Sonnet

## Goal

The main window's picker lists, document loading, busy state and comment demo exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `samples/LoomEditor.Gallery/MainWindow.xaml.cs`, `samples/LoomEditor.Gallery/MainWindow.Comments.cs` and the Avalonia twins.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `samples/LoomEditor.Gallery.Avalonia/MainWindow.axaml.cs` | 19+49, 326+27, 358+32 | `LanguageIds`, `ThemeNames`, `PopulatePickers`, `LoadDocumentAsync`, `BeginBusy`, `EndBusy`, `SetSampleButtonsEnabled` | `MainWindow.xaml.cs` 15+49, 266+27, 304+32 |
| `samples/LoomEditor.Gallery.Avalonia/MainWindow.Comments.cs` | 6+38, 132+25 | `InitializeComments` (without the key accelerator), `RemoveAllThreads`, `Lines`, `TextOf`, `LogComment` | WinUI same path |

## Files to create or change

- Shared `samples/LoomEditor.Gallery/MainWindow.Pickers.cs`, `samples/LoomEditor.Gallery/MainWindow.Documents.cs` (new, members of the first row) and `samples/LoomEditor.Gallery/MainWindow.Comments.cs` (linked);
  the code-behind files keep the constructor; `MainWindow.Comments.Host.cs` per Gallery holds the Ctrl+Alt+M accelerator (WinUI `KeyboardAccelerator`, Avalonia key binding).

## Design decisions (already made — do not change)

- File pickers and window activation stay per host.

## Tests

- [ ] No new test. `--text-selfcheck` and `--comments-selfcheck` PASS on the Avalonia Gallery.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Other windows.

## Suggested commit message

`refactor(gallery): share the main window logic`
