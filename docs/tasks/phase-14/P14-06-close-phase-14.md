# P14-06 — Close phase 14: package test, how-to, CHANGELOG

- **Status:** done
- **Depends on:** P14-05
- **Size:** S

**Recommended model:** Sonnet

## Goal

The new public members are proven from the packages (the package test app calls each one), documented in the diff editor how-to, and listed in the CHANGELOG; the
roadmap checklist is ticked.

## Sources

Sources: none (specified in this card). Read first: `docs/tasks/phase-13/P13-10-native-aot-and-package-check.md` and `P13-11-close-phase-13.md` (the same steps),
`docs/how-to/use-the-diff-editor.md`, `CHANGELOG.md`, `docs/roadmap/phase-14-diff-actions.md`.

## Files to create or change

- The package test app and the Native AOT self-check named in P13-10 — change: call every phase 14 public member
- `docs/how-to/use-the-diff-editor.md` — change: section "Line and block actions (staging)" with a VIGO-style example (block action → hunk, `SelectedLines` → partial
  patch, the start-line rule for empty sides)
- `CHANGELOG.md` — `## [Unreleased]` / `### Added`: one line per feature (`Changes`; block action buttons; line selection and line action)
- `docs/roadmap/phase-14-diff-actions.md` — tick the checklist; `docs/tasks/README.md` — phase status

## Design decisions (already made — do not change)

- `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` must succeed with 0 AOT/trim warnings.
- The how-to lists the final public API (the phase README section) as shipped; any name that changed during the phase is fixed in the phase README too.

## Tests

- [x] Package test and AOT self-check pass (as in P13-10).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Native AOT publish — 0 warnings
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Releasing (tagging) a version.

## Suggested commit message

`docs: close phase 14 (diff line and block actions)`

## Result

Native AOT publish (`dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror`) succeeded with 0 AOT/trim warnings. The
host API self-check gained a sixth check, `change-blocks` (`DiffChangeBlock.FromMappings` on a 4-line pair, its properties and inner change, and the pair toggle of
`DiffLineSelection`); the WinUI members stay with the package test. Published exe, last lines of each report:

```
hostapi:  PASS change-blocks — insertion 1,0 -> 1,1, pair 2,1 -> 3,1 (cat -> dog), deletion 4,1 -> 5,0; the pair toggles together
          6 of 6 checks passed            (exit code 0)
comments: 17 of 17 checks passed          (exit code 0)
text:     10 of 10 checks passed          (exit code 0)
diff:     SUMMARY: 9 passed, 0 failed     (exit code 0)
```

Package test: `UseDiffActions()` in the package test app touches every phase 14 public member (a `LoomDiffEditor` that is never shown); `./eng/Test-Packages.ps1`
printed `AOT smoke test passed: LoomEditor.PackageTest (win-x64), alive after 10 s.` then `Package test passed: 0.2.0-alpha`. Same caveat as P13-10: no MSVC ARM64
tools locally, so a placeholder `artifacts/native/win-arm64/onig.dll` (a copy of the x64 one, git-ignored) was used for this run and deleted afterwards.

Build 0 warnings; tests 7 590 (7 588 passed, 2 skipped). One full run had a single failure in `SuggestControllerTests.Suggest_ShowsTheList_AndSelectsTheFirst`
(untouched code); it passed 3 times alone and the next full run was green — a flaky test, not fixed here. The phase README's public API names all match the
`PublicAPI.Unshipped.txt` files; a short "as shipped" note lists what the block did not show (the `…Property` fields, the event args constructors). The raw
private-use glyphs of P14-04 and P14-05 are now written `\uE73E`, `\uE710`, `\uE7A7`.
