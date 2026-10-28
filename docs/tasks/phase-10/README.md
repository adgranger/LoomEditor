# Phase 10 — read this before any card

Phase 10 makes the editor more complete: the **text rendering options** that Monaco has and LoomEditor does not (ligatures, font weight, letter spacing, editor zoom, `renderWhitespace`, `renderControlCharacters`),
the **editing features** that are still missing (overtype mode, selection anchor, cursor undo, smart select, drag and drop of selected text, **snippets**) and **diagnostics navigation** (`F8`).
The backlog is [phase-10-editor-completeness.md](../../roadmap/phase-10-editor-completeness.md); this phase takes the part of it that is realistic as small cards. What is not taken is listed under "Later" at the end of this page.
The conventions of [phase-3/README.md](../phase-3/README.md) (sections 1, 3–6), [phase-5/README.md](../phase-5/README.md) (sections 1–5), [phase-6/README.md](../phase-6/README.md) (sections 1, 2),
[phase-7/README.md](../phase-7/README.md) (sections 5–7), [phase-8/README.md](../phase-8/README.md) (sections 1, 2, 4) and [phase-9/README.md](../phase-9/README.md) (sections 1, 3, 6) still apply: a card never repeats them.

## Order of the cards

Take the cards in table order of [../README.md](../README.md) (each card's "Depends on" is satisfied by the cards above it). Steps:

- **A — Audit and decisions** (P10-01, P10-02): the Monaco-vs-LoomEditor audit (no code) and ADR 0013 (no code).
- **B — Fonts and zoom** (P10-03 … P10-07): the font options (weight, letter spacing, ligatures, variations), their DirectWrite implementation, the control properties, editor zoom, `Ctrl` + wheel.
- **C — Whitespace and control characters** (P10-08 … P10-10): `renderWhitespace` (logic, drawing) and `renderControlCharacters`.
- **D — Editing** (P10-11 … P10-18): overtype mode, cursor styles and the `Insert` key, selection anchor and expand line selection, cursor undo, move selected text, smart select, drag and drop (two cards).
- **E — Snippets** (P10-19 … P10-26): scanner and markers, transforms and `TextmateSnippet`, parser, variables, `OneSnippet` (tab stops), `SnippetSession`, controller and keys, completion items as snippets.
- **F — Diagnostics navigation** (P10-27, P10-28): the marker list and `F8`.
- **G — Prove it and close** (P10-29 … P10-31): Gallery samples and manual plan, Native AOT self-check, close the phase.

Steps B and C start with pure logic that is unit tested without UI; the cards that touch DirectWrite or the control have **manual checks** for the maintainer (`docs/testing/phase-10-manual-tests.md`, written in full by P10-29: a card that needs a manual check adds its lines to that file as it goes, creating the file in the first card that needs it).
P10-17 and P10-18 (drag and drop) and P10-24 (`SnippetSession`) are the riskiest cards: if one does not fit in one session, stop and report with a proposed split, do not cut corners.

## Out of scope for phase 10 ("Later")

Everything below stays in the backlog file, unchecked. A reason is given when it is not just "more work".

| Item | Why it is not in phase 10 |
|---|---|
| Unicode highlight (`unicodeHighlighter`, ambiguous / invisible characters) | needs Monaco's generated confusables table (large data file) and a decision on its licence and size |
| Font tokens of themes (`fontFamily`, `fontSize`, `lineHeight` per token), custom line heights from decorations | changes the layout of a line (non-uniform runs); needs its own ADR |
| Semantic highlighting, tree-sitter | needs LSP semantic tokens and theme rules; separate theme |
| `fontVariationSettings` beyond `wght` | Win2D has no API for variation axes |
| Rich (HTML) copy, touch gestures, Intl word segmentation, IME on Windows 10 (TSF) | each is a platform integration of its own |
| Linked editing, format document / selection / on type, signature help, references, rename, code actions, document links, workspace symbols | each needs a provider contract in Core, a widget and an LSP binding: one theme per feature (phase 12 proposal) |
| Syntax folding from a provider, sticky scroll outline model, minimap section headers, find history | need provider contracts or the outline model |
| Color decorators and picker, CodeLens, inlay hints, inline completions (ghost text) | each needs a provider contract and new widgets |
| Context menu, command palette, quick access | host UI: needs an ADR to decide whether the control owns them |
| Marker navigation **widget** (the peek-like box of `F8`) | needs zone widgets (`peekView`); phase 10 selects the marker and announces its message |
| Diff editor deferred items (moved blocks, revert buttons, embedded diff editor…) | see [ADR 0012](../../adr/0012-diff-editor.md) |
| Accessibility help dialog, `ariaLabel` / `ariaRole`, reduced motion, UIA text edit pattern | see the roadmap, section F |
| Zoom shared between the two editors of `LoomDiffEditor` | a diff editor option of its own |
| Directional formatting characters rendered as `[U+202A]` by `renderControlCharacters` | the text would no longer map 1:1 to the layout (P10-10) |

## 1. Upstream sources

Same rule as [phase-9/README.md](../phase-9/README.md) §1 (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`; line numbers are hints, the function or class **name** is authoritative; a card with no Monaco counterpart says "Sources: none").

## 2. Monaco concepts → LoomEditor (new in phase 10)

| Monaco | LoomEditor |
|---|---|
| `EditorZoom` (process-wide singleton) | `EditorZoomState`, **one per editor** (P10-06) |
| `InputMode` (process-wide singleton, `'insert' \| 'overtype'`) | `EditorContext.InputMode`, one per editor (P10-11) |
| `fontLigatures: boolean \| string`, `fontVariations`, `fontWeight`, `letterSpacing` | `EditorFontOptions` (View) and the control properties `FontLigatures`, `FontFeatureSettings`, `FontVariationSettings`, `LetterSpacing`, and the inherited `FontWeight` (P10-03, P10-05) |
| `renderWhitespace`, `renderControlCharacters` | `RenderWhitespace` enum and `RenderControlCharacters` control properties (P10-08 … P10-10) |
| `TextEditorCursorStyle`, `cursorStyle`, `overtypeCursorStyle` | `TextEditorCursorStyle`, `CursorStyle`, `OvertypeCursorStyle` (P10-12) |
| `EditorAction` / `registerEditorAction` | an `EditorCommand` registered by a feature (see `SelectAllFeature`) |
| `RawContextKey` (`inSnippetMode`, `selectionAnchorSet`…) | a field of `KeybindingContext` (View/Commands) |
| `SnippetController2`, `SnippetSession`, `OneSnippet` | same names in `LoomEditor.View/Features/Snippet/`; the parser and markers in `LoomEditor.Core/Snippets/` |
| `MarkerList`, `MarkerNavigationService` | `MarkerList` over `MarkerService` (P10-27) |

## 3. Conventions

1. **Namespaces.** Core: `LoomEditor.Core.Snippets`. View: `LoomEditor.View.Features.<Name>` (one folder per feature, rule 5 of the architecture: `XxxFeature.cs`, `XxxCommands.cs`, `README.md`). A feature that adds a context key adds one field to `KeybindingContext` and fills it where the control fills the others (grep a neighbour such as `SuggestWidgetVisible`).
2. **A new command** has its id in the feature's `XxxCommands.cs` (`"editor.action.…"`, Monaco's id when there is one) and a row in `DefaultKeybindings.cs` only when the card gives a key. Monaco's `primary: 0` means "no default key": do not invent one.
3. **A new control property** is a `DependencyProperty` in a `LoomEditor.<Topic>.cs` partial file, with the validation and the "previous value is kept" behavior of `TabSize` / `WordWrapColumn` (`LoomEditor.Font.cs`, `LoomEditor.WordWrap.cs`), and a line in `PublicAPI.Unshipped.txt` (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`).
4. **Windows only for keys:** where Monaco has a macOS binding, port only the Windows / Linux one.
5. **Manual checks.** A card that changes pixels or input says what the maintainer checks. The agent adds those lines to `docs/testing/phase-10-manual-tests.md` (one `##` section per card) and does **not** run the Gallery unless the card says so.
6. **Tests.** Ported tests keep Monaco's name in a `// Monaco:` comment. Rendering tests use the recording surface of `tests/LoomEditor.Rendering.Tests/TestHelpers` (the same one `TextViewRendererDecorationsTests` uses). View tests use the helpers of `tests/LoomEditor.View.Tests/TestHelpers`.
