# P11-12 — The zone-widget layer: a WinUI element over each `Widget` zone, positioned in the draw pass

- **Status:** todo
- **Depends on:** P11-11
- **Size:** M
- **Recommended model:** Opus (first XAML element placed over a view zone in this code base; template part order, canvas coordinates, clipping and frame synchronization with the Win2D draw only show at runtime. On failure — the box drifts from its gap during scroll, covers the find widget or the gutter, or is not clipped at the edges — stop, do not commit, report what you saw, the numbers of the diagnostics line below at that moment, and what you tried.)

## Goal

For every thread a `CommentThreadWidget` placeholder (a bordered box with the title and the comments as plain text) sits exactly over its `Widget` zone, in a new layer that is above the text and **below** the find widget, the hover and the scroll bars; it moves with the text in the same frame, is clipped by the editor's bounds and is hidden when its zone is not in the viewport. The **height** stays the zone's fixed 120 px in this card (P11-13 makes it follow the content).

## Sources

Sources: none (specified in this card; ADR 0014 decision 4). **There is no existing code that places a XAML element over a zone** (`HideUnchangedRegionsController` and `DiffEditorViewZones` only create renderer-drawn zones): this card creates the pattern. Read first, completely:
- `src/LoomEditor.WinUI/Themes/Generic.xaml` lines 18–48 (the template: one `Grid` cell, children in z-order `PART_Canvas`, `PART_FindWidget`, `PART_OverlayCanvas`, the two `ScrollBar`s);
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` (`[TemplatePart]` attributes lines 38–42, `OnApplyTemplate` lines 147–160, `OnCanvasDraw` lines 266–277, the `_canvas?.Invalidate()` subscriptions lines 85–99);
- `src/LoomEditor.WinUI/Controls/Overlay/OverlayHost.cs` (reused as is for the new layer), `LoomEditor.Overlay.cs` (`InitializeOverlayWidgets`, `ApplyOverlayWidgetsTheme`);
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` (`IsFromOverlay` line 354 and its three call sites), `LoomEditor.HostHooks.cs`, `LoomEditor.Scrolling.cs` (`ScrollTop`);
- `src/LoomEditor.View/Layout/ViewLayout.cs` `GetWhitespaceViewportData()` (line ~364) and `ViewWhitespaceViewportData.cs` (`Id`, `AfterLineNumber`, `VerticalOffset`, `Height`); `EditorLayoutInfo.cs` (`ContentLeft`, `ContentWidth`, `Height`, `HorizontalScrollbarHeight`);
- `src/LoomEditor.WinUI/Controls/Hover/HoverWidget.Theme.cs` (how a widget takes its colors from `EditorTheme.GetColor`).

## Files to create or change

- `src/LoomEditor.WinUI/Themes/Generic.xaml` — change: `<Canvas x:Name="PART_ZoneWidgetCanvas" />` **between** `PART_Canvas` and `PART_FindWidget`
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: `[TemplatePart(Name = ZoneWidgetCanvasPartName, Type = typeof(Canvas))]`, the constant, `OnApplyTemplate` passes the part to the comments partial; `OnCanvasDraw` calls the host's `Arrange()` right after `_renderer.Render(...)`
- `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs` — change: `internal IReadOnlyList<ViewWhitespaceViewportData> GetWhitespaceViewportData() => _viewModel.ViewLayout.GetWhitespaceViewportData();`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: `IsFromOverlay` also returns `true` for the zone-widget layer
- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` — change: create / dispose the host when the template part appears / the template is re-applied
- `src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.cs` — create
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs` — create (placeholder)
- `samples/LoomEditor.Gallery/FeatureSamples.cs` (and the Gallery button area it uses) — change: the **temporary** sample "Comment threads (P11-12)" (see Tests)
- `docs/testing/phase-11-manual-tests.md` — create (the header of `phase-10-manual-tests.md`, then the section "P11-12")

## Design decisions (already made — do not change)

- **Layer:** the new part is optional (no part → no widgets, threads still reserve their gap). `CommentZoneHost` gets an `OverlayHost` over that canvas. The canvas is clipped to its bounds: `canvas.Clip = new RectangleGeometry { Rect = new Rect(0, 0, w, h) }` updated on `SizeChanged` (a `Canvas` never clips its children; without this a box scrolled half out draws over the page around the editor).
- `internal sealed class CommentZoneHost : IDisposable`, ctor `(LoomEditor editor, OverlayHost layer, CommentThreadsController controller)`: one `CommentThreadWidget` per thread, created on `ThreadAdded` (and for the threads that exist when the host is created), removed on `ThreadRemoving`. Elements are kept in a `Dictionary<string /*zone id*/, CommentThreadWidget>`.
- **`Arrange()`** (called from `OnCanvasDraw`, after the text is drawn, so text and boxes move in the same frame; a scroll already invalidates the canvas): reads `editor.GetWhitespaceViewportData()` once; for each entry whose `Id` is a widget zone: `Left = LayoutInfo.ContentLeft + 4`, `Top = VerticalOffset − ScrollTop`, `Width = LayoutInfo.ContentWidth − 8`, height = the zone's `Height`; shows it with `OverlayHost.Show(element, rect)` (which also sets `MaxWidth` / `MaxHeight`). Every widget **not** in the list, or with `Height < 1` (hidden area), is collapsed. **No allocation per frame** beyond the list the view layout returns: reuse a `HashSet<string>` field for "seen this frame".
- `Arrange()` only writes `Canvas.Left` / `Top` / sizes when a value changed (avoid a XAML layout per frame when nothing moves).
- The text-area origin is the canvas origin (both fill the same `Grid` cell with no margin; verify in the template and **write the check into the manual test**: a box under line 1 at scroll 0 starts exactly at the bottom of line 1).
- **Pointer:** `IsFromOverlay` covers the new layer, so a press, move or release on a box never reaches the text logic (caret, selection, drag). The **wheel** is not filtered: `OnPointerWheelChanged` scrolls the editor for events that bubble from a box (verify; no forwarding code).
- **Placeholder widget** (`internal sealed class CommentThreadWidget : Border`): ctor `(CommentThreadViewModel viewModel)`; a `StackPanel` with the title (`TextBlock`, semi-bold) and one `TextBlock` per comment (`"{Author.Name}: {Body}"`, wrapping); `BorderThickness = 1`, `CornerRadius = 4`, `Padding = 8`; `ApplyTheme(EditorTheme)` sets background `editorWidget.background`, border `editorWidget.border`, text `editor.foreground` (fallbacks: the hover's colors); rebuilds on `viewModel.Changed`. P11-16 replaces the body and keeps the class name, the constructor and `ApplyTheme`. `ApplyOverlayWidgetsTheme` calls the host's `ApplyTheme`.
- **Diagnostics for the manual test** (internal, used by the Gallery only): `CommentZoneHost.Diagnostics` returns a short string `"zones {n} visible {v} lastArrange {ms:0.00} ms"` (`Stopwatch` around `Arrange`). No logging.
- No `x:Bind`, no `{Binding}`, no reflection: elements are built in code.

## Tests

- [ ] Automated: none for WinUI (no WinUI test project). The build proves the template part compiles.
- [ ] Manual (section "P11-12"). The card adds the **temporary** Gallery sample "Comment threads (P11-12)" in `FeatureSamples.cs`: 200 numbered lines, buttons **Add thread at caret** (`AddCommentThread(caretLine)`), **Add 3 threads (lines 5, 6, 40)**, **Add 200 threads**, **Remove all**, and a `TextBlock` that shows `CommentZoneHost.Diagnostics` refreshed every 500 ms. P11-23 replaces this sample. Checks, each with a measurable expectation:
  1. a thread on line 5: the box's top edge touches the bottom of line 5 and line 6 starts 120 px lower (use the Gallery's zoom 100 %, compare with a screenshot ruler or the line numbers);
  2. scroll by wheel and by dragging the scroll bar for 10 s: the box never separates from line 5 by more than 1 px in any screenshot taken (Win+Shift+S) during the scroll;
  3. scroll line 5 above the top edge: the box is cut at the editor's top edge, nothing is drawn above the editor; the same at the bottom edge and above the horizontal scroll bar;
  4. open the find widget (`Ctrl+F`) and scroll a box under it: the find widget stays on top; a hover popup and the completion list are drawn above a box;
  5. insert a line above line 5: the three boxes move down one line;
  6. resize the window and zoom (`Ctrl` + wheel): the box keeps `ContentLeft + 4` and the content width (it never covers the line numbers, the minimap or the vertical scroll bar);
  7. word wrap on with a long line 5: the box sits under the **last** wrapped row of line 5;
  8. a click and a drag on a box do not move the caret or select text; the wheel over a box scrolls the editor;
  9. 200 threads: scrolling stays smooth; `lastArrange` stays under 1 ms on the test machine (write the measured value in the Result column);
  10. collapse a fold that contains a thread's line: its box disappears; expand: it returns.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The template has the new part between `PART_Canvas` and `PART_FindWidget`, and `IsFromOverlay` covers it
- [ ] The manual section is written with the ten checks; the card result says the agent did **not** run them
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Height that follows the content (P11-13), focus and keys (P11-14), real content (P11-16 …), the diff editor (P11-22), animations.

## Suggested commit message

`feat(winui): place comment widgets over their view zones in a dedicated layer`
