# P15-32 — Native AOT on Windows, macOS and Linux

- **Status:** done
- **Depends on:** P15-31
- **Size:** S (CI and fixes)

**Recommended model:** Sonnet

## Goal

CI publishes the Avalonia Gallery with Native AOT for `win-x64`, `linux-x64` and `osx-arm64` with zero trimming/AOT warnings. It starts each published app, and runs the text,
diff, comments and host API self-checks of the published binary.

## Sources

Sources: none (specified in this card). Read first: `.github/workflows/ci.yml` (job `aot-smoke-test`), `eng/Test-AotApp.ps1`, `docs/testing/phase-10-manual-tests.md`
(the self-check), [ADR 0001](../../adr/0001-baseline.md) (AOT rules).

## Files to create or change

- `.github/workflows/ci.yml` — new job `aot-avalonia` (matrix: `windows-latest`/`win-x64`, `ubuntu-latest`/`linux-x64`, `macos-latest`/`osx-arm64`), `needs: [native]`:
  download the `oniguruma-<rid>` artifact; `dotnet publish samples/LoomEditor.Gallery.Avalonia -c Release -r <rid> -p:PublishAot=true -warnaserror`; scan the log as the existing job;
  check `libonig.*` / `onig.dll` is next to the executable; run the self-checks (`--text-selfcheck=…`, `--diff-selfcheck`, `--comments-selfcheck`, `--hostapi-selfcheck`: use the
  exact flags of the Gallery); start the app for 10 s (`xvfb-run -a` on Linux) and fail if it exits
- `eng/Test-AotApp.ps1` — accept a non-Windows RID if the CI job uses it (`pwsh` exists on all three runners)
- Any AOT fix in `src/LoomEditor.Avalonia` (a trimming warning is a bug: fix it at its root, no `UnconditionalSuppressMessage` without a reason comment)

## Design decisions (already made — do not change)

- `osx-x64` and `linux-arm64` are packed (P15-03) but not AOT-published in CI (runner cost). This is written in `docs/RELEASING.md` by P15-33.
- Publish artifacts uploaded as `gallery-avalonia-<rid>`.

## Tests

- [x] The three `aot-avalonia` jobs are green (link the run in the Result, with the self-check counts)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery.Avalonia -c Release -r win-x64 -p:PublishAot=true` — 0 warnings locally (build first, then `publish --no-build`; see the Result)
- [ ] Card status and `docs/tasks/README.md` updated

## Result

- **Local (Windows, win-x64):** `dotnet build -r win-x64 -warnaserror` then `dotnet publish -r win-x64 -warnaserror --no-build` succeed with 0 warnings (ILC prints two
  "will always throw" notes for `Avalonia.Win32` DComposition methods; they are not `warning` lines). `onig.dll` is next to the exe. The published exe passes the
  self-checks: text 10 of 10, diff 9 passed / 0 failed, comments 17 of 17, host API 6 of 6, and stays alive after 10 s.
- A single `dotnet publish -r win-x64` (without the build first) fails with MSB3030 (the documentation file of the rid folder is not found): the CI job and the local command
  therefore build first and publish with `--no-build`, as `eng/Test-AotApp.ps1 -BuildFirst` does. `eng/Test-AotApp.ps1` is not changed: the new job does its own checks on all three systems.
- **CI:** job `aot-avalonia` added to `.github/workflows/ci.yml`; the three jobs are green in https://github.com/adgranger/LoomEditor/actions/runs/41654238868 (win-x64, linux-x64, osx-arm64):
  text 10 of 10, diff 9 passed / 0 failed, comments 17 of 17, host API 6 of 6 on each; the app stays alive 10 s. The first run failed on win-x64 only because `&` does not wait
  for a GUI exe: the job uses `Start-Process -Wait` and reads the exit code. In that run the macOS job of "Portable projects" failed on an existing Lsp test
  (`LspServerProcessTests.WorkingDirectory_Used`: `/var` vs `/private/var`), unrelated to this card.

## Out of scope

Signing and notarizing macOS apps (a host concern).

## Suggested commit message

`ci: Native AOT publish and self-checks of the Avalonia Gallery on three OSes`
