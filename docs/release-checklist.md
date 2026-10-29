# Release checklist

Everything that must be true before the first tag. Commands and the one-time setup are explained in
[RELEASING.md](RELEASING.md); this file only collects them in one place.

Boxes marked **HUMAN** are for the user; an agent never ticks them and never records a result it did not observe.

Release: v____

## 1. Automated gates

An agent or the user may run them. Tick with the date and the commit hash.

- [ ] `rtk dotnet build LoomEditor.slnx -c Release` — 0 warnings (date / commit: ________)
- [ ] `rtk test dotnet test --solution LoomEditor.slnx -c Release` — all green (date / commit: ________)
- [ ] `rtk proxy pwsh -NoProfile -File eng/Invoke-ReleaseBuild.ps1 -Version <version>` (date / commit: ________)
- [ ] The CI run of the latest `main` commit is green (jobs "Build and test", "Native AOT publish" for `win-x64` and
  `win-arm64`; "Pack NuGet packages" runs on `main` pushes only)

## 2. Manual tests of the AOT gallery (HUMAN)

Source of truth: the `docs/testing/phase-<n>-manual-tests.md` files (phases 3 to 12). Run them on the AOT-published
gallery, not a Debug build.

- [ ] **HUMAN:** run on the AOT-published gallery (`dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64`)
- [ ] **HUMAN:** every item of `docs/testing/phase-3-manual-tests.md` to `phase-12-manual-tests.md` that is still
  unticked is ticked or consciously deferred (list the deferred ones here: ________)

## 3. Packages on a clean machine (HUMAN)

On a Windows 11 machine or VM without Visual Studio, outside the repository:

- [ ] **HUMAN:** take `artifacts/packages` of the dry run, build and run a tiny app that references
  `LoomEditor.WinUI` from that folder (the published `LoomEditor.PackageTest` of `eng/package-test` is such an app:
  copy its published folder and start the exe): a window opens and shows the editor with its text
- [ ] **HUMAN:** the README of each package is readable
- [ ] **HUMAN:** `LoomEditor.Core` alone works from a console app (create a `TextModel`, read a line)

## 4. Repository content (agent can prepare, HUMAN confirms)

- [ ] **HUMAN:** `CHANGELOG.md`: the `## [Unreleased]` section is renamed to the version (RELEASING.md section 3)
- [ ] **HUMAN:** `README.md`: status and install section are correct for this version
- [ ] **HUMAN:** `LICENSE` and `NOTICE.md` are present
- [ ] **HUMAN:** no secret or personal path in the repository (review the output of
  `rtk git grep -n -i "api[_-]key\|password\|C:\\Users"`)

## 5. External setup (HUMAN, RELEASING.md section 2)

- [ ] **HUMAN:** nuget.org account with 2FA
- [ ] **HUMAN:** the six ids are free (`LoomEditor.Core`, `LoomEditor.TextMate`, `LoomEditor.Themes`, `LoomEditor.View`,
  `LoomEditor.Rendering`, `LoomEditor.WinUI`)
- [ ] **HUMAN:** ID prefix reservation `LoomEditor.` requested (optional)
- [ ] **HUMAN:** Trusted Publishing policy created (owner `adgranger`, repository `LoomEditor`, workflow
  `release.yml`, environment `nuget`, scope "Push new packages and package versions"); no API key exists
- [ ] **HUMAN:** GitHub environment `nuget` with a required reviewer and the secret `NUGET_USER` (nuget.org profile
  name)
- [ ] **HUMAN:** tag ruleset on `v*`
- [ ] **HUMAN:** branch ruleset on `main`

## 6. Dry run (HUMAN)

- [ ] **HUMAN:** Release workflow run with `version` = the version: green, twelve files in the `nuget-packages`
  artifact, `publish` skipped

## 7. Go

- [ ] **HUMAN:** the tag is created by the user (RELEASING.md section 4); nobody else creates it
