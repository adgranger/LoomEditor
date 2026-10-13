# Phase 1 — Text model: piece tree, positions, EOL, undo/redo

**Done when:** Monaco's model tests are ported and green.

Step-by-step task cards: [docs/tasks/README.md](../tasks/README.md) (P1-01 … P1-30).

- [ ] Port the piece tree (`pieceTreeTextBuffer/*`) — P1-01 … P1-09
- [ ] Port `pieceTreeTextBuffer.test.ts` (except `chunk based search`, phase 5) — P1-10 … P1-14
- [ ] Fuzz test against a naive reference buffer — P1-15
- [ ] `PieceTreeTextBuffer`: queries and `ApplyEdits`, `linesTextBuffer*.test.ts` — P1-16 … P1-18
- [ ] Benchmarks: open 100 MB, 10,000 random edits; results in docs/performance.md — P1-19
- [ ] `TextModel`: lines, EOL (LF/CRLF), validation, edits, change events; `textModel.test.ts` (model part), `model.test.ts`, `editableTextModel*.test.ts` — P1-20 … P1-25
- [ ] Undo/redo (`textChange.ts`, `editStack.ts`), edit grouping — P1-26, P1-27
- [ ] Snapshots — P1-28
- [ ] The editor uses `TextModel` — P1-29
