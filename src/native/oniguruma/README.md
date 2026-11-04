# Oniguruma (native)

[Oniguruma](https://github.com/kkos/oniguruma) is the regular-expression engine used by TextMate grammars
(and by VS Code, through `vscode-oniguruma`). License: BSD-2-Clause (see NOTICE.md).

We do **not** vendor its sources: `build.ps1` (Windows) and `build.sh` (Linux, macOS) download the same pinned release,
check the same SHA-256 and build the library.

```powershell
./src/native/oniguruma/build.ps1 -Architecture x64     # -> artifacts/native/win-x64/onig.dll
./src/native/oniguruma/build.ps1 -Architecture ARM64   # -> artifacts/native/win-arm64/onig.dll
```

```bash
./src/native/oniguruma/build.sh --rid linux-x64     # -> artifacts/native/linux-x64/libonig.so   (build on an x64 machine)
./src/native/oniguruma/build.sh --rid linux-arm64   # -> artifacts/native/linux-arm64/libonig.so (build on an arm64 machine)
./src/native/oniguruma/build.sh --rid osx           # -> artifacts/native/osx/libonig.dylib      (universal x86_64 + arm64, macOS 12+)
```

`build.sh` needs CMake, a C compiler (Xcode command line tools on macOS), `curl` and `tar`. The Linux build uses `-fPIC -O2`
and no `-march`, so the library is portable. The library of another OS or architecture cannot be built on the current machine.

> ⚠️ Upstream ended the project on 2025-04-24 (the repository is archived). We pin the last release (6.9.10).
> See docs/adr/0001-baseline.md for the consequences and the fallback plan.

## How the DLL reaches the apps

`Oniguruma.targets` (imported by `LoomEditor.TextMate.csproj`) does it all:

- **Build on demand.** If `artifacts/native/<rid>/<library>` is missing (`onig.dll`, `libonig.so`, `libonig.dylib`), the
  build of `LoomEditor.TextMate` runs `build.ps1` on Windows (`pwsh`, or `powershell.exe` when `pwsh` is not installed) and
  `build.sh` elsewhere. CI downloads the libraries built and cached by its `native` job (one artifact `oniguruma-<rid>` per RID).
- **Platform.** The OS comes from `$(RuntimeIdentifier)`, else from the build machine. macOS has one universal dylib in
  `artifacts/native/osx`, packed under both `osx-x64` and `osx-arm64`.
- **Architecture.** From `$(RuntimeIdentifier)` (`win-x64`, `win-arm64`, `linux-x64`, ...), then `$(Platform)` (`x64`, `ARM64`), then the
  architecture of the build machine. `LoomEditor.TextMate` is an AnyCPU library: through a project reference it gets
  neither, so it ships the DLL of the build machine (an ARM64 app built on an x64 machine from this repository gets the
  x64 DLL). NuGet consumers are not affected: they get `runtimes/win-<arch>/native/onig.dll` for their own RID.
- **Copy.** The DLL is copied next to the assemblies of every project that references TextMate (tests, Gallery) and is
  published with Native AOT (checked by P4-01, ADR 0008 decision 7).
- **Pack.** The package contains `runtimes/<rid>/native/<library>` for every library present in `artifacts/native`
  (six RIDs in CI: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`).
- The default library probing finds `onig.dll`, `libonig.so` or `libonig.dylib` next to the app: no
  `NativeLibrary.SetDllImportResolver` (to be confirmed on Linux and macOS by the CI of P15-04).

The managed side is in `src/LoomEditor.TextMate/Oniguruma/` (`[LibraryImport]` only, behind `IRegexEngine`).
