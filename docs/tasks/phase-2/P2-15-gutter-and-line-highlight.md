# P2-15 — Gutter: line numbers, active line number, current line highlight

- **Status:** todo
- **Depends on:** P2-14
- **Size:** S

## Goal

Draw the gutter like Monaco on Windows: line numbers right-aligned in `lineNumbersWidth`, the active line number in
its own color, and the current line highlight (VS Code's thin border around the cursor line in the Modern themes).
The theme gets the colors these need.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/lineNumbers/lineNumbers.ts` | 111–145 | `_getLineRenderLineNumber`: **only** `RenderLineNumbersType.On` (relative, interval and custom render functions are out of scope) |
| same | 147–206 | `prepareRender`: the active line, `renderFinalNewline` (Windows default `on`: the last empty line keeps its number), without line-number decorations |
| `src/vs/editor/browser/viewParts/currentLineHighlight/currentLineHighlight.ts` | 19–205 | `AbstractLineHighlightOverlay`: when to draw in the content and in the margin (`renderLineHighlight` default `line`, `_selectionIsEmpty`, `renderLineHighlightOnlyWhenFocus` default `false`) |
| same | 236–264 | theming: background if `editor.lineHighlightBackground` is set, else a 2 DIP border of `editor.lineHighlightBorder` |
| `src/vs/editor/common/config/editorOptions.ts` | 6636–6638, 6642–6644 | `renderFinalNewline`, `renderLineHighlight` defaults |
| `src/vs/editor/common/core/editorColorRegistry.ts` | 14–16, 22, 30, 49–50 | default colors: `editor.lineHighlightBorder` (dark `#282828`, light `#eeeeee`), `editorCursor.foreground` (dark `#AEAFAD`, light black), `editorLineNumber.activeForeground` |
| `extensions/theme-defaults/themes/dark_modern.json`, `light_modern.json` | 43–45 | `editorLineNumber.activeForeground` (dark `#CCCCCC`, light `#171184`) |

## Files to create or change

- `src/LoomEditor.Themes/EditorTheme.cs` — add `LineNumberActiveForeground` (`editorLineNumber.activeForeground`),
  `LineHighlightBackground` (`editor.lineHighlightBackground`, nullable), `LineHighlightBorder` (`editor.lineHighlightBorder`, nullable),
  `CursorForeground` (`editorCursor.foreground`, used in P2-16), with the values above for `Dark` and `Light`
  (`LineHighlightBackground = null` for both: the Modern themes use the border)
- `src/LoomEditor.Rendering/TextViewRenderer.cs` (or `TextViewRenderer.Gutter.cs` if the file goes over ~400 lines) — line numbers and line highlight
- `tests/LoomEditor.Rendering.Tests/TextViewRendererGutterTests.cs` — create
- `src/LoomEditor.Themes/PublicAPI.Unshipped.txt` — `./eng/Update-PublicApi.ps1 src/LoomEditor.Themes` (`EditorTheme` is public)

## Design decisions (already made — do not change)

- Active line = `selection.PositionLineNumber` (one cursor until phase 3).
- Line numbers: drawn with `LineNumberForeground`, the active one with `LineNumberActiveForeground`, right-aligned:
  `x = LineNumbersLeft + LineNumbersWidth - layout.Width`.
- Line highlight in the content: drawn when `selection.IsEmpty` (`_shouldRenderInContent`), from `ContentLeft` to the
  right edge of the viewport, after the selection and before the text. If `LineHighlightBackground` is set, fill it.
  Otherwise draw `LineHighlightBorder` with `DrawRectangle(..., strokeWidth: 2)` inset by 1 DIP, so the 2 DIP stroke
  stays inside the line box like CSS `box-sizing: border-box`.
- Margin (gutter) highlight: Monaco's default `renderLineHighlight = 'line'` does not draw in the margin. Port the
  `none | gutter | line | all` switch as an `internal enum RenderLineHighlight` with default `Line`, and draw the
  margin part only for `Gutter` / `All`.
- `renderLineHighlightOnlyWhenFocus` stays `false`. The renderer gets a `bool hasFocus` parameter now (P2-16 needs it
  too): `Render(IRenderSurface, ViewModel, Selection, bool hasFocus)`.

## Tests

- [ ] New: line 7 active → its number uses `LineNumberActiveForeground`, others `LineNumberForeground`
- [ ] New: numbers are right-aligned (`x + width == LineNumbersLeft + LineNumbersWidth`)
- [ ] New: the last empty line of `"a\n"` still gets its number (`renderFinalNewline = on`)
- [ ] New: empty selection → one `DrawRectangle` with `LineHighlightBorder`, stroke 2; non-empty selection → none
- [ ] New: with `RenderLineHighlight.All`, a margin highlight is drawn too

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `lineNumbers.ts` and `currentLineHighlight.ts` → `TextViewRenderer` (adapted: drawn on the canvas)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Relative / interval line numbers, glyph margin, folding arrows, line-number decorations, `editor.inactiveLineHighlightBackground`.

## Suggested commit message

`feat(rendering): draw the active line number and the current line highlight`
