# Phase 1 — Text model: piece tree, positions, EOL, undo/redo

**Done when:** Monaco's model tests are ported and green.

Step-by-step task cards: [docs/tasks/README.md](../tasks/README.md) (P1-01 … P1-30).

- [x] Port the piece tree (`pieceTreeTextBuffer/*`) — P1-01 … P1-09
- [x] Port `pieceTreeTextBuffer.test.ts` (except `chunk based search`, phase 5) — P1-10 … P1-14
- [x] Fuzz test against a naive reference buffer — P1-15
- [x] `PieceTreeTextBuffer`: queries and `ApplyEdits`, `linesTextBuffer*.test.ts` — P1-16 … P1-18
- [x] Benchmarks: open 100 MB, 10,000 random edits; results in docs/performance.md — P1-19
- [x] `TextModel`: lines, EOL (LF/CRLF), validation, edits, change events; `textModel.test.ts` (model part), `model.test.ts`, `editableTextModel*.test.ts` — P1-20 … P1-25
- [x] Undo/redo (`textChange.ts`, `editStack.ts`), edit grouping — P1-26, P1-27
- [x] Snapshots — P1-28
- [x] The editor uses `TextModel` — P1-29
