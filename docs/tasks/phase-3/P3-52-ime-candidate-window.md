# P3-52 — IME: place the candidate window at the caret

- **Status:** done
- **Depends on:** P3-51
- **Size:** S

## Goal

The IME candidate window (the list of conversions) opens right below the text being composed, follows it when the
editor scrolls, and is at the right place at any display scale and after the window moves.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

`src/vs/editor/browser/controller/editContext/native/nativeEditContext.ts`:

| Lines | What |
|---|---|
| 289–304 | `onBeforeRender` / `prepareRender` / `render`: bounds are refreshed on every render |
| 539–577 | `_updateSelectionAndControlBoundsData`, `_updateSelectionAndControlBounds`: top = line vertical offset − scrollTop, left = contentLeft − scrollLeft (+ caret x for an empty selection) |
| 579–612 | `_updateCharacterBounds`: one rectangle per character of the requested range |

## Files to create or change

- `src/LoomEditor.WinUI/Input/ImeInput.cs` — change: answer `LayoutRequested`
- `src/LoomEditor.WinUI/Input/ScreenCoordinates.cs` — create: DIPs in the control → physical screen pixels
- `src/LoomEditor.WinUI/Interop/NativeMethods.cs` — create: `ClientToScreen` with `[LibraryImport]` (Native AOT)
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — change if needed: `TryGetHorizontalRange` (P3-40) is reused for the caret x
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: `NotifyLayoutChanged` on scroll, size, DPI and window moves

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first, and the IME spike findings in ADR 0006 (P3-48): they say whether
  Windows wants **screen pixels** (`LayoutBounds`) or **DIPs** (`LayoutBoundsVisualPixels`). Use what the spike found;
  the steps below are for screen pixels.
- `ScreenCoordinates.ToScreenPixels(UIElement element, Rect rectInElement)`:
  1. `element.TransformToVisual(null).TransformBounds(rect)` → DIPs in the window content;
  2. × `element.XamlRoot.RasterizationScale` → client pixels;
  3. + `ClientToScreen(hwnd, (0,0))`, with `hwnd = Win32Interop.GetWindowFromWindowId(element.XamlRoot.ContentIslandEnvironment.AppWindowId)`.
- `TextBounds`: the rectangle from the start of the requested range to its end on the primary selection's line
  (one line high; for a multi-line range, the first line, like Monaco's selection bounds); x from
  `TextViewRenderer.TryGetHorizontalRange`, y from `ViewLayout.GetVerticalOffsetForLineNumber - scrollTop`, then
  `+ contentLeft` and the control origin. `ControlBounds`: the text area of the control (content left to right edge).
- `NotifyLayoutChanged()` (queued, never inside a handler — P3-51) after: a scroll (`ViewLayout.Scroll`), a size change,
  a DPI change (`XamlRoot.Changed`, P2-20) and a window move (`AppWindow.Changed` with `DidPositionChange`).

## Tests

Manual (Windows 11, Gallery, Debug and AOT):

- [ ] Japanese IME: the candidate window opens under the composed text, at 100 % and 150 % display scale
- [ ] Scroll with the wheel during a composition: the window follows (or closes, as the IME decides) — no stale position
- [ ] Move the Gallery window during a composition, then continue: the candidate window is at the new place
- [ ] Two monitors with different scales: same checks on the second monitor

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the manual checks pass in the AOT app
- [x] `docs/PORTING.md`: map row `nativeEditContext.ts` (+ bounds, P3-52)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Per-character bounds for accessibility tools (phase 7).

## Suggested commit message

`feat(winui): place the IME candidate window at the caret`
