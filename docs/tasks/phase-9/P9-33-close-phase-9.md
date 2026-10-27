# P9-33 — Close phase 9

- **Status:** todo
- **Depends on:** all phase 9 cards
- **Size:** S

## Goal

The documentation matches what phase 9 delivered: the architecture overview, the porting map, the roadmap file and the ADR say what exists, what differs from Monaco and what is left, with the honest status of what was and was not verified.

## Monaco sources

Sources: none (documentation only).

## Files to create or change

- `docs/ARCHITECTURE.md` — change: a "Diff editor" section (the four layers of ADR 0012, the data flow `TextModel`s → `DocumentDiffProvider` → `DiffEditorViewModel` → decorations / zones / overview → `LoomDiffEditor`), and the "Where do I find" rows
- `docs/PORTING.md` — change: check that every file ported in phase 9 has a row in the map and every deliberate difference a row in "Behavior differences" (list them from the cards' results); count the tests (counted, not copied from cards); mark the cut points "not delivered in phase 9"
- `docs/roadmap/phase-9-diff-editor.md` — change: the real checklist (below)
- `docs/adr/0012-diff-editor.md` — change: a "Changes during implementation" section listing every decision a card changed, and the date
- `docs/tasks/README.md` — change: statuses, the "Deferred on purpose" paragraph of phase 9 (copy the out-of-scope list of `phase-9/README.md`)
- `README.md` (root) — change only if it has a feature list: add the diff editor line
- `docs/how-to/use-the-diff-editor.md` — create (30–50 lines: a `LoomDiffEditor` snippet with XAML and code, the options, navigation, the accessible viewer, how to get the computed diff counts)
- `docs/GLOSSARY.md` — change: add *view zone*, *unchanged region*, *alignment*, *overview ruler*

## Design decisions (already made — do not change)

- No code change in this card. A problem found while writing the docs becomes a follow-up card in the index (status `todo`, a "Follow-ups" step of phase 9), not a fix.
- **Honesty rules** (same as P7-26 and P8-39): test counts are counted; "not verified" items are listed as such: the Native AOT results of each card (copy from the card statuses), the manual plan (not run by the agent), Narrator behavior; never write "verified" where a card says it did not run.
- **The roadmap file** (`phase-9-diff-editor.md`) keeps its two original lines and adds (ticked when the cards are done): the diff algorithm (P9-01 … P9-11, fixtures P9-10), the diff model (P9-12 … P9-17), view zones, read-only and control hooks (P9-18 … P9-21), side-by-side and inline views (P9-22 … P9-24), hide unchanged regions (P9-25), go to change (P9-26), the generic overview ruler (P9-27, P9-28), the accessible diff viewer (P9-29, P9-30), Gallery demo, self-check and benchmarks (P9-31, P9-32); an unticked line for "manual test plan run by the maintainer"; the "Not delivered" list (ADR 0012 decision 9 and the phase README); open risks: the stale diff while typing, the inline deleted text not selectable, the hatching rasterization at high DPI, the performance numbers of P9-32, Narrator not run by an agent.
- Update the cut-point rows of earlier phases that point at phase 9 (`The generic OverviewRuler … | phase 9`, the accessible view of the diff editor in ADR 0010 / phase 7 docs) to say where they were delivered.

## Tests

- [ ] None (documentation).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings (unchanged)
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (unchanged)
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` — result stated in the roadmap file (cite P9-31 / P9-32, or `NOT verified`), never ticked if not run
- [ ] Every link in the new docs resolves (open each new relative link once)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Phase 10, running the manual plan, any code change.

## Suggested commit message

`docs: close phase 9`
