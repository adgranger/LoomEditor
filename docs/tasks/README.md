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
| [P2-05](phase-2/P2-05-lines-layout-2.md) | `LinesLayout` (2/2): viewport data | P2-04 | done |

### Step B — Font, editor layout, view model (`LoomEditor.View/Layout/`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P2-06](phase-2/P2-06-font-info.md) | `FontInfo` and font measurements | P2-05 | done |
| [P2-07](phase-2/P2-07-editor-layout-info.md) | `EditorLayoutInfo` (gutter and content area) | P2-06 | done |
| [P2-08](phase-2/P2-08-view-layout.md) | `ViewLayout`: scroll dimensions of the editor | P2-07 | done |
| [P2-09](phase-2/P2-09-mouse-wheel.md) | Mouse wheel: `MouseWheelClassifier` and wheel → scroll position | P2-08 | done |
| [P2-10](phase-2/P2-10-view-model.md) | `ViewModel` (phase 2 subset) and `ViewportData` | P2-09 | done |

### Step C — Rendering (`LoomEditor.Rendering`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P2-11](phase-2/P2-11-rendering-tests-and-visibility.md) | Rendering test project and internal visibility | P2-10 | done |
| [P2-12](phase-2/P2-12-text-measurement.md) | Real text layout: `ITextMeasurer`, `ILineLayout`, DirectWrite | P2-11 | done |
| [P2-13](phase-2/P2-13-line-layout-cache.md) | Line layout cache and very long lines | P2-12 | done |
| [P2-14](phase-2/P2-14-viewport-renderer.md) | Draw the viewport: visible lines, scroll offsets, selection | P2-13 | done |
| [P2-15](phase-2/P2-15-gutter-and-line-highlight.md) | Gutter: line numbers, active line number, current line highlight | P2-14 | done |
| [P2-16](phase-2/P2-16-caret.md) | Caret: drawing and blinking logic | P2-15 | done |

### Step D — The control (`LoomEditor.WinUI`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P2-17](phase-2/P2-17-control-scrollbars.md) | Scrollbars and `ScrollTop` / `ScrollLeft` | P2-16 | done |
| [P2-18](phase-2/P2-18-control-mouse-wheel.md) | Mouse wheel and smooth scrolling | P2-17 | done |
| [P2-19](phase-2/P2-19-control-caret-and-focus.md) | Caret blinking and focus | P2-18 | done |
| [P2-20](phase-2/P2-20-control-font-tabs-dpi.md) | Font, tab size and DPI changes | P2-19 | done |

### Step E — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P2-21](phase-2/P2-21-gallery-large-file-demo.md) | Gallery: large file, long line, Unicode, scroll test | P2-20 | done |
| [P2-22](phase-2/P2-22-scrolling-performance.md) | Scrolling performance: benchmarks and reference numbers | P2-21 | done |
| [P2-23](phase-2/P2-23-close-phase-2.md) | Close phase 2 (docs, porting map, roadmap) | all | done |

Phase 2 cards form a single chain, in table order: each card builds on the one before it.

**Deferred on purpose:** word wrap and view-line projections (`viewModelLines.ts`), whitespace rendering, minimap
(phase 6); cursor movement, keyboard scrolling, mouse selection, IME (phase 3); decorations and custom line heights
from decorations; Monaco's custom scrollbars (replaced by XAML `ScrollBar`, ADR 0005).

## Phase 3 — Editing

Monaco reference commit: `0c9c0a4295a9222e12a605537738719455ae3056`. Design: [ADR 0006](../adr/0006-editing-and-text-input.md)
(Monaco's cursor in `LoomEditor.View`, tracked ranges on a ported interval tree, no language configuration until
phases 4–5, plain typing through `CharacterReceived`, IME through `CoreTextEditContext` on Windows 11).
**Read [phase-3/README.md](phase-3/README.md) before any phase 3 card**: it holds the conventions every card relies on
(where files go, Monaco → C# API map, JavaScript traps, shared test helpers).

### Step A — Core prerequisites: strings, columns, words, interval tree, model edits

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P3-01](phase-3/P3-01-strings-code-points.md) | Strings: code points, surrogates, full-width and emoji helpers | P2-23 | done |
| [P3-02](phase-3/P3-02-strings-graphemes.md) | Strings: grapheme clusters (`GraphemeIterator`, `nextCharLength`, `prevCharLength`) | P3-01 | done |
| [P3-03](phase-3/P3-03-cursor-columns.md) | `CursorColumns` and indentation helpers | P3-02 | done |
| [P3-04](phase-3/P3-04-word-character-classifier.md) | `CharacterClassifier` and `WordCharacterClassifier` | P3-03 | done |
| [P3-05](phase-3/P3-05-interval-tree-1.md) | Interval tree (1/3): nodes, red-black insert and delete | P3-04 | done |
| [P3-06](phase-3/P3-06-interval-tree-2.md) | Interval tree (2/3): searching, and the `IntervalTree 1` test suite | P3-05 | done |
| [P3-07](phase-3/P3-07-interval-tree-3.md) | Interval tree (3/3): edits (`acceptReplace`, `nodeAcceptEdit`) | P3-06 | done |
| [P3-08](phase-3/P3-08-tracked-ranges.md) | Tracked ranges in `TextModel` | P3-07 | done |
| [P3-09](phase-3/P3-09-model-options.md) | Model options (`tabSize`, `indentSize`, `insertSpaces`, `trimAutoWhitespace`) and indent helpers | P3-08 | done |
| [P3-10](phase-3/P3-10-identified-edit-operations.md) | Identified edit operations and `ResultingSelection` | P3-09 | done |
| [P3-11](phase-3/P3-11-trim-auto-whitespace.md) | Trim auto-inserted whitespace (`trimAutoWhitespace`) | P3-10 | done |

### Step B — The cursor core (`LoomEditor.View/Cursors/`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P3-12](phase-3/P3-12-cursor-state-types.md) | Cursor state types and the identity coordinates converter | P3-11 | done |
| [P3-13](phase-3/P3-13-cursor-configuration.md) | `CursorConfiguration` and the editor's cursor options | P3-12 | done |
| [P3-14](phase-3/P3-14-edit-commands-and-executor.md) | `IEditCommand` and `CommandExecutor` | P3-13 | done |
| [P3-15](phase-3/P3-15-replace-commands.md) | Replace commands | P3-14 | done |
| [P3-16](phase-3/P3-16-cursor-and-collection.md) | `Cursor`, `CursorContext` and `CursorCollection` | P3-15 | done |
| [P3-17](phase-3/P3-17-view-model-events.md) | View model events (dispatcher subset) | P3-16 | done |
| [P3-18](phase-3/P3-18-cursors-controller-state.md) | `CursorsController` (1/2): state, model changes, reveal, events | P3-17 | done |
| [P3-19](phase-3/P3-19-cursors-controller-edits.md) | `CursorsController` (2/2): executing edits and commands | P3-18 | done |
| [P3-20](phase-3/P3-20-view-model-cursors.md) | The view model owns the cursors (and the test editor) | P3-19 | done |
| [P3-21](phase-3/P3-21-editor-context-cursors.md) | `EditorContext` and the control use the cursors | P3-20 | done |

### Step C — Cursor moves, column selection, words

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P3-22](phase-3/P3-22-atomic-tab-moves.md) | Atomic soft tabs (`AtomicTabMoveOperations`) | P3-21 | done |
| [P3-23](phase-3/P3-23-move-operations.md) | `MoveOperations`: left, right, up, down, line and buffer edges | P3-22 | done |
| [P3-24](phase-3/P3-24-cursor-move-commands-1.md) | `CursorMoveCommands` (1/2): line, buffer, select all, add cursor, move to | P3-23 | done |
| [P3-25](phase-3/P3-25-cursor-move-commands-2.md) | `CursorMoveCommands` (2/2): `simpleMove`, `viewportMove` and the `cursorMove` arguments | P3-24 | done |
| [P3-26](phase-3/P3-26-core-navigation-commands.md) | Core navigation commands (`cursorLeft`, `cursorHome`, `cursorMove`…) | P3-25 | done |
| [P3-27](phase-3/P3-27-cursor-tests-movement.md) | Port the cursor movement tests of `cursor.test.ts` | P3-26 | done |
| [P3-28](phase-3/P3-28-column-selection.md) | Column (box) selection | P3-27 | done |
| [P3-29](phase-3/P3-29-word-operations-move.md) | Word moves (`WordOperations` 1/2) and the `WordOperations` feature | P3-28 | done |
| [P3-30](phase-3/P3-30-word-operations-delete.md) | Word deletes, word selection and word parts (`WordOperations` 2/2) | P3-29 | done |

### Step D — Typing, deleting, indentation, paste, copy, undo

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P3-31](phase-3/P3-31-shift-command.md) | `ShiftCommand` (indent and outdent lines) | P3-30 | done |
| [P3-32](phase-3/P3-32-delete-operations.md) | Backspace and Delete (`DeleteOperations`, `deleteLeft`, `deleteRight`) | P3-31 | done |
| [P3-33](phase-3/P3-33-typing-and-enter.md) | Typing and Enter (`TypeOperations`, `type`, `lineBreakInsert`) | P3-32 | done |
| [P3-34](phase-3/P3-34-tab-and-outdent.md) | Tab and Shift+Tab (`tab`, `outdent`) | P3-33 | done |
| [P3-35](phase-3/P3-35-paste.md) | Paste (single, distributed over cursors, block paste) | P3-34 | done |
| [P3-36](phase-3/P3-36-copy-and-cut.md) | Copy and cut (text to copy, clipboard metadata, `cut`) | P3-35 | done |
| [P3-37](phase-3/P3-37-undo-redo-commands.md) | Undo and redo commands, read-only editor | P3-36 | done |

### Step E — Commands, keybindings, reveal, keyboard in the control

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P3-38](phase-3/P3-38-multicursor-and-scroll-commands.md) | Multi-cursor commands (without Find) and scroll commands | P3-37 | done |
| [P3-39](phase-3/P3-39-default-keybindings.md) | Default keybindings and the keybinding resolver | P3-38 | done |
| [P3-40](phase-3/P3-40-reveal.md) | Reveal: scroll so that the cursor stays visible | P3-39 | done |
| [P3-41](phase-3/P3-41-control-keyboard-and-typing.md) | The control: keyboard commands, typing and `IsReadOnly` | P3-40 | done |

### Step F — Several cursors on screen, and the mouse

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P3-42](phase-3/P3-42-draw-several-cursors.md) | Draw several cursors and selections | P3-41 | done |
| [P3-43](phase-3/P3-43-mouse-hit-testing.md) | Mouse hit testing on the canvas | P3-42 | done |
| [P3-44](phase-3/P3-44-mouse-dispatch.md) | Mouse clicks to cursor commands (`ViewController.dispatchMouse`) | P3-43 | done |
| [P3-45](phase-3/P3-45-mouse-down-and-drag-scrolling.md) | Mouse down, drag selection and drag scrolling (pure logic) | P3-44 | done |
| [P3-46](phase-3/P3-46-control-pointer-input.md) | The control: mouse selection | P3-45 | done |

### Step G — Clipboard

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P3-47](phase-3/P3-47-control-clipboard.md) | Clipboard: copy, cut and paste with the Windows clipboard | P3-46 | done |

### Step H — IME

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P3-48](phase-3/P3-48-ime-spike.md) | IME spike: check `CoreTextEditContext` in the WinUI 3 desktop control | P3-47 | done |
| [P3-49](phase-3/P3-49-composition-in-the-cursor.md) | Composition in the cursor (`compositionType`, start and end of a composition) | P3-48 | done |
| [P3-50](phase-3/P3-50-edit-context-state.md) | Edit context state (the text the IME sees, and its updates) | P3-49 | done |
| [P3-51](phase-3/P3-51-control-core-text-edit-context.md) | The control: IME through `CoreTextEditContext` | P3-50 | done |
| [P3-52](phase-3/P3-52-ime-candidate-window.md) | IME: place the candidate window at the caret | P3-51 | done |
| [P3-53](phase-3/P3-53-composition-underline.md) | IME: underline the text being composed | P3-52 | done |

### Step I — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P3-54](phase-3/P3-54-gallery-editing-demo.md) | Gallery: editing demo and the phase 3 manual test list | P3-53 | done |
| [P3-55](phase-3/P3-55-close-phase-3.md) | Close phase 3 | all | done |

Phase 3 cards form a single chain, in table order: each card builds on the one before it. P3-48 (IME spike) and the
manual checks of P3-41, P3-46, P3-47, P3-51 … P3-54 need the maintainer at the keyboard (keyboard layouts, IMEs, mouse).

**Deferred on purpose:** everything that needs a language configuration or tokens — auto-closing pairs, auto-indent
rules, electric characters, surround with brackets/quotes, double-click on brackets or strings (phases 4–5; each cut
point is named in the cards and listed in `docs/PORTING.md` by P3-55); Find-based multi-cursor (`Ctrl+D`, phase 5);
word wrap and folding moves (phases 5–6); overtype mode, selection drag-and-drop, rich (HTML) copy, touch gestures,
Intl word segmentation; IME on Windows 10 (would need a TSF `ITextStoreACP2` implementation).

## Phase 4 — TextMate grammars and VS Code themes

Reference commits: `microsoft/vscode` `0c9c0a4295a9222e12a605537738719455ae3056`, `microsoft/vscode-textmate` v9.3.2
`25b68dad91920b1ed79d357534b8c4582f62d80d`, `microsoft/vscode-oniguruma` v1.7.0 `716aeaa229e4ae2e3b0057377b55743e9a3e995b`.
Design: [ADR 0007](../adr/0007-textmate-regex-engine.md) (native Oniguruma 6.9.10, frozen) and
[ADR 0008](../adr/0008-syntax-highlighting.md) (vscode-textmate port, Monaco's tokens in Core, a background tokenizer
thread, VS Code themes, golden tests from the real vscode-textmate).
**Read [phase-4/README.md](phase-4/README.md) before any phase 4 card.**

### Step A — The regex engine

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P4-01](phase-4/P4-01-aot-resources-spike.md) | Spike: embedded resources and a native DLL in the Native AOT Gallery | P3-55 | done |
| [P4-02](phase-4/P4-02-ship-onig-dll.md) | Build and ship `onig.dll` with `LoomEditor.TextMate` | P4-01 | done |
| [P4-03](phase-4/P4-03-oniguruma-interop.md) | Oniguruma interop: `[LibraryImport]`, safe handles, one compiled regex | P4-02 | done |
| [P4-04](phase-4/P4-04-onig-scanner.md) | `OnigScanner`, `OnigString` and `IOnigLib` (vscode-oniguruma tests) | P4-03 | done |

### Step B — vscode-textmate building blocks

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P4-05](phase-4/P4-05-textmate-utils-and-matcher.md) | Helpers (`utils.ts`) and scope selectors (`matcher.ts`) | P4-04 | done |
| [P4-06](phase-4/P4-06-raw-grammar-json.md) | Raw grammar types and the JSON grammar reader | P4-05 | done |
| [P4-07](phase-4/P4-07-plist-grammars.md) | plist grammars (`plist.ts`) | P4-06 | done |
| [P4-08](phase-4/P4-08-textmate-theme-parsing.md) | Theme (1/2): scope stacks, theme parsing, color map | P4-07 | done |
| [P4-09](phase-4/P4-09-textmate-theme-matching.md) | Theme (2/2): resolving rules and matching scopes | P4-08 | done |
| [P4-10](phase-4/P4-10-textmate-encoded-attributes.md) | Token metadata (`encodedTokenAttributes.ts`) | P4-09 | done |

### Step C — vscode-textmate rules, grammar and tokenizer

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P4-11](phase-4/P4-11-regexp-source.md) | Rules (1/2): `RegExpSource`, `RegExpSourceList`, `CompiledRule` | P4-10 | done |
| [P4-12](phase-4/P4-12-rules-and-rule-factory.md) | Rules (2/2): rule classes, `RuleFactory`, `parseInclude` | P4-11 | done |
| [P4-13](phase-4/P4-13-grammar-dependencies-and-basic-scopes.md) | Grammar dependencies and basic scope attributes | P4-12 | done |
| [P4-14](phase-4/P4-14-attributed-scope-stack.md) | Grammar (1/6): `AttributedScopeStack`, `BalancedBracketSelectors` | P4-13 | done |
| [P4-15](phase-4/P4-15-state-stack.md) | Grammar (2/6): `StateStackImpl` | P4-14 | done |
| [P4-16](phase-4/P4-16-line-tokens-accumulator.md) | Grammar (3/6): the token accumulator | P4-15 | done |
| [P4-17](phase-4/P4-17-grammar-class.md) | Grammar (4/6): the `Grammar` class, injections, rule registry | P4-16 | done |
| [P4-18](phase-4/P4-18-tokenize-string-helpers.md) | Grammar (5/6): rule and injection matching | P4-17 | done |
| [P4-19](phase-4/P4-19-tokenize-line.md) | Grammar (6/6): the tokenization loop, captures, while, `tokenizeLine(2)` | P4-18 | done |
| [P4-20](phase-4/P4-20-registry.md) | `SyncRegistry`, `Registry` (synchronous), `diffStateStacks` | P4-19 | done |
| [P4-21](phase-4/P4-21-tests-first-mate.md) | Tests: the `first-mate` tokenization suite | P4-20 | done |
| [P4-22](phase-4/P4-22-tests-suite1-and-grammar.md) | Tests: `suite1`, `whileTests`, the rest of `grammar.test.ts` | P4-21 | done |

### Step D — Monaco's tokens in the text model (`LoomEditor.Core`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P4-23](phase-4/P4-23-core-token-types.md) | Token metadata, tokenization interfaces, null tokenizer, language ids, registry | P4-22 | done |
| [P4-24](phase-4/P4-24-line-tokens.md) | `LineTokens` and `IViewLineTokens` | P4-23 | done |
| [P4-25](phase-4/P4-25-contiguous-tokens-store.md) | `ContiguousTokensEditing` and `ContiguousTokensStore` | P4-24 | done |
| [P4-26](phase-4/P4-26-contiguous-multiline-tokens.md) | `ContiguousMultilineTokens` and its builder | P4-25 | done |
| [P4-27](phase-4/P4-27-state-stores-and-priority-queue.md) | `FixedArray`, state stores, `RangePriorityQueueImpl` | P4-26 | done |
| [P4-28](phase-4/P4-28-tokenizer-with-state-store.md) | `TokenizerWithStateStore(AndTextModel)`, `DefaultBackgroundTokenizer` | P4-27 | done |
| [P4-29](phase-4/P4-29-text-model-tokenization.md) | `TextModel` tokenization (1/2): language id, backend, `TokensChanged` | P4-28 | done |
| [P4-30](phase-4/P4-30-attached-views-and-model-modes.md) | `TextModel` tokenization (2/2): attached views, size limits, `model.modes.test.ts` | P4-29 | done |
| [P4-31](phase-4/P4-31-tests-model-line-tokens-1.md) | Tests: `model.line.test.ts` (1/2) | P4-30 | done |
| [P4-32](phase-4/P4-32-tests-model-line-tokens-2.md) | Tests: `model.line.test.ts` (2/2), token regressions | P4-31 | done |

### Step E — TextMate meets the model: languages, grammars, background thread

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P4-33](phase-4/P4-33-textmate-tokenization-support.md) | `TextMateTokenizationSupport` and `TokenizationSupportWithLineLimit` | P4-32 | done |
| [P4-34](phase-4/P4-34-grammar-factory-and-languages.md) | Languages, grammar definitions, `TMScopeRegistry`, `TMGrammarFactory` | P4-33 | done |
| [P4-35](phase-4/P4-35-bundle-grammars.md) | Bundle the ten built-in grammars | P4-34 | done |
| [P4-36](phase-4/P4-36-mirror-model-and-length-edits.md) | Background (1/3): `MirrorTextModel`, `PrefixSumComputer`, `LengthEdit`, index transformer | P4-35 | done |
| [P4-37](phase-4/P4-37-tokenization-worker.md) | Background (2/3): the worker on its own thread | P4-36 | done |
| [P4-38](phase-4/P4-38-worker-controller.md) | Background (3/3): the UI side and `IUiDispatcher` | P4-37 | done |

### Step F — VS Code themes (`LoomEditor.Themes`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P4-39](phase-4/P4-39-editor-color-defaults.md) | Theme types and VS Code's default editor colors | P4-38 | done |
| [P4-40](phase-4/P4-40-vscode-theme-loading.md) | Load a VS Code color theme | P4-39 | done |
| [P4-41](phase-4/P4-41-built-in-themes.md) | The ten built-in themes and `EditorTheme`'s public API | P4-40 | done |

### Step G — Highlighting in the editor

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P4-42](phase-4/P4-42-syntax-highlighter.md) | `SyntaxHighlighter`: grammars, worker and theme for one editor | P4-41 | done |
| [P4-43](phase-4/P4-43-view-model-tokens.md) | View model: tokens of the visible lines, visible lines to the model | P4-42 | done |
| [P4-44](phase-4/P4-44-render-token-colors.md) | Rendering: token colors and font styles | P4-43 | done |
| [P4-45](phase-4/P4-45-control-language-and-theme.md) | The control: `LanguageId` and `Theme` properties | P4-44 | done |
| [P4-46](phase-4/P4-46-gallery-highlighting-demo.md) | Gallery: highlighting demo and the phase 4 manual test list | P4-45 | done |

### Step H — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P4-47](phase-4/P4-47-golden-generator.md) | Golden tests (1/2): the generator (real vscode-textmate) and the samples | P4-46 | done |
| [P4-48](phase-4/P4-48-golden-tests.md) | Golden tests (2/2): ten languages, two themes | P4-47 | done |
| [P4-49](phase-4/P4-49-tokenization-benchmarks.md) | Tokenization benchmarks and reference numbers | P4-48 | done |
| [P4-50](phase-4/P4-50-close-phase-4.md) | Close phase 4 | all | done |

Phase 4 cards form a single chain, in table order. P4-45 and P4-46 have manual checks for the maintainer; P4-47 needs
Node (only to regenerate golden files).

**Deferred on purpose:** language configuration and everything that uses it (brackets, auto-closing, auto-indent,
comments — phase 5), bracket pair colorization, semantic highlighting, tree-sitter, font tokens (vscode-textmate 9.3
`fontFamily` / `fontSize` / `lineHeight`), `.tmTheme` themes, grammars supplied by the host application, the token
inspector.


## Phase 5 — Editor features

Monaco reference commit: `0c9c0a4295a9222e12a605537738719455ae3056`. Design: [ADR 0009](../adr/0009-editor-features.md)
(model decorations, Monaco's language configuration per editor, the full bracket pair tree, hidden areas without word
wrap, folding from indentation and markers, a XAML find widget, .NET regexes). Extras chosen by the maintainer on
2026-10-20: Find-based multi-cursor, line operations, the chunk based search tests.
**Read [phase-5/README.md](phase-5/README.md) before any phase 5 card.**

### Step A — Scheduling and model decorations

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P5-01](phase-5/P5-01-editor-scheduler.md) | Editor scheduler: `IEditorScheduler`, `RunOnceScheduler`, `Delayer`, `TimeoutTimer` | P4-50 | done |
| [P5-02](phase-5/P5-02-model-decoration-options.md) | `ModelDecorationOptions`: Monaco's options, `Register` / `CreateDynamic` | P5-01 | done |
| [P5-03](phase-5/P5-03-text-model-decorations.md) | `TextModel` decorations: `DeltaDecorations`, `ChangeDecorations`, queries | P5-02 | done |
| [P5-04](phase-5/P5-04-decorations-changed-event.md) | `TextModel.DecorationsChanged` and the decorations-and-editing tests | P5-03 | done |
| [P5-05](phase-5/P5-05-decoration-theme-colors.md) | Theme colors of the phase 5 decorations | P5-04 | done |
| [P5-06](phase-5/P5-06-editor-decorations-collection.md) | Editor id and `EditorDecorationsCollection` | P5-05 | done |
| [P5-07](phase-5/P5-07-view-model-decorations.md) | View model decorations: viewport decorations, inline decorations, decorations-changed view event | P5-06 | done |
| [P5-08](phase-5/P5-08-decoration-styles-and-backgrounds.md) | `DecorationStyles` and drawing decoration backgrounds and borders | P5-07 | done |
| [P5-09](phase-5/P5-09-inline-and-gutter-decorations.md) | Drawing inline decorations, after content and gutter decorations | P5-08 | done |

### Step B — Language configuration

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P5-10](phase-5/P5-10-language-configuration-types.md) | Language configuration types and scoped line tokens | P5-09 | done |
| [P5-11](phase-5/P5-11-rich-edit-brackets.md) | `RichEditBrackets`, `BracketsUtils` and electric characters | P5-10 | done |
| [P5-12](phase-5/P5-12-character-pair-on-enter-indent-rules.md) | `CharacterPairSupport`, `OnEnterSupport` and `IndentRulesSupport` | P5-11 | done |
| [P5-13](phase-5/P5-13-language-brackets-configuration.md) | `LanguageBracketsConfiguration` | P5-12 | done |
| [P5-14](phase-5/P5-14-language-configuration-registry.md) | `LanguageConfigurationRegistry` and `ResolvedLanguageConfiguration` | P5-13 | done |
| [P5-15](phase-5/P5-15-text-model-language-configuration-and-words.md) | The model gets the registry: `GetWordAtPosition`, `LanguageConfigurationChanged` | P5-14 | done |
| [P5-16](phase-5/P5-16-indentation-line-processor.md) | Indentation line processor and the test languages | P5-15 | done |
| [P5-17](phase-5/P5-17-enter-action-and-inherit-indent.md) | `GetEnterAction`, `GetInheritIndentForLine`, `GetGoodIndentForLine` | P5-16 | done |
| [P5-18](phase-5/P5-18-auto-indent-enter-and-type.md) | `GetIndentForEnter`, `GetIndentActionForType`, `GetIndentMetadata` | P5-17 | done |
| [P5-19](phase-5/P5-19-indentation-guesser.md) | Indentation guesser, `DetectIndentation`, `NormalizeIndentation` | P5-18 | done |
| [P5-20](phase-5/P5-20-language-configuration-reader.md) | `LanguageConfigurationReader`: `language-configuration.json` → `LanguageConfiguration` | P5-19 | done |
| [P5-21](phase-5/P5-21-bundle-language-configurations.md) | Bundle the ten language configurations and register them | P5-20 | done |

### Step C — The bracket pair tree, bracket matching and colorization

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P5-22](phase-5/P5-22-bracket-tree-length-and-small-set.md) | Bracket pair tree (1/9): `Length`, `TextLength`, `SmallImmutableSet` | P5-21 | done |
| [P5-23](phase-5/P5-23-bracket-tree-ast-leaves-and-pairs.md) | Bracket pair tree (2/9): AST base, leaves and pairs | P5-22 | done |
| [P5-24](phase-5/P5-24-bracket-tree-ast-lists.md) | Bracket pair tree (3/9): AST list nodes | P5-23 | done |
| [P5-25](phase-5/P5-25-bracket-tree-concat-and-node-reader.md) | Bracket pair tree (4/9): `concat23Trees` and `NodeReader` | P5-24 | done |
| [P5-26](phase-5/P5-26-bracket-tree-edit-mapping.md) | Bracket pair tree (5/9): `TextEditInfo`, `BeforeEditPositionMapper`, `combineTextEditInfos` | P5-25 | done |
| [P5-27](phase-5/P5-27-bracket-tree-bracket-tokens.md) | Bracket pair tree (6/9): bracket tokens per language | P5-26 | done |
| [P5-28](phase-5/P5-28-bracket-tree-tokenizers.md) | Bracket pair tree (7/9): `TextBufferTokenizer` and `FastTokenizer` | P5-27 | done |
| [P5-29](phase-5/P5-29-bracket-tree-parser-and-result-types.md) | Bracket pair tree (8/9): parser, result types, `CallbackIterable` | P5-28 | done |
| [P5-30](phase-5/P5-30-bracket-pairs-tree.md) | Bracket pair tree (9/9): `BracketPairsTree` | P5-29 | done |
| [P5-31](phase-5/P5-31-text-model-bracket-pairs-part.md) | `TextModel.BracketPairs`: the bracket pairs part (tree path) | P5-30 | done |
| [P5-32](phase-5/P5-32-bracket-pairs-legacy-match.md) | Bracket pairs: legacy `MatchBracket` and `FindMatchingBracketUp` (no tree) | P5-31 | done |
| [P5-33](phase-5/P5-33-bracket-pairs-legacy-find.md) | Bracket pairs: legacy `FindPrevBracket`, `FindNextBracket`, `FindEnclosingBrackets` (no tree) | P5-32 | done |
| [P5-34](phase-5/P5-34-bracket-pair-colorization.md) | Bracket pair colorization | P5-33 | done |
| [P5-35](phase-5/P5-35-bracket-matching-feature.md) | BracketMatching feature: highlight, jump, select, remove | P5-34 | done |
| [P5-36](phase-5/P5-36-double-click-selects-block.md) | Double-click inside brackets or quotes selects their content | P5-35 | done |

### Step D — Typing with the language configuration (phase 3 cut points)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P5-37](phase-5/P5-37-editor-controller-test-languages.md) | Test editor with a language (`createCodeEditorServices`, the `Editor Controller` languages) | P5-36 | done |
| [P5-38](phase-5/P5-38-cursor-configuration-language.md) | `CursorConfiguration` reads the language configuration | P5-37 | done |
| [P5-39](phase-5/P5-39-enter-on-enter-rules.md) | Enter with on-enter rules and indentation rules (`_enter`) | P5-38 | done |
| [P5-40](phase-5/P5-40-smart-tab-and-shift-doc-blocks.md) | Smart Tab (`_goodIndentForLine`) and `ShiftCommand` in doc blocks | P5-39 | done |
| [P5-41](phase-5/P5-41-auto-closed-characters.md) | Track auto-closed characters with model decorations (`AutoClosedAction`) | P5-40 | done |
| [P5-42](phase-5/P5-42-auto-closing-overtype-and-delete.md) | Overtype auto-closed characters, delete auto-closed pairs | P5-41 | done |
| [P5-43](phase-5/P5-43-auto-closing-open-characters.md) | Auto-close when typing an open character (`AutoClosingOpenCharTypeOperation`) | P5-42 | done |
| [P5-44](phase-5/P5-44-surround-selection-and-composition-end.md) | Surround the selection, and auto-close at the end of a composition | P5-43 | done |
| [P5-45](phase-5/P5-45-auto-indent-on-type.md) | Auto-indent while typing (`AutoIndentOperation`) | P5-44 | done |
| [P5-46](phase-5/P5-46-electric-characters-and-bracket-balance.md) | Electric characters and bracket balance | P5-45, P5-31, P5-32 | done |

### Step E — Comment, indentation and line operations

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P5-47](phase-5/P5-47-keybinding-chords.md) | Two-key chords in keybindings (`Ctrl+K Ctrl+C`) | P5-46 | done |
| [P5-48](phase-5/P5-48-block-comment-command.md) | `BlockCommentCommand` | P5-47 | done |
| [P5-49](phase-5/P5-49-line-comment-command.md) | `LineCommentCommand` | P5-48 | done |
| [P5-50](phase-5/P5-50-line-comment-tests.md) | Line comment tests: empty lines, block fallback, mixed languages | P5-49 | done |
| [P5-51](phase-5/P5-51-comment-feature.md) | Comment feature: toggle, add, remove line comment and block comment | P5-50 | done |
| [P5-52](phase-5/P5-52-indentation-commands-and-reindent.md) | Indentation: convert indentation commands and reindent edits | P5-51 | done |
| [P5-53](phase-5/P5-53-indentation-actions.md) | Indentation feature: convert, change size, detect, reindent | P5-52 | done |
| [P5-54](phase-5/P5-54-auto-indent-on-paste.md) | Auto indent on paste | P5-53 | done |
| [P5-55](phase-5/P5-55-copy-sort-trim-commands.md) | `CopyLinesCommand`, `SortLinesCommand`, `TrimTrailingWhitespaceCommand` | P5-54 | done |
| [P5-56](phase-5/P5-56-move-lines-command.md) | `MoveLinesCommand` (with auto-indent) | P5-55 | done |
| [P5-57](phase-5/P5-57-lines-operations-copy-move-sort.md) | LinesOperations: copy, duplicate, move, sort, remove duplicates, reverse, trim | P5-56 | done |
| [P5-58](phase-5/P5-58-delete-indent-insert-lines.md) | LinesOperations: delete lines, indent / outdent lines, insert line above / below | P5-57 | done |
| [P5-59](phase-5/P5-59-delete-all-and-join-lines.md) | LinesOperations: delete all left / right, join lines | P5-58 | done |
| [P5-60](phase-5/P5-60-transpose-and-case-transforms.md) | LinesOperations: transpose and case transforms | P5-59 | done |
| [P5-61](phase-5/P5-61-auto-indent-on-type-tests.md) | Auto indent on type: the language suites of `indentation.test.ts` | P5-60 | done |

### Step F — Find, replace and Find-based multi-cursor

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P5-62](phase-5/P5-62-search-primitives.md) | Search primitives: `SearchParams`, `SearchData`, `FindMatch`, `Searcher` | P5-61 | done |
| [P5-63](phase-5/P5-63-text-model-search.md) | `TextModelSearch`: find all, next and previous matches in a model | P5-62 | done |
| [P5-64](phase-5/P5-64-piece-tree-search-and-model-api.md) | Piece tree search and `TextModel.FindMatches / FindNextMatch / FindPreviousMatch` | P5-63 | done |
| [P5-65](phase-5/P5-65-find-replace-state.md) | The Find feature folder, `FindReplaceState` and the selection search string | P5-64 | done |
| [P5-66](phase-5/P5-66-replace-pattern.md) | `ReplacePattern` and case-preserving replace | P5-65 | done |
| [P5-67](phase-5/P5-67-find-decorations.md) | `FindDecorations` and the find colors | P5-66 | done |
| [P5-68](phase-5/P5-68-find-model-navigation.md) | `FindModelBoundToEditorModel`: search, decorations, next / previous, select all | P5-67 | done |
| [P5-69](phase-5/P5-69-find-model-replace.md) | Find model: replace, replace all, `ReplaceAllCommand` | P5-68 | done |
| [P5-70](phase-5/P5-70-find-controller.md) | `FindController` and the find actions (without UI) | P5-69 | done |
| [P5-71](phase-5/P5-71-find-commands-and-keybindings.md) | Find commands, `when` clauses and VS Code's keybindings | P5-70 | done |
| [P5-72](phase-5/P5-72-find-widget-layout.md) | The find widget (XAML): layout, theme colors and the find row | P5-71 | done |
| [P5-73](phase-5/P5-73-find-widget-keyboard-and-focus.md) | The find widget: keyboard and focus routing | P5-72 | done |
| [P5-74](phase-5/P5-74-find-widget-replace-and-count.md) | The find widget: replace row, "n of m", find in selection, manual checks | P5-73 | done |
| [P5-75](phase-5/P5-75-multicursor-find-session.md) | Find-based multi-cursor: `MultiCursorSession` and Ctrl+D | P5-74 | done |
| [P5-76](phase-5/P5-76-multicursor-selected-text-tests.md) | Port tests: multi-cursor "Selected text occurrence matching" | P5-75 | done |
| [P5-77](phase-5/P5-77-selection-highlighter.md) | `SelectionHighlighter`: highlight the other occurrences of the selection | P5-76 | done |

### Step G — Hidden areas and folding

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P5-78](phase-5/P5-78-constant-time-prefix-sum-and-projections.md) | `ConstantTimePrefixSumComputer` and the identity / hidden line projections | P5-77 | done |
| [P5-79](phase-5/P5-79-view-model-lines.md) | `ViewModelLinesFromProjectedModel`: view lines, hidden areas, model changes | P5-78 | done |
| [P5-80](phase-5/P5-80-view-model-lines-conversions.md) | `ViewModelLinesFromProjectedModel`: coordinate conversions and the real `CoordinatesConverter` | P5-79 | done |
| [P5-81](phase-5/P5-81-view-model-hidden-areas.md) | The view model uses view lines: `SetHiddenAreas`, the real converter, the cursor in folded code | P5-80 | done |
| [P5-82](phase-5/P5-82-view-lines-in-renderer-and-mouse.md) | The renderer, the hit testing and the control draw view lines | P5-81 | done |
| [P5-83](phase-5/P5-83-folding-ranges.md) | Folding ranges (`FoldingRegions`, `sanitizeAndMerge`) | P5-82 | done |
| [P5-84](phase-5/P5-84-indent-range-provider.md) | Indentation folding ranges with `#region` markers and off-side rule | P5-83 | done |
| [P5-85](phase-5/P5-85-folding-model.md) | `FoldingModel`: regions, collapse state and their decorations | P5-84 | done |
| [P5-86](phase-5/P5-86-folding-model-functions.md) | Folding model functions: fold levels, recursive folding, fold jumping | P5-85 | done |
| [P5-87](phase-5/P5-87-hidden-range-model-and-folding-decorations.md) | Hidden range model and the folding decoration provider | P5-86 | done |
| [P5-88](phase-5/P5-88-folding-controller.md) | The `Folding` feature: the folding controller | P5-87 | done |
| [P5-89](phase-5/P5-89-folding-commands-and-keybindings.md) | Folding commands and their keyboard shortcuts | P5-88 | done |
| [P5-90](phase-5/P5-90-gutter-folding-controls.md) | Folding controls in the gutter: room, chevrons, mouse-over | P5-89 | done |
| [P5-91](phase-5/P5-91-folding-mouse.md) | Clicking the folding controls and the `⋯` | P5-90 | done |

### Step H — Word highlight

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P5-92](phase-5/P5-92-word-highlighter.md) | Word highlighter: textual occurrences and their decorations | P5-91 | done |
| [P5-93](phase-5/P5-93-word-highlighter-feature.md) | The `WordHighlighter` feature and its commands (F7 / Shift+F7) | P5-92 | done |

### Step I — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P5-94](phase-5/P5-94-detect-indentation-on-set-text.md) | Detect the indentation when the text is set (`detectIndentation`) | P5-93 | done |
| [P5-95](phase-5/P5-95-gallery-features-demo.md) | Gallery: the phase 5 features demo and the manual test list | P5-94 | done |
| [P5-96](phase-5/P5-96-close-phase-5.md) | Close phase 5 | all | done |

Phase 5 cards form a single chain, in table order (P5-46 also needs P5-31 and P5-32). P5-72 … P5-74, P5-90, P5-91 and P5-95 have manual checks for the maintainer.

**Deferred on purpose:** syntax folding and language-feature providers (word highlight, folding ranges: phase 8);
word wrap, bracket pair guides, minimap, sticky scroll (phase 6); overtype mode; find history, overview ruler marks,
the global find clipboard; injected text; the `editor.language.brackets` / `colorizedBracketPairs` settings.

## Phase 6 — View extras: word wrap, injected text, guides, overview ruler, minimap, sticky scroll

Monaco reference commit: `0c9c0a4295a9222e12a605537738719455ae3056`. Roadmap: [phase-6-view-extras.md](../roadmap/phase-6-view-extras.md).
**Read [phase-6/README.md](phase-6/README.md) before any phase 6 card.**

### Step A — Word wrap options and layout

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-01](phase-6/P6-01-wrapping-options.md) | Wrapping option types: `WrappingIndent`, `WordWrap`, `WordBreak`, `EditorWrappingOptions` | P5-96 | done |
| [P6-02](phase-6/P6-02-layout-wrapping-column.md) | Layout: `IsViewportWrapping` and `WrappingColumn` | P6-01 | done |

### Step B — Injected text in the text model

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-03](phase-6/P6-03-injected-text-options.md) | `InjectedTextOptions` and the `before` / `after` decoration options | P5-96 | done |
| [P6-04](phase-6/P6-04-line-injected-text.md) | `LineInjectedText`: applying injected text and fixed-width ranges | P6-03 | todo |
| [P6-05](phase-6/P6-05-text-model-injected-text-queries.md) | `TextModel.GetLineInjectedText` and `GetInjectedTextDecorations` | P6-04 | todo |
| [P6-06](phase-6/P6-06-injected-text-changed-event.md) | The injected text changed event of the text model | P6-05 | todo |

### Step C — Line-break data and the monospace line-break computer

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-07](phase-6/P6-07-model-line-projection-data.md) | `ModelLineProjectionData`: break offsets and offset translation | P6-04 | todo |
| [P6-08](phase-6/P6-08-projection-data-normalization.md) | `ModelLineProjectionData`: normalizing positions around injected text | P6-07 | todo |
| [P6-09](phase-6/P6-09-line-breaks-computer-primitives.md) | Line-break computer interfaces, the wrapping character classifier and width helpers | P6-08 | todo |
| [P6-10](phase-6/P6-10-create-line-breaks.md) | `MonospaceLineBreaksComputerFactory` and `CreateLineBreaks` | P6-09 | todo |
| [P6-11](phase-6/P6-11-fixed-width-injected-text-wrapping-tests.md) | Line breaks with fixed-width and width-only injected text (tests) | P6-10 | todo |
| [P6-12](phase-6/P6-12-line-breaks-from-previous-line-breaks.md) | `CreateLineBreaksFromPreviousLineBreaks` (the resize fast path) | P6-11 | todo |
| [P6-13](phase-6/P6-13-two-cell-fullwidth-tests.md) | Line breaks with two-cell full-width characters (tests) | P6-12 | todo |

### Step D — Wrapped model line projections

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-14](phase-6/P6-14-injected-text-inline-decorations.md) | `InjectedTextInlineDecorationsComputer` and `ViewLineData.InlineDecorations` | P6-08 | todo |
| [P6-15](phase-6/P6-15-wrapped-model-line-projection.md) | `ModelLineProjection` for wrapped lines and injected text (content and positions) | P6-14 | todo |
| [P6-16](phase-6/P6-16-wrapped-projection-view-lines-data.md) | Wrapped projection: `GetViewLinesData` with tokens and injected text | P6-15 | todo |

### Step E — Word wrap in the view model, the renderer and the control

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-17](phase-6/P6-17-view-model-lines-wrapping-settings.md) | `ViewModelLinesFromProjectedModel`: wrapping settings and wrapped construction | P6-16, P6-13, P6-05 | todo |
| [P6-18](phase-6/P6-18-view-model-lines-model-changes-with-line-breaks.md) | `ViewModelLinesFromProjectedModel`: model changes with line breaks, `GetInjectedTextAt` | P6-17 | todo |
| [P6-19](phase-6/P6-19-view-lines-wrapping-tests.md) | Port the wrapping tests of `modelLineProjection.test.ts` | P6-18 | todo |
| [P6-20](phase-6/P6-20-view-model-word-wrap.md) | The view model wraps lines: settings, model changes, content width | P6-19, P6-02 | todo |
| [P6-21](phase-6/P6-21-view-model-injected-text-changes.md) | The view model and the cursors follow injected text changes | P6-20, P6-06 | todo |
| [P6-22](phase-6/P6-22-word-wrap-cursor-tests.md) | Port the word wrap and injected text tests of the view model and the cursors | P6-21 | todo |
| [P6-23](phase-6/P6-23-view-model-decorations-wrap-tests.md) | Port the word wrap tests of `viewModelDecorations.test.ts` | P6-22 | todo |
| [P6-24](phase-6/P6-24-render-wrapped-lines.md) | Drawing, hit testing and folding on wrapped lines | P6-23 | todo |
| [P6-25](phase-6/P6-25-injected-text-rendering-and-mouse.md) | Injected text: its inline decorations in the viewport and the mouse | P6-24 | todo |
| [P6-26](phase-6/P6-26-control-word-wrap-properties.md) | The control's word wrap properties and a gallery switch | P6-25 | todo |

### Step F — Indent and bracket pair guides

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-27](phase-6/P6-27-guide-types.md) | Guide types: `IndentGuide`, `BracketGuideOptions`, `ActiveIndentGuideInfo` | P5-96 | todo |
| [P6-28](phase-6/P6-28-lines-indent-guides.md) | `GuidesTextModelPart`: `GetLinesIndentGuides` | P6-27 | todo |
| [P6-29](phase-6/P6-29-active-indent-guide.md) | `GetActiveIndentGuide` and the 18 indent guide tests | P6-28 | todo |
| [P6-30](phase-6/P6-30-lines-bracket-guides.md) | `GuidesTextModelPart.GetLinesBracketGuides` | P6-29 | todo |
| [P6-31](phase-6/P6-31-view-lines-indent-guides.md) | View lines: indent guides and the active indent guide | P6-30, P6-20 | todo |
| [P6-32](phase-6/P6-32-view-lines-bracket-guides.md) | View lines and view model: bracket guides, guide options | P6-31 | todo |
| [P6-33](phase-6/P6-33-guide-colors.md) | The colors of indent and bracket pair guides | P6-27 | todo |
| [P6-34](phase-6/P6-34-guides-by-line.md) | The guides of each visible line (`getGuidesByLine`) | P6-32, P6-33 | todo |
| [P6-35](phase-6/P6-35-draw-guides.md) | Drawing the guides, and the control's guide options | P6-34 | todo |

### Step G — The decorations overview ruler

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-36](phase-6/P6-36-overview-ruler-decoration-options.md) | Overview ruler options of decorations and `GetOverviewRulerDecorations` | P5-96 | todo |
| [P6-37](phase-6/P6-37-overview-ruler-decorations-groups.md) | `ViewModel.GetAllOverviewRulerDecorations`: groups per color | P6-36 | todo |
| [P6-38](phase-6/P6-38-draw-overview-ruler.md) | Drawing the decorations overview ruler | P6-37, P6-33 | todo |
| [P6-39](phase-6/P6-39-features-overview-ruler-marks.md) | Find, highlights and bracket matching mark the overview ruler | P6-38 | todo |

### Step H — The minimap

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-40](phase-6/P6-40-minimap-options.md) | Minimap option and layout types | P6-02 | todo |
| [P6-41](phase-6/P6-41-minimap-layout.md) | `ComputeMinimapLayout` and the minimap in the editor layout | P6-40 | todo |
| [P6-42](phase-6/P6-42-minimap-decoration-options.md) | Minimap options of decorations | P6-36 | todo |
| [P6-43](phase-6/P6-43-minimap-view-model-data.md) | The view model feeds the minimap: lines data and minimap decorations | P6-42, P6-41, P6-37 | todo |
| [P6-44](phase-6/P6-44-minimap-char-renderer.md) | Minimap character renderer, char sheet and pre-baked font data | P5-96 | todo |
| [P6-45](phase-6/P6-45-minimap-font-sample-data.md) | Minimap font sample data with Win2D (scales above 2) | P6-44 | todo |
| [P6-46](phase-6/P6-46-minimap-options-and-colors.md) | `MinimapOptions`, the token color tracker and the minimap colors | P6-44, P6-43 | todo |
| [P6-47](phase-6/P6-47-minimap-layout-slider.md) | `MinimapLayout`: rendered line range and slider position | P6-46 | todo |
| [P6-48](phase-6/P6-48-minimap-sampling-state.md) | `MinimapSamplingState`: which view lines a sampled minimap shows | P6-46 | todo |
| [P6-49](phase-6/P6-49-minimap-model.md) | The minimap model: `IMinimapModel` over the view model | P6-47, P6-48 | todo |
| [P6-50](phase-6/P6-50-minimap-render-lines.md) | Rendering minimap lines into a pixel buffer | P6-49 | todo |
| [P6-51](phase-6/P6-51-minimap-render-decorations.md) | Rendering minimap selections and decorations | P6-50 | todo |
| [P6-52](phase-6/P6-52-draw-minimap.md) | Drawing the minimap with Win2D: canvas, slider, shadow, auto-hide | P6-51, P6-45 | todo |
| [P6-53](phase-6/P6-53-minimap-mouse.md) | Minimap pointer input: click to reveal, slider drag, hover | P6-52 | todo |
| [P6-54](phase-6/P6-54-minimap-features-and-control.md) | Features mark the minimap; the control's minimap properties | P6-53, P6-39 | todo |

### Step I — Sticky scroll

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-55](phase-6/P6-55-sticky-scroll-model-types.md) | Sticky scroll options and model types | P5-96 | todo |
| [P6-56](phase-6/P6-56-sticky-model-from-folding-regions.md) | The sticky model from indentation folding regions | P6-55 | todo |
| [P6-57](phase-6/P6-57-sticky-line-candidate-provider.md) | `StickyLineCandidateProvider` | P6-56 | todo |
| [P6-58](phase-6/P6-58-sticky-scroll-feature.md) | The `StickyScroll` feature: widget state and re-render triggers | P6-57 | todo |
| [P6-59](phase-6/P6-59-draw-sticky-scroll.md) | Drawing the sticky lines | P6-58 | todo |
| [P6-60](phase-6/P6-60-sticky-scroll-mouse.md) | Sticky scroll pointer input: reveal, shift-click, folding chevron, hover | P6-59 | todo |
| [P6-61](phase-6/P6-61-sticky-scroll-command-and-control.md) | Toggle sticky scroll: command, control property, gallery | P6-60 | todo |

### Step J — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-62](phase-6/P6-62-gallery-view-extras-demo.md) | Gallery: the phase 6 samples and the manual test list | P6-61, P6-54, P6-35, P6-26 | todo |
| [P6-63](phase-6/P6-63-close-phase-6.md) | Close phase 6 | all | todo |

Take phase 6 cards in table order (each card's "Depends on" is satisfied by the cards above it). P6-26, P6-35, P6-45,
P6-52 … P6-54, P6-59 … P6-62 have manual checks for the maintainer.

**Deferred on purpose:** `renderWhitespace`; the word wrap indicator drawing; minimap section headers; the outline model of
sticky scroll, its click-link gesture and its keyboard navigation (phase 8 / follow-up); the DOM line-break strategy
(`wrappingStrategy: advanced`); accessibility-driven wrapping (phase 7); the generic `OverviewRuler` of the diff editor (phase 9).

## How to run a card with a coding agent (Claude Code)

Start a fresh session for each card, on the phase branch, with a clean working tree, and type:

```
/next-task          # picks the first `todo` card whose dependencies are `done`
/next-task P1-07    # or a specific card
```

The skill (`.claude/skills/next-task/SKILL.md`) implements the card, runs the build and the tests, updates the card
and this index, and makes **one commit without any co-author trailer**. It never pushes. Review the commit
(`git show`), then push when you are happy with it.
