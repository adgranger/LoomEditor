# P11-11 — Focus and key routing: the editor must not steal what the widget's text box needs

- **Status:** todo
- **Depends on:** P11-10
- **Size:** M
- **Recommended model:** Opus (keyboard, focus, IME and bubbling events cross the editor control and its overlay children; a gap shows only as a typed character landing in the document, a swallowed Tab, or a broken IME composition at runtime. On failure — characters reach the document, Tab or Escape misbehave, the caret keeps blinking, the IME candidate window sits in the wrong place — stop, do not commit, report the exact keys and the control that had the focus, and what you tried.)

## Goal

With the focus inside a comment widget, **every key belongs to the widget**: typing, `Tab` / `Shift+Tab`, `Escape`, `Enter`, `Ctrl+Enter`, shortcuts and IME composition work as in any text box, nothing is inserted into the document and no editor shortcut fires. Focus enters the widget on request (`startComposing`) and returns to the editor (caret on the thread's line) when the composer closes. The widget gets a **temporary** text box and two buttons so this can be verified before the real composer exists (P11-14 replaces them).

## Sources

Sources: none (specified in this card; ADR 0014 decision 7). Read first, completely: `src/LoomEditor.WinUI/Controls/LoomEditor.cs` (key and focus handlers: `OnKeyDown`, `PreviewKeyDown`, `GotFocus`, `LostFocus` — grep), `LoomEditor.Keyboard.cs` (`OnCharacterReceived`, `TabMovesFocus`, `HasFocus`), `LoomEditor.Ime.cs` (the `CoreTextEditContext` is created for the editor: what happens on focus loss), `LoomEditor.Caret.cs` (focus → caret blinking), `LoomEditor.Pointer.cs` (`IsFromOverlay`, `IsFromFindWidget`, how a press moves the focus to the editor), `Find/FindWidget*.cs` (**the precedent**: a real WinUI text box inside the control; `TryHandleKey`, how it keeps its keys), `Suggest/SuggestPresenter.cs`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` / `LoomEditor.Keyboard.cs` / `LoomEditor.Pointer.cs` — change: the guards (below); the smallest edits that work
- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` — change: `internal void FocusEditor()` / `internal void RevealThreadLine(CommentThread)` helpers if missing
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs` — change: the temporary controls and `FocusComposer()`
- `src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.cs` — change: `FocusComposer(thread)` / `ReturnFocusToEditor(thread)` and the call when `StartComposing` is requested
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-11"

## Design decisions (already made — do not change)

- **The rule:** an input event whose `OriginalSource` is inside the overlay layer (`IsFromOverlay`) is **not** handled by the editor's key logic (`OnKeyDown`, `PreviewKeyDown`, `OnCharacterReceived`, access keys, `TryHandle` of the keybinding resolver), exactly as the find widget's keys are not (follow how it is done there; the widget routes the few editor commands it wants through its own `TryHandleKey`, the comment widget routes **none**). Do not mark events `Handled` to achieve this: the text box must still see them.
- **Characters:** `OnCharacterReceived` already returns when `!HasFocus` (the editor itself is unfocused while a child has the focus, because `FocusState` is per element). Add the explicit `IsFromOverlay(e.OriginalSource)` guard too, and a test-by-reading comment saying why both exist.
- **Tab:** the temporary and the real text box have `AcceptsTab = false`: `Tab` moves the focus to the next focusable element and `Shift+Tab` to the previous one, through standard XAML focus navigation. Make sure the editor's tab-focus-mode handling (`TabMovesFocus`, the `ToggleTabFocusMode` feature) cannot intercept a `Tab` that originates in the overlay. The focus order inside the widget is the order of the elements in the visual tree (toolbar, text box, Preview, Cancel, Comment: P11-14); focus leaving the **last** widget element by `Tab` goes on to whatever follows the editor in the page (the default), `Shift+Tab` from the first goes back to the editor control itself.
- **Pointer:** a press inside the widget focuses the clicked control (default XAML) and does not move the editor's caret or selection; a press in the **editor text** while the widget has the focus moves the focus to the editor (existing behavior) and does **not** discard the draft.
- **Caret and selection of the editor** while the widget has the focus: the caret stops blinking and is drawn as unfocused (as with the find widget); the selection stays.
- **IME:** composition in the widget's text box uses the platform's own text box machinery. The editor's `CoreTextEditContext` must be **inactive** while the editor has no focus (verify in `LoomEditor.Ime.cs`; if it stays active add the guard). The manual test covers a real IME and the Win+. emoji panel.
- **API:** `CommentZoneHost.FocusComposer(CommentThread)` moves the focus into the widget's primary input **after layout** (dispatcher at low priority; retry once if the element is not yet in the visual tree); called when a thread starts composing (`startComposing: true`, the "Reply" action, the add-comment request). `ReturnFocusToEditor(CommentThread)` focuses the editor, puts the cursor at the start of the thread's line and reveals it (no scrolling when already visible); called when the composer closes by Cancel / Escape.
- **Temporary controls** in the placeholder widget (removed by P11-14): a multi-line `TextBox` (`AcceptsReturn = true`, `AcceptsTab = false`) and two `Button`s `Cancel` and `Comment` wired to the view model (`Cancel()` → `ReturnFocusToEditor`; `Submit()` → `controller.RaiseSubmitted`), `Escape` → `viewModel.TryEscape()` (P11-07), `Ctrl+Enter` → submit. Escape and Ctrl+Enter handling lives in the widget's `KeyDown` and sets `e.Handled = true` **only** for those keys.
- No new keybinding context key and no new `KeybindingContext` field.

## Tests

- [ ] Automated: none for the WinUI classes. If a guard needs a pure helper, test it in View tests.
- [ ] Manual (section "P11-11"; Gallery sample of P11-10 plus a "Start composing" button): (1) type text in the box: nothing appears in the document, the document's caret does not blink; (2) `Tab` goes text box → Cancel → Comment → (leaves); `Shift+Tab` goes back; no tab character is typed anywhere; (3) `Enter` adds a line in the box; `Ctrl+Enter` submits (the Gallery logs the event); (4) `Escape` with an empty box closes it and the document caret is on the thread's line and blinks again; `Escape` with text does nothing; (5) `Ctrl+F`, `F12`, `Ctrl+Z`, `Ctrl+A` inside the box act on the **box** (select all in the box, undo in the box), not on the document; (6) click in the document text while the box has text: focus moves to the document, the text of the box is still there when clicking back; (7) a Japanese or Chinese IME composes in the box and the candidate window is next to the box; Win+. inserts an emoji in the box; (8) Narrator / screen reader focus follows into the box.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (the keyboard tests of earlier phases unchanged)
- [ ] The manual section is written; the card result says which checks the agent could not run
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The toolbar, Preview, final styling (P11-14), accessibility names (P11-16), an editor `KeybindingContext` for the widget, drag and drop of text between the box and the editor.

## Suggested commit message

`feat(winui): route keys, focus and IME between the editor and comment widgets`
