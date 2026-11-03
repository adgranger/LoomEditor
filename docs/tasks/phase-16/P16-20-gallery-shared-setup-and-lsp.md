# P16-20 — Gallery: global control usings; share the LSP demo

- **Status:** done
- **Depends on:** P15-34
- **Size:** S

**Recommended model:** Sonnet

## Goal

Both Galleries can compile the same file against their own control. The LSP self-check and demo controller exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `samples/LoomEditor.Gallery/LoomEditor.Gallery.csproj`, `samples/LoomEditor.Gallery.Avalonia/LoomEditor.Gallery.Avalonia.csproj`, `samples/LoomEditor.Gallery/Lsp/LspSelfCheck.cs`, `samples/LoomEditor.Gallery/Lsp/LspDemoController.cs`, `samples/LoomEditor.Gallery/MainWindow.Lsp.cs` and the Avalonia twins.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `samples/LoomEditor.Gallery.Avalonia/Lsp/LspSelfCheck.cs` | 7+127 | whole file (only the alias differs) | `samples/LoomEditor.Gallery/Lsp/LspSelfCheck.cs` |
| `samples/LoomEditor.Gallery.Avalonia/Lsp/LspDemoController.cs` | 6+51, 62+45 | whole file but the dispatcher lambda | `samples/LoomEditor.Gallery/Lsp/LspDemoController.cs` |
| `samples/LoomEditor.Gallery.Avalonia/MainWindow.Lsp.cs` | 35+25 | `SetLspStatus`, `RefreshLspButtons`, `AppendLspLog` | `samples/LoomEditor.Gallery/MainWindow.Lsp.cs` 34+25 |

## Files to create or change

- Both Gallery csproj: `<Using Include="LoomEditor.WinUI.Controls" />` and `<Using Include="LoomEditor.WinUI.Controls.LoomEditor" Alias="LoomEditorControl" />`
  (Avalonia: `LoomEditor.Avalonia.Controls…`). Remove the local `using LoomEditorControl = …;` lines they replace.
- `samples/LoomEditor.Gallery/GalleryDispatcher.cs` + `samples/LoomEditor.Gallery.Avalonia/GalleryDispatcher.cs` — create: `internal static class GalleryDispatcher` with `public static bool Post(Action action)` (WinUI
  `DispatcherQueue.GetForCurrentThread().TryEnqueue`, Avalonia `Dispatcher.UIThread.Post` + `true`).
- `samples/LoomEditor.Gallery.Avalonia/Lsp/LspSelfCheck.cs`, `samples/LoomEditor.Gallery.Avalonia/Lsp/LspDemoController.cs` — delete; link `samples/LoomEditor.Gallery/Lsp/…` instead (`Link="Shared\Lsp\…"`); `LspDemoController` posts through `GalleryDispatcher`.
- `MainWindow.Lsp.cs`: the three members → shared `samples/LoomEditor.Gallery/MainWindow.Lsp.cs`; framework code (event handlers, `Closed`/`Closing`, log caret) → `MainWindow.Lsp.Host.cs` in each Gallery.

## Design decisions (already made — do not change)

- Gallery files are not under `src/`: no header block; they compile per project through the global usings. A shared Gallery file names no WinUI or Avalonia type.

## Tests

- [x] No new test. Run `--lsp-selfcheck` on the Avalonia Gallery (`dotnet run --project samples/LoomEditor.Gallery.Avalonia -c Release -- --lsp-selfcheck=<file>`) and the WinUI Gallery if it runs on the machine: PASS.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Other windows (P16-21 … P16-23).

## Suggested commit message

`refactor(gallery): global control usings; share the LSP demo between the Galleries`

## Result

Done as written, except the global `<Using Include="LoomEditor.<Host>.Controls" />` of the card: only the `LoomEditorControl` alias is a global using (the namespace using is not needed by any shared file yet).
`--lsp-selfcheck` on the Avalonia Gallery runs the shared code but reports `FAIL server found: not found` on this machine (`csharp-ls` is not installed), as it would before the change; it did not exit by itself
within 150 s (the process was killed). Re-run it where `csharp-ls` is installed (P16-24 runs the self-checks).
