# P2-19 — Control: caret blinking and focus

- **Status:** done
- **Depends on:** P2-18
- **Size:** S

## Goal

The caret blinks when the editor has focus and disappears when it loses it. The line highlight and the caret are
redrawn on selection changes. The control drives `CursorBlinking` (P2-16) with a `DispatcherQueueTimer`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/viewCursors/viewCursors.ts` | 130–137, 161–175 | a cursor position change calls `_updateBlinking` (caret shown again, interval restarted) |
| same | 183–187 | `onFocusChanged` → `_updateBlinking` |

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.Caret.cs` — create (partial): timer, focus, selection
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — pass `hasFocus` and `cursorVisible` to `Render`

## Design decisions (already made — do not change)

- Focus: `GotFocus` / `LostFocus` → `CursorBlinking.Update(hasFocus, readOnly: false)` + `Invalidate()`.
  `hasFocus` = `FocusState != FocusState.Unfocused`.
- Selection change (`EditorContext.SelectionChanged`) → `CursorBlinking.Reset()` + `Invalidate()`.
- Timer: one `DispatcherQueueTimer` (`DispatcherQueue.CreateTimer()`), `Interval = 500 ms`, `IsRepeating = true`,
  started when `CursorBlinking.NeedsTimer` becomes true and stopped when it becomes false, so an unfocused editor
  uses no timer. `Reset()` restarts the timer, so the caret stays visible for a full 500 ms after a change, like
  Monaco's `cancelAndSet`. Each `Tick` → `CursorBlinking.Tick()` + `Invalidate()`.
- The timer is stopped in `OnUnloaded`.
- Redraw cost: a blink redraws the whole viewport. That is acceptable (the layout cache makes it cheap). Do not add
  partial invalidation in this card.

## Tests

- [x] Existing tests pass (the state machine is tested in P2-16)
- [x] Manual: click in the editor → caret at 1,1 blinks; click a Gallery button → caret disappears, line highlight stays
- [x] Manual: Ctrl+A → caret moves to the end of the document, stays visible for 500 ms, then blinks; the line highlight disappears (non-empty selection)
- [x] Manual: Task Manager shows no CPU use from the Gallery when the editor is not focused

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, manual checks done
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Moving the caret with keys or the mouse (phase 3), IME (phase 3), caret announcements for screen readers (phase 7).

## Suggested commit message

`feat(winui): blink the caret while the editor has focus`
