# ADR 0013 — Text rendering options and snippets: fonts, ligatures, zoom, whitespace, input mode, snippets

- **Status:** accepted
- **Date:** 2026-10-28

## Context

Phase 10 ([roadmap](../roadmap/phase-10-editor-completeness.md), [audit](../roadmap/phase-10-audit.md)) closes the gaps between Monaco and LoomEditor that the audit
found and that fit in small cards. Three flows need a design decision before any code:

1. **Fonts.** Monaco measures text in the DOM (`fontMeasurements.ts`, `isMonospace` is false when ligatures are on, line 219), applies `fontLigatures` / `fontVariations` /
   `fontWeight` / `letterSpacing` as CSS (`fontInfo.ts` 30-75, `editorOptions.ts` 1918-1966) and multiplies the font size by the zoom factor (`fontInfo.ts` 48-50).
   LoomEditor measures a width table (`FontInfo`, P2-06) and draws with Win2D / DirectWrite. There is no DOM: ligatures, spacing and weight must go through DirectWrite.
2. **Whitespace and control characters.** Monaco rewrites the line into spans (`viewLineRenderer.ts` 474-551, 794-956). LoomEditor draws one text layout per line.
3. **Snippets.** `snippetParser.ts` and `snippetSession.ts` (28-60) are large, pure logic plus a session that tracks tab stops with decorations.

## Decisions

1. **Font options are one immutable record.** `EditorFontOptions` (View/Layout) holds `FontWeight` (Monaco's string: `normal`, `bold` or `1`-`1000`), `LetterSpacing` (double),
   `FontFeatureSettings` (CSS `font-feature-settings` syntax, `""` is off) and `FontVariationSettings`. The control keeps `FontFamily` and `FontSize` and reuses the inherited `FontWeight`;
   the new properties are `FontLigatures`, `FontFeatureSettings`, `FontVariationSettings`, `LetterSpacing` (P10-05). Any change goes through `RecreateTextMeasurer`, like a font change,
   so there is one path that rebuilds measurements and layouts.
2. **Ligatures use DirectWrite typography features** (`CanvasTypography` on the `CanvasTextLayout`), always set explicitly: DirectWrite turns `liga` and `calt` on by default, so "off"
   means `liga=0, calt=0` (Monaco's `EditorFontLigatures.OFF`) and "on" means `liga=1, calt=1`. A string value is parsed (`'ss01' 1, "calt" off`) into `(tag, value)` pairs. Setting
   features explicitly makes the result independent of the font's defaults.
3. **Positions come from the layout, never from the width table.** `ILineLayout.GetCharacterX` / `HitTest` already drive columns, caret, selection and hit testing. DirectWrite shares a
   ligature's advance equally between its characters; that is accepted, as Monaco has no caret special case either. `FontInfo` stays the input of word wrap, the minimap and the view model;
   with ligatures on `FontInfo.IsMonospace` is `false` and word wrap is an approximation, as it already is (`PORTING.md`, "Behavior differences").
4. **Letter spacing** is `CanvasTextLayout.SetCharacterSpacing(0, length, 0, letterSpacing, 0)` on every layout and is added to every width read by `CharacterWidthReader`, so
   `FontInfo` includes it. Monaco gets the same result by measuring in the DOM with the CSS property set.
5. **Font variations:** only the `wght` axis is applied (as the font weight); other axes are parsed, stored and ignored, because Win2D has no variation API. The XML doc of the property says so.
6. **Zoom is per editor.** `EditorZoomState` (View) holds the level, clamped to -5...20 (`editorZoom.ts`), and raises `Changed`. `BareFontInfo.Create` multiplies the font size and the line
   height by `1 + level * 0.1`. A change goes through `RecreateTextMeasurer`. No default key is bound (Monaco binds none); `Ctrl` + wheel is the option `MouseWheelZoom` (default `false`).
   Monaco's zoom is a process-wide singleton; a control library must not share state between two editors.
7. **Whitespace is an overlay**, not a character substitution. A pure computer (`RenderWhitespaceComputer`, a port of `_applyRenderWhitespace`) returns the whitespace ranges of a line;
   the renderer draws `·` (U+00B7, or U+2E31 when the font's `·` is wider than a space, `viewLineRenderer.ts` 148-157) centered in the cell of each space and `→` (U+2192) at the start of
   each tab, in `editorWhitespace.foreground`, over the text layout (positions from `GetCharacterX`). The text layout and every offset stay unchanged.
8. **Control characters are substituted 1:1** in the text given to the layout (`U+0000`-`U+001F` except tab becomes `U+2400 + code`, DEL becomes `U+2421`), so offsets are unchanged.
   Directional formatting characters are not rendered as `[U+202A]`: that would not be 1:1.
9. **The input mode (overtype) is per editor**: state in `EditorContext.InputMode`, not a singleton, for the same reason as the zoom. The `Insert` key toggles it. The caret style comes
   from `CursorStyle` / `OvertypeCursorStyle` (the six Monaco styles).
10. **Snippets.** The grammar, scanner and markers (`snippetParser.ts`) and the variables are pure logic in `LoomEditor.Core/Snippets/`, internal. The session (`OneSnippet`,
    `SnippetSession`) and the controller are in `LoomEditor.View/Features/Snippet/`. Tab stops and placeholders are tracked with model decorations
    (`TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges`, as Monaco). Variables that need a host (`CLIPBOARD`, `TM_FILENAME`, `TM_DIRECTORY`, `TM_FILEPATH`, `WORKSPACE_*`) go through one
    interface, `ISnippetVariableSource`, whose default returns `null` for all of them (an unresolved variable keeps its default text, or its name, as in Monaco). Showing the choices of
    `${1|a,b|}` as a list is out of scope: the first choice is inserted.
11. **Completion items as snippets (P10-26).** `CompletionItem` gets `InsertTextRules` (flags `InsertAsSnippet`, `KeepWhitespace`); `LspConvert` sets `InsertAsSnippet` for
    `InsertTextFormat == 2` and stops calling `LspSnippet.ToPlainText`, which is deleted when unused. The snippet syntax then has one implementation.
12. **No new project, no new dependency.** The dependency table of `ARCHITECTURE.md` does not change.

## Consequences

- One rebuild path (`RecreateTextMeasurer`) serves fonts, ligatures, spacing and zoom, so a change is a property set, with no special invalidation.
- Ligature carets are approximate inside a ligature; word wrap is approximate with ligatures on. Both are documented as behavior differences.
- Whitespace and control characters cannot change offsets, so selection, hit testing and accessibility are unaffected.
- Snippets are testable without a UI: the parser and variables are in Core, the session only needs a model.

## Alternatives

- **Split the line into runs and turn ligatures off around the caret.** Rejected: Monaco does not do it either (it sets `font-feature-settings` and reads positions from the DOM).
- **Draw whitespace by replacing characters in the text.** Rejected: widths and offsets would change, and the layout would no longer be the user's text.
- **A process-wide zoom and input mode, as in Monaco.** Rejected: two editors in one window would change each other.
- **Variable fonts through a native DirectWrite call.** Rejected for phase 10: Win2D has no API, and a second text stack is out of proportion for one axis.

## Changes during implementation (2026-10-29, P10-31)

Only the changes that cards P10-22 to P10-30 recorded are listed here. The other cards (P10-03 to P10-21) record theirs in their own file and in the "Behavior differences" rows of
[PORTING.md](../PORTING.md); they were not re-read for this section.

- **Decision 10, the host interface** is named `ISnippetVariableHost` (not `ISnippetVariableSource`) and also gives the workspace name and folder; the time comes from a `TimeProvider` and `RANDOM` from a `Random` (P10-22).
- **Decision 10, the clipboard.** The clipboard can only be read asynchronously on Windows, so `LoomEditor.InsertSnippet` reads it first when the template mentions `CLIPBOARD` and passes the text as an option; the host the control gives returns `null` (P10-25).
- **Decision 10, tab stops.** `Placeholder.Index` is an `int`, so the fractional indices Monaco gives nested snippets live in a local table until the renumbering (P10-23); `SnippetSession` has no overtyping capturer and no snippet edits (P10-24).
- **Decision 10, keys.** `Tab` / `Shift+Tab` / `Escape` of snippet mode use Monaco's weight `EditorContrib + 30`, and `Escape` of the problem navigation `EditorContrib + 50` (P10-28), as Monaco does; the task cards had described the relative order with the find widget differently (P10-25, P10-28).
- **Decision 11.** `LspSnippet.cs` and its tests were deleted and the client declares `snippetSupport: true` (P10-26); a snippet item is inserted as plain text when no snippet controller is attached.
- **Not in the ADR at all.** `F8` navigation (P10-27, P10-28) selects the marker and announces it with no widget; the Native AOT self-check of P10-30 could not be run (the published Gallery crashes at start-up, see the [roadmap](../roadmap/phase-10-editor-completeness.md)).

## Not delivered

Taken from the "Later" table of [docs/tasks/phase-10/README.md](../tasks/phase-10/README.md):

- Unicode highlight (`unicodeHighlighter`), which needs Monaco's generated confusables table and a licence / size decision.
- Font tokens of themes and custom line heights from decorations (non-uniform runs; needs its own ADR).
- Semantic highlighting and tree-sitter.
- `fontVariationSettings` beyond `wght` (no Win2D API).
- Rich (HTML) copy, touch gestures, Intl word segmentation, IME on Windows 10 (TSF).
- Linked editing, format document / selection / on type, signature help, references, rename, code actions, document links, workspace symbols (one provider contract, widget and LSP binding each).
- Syntax folding from a provider, sticky scroll outline model, minimap section headers, find history.
- Color decorators and picker, CodeLens, inlay hints, inline completions.
- Context menu, command palette, quick access (host UI: needs an ADR).
- The marker navigation widget (the peek-like box of `F8`): phase 10 selects the marker and announces its message.
- The choice list of snippets, snippet text edits (`apply`) and the overtyping capturer.
- Diff editor deferred items ([ADR 0012](0012-diff-editor.md)).
- Accessibility help dialog, `ariaLabel` / `ariaRole`, reduced motion, UIA text edit pattern.
- Zoom shared between the two editors of `LoomDiffEditor`.
- Directional formatting characters rendered as `[U+202A]` by `renderControlCharacters`.
