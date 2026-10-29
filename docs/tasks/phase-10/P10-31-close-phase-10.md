# P10-31 — Close phase 10

- **Status:** done
- **Depends on:** all phase 10 cards
- **Size:** S

## Goal

The documentation matches what phase 10 delivered: the roadmap backlog shows what is done and what is left, the architecture overview, the porting map and the ADR say what exists, what differs from Monaco and what was not verified.

## Monaco sources

Sources: none (documentation only).

## Files to create or change

- `docs/roadmap/phase-10-editor-completeness.md` — change: tick each backlog item that phase 10 delivered (with the card ids, e.g. "(P10-04, P10-05)"), leave the rest unticked, copy the "Later" table of `docs/tasks/phase-10/README.md` as a section "Not taken in phase 10", add the section "Status of the done-when line" (as `phase-9-diff-editor.md` has: built and covered by automated tests vs. not run manually)
- `docs/roadmap/phase-10-audit.md` — change: re-check the `missing` rows of P10-01 against the code; update the rows phase 10 closed; the counts at the top
- `docs/ARCHITECTURE.md` — change: the section written by P10-02 gets its final text (file names that exist); the "Where do I find…?" rows point at real folders; the "Deliberately not delivered" lines of phases 8 and 9 that phase 10 delivered (snippets, ligatures…) get "(phase 10)"
- `docs/PORTING.md` — change: every file ported in phase 10 has a row and every deliberate difference a row in "Behavior differences" (list them from the cards' results); test counts are **counted** (`dotnet test --list-tests` per project), not copied
- `docs/adr/0013-text-options-and-snippets.md` — change: a "Changes during implementation" section listing every decision a card changed, with the date
- `docs/adr/0011-language-server-client.md` — change: "snippets" in its "Not delivered" list gets "(delivered in phase 10)"
- `docs/tasks/README.md` — change: statuses and a "Deferred on purpose" paragraph for phase 10 (copy the "Later" table, shortened)
- `docs/GLOSSARY.md` — change: add *snippet*, *tab stop*, *placeholder*, *input mode (overtype)*, *ligature*, *typography feature*
- `docs/how-to/` — create `use-snippets.md` (30–40 lines: the syntax in one table with examples, `InsertSnippet`, completion items as snippets, what is not supported) and `docs/how-to/font-and-whitespace-options.md` (20–30 lines: the properties, their defaults, fonts with ligatures)
- `README.md` (root) — change only if it has a feature list

## Design decisions (already made — do not change)

- No code change in this card. A problem found while writing the docs becomes a follow-up card in the index (status `todo`, a "Follow-ups" step of phase 10), not a fix.
- **Honesty rules** (same as P7-26, P8-39 and P9-33): test counts are counted; "not verified" items are listed as such: the manual plan (not run by the agents), Narrator / NVDA behavior of the new announcements, ligature rendering on fonts other than Cascadia Code, the Native AOT result (cite P10-30's Result section exactly); never write "verified" where a card says it did not run.
- The roadmap checklist uses the date of the last card.
- Update any cut-point row of earlier phases that points at phase 10 ("snippets", "ligatures", "overtype", "renderWhitespace", `editor zoom`, "go to next problem") to say where it was delivered.

## Tests

- [x] None (documentation).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings (unchanged)
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green (unchanged)
- [x] Every relative link in the new and changed docs resolves (open each new link once)
- [x] The AOT result in the roadmap file cites P10-30, or says `NOT verified`; never ticked if not run
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Phases 11 (inline comments) and 12 (Avalonia), running the manual plan, any code change.

## Suggested commit message

`docs: close phase 10`

## Result (2026-10-29)

- Documentation only; no code changed. The audit rows were updated only for the rows a phase 10 card named (the other `missing` rows were **not** re-verified against the code, and the audit says so).
- Test counts: 7,103 in the solution (7,101 passed, 2 skipped, 0 failed), 40 new test files with 503 `[Fact]` / `[Theory]` methods (counted by attribute); the root `README.md` has no feature list for phase 10 and was not changed.
- **Resolved after the close (2026-10-29)**: the Gallery crashed at start-up (`0xc000027b`) because the `Toggled` handler of the control characters switch (P10-10) ran before `Editor` existed. After the null guard the
  published Native AOT exe passes both self-checks (text 10 of 10, diff 9 passed, 0 failed). See the roadmap file.
- Not run by an agent: the manual plan of phase 10, Narrator / NVDA, ligature rendering other than Cascadia Code.
