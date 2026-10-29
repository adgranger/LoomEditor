# P11-14 — Focus and keys between the editor and the widget's text box

- **Status:** done
- **Depends on:** P11-13
- **Size:** S
- **Recommended model:** Opus (keyboard, focus and IME paths cross the control and its children; a gap shows only at runtime as a character typed into the document, an editor shortcut firing from the box, focus lost after a removal, or an IME candidate window in the wrong place. On failure — stop, do not commit, report the exact keys, which element had the focus (`FocusManager.GetFocusedElement`), what happened, and what you tried.)

## Goal

With the focus in a comment box every key belongs to the box: typing, `Tab` / `Shift+Tab`, `Enter`, editor shortcuts and IME composition behave as in any text box and nothing reaches the document. The focus enters the box on request and returns to the editor (caret on the thread's line) when the composer closes or the box is removed. The placeholder widget gets a **temporary** text box and two buttons so this is checkable before the real composer exists (P11-17 replaces them).

## Sources

Sources: none (specified in this card; ADR 0014 decision 7). **Most of the routing already exists** — check each path, change only what the check shows is missing:
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` lines 163–197 `OnKeyDown`: when `!HasFocus` the key goes to `base.OnKeyDown` without any keybinding (comment: "A key that bubbles up from the find widget"); `OnKeyUp` likewise;
- `LoomEditor.Keyboard.cs` `OnCharacterReceived`: returns when `!HasFocus`;
- `LoomEditor.Caret.cs` line 75: `HasFocus => FocusState != FocusState.Unfocused` (the control's own state: `Unfocused` while a child has the focus);
- `LoomEditor.Ime.cs` lines 48–56: `NotifyFocusEnter` only when `HasFocus`; `NotifyFocusLeave` on any bubbled `LostFocus`;
- `LoomEditor.Pointer.cs`: `IsFromOverlay` guards press / move / release (P11-12 extended it to the zone-widget layer);
- `LoomEditor.cs` line 87: `LostFocus += _keyboard.LeaveChordMode()`;
- `Find/FindWidget.Keyboard.cs` (the precedent: a text box inside the control; it routes a few keys back to the editor with `TryHandleKey`; **the comment box routes none**);
- `src/LoomEditor.View/Features/TabFocus/` (tab focus mode: must not see a `Tab` from the box — it cannot, if `OnKeyDown` returns early; verify).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor*.cs` — change **only** where a check below fails (expected: no change or a one-line guard)
- `src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.cs` — change: `FocusComposer(CommentThread)`, `ReturnFocusToEditor(CommentThread)`, the focus move on `ThreadRemoving`
- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` — change: `AddCommentThread(..., startComposing: true)` asks the host to focus the box
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs` — change: the temporary controls
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-14"

## Design decisions (already made — do not change)

- **Check list (write the outcome of each in the card's Result section):** (a) a key pressed in the box reaches `LoomEditor.OnKeyDown` with `HasFocus == false` and runs no keybinding; (b) `OnCharacterReceived` inserts nothing; (c) `Tab` from the box is not handled by the tab-focus feature; (d) the IME context of the editor gets `NotifyFocusLeave` and no `NotifyFocusEnter` while the box has the focus; (e) the caret stops blinking and is drawn unfocused; (f) a pointer press on the box is ignored by the text logic. Add a guard only for a check that fails.
- **Tab:** standard XAML focus navigation. A WinUI `TextBox` never inserts a tab character (there is no `AcceptsTab` property in WinUI — do not look for one). `Shift+Tab` from the first element of the box goes to the editor control.
- **`FocusComposer(thread)`:** focuses the box's primary input with `FocusState.Programmatic`; if the element is not yet loaded, do it on its `Loaded` event (once). Called by `AddCommentThread(..., startComposing: true)`, by the Reply action (P11-17) and by the Gallery.
- **`ReturnFocusToEditor(thread)`:** `editor.Focus(FocusState.Programmatic)`, the cursor at `(thread.LineNumber, 1)` through the editor's selection API (`_context.SetSelections`), and a reveal of that line only when it is outside the viewport (grep `LoomEditor.Reveal.cs`). Called after Cancel and Escape.
- **Removal while focused:** on `controller.ThreadRemoving`, if the focused element (`FocusManager.GetFocusedElement(editor.XamlRoot)`) is inside that widget, call `ReturnFocusToEditor` **before** the element leaves the tree.
- **Temporary controls** (deleted by P11-17): a multi-line `TextBox` (`AcceptsReturn = true`, `TextWrapping = Wrap`, `MinHeight = 64`), `TextChanged` → `viewModel.SetDraft`; buttons **Cancel** (`viewModel.Cancel()` then `ReturnFocusToEditor`) and **Comment** (`viewModel.Submit()`); in the text box's `KeyDown`: `Escape` → `viewModel.TryEscape()` then, if `true`, `ReturnFocusToEditor`; `Ctrl+Enter` → `Submit()`. Mark `Handled` only for these two keys, and only when they did something.
- No new keybinding context key, no new `KeybindingContext` field, no change to `IEditorContext`.

## Tests

- [x] Automated: none for WinUI.
- [x] Manual (section "P11-14"; temporary sample plus a button **Start composing on line 5**). Each check states the expected result:
  1. type `abc` and `Backspace` in the box: the box shows `ab`; the document text is unchanged (its undo stack too: `Ctrl+Z` after leaving the box does nothing) and its caret does not blink;
  2. `Tab` goes box → Cancel → Comment → next control after the editor; `Shift+Tab` back; the document gets no tab;
  3. `Enter` adds a line in the box; `Ctrl+Enter` submits (the Gallery event log shows one `CommentSubmitted`);
  4. `Ctrl+A`, `Ctrl+Z`, `Ctrl+C` / `Ctrl+V`, `Ctrl+F`, `F12`, `Ctrl+D` in the box: they act on the box or do nothing; **no** editor command runs (no find widget opens, no selection change in the document);
  5. `Escape` with an empty box: the composer closes, the focus is in the document, the caret on line 5 blinks; `Escape` with text: nothing happens;
  6. click in the document while the box has text, then click back in the box: the text is still there;
  7. **Remove all** while the box has the focus: the focus lands in the document (not on the window), keyboard input works at once;
  8. a Japanese or Chinese IME composes in the box, its candidate window is next to the box; `Win+.` inserts an emoji in the box;
  9. Narrator follows the focus into the box and back.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green (keyboard tests of earlier phases unchanged)
- [x] The Result section lists checks (a)–(f) of the design decisions with "already true" or "fixed by …"
- [x] The manual section is written; the card result says the agent did **not** run it
- [x] Card status and `docs/tasks/README.md` updated

## Result

Checks of the design decisions (verified by reading the code paths; the manual run is still to do):

- (a) **Already true.** A key from the box bubbles to `LoomEditor.OnKeyDown` with `HasFocus == false` (`FocusState` is `Unfocused` while a child has the
  focus): it goes to `base.OnKeyDown` before `EmitKeyDown` and `_keyboard.TryHandle`. `OnKeyUp` emits nothing either. No `PreviewKeyDown` or keyboard
  accelerator on the control.
- (b) **Already true.** `OnCharacterReceived` returns when `!HasFocus`.
- (c) **Already true.** Tab focus mode is a keybinding: `OnKeyDown` returns before any keybinding runs.
- (d) **Already true.** `OnImeLostFocus` calls `NotifyFocusLeave` on the bubbled `LostFocus`; `OnImeGotFocus` checks `HasFocus`, which is false for a
  bubbled `GotFocus` from the box.
- (e) **Already true.** `OnCaretFocusChanged` runs on the bubbled focus events and reads `HasFocus` (false): the blink stops, the caret draws unfocused.
- (f) **Already true.** `OnPointerPressed` / move / release return on `IsFromOverlay`, which covers the zone-widget layer since P11-12.

No guard was added to `LoomEditor*.cs`. Other notes:

- `Escape` and `Ctrl+Enter` are read in the text box's `PreviewKeyDown`: the WinUI `TextBox` handles `Enter` itself, so its `KeyDown` never sees `Ctrl+Enter`.
- The text box writes `\r` line breaks; the draft comparison normalizes them, or each key press would reset the text and the caret.
- When the composer closes with the focus inside the widget (Submit, Cancel, Escape), the widget raises `FocusReturnRequested` and the host calls
  `ReturnFocusToEditor`: after a submit the composer collapses, and a collapsed focused box would leave the focus on the window.
- `ReturnFocusToEditor` reveals with `RevealRange` (Monaco `revealRange`: the scroll position is kept when the line is visible).
- Manual check 7: clicking **Remove all** moves the focus to the button first, so the check also asks for Cancel on a thread with no comment.
- The manual section "P11-14" is written; the agent did **not** run it.

## Out of scope

The toolbar and Preview (P11-17, P11-18), accessibility names (P11-20), drag and drop of text between the box and the document.

## Suggested commit message

`feat(winui): move the focus into and out of comment widgets`
