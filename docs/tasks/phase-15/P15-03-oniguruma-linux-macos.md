# P15-03 — Oniguruma for Linux and macOS

- **Status:** todo
- **Depends on:** P15-02
- **Size:** M (scripts and MSBuild)

**Recommended model:** Sonnet

## Goal

The same Oniguruma 6.9.10 builds as `libonig.so` (`linux-x64`, `linux-arm64`) and as a universal `libonig.dylib` (`osx-x64` + `osx-arm64`). `LoomEditor.TextMate` copies the right
one next to the assemblies and packs every one that is built. TextMate tests pass on Linux and macOS.

## Sources

Sources: none (specified in this card; [ADR 0007](../../adr/0007-textmate-regex-engine.md) decision 5, [ADR 0016](../../adr/0016-avalonia-backend.md) decision 8). Read first:
`src/native/oniguruma/build.ps1`, `Oniguruma.targets`, `README.md`, `src/LoomEditor.TextMate/Oniguruma/` (the `[LibraryImport]` library name), `.github/workflows/ci.yml` (job `native`).

## Files to create or change

- `src/native/oniguruma/build.sh` — create: same download URL, same SHA-256 check, same CMake options as `build.ps1` (static runtime off, shared library on);
  `--rid linux-x64|linux-arm64|osx` → `artifacts/native/<rid>/libonig.so`, or `artifacts/native/osx/libonig.dylib` built with `CMAKE_OSX_ARCHITECTURES="x86_64;arm64"`
- `src/native/oniguruma/Oniguruma.targets` — the RID becomes `<os>-<arch>`: OS from `$(RuntimeIdentifier)`, else the build machine
  (`$([MSBuild]::IsOSPlatform('Linux'))`, `('OSX')`); file name `onig.dll` / `libonig.so` / `libonig.dylib`; the missing-library target runs `build.ps1` on Windows and `build.sh` elsewhere;
  pack every built RID: `runtimes/win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64` (the universal dylib packed under both osx RIDs)
- `src/native/oniguruma/README.md` — how to build on each OS (CMake and a C compiler; Xcode command line tools on macOS)
- `.github/workflows/ci.yml` — job `native`: matrix gains `ubuntu-latest` (`linux-x64`), `ubuntu-24.04-arm` (`linux-arm64`), `macos-latest` (`osx`); cache key also hashes `build.sh`;
  artifacts named `oniguruma-<rid>`
- `.github/workflows/release.yml` — downloads every `oniguruma-*` artifact before packing (check how it gets the Windows ones today and do the same)
- `docs/adr/0007-textmate-regex-engine.md` — decision 5: one sentence that links to ADR 0016 decision 8

## Design decisions (already made — do not change)

- No change to the C# interop: the `[LibraryImport]` name stays `"onig"`; .NET probes `libonig.so` / `libonig.dylib` itself. If it does not (test fails), add a
  `NativeLibrary.SetDllImportResolver` in the Oniguruma folder, AOT-safe, and say so in the Result.
- Same Oniguruma version, same tarball, same hash on every OS. No system `libonig` (distributions ship other versions).
- Linux build: `-fPIC`, `-O2`, no `-march` (portable). macOS minimum: `CMAKE_OSX_DEPLOYMENT_TARGET=12.0`.

## Tests

- [ ] Existing TextMate tests pass on Linux and macOS (checked by CI after P15-04; in this card, the `native` jobs must be green and the artifact must contain the library)
- [ ] `dotnet pack` of TextMate locally (Windows) still contains `runtimes/win-x64/native/onig.dll`; with the CI artifacts, the package contains all six RIDs (list in the Result)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `bash src/native/oniguruma/build.sh --rid linux-x64` works in WSL or a Linux container if one is available (else: CI only, said in the Result)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Running the .NET tests on Linux and macOS (P15-04), Native AOT on those OSes (P15-32).

## Suggested commit message

`build(textmate): build and pack Oniguruma for Linux and macOS`
