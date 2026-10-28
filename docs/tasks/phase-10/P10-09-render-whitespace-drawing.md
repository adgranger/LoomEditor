# P10-09 — `renderWhitespace`: drawing the dots and arrows, and the control property

- **Status:** todo
- **Depends on:** P10-08
- **Size:** M

## Goal

`LoomEditor.RenderWhitespace = Boundary | Selection | Trailing | All` draws a `·` over every space and a `→` over every tab that the computer of P10-08 selects, in the `editorWhitespace.foreground` color, and nothing for `None` (the default, as in Monaco).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewLayout/viewLineRenderer.ts` | 148–157 | `renderSpaceWidth` / `renderSpaceCharCode`: `·` U+00B7 with `MiddotWidth`, U+2E31 with `WsmiddotWidth` when `abs(WsmiddotWidth - SpaceWidth) < abs(MiddotWidth - SpaceWidth)` (the constructor of `RenderLineInput`, lines 148-157) |
| `src/vs/editor/common/viewLayout/viewLineRenderer.ts` | 1113–1132 | what a tab and a space look like: `→` U+2192 (or the halfwidth arrow U+FFEB when `CanUseHalfwidthRightwardsArrow` and the tab is one cell wide), `·` for a space |
| `src/vs/editor/common/config/editorOptions.ts` | 6664–6680 | `renderWhitespace` option: Monaco's default is `selection`; LoomEditor's default is `None` (decision below) |

## Files to create or change

- `src/LoomEditor.Rendering/TextViewRenderer.Whitespace.cs` — create (`DrawWhitespace`, called from `Render` right after the text and before the caret, next to `DrawGuides`)
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — change: call it; a `RenderWhitespace` property (`RenderWhitespace` enum of P10-08, default `None`)
- `src/LoomEditor.Themes/EditorTheme.cs` — change: `WhitespaceForeground = Color("editorWhitespace.foreground")` (the default row exists in `EditorColorDefaults.cs`), with its XML doc
- `src/LoomEditor.WinUI/Controls/LoomEditor.Whitespace.cs` — create: the `RenderWhitespace` dependency property (type `RenderWhitespace`: make the enum `public` here and list it in `PublicAPI.Unshipped.txt`; if making P10-08's enum public needs a doc comment, add it)
- `tests/LoomEditor.Rendering.Tests/TextViewRendererWhitespaceTests.cs` — create
- Gallery (`MainWindow.xaml(.cs)`): a `ComboBox` "Render whitespace" (None, Selection, Boundary, Trailing, All); the sample text has a line with tabs, double spaces, and trailing spaces

## Design decisions (already made — do not change)

- **Overlay** (ADR 0013 decision 7). For each visible view line: `offsets = RenderWhitespaceComputer.Compute(content, content.Length, MinColumn - 1, mode, ContinuesWithWrappedLine, SelectionsOnLine(...))`; `SelectionsOnLine` only in `Selection` mode, from the same selections the renderer already uses for `DrawSelection`. For each offset `i`: `x0 = layout.GetCharacterX(i)`, `x1 = layout.GetCharacterX(i + 1)`; a space draws the one-character layout of `·` (a `LineLayoutCache` entry, `Get("·", ...)` with a `LineStyle` whose only run has the whitespace color) **centered** in `[x0, x1]`; a tab draws `→` at `x0`, **left aligned**, and the cell width is `x1 − x0` (the arrow is not stretched). Use U+2E31 instead of U+00B7 under the Monaco condition above. Draw with the same `top` and `scrollLeft` as the line's text.
- Characters inside a view line that the renderer cut (offset ≥ layout text length) are skipped.
- The default is `None` although Monaco's is `selection`: an editor that draws dots unasked would surprise users of a control library (documented in `PORTING.md` "Behavior differences").
- Changing the property only invalidates the canvas (no measurer recreation).
- Performance: nothing is computed when the mode is `None`. Minimum one string allocation per drawn line (the offsets list); do not cache across frames.
- The word wrap indicator of the backlog is **not** part of this card (Monaco has none at this commit).

## Tests

- [ ] New (recording surface): `None_DrawsNothing`, `All_DrawsOneDotPerSpace` (draw calls for `"a b c"` = 2), `All_Tab_DrawsArrowAtTabStart`, `Boundary_InnerSingleSpace_NotDrawn`, `Selection_OnlyInsideSelection`, `Trailing_OnlyAtTheEnd`, `Dot_IsCentered_InTheCell`, `UsesWhitespaceForeground`, `ScrollLeft_ShiftsTheDots`, `WrappedLine_FauxIndent_NotDrawn` (use `ViewHarness` with word wrap, as `TextViewRendererWordWrapTests` does)
- [ ] Manual (section "P10-09"): the five modes on the sample; tabs show arrows of the tab width; with a proportional-width font nothing overlaps text; dots stay aligned while scrolling; high contrast theme: the dots are visible.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: "Behavior differences" rows (default `None`, overlay drawing instead of character substitution)
- [ ] Public API file updated; card status and `docs/tasks/README.md` updated

## Out of scope

Control characters (P10-10), the minimap, rendering whitespace in the diff editor's deleted-lines zones, `experimentalWhitespaceRendering`.

## Suggested commit message

`feat(rendering): draw whitespace (renderWhitespace) and add the RenderWhitespace property`
