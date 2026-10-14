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
| [P1-24](phase-1/P1-24-tests-editable-model-1.md) | Tests: `editableTextModel.test.ts` part 1 | P1-23 | done |
| [P1-25](phase-1/P1-25-tests-editable-model-2.md) | Tests: `editableTextModel.test.ts` part 2 + auto tests | P1-24 | done |
| [P1-26](phase-1/P1-26-text-change.md) | `TextChange` + compression | P1-22 | done |
| [P1-27](phase-1/P1-27-undo-redo.md) | Undo / redo (`EditStack`) | P1-26 | done |
| [P1-28](phase-1/P1-28-text-model-snapshot.md) | `TextModel.CreateSnapshot` | P1-22 | done |

### Step F — Plug the model into the editor and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P1-29](phase-1/P1-29-use-text-model-in-view.md) | `IEditorContext.Model` replaces `Lines` | P1-27, P1-28 | done |
| [P1-30](phase-1/P1-30-close-phase-1.md) | Close phase 1 (docs, porting map, roadmap) | all | done |

### Follow-ups found while working on the cards

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P1-31](phase-1/P1-31-edit-allocations.md) | Reduce per-edit allocations in `ApplyEdits` | P1-19 | done |
| [P1-32](phase-1/P1-32-gallery-text-model-demo.md) | Gallery demo: edits, undo/redo, EOL | P1-29 | done |

Cards in the same step that depend only on an earlier step (e.g. P1-10 … P1-14) can be done in any order.

**Deferred on purpose:** search inside the piece tree (`findMatchesLineByLine`, suite `chunk based search`) goes to
phase 5 with the Find feature; indentation guessing and options, word helpers and tokens (`textModel.test.ts`
"guess indentation" / "normalizeIndentation", `model.test.ts` "Words", `model.line.test.ts`) go to phases 4–5;
decorations and injected text go to phase 2+.

## Phase 2 — Rendering

Monaco reference commit: `0c9c0a4295a9222e12a605537738719455ae3056`. Design: [ADR 0005](../adr/0005-viewport-rendering.md)
(viewport-sized canvas, Monaco's scrolling classes, XAML scrollbars, one DirectWrite layout per line).

### Step A — Scrolling and vertical layout (`LoomEditor.View/Layout/`, ported with their tests)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P2-01](phase-2/P2-01-scroll-state.md) | `ScrollState` and `ScrollEvent` | P1-30 | done |
| [P2-02](phase-2/P2-02-scrollable.md) | `Scrollable` and smooth scrolling | P2-01 | done |
| [P2-03](phase-2/P2-03-line-heights-manager.md) | `LineHeightsManager` | P2-02 | done |
| [P2-04](phase-2/P2-04-lines-layout-1.md) | `LinesLayout` (1/2): vertical offsets and whitespaces | P2-03 | done |
| [P2-05](phase-2/P2-05-lines-layout-2.md) | `LinesLayout` (2/2): viewport data | P2-04 | todo |

### Step B — Font, editor layout, view model (`LoomEditor.View/Layout/`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P2-06](phase-2/P2-06-font-info.md) | `FontInfo` and font measurements | P2-05 | todo |
| [P2-07](phase-2/P2-07-editor-layout-info.md) | `EditorLayoutInfo` (gutter and content area) | P2-06 | todo |
| [P2-08](phase-2/P2-08-view-layout.md) | `ViewLayout`: scroll dimensions of the editor | P2-07 | todo |
| [P2-09](phase-2/P2-09-mouse-wheel.md) | Mouse wheel: `MouseWheelClassifier` and wheel → scroll position | P2-08 | todo |
| [P2-10](phase-2/P2-10-view-model.md) | `ViewModel` (phase 2 subset) and `ViewportData` | P2-09 | todo |

### Step C — Rendering (`LoomEditor.Rendering`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P2-11](phase-2/P2-11-rendering-tests-and-visibility.md) | Rendering test project and internal visibility | P2-10 | todo |
| [P2-12](phase-2/P2-12-text-measurement.md) | Real text layout: `ITextMeasurer`, `ILineLayout`, DirectWrite | P2-11 | todo |
| [P2-13](phase-2/P2-13-line-layout-cache.md) | Line layout cache and very long lines | P2-12 | todo |
| [P2-14](phase-2/P2-14-viewport-renderer.md) | Draw the viewport: visible lines, scroll offsets, selection | P2-13 | todo |
| [P2-15](phase-2/P2-15-gutter-and-line-highlight.md) | Gutter: line numbers, active line number, current line highlight | P2-14 | todo |
| [P2-16](phase-2/P2-16-caret.md) | Caret: drawing and blinking logic | P2-15 | todo |

### Step D — The control (`LoomEditor.WinUI`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P2-17](phase-2/P2-17-control-scrollbars.md) | Scrollbars and `ScrollTop` / `ScrollLeft` | P2-16 | todo |
| [P2-18](phase-2/P2-18-control-mouse-wheel.md) | Mouse wheel and smooth scrolling | P2-17 | todo |
| [P2-19](phase-2/P2-19-control-caret-and-focus.md) | Caret blinking and focus | P2-18 | todo |
| [P2-20](phase-2/P2-20-control-font-tabs-dpi.md) | Font, tab size and DPI changes | P2-19 | todo |

### Step E — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P2-21](phase-2/P2-21-gallery-large-file-demo.md) | Gallery: large file, long line, Unicode, scroll test | P2-20 | todo |
| [P2-22](phase-2/P2-22-scrolling-performance.md) | Scrolling performance: benchmarks and reference numbers | P2-21 | todo |
| [P2-23](phase-2/P2-23-close-phase-2.md) | Close phase 2 (docs, porting map, roadmap) | all | todo |

Phase 2 cards form a single chain, in table order: each card builds on the one before it.

**Deferred on purpose:** word wrap and view-line projections (`viewModelLines.ts`), whitespace rendering, minimap
(phase 6); cursor movement, keyboard scrolling, mouse selection, IME (phase 3); decorations and custom line heights
from decorations; Monaco's custom scrollbars (replaced by XAML `ScrollBar`, ADR 0005).

## How to run a card with a coding agent (Claude Code)

Start a fresh session for each card, on the phase branch, with a clean working tree, and type:

```
/next-task          # picks the first `todo` card whose dependencies are `done`
/next-task P1-07    # or a specific card
```

The skill (`.claude/skills/next-task/SKILL.md`) implements the card, runs the build and the tests, updates the card
and this index, and makes **one commit without any co-author trailer**. It never pushes. Review the commit
(`git show`), then push when you are happy with it.
