# P6-59 — Drawing the sticky lines

- **Status:** done
- **Depends on:** P6-58
- **Size:** M

## Goal

The renderer draws the sticky lines over the top of the text: each line with its tokens and inline decorations, its line
number in a gutter band, the folding chevron of folding scopes, the last line pushed up by the next scope, the bottom border
and shadow, and the hover background — following Monaco's `StickyScrollWidget`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/stickyScroll/browser/stickyScrollWidget.ts` | 140–153 | `setState` (what changes when the state changes) |
| same | 210–254 | `_renderRootNode` (tops of the lines, `lastLineRelativePosition` on the last one, total height) |
| same | 265–281 | `_setHeight` |
| same | 301–333 | `_renderChildNode`, `_updatePosition` (the last line under the others: z-index 0, moved by `lastLineRelativePosition`, +1 when its fold is collapsed) |
| same | 431–535 | `RenderedStickyLine` (the view line rendering data of the model line's **first view line**, inline decorations, line number text and its box `lineNumbersLeft` / `lineNumbersWidth`, the gutter band of width `contentLeft`) |
| same | 537–578 | `_renderFoldingIconForLine`, `StickyFoldingIcon` (visibility: hovered gutter, collapsed, or `showFoldingControls: always`) |
| `src/vs/editor/contrib/stickyScroll/browser/stickyScroll.css` | 6–73 | the bottom border, the shadow (`0 4px 2px -2px`), the backgrounds, the hover background |
| `src/vs/platform/theme/common/colors/editorColors.ts` | 28–46 | `editorStickyScroll.background`, `editorStickyScrollGutter.background`, `editorStickyScrollHover.background`, `editorStickyScroll.border`, `editorStickyScroll.shadow` |

## Files to create or change

- `src/LoomEditor.Rendering/TextViewRenderer.StickyScroll.cs` — create (header `stickyScrollWidget.ts`): `DrawStickyScroll(...)`, called after
  the text, the after-contents and the composition, and before the minimap, the overview ruler and the caret (the widget is an overlay
  above the text; the caret of a line hidden under it stays hidden by the opaque background)
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: the five colors (derived defaults: `editor.background`, `scrollbar.shadow`,
  `contrastBorder`)
- `src/LoomEditor.Rendering/TextViewRenderer.HitTest.cs` — change: `IsInStickyScroll(double y)` and `GetStickyLineAt(double y)` helpers used by
  P6-60
- `tests/LoomEditor.Rendering.Tests/TextViewRendererStickyScrollTests.cs` — create

## Design decisions (already made — do not change)

- The line content: the view line data of `ConvertModelPositionToViewPosition(line, 1).LineNumber` (only its first view line, like
  Monaco), drawn with the same `LineStyle` path as the main text (tokens, style overrides of inline decorations) at
  `ContentLeft - scrollLeft` when `ScrollWithEditor` (Monaco's default) — otherwise at `ContentLeft`.
- Line numbers: the same formatting and colors as the main gutter (`LineNumberForeground`); relative / interval line numbers are not ported.
- Folding chevron: reuse the gutter chevron drawing of `TextViewRenderer.GutterDecorations.cs` (`DrawGutterIcon`) at `lineNumbersLeft + lineNumbersWidth`.
- Shadow: a 4-DIP band under the widget filled with `editorStickyScroll.shadow` fading to transparent in 2 steps is acceptable; a
  solid 2-DIP band is also acceptable — choose the solid band (simplest) and document it as a behavior difference.
- Hover background: the renderer gets `HoveredStickyLineIndex` from the control (P6-60); `-1` when none.

## Tests

- [x] New: `StickyScroll_Null_DrawsNothing`
- [x] New: `StickyScroll_TwoLines_DrawsBackgroundsNumbersAndTexts`
- [x] New: `StickyScroll_LastLinePushedUp_IsDrawnHigher`
- [x] New: `StickyScroll_CollapsedScope_DrawsTheChevron`
- [x] New: `StickyScroll_Hover_UsesTheHoverBackground`
- [x] New: `GetStickyLineAt_ReturnsTheIndex`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `stickyScrollWidget.ts` row (Win2D drawing; behavior differences: solid shadow, no relative line numbers)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Pointer input (P6-60), commands and the control property (P6-61).

## Suggested commit message

`feat(rendering): draw the sticky scroll lines`
