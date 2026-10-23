# P8-08 — Mouse move and mouse leave events for features

- **Status:** todo
- **Depends on:** P8-07
- **Size:** S

## Goal

Features (hover, later others) hear `MouseMove` and `MouseLeave`, with what is under the mouse in **model** coordinates, like they already hear `MouseDown` / `MouseUp`; the control raises them.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/view/viewUserInputEvents.ts` | the `onMouseMove`, `onMouseLeave` emitters and `emitMouseMove`, `emitMouseLeave` (search the names) | `ViewUserInputEvents.MouseMove`, `MouseLeave`, `EmitMouseMove`, `EmitMouseLeave` |
| `src/vs/editor/browser/controller/mouseHandler.ts` | the `mousemove` and `mouseleave` listeners (search `onMouseMove`, `_onMouseLeave`) | when the control raises them |

## Files to create or change

- `src/LoomEditor.View/Controller/ViewUserInputEvents.cs` — change: two events and two emitters
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: `OnPointerMoved` and `OnPointerExited` raise them
- `tests/LoomEditor.View.Tests/Controller/ViewUserInputEventsTests.cs` — change (or create: `grep -rn "ViewUserInputEvents" tests | head`)
- `docs/testing/phase-8-manual-tests.md` — change: the mouse event line in the Setup section (below)

## Design decisions (already made — do not change)

- `ViewUserInputEvents` gets `public event EventHandler<EditorMouseEventArgs>? MouseMove;` and `public event EventHandler? MouseLeave;`, `public void EmitMouseMove(EditorMouseEventArgs e) => MouseMove?.Invoke(this, Convert(e));`
  and `public void EmitMouseLeave() => MouseLeave?.Invoke(this, EventArgs.Empty);` (the existing `Convert` converts the view target to model coordinates; the "Not ported" remark of the class loses `leave`, keeps drag, drop, wheel, key, context menu).
  Monaco's `onMouseLeave` carries a partial mouse event; LoomEditor's does not (the hover only needs "the mouse left"). The XML docs say so.
- **The control** (`LoomEditor.Pointer.cs`):
  - `OnPointerMoved`: after the existing early returns for the find widget and the minimap, and **only when no mouse-down operation is active** (`_mouseDown is not { IsActive: true }`; during a drag the hover must not show), call
    `_context.UserInputEvents.EmitMouseMove(new EditorMouseEventArgs(mouseEvent, CreateMouseTarget(mouseEvent.X, mouseEvent.Y)))` with `mouseEvent = PointerInput.Create(e, this, 1)` (the same helpers `OnPointerReleased` uses), then continue with `UpdatePointerShape(e)` as today.
    It is **not** raised when `_renderer` is null. A pointer over the scrollbars (`IsFromScrollBar(e.OriginalSource)`) does not raise it.
  - `OnPointerExited`: `_context.UserInputEvents.EmitMouseLeave();` (also when the minimap is active; one line after `base.OnPointerExited(e)`).
  - A pointer over a widget of the overlay layer (P8-11: the hover popup, the completion list) is **not** a `LoomEditor` pointer event of the text area: the widgets are children of the control, so their pointer events bubble to the control's `OnPointerMoved` with `e.OriginalSource` inside the widget.
    Add `private bool IsFromOverlay(object? source)` now (returns `false` until P8-11 gives it a body: it checks whether `source` is a `DependencyObject` inside the overlay canvas) and make `OnPointerMoved` / `OnPointerPressed` return early when it is true, like `IsFromFindWidget`. In this card the method exists and always returns `false`, with a comment `// P8-11 fills this in`.
- No new public API (events are on an `internal` class).

## Tests

- [ ] New (`ViewUserInputEventsTests`; comments `// Monaco: viewUserInputEvents.emitMouseMove`): `EmitMouseMove_ConvertsTheTargetToModelCoordinates` (use the coordinates converter test double the existing mouse-down test uses; if there is none, `grep -rn "ICoordinatesConverter" tests/LoomEditor.View.Tests | head -3` and reuse it or the identity converter), `EmitMouseMove_NoListener_NoException`,
      `EmitMouseLeave_RaisesOnce`, `EmitMouseMove_PassesTheEvent` (modifiers and coordinates unchanged), `Unsubscribe_StopsDelivery`
- [ ] The control change has no unit test; manual line for the Setup section of `docs/testing/phase-8-manual-tests.md`: "Run the Gallery from a debugger with a breakpoint in a `MouseMove` handler (or the hover of P8-11) to see the events"; the real check is P8-11's manual section

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: the two new emitters (`viewUserInputEvents.ts`)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Hover logic (P8-09, P8-10), the overlay layer (P8-11), drag / drop / wheel events.

## Suggested commit message

`feat(view): raise mouse move and mouse leave events for features`
