# P11-21 — Close phase 11

- **Status:** todo
- **Depends on:** all phase 11 cards
- **Size:** S

## Goal

The documentation matches what phase 11 delivered: the roadmap checklist shows what is done, the architecture overview, the glossary, the ADR and the how-to say what exists, what differs from the plan and what was not verified.

## Sources

Sources: none (documentation only). Pattern: `docs/tasks/phase-10/P10-31-close-phase-10.md` (read it and follow its structure and honesty rules).

## Files to create or change

- `docs/roadmap/phase-11-inline-comments.md` — change: tick each item with its card ids, copy the "Out of scope" list as "Not delivered in phase 11", add "Status of the done-when line" (built and covered by automated tests vs. not run manually vs. Native AOT result cited from P11-20) and "Open risks"; replace "Not started" in the first paragraph
- `docs/adr/0014-inline-comments.md` — change: "Changes during implementation": every decision a card changed, with the date (read each card's Result section)
- `docs/ARCHITECTURE.md` — change: a section "Inline comment threads" (≤ 25 lines: the data in Core, logic in View, zone kind `Widget`, the overlay host, the key routing rule, the diff editor behavior, what is not delivered) and the "Where do I find…?" rows
- `docs/PORTING.md` — change: a short section "Phase 11 (not a port)" listing the new folders and saying there is no Monaco source; **no** rows in the Monaco → LoomEditor map; test counts are **counted** (`dotnet test --list-tests` per project), not copied
- `docs/GLOSSARY.md` — change: add *comment thread*, *view zone (widget kind)*, *suggestion block*, *composer*
- `docs/how-to/use-comment-threads.md` — create (30–45 lines: add a thread, the four events and the host's job, a suggestion example, the gutter button, the diff editor, limits)
- `docs/how-to/use-the-diff-editor.md` — change: link to the new how-to; the paragraph of P11-18 stays
- `src/LoomEditor.View/Markdown/README.md` — check it matches P11-02 (no change expected)
- `docs/tasks/README.md` — change: statuses and a "Deferred on purpose" paragraph for phase 11 (copy the phase README's out-of-scope list, shortened)
- `README.md` (root) — change only if it has a feature list

## Design decisions (already made — do not change)

- No code change in this card. A problem found while writing the docs becomes a follow-up card in the index (status `todo`, a "Follow-ups" step of phase 11), not a fix.
- **Honesty rules** (same as P7-26, P8-39, P9-33, P10-31): test counts are counted; "not verified" items are listed as such: the manual plan (not run by the agents), Narrator / NVDA / Accessibility Insights, IME composition in the widget, high contrast, the diff editor alignment under heavy typing, the Native AOT result (cite P11-20's Result section exactly); never write "verified" where a card says it did not run.
- The roadmap checklist uses the date of the last card.
- Update any earlier document that says comment widgets, peek views or zone widgets are "not delivered" (the "Marker navigation widget" row of the phase 10 "Later" table needs zone widgets: add "(zone widgets exist since phase 11, ADR 0014)" without ticking anything).
- Every relative link in new and changed docs is opened once.

## Tests

- [ ] None (documentation).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings (unchanged)
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (unchanged)
- [ ] Every relative link in the new and changed docs resolves
- [ ] The AOT result in the roadmap file cites P11-20, or says `NOT verified`; never ticked if not run
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Phase 12 (Avalonia), running the manual plan, any code change.

## Suggested commit message

`docs: close phase 11`
