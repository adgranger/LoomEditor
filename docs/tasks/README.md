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

**Deferred on purpose:** syntax folding and language-feature providers (word highlight, folding ranges: **not delivered in phase 8**, ADR 0011 decision 7);
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
| [P6-04](phase-6/P6-04-line-injected-text.md) | `LineInjectedText`: applying injected text and fixed-width ranges | P6-03 | done |
| [P6-05](phase-6/P6-05-text-model-injected-text-queries.md) | `TextModel.GetLineInjectedText` and `GetInjectedTextDecorations` | P6-04 | done |
| [P6-06](phase-6/P6-06-injected-text-changed-event.md) | The injected text changed event of the text model | P6-05 | done |

### Step C — Line-break data and the monospace line-break computer

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-07](phase-6/P6-07-model-line-projection-data.md) | `ModelLineProjectionData`: break offsets and offset translation | P6-04 | done |
| [P6-08](phase-6/P6-08-projection-data-normalization.md) | `ModelLineProjectionData`: normalizing positions around injected text | P6-07 | done |
| [P6-09](phase-6/P6-09-line-breaks-computer-primitives.md) | Line-break computer interfaces, the wrapping character classifier and width helpers | P6-08 | done |
| [P6-10](phase-6/P6-10-create-line-breaks.md) | `MonospaceLineBreaksComputerFactory` and `CreateLineBreaks` | P6-09 | done |
| [P6-11](phase-6/P6-11-fixed-width-injected-text-wrapping-tests.md) | Line breaks with fixed-width and width-only injected text (tests) | P6-10 | done |
| [P6-12](phase-6/P6-12-line-breaks-from-previous-line-breaks.md) | `CreateLineBreaksFromPreviousLineBreaks` (the resize fast path) | P6-11 | done |
| [P6-13](phase-6/P6-13-two-cell-fullwidth-tests.md) | Line breaks with two-cell full-width characters (tests) | P6-12 | done |

### Step D — Wrapped model line projections

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-14](phase-6/P6-14-injected-text-inline-decorations.md) | `InjectedTextInlineDecorationsComputer` and `ViewLineData.InlineDecorations` | P6-08 | done |
| [P6-15](phase-6/P6-15-wrapped-model-line-projection.md) | `ModelLineProjection` for wrapped lines and injected text (content and positions) | P6-14 | done |
| [P6-16](phase-6/P6-16-wrapped-projection-view-lines-data.md) | Wrapped projection: `GetViewLinesData` with tokens and injected text | P6-15 | done |

### Step E — Word wrap in the view model, the renderer and the control

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-17](phase-6/P6-17-view-model-lines-wrapping-settings.md) | `ViewModelLinesFromProjectedModel`: wrapping settings and wrapped construction | P6-16, P6-13, P6-05 | done |
| [P6-18](phase-6/P6-18-view-model-lines-model-changes-with-line-breaks.md) | `ViewModelLinesFromProjectedModel`: model changes with line breaks, `GetInjectedTextAt` | P6-17 | done |
| [P6-19](phase-6/P6-19-view-lines-wrapping-tests.md) | Port the wrapping tests of `modelLineProjection.test.ts` | P6-18 | done |
| [P6-20](phase-6/P6-20-view-model-word-wrap.md) | The view model wraps lines: settings, model changes, content width | P6-19, P6-02 | done |
| [P6-21](phase-6/P6-21-view-model-injected-text-changes.md) | The view model and the cursors follow injected text changes | P6-20, P6-06 | done |
| [P6-22](phase-6/P6-22-word-wrap-cursor-tests.md) | Port the word wrap and injected text tests of the view model and the cursors | P6-21 | done |
| [P6-23](phase-6/P6-23-view-model-decorations-wrap-tests.md) | Port the word wrap tests of `viewModelDecorations.test.ts` | P6-22 | done |
| [P6-24](phase-6/P6-24-render-wrapped-lines.md) | Drawing, hit testing and folding on wrapped lines | P6-23 | done |
| [P6-25](phase-6/P6-25-injected-text-rendering-and-mouse.md) | Injected text: its inline decorations in the viewport and the mouse | P6-24 | done |
| [P6-26](phase-6/P6-26-control-word-wrap-properties.md) | The control's word wrap properties and a gallery switch | P6-25 | done |

### Step F — Indent and bracket pair guides

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-27](phase-6/P6-27-guide-types.md) | Guide types: `IndentGuide`, `BracketGuideOptions`, `ActiveIndentGuideInfo` | P5-96 | done |
| [P6-28](phase-6/P6-28-lines-indent-guides.md) | `GuidesTextModelPart`: `GetLinesIndentGuides` | P6-27 | done |
| [P6-29](phase-6/P6-29-active-indent-guide.md) | `GetActiveIndentGuide` and the 18 indent guide tests | P6-28 | done |
| [P6-30](phase-6/P6-30-lines-bracket-guides.md) | `GuidesTextModelPart.GetLinesBracketGuides` | P6-29 | done |
| [P6-31](phase-6/P6-31-view-lines-indent-guides.md) | View lines: indent guides and the active indent guide | P6-30, P6-20 | done |
| [P6-32](phase-6/P6-32-view-lines-bracket-guides.md) | View lines and view model: bracket guides, guide options | P6-31 | done |
| [P6-33](phase-6/P6-33-guide-colors.md) | The colors of indent and bracket pair guides | P6-27 | done |
| [P6-34](phase-6/P6-34-guides-by-line.md) | The guides of each visible line (`getGuidesByLine`) | P6-32, P6-33 | done |
| [P6-35](phase-6/P6-35-draw-guides.md) | Drawing the guides, and the control's guide options | P6-34 | done |

### Step G — The decorations overview ruler

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-36](phase-6/P6-36-overview-ruler-decoration-options.md) | Overview ruler options of decorations and `GetOverviewRulerDecorations` | P5-96 | done |
| [P6-37](phase-6/P6-37-overview-ruler-decorations-groups.md) | `ViewModel.GetAllOverviewRulerDecorations`: groups per color | P6-36 | done |
| [P6-38](phase-6/P6-38-draw-overview-ruler.md) | Drawing the decorations overview ruler | P6-37, P6-33 | done |
| [P6-39](phase-6/P6-39-features-overview-ruler-marks.md) | Find, highlights and bracket matching mark the overview ruler | P6-38 | done |

### Step H — The minimap

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-40](phase-6/P6-40-minimap-options.md) | Minimap option and layout types | P6-02 | done |
| [P6-41](phase-6/P6-41-minimap-layout.md) | `ComputeMinimapLayout` and the minimap in the editor layout | P6-40 | done |
| [P6-42](phase-6/P6-42-minimap-decoration-options.md) | Minimap options of decorations | P6-36 | done |
| [P6-43](phase-6/P6-43-minimap-view-model-data.md) | The view model feeds the minimap: lines data and minimap decorations | P6-42, P6-41, P6-37 | done |
| [P6-44](phase-6/P6-44-minimap-char-renderer.md) | Minimap character renderer, char sheet and pre-baked font data | P5-96 | done |
| [P6-45](phase-6/P6-45-minimap-font-sample-data.md) | Minimap font sample data with Win2D (scales above 2) | P6-44 | done |
| [P6-46](phase-6/P6-46-minimap-options-and-colors.md) | `MinimapOptions`, the token color tracker and the minimap colors | P6-44, P6-43 | done |
| [P6-47](phase-6/P6-47-minimap-layout-slider.md) | `MinimapLayout`: rendered line range and slider position | P6-46 | done |
| [P6-48](phase-6/P6-48-minimap-sampling-state.md) | `MinimapSamplingState`: which view lines a sampled minimap shows | P6-46 | done |
| [P6-49](phase-6/P6-49-minimap-model.md) | The minimap model: `IMinimapModel` over the view model | P6-47, P6-48 | done |
| [P6-50](phase-6/P6-50-minimap-render-lines.md) | Rendering minimap lines into a pixel buffer | P6-49 | done |
| [P6-51](phase-6/P6-51-minimap-render-decorations.md) | Rendering minimap selections and decorations | P6-50 | done |
| [P6-52](phase-6/P6-52-draw-minimap.md) | Drawing the minimap with Win2D: canvas, slider, shadow, auto-hide | P6-51, P6-45 | done |
| [P6-53](phase-6/P6-53-minimap-mouse.md) | Minimap pointer input: click to reveal, slider drag, hover | P6-52 | done |
| [P6-54](phase-6/P6-54-minimap-features-and-control.md) | Features mark the minimap; the control's minimap properties | P6-53, P6-39 | done |

### Step I — Sticky scroll

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-55](phase-6/P6-55-sticky-scroll-model-types.md) | Sticky scroll options and model types | P5-96 | done |
| [P6-56](phase-6/P6-56-sticky-model-from-folding-regions.md) | The sticky model from indentation folding regions | P6-55 | done |
| [P6-57](phase-6/P6-57-sticky-line-candidate-provider.md) | `StickyLineCandidateProvider` | P6-56 | done |
| [P6-58](phase-6/P6-58-sticky-scroll-feature.md) | The `StickyScroll` feature: widget state and re-render triggers | P6-57 | done |
| [P6-59](phase-6/P6-59-draw-sticky-scroll.md) | Drawing the sticky lines | P6-58 | done |
| [P6-60](phase-6/P6-60-sticky-scroll-mouse.md) | Sticky scroll pointer input: reveal, shift-click, folding chevron, hover | P6-59 | done |
| [P6-61](phase-6/P6-61-sticky-scroll-command-and-control.md) | Toggle sticky scroll: command, control property, gallery | P6-60 | done |

### Step J — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P6-62](phase-6/P6-62-gallery-view-extras-demo.md) | Gallery: the phase 6 samples and the manual test list | P6-61, P6-54, P6-35, P6-26 | done |
| [P6-63](phase-6/P6-63-close-phase-6.md) | Close phase 6 | all | done |

Take phase 6 cards in table order (each card's "Depends on" is satisfied by the cards above it). P6-26, P6-35, P6-45,
P6-52 … P6-54, P6-59 … P6-62 have manual checks for the maintainer.

**Deferred on purpose:** `renderWhitespace`; the word wrap indicator drawing; minimap section headers; the outline model of
sticky scroll, its click-link gesture and its keyboard navigation (**not delivered in phase 8** / follow-up); the DOM line-break strategy
(`wrappingStrategy: advanced`); accessibility-driven wrapping (done in phase 7); the generic `OverviewRuler` of the diff editor (phase 9).

## Phase 7 — Full UI Automation accessibility

Monaco reference commit: `0c9c0a4295a9222e12a605537738719455ae3056`. Roadmap: [phase-7-accessibility.md](../roadmap/phase-7-accessibility.md). Design: [ADR 0010](../adr/0010-accessibility.md).
**Read [phase-7/README.md](phase-7/README.md) before any phase 7 card** (it also holds the spec of the UIA text units and `Move` rules).

### Step A — Screen reader mode

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P7-01](phase-7/P7-01-accessibility-options.md) | Accessibility options and the effective wrapping indent | P6-63 | done |
| [P7-02](phase-7/P7-02-model-dominated-by-long-lines.md) | `TextModel.IsDominatedByLongLines` | P7-01 | done |
| [P7-03](phase-7/P7-03-layout-forced-wrapping.md) | Layout: force viewport wrapping for long-line models in screen reader mode | P7-02 | done |
| [P7-04](phase-7/P7-04-control-accessibility-support.md) | The control: `AccessibilitySupport` and screen reader detection | P7-03 | done |

### Step B — The UIA text model (`LoomEditor.View/Accessibility/`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P7-05](phase-7/P7-05-automation-text-document.md) | `AutomationTextDocument` and `AutomationTextRange` (text, compare, validate) | P7-04 | done |
| [P7-06](phase-7/P7-06-units-character-paragraph-document.md) | Text units: character, paragraph, document | P7-05 | done |
| [P7-07](phase-7/P7-07-unit-word.md) | Text unit: word (and format) | P7-06 | done |
| [P7-08](phase-7/P7-08-units-line-page.md) | Text units: line (view line) and page | P7-07 | done |
| [P7-09](phase-7/P7-09-expand-and-move.md) | Expand and move text ranges | P7-08 | done |
| [P7-10](phase-7/P7-10-find-text.md) | Find text in a range | P7-09 | done |
| [P7-11](phase-7/P7-11-automation-geometry.md) | Geometry: bounding rectangles, point to position, visible range | P7-10 | done |

### Step C — Announcements, events, tab focus

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P7-12](phase-7/P7-12-announcer-and-messages.md) | `IEditorAnnouncer` and the announcement messages | P7-11 | done |
| [P7-13](phase-7/P7-13-find-announces-results.md) | Find announces its results | P7-12 | done |
| [P7-14](phase-7/P7-14-word-highlight-announces.md) | Word highlight navigation announces the occurrence | P7-13 | done |
| [P7-15](phase-7/P7-15-automation-event-coalescer.md) | `AutomationEventCoalescer`: selection and text change events | P7-14 | done |
| [P7-16](phase-7/P7-16-tab-focus-mode.md) | Tab focus mode (`Ctrl+M`) and the `TabMovesFocus` property | P7-15 | done |

### Step D — The UIA peer (`LoomEditor.WinUI/Accessibility/`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P7-17](phase-7/P7-17-peer-text-provider.md) | The UIA peer: text and value patterns, read members of the range provider (tracer bullet) | P7-16 | done |
| [P7-18](phase-7/P7-18-range-provider-navigation.md) | The range provider: navigation, find, selection and scrolling | P7-17 | done |
| [P7-19](phase-7/P7-19-uia-events-and-notifications.md) | UIA events, notifications and `ITextProvider2` | P7-18 | done |

### Step E — High contrast

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P7-20](phase-7/P7-20-contrast-ratio.md) | Contrast ratio helper and the contrast of the bundled themes | P7-19 | done |
| [P7-21](phase-7/P7-21-selection-foreground.md) | High contrast: the selected text color (`editor.selectionForeground`) | P7-20 | done |
| [P7-22](phase-7/P7-22-system-high-contrast-theme.md) | The Windows system high contrast theme | P7-21 | done |
| [P7-23](phase-7/P7-23-control-follows-high-contrast.md) | The control follows Windows high contrast | P7-22 | done |

### Step F — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P7-24](phase-7/P7-24-gallery-accessibility-demo.md) | Gallery: accessibility switches and the in-process UIA self-check | P7-23 | done |
| [P7-25](phase-7/P7-25-manual-test-plan.md) | The manual test plan: Narrator, NVDA, Accessibility Insights | P7-24 | done |
| [P7-26](phase-7/P7-26-close-phase-7.md) | Close phase 7 | all | done |

Take phase 7 cards in table order (each card's "Depends on" is satisfied by the cards above it; step D needs steps B and C, which come first). P7-04, P7-16, P7-17 … P7-19, P7-23 and P7-24 have manual checks for the maintainer.

**Deferred on purpose:** accessibility help dialog, `ariaLabel` / `ariaRole` options, reduced motion, the UIA text edit pattern and annotations, per-token text attributes, the `cursorWordAccessibility*` keybindings,
localization of announcements, a UIA client based automated test (details in [phase-7/README.md](phase-7/README.md) and [ADR 0010](../adr/0010-accessibility.md)).

## Phase 8 — LSP client: completion, hover, diagnostics, go-to-definition

Monaco reference commit: `0c9c0a4295a9222e12a605537738719455ae3056`. Roadmap: [phase-8-lsp.md](../roadmap/phase-8-lsp.md). Design: [ADR 0011](../adr/0011-language-server-client.md).
**Read [phase-8/README.md](phase-8/README.md) before any phase 8 card** (conventions, threading rules, test helpers). Monaco has no LSP: the cards of step F are specified by the LSP 3.17 specification and by the card itself.

### Step A — Language features in Core (`LoomEditor.Core`, public API)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P8-01](phase-8/P8-01-language-feature-types.md) | Language feature contracts: completion, hover, definition and `MarkdownString` (public API of Core) | P7-26 | done |
| [P8-02](phase-8/P8-02-language-feature-registry.md) | `LanguageFeatureRegistry<T>` and `LanguageFeaturesService` | P8-01 | done |
| [P8-03](phase-8/P8-03-marker-service.md) | `MarkerService`: markers per owner (public API of Core) | P8-02 | done |
| [P8-04](phase-8/P8-04-marker-decorations.md) | `MarkerDecorations`: markers become model decorations | P8-03 | done |
| [P8-05](phase-8/P8-05-editor-context-wiring.md) | `EditorContext` and the control expose `LanguageFeatures`, `Markers`, `DocumentUri`; `UiContinuation` | P8-04 | done |

### Step B — Diagnostics on screen

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P8-06](phase-8/P8-06-squiggles.md) | Squiggles: error, warning, info and hint underlines, and the ruler colors of markers | P8-05 | done |

### Step C — Hover

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P8-07](phase-8/P8-07-markdown-subset.md) | A Markdown subset parser for hover and documentation text | P8-06 | done |
| [P8-08](phase-8/P8-08-mouse-move-events.md) | Mouse move and mouse leave events for features | P8-07 | done |
| [P8-09](phase-8/P8-09-hover-computation.md) | Hover computation: anchors, parts, `ContentHoverComputer` and `HoverOperation` | P8-08 | done |
| [P8-10](phase-8/P8-10-hover-controller.md) | `HoverController` and the `Hover` feature | P8-09 | done |
| [P8-11](phase-8/P8-11-overlay-and-hover-widget.md) | The overlay layer and the hover popup (WinUI) | P8-10 | done |

### Step D — Completion

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P8-12](phase-8/P8-12-fuzzy-score.md) | The fuzzy filter of completion: `FuzzyScore`, `fuzzyScore`, `anyScore`, `createMatches` | P8-11 | done |
| [P8-13](phase-8/P8-13-fuzzy-graceful.md) | The graceful fuzzy scorers | P8-12 | done |
| [P8-14](phase-8/P8-14-suggest-items.md) | Suggest items: `SuggestItem` and `SuggestItems.ProvideAsync` | P8-13 | done |
| [P8-15](phase-8/P8-15-completion-model.md) | `CompletionModel`: the filtered and sorted list | P8-14 | done |
| [P8-16](phase-8/P8-16-suggest-model.md) | `SuggestModel`: trigger, cancel, refilter and retrigger (the state machine) | P8-15 | done |
| [P8-17](phase-8/P8-17-suggest-auto-triggers.md) | Automatic triggers: quick suggest while typing and trigger characters | P8-16 | done |
| [P8-18](phase-8/P8-18-suggest-list-state.md) | `SuggestListState`: selection and scrolling of the completion list, and the initial selection | P8-17 | done |
| [P8-19](phase-8/P8-19-suggest-controller.md) | `SuggestController`: show the list, move in it, accept an item | P8-18 | done |
| [P8-20](phase-8/P8-20-suggest-async-edits-and-commit-characters.md) | Suggest: additional edits that arrive after the accept, and commit characters | P8-19 | done |
| [P8-21](phase-8/P8-21-suggest-feature.md) | The `Suggest` feature: commands, keybindings and context keys | P8-20 | done |
| [P8-22](phase-8/P8-22-suggest-widget.md) | The completion list (WinUI) | P8-21 | done |

### Step E — Go to definition

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P8-23](phase-8/P8-23-go-to-definition.md) | Go to definition (`F12` and `Ctrl+click`) | P8-22 | done |

### Step F — The LSP client (`LoomEditor.Lsp`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P8-24](phase-8/P8-24-lsp-project-skeleton.md) | The `LoomEditor.Lsp` project, its test projects and the architecture rules | P8-23 | done |
| [P8-25](phase-8/P8-25-message-framing.md) | LSP message framing: `Content-Length` reader and writer | P8-24 | done |
| [P8-26](phase-8/P8-26-json-rpc-connection.md) | The JSON-RPC connection: requests, notifications, cancellation, server requests | P8-25 | done |
| [P8-27](phase-8/P8-27-lsp-protocol-types.md) | The LSP protocol types, source-generated JSON and the union decoders | P8-26 | done |
| [P8-28](phase-8/P8-28-fake-language-server.md) | The fake language server (test double) and its test host | P8-27 | done |
| [P8-29](phase-8/P8-29-lsp-client-lifecycle.md) | `LspClient`: start the server process, initialize, shut down, survive a crash | P8-28 | done |
| [P8-30](phase-8/P8-30-document-sync.md) | Text document synchronization: `didOpen`, incremental `didChange`, `didClose` | P8-29 | done |
| [P8-31](phase-8/P8-31-lsp-conversions.md) | Converting LSP types to the editor's types (`LspConvert`) | P8-30 | done |
| [P8-32](phase-8/P8-32-diagnostics-binding.md) | Diagnostics: server `publishDiagnostics` become markers | P8-31 | done |
| [P8-33](phase-8/P8-33-completion-provider.md) | `LspCompletionProvider`: completion and resolve over LSP | P8-32 | done |
| [P8-34](phase-8/P8-34-hover-and-definition-providers.md) | `LspHoverProvider` and `LspDefinitionProvider` | P8-33 | done |
| [P8-35](phase-8/P8-35-lsp-editor-session.md) | `LspEditorSession`: one call connects an editor to a language server | P8-34 | done |
| [P8-36](phase-8/P8-36-end-to-end-tests.md) | End-to-end tests: the real editor features against the fake language server | P8-35 | done |

### Step G — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P8-37](phase-8/P8-37-gallery-language-server.md) | Gallery: connect to a real language server, demo providers and the LSP self-check | P8-36 | done |
| [P8-38](phase-8/P8-38-manual-test-plan.md) | The manual test plan of phase 8 | P8-37 | done |
| [P8-39](phase-8/P8-39-close-phase-8.md) | Close phase 8 | all phase 8 cards | done |

Take phase 8 cards in table order (each card's "Depends on" is satisfied by the cards above it). P8-11, P8-16, P8-26, P8-27, P8-29 and P8-37 are flagged **Recommended model: Opus** (reason in the card). P8-06, P8-08, P8-11, P8-22 and P8-37 have manual checks for the maintainer; whether the Gallery works with a real language server (`csharp-ls`) is only verified by a human (P8-38).

**Deferred on purpose:** snippets, signature help, references, rename, code actions, formatting, semantic tokens, inlay hints, document highlights and folding ranges from the server, document links, workspace symbols, pull diagnostics, several documents or workspace folders, server-initiated edits, peek definition, Ctrl+hover link underlines, the diagnostics list (`F8`), screen reader support of the hover popup and the completion list, bundling a server (**not delivered in phase 8**; details in [phase-8/README.md](phase-8/README.md) and [ADR 0011](../adr/0011-language-server-client.md)).

## Phase 9 — Diff editor

Monaco reference commit: `0c9c0a4295a9222e12a605537738719455ae3056`. Roadmap: [phase-9-diff-editor.md](../roadmap/phase-9-diff-editor.md). Design: [ADR 0012](../adr/0012-diff-editor.md).
**Read [phase-9/README.md](phase-9/README.md) before any phase 9 card** (conventions, Monaco → LoomEditor concepts, what is out of scope).

### Step A — The diff algorithm (`LoomEditor.Core`, internal)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P9-01](phase-9/P9-01-line-range.md) | `LineRange` and `LineRangeSet` | P8-39 | done |
| [P9-02](phase-9/P9-02-sequence-contracts.md) | Sequence contracts: `ISequence`, `SequenceDiff`, `OffsetPair`, timeouts, `LineSequence` | P9-01 | done |
| [P9-03](phase-9/P9-03-myers-and-dynamic-programming.md) | `MyersDiffAlgorithm` and `DynamicProgrammingDiffing` | P9-02 | done |
| [P9-04](phase-9/P9-04-lines-slice-char-sequence.md) | `LinesSliceCharSequence` | P9-03 | done |
| [P9-05](phase-9/P9-05-heuristics-shifting.md) | Diff heuristics (1/2): `optimizeSequenceDiffs` and `removeShortMatches` | P9-04 | done |
| [P9-06](phase-9/P9-06-heuristics-words.md) | Diff heuristics (2/2): word extension and very short matches | P9-05 | done |
| [P9-07](phase-9/P9-07-range-mappings.md) | Text abstraction and range mappings: `LineRangeMapping`, `RangeMapping`, `LinesDiff` | P9-06 | done |
| [P9-08](phase-9/P9-08-default-lines-diff-computer.md) | `DefaultLinesDiffComputer` (without moved lines) | P9-07 | done |
| [P9-09](phase-9/P9-09-moved-lines.md) | Moved lines: `ComputeMovedLines` | P9-08 | done |
| [P9-10](phase-9/P9-10-diff-fixtures.md) | Monaco's diff fixture tests | P9-09 | done |
| [P9-11](phase-9/P9-11-document-diff-provider.md) | `DocumentDiffProvider`: diffing two models off the UI thread | P9-10 | done |

### Step B — The diff model (`LoomEditor.View/DiffEditor`, no UI)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P9-12](phase-9/P9-12-diff-options-and-state.md) | `DiffEditorOptions`, `DiffState` and `DiffMapping` | P9-11 | done |
| [P9-13](phase-9/P9-13-unchanged-region.md) | `UnchangedRegion`: which unchanged lines are hidden | P9-12 | done |
| [P9-14](phase-9/P9-14-diff-editor-view-model.md) | `DiffEditorViewModel`: computing and publishing the diff | P9-13 | done |
| [P9-15](phase-9/P9-15-line-range-alignment.md) | Line range alignment: how much empty space each side needs | P9-14 | done |
| [P9-16](phase-9/P9-16-diff-colors-and-styles.md) | Diff theme colors and decoration styles | P9-12 | done |
| [P9-17](phase-9/P9-17-diff-decorations.md) | Diff decorations: from a `DiffState` to decorations of the two models | P9-16, P9-14 | done |

### Step C — Editor infrastructure (View, Rendering, WinUI)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P9-18](phase-9/P9-18-view-zones.md) | View zones: gaps with a purpose, anchored to a model line | P9-17 | done |
| [P9-19](phase-9/P9-19-draw-view-zones.md) | Drawing view zones: the diagonal fill | P9-18, P9-16 | done |
| [P9-20](phase-9/P9-20-read-only.md) | Read-only editor | P9-18 | done |
| [P9-21](phase-9/P9-21-control-host-hooks.md) | Control hooks for a host: zones, scroll, hidden areas and decorations | P9-19, P9-20 | done |

### Step D — The control (`LoomEditor.WinUI`, public `LoomDiffEditor`)

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P9-22](phase-9/P9-22-diff-editor-control-shell.md) | `LoomDiffEditor`: the control, its two editors, the sash and the public API | P9-21, P9-17, P9-15 | done |
| [P9-23](phase-9/P9-23-side-by-side.md) | Side by side: aligned view zones and synchronized scrolling | P9-22 | done |
| [P9-24](phase-9/P9-24-inline-view.md) | Inline view: deleted lines drawn inside the modified editor | P9-23 | done |
| [P9-25](phase-9/P9-25-hide-unchanged-regions.md) | Hide unchanged regions: the collapsed bar | P9-24 | done |
| [P9-26](phase-9/P9-26-go-to-change.md) | Go to next / previous change | P9-25 | done |

### Step E — Overview ruler

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P9-27](phase-9/P9-27-overview-zone-manager.md) | `OverviewZoneManager`: zones of a generic overview ruler | P9-26 | done |
| [P9-28](phase-9/P9-28-overview-ruler-and-diff-overview.md) | The generic `OverviewRuler` and the diff overview | P9-27, P9-23 | done |

### Step F — Accessible diff viewer

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P9-29](phase-9/P9-29-accessible-diff-viewer-model.md) | Accessible diff viewer: the model | P9-26 | done |
| [P9-30](phase-9/P9-30-accessible-diff-viewer-ui.md) | Accessible diff viewer: the UI | P9-29, P9-26 | done |

### Step G — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P9-31](phase-9/P9-31-gallery-diff-demo-and-manual-plan.md) | Gallery: the diff editor demo and the manual test plan | P9-30, P9-28 | done |
| [P9-32](phase-9/P9-32-diff-self-check-and-benchmarks.md) | Native AOT self-check of the diff stack and diff benchmarks | P9-31 | done |
| [P9-33](phase-9/P9-33-close-phase-9.md) | Close phase 9 | all phase 9 cards | done |

Take phase 9 cards in table order (each card's "Depends on" is satisfied by the cards above it). P9-10, P9-23 and P9-24 are flagged **Recommended model: Opus** (reason in the card). P9-19, P9-22 … P9-26, P9-28, P9-30 and P9-31 have manual checks for the maintainer (`docs/testing/phase-9-manual-tests.md`, written by P9-31); whether the Native AOT Gallery works with the diff editor is verified by P9-31 and P9-32, and Narrator by a human only.

**Deferred on purpose:** moved blocks drawing (the computation is ported), the gutter menu and revert buttons, `diffCodeLens`, the embedded diff editor, `compactMode`, true inline view, the legacy diff computer and algorithm selection, `serializeState`, accessibility sounds, mapping the old diff through edits while a new one is computed, `setModel`, breadcrumbs (**not delivered in phase 9**; details in [phase-9/README.md](phase-9/README.md) and [ADR 0012](../adr/0012-diff-editor.md)).

## Phase 10 — Editor completeness

Monaco reference commit: `0c9c0a4295a9222e12a605537738719455ae3056`. Roadmap (backlog by theme): [phase-10-editor-completeness.md](../roadmap/phase-10-editor-completeness.md). Design: [ADR 0013](../adr/0013-text-options-and-snippets.md) (written by P10-02).
**Read [phase-10/README.md](phase-10/README.md) before any phase 10 card** (conventions, Monaco → LoomEditor concepts, what is out of scope).

### Step A — Audit and decisions

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P10-01](phase-10/P10-01-monaco-audit.md) | Audit: what of Monaco's editor is not in LoomEditor yet | P9-33 | done |
| [P10-02](phase-10/P10-02-adr-fonts-whitespace-snippets.md) | ADR 0013: font options, ligatures, zoom, whitespace and snippets | P10-01 | done |

### Step B — Fonts and zoom

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P10-03](phase-10/P10-03-font-options.md) | Font options: weight, letter spacing, ligatures, variations (the model) | P10-02 | done |
| [P10-04](phase-10/P10-04-directwrite-font-options.md) | Font options in DirectWrite: weight, letter spacing, ligatures | P10-03 | done |
| [P10-05](phase-10/P10-05-font-control-properties.md) | Control properties for the font options, and Gallery switches | P10-04 | done |
| [P10-06](phase-10/P10-06-editor-zoom.md) | Editor zoom: the state, the three commands and the `ZoomLevel` property | P10-05 | done |
| [P10-07](phase-10/P10-07-mouse-wheel-zoom.md) | `Ctrl` + mouse wheel zoom | P10-06 | done |

### Step C — Whitespace and control characters

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P10-08](phase-10/P10-08-render-whitespace-computer.md) | `renderWhitespace`: which characters of a line are rendered as whitespace (the logic) | P9-33 | done |
| [P10-09](phase-10/P10-09-render-whitespace-drawing.md) | `renderWhitespace`: drawing the dots and arrows, and the control property | P10-08 | done |
| [P10-10](phase-10/P10-10-render-control-characters.md) | `renderControlCharacters`: visible control characters | P10-09 | done |

### Step D — Editing

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P10-11](phase-10/P10-11-overtype-mode.md) | Overtype mode: the input mode and the overtype edits | P9-33 | done |
| [P10-12](phase-10/P10-12-cursor-styles-and-insert-key.md) | Cursor styles, `OvertypeCursorStyle` and the `Insert` key | P10-11 | done |
| [P10-13](phase-10/P10-13-selection-anchor-and-expand-line.md) | Selection anchor and `Expand Line Selection` | P9-33 | done |
| [P10-14](phase-10/P10-14-cursor-undo.md) | Cursor undo and redo | P10-13 | done |
| [P10-15](phase-10/P10-15-move-selected-text.md) | Move selected text one character left or right | P9-33 | done |
| [P10-16](phase-10/P10-16-smart-select.md) | Smart select: expand and shrink the selection | P9-33 | done |
| [P10-17](phase-10/P10-17-mouse-drag-state.md) | Drag and drop of the selection (1/2): the drag state of the mouse operation | P9-33 | done |
| [P10-18](phase-10/P10-18-drag-and-drop-feature.md) | Drag and drop of the selection (2/2): the command, the drop marker and the mouse cursor | P10-17 | done |

### Step E — Snippets

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P10-19](phase-10/P10-19-snippet-scanner-and-markers.md) | Snippet syntax (1/3): the scanner and the text, placeholder and choice markers | P10-02 | done |
| [P10-20](phase-10/P10-20-snippet-transforms-variables-snippet.md) | Snippet syntax (2/3): transforms, format strings, variables and `TextmateSnippet` | P10-19 | done |
| [P10-21](phase-10/P10-21-snippet-parser.md) | Snippet syntax (3/3): `SnippetParser` | P10-20 | done |
| [P10-22](phase-10/P10-22-snippet-variables.md) | Snippet variables: the resolvers | P10-21 | done |
| [P10-23](phase-10/P10-23-one-snippet.md) | `OneSnippet`: tab stops, placeholders and moving between them | P10-22 | done |
| [P10-24](phase-10/P10-24-snippet-session.md) | `SnippetSession`: insert a snippet at every cursor, merge, next and previous | P10-23 | done |
| [P10-25](phase-10/P10-25-snippet-controller.md) | `SnippetController`: the feature, the keys and `InsertSnippet` | P10-24 | done |
| [P10-26](phase-10/P10-26-completion-snippets.md) | Completion items that are snippets | P10-25 | done |

### Step F — Diagnostics navigation

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P10-27](phase-10/P10-27-marker-list.md) | `MarkerList`: the sorted problems of a model and the "next one" arithmetic | P9-33 | done |
| [P10-28](phase-10/P10-28-go-to-problem.md) | Go to next / previous problem (`F8`) | P10-27 | done |

### Step G — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P10-29](phase-10/P10-29-gallery-demo-and-manual-plan.md) | Gallery: the phase 10 samples and the manual test plan | P10-28 | done |
| [P10-30](phase-10/P10-30-native-aot-self-check.md) | Native AOT self-check of the phase 10 logic | P10-29 | done |
| [P10-31](phase-10/P10-31-close-phase-10.md) | Close phase 10 | all phase 10 cards | done |

Take phase 10 cards in table order (each card's "Depends on" is satisfied by the cards above it). P10-17, P10-18 and P10-24 are the riskiest: a card that does not fit in one session stops with a proposed split. Cards with manual checks add them to `docs/testing/phase-10-manual-tests.md` (P10-29 completes it).

**Deferred on purpose ("Later", reasons in [phase-10/README.md](phase-10/README.md)):** unicode highlight, font tokens of themes and custom line heights, semantic highlighting, `fontVariationSettings` beyond `wght`, rich copy, touch, Intl word segmentation, IME on Windows 10, linked editing, format, signature help, references, rename, code actions, document links, workspace symbols, provider based folding, sticky scroll outline, minimap section headers, find history, color picker, CodeLens, inlay hints, inline completions, context menu / command palette / quick access, the marker navigation widget, the diff editor deferred items, accessibility help dialog, `ariaLabel` / `ariaRole`, reduced motion, UIA text edit pattern, zoom shared by the two diff editors, directional formatting characters in `renderControlCharacters`.

## Phase 11 — Inline comment threads

Design: [ADR 0014](../adr/0014-inline-comments.md); conventions: [phase-11/README.md](phase-11/README.md). **Not a port** (Monaco has no comment widget): cards say "Sources: none" and have no Monaco header.

### Step A — Logic, no UI

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P11-01](phase-11/P11-01-comment-data-model.md) | Comment data model: `Comment`, `CommentAuthor`, `CommentThread` | P10-31 | done |
| [P11-02](phase-11/P11-02-markdown-extended-syntax.md) | Markdown: blockquote, strikethrough and underline (opt-in) | P10-31 | done |
| [P11-03](phase-11/P11-03-markdown-toolbar-inline.md) | Markdown toolbar (1/2): inline formats as pure text edits | P11-02 | done |
| [P11-04](phase-11/P11-04-markdown-toolbar-blocks.md) | Markdown toolbar (2/2): heading, lists, quote, code block and suggestion | P11-03 | done |
| [P11-05](phase-11/P11-05-strings-relative-time-initials.md) | `CommentStrings`, relative time and avatar initials | P11-03 | done |
| [P11-06](phase-11/P11-06-suggestion-extract-and-edit.md) | Suggestion blocks (1/2): extraction and the model edit | P11-02 | done |
| [P11-07](phase-11/P11-07-suggestion-diff.md) | Suggestion blocks (2/2): the red / green diff rows | P11-05, P11-06 | done |
| [P11-08](phase-11/P11-08-thread-view-model.md) | Thread view model: draft, preview, submit, cancel and Escape | P11-01, P11-05 | done |

### Step B — Threads in the editor

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P11-09](phase-11/P11-09-thread-manager-and-widget-zone.md) | Threads in an editor: the `Widget` zone kind and `CommentThreadManager` | P11-01 | done |
| [P11-10](phase-11/P11-10-comment-threads-controller.md) | `CommentThreadsController` (View), the apply-suggestion path and the `CommentThreads` feature | P11-06, P11-08, P11-09 | done |
| [P11-11](phase-11/P11-11-control-public-api.md) | The public API of `LoomEditor`: `AddCommentThread`, `RemoveCommentThread` and the four events | P11-10 | done |

### Step C — The widget

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P11-12](phase-11/P11-12-zone-widget-layer.md) | The zone-widget layer: a WinUI element over each `Widget` zone, positioned in the draw pass (**Opus**) | P11-11 | done |
| [P11-13](phase-11/P11-13-widget-height-follows-content.md) | The zone height follows the widget's content (**Opus**) | P11-12 | done |
| [P11-14](phase-11/P11-14-focus-and-key-routing.md) | Focus and keys between the editor and the widget's text box (**Opus**) | P11-13 | done |
| [P11-15](phase-11/P11-15-shared-markdown-element-builder.md) | Share the Markdown → XAML builder between the hover and the comment widget | P11-02, P11-14 | done |
| [P11-16](phase-11/P11-16-widget-read-mode.md) | The widget (1/4): header, comments and the collapsed "Resolved" state | P11-05, P11-15 | done |
| [P11-17](phase-11/P11-17-widget-composer.md) | The widget (2/4): the composer — text box, Preview, Cancel / Comment, Reply | P11-16 | done |
| [P11-18](phase-11/P11-18-composer-toolbar.md) | The widget (3/4): the Markdown toolbar of the composer and its shortcuts | P11-17 | done |
| [P11-19](phase-11/P11-19-widget-suggestion-block.md) | The widget (4/4): the "Suggested change" block and **Apply suggestion** | P11-18 | done |
| [P11-20](phase-11/P11-20-widget-accessibility.md) | Accessibility of the comment widget: UIA names, announcements, high contrast | P11-19 | done |
| [P11-21](phase-11/P11-21-gutter-add-comment-button.md) | The "+" button in the gutter and the add-comment request | P11-20 | done |

### Step D — Diff editor

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P11-22](phase-11/P11-22-diff-editor-comment-threads.md) | Comment threads in the diff editor (original and modified side) | P11-21 | done |

### Step E — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P11-23](phase-11/P11-23-gallery-demo-and-manual-plan.md) | Gallery: the comment threads demo and the manual test plan | P11-22 | done |
| [P11-24](phase-11/P11-24-native-aot-self-check.md) | Native AOT self-check of the phase 11 logic | P11-23 | done |
| [P11-25](phase-11/P11-25-close-phase-11.md) | Close phase 11 | all phase 11 cards | done |

Take phase 11 cards in table order (each card's "Depends on" is satisfied by the cards above it). P11-12, P11-13 and P11-14 are flagged **Recommended model: Opus**; the others are written for Sonnet. A card that does not fit in one session stops with a proposed split. The WinUI cards (P11-12 … P11-22) are verified by the build, the tests and a written manual section; agents do not run the Gallery UI.

**Deferred on purpose (reasons in [phase-11/README.md](phase-11/README.md)):** multi-line thread ranges, reactions, comment edit / delete UI, mentions, attachments, avatar images, localization, original-side threads in the inline diff view, "commit suggestion", a comments panel, persistence, a WinUI test project.

## Phase 12 — Multi-line ranges for inline comment threads

Roadmap: [phase-12-comment-ranges.md](../roadmap/phase-12-comment-ranges.md); conventions and design decisions: [phase-12/README.md](phase-12/README.md) (read phase-11/README.md first).
**Not a port** (as phase 11): cards say "Sources: none" and have no Monaco header.

### Step A — Logic, no UI

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P12-01](phase-12/P12-01-comment-thread-line-range.md) | `CommentThread` covers a range of lines: `StartLineNumber` | P11-25 | done |
| [P12-02](phase-12/P12-02-suggestion-edit-range.md) | Suggestion edit for a range of lines, the stale check and the range text | P12-01 | done |
| [P12-03](phase-12/P12-03-suggestion-diff-range.md) | Suggestion diff rows for N old lines | P12-02 | done |
| [P12-04](phase-12/P12-04-strings-and-multiline-seed.md) | Strings for ranges and a multi-line "Suggest a change" seed | P12-03 | done |

### Step B — Ranges in the editor

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P12-05](phase-12/P12-05-manager-range-anchor.md) | The manager anchors a thread to a range of lines | P12-04 | done |
| [P12-06](phase-12/P12-06-range-highlight.md) | Highlight the lines of a multi-line thread | P12-05 | done |
| [P12-07](phase-12/P12-07-controller-ranges.md) | Controller: add, apply a suggestion and request a thread on a range | P12-06 | done |
| [P12-08](phase-12/P12-08-add-command-selection.md) | The add command uses a multi-line selection | P12-07 | done |

### Step C — Public API and WinUI

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P12-09](phase-12/P12-09-editor-public-api.md) | Public API of `LoomEditor`: a thread on a range of lines | P12-08 | done |
| [P12-10](phase-12/P12-10-diff-editor-public-api.md) | Public API of `LoomDiffEditor`: a thread on a range of lines of one side | P12-09 | done |
| [P12-11](phase-12/P12-11-widget-range.md) | The widget for a range: title, accessibility and an N-line suggestion block | P12-10 | done |
| [P12-12](phase-12/P12-12-gutter-button-selection.md) | The gutter "+" button requests the selected lines | P12-11 | done |

### Step D — Prove it

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P12-13](phase-12/P12-13-gallery-demo-and-manual-plan.md) | Gallery: the range demo and the complete manual test plan | P12-12 | done |
| [P12-14](phase-12/P12-14-native-aot-self-check.md) | Native AOT self-check of the phase 12 logic | P12-13 | done |

### Step E — Exact text ranges (columns), added 2026-10-29

Design: [phase-12/README.md](phase-12/README.md) section 3 (a thread on the exact selected text; suggestions still replace the whole lines that contain it).

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P12-16](phase-12/P12-16-comment-thread-columns.md) | `CommentThread` knows its columns: `StartColumn`, `EndColumn`, `IsWholeLines` | P12-14 | done |
| [P12-17](phase-12/P12-17-comment-range-and-strings.md) | `CommentRange`: a selection becomes whole lines or exact text; strings for the selected text | P12-16 | done |
| [P12-18](phase-12/P12-18-manager-text-anchor.md) | The manager anchors a thread to the exact text and keeps its columns | P12-17 | done |
| [P12-19](phase-12/P12-19-text-highlight.md) | Highlight the exact text of a thread | P12-18 | done |
| [P12-20](phase-12/P12-20-controller-and-command-text-ranges.md) | Controller and add command: threads and requests on the exact selected text | P12-19 | done |
| [P12-21](phase-12/P12-21-public-api-text-ranges.md) | Public API: threads on the exact text in `LoomEditor` and `LoomDiffEditor` | P12-20 | done |
| [P12-22](phase-12/P12-22-widget-and-gutter-text-ranges.md) | The box tells screen readers the commented text; the gutter "+" requests the selected text | P12-21 | done |
| [P12-23](phase-12/P12-23-gallery-selfcheck-and-manual-plan-text-ranges.md) | Gallery, Native AOT self-check and manual plan for exact text ranges | P12-22 | done |

### Close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P12-15](phase-12/P12-15-close-phase-12.md) | Close phase 12 (reopened for step E: the docs must cover column ranges) | P12-23 | done |

Take phase 12 cards in table order (each card's "Depends on" is satisfied by the cards above it); P12-15 comes last, after P12-23. Every card is written for **Sonnet**;
none is flagged Opus (the delicate tracking code exists since P11-09 and is extended — step E reuses the same decoration for columns — with every edge case listed as a
test). A card that does not fit in one session stops with a proposed split. The WinUI cards (P12-09 … P12-12, P12-21, P12-22) are verified by the build, the tests and a
written manual section; agents do not run the Gallery UI.

**Deferred on purpose (phase 12):** a suggestion that replaces only the selected text (it replaces the lines that contain it), columns in the thread title, dragging from or shift-clicking the gutter "+" button to select lines, a gutter bar or overview-ruler mark for a range, overlap rules between ranges, and everything phase 11 deferred (reactions, edit / delete UI, mentions, attachments, avatar images, localization, original-side threads in the inline diff view, "commit suggestion", a comments panel, persistence).

## Phase 13 — Public API for host applications

Roadmap: [phase-13-host-api.md](../roadmap/phase-13-host-api.md); conventions and design decisions (the public member chosen for each need of VIGO, and what stays
internal): [phase-13/README.md](phase-13/README.md). **Not a port** (as phases 11 and 12): cards say "Sources: none" and have no Monaco header. Planned 2026-10-30; the
Avalonia phase of the roadmap is renumbered 14.

### Step A — Logic, no UI

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P13-01](phase-13/P13-01-language-by-file-name.md) | `BuiltInLanguages.GetLanguageIdByFileName` | P12-15 | todo |
| [P13-02](phase-13/P13-02-host-decoration-types.md) | Host decorations: `EditorDecoration`, `EditorDecorationOptions`, `DecorationsCollection` | P13-01 | todo |
| [P13-03](phase-13/P13-03-custom-decoration-styles.md) | Host decoration styles: `CustomDecorationStyle`, `CustomDecorationStyles` | P13-02 | todo |

### Step B — Public API of `LoomEditor`

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P13-04](phase-13/P13-04-editor-decorations-api.md) | `LoomEditor.CreateDecorationsCollection` for the host | P13-03 | todo |
| [P13-05](phase-13/P13-05-scroll-and-line-metrics.md) | Scroll and layout sync: `ScrollChanged`, `LayoutChanged`, `LineHeight`, `GetTopForLineNumber` | P13-04 | todo |
| [P13-06](phase-13/P13-06-line-numbers-and-reset-text.md) | `LineNumbersEnabled` and `ResetText` | P13-05 | todo |
| [P13-07](phase-13/P13-07-hidden-areas-and-gutter-clicks.md) | `SetHiddenAreas` and the `GutterClicked` event | P13-06 | todo |
| [P13-08](phase-13/P13-08-host-view-zones.md) | Host view zones: `AddViewZone` / `RemoveViewZone` with WinUI content | P13-07 | todo |

### Step C — Prove it and close the phase

| Card | Title | Depends on | Status |
|---|---|---|---|
| [P13-09](phase-13/P13-09-gallery-demo-and-manual-plan.md) | Gallery: the "Host API" window and the complete manual test plan | P13-08 | todo |
| [P13-10](phase-13/P13-10-native-aot-and-package-check.md) | Native AOT self-check and package test of the host API | P13-09 | todo |
| [P13-11](phase-13/P13-11-close-phase-13.md) | Close phase 13: the host API how-to, CHANGELOG and docs | P13-10 | todo |

Take phase 13 cards in table order (each card's "Depends on" is satisfied by the cards above it). Every card is written for **Sonnet**; none is flagged Opus (the only
delicate WinUI piece, content over a view zone, reuses the phase 11 zone-widget layer and its focus rules). A card that does not fit in one session stops with a proposed
split. The WinUI cards (P13-04 … P13-08) are verified by the build, the tests and a written manual section; agents do not run the Gallery UI.

**Deferred on purpose (phase 13):** public Monaco internals (decoration options, view zones, language registry, style table), a view-zone pointer event, host zones and hidden
areas on the sides of a `LoomDiffEditor`, host-registered languages, per-editor styles, hover / injected text / glyph icons on host decorations, a "visible lines" API,
stripping internal members from the packages' XML docs.

## How to run a card with a coding agent (Claude Code)

Start a fresh session for each card, on the phase branch, with a clean working tree, and type:

```
/next-task          # picks the first `done` card whose dependencies are `done`
/next-task P1-07    # or a specific card
```

The skill (`.claude/skills/next-task/SKILL.md`) implements the card, runs the build and the tests, updates the card
and this index, and makes **one commit without any co-author trailer**. It never pushes. Review the commit
(`git show`), then push when you are happy with it.

