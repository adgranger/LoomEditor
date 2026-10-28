# P10-02 — ADR 0013: font options, ligatures, zoom, whitespace and snippets

- **Status:** done
- **Depends on:** P10-01
- **Size:** S (documentation only, no code)

## Goal

`docs/adr/0013-text-options-and-snippets.md` records the design decisions below, in the format of the earlier ADRs (`docs/adr/0012-diff-editor.md`: Context, Decision, Consequences, Alternatives), and `docs/ARCHITECTURE.md` gets a "Text rendering options and snippets (phase 10)" section with the three flows. The decisions are **already made here**; the ADR writes them down with their reasons, it does not reopen them.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

Sources: none (specified in this card). Read to write the "Context" part: `src/vs/editor/browser/config/fontMeasurements.ts` 140–250, `src/vs/editor/common/config/fontInfo.ts` 30–75, `src/vs/editor/common/viewLayout/viewLineRenderer.ts` 474–551 and 794–956, `src/vs/editor/contrib/snippet/browser/snippetSession.ts` 28–60.

## Files to create or change

- `docs/adr/0013-text-options-and-snippets.md` — create
- `docs/ARCHITECTURE.md` — change: add the section after "Diff editor (phase 9)", update "Where do I find…?" with rows for fonts / zoom, whitespace, snippets, input mode (point at the folders the cards create)
- `docs/adr/README.md` (if it lists the ADRs; `Get-ChildItem docs/adr`) — change

## Design decisions (already made — do not change)

1. **Font options are one immutable record.** `EditorFontOptions` (View/Layout): `FontWeight` (string, Monaco's `fontWeight`: `normal`, `bold` or `1`–`1000`), `LetterSpacing` (double), `FontFeatureSettings` (string, the CSS `font-feature-settings` syntax; `""` is "off"), `FontVariationSettings` (string). The control keeps `FontFamily` and `FontSize` as today and reuses the inherited `FontWeight`; the new ones are `FontLigatures`, `FontFeatureSettings`, `FontVariationSettings`, `LetterSpacing` (P10-05). Changing any of them goes through `RecreateTextMeasurer`, like a font change.
2. **Ligatures use DirectWrite typography features** (`CanvasTypography` on the `CanvasTextLayout`). The features are **always set explicitly**: DirectWrite enables `liga` and `calt` by default, so "off" means `liga=0, calt=0` (Monaco's `EditorFontLigatures.OFF`, `editorOptions.ts` 1918–1966), "on" means `liga=1, calt=1`. A string value is parsed (`'ss01' 1, "calt" off`) into `(tag, value)` pairs.
3. **Positions come from the layout, never from the width table.** `ILineLayout.GetCharacterX` / `HitTest` already give columns, caret, selection and hit testing; DirectWrite divides a ligature's advance equally between its characters, and that is accepted (Monaco has no special case either). The width table (`FontInfo`) stays the input of word wrap, the minimap and the view model; with ligatures on, `FontInfo.IsMonospace` is `false` (`fontMeasurements.ts` 219) and word wrap is therefore an approximation, as it already is (see "Behavior differences" in `PORTING.md`).
4. **Letter spacing** is `CanvasTextLayout.SetCharacterSpacing(0, length, 0, letterSpacing, 0)` on every layout and is added to every measured character width (`CharacterWidthReader`), so `FontInfo` includes it (Monaco measures in the DOM with the CSS property set).
5. **Font variations:** only the `'wght'` axis is applied (as the font weight); other axes are parsed, stored and ignored. Win2D has no variation API. The XML doc of the property says so.
6. **Zoom is per editor.** `EditorZoomState` (View) holds the level (clamped to −5…20, `editorZoom.ts`) and fires `Changed`; `BareFontInfo.Create` multiplies font size and line height by `1 + level * 0.1` (`fontInfo.ts` 48–50). A zoom change goes through `RecreateTextMeasurer`. No default key is bound (Monaco binds none); `Ctrl` + wheel is the option `MouseWheelZoom` (default `false`, like Monaco).
7. **Whitespace is drawn as an overlay**, not by substituting characters: a pure computer (`RenderWhitespaceComputer`, a port of `_applyRenderWhitespace`) returns the whitespace ranges of a line; the renderer draws a `·` (U+00B7, or U+2E31 when the font's `·` is wider than a space, `viewLineRenderer.ts` 148–157) centered in the cell of each space and a `→` (U+2192) at the start of each tab, in the color `editorWhitespace.foreground`, **over** the text layout (positions from `GetCharacterX`). The text layout and every offset stay unchanged.
8. **Control characters** are substituted **1:1** in the text given to the layout (`U+0000`–`U+001F` except tab → `U+2400 + code`, DEL → `U+2421`), so offsets are unchanged. Directional formatting characters are not rendered as `[U+202A]` (it would not be 1:1): out of scope.
9. **Input mode (overtype) is per editor**, state in `EditorContext.InputMode`, not a singleton. The `Insert` key toggles it. The caret style comes from `CursorStyle` / `OvertypeCursorStyle` (all six Monaco styles).
10. **Snippets.** The grammar, scanner and markers (`snippetParser.ts`) and the variables are pure logic in `LoomEditor.Core/Snippets/`, internal. The session (`OneSnippet`, `SnippetSession`) and the controller are in `LoomEditor.View/Features/Snippet/`. Tab stops and placeholders are tracked with model decorations (`TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges`, as Monaco). Variables that need a host (`CLIPBOARD`, `TM_FILENAME`, `TM_DIRECTORY`, `TM_FILEPATH`, `WORKSPACE_*`) go through one interface, `ISnippetVariableSource`, whose default implementation returns `null` for all of them (an unresolved variable keeps its default text, or its name, as in Monaco). Showing the choices of a `${1|a,b|}` placeholder as a list is **out of scope**: the first choice is inserted.
11. **Completion items as snippets (P10-26):** `CompletionItem` gets `InsertTextRules` (flags: `InsertAsSnippet`, `KeepWhitespace`); `LspConvert` sets `InsertAsSnippet` for `InsertTextFormat == 2` and stops calling `LspSnippet.ToPlainText` (which is deleted when unused).
12. **No new project, no new dependency.** The dependency table of `ARCHITECTURE.md` does not change.

## Tests

- [x] None (documentation).

## Acceptance criteria

- [x] ADR 0013 has the 12 decisions above (each with its reason in one or two sentences) and a "Not delivered" list equal to the "Later" table of `docs/tasks/phase-10/README.md`
- [x] `docs/ARCHITECTURE.md` section added; the "Where do I find…?" rows point to folders that the later cards create (write them as `src/LoomEditor.View/Features/Snippet/`, etc.)
- [x] `git status` shows only `docs/` files; no build needed
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Any code. Changing a decision: if one looks wrong while writing the ADR, stop and report.

## Suggested commit message

`docs(adr): add ADR 0013 for font options, zoom, whitespace and snippets`
