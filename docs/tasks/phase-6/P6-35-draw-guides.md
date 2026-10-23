# P6-35 — Drawing the indent and bracket pair guides, and the control's guide options

- **Status:** done
- **Depends on:** P6-34
- **Size:** M

## Goal

The renderer draws the guides of the visible lines (1-DIP vertical lines at the guide column, horizontal bracket lines at
the top or bottom of the line), with the colors of P6-33 and the active guide highlighted; the control exposes
`RenderIndentGuides` and `BracketPairGuides`; the gallery can toggle them.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/indentGuides/indentGuides.ts` | 109–158 | `prepareRender`: `left` (from the column with `visibleRangeForPosition`, or `leftOffset + (visibleColumn - 1) * spaceWidth`), the `scrollWidth` / `_maxIndentLeft` cut, the width of horizontal guides |
| same | 60–107 | the events that make the overlay re-render (cursor, decorations, lines, scroll, language configuration): informative — the Win2D renderer redraws every frame |
| same | 321–343 | the CSS rules: `vertical` / `core-guide-indent` = a 1 px line on the **left** edge (inset box-shadow), `horizontal-top` / `horizontal-bottom` = a 1 px border at the top / bottom of the line, `--guide-color-active` / `--indent-color-active` when `indent-active` |
| `src/vs/editor/browser/viewParts/indentGuides/indentGuides.css` | 6–10 | `.core-guide`: full line height |

## Files to create or change

- `src/LoomEditor.Rendering/TextViewRenderer.Guides.cs` — create (header `indentGuides.ts`): `DrawGuides(...)`, called in
  `Render` **after the decorations and before the text** (Monaco's overlay is a content overlay under the text)
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — change: the call
- `src/LoomEditor.WinUI/Controls/LoomEditor.Guides.cs` — create: `public bool RenderIndentGuides` (default `true`) and
  `public EditorBracketPairGuides BracketPairGuides` (`public enum EditorBracketPairGuides { Off, On, Active }`, default `Off`)
  dependency properties that set `EditorLayoutOptions.Guides`
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — update (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `samples/LoomEditor.Gallery/MainWindow.xaml(.cs)` — change: a "Guides" combo box (Indent only / Indent + bracket pairs / None)
- `tests/LoomEditor.Rendering.Tests/TextViewRendererGuidesTests.cs` — create
- `docs/testing/phase-6-manual-tests.md` — add the guide checks (create the file as P6-26 describes if it does not exist yet)

## Design decisions (already made — do not change)

- Pixel positions: `left = layout.GetCharacterX(column - 1)` for a `Column` guide; for a `VisibleColumn` guide,
  `leftOffset + (visibleColumn - 1) * font.SpaceWidth` where `leftOffset` is the x of column 1 (0 in the layout), plus
  `layoutInfo.ContentLeft - scrollLeft`. Monaco's `_maxIndentLeft` = `wrappingColumn * typicalHalfwidthCharacterWidth` when
  wrapping (`-1` otherwise); `scrollWidth` = `ViewLayout`'s scroll width. Stop the line's loop at the first guide past either,
  like Monaco (`break`).
- Drawing: vertical → `FillRectangle(left, top, 1, lineHeight, color)`; horizontal-top → `FillRectangle(left, top, width, 1, …)`;
  horizontal-bottom → `FillRectangle(left, top + lineHeight - 1, width, 1, …)`; width = x of `HorizontalLine.EndColumn` − left
  (or `SpaceWidth` when that column is not in the layout).
- Color: parse the class names produced by P6-27 / P6-34 (`bracket-indent-guide lvl-N`, `core-guide-indent lvl-N`,
  `indent-active`) into `(kind, level, active)` with a small parser in the renderer (no regex needed), then
  `GuideColors` (P6-33). A guide with no color is not drawn.
- The active cursor position is the primary selection's position (`selections[0]`), converted nowhere (it is already a view position).
- Monaco queries `Math.min(visibleEnd + 1, lineCount)` lines and draws only the visible ones: do the same.

## Tests

- [x] New: `IndentGuides_DrawnAtEachLevel` (recording surface: one 1-wide rectangle per level at the expected x)
- [x] New: `ActiveIndentGuide_UsesTheActiveColor`
- [x] New: `RenderIndentGuidesOff_DrawsNothing`
- [x] New: `BracketPairGuides_On_DrawsVerticalAndHorizontalGuides`
- [x] New: `Guides_PastTheWrappingColumn_AreNotDrawn`
- [x] Manual (`phase-6-manual-tests.md`): guides follow folding and wrapping; the active guide follows the cursor

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — succeeds (new public API)
- [x] `docs/PORTING.md`: `indentGuides.ts` row ✅ (Win2D drawing instead of CSS: behavior difference row); roadmap
      `phase-6-view-extras.md`: tick "indentation guides, bracket pair guides"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`renderWhitespace` (not in this phase's cards: listed as a follow-up in the close card).

## Suggested commit message

`feat(rendering): draw the indent and bracket pair guides`
