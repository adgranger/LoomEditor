# P13-08 — Host view zones: `AddViewZone` / `RemoveViewZone` with WinUI content

- **Status:** todo
- **Depends on:** P13-07
- **Size:** M

**Recommended model:** Sonnet

## Goal

A host opens a gap after a line and puts its own WinUI element in it — a conflict header with **Accept current / Accept incoming** buttons, a hunk header with **Stage hunk** —
through two members: `string AddViewZone(int afterLineNumber, double height, FrameworkElement? content = null)` and `bool RemoveViewZone(string id)`. The element scrolls with
the text and is placed by the zone-widget layer built for comment threads in phase 11.

## Sources

Sources: none (specified in this card; [phase-13/README.md](README.md) decisions 6 and 15). Read first: `src/LoomEditor.View/Layout/ViewZone.cs`, `ViewZones.cs`,
`IViewZoneChangeAccessor.cs`; `src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.cs` (`Arrange`: the loop over `GetWhitespaceViewportData`, `_layer.Show`, `Hide`) and
`CommentZoneHost.Height.cs` (`BoxWidth`); `src/LoomEditor.WinUI/Controls/Overlay/OverlayHost.cs`; `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs`
(`InitializeCommentZoneHost`: the `PART_ZoneWidgetCanvas` layer and its clip); `src/LoomEditor.WinUI/Controls/LoomEditor.cs` (`OnCanvasDraw`);
`src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` (`IsFromOverlay`); `src/LoomEditor.WinUI/Controls/DiffEditor/DiffEditorViewZones.cs` (does the diff alignment look at
other zones?); `docs/tasks/phase-11/P11-14-focus-and-key-routing.md` (the focus check list (a)–(f) and its Result section).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/ViewZones/HostViewZoneLayer.cs` — create
- `src/LoomEditor.WinUI/Controls/LoomEditor.ViewZones.cs` — create
- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs`, `LoomEditor.cs`, `LoomEditor.Pointer.cs` — change (attach, arrange, pointer)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change
- `docs/testing/phase-13-manual-tests.md` — change (section "P13-08")

## Design decisions (already made — do not change)

- **`AddViewZone(int afterLineNumber, double height, FrameworkElement? content = null)`** → the zone id. `ArgumentOutOfRangeException` when `afterLineNumber` is outside
  `0 … Model.LineCount` (0 = before the first line) or `height` is not finite or `<= 0`. It adds, in one `_viewModel.ViewZones.Change(accessor => …)`, a
  `ViewZone { AfterLineNumber = afterLineNumber, HeightInPx = (int)Math.Ceiling(height), Kind = ViewZoneKind.Widget, SuppressMouseDown = true }`, keeps the id in a
  `HashSet<string> _hostZoneIds`, then, with content, `_hostZoneLayer.Add(id, content)` and `InvalidateCanvas()`. XML doc: Monaco `changeViewZones` + `addZone`; the zone
  follows edits like any view zone; the content is sized to the zone (width of the text area, height of the zone) and receives its own pointer and keyboard input; without the
  template part `PART_ZoneWidgetCanvas` the zone is an empty gap; the theme of the content is the host's job; on a side of a `LoomDiffEditor` write what your reading of
  `DiffEditorViewZones` shows (if the alignment ignores other zones: "the two sides are not realigned around a host zone").
- **`RemoveViewZone(string id)`** → `false` when `id` was not returned by `AddViewZone` (comment and diff zones cannot be removed through it) or was already removed.
  `ArgumentNullException` for null. If the focused element (`FocusManager.GetFocusedElement(XamlRoot)`) is inside the zone's content, `Focus(FocusState.Programmatic)`
  first (P11-14 rule: before the element leaves the tree). Then remove the element from the layer and the zone from `ViewZones`.
- **`HostViewZoneLayer`** (internal sealed, `Controls/ViewZones/`, threading: UI thread only), constructed with the editor in the `LoomEditor` constructor (field
  `_hostZoneLayer`), so zones and content survive a template re-apply:
  - `Attach(OverlayHost? layer)`: called by `InitializeCommentZoneHost` with the same `OverlayHost` it creates (or `null` when there is no part); moves every element to the
    new layer (`Remove` from the old one first);
  - `Add(id, element)` (`element.Visibility = Collapsed` until the next arrange), `Remove(id)`, `Contains(DependencyObject?)` (`_layer?.Contains(...)` limited to its own
    elements: walk up the parents to one of them);
  - `Arrange()`: the loop of `CommentZoneHost.Arrange` without the dirty / height logic: for each `ViewWhitespaceViewportData` whose id has an element and height ≥ 1, show it at
    `left = layout.ContentLeft`, `top = zone.VerticalOffset - editor.ScrollTop`, `width = Math.Max(0, layout.ContentWidth)`, `height = zone.Height` (set `Width` / `Height`
    too); collapse the others. Called in `OnCanvasDraw` right after `_commentZoneHost?.Arrange()`.
- `IsFromOverlay` also returns `true` for `_hostZoneLayer.Contains(source)` (pointer presses on the content never reach the text logic).
- **No new focus code.** The editor's keyboard and IME paths already ignore input when the control itself does not have the focus (P11-14); the manual checks below verify it
  for a host `TextBox`. If a check fails, stop and report (the fix would be shared with comment widgets).
- `PublicAPI.Unshipped.txt` (WinUI) gains exactly the two methods.
- **Manual section "P13-08"**: (1) **Add conflict header** (Gallery, P13-09) after line 9: a 1-line-high bar with two buttons appears between lines 9 and 10, the text below
  moves down by one line height; (2) it scrolls with the text and is clipped at the top / bottom edges; (3) clicking **Accept current** runs the host handler (log) and does not
  move the caret; (4) typing a new line above line 9 moves the bar down with its line; (5) **Remove** removes it and the text closes the gap; `RemoveViewZone` a second time
  returns `false` (log); (6) a zone with a `TextBox`: typing in it inserts nothing in the editor, `Tab` leaves the box by XAML focus navigation, the caret stops blinking — the
  P11-14 checks (a)–(f), one line each; (7) **Remove** while the `TextBox` has the focus → the editor has the focus; (8) a zone after line 0 shows above line 1; (9) a comment
  thread's box and a host zone after the same line both show, without overlap.

## Tests

- [ ] None automated (no WinUI test project; `ViewZones` itself is covered by `ViewZonesTests`).
- [ ] Manual: section "P13-08" written (not run by the agent).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `PublicAPI.Unshipped.txt` (WinUI) gains exactly the two lines; `ViewZone`, `ViewZones` and `ViewZonePointerEventArgs` stay internal
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Changing a zone's line or height in place (remove and add), a height that follows the content, zones painted by the renderer (`DeletedLines`, `Collapsed`), a public
`ViewZonePointer` event, `AfterColumn`, `Ordinal`, `MinWidthInPx`, realigning a `LoomDiffEditor` around host zones.

## Suggested commit message

`feat(winui): host view zones with WinUI content`
