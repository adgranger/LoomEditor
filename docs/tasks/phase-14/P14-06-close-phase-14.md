# P14-06 — Close phase 14: package test, how-to, CHANGELOG

- **Status:** todo
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

- [ ] Package test and AOT self-check pass (as in P13-10).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Native AOT publish — 0 warnings
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Releasing (tagging) a version.

## Suggested commit message

`docs: close phase 14 (diff line and block actions)`
