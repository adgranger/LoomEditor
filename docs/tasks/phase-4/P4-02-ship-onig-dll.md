# P4-02 — Build and ship `onig.dll` with `LoomEditor.TextMate`

- **Status:** todo
- **Depends on:** P4-01
- **Size:** S (MSBuild + CI, no C# beyond one smoke test)

## Goal

Every build of `LoomEditor.TextMate` has `onig.dll` for its architecture: it is built on demand, copied next to the
assemblies of every project that references TextMate (tests, Gallery), published with Native AOT, and packed in the NuGet
package as a native asset.

## Sources

None to port. `src/native/oniguruma/build.ps1` and `README.md`; `.github/workflows/ci.yml` (job that builds and caches
`onig.dll`, lines 58–72); the findings of P4-01 in ADR 0008 (decision 7).

## Files to create or change

- `src/native/oniguruma/Oniguruma.targets` — create
- `src/LoomEditor.TextMate/LoomEditor.TextMate.csproj` — import it
- `.github/workflows/ci.yml` — change: the build/test jobs download the cached `onig.dll` artifact into
  `artifacts/native/win-<arch>/` before `dotnet build`
- `tests/LoomEditor.TextMate.Tests/Oniguruma/OnigLibraryTests.cs` — create
- `src/native/oniguruma/README.md`, `NOTICE.md` (Oniguruma BSD-2-Clause entry: check it is there, add it if not)

## Design decisions (already made — do not change)

- The DLL lives in `artifacts/native/win-<arch>/onig.dll` (the output of `build.ps1`, git-ignored). `<arch>` is `x64` or
  `arm64`, from `$(Platform)` / `$(RuntimeIdentifier)`; when neither says, use the architecture of the build machine.
- `Oniguruma.targets` has a target `LoomBuildOniguruma`, `BeforeTargets="Build"`, with
  `Condition="!Exists('…/onig.dll')"`, that runs `pwsh -NoProfile -File build.ps1 -Architecture <arch>` (fall back to
  `powershell.exe` when `pwsh` is not found). If it fails, the build fails with the script's message.
- The DLL is an item with `Pack="true"`, `PackagePath="runtimes/win-<arch>/native"`, `CopyToOutputDirectory="PreserveNewest"`,
  and `Link="onig.dll"`, so that project references and package references both get it next to the app. Use exactly the
  MSBuild items P4-01 recorded for the AOT publish.
- No `[DllImport]`, no `NativeLibrary.SetDllImportResolver`: the default probing finds `onig.dll` next to the app.

## Tests

- [ ] New: `OnigLibrary_IsNextToTheTestAssembly` (`File.Exists(Path.Combine(AppContext.BaseDirectory, "onig.dll"))`)
- [ ] New: `OnigLibrary_LoadsAndExportsVersion` (`NativeLibrary.TryLoad` + `NativeLibrary.TryGetExport(…, "onig_version")`)

## Acceptance criteria

- [ ] After deleting `artifacts/native`, `dotnet build LoomEditor.slnx -c Release` rebuilds the DLL — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — `onig.dll` is in the publish folder, 0 warnings
- [ ] `dotnet pack src/LoomEditor.TextMate -c Release` — the package contains `runtimes/win-x64/native/onig.dll`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The interop code (P4-03). ARM64 is built in CI only (the local machine has no ARM64 build tools, ADR 0001).

## Suggested commit message

`build(textmate): build and ship onig.dll`
