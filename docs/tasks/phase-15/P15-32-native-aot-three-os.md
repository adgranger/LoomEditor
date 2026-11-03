# P15-32 — Native AOT on Windows, macOS and Linux

- **Status:** todo
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

- [ ] The three `aot-avalonia` jobs are green (link the run in the Result, with the self-check counts)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery.Avalonia -c Release -r win-x64 -p:PublishAot=true` — 0 warnings locally
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Signing and notarizing macOS apps (a host concern).

## Suggested commit message

`ci: Native AOT publish and self-checks of the Avalonia Gallery on three OSes`
