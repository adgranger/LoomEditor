# P10-12 — Cursor styles, `OvertypeCursorStyle` and the `Insert` key

- **Status:** done
- **Depends on:** P10-11
- **Size:** M

## Goal

The caret can be drawn in Monaco's six styles (`Line`, `Block`, `Underline`, `LineThin`, `BlockOutline`, `UnderlineThin`); the `Insert` key toggles overtype mode, and while in overtype mode the caret uses `OvertypeCursorStyle` (default `Block`).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/config/editorOptions.ts` | 1630–1690 | `TextEditorCursorStyle` and its string conversions |
| `src/vs/editor/common/config/editorOptions.ts` | 2064–2080 | `EffectiveCursorStyle.compute`: the overtype style is used when `inputMode === 'overtype'` |
| `src/vs/editor/common/config/editorOptions.ts` | 6272–6285 | the `cursorStyle` (default `Line`) and `overtypeCursorStyle` (default `Block`) options |
| `src/vs/editor/browser/viewParts/viewCursors/viewCursor.ts` | 160–242 | `ViewCursor._prepareRender`: the geometry of each style (`Line` / `LineThin` use the line width; the others measure the **next character**; `Underline` / `UnderlineThin` are 2 px high at the bottom of the line; `Block` draws the character on top; `BlockOutline` is a 1 px outline) |
| `src/vs/editor/browser/viewParts/viewCursors/viewCursors.css` | the rules for `.cursor-block`, `.cursor-block-outline`, `.cursor-underline`, `.cursor-underline-thin`, `.cursor-line-thin` | color and thickness of each (read the file: it has the exact values; `block-outline` is `border: 1px solid` in the cursor color, `*-thin` use 1 px, the others the cursor width) |

## Files to create or change

- `src/LoomEditor.View/Layout/TextEditorCursorStyle.cs` — create (enum + `static` conversions if needed)
- `src/LoomEditor.Rendering/TextViewRenderer.Caret.cs` — create: move `DrawCaret` (now in `TextViewRenderer.cs` ~260) here and extend it; `TextViewRenderer.cs` — change accordingly. Properties `CursorStyle` and `OvertypeCursorStyle` on the renderer, and the active `EditorInputMode` (a field the control sets)
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: `Insert` (no modifiers, `editorTextFocus`) → `editor.action.toggleOvertype` (P10-11); `KeyCode` may need `Insert` (check `KeyCode.cs`; add it and map `VirtualKey.Insert` in `src/LoomEditor.WinUI/Input/` where the other keys are mapped)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Caret.cs` — change: the `CursorStyle` / `OvertypeCursorStyle` dependency properties (type `TextEditorCursorStyle`, made public with a doc), `InputMode` read-only mirror (`EditorInputMode`, a `DependencyProperty` with a private setter updated from `EditorContext.InputModeChanged`) and the invalidate
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change
- `tests/LoomEditor.Rendering.Tests/TextViewRendererCaretStyleTests.cs` — create; the Gallery gets a `ComboBox` "Cursor style" (6 values) and an "Overtype" `ToggleSwitch` bound to `Editor.InputMode`

## Design decisions (already made — do not change)

- Geometry per style, in DIPs, from the layout (`GetCharacterX(i)` and `GetCharacterX(i + 1)` give the character cell; a cell narrower than 1 or a tab gets `font.TypicalHalfwidthCharacterWidth`, as `viewCursor.ts` 219–225):
  `Line`: today's drawing (the width rules of the current `DrawCaret` stay); `LineThin`: 1 px wide (`1 / PixelRatio` DIPs, at least 1 device pixel), same height; `Block`: the cell × line height, filled with the caret color, then the character drawn on top in the **editor background color** (`Theme.Background`) through a one-character `LineLayoutCache` entry (when the caret is at the end of the line there is no character: only the filled cell of `TypicalHalfwidthCharacterWidth`); `BlockOutline`: the cell outlined 1 px; `Underline`: the cell × 2 px at the bottom of the line; `UnderlineThin`: the cell × 1 px at the bottom.
- The next grapheme (for the cell width of the block styles) is the UTF-16 run to the next caret stop: use `layout.GetCharacterX` of the next offset that is not a low surrogate.
- Effective style = `InputMode == Overtype ? OvertypeCursorStyle : CursorStyle`. The blinking (`CursorBlinking`) is unchanged.
- Defaults: `CursorStyle = Line`, `OvertypeCursorStyle = Block`.
- `InputMode` on the control is **read-only** (changed by the `Insert` key or the command `editor.action.toggleOvertype`); setting the mode from code is `ExecuteCommand`-like and out of scope.
- No change to how the caret is positioned for the IME or accessibility.

## Tests

- [x] New (recording surface): `Line_Draws2PxWideRect` (the current behavior, unchanged), `Block_FillsCell_AndRedrawsCharacterInBackgroundColor`, `Block_AtEndOfLine_FillsTypicalWidthWithoutCharacter`, `BlockOutline_DrawsRectangleOutline`, `Underline_Is2PxAtBottom`, `UnderlineThin_Is1PxAtBottom`, `LineThin_Is1PxWide`, `OvertypeMode_UsesOvertypeStyle`, `Tab_BlockCursor_UsesTypicalWidth`
- [x] New (View): `Insert_Key_ResolvesToToggleOvertype` (the keybinding resolver test pattern of `tests/LoomEditor.View.Tests/Commands`), `Insert_Key_NotWhenFindInputFocused`
- [x] Manual (section "P10-12"): `Insert` toggles; block caret in overtype with the character visible inside; typing overwrites; every style in the combo box looks like Monaco's; blinking still works; the caret in a long wrapped line is on the right view line.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `viewCursor.ts` (cursor styles) and `editorOptions.ts` (cursor style options) rows
- [x] Public API file updated; card status and `docs/tasks/README.md` updated

## Out of scope

`cursorWidth` option changes, `cursorSmoothCaretAnimation`, `cursorBlinking` styles beyond the existing ones, a status bar.

## Suggested commit message

`feat(rendering): add the six cursor styles and toggle overtype with the Insert key`
