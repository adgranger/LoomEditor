# P12-15 — Close phase 12

- **Status:** todo (reopened on 2026-10-29: first done for steps A–D in commit `bb391b5`, reopened when step E — exact text ranges, P12-16 … P12-23 — was added)
- **Depends on:** P12-23 (and so every phase 12 card)
- **Size:** S

**Recommended model:** Sonnet

## Goal

The documentation matches what phase 12 delivered, **including the exact text ranges of step E**: the roadmap checklist is ticked with card ids, ADR 0014 records the
range design (lines and columns), the how-to and the architecture overview explain both kinds of threads, and what was not verified is said plainly.

**Reopened note.** The first close (`bb391b5`) already wrote every file below for whole-line ranges. This pass **revises** them; it does not add a second section. The closing
docs must mention column ranges: `CommentThread.StartColumn` / `EndColumn` / `IsWholeLines`, the `CommentRange` rule ([README](README.md) decision 15), the
`comment-thread-text` highlight, the `Range` overloads and `TextRange`, and the suggestion rule (decision 19: a suggestion replaces the whole lines that contain the range).
"Character-precise ranges" must leave every "not delivered" / "deferred" list.

## Sources

Sources: none (documentation only). Pattern: `docs/tasks/phase-11/P11-25-close-phase-11.md` (follow its structure and honesty rules). Read the Result section of every card
P12-16 … P12-23 and section 3 of [phase-12/README.md](README.md).

## Files to create or change

- `docs/roadmap/phase-12-comment-ranges.md` — change: add the step E items to the checklist and tick each with its card id; remove "Character-precise ranges" from
  "Not in phase 12"; update "Status of the done-when line" (automated
  tests vs. not run manually vs. the Native AOT result cited from **P12-23's** Result section, `17 of 17`) and "Open risks" (exact ranges: word wrap, high contrast, typing
  near the edges of the text)
- `docs/adr/0014-inline-comments.md` — change: rename the section to `## Phase 12: line and text ranges (YYYY-MM-DD)` (≤ 30 lines): the decisions of
  [phase-12/README.md](README.md) §2 and §3 in short (say that §3 superseded decision 1 and why), then every decision a phase 12 card changed (read each card's Result
  section); "What is not delivered" lists the updated phase 12 out-of-scope list (no "character-precise ranges")
- `docs/ARCHITECTURE.md` — change: the "Inline comment threads" lines on ranges (≤ 8 lines in all) mention both kinds, the columns, `CommentRange` (instead of
  `CommentLineRange`, deleted by P12-23) and the two highlight classes
- `docs/how-to/use-comment-threads.md` — change: the "Ranges of lines" paragraph becomes "Ranges of lines and exact text": keep the line example and add a 3-line one
  (`AddCommentThread(new Range(4, 5, 4, 10))`; answering `AddCommentRequested` with `e.TextRange is { } r ? AddCommentThread(r) : AddCommentThread(e.StartLineNumber,
  e.LineNumber)`), and state the suggestion rule (whole lines)
- `docs/PORTING.md` — change: the "Phase 12 (not a port)" section lists the step E files and the **re-counted** test numbers (`dotnet test --list-tests` per project)
- `docs/GLOSSARY.md` — change: *comment range* covers exact text; add *commented text*
- `docs/tasks/README.md` — change: statuses and the phase 12 "Deferred on purpose" paragraph (copy the phase README's updated out-of-scope list, shortened)

## Design decisions (already made — do not change)

- No code change. A problem found while writing becomes a follow-up card (status `todo`, a "Follow-ups" step of phase 12), not a fix.
- Honesty rules of P11-25: counted test numbers; "not verified" lists the manual plan (not run by agents), Narrator / NVDA, the highlight in high contrast, word wrap, the
  diff editor; the AOT line cites P12-23 (and P12-14 for steps A–D) exactly; never "verified" where a card says it did not run.
- The roadmap checklist uses the date of the last card. Every relative link in new and changed docs is opened once.

## Tests

- [ ] None (documentation).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings (unchanged)
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (unchanged)
- [ ] Every relative link in the new and changed docs resolves
- [ ] The AOT result in the roadmap file cites P12-23, or says `NOT verified`
- [ ] `grep -rni "character-precise" docs/adr docs/roadmap docs/how-to docs/ARCHITECTURE.md docs/GLOSSARY.md` finds nothing (only `docs/tasks` keeps history notes)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Phase 13 (Avalonia), running the manual plan, any code change.

## Suggested commit message

`docs: close phase 12 with exact text ranges`
