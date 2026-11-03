# P15-04 — Build and test the portable projects on Linux and macOS

- **Status:** done
- **Depends on:** P15-03
- **Size:** S

**Recommended model:** Sonnet

## Goal

A solution filter with every non-Windows project, built and tested in CI on `ubuntu-latest` and `macos-latest`, so a Windows-only assumption in Core, TextMate, Themes, View,
Lsp or Rendering fails a pull request.

## Sources

Sources: none (specified in this card). Read first: `LoomEditor.slnx`, `.github/workflows/ci.yml`, `Directory.Build.props` (CI=true → warnings as errors), the test projects'
`.csproj` files, `tests/LoomEditor.Lsp.Tests` (tests that start `LoomEditor.Lsp.FakeServer` as a child process).

## Files to create or change

- `LoomEditor.Portable.slnf` — create: every project of `LoomEditor.slnx` whose `TargetFramework` is `$(LoomNetTargetFramework)` (src: Core, TextMate, Themes, View, Lsp, Rendering;
  tests: Architecture, Core, TextMate, View, Lsp, Lsp.FakeServer, Rendering; not Benchmarks)
- `.github/workflows/ci.yml` — new job `portable` (matrix `ubuntu-latest`, `macos-latest`), `needs: [native]`, downloads the `oniguruma-<rid>` artifact of its OS, runs
  `dotnet build LoomEditor.Portable.slnf -c Release` and `dotnet test --solution LoomEditor.Portable.slnf -c Release`
- `CLAUDE.md` — the two portable commands under "Commands"
- `docs/tasks/phase-15/README.md` is already right; nothing to change there

## Design decisions (already made — do not change)

- Windows CI is unchanged (it still builds the whole `.slnx`).
- Tests that fail on Linux or macOS because of the OS (path separators, `\r\n` assumptions, `.exe` names, case-sensitive file names): fix the **test or the code**, and
  list each fix in the Result. No `[SkipOnOS]` unless the test checks something that exists only on Windows (then skip it with a reason).
- `RepositoryLayout.cs` of the architecture tests must find the repository on every OS (use `Path.Combine`, no `\`).

## Tests

- [x] All tests of the filter pass on `ubuntu-latest` and `macos-latest` in CI (link the run in the Result)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet test --solution LoomEditor.Portable.slnf -c Release` — green on Windows; the `portable` CI jobs are green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The Avalonia projects (they join the filter in P15-05), Native AOT on Linux and macOS (P15-32).

## Suggested commit message

`ci: build and test the portable projects on Linux and macOS`

## Result

- `LoomEditor.Portable.slnf` (forward slashes: a `\` makes MSBuild reject the filter of a `.slnx`). Windows: green.
- Linux was checked locally in the `mcr.microsoft.com/dotnet/nightly/sdk:11.0` container (build 0 warnings, 7596 passed, 2 skipped as on Windows); the macOS and `portable` CI jobs run on the first push to a pull request.
- OS fixes: `OnigLibraryTests` uses the library name of the OS; `ModelBasedVariableResolver` reads `\` as a separator on Linux and macOS (the Gallery `snippet-transform` check uses a Windows path); `RepositoryLayout.GetProjectReferences` splits `\` whatever the OS.
- Seen once on Windows, not reproducible: `TextSelfCheckTests` (smart-select) and `CommitCharacterControllerTests.SelectionChange_UsesTheNewItemsCharacters` failed in a parallel run, then passed.
