# P11-17 — The "+" button in the gutter and the add-comment request

- **Status:** todo
- **Depends on:** P11-16
- **Size:** M

## Goal

When `CommentGutterButtonEnabled` is `true`, hovering a line's number area shows a small **+** button at that line; clicking it raises `AddCommentRequested(line)`; the host answers with `AddCommentThread(line, startComposing: true)`. The command `loom.commentThreads.add` does the same for the cursor line from the keyboard.

## Sources

Sources: none (specified in this card; ADR 0014 decision 2). Read first: `LoomEditor.Pointer.cs` (pointer move: how the hovered line / region is computed; the margin, line numbers and glyph margin are drawn by the renderer), `LoomEditor.HostHooks.cs` (`GetTopForLineNumber`, `LayoutInfo`), `src/LoomEditor.View/Layout/EditorLayoutInfo.cs` (`GlyphMarginLeft`, `GlyphMarginWidth`, `LineNumbersLeft`, `LineNumbersWidth`, `ContentLeft` — grep the real names), `Overlay/OverlayHost.cs`, the `HoverController` mouse-target logic in `LoomEditor.View/Hover/` (a `MouseTarget` concept may exist), `LoomEditor.Font.cs` (how a `DependencyProperty` is declared and validated), `Controls/Comments/CommentZoneHost.cs`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentGutterButton.cs` — create (a small `Button`-derived element or a `Button` wrapper, 22 × 22, "+" text, theme colors)
- `src/LoomEditor.WinUI/Controls/Comments/CommentGutterController.cs` — create
- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` — change: the `CommentGutterButtonEnabled` dependency property (`bool`, default `false`) and the wiring
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change: feed the pointer position / leave events to the controller (smallest hook)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-17"

## Design decisions (already made — do not change)

- **Property:** `public bool CommentGutterButtonEnabled { get; set; }` (DependencyProperty, default `false`; changing it to `false` hides the button). Declared like the other control properties (a `LoomEditor.CommentThreads.cs` partial, `PublicAPI.Unshipped.txt` line).
- **Which line:** the model line under the pointer when the pointer is in the **gutter** (`x < LayoutInfo.ContentLeft`, covering the glyph margin and the line numbers), computed through the same hit-test the pointer code uses (view line → model line via `CoordinatesConverter`; for a wrapped line the button sits at the **first** row of the model line). Lines past the end of the text show no button.
- **The button** is an element of the overlay layer (like the widgets), placed in the gutter at `(LayoutInfo.LineNumbersLeft … )` aligned with the line's first row, vertically centered on the line height, **inside** the gutter (it covers the line number while hovered, as on GitHub). It follows scrolling and is hidden when the pointer leaves the gutter **and** is not over the button itself (a short dispatcher delay is **not** used: track enter / leave of both).
- **Not shown** when: the property is `false`; the editor has no overlay; a thread already exists on that line (one box per line from the gutter; the host can still add more through the API); a drag selection by mouse is in progress; the editor is not laid out.
- **Click:** raises `CommentThreadsController.RaiseAddRequested(line)` (→ `AddCommentRequested`). The editor's caret and selection do not change (the press goes through `IsFromOverlay`). The button never adds a thread by itself (ADR 0014).
- **Command:** `loom.commentThreads.add` (P11-09) raises the same event for the cursor's line; nothing new is needed here except a manual check.
- **Accessibility:** the button's automation name is `"Add a comment on line {N}"` (add to `CommentStrings`), it is **not** a tab stop (pointer affordance; keyboard users use the command), and it is excluded from the Narrator focus order by `IsTabStop = false`.
- **Theme:** `ApplyTheme` with the widget colors (button background `button.background`, foreground `button.foreground`; fallback system accent).
- Performance: the controller does O(1) work per pointer move (one hit test), no allocation per move.

## Tests

- [ ] Automated: if the line-from-position computation is extracted as a pure helper in View (`GutterHitTest.TryGetLine(double x, double y, …)`), tests for it: `InGutter_ReturnsModelLine`, `InContent_ReturnsNone`, `PastLastLine_ReturnsNone`, `WrappedLine_ReturnsFirstRowLine`. The element code is manual.
- [ ] Manual (section "P11-17"): (1) property off: nothing appears; on: hovering the line numbers shows **+** on the hovered line only; (2) moving the pointer down the gutter moves the button without flicker; (3) scrolling with the wheel while hovering keeps the button on the right line; (4) clicking raises the event (Gallery: opens a composer on that line); the caret did not move; (5) a line that already has a thread shows no button; (6) word wrap on: the button is at the first row of a wrapped line; (7) leaving the gutter to the text hides the button; (8) the command (Gallery button "Add comment at caret") works from the keyboard.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `PublicAPI.Unshipped.txt` lists `CommentGutterButtonEnabled` and its dependency property field
- [ ] The manual section is written; the card result says it was not run by the agent
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drag from the button to select a range of lines (multi-line threads), a glyph drawn by the renderer, a default key for the command, showing existing threads as gutter icons.

## Suggested commit message

`feat(winui): add the gutter button that requests a new comment thread`
