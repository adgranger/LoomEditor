# P15-34 — Close phase 15: parity, how-to, docs, CHANGELOG

- **Status:** done
- **Depends on:** P15-33
- **Size:** S (docs)

**Recommended model:** Sonnet

## Goal

Phase 15 is closed. API parity is complete, a host knows how to use LoomEditor in an Avalonia app, and every doc tells the truth about what was verified.

## Sources

Sources: none (specified in this card). Read first: `docs/tasks/phase-14/P14-06-close-phase-14.md` (how a phase is closed), `README.md`, `CHANGELOG.md`, `docs/ARCHITECTURE.md`,
`docs/PORTING.md`, `docs/roadmap/phase-15-avalonia.md`, every Result section of the phase 15 cards.

## Files to create or change

- `tests/LoomEditor.Architecture.Tests/AvaloniaParityPending.txt` — must be empty; delete the file and the "pending" part of the test (the test becomes "no missing member"
  apart from the exceptions)
- `docs/how-to/use-the-avalonia-control.md` — create: package, `LoomEditorStyles`, a minimal window, the differences from WinUI (substitution table of
  [README](README.md) decision 3, `GlyphFontFamily`, macOS keys), Native AOT notes, the Oniguruma RIDs
- `README.md` — supported platforms, the two packages
- `docs/ARCHITECTURE.md` — a "Avalonia control (phase 15)" section; the "Where do I find…?" rows for Avalonia
- `docs/PORTING.md` — check that every map row with a WinUI path has its Avalonia path; "Behavior differences" complete
- `docs/adr/0016-avalonia-backend.md` — "Consequences" with what was verified (CI runs, self-check counts) and what was **not** (the manual plan)
- `docs/roadmap/phase-15-avalonia.md` — tick the checklist
- `CHANGELOG.md` — the phase 15 entry under "Unreleased"
- `CLAUDE.md` — the layer rule line includes Avalonia: `WinUI | Avalonia → Rendering → View → (TextMate, Themes) → Core`

## Design decisions (already made — do not change)

- The how-to only states what tests or CI proved; everything else is marked "not verified" with a link to the manual plan.

## Tests

- [x] `AvaloniaApiParityTests` passes with no pending list

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Result

- `AvaloniaParityPending.txt` is deleted; the test is now `MissingMembers_AreOnlyTheExceptions`. The two last pending keys (`FindWidget.Connect`, `FindWidget.GetBindingConnector`) are WinUI XAML compiler output
  and are now in `AvaloniaParityExceptions.txt` with that reason.
- New: `docs/how-to/use-the-avalonia-control.md`. Updated: `README.md`, `docs/ARCHITECTURE.md`, `docs/PORTING.md` (the nine map rows that named a WinUI path only now name the Avalonia one),
  `docs/adr/0016-avalonia-backend.md` (verified / not verified, with the CI runs), `docs/roadmap/phase-15-avalonia.md`, `CHANGELOG.md`, `CLAUDE.md`.
- Also fixes the status of P15-33 in `docs/tasks/README.md` (left `todo` by the previous commit).
- Not verified (written in the docs): the manual plan, `osx-x64` and `linux-arm64` Native AOT, the package test on macOS.

## Out of scope

A release (`docs/RELEASING.md` is a separate, manual step).

## Suggested commit message

`docs: close phase 15 (Avalonia control)`
