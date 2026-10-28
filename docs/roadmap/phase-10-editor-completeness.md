# Phase 10 — Editor completeness: what Monaco has and LoomEditor does not yet

Not scheduled in detail: this file is a **backlog by theme**, built from the "Deferred on purpose" lists of phases 1 to 9 (`docs/tasks/README.md`), the "Not delivered" lists of ADR 0011 and ADR 0012, and the out-of-scope lists of the cards.
Each theme becomes task cards (`docs/tasks/phase-10/`) when it is picked; the order below is a proposal, the maintainer decides. Phase 11 (Avalonia) stays after it.

Task cards: [docs/tasks/phase-10/](../tasks/phase-10/README.md) (31 cards: themes A, B and parts of C and D; the rest is listed under "Later" in that README).

## A. Text rendering and fonts (first: this is what the maintainer asked for)

- [ ] **Font ligatures** (`fontLigatures`, `fontFeatureSettings`): a ligature fuses several characters into one glyph, so the character width table of `FontMeasurements` (P2-06) is no longer enough: columns, caret position, selection and hit testing must go through the shaped text. Needs an ADR (shaping through DirectWrite typography features; Monaco itself splits the line into runs and disables ligatures while the caret is inside one)
- [ ] Font variations (`fontVariationSettings`), font weight and letter spacing options
- [ ] Editor zoom (`Ctrl` + wheel, `EditorZoom`)
- [ ] `renderWhitespace` (none, boundary, selection, trailing, all) and the word wrap indicator
- [ ] Control characters and unicode highlighting (`renderControlCharacters`, `unicodeHighlight`)
- [ ] Font tokens of themes (`fontFamily`, `fontSize`, `lineHeight` per token) and custom line heights from decorations
- [ ] Semantic highlighting (needs LSP semantic tokens, theme rules) and, later, tree-sitter

## B. Editing

- [ ] Overtype mode (`Insert` key)
- [ ] Selection drag and drop (`dragAndDrop`), rich (HTML) copy
- [ ] Touch gestures; Intl word segmentation; IME on Windows 10 (TSF `ITextStoreACP2`)
- [ ] Snippets (`SnippetController2`): the text syntax, tab stops, choices, variables. Also unlocks `insertText` rules of completion items, the multi-cursor insertion and the re-indentation that P8-19 left out
- [ ] Linked editing (rename a tag pair together), `editor.action.*` commands that need a provider: format document / selection, format on type / paste

## C. View features

- [ ] Syntax folding (indentation-based exists; folding ranges from a provider), folding controls polish
- [ ] Sticky scroll outline model, click-link gesture and keyboard navigation
- [ ] Minimap section headers; find history; the global find clipboard
- [ ] Word highlight from a provider (document highlights), bracket jump / select to bracket if missing
- [ ] Color decorators and the color picker; `CodeLens`; inlay hints; inline completions (ghost text), which also gives the `Inline` quick suggestion value its meaning
- [ ] Context menu, command palette and "go to line / symbol" quick access (host UI: decide in an ADR if they belong to the control)

## D. Language features and LSP (phase 8 deferred)

- [ ] Signature help; references; rename; code actions and quick fixes; formatting; document links; workspace symbols
- [ ] Semantic tokens, inlay hints, document highlights and folding ranges from the server
- [ ] Peek definition and Ctrl+hover link underlines; the diagnostics list and `F8` navigation
- [ ] Pull diagnostics; several documents and workspace folders; server-initiated edits; hover verbosity
- [ ] Completion details side pane and `suggestMemory` (recency of the selection); word based completions

## E. Diff editor (phase 9 deferred)

- [ ] Draw moved blocks (the computation is ported), the gutter menu and revert buttons, `diffCodeLens`
- [ ] Embedded diff editor, `compactMode`, true inline view (one editor with injected deleted lines), `serializeState`
- [ ] Keep the old diff mapped through edits while a new one is computed; `setModel`; accessibility sounds; breadcrumbs

## F. Accessibility (phase 7 deferred)

- [ ] Accessibility help dialog; `ariaLabel` / `ariaRole` options; reduced motion
- [ ] UIA text edit pattern and annotations; per-token text attributes
- [ ] Localization of announcements and messages; a UIA client based automated test
- [ ] Screen reader support of the hover popup and the completion list

## G. To verify before writing cards

These are Monaco features that no document of the repository mentions as deferred. The first step of this phase is a pass over `src/vs/editor/contrib` and `editorOptions.ts` at the pinned commit to turn this list into either "ported", "deferred" (with a reason) or "out of scope":

- [ ] Compare `src/vs/editor/contrib/*` folder by folder with `docs/PORTING.md` and list the folders that have no row
- [ ] Compare the options of `editorOptions.ts` with the options the control exposes, and list the ones that are missing
- [ ] Compare the commands of `coreCommands.ts` and the `editor.action.*` contributions with the registered commands
