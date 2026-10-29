# Phase 10 — Editor completeness: what Monaco has and LoomEditor does not yet

**Done when:** the editor has the text rendering options (ligatures, font weight, letter spacing, zoom, whitespace, control characters), the editing features (overtype, selection anchor, cursor undo, smart select, drag and drop), snippets and `F8` problem navigation, and the Gallery shows them. — **Built and covered by automated tests; the manual plan has not been run, and the Native AOT self-check is NOT verified** (see "Status of the done-when line").

Checklist ticked 2026-10-29 = built and covered by automated tests (the last card, P10-31). A ticked line with a note says what part was taken.

Not scheduled in detail: this file is a **backlog by theme**, built from the "Deferred on purpose" lists of phases 1 to 9 (`docs/tasks/README.md`), the "Not delivered" lists of ADR 0011 and ADR 0012, and the out-of-scope lists of the cards.
Each theme becomes task cards (`docs/tasks/phase-10/`) when it is picked; the order below is a proposal, the maintainer decides. Phase 11 (inline comments) and phase 12 (multi-line comment ranges) and phase 13 (Avalonia) come after it.

Task cards: [docs/tasks/phase-10/](../tasks/phase-10/README.md) (31 cards: themes A, B and parts of C and D; the rest is listed under "Later" in that README).

## A. Text rendering and fonts (first: this is what the maintainer asked for)

- [x] (P10-03, P10-04, P10-05) **Font ligatures** (`fontLigatures`, `fontFeatureSettings`): a ligature fuses several characters into one glyph, so the character width table of `FontMeasurements` (P2-06) is no longer enough: columns, caret position, selection and hit testing must go through the shaped text. Needs an ADR (shaping through DirectWrite typography features; Monaco sets `font-feature-settings` and reads positions from the DOM (`FontMeasurements`: `isMonospace` is false when ligatures are on, `fontMeasurements.ts` line 219); there is no caret special case)
- [x] (P10-03 … P10-05) Font weight and letter spacing options; font variations: parsed and stored, only `wght` is applied (Win2D has no variation axes API)
- [x] (P10-06, P10-07) Editor zoom (`Ctrl` + wheel, `EditorZoom`)
- [x] (P10-08, P10-09) `renderWhitespace` (none, boundary, selection, trailing, all); the word wrap indicator was not taken
- [x] (P10-10) Control characters (`renderControlCharacters`)
- [ ] Unicode highlighting (`unicodeHighlight`)
- [ ] Font tokens of themes (`fontFamily`, `fontSize`, `lineHeight` per token) and custom line heights from decorations
- [ ] Semantic highlighting (needs LSP semantic tokens, theme rules) and, later, tree-sitter

## B. Editing

- [x] (P10-11, P10-12) Overtype mode (`Insert` key) and the six cursor styles
- [x] (P10-17, P10-18) Selection drag and drop (`dragAndDrop`)
- [ ] Rich (HTML) copy
- [ ] Touch gestures; Intl word segmentation; IME on Windows 10 (TSF `ITextStoreACP2`)
- [x] (P10-19 … P10-26) Snippets (`SnippetController2`): the text syntax, tab stops, choices, variables. Also unlocks `insertText` rules of completion items, the multi-cursor insertion and the re-indentation that P8-19 left out (done in P10-26); the choice list and snippet text edits were not taken
- [ ] Linked editing (rename a tag pair together), `editor.action.*` commands that need a provider: format document / selection, format on type / paste

Also delivered, not in this backlog but in the audit (P10-01): the selection anchor and *Expand Line Selection* (P10-13), cursor undo and redo (P10-14), move selected text one character left or right (P10-15), smart select (P10-16).

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
- [x] (P10-27, P10-28) `F8` navigation: the caret moves to the next / previous problem and its message is announced; no marker widget
- [ ] Peek definition and Ctrl+hover link underlines; the diagnostics list
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

## Found by the audit

The audit is [phase-10-audit.md](phase-10-audit.md) (59 `contrib` folders, 178 options, 163 commands). Contribs with no phase 10 card, to schedule later: `inPlaceReplace`, `insertFinalNewLine`, `longLinesHelper`, `middleScroll`, `readOnlyMessage`, `unusualLineTerminators`, `dropOrPasteInto`, `sectionHeaders`, `symbolIcons`. Options still missing and not covered by a card are marked "later" in table 2 (for example `rulers`, `scrollbar`, `selectionClipboard`, `cursorSmoothCaretAnimation`, `renderFinalNewline`).

## G. To verify before writing cards (done by P10-01)

These are Monaco features that no document of the repository mentions as deferred. The first step of this phase is a pass over `src/vs/editor/contrib` and `editorOptions.ts` at the pinned commit to turn this list into either "ported", "deferred" (with a reason) or "out of scope":

- [x] Compare `src/vs/editor/contrib/*` folder by folder with `docs/PORTING.md` and list the folders that have no row
- [x] Compare the options of `editorOptions.ts` with the options the control exposes, and list the ones that are missing
- [x] Compare the commands of `coreCommands.ts` and the `editor.action.*` contributions with the registered commands


## Not taken in phase 10

Copied from the "Later" table of [docs/tasks/phase-10/README.md](../tasks/phase-10/README.md). Besides these, the unticked lines above and the audit rows still `missing` (including `cursorHeight`, `renderFinalNewline` and `editor.action.transposeLetters`, which a card named but did not deliver) stay in the backlog.

| Item | Why it is not in phase 10 |
|---|---|
| Unicode highlight (`unicodeHighlighter`, ambiguous / invisible characters) | needs Monaco's generated confusables table (large data file) and a decision on its licence and size |
| Font tokens of themes (`fontFamily`, `fontSize`, `lineHeight` per token), custom line heights from decorations | changes the layout of a line (non-uniform runs); needs its own ADR |
| Semantic highlighting, tree-sitter | needs LSP semantic tokens and theme rules; separate theme |
| `fontVariationSettings` beyond `wght` | Win2D has no API for variation axes |
| Rich (HTML) copy, touch gestures, Intl word segmentation, IME on Windows 10 (TSF) | each is a platform integration of its own |
| Linked editing, format document / selection / on type, signature help, references, rename, code actions, document links, workspace symbols | each needs a provider contract in Core, a widget and an LSP binding: one theme per feature (a later phase, after phase 13) |
| Syntax folding from a provider, sticky scroll outline model, minimap section headers, find history | need provider contracts or the outline model |
| Color decorators and picker, CodeLens, inlay hints, inline completions (ghost text) | each needs a provider contract and new widgets |
| Context menu, command palette, quick access | host UI: needs an ADR to decide whether the control owns them |
| Marker navigation **widget** (the peek-like box of `F8`) | needs zone widgets (`peekView`) (zone widgets exist since phase 11, ADR 0014); phase 10 selects the marker and announces its message |
| Diff editor deferred items (moved blocks, revert buttons, embedded diff editor…) | see [ADR 0012](../adr/0012-diff-editor.md) |
| Accessibility help dialog, `ariaLabel` / `ariaRole`, reduced motion, UIA text edit pattern | see the roadmap, section F |
| Zoom shared between the two editors of `LoomDiffEditor` | a diff editor option of its own |
| Directional formatting characters rendered as `[U+202A]` by `renderControlCharacters` | the text would no longer map 1:1 to the layout (P10-10) |

## Status of the done-when line

- **Native AOT**: the Gallery publish (`dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror`) **passes** (P10-30, 2026-10-29), and the published exe runs both
  self-checks: `--text-selfcheck=<file>` ends with `10 of 10 checks passed` (exit 0) and `--diff-selfcheck=<file>` with `SUMMARY: 9 passed, 0 failed` (exit 0), 2026-10-29. The Gallery used to crash at start-up
  (`0xc000027b`, a `XamlParseException`) because the `Toggled` handler of the control characters switch ran during `InitializeComponent` before `Editor` existed (card P10-10); a null guard fixed it. See P10-30's
  Result section ([P10-30](../tasks/phase-10/P10-30-native-aot-self-check.md)).
- **The phase 10 logic**: the ten checks of the self-check (snippet parse, regex transform, insertion, whitespace, control characters, font features, zoom, overtype, smart select, marker list) run and pass
  in the normal test run (`TextSelfCheckTests`, JIT build only).
- **Not verified**: the manual plan ([testing/phase-10-manual-tests.md](../testing/phase-10-manual-tests.md), written by the cards and completed by P10-29; an agent runs none of it), Narrator / NVDA reading
  the new announcements (the `F8` message, the snippet and overtype ones), ligature rendering on fonts other than Cascadia Code, the DirectWrite drawing (ligatures, letter spacing, whitespace, control pictures,
  cursor styles, the drop marker) and the pointer features (drag and drop, `Ctrl` + wheel) with a human, the Gallery run by anyone.
- **Tests**: 7,103 tests in the solution on 2026-10-29 (7,101 passed, 2 skipped on purpose, 0 failed), against 6,527 at the end of phase 9; the 40 test files added in phase 10 hold 503 `[Fact]` / `[Theory]` methods
  (counted by attribute: a theory counts once). The build has 0 warnings.
- **Open point for the maintainer**: the start-up crash of the Native AOT Gallery above; until it is understood the AOT claim of phases 9 and 10 for the Gallery cannot be repeated.

## Not delivered (ADR 0013)

The "Later" table above, the choice list of snippets, snippet text edits (`apply`) and the overtyping capturer, the marker widget of `F8`, and the audit rows listed under "Not taken". See [ADR 0013](../adr/0013-text-options-and-snippets.md).
