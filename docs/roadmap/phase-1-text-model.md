# Phase 1 — Text model: piece tree, positions, EOL, undo/redo

**Done when:** Monaco's model tests are ported and green.

- [ ] Port the piece tree (`pieceTreeTextBuffer/*`) — plan in docs/PORTING.md
- [ ] Fuzz test against a naive `StringBuilder` buffer
- [ ] `TextModel`: lines, EOL (LF/CRLF), validation of positions/ranges, change events
- [ ] Undo/redo (`editStack.ts`), edit grouping
- [ ] Port `pieceTreeTextBuffer.test.ts`, `textModel.test.ts`, `model.line.test.ts`, `editableTextModel*.test.ts`
- [ ] Benchmarks: open 100 MB, 10,000 random edits; results in docs/performance.md
