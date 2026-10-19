# Oniguruma (native)

[Oniguruma](https://github.com/kkos/oniguruma) is the regular-expression engine used by TextMate grammars
(and by VS Code, through `vscode-oniguruma`). License: BSD-2-Clause (see NOTICE.md).

We do **not** vendor its sources: `build.ps1` downloads a pinned release, checks its SHA-256 and builds `onig.dll`.

```powershell
./src/native/oniguruma/build.ps1 -Architecture x64     # -> artifacts/native/win-x64/onig.dll
./src/native/oniguruma/build.ps1 -Architecture ARM64   # -> artifacts/native/win-arm64/onig.dll
```

> ⚠️ Upstream ended the project on 2025-04-24 (the repository is archived). We pin the last release (6.9.10).
> See docs/adr/0001-baseline.md for the consequences and the fallback plan.

## How the DLL reaches the apps

`Oniguruma.targets` (imported by `LoomEditor.TextMate.csproj`) does it all:

- **Build on demand.** If `artifacts/native/win-<arch>/onig.dll` is missing, the build of `LoomEditor.TextMate` runs
  `build.ps1` first (`pwsh`, or `powershell.exe` when `pwsh` is not installed). CI downloads the DLLs built and cached
  by its `native` job instead.
- **Architecture.** From `$(RuntimeIdentifier)` (`win-x64`, `win-arm64`), then `$(Platform)` (`x64`, `ARM64`), then the
  architecture of the build machine. `LoomEditor.TextMate` is an AnyCPU library: through a project reference it gets
  neither, so it ships the DLL of the build machine (an ARM64 app built on an x64 machine from this repository gets the
  x64 DLL). NuGet consumers are not affected: they get `runtimes/win-<arch>/native/onig.dll` for their own RID.
- **Copy.** The DLL is copied next to the assemblies of every project that references TextMate (tests, Gallery) and is
  published with Native AOT (checked by P4-01, ADR 0008 decision 7).
- **Pack.** The package contains `runtimes/win-<arch>/native/onig.dll` for every architecture present in
  `artifacts/native`.
- The default DLL probing finds `onig.dll` next to the app: no `NativeLibrary.SetDllImportResolver`.

The managed side is in `src/LoomEditor.TextMate/Oniguruma/` (`[LibraryImport]` only, behind `IRegexEngine`).
