# P3-42 — Draw several cursors and selections

- **Status:** done
- **Depends on:** P3-41
- **Size:** S

## Goal

The renderer draws every cursor and every selection, not only the primary one, and the current line highlight
follows Monaco's multi-cursor rules. Typing with several cursors becomes visible.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/viewCursors/viewCursors.ts` | 130–175 | `_onCursorPositionChanged`, `onCursorStateChanged`: one caret per selection position, the primary first |
| `src/vs/editor/browser/viewParts/currentLineHighlight/currentLineHighlight.ts` | 60–80 | `_readFromSelections`: the highlighted lines are the **distinct, sorted** position lines of all selections; `_selectionIsEmpty` is true only when **every** selection is empty |
| same | 184–197 | `_shouldRenderInMargin`, `_shouldRenderInContent` |
| `src/vs/editor/browser/viewParts/lineNumbers/lineNumbers.ts` | 70–80, 190–200 | the active line number is the line of the **primary** selection only |

## Files to create or change

- `src/LoomEditor.Rendering/TextViewRenderer.cs`, `TextViewRenderer.Gutter.cs` — change: `Render(IRenderSurface surface, ViewModel viewModel, IReadOnlyList<Selection> selections, bool hasFocus, bool cursorVisible)` (the primary selection first)
- `src/LoomEditor.View/Layout/ViewModel.cs` — change: `GetViewportData(IReadOnlyList<Selection> selections)` (it already stores a list)
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: pass `ViewModel.GetSelections()`
- `tests/LoomEditor.Rendering.Tests/TextViewRendererMultiCursorTests.cs` — create; existing renderer tests updated to the new signature

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- All carets use `Theme.CursorForeground` and the same width (Monaco's secondary cursors have the same color by default).
- Selections are drawn for every selection, in list order, before the text (as today for one selection).
- Current line highlight: content highlight on each distinct line of `selection.PositionLineNumber`, only when all
  selections are empty; gutter highlight on the same lines (its existing rule); active line number = primary only.
- Keep the old single-selection `Render` overload out: update the call sites and the tests (the tests pass a one-item list).

## Tests

- [x] New: two empty selections on lines 2 and 5 → two caret rectangles, two content line highlights, only line 2's number is "active"
- [x] New: one empty and one non-empty selection → no content line highlight, two carets, one selection rectangle
- [x] New: two cursors on the same line → one line highlight, two carets
- [x] Existing renderer tests pass with a one-item list
- [ ] Manual (Gallery): Ctrl+Alt+↓ twice, type → three carets blink together and the text appears on three lines _(left to the maintainer)_

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows `viewCursors.ts`, `currentLineHighlight.ts`, `lineNumbers.ts` mention multi-cursor (P3-42)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Secondary cursor styles, cursor animation (`cursorSmoothCaretAnimation`), selection rounded corners.

## Suggested commit message

`feat(rendering): draw every cursor and selection`
