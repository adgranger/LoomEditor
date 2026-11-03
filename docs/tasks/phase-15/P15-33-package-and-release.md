# P15-33 — The `LoomEditor.Avalonia` package, package test, release workflow

- **Status:** todo
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

- [ ] `./eng/Test-Packages.ps1` passes locally (Windows) with the Avalonia app
- [ ] CI: the Linux package test is green (link)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Publishing to nuget.org.

## Suggested commit message

`build: pack and test the LoomEditor.Avalonia package`
