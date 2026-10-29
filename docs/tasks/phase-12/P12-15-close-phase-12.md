# P12-15 — Close phase 12

- **Status:** todo
- **Depends on:** all phase 12 cards
- **Size:** S

**Recommended model:** Sonnet

## Goal

The documentation matches what phase 12 delivered: the roadmap checklist is ticked with card ids, ADR 0014 records the range design, the how-to and the architecture overview
explain ranges, and what was not verified is said plainly.

## Sources

Sources: none (documentation only). Pattern: `docs/tasks/phase-11/P11-25-close-phase-11.md` (follow its structure and honesty rules).

## Files to create or change

- `docs/roadmap/phase-12-comment-ranges.md` — change: tick each item, replace "Not started", add "Status of the done-when line" (automated tests vs. not run manually vs. the
  Native AOT result cited from P12-14's Result section) and "Open risks"
- `docs/adr/0014-inline-comments.md` — change: a section `## Phase 12: line ranges (YYYY-MM-DD)` (≤ 20 lines): the decisions of [phase-12/README.md](README.md) §2 in
  short, then every decision a phase 12 card changed (read each card's Result section); remove "Multi-line thread ranges" from "What is not delivered" and replace it with the
  phase 12 out-of-scope list
- `docs/ARCHITECTURE.md` — change: the "Inline comment threads" section gains ≤ 6 lines on ranges (`StartLineNumber` … `LineNumber`, the box under the last line, the highlight
  class, `CommentLineRange`)
- `docs/how-to/use-comment-threads.md` — change: a "Ranges of lines" paragraph with a 3-line C# example (`AddCommentThread(12, 15)`, answering `AddCommentRequested` with
  `e.StartLineNumber, e.LineNumber`) and the multi-line suggestion rule
- `docs/PORTING.md` — change: a short section "Phase 12 (not a port)" (new files, no Monaco source, **counted** test numbers: `dotnet test --list-tests` per project)
- `docs/GLOSSARY.md` — change: *comment range*, *range text*
- `docs/tasks/README.md` — change: statuses and a "Deferred on purpose" paragraph for phase 12 (copy the phase README's out-of-scope list, shortened)

## Design decisions (already made — do not change)

- No code change. A problem found while writing becomes a follow-up card (status `todo`, a "Follow-ups" step of phase 12), not a fix.
- Honesty rules of P11-25: counted test numbers; "not verified" lists the manual plan (not run by agents), Narrator / NVDA, the highlight in high contrast, word wrap, the
  diff editor; the AOT line cites P12-14 exactly; never "verified" where a card says it did not run.
- The roadmap checklist uses the date of the last card. Every relative link in new and changed docs is opened once.

## Tests

- [ ] None (documentation).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings (unchanged)
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (unchanged)
- [ ] Every relative link in the new and changed docs resolves
- [ ] The AOT result in the roadmap file cites P12-14, or says `NOT verified`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Phase 13 (Avalonia), running the manual plan, any code change.

## Suggested commit message

`docs: close phase 12`
