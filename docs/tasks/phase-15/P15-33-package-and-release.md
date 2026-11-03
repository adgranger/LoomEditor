# P15-33 — The `LoomEditor.Avalonia` package, package test, release workflow

- **Status:** in progress
- **Depends on:** P15-32
- **Size:** S

**Recommended model:** Sonnet

## Goal

`eng/Pack.ps1` produces `LoomEditor.Avalonia.<version>.nupkg`. A package test app that uses only the packages builds and is AOT-published on Windows and Linux. The release
workflow packs and tests it with the others.

## Sources

Sources: none (specified in this card). Read first: `eng/Pack.ps1`, `eng/Test-Packages.ps1`, `eng/package-test/` (the WinUI package test app), `.github/workflows/release.yml`,
`docs/RELEASING.md`, `docs/release-checklist.md`.

## Files to create or change

- `eng/Pack.ps1` — add `LoomEditor.Avalonia`
- `eng/package-test/LoomEditor.PackageTest.Avalonia/` — create: an Avalonia app that references only `LoomEditor.Avalonia` (package) and calls every public member group once (editor
  text, a decoration, a host view zone, `LoomDiffEditor.Changes`, `SetSelection`), and exits 0. It is the twin of the WinUI package test app
- `eng/Test-Packages.ps1` — pack, then publish and run the Avalonia package test app (`win-x64` locally; on CI also `linux-x64` from the `portable` Linux job or a new step)
- `.github/workflows/ci.yml` / `release.yml` — the Avalonia package test on Linux; the TextMate package checked for its six `runtimes/*/native` entries
- `docs/RELEASING.md`, `docs/release-checklist.md` — the new package, the RIDs, what CI covers (P15-32)

## Design decisions (already made — do not change)

- Same version as the other packages; same metadata (licence, icon, readme, repository). `PackageTags` add `avalonia;macos;linux;cross-platform`.
- No package is published by this card (publishing stays the manual step of `docs/RELEASING.md`).

## Tests

- [x] `./eng/Test-Packages.ps1` passes locally (Windows) with the Avalonia app
- [ ] CI: the Linux package test is green (link)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Result

- `eng/Pack.ps1` packs `LoomEditor.Avalonia` (7 packages; `LoomEditor.WinUI` only on Windows), checks its dependencies (`LoomEditor.Rendering`, `Avalonia`) and, with `-RequireAllNatives`, the six
  `runtimes/*/native` entries of TextMate (the local run needs the win-x64 library only; before, it also needed win-arm64, which this machine has not built).
  `src/LoomEditor.Avalonia/README.md` is new (the readme check needs it); `PackageTags` has `macos;linux;cross-platform`.
- `eng/package-test/LoomEditor.PackageTest.Avalonia`: a headless console app (Avalonia.Headless + Skia + the package) that uses the editor host API and the diff editor once and exits 0.
  `eng/Test-Packages.ps1` publishes it with Native AOT for the runtime of the machine and runs it; the WinUI app runs on Windows only. Local Windows run: both apps pass, 0 warnings.
- CI: new job `package-avalonia` (ubuntu); the pack job and `release.yml` pass `-RequireAllNatives`; `release.yml` pushes `LoomEditor.Avalonia` before `LoomEditor.WinUI`. Docs: `RELEASING.md`, `release-checklist.md`.
- **Not verified yet:** the Linux CI job `package-avalonia` (link to add when it has run).

## Out of scope

Publishing to nuget.org.

## Suggested commit message

`build: pack and test the LoomEditor.Avalonia package`
