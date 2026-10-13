# P1-30 — Close phase 1

- **Status:** todo
- **Depends on:** all other phase 1 cards
- **Size:** S (docs only)

## Goal

Make sure the documentation tells the truth about phase 1, and that phase 1's "done when" criterion is met:
*Monaco's model tests are ported and green.*

## Files to create or change

- `docs/PORTING.md` — map complete for every file touched in phase 1; "Behavior differences" complete; replace the
  "Plan: porting the piece tree" section with a short "Done in phase 1" paragraph pointing to `docs/tasks/phase-1/`
- `docs/roadmap/phase-1-text-model.md` — tick every item
- `docs/ARCHITECTURE.md` — "Where do I find…?" table: add the text model, undo, piece tree
- `docs/GLOSSARY.md` — add any term introduced during phase 1 that is missing (e.g. *alternative version id*, *raw change event*, *snapshot*)
- `src/LoomEditor.Core/Model/README.md`, `PieceTree/README.md`, `Undo/README.md` — describe what is there now
- `README.md` — status line and roadmap table ("we are here" → phase 2)
- `docs/tasks/README.md` — all phase 1 cards `done`

## Checks

- [ ] Count the ported Monaco tests per file and write the table in `docs/PORTING.md` (file, ported / total, reason for the ones not ported)
- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings

## Out of scope

Writing phase 2 task cards (done by the maintainer with the planning model, not here).

## Suggested commit message

`docs: close phase 1`
