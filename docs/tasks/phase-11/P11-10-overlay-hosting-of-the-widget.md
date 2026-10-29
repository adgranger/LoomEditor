# P11-10 — Hosting: a WinUI element over each `Widget` zone, repositioned on scroll, height follows content

- **Status:** todo
- **Depends on:** P11-09
- **Size:** M
- **Recommended model:** Opus (a view zone and a XAML element have to agree on position and height through a layout feedback loop; wrong coordinates, a clipping error or an endless re-layout appear only at runtime and cannot be diagnosed from this card. On failure — the widget drifts from its gap, flickers, never settles, or covers the gutter — stop, do not commit, report what you observed, the zone top / the canvas offset / the measured heights at that moment, and what you tried.)

## Goal

For every thread a `CommentThreadWidget` element sits in `PART_OverlayCanvas` exactly over the thread's `Widget` zone: it follows the scroll, the resize, the zoom and the zone's moves, is clipped by the editor, is hidden when its zone is off screen, and sets the zone height to its own measured height. The widget is a **placeholder** (a bordered box with the title and the comments' text); the real content comes with P11-13 … P11-15.

## Sources

Sources: none (specified in this card; ADR 0014 decision 4). Concepts kept from Monaco: view zone, content widget. Read first, completely:
`src/LoomEditor.WinUI/Controls/Overlay/OverlayHost.cs`, `LoomEditor.Overlay.cs`, `LoomEditor.HostHooks.cs` (zone and scroll hooks, `GetWhitespaces`, `GetTopForLineNumber`, `HostScrollChanged`, `HostLayoutChanged`), `LoomEditor.Scrolling.cs`, `LoomEditor.Pointer.cs` (`IsFromOverlay`, wheel handling), `src/LoomEditor.WinUI/Controls/DiffEditor/HideUnchangedRegionsController.cs` and `DiffEditorViewZones.cs` (they already place elements by zone and keep a pass limit: copy the pattern), `Hover/HoverPresenter.cs` (coordinates of the overlay canvas vs the control), the control template (`Themes/Generic.xaml`: where `PART_OverlayCanvas` sits relative to the text area, the scroll bars and the minimap), and `src/LoomEditor.View/Layout/ViewLayout*.cs` (how to get the vertical offset of a whitespace by id: `GetVerticalOffsetForWhitespaceIndex` / whitespace viewport data — grep). If no accessor gives a zone's top, add the **smallest** internal one to `ViewLayout` / `LoomEditor.HostHooks.cs`, with a View test.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.cs`, `CommentZoneHost.Layout.cs` — create
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs` — create (placeholder, see below)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Overlay.cs` — change: create / dispose the host with the overlay widgets
- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` — change: connect the controller (P11-09) to the host
- `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs` — change only if an accessor is missing
- `docs/testing/phase-11-manual-tests.md` — create (header like phase 10's, then the section "P11-10")

## Design decisions (already made — do not change)

- `internal sealed class CommentZoneHost : IDisposable`, ctor `(LoomEditor editor, OverlayHost overlay, CommentThreadsController controller)`; one widget per thread: created on `controller` thread-added, destroyed on removed; **no** widget when the editor has no overlay.
- **Position** (in overlay canvas coordinates): `Left = LayoutInfo.ContentLeft + 4`, `Width = LayoutInfo.ContentWidth − 8` (the content width excludes the minimap and the vertical scroll bar; **verify** with the template and report the arithmetic if the template differs), `Top = zoneTopInScroll − ScrollTop + textAreaTopInCanvas` (the last term is 0 when the text area starts at the top of the canvas; measure it, do not assume). The widget does **not** follow the horizontal scroll (ADR 0014).
- **Height:** the widget is measured with `Measure(new Size(width, double.PositiveInfinity))`; `Math.Ceiling(DesiredSize.Height)` is the target. If it differs from `controller.Manager.GetHeight(thread)` call `SetHeight`. The zone's change raises `ZonesChanged` → the host repositions: the loop ends because a pass that finds equal heights does nothing. **Pass limit 3 per dispatcher tick** (log once to the same diagnostic channel `DiffEditorViewZones` uses when exceeded, and stop). Re-measure on: widget `SizeChanged`, width change, thread / view-model `Changed`.
- **Triggers:** `HostScrollChanged` (reposition only, no measure), `HostLayoutChanged` and `ViewZones.ZonesChanged` (reposition + height), editor font / zoom change (arrive as layout changes), `ThreadMoved`. Coalesce layout-driven work into one dispatcher tick (same helper as `DiffEditorViewZones`).
- **Visibility:** a widget whose `[top, top + height]` does not intersect `[0, viewportHeight]` is `Collapsed` (not removed); a zone with computed height 0 (hidden area) hides the widget.
- **Clipping:** the part of a widget above the top edge or below the bottom edge (or over the horizontal scroll bar area) is clipped with `UIElement.Clip` (a `RectangleGeometry` in the element's coordinates, updated on every reposition). The overlay canvas is shared with the hover and the completion list: do **not** clip the canvas.
- **Z order:** the widget is added **below** the hover popup and the completion list (insert at the start of the canvas children, or set `Canvas.ZIndex` lower — check what `OverlayHost.Show` does and keep the hover on top).
- **Wheel:** a wheel turn over the widget must scroll the editor. Check whether the control already receives the bubbled `PointerWheelChanged` from the canvas children; if not, forward it from the widget (`editor.ScrollBy…` — grep `LoomEditor.Scrolling.cs`). A widget that can scroll itself (the multi-line text box of P11-14) keeps the wheel when it can still scroll in that direction (use `ScrollViewer` handled flags; for the placeholder nothing to do, but the hook point is a method `ShouldForwardWheel(PointerRoutedEventArgs)` on the widget that returns `true` here).
- **Pointer:** a press on a widget never reaches the text logic (`IsFromOverlay` already guarantees it: verify and add a line to the manual test).
- **Placeholder widget** (`CommentThreadWidget : Border`, internal): a `StackPanel` with the thread title (`TextBlock`, from the view model) and one `TextBlock` per comment (`Author: Body` plain text), a 1 px border, 4 px corner radius, 8 px padding, background and border from `EditorTheme` colors the way `HoverWidget.Theme.cs` does (a `ApplyTheme(EditorTheme)` method called by `ApplyOverlayWidgetsTheme`). It subscribes to the thread's `Changed` and rebuilds. P11-13 replaces the body, **keeping** the class name, the constructor `(CommentThreadViewModel, …)` and `ApplyTheme`.
- Dispose: unsubscribes everything, removes the elements from the canvas.
- No reflection, no `x:Bind` on view models (code only, as the other widgets).

## Tests

- [ ] Automated: none for the WinUI classes (no WinUI test project). If an accessor was added to `ViewLayout`, a View test for it.
- [ ] Manual (section "P11-10", Gallery **with a temporary sample** the card adds in `FeatureSamples.cs` ("Comment threads (P11-10)": 40 lines and a menu button "Add thread at caret" calling `Editor.AddCommentThread(line, startComposing: false)`; P11-19 replaces it): (1) the box appears under the line and pushes the text down by its height; (2) scrolling up and down keeps it glued to the line, no lag, no flicker; (3) it disappears behind the top edge cleanly (clipped, not overlapping the line numbers or the horizontal scroll bar); (4) three threads, then insert a line above the first: all three follow; (5) resize the window and change the zoom (`Ctrl` + wheel): the width and the position follow; (6) turn word wrap on: the box stays under the **last wrapped row** of its line; (7) wheel over the box scrolls the editor; (8) a click on the box does not move the caret; (9) 200 threads (a button) scroll smoothly; (10) hover popup and completion list are drawn above the box.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The manual section is written; the card result states which of the ten checks the agent could **not** run (the agent does not run the Gallery)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Real widget content (P11-13 …), focus and keys (P11-11), accessibility (P11-16), the diff editor (P11-18), animating the height change.

## Suggested commit message

`feat(winui): host comment thread widgets over their view zones in the overlay layer`
