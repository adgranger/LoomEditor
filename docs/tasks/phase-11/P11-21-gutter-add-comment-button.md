# P11-21 — The "+" button in the gutter and the add-comment request

- **Status:** done
- **Depends on:** P11-20
- **Size:** M

## Goal

When `CommentGutterButtonEnabled` is `true`, hovering a line's number area shows a small **+** button at that line; clicking it raises `AddCommentRequested(line)`; the host answers with `AddCommentThread(line, startComposing: true)`. The command `loom.commentThreads.add` does the same for the cursor line from the keyboard.

## Sources

Sources: none (specified in this card; ADR 0014 decision 2). Read first: `LoomEditor.Pointer.cs` (`OnPointerMoved`, `OnPointerExited`, `SetMarginHovered` — the existing "pointer is over the gutter" signal that shows the folding chevrons — and `CreateMouseTarget`: reuse them, do not add a second hit test), `LoomEditor.HostHooks.cs` (`GetTopForLineNumber`, `LayoutInfo`), `src/LoomEditor.View/Layout/EditorLayoutInfo.cs` (`GlyphMarginLeft`, `GlyphMarginWidth`, `LineNumbersLeft`, `LineNumbersWidth`, `DecorationsLeft`, `ContentLeft`), `Overlay/OverlayHost.cs`, `LoomEditor.Font.cs` (how a `DependencyProperty` is declared and validated), `Controls/Comments/CommentZoneHost.cs`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentGutterButton.cs` — create (a small `Button`-derived element or a `Button` wrapper, 22 × 22, "+" text, theme colors)
- `src/LoomEditor.WinUI/Controls/Comments/CommentGutterController.cs` — create
- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` — change: the `CommentGutterButtonEnabled` dependency property (`bool`, default `false`) and the wiring
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: feed the pointer position / leave events to the controller (smallest hook)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-21"

## Design decisions (already made — do not change)

- **Property:** `public bool CommentGutterButtonEnabled { get; set; }` (DependencyProperty, default `false`; changing it to `false` hides the button). Declared like the other control properties (a `LoomEditor.CommentThreads.cs` partial, `PublicAPI.Unshipped.txt` line).
- **Which line:** the model line under the pointer when the pointer is in the **gutter** (`x < LayoutInfo.ContentLeft`, covering the glyph margin and the line numbers), computed through the same hit-test the pointer code uses (view line → model line via `CoordinatesConverter`; for a wrapped line the button sits at the **first** row of the model line). Lines past the end of the text show no button.
- **The button** is an element of the zone-widget layer (`PART_ZoneWidgetCanvas`, P11-12: below the hover and the find widget; `IsFromOverlay` already covers it), placed in the gutter at `(LayoutInfo.LineNumbersLeft … )` aligned with the line's first row, vertically centered on the line height, **inside** the gutter (it covers the line number while hovered, as on GitHub). It follows scrolling and is hidden when the pointer leaves the gutter **and** is not over the button itself (a short dispatcher delay is **not** used: track enter / leave of both).
- **Not shown** when: the property is `false`; the template has no `PART_ZoneWidgetCanvas`; a thread already exists on that line (one box per line from the gutter; the host can still add more through the API); a drag selection by mouse is in progress; the editor is not laid out.
- **Click:** calls `CommentThreadsController.RequestAdd(line)` (→ `AddCommentRequested`). The editor's caret and selection do not change (the press goes through `IsFromOverlay`). The button never adds a thread by itself (ADR 0014).
- **Command:** `loom.commentThreads.add` (P11-10) raises the same event for the cursor's line; nothing new is needed here except a manual check.
- **Accessibility:** the button's automation name is `"Add a comment on line {N}"` (add to `CommentStrings`), it is **not** a tab stop (pointer affordance; keyboard users use the command), and it is excluded from the Narrator focus order by `IsTabStop = false`.
- **Theme:** `ApplyTheme` with the widget colors (button background `button.background`, foreground `button.foreground`; fallback system accent).
- Performance: the controller does O(1) work per pointer move (one hit test), no allocation per move.

## Tests

- [x] Automated: if the line-from-position computation is extracted as a pure helper in View (`GutterHitTest.TryGetLine(double x, double y, …)`), tests for it: `InGutter_ReturnsModelLine`, `InContent_ReturnsNone`, `PastLastLine_ReturnsNone`, `WrappedLine_ReturnsFirstRowLine`. The element code is manual.
- [x] Manual (section "P11-21"): (1) property off: nothing appears; on: hovering the line numbers shows **+** on the hovered line only; (2) moving the pointer down the gutter moves the button without flicker; (3) scrolling with the wheel while hovering keeps the button on the right line; (4) clicking raises the event (Gallery: opens a composer on that line); the caret did not move; (5) a line that already has a thread shows no button; (6) word wrap on: the button is at the first row of a wrapped line; (7) leaving the gutter to the text hides the button; (8) the command (Gallery button "Add comment at caret") works from the keyboard.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` lists `CommentGutterButtonEnabled` and its dependency property field
- [x] The manual section is written; the card result says it was not run by the agent
- [x] Card status and `docs/tasks/README.md` updated

## Result

- `CommentGutterButton` (a `Button`, 22 × 22, "+", not a tab stop, automation name `CommentStrings.AddCommentOnLine`, which already existed)
  and `CommentGutterController` live in the zone-widget layer (the same `OverlayHost` as `CommentZoneHost`, so `IsFromOverlay` covers the button).
- The line comes from the `MouseTarget` that `OnPointerMoved` already computes for `EmitMouseMove` (no second hit test, no extra allocation per
  move): gutter target types, not `IsAfterLines`, model line of `Position` (a wrapped line's rows all map to it, so the button sits at its first
  row through `GetTopForLineNumber`). The thread lookup runs only when the hovered line changes. No pure helper was extracted, so no new tests.
- Scrolling under a still pointer: `CommentGutterController.Arrange()` runs after each draw (next to `CommentZoneHost.Arrange`) and hit-tests
  the last pointer position again while the pointer is over the gutter.
- Hidden on: property off, no template part, thread on the line, drag selection (the drag branch of `OnPointerMoved`), pointer leaving both
  the gutter and the button (`PointerEntered` / `PointerExited` of the button, `OnPointerExited` of the editor).
- Theme: `button.background` / `button.foreground` when the theme has both, else the application's `AccentButtonStyle`.
- Gallery: **Gutter + button** switch, **Add comment at caret** (runs the command), and the `AddCommentRequested` handler.
- `PublicAPI.Unshipped.txt`: the property and its dependency property field.
- The manual section "P11-21" is written; the agent did **not** run it.

## Out of scope

Drag from the button to select a range of lines (multi-line threads), a glyph drawn by the renderer, a default key for the command, showing existing threads as gutter icons.

## Suggested commit message

`feat(winui): add the gutter button that requests a new comment thread`
