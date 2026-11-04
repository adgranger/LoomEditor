# P15-31 — The Avalonia Gallery

- **Status:** done
- **Depends on:** P15-30
- **Size:** M (app code; samples and self-checks are shared, not copied)

**Recommended model:** Sonnet

## Goal

`samples/LoomEditor.Gallery.Avalonia` runs on Windows, macOS and Linux. It has the main window (editor, language, theme, options, open file), the diff demo window with the
phase 14 actions, the host API demo, the LSP demo, and the same `--…-selfcheck` command-line modes as the WinUI Gallery. It is the manual test bed of
`docs/testing/phase-15-manual-tests.md`.

## Sources

The WinUI Gallery: `samples/LoomEditor.Gallery/` (`MainWindow.*`, `DiffDemoWindow.*`, `HostApiDemoWindow.*`, `App.xaml*`, `Program.cs`, `Lsp/`). Also read
`docs/testing/phase-13-manual-tests.md` and `phase-14-manual-tests.md` (what the windows must offer).

## Files to create or change

- `samples/LoomEditor.Gallery.Avalonia/LoomEditor.Gallery.Avalonia.csproj` — create: `WinExe`, `$(LoomNetTargetFramework)`, `PublishAot=true`, `Avalonia.Desktop`,
  `Avalonia.Themes.Fluent`; references `LoomEditor.Avalonia`, `LoomEditor.Lsp`
- **Linked, not copied** (`<Compile Include="..\LoomEditor.Gallery\X.cs" Link="Shared\X.cs" />`): every Gallery file that uses no WinUI type: `CommentSamples`, `CommentsSelfCheck`,
  `DiffSamples`, `DiffSelfCheck`, `FeatureSamples`, `FrameStatistics`, `HighlightingSamples`, `HostApiSamples`, `HostApiSelfCheck`, `LargeDocuments`, `ModelDemoActions`,
  `Phase10Samples`, `SampleText`, `TextSelfCheck`, `Lsp/DemoMarkers`, `Lsp/DemoProviders`, `Lsp/GalleryLocationOpener`, `Lsp/LspDemoSample`, `Lsp/LspDemoWorkspace`,
  `Lsp/LspServerLocator`. If a linked file needs a change to compile in both apps, make that change in the WinUI Gallery file (no `#if`) and say so in the Result
- Create `Program.cs` (new; WinUI generates its entry point): `[STAThread] Main` → `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`, with the self-check
  flags handled before the app starts when the WinUI Gallery handles them before its window
- Create, as Avalonia twins of the WinUI files: `App.axaml(.cs)`, `MainWindow.axaml(.cs)` and its partials (`Comments`, `CommentsCheck`, `Diff`, `HostApiCheck`,
  `Lsp`, `TextCheck`), `DiffDemoWindow.axaml(.cs)` (+ `Actions`, `Comments`), `HostApiDemoWindow.axaml(.cs)` (+ `Blame`, `Diff`), `ScrollTest.cs`, `Lsp/LspDemoController.cs`,
  `Lsp/LspSelfCheck.cs`
- `UiaSelfCheck.cs` — **not** ported (UI Automation client, Windows only); the Avalonia Gallery prints `uia self-check: not available on Avalonia` for that flag
- `LoomEditor.slnx`, `LoomEditor.Portable.slnf` — add the project
- `docs/testing/phase-15-manual-tests.md` — the "Gallery setup" paragraph for the Avalonia Gallery

## Design decisions (already made — do not change)

- `App.axaml` uses `FluentTheme` + `<loom:LoomEditorStyles />`; the theme switch sets `RequestedThemeVariant`.
- Open file: `TopLevel.StorageProvider.OpenFilePickerAsync` (WinUI `FileOpenPicker`).
- Self-check modes run without showing a window when the WinUI ones do, and exit with the same codes.

## Tests

- [x] `dotnet run --project samples/LoomEditor.Gallery.Avalonia -- --text-selfcheck=<file>` (and the diff, comments, host API self-checks) pass on Windows; record the
  numbers (`10 of 10`, …) in the Result
- [x] Manual (written, not run): the Gallery on macOS and Ubuntu, each window opens

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Result

- **Self-checks on Windows** (Release build, exit code 0 each): `--text-selfcheck` 10 of 10, `--diff-selfcheck` 9 passed / 0 failed, `--comments-selfcheck` 17 of 17,
  `--hostapi-selfcheck` 6 of 6. `--lsp-selfcheck` was not run (no language server needed by this card). The window starts and stays up (6 s smoke run).
- **Manual** (written, not run): `docs/testing/phase-15-manual-tests.md` "Gallery setup" describes how to run the Gallery on macOS and Ubuntu.
- **Linked files:** the 20 files listed above compile unchanged in both Galleries. To make them compile, `LoomEditor.Core` and `LoomEditor.View` also expose their
  internals to `LoomEditor.Gallery.Avalonia` (`InternalsVisibleTo`, like `LoomEditor.Gallery`); no change to a WinUI Gallery file was needed.
- **Deviations:** the WinUI "Comment threads" diagnostics line (`CommentZonesDiagnostics`, an internal of the WinUI control) has no Avalonia counterpart and is not shown.
  The self-checks run on the main window's `Opened` event (the WinUI ones on `Activated`). `--uia-selfcheck` and the "UIA self-check" button print
  `uia self-check: not available on Avalonia`. The Comment threads gutter-key (Ctrl+Alt+M) is a key handler on the editor. The font picker has Menlo and DejaVu Sans Mono
  next to the Windows fonts, and the first entry restores the control's default font. The staging glyphs fall back to "+" and "✕" outside Windows
  (the default glyph font, Segoe Fluent Icons, exists only there). The Gallery project is in `LoomEditor.slnx` and `LoomEditor.Portable.slnf`.

## Out of scope

Native AOT publish in CI (P15-32), packages (P15-33).

## Suggested commit message

`feat(gallery): the Avalonia Gallery`
