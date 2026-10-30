# P13-11 — Close phase 13: the host API how-to, CHANGELOG and docs

- **Status:** done
- **Depends on:** P13-10 (and so every phase 13 card)
- **Size:** S

**Recommended model:** Sonnet

## Goal

A host developer finds, in one short how-to, how to build VIGO's features with the public API (blame column, highlights with their own styles, language of a file,
unified diff with hidden lines, view zones and gutter clicks). The CHANGELOG `[Unreleased]` section lists the new public members, the roadmap checklist is ticked with
card ids, the design is recorded in an ADR, and what was not verified is said plainly.

## Sources

Sources: none (documentation only). Pattern: `docs/tasks/phase-12/P12-15-close-phase-12.md` (structure and honesty rules). Read the Result section of every card
P13-01 … P13-10, [phase-13/README.md](README.md), `docs/how-to/use-comment-threads.md` and `use-the-diff-editor.md` (tone and length of a how-to), `CHANGELOG.md`
(Keep a Changelog format), `docs/RELEASING.md` (what the release reads from `[Unreleased]`).

## Files to create or change

- `docs/how-to/use-the-host-api.md` — create
- `CHANGELOG.md` — change (`## [Unreleased]`)
- `docs/adr/0015-public-host-api.md` — create
- `docs/roadmap/phase-13-host-api.md` — change (checklist, status, risks)
- `docs/ARCHITECTURE.md` — change (≤ 8 lines: "Public API for host applications", pointing to the how-to and the ADR)
- `docs/PORTING.md` — change (a short section "Phase 13 (not a port)": the new files and the **re-counted** test numbers, `dotnet test --list-tests` per project)
- `docs/GLOSSARY.md` — change (*host*, *host decoration style*, *host view zone*)
- the README of each package whose public API grew (`src/LoomEditor.WinUI/README.md`, `src/LoomEditor.View/README.md`, `src/LoomEditor.TextMate/README.md` if they list
  features) — change (one line each, only if the file lists features)
- `docs/tasks/README.md` — change (statuses)

## Design decisions (already made — do not change)

- **No code change.** A problem found while writing becomes a follow-up card (status `todo`, a "Follow-ups" step of phase 13), not a fix.
- **How-to** (`use-the-host-api.md`, ≤ ~150 lines, English, short code blocks that compile against the public API only): one section per VIGO need, in this order —
  *Language of a file* (`BuiltInLanguages.GetLanguageIdByFileName`, fall back to `"plaintext"`); *Highlight lines* (`EditorDecorationOptions` reused, `EditorDecoration`,
  `CreateDecorationsCollection`, `Set` / `Clear`; the list of useful built-in classes: `line-insert`, `line-delete`, `char-insert`, `char-delete`, `rangeHighlight`);
  *Your own styles* (`CustomDecorationStyles.Register` with light / dark colors, global like CSS, register before use, name prefix); *A column beside the editor* (the redraw
  loop of the Gallery's blame column: `ScrollChanged`, `LayoutChanged`, `GetTopForLineNumber(line, includeViewZones: true) - ScrollTop`, `LineHeight`,
  `LineNumbersEnabled = false`); *Reload a file* (`ResetText` vs `Text`); *Unified diff and staging* (`SetHiddenAreas`, `GutterClicked` with `Handled`, a staged bar with
  `LinesDecorationsClassName`); *Content between lines* (`AddViewZone` / `RemoveViewZone`, focus and theme are the host's job); *The diff editor* (use
  `Original` / `Modified`; host zones and hidden areas are not supported on its sides). End with "What stays internal and why" (3–5 lines, from README section 2).
- **CHANGELOG** `## [Unreleased]` → `### Added`: one bullet per package (`LoomEditor.TextMate`, `LoomEditor.View`, `LoomEditor.WinUI`) naming the new public members; one
  `### Changed` bullet if any public member of `0.1.0-alpha.1` changed (there should be none: the renamed members were internal — check the PublicAPI diffs since tag
  `v0.1.0-alpha.1` and write "none" in the card's Result section if so). Do not touch the released sections or the compare links.
- **ADR 0015** (≤ 60 lines, the format of the other ADRs): context (VIGO, internal members seen in the XML docs), decision (facade, not blanket public; the table of
  README section 2 in short; global styles; no `LoomDiffEditor` members), consequences (what a host can and cannot do; the XML docs of the packages still list internal
  members — open risk), and every decision a phase 13 card changed (read each Result section).
- **Roadmap**: tick each checklist item with its card id; "Status of the done-when line" separates automated tests, the Native AOT self-check and package test (cite
  **P13-10**'s Result section exactly), and what is not verified (the manual plan — not run by agents —, Narrator, high contrast, a host `TextBox` in a zone, VIGO itself).
- Honesty rules of P11-25 / P12-15: counted test numbers; never "verified" where a card says it did not run; every relative link in new and changed docs is opened once.

## Tests

- [x] None (documentation).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings (no code change, but the docs folder is part of the solution checks)
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Every relative link in the new and changed docs resolves
- [x] The roadmap checklist is ticked with card ids; `docs/tasks/README.md` shows every phase 13 card `done`
- [x] Card status updated

## Out of scope

Running the manual plan, releasing a package, stripping internal members from the XML docs, any code change.

## Suggested commit message

`docs: close phase 13, the public API for host applications`

## Result

Docs only. How-to, ADR 0015, CHANGELOG `[Unreleased]`, roadmap, ARCHITECTURE, PORTING (re-counted tests), GLOSSARY and the View README written. Changed public members since `v0.1.0-alpha.1`: none (the three `PublicAPI.Unshipped.txt` files only gained lines). Not verified: the manual plan, Narrator, high contrast, a host TextBox in a zone, VIGO itself.
