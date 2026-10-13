# Task cards

The roadmap (`docs/roadmap/`) says **what** each phase delivers. Task cards say **exactly how to get there, one small
step at a time**. Each card is sized so that one developer — or one coding-agent session — can finish it in one go:

- **one card = one pull request = one commit**, with its tests;
- at most ~400 lines of production code (most cards are much smaller);
- the Monaco source to port is given with **file and line numbers** at the pinned commit;
- design decisions are **already made** in the card, so nobody has to re-decide them;
- acceptance criteria are commands that pass or fail.

Rules for working on a card: see `CLAUDE.md` at the repository root. Template: [`_template.md`](_template.md).
Cards are written for the phase that is starting; later phases get their cards when they start.

## Phase 1 — Text model

Monaco reference commit: `0c9c0a4295a9222e12a605537738719455ae3056`.

### Step A — Piece tree data structures

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P1-01](phase-1/P1-01-line-starts.md) | Line starts (`createLineStarts*`) | — | done |
| [P1-02](phase-1/P1-02-piece-tree-value-types.md) | `StringBuffer`, `BufferCursor`, `Piece`, `NodePosition` | P1-01 | done |
| [P1-03](phase-1/P1-03-red-black-tree.md) | Red-black tree (`rbTreeBase.ts`) + invariant checker | P1-02 | done |
| [P1-04](phase-1/P1-04-search-cache.md) | `PieceTreeSearchCache` | P1-03 | done |

### Step B — `PieceTreeBase`

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P1-05](phase-1/P1-05-piece-tree-create-and-lines.md) | Construction and reading lines | P1-04 | done |
| [P1-06](phase-1/P1-06-piece-tree-offsets-positions.md) | Offsets ↔ positions, ranges, char codes | P1-05 | done |
| [P1-07](phase-1/P1-07-piece-tree-insert.md) | Insert (with CRLF handling) | P1-06 | done |
| [P1-08](phase-1/P1-08-piece-tree-delete.md) | Delete | P1-07 | done |
| [P1-09](phase-1/P1-09-text-buffer-builder.md) | `PieceTreeTextBufferBuilder` + factory + `PieceTreeTextBuffer` shell | P1-08 | done |

### Step C — Port the piece tree test suite (fix bugs as they appear)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P1-10](phase-1/P1-10-tests-inserts-deletes.md) | Tests: inserts and deletes, prefix sums, offset → position, search offset cache | P1-09 | done |
| [P1-11](phase-1/P1-11-tests-text-in-range.md) | Tests: get text in range, line content | P1-09 | done |
| [P1-12](phase-1/P1-12-tests-crlf.md) | Tests: CRLF and centralized line starts | P1-09 | done |
| [P1-13](phase-1/P1-13-snapshot-and-equality.md) | Snapshots, equality, buffer API tests | P1-09 | done |
| [P1-14](phase-1/P1-14-eol-normalization.md) | `NormalizeEol` / `SetEol` | P1-09 | done |
| [P1-15](phase-1/P1-15-fuzz-tests.md) | Randomized tests vs. a naive reference buffer | P1-10…P1-14 | done |

### Step D — `PieceTreeTextBuffer` (the API used by `TextModel`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P1-16](phase-1/P1-16-text-buffer-queries.md) | `ITextBuffer` + read queries | P1-15 | done |
| [P1-17](phase-1/P1-17-inverse-edits.md) | Pure edit helpers: inverse ranges, single-edit merge | P1-16 | done |
| [P1-18](phase-1/P1-18-apply-edits.md) | `ApplyEdits` | P1-17 | done |
| [P1-19](phase-1/P1-19-piece-tree-benchmarks.md) | Benchmarks + reference numbers | P1-18 | done |

### Step E — `TextModel`

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P1-20](phase-1/P1-20-text-model-read.md) | `TextModel`: creation and read API | P1-18 | done |
| [P1-21](phase-1/P1-21-text-model-validation.md) | `ValidatePosition`, `ValidateRange`, `ModifyPosition` | P1-20 | done |
| [P1-22](phase-1/P1-22-text-model-edits-and-events.md) | Edits, `SetValue`, `SetEol`, `ContentChanged` event | P1-21 | done |
| [P1-23](phase-1/P1-23-raw-content-events.md) | Line-level ("raw") change events + `model.test.ts` | P1-22 | done |
| [P1-24](phase-1/P1-24-tests-editable-model-1.md) | Tests: `editableTextModel.test.ts` part 1 | P1-23 | todo |
| [P1-25](phase-1/P1-25-tests-editable-model-2.md) | Tests: `editableTextModel.test.ts` part 2 + auto tests | P1-24 | todo |
| [P1-26](phase-1/P1-26-text-change.md) | `TextChange` + compression | P1-22 | todo |
| [P1-27](phase-1/P1-27-undo-redo.md) | Undo / redo (`EditStack`) | P1-26 | todo |
| [P1-28](phase-1/P1-28-text-model-snapshot.md) | `TextModel.CreateSnapshot` | P1-22 | todo |

### Step F — Plug the model into the editor and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P1-29](phase-1/P1-29-use-text-model-in-view.md) | `IEditorContext.Model` replaces `Lines` | P1-27, P1-28 | todo |
| [P1-30](phase-1/P1-30-close-phase-1.md) | Close phase 1 (docs, porting map, roadmap) | all | todo |

### Follow-ups found while working on the cards

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P1-31](phase-1/P1-31-edit-allocations.md) | Reduce per-edit allocations in `ApplyEdits` | P1-19 | todo |

Cards in the same step that depend only on an earlier step (e.g. P1-10 … P1-14) can be done in any order.

**Deferred on purpose:** search inside the piece tree (`findMatchesLineByLine`, suite `chunk based search`) goes to
phase 5 with the Find feature; indentation guessing and options, word helpers and tokens (`textModel.test.ts`
"guess indentation" / "normalizeIndentation", `model.test.ts` "Words", `model.line.test.ts`) go to phases 4–5;
decorations and injected text go to phase 2+.

## How to run a card with a coding agent (Claude Code)

Start a fresh session for each card, on the phase branch, with a clean working tree, and type:

```
/next-task          # picks the first `todo` card whose dependencies are `done`
/next-task P1-07    # or a specific card
```

The skill (`.claude/skills/next-task/SKILL.md`) implements the card, runs the build and the tests, updates the card
and this index, and makes **one commit without any co-author trailer**. It never pushes. Review the commit
(`git show`), then push when you are happy with it.
