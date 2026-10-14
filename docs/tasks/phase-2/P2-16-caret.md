# P2-16 — Caret: drawing and blinking logic

- **Status:** todo
- **Depends on:** P2-15
- **Size:** S

## Goal

Draw the caret (Monaco's "cursor", line style, 2 DIPs wide) at the selection's active position, and port Monaco's
blinking state machine. Blinking is hidden when unfocused, solid when read-only, and otherwise toggles every 500 ms,
restarting visible after each change. The WinUI timer that drives it comes in P2-19.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/viewCursors/viewCursors.ts` | 32 | `BLINK_INTERVAL = 500` |
| same | 226–278 | `_getCursorBlinking`, `_updateBlinking` (flat `Blink` style only) |
| same | 343–356 | `_show`, `_hide` |
| `src/vs/editor/browser/viewParts/viewCursors/viewCursor.ts` | 67–68, 163–205 | `_lineCursorWidth = min(cursorWidth, typicalHalfwidthCharacterWidth)`; `_prepareRender` for `TextEditorCursorStyle.Line`: width `cursorWidth > 0 ? cursorWidth : 2` rounded to device pixels, full line height, top = line top |
| `src/vs/editor/common/config/editorOptions.ts` | 1583–1608 | `TextEditorCursorBlinkingStyle` |
| same | 6251–6253, 6302–6303 | `cursorBlinking` (`blink`), `cursorWidth` (0) defaults |

## Files to create or change

- `src/LoomEditor.Rendering/CursorBlinking.cs` — create (the state machine)
- `src/LoomEditor.Rendering/TextEditorCursorBlinkingStyle.cs` — create
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — draw the caret
- `tests/LoomEditor.Rendering.Tests/CursorBlinkingTests.cs`, `TextViewRendererCaretTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class CursorBlinking` with no timer inside it. The host calls it:
  - `void Update(bool hasFocus, bool readOnly)` → recomputes the effective style (`Hidden` when unfocused, `Solid` when
    read-only, else the configured style) and shows or hides the caret, like `_updateBlinking`;
  - `void Reset()` → shows the caret again (call it on selection change, like Monaco's `onCursorStateChanged`);
  - `void Tick()` → toggles visibility, only when the effective style is `Blink`;
  - `bool IsVisible`, `bool NeedsTimer` (true when the effective style is `Blink`), `event EventHandler? Changed`.
- Styles `Smooth`, `Phase`, `Expand` are CSS animations in Monaco. They are ported as enum values and behave like
  `Blink`. Document this in the XML doc and in `docs/PORTING.md`.
- The editor is not read-only in phase 2 (the model can be edited through the API), so `readOnly` is `false`.
- The renderer draws the caret when `hasFocus && cursorVisible` (new `Render` parameter: `bool cursorVisible`), after
  the text and before the gutter, with `Theme.CursorForeground`:
  `x = ContentLeft + GetCharacterX(column - 1) - scrollLeft`, width = `cursorWidth > 0 ? min(cursorWidth, TypicalHalfwidthCharacterWidth) : 2`,
  snapped to device pixels with `FontInfo.PixelRatio` like Monaco's `computeScreenAwareSize`
  (`src/vs/base/browser/dom.ts` 1609–1612: `Math.Max(1, Math.Floor(w × ratio)) / ratio`). As in `viewCursor.ts`
  lines 195–200, when the width is ≥ 2 and x ≥ 1, x moves 1 DIP to the left ("between the characters").
  If the position's line is not visible, nothing is drawn.
- Cursor style is `Line` only. `Block` and `Underline` come with the options of phase 3.

## Tests

- [ ] New: unfocused → `IsVisible == false`, `NeedsTimer == false`; focused → visible and `NeedsTimer == true`
- [ ] New: `Tick` toggles; `Reset` after a tick makes it visible again
- [ ] New: `Update(hasFocus: true, readOnly: true)` → visible, `NeedsTimer == false` (solid)
- [ ] New: renderer draws a 2-wide caret rectangle at `GetCharacterX(column - 1) - 1` (column > 1) with `CursorForeground`;
      at column 1 there is no shift; at pixel ratio 1.25 the width is `Math.Floor(2.5) / 1.25 = 1.6`
- [ ] New: no caret when `cursorVisible == false` or when the caret line is scrolled out

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `viewCursors.ts` (blinking logic) and `viewCursor.ts` (line style) → adapted; behavior difference for CSS-animated styles
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The WinUI timer and focus events (P2-19); multiple cursors, block/underline styles, IME composition (phase 3).

## Suggested commit message

`feat(rendering): draw the caret and port the blinking logic`
