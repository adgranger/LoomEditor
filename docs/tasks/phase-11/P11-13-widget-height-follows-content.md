# P11-13 — The zone height follows the widget's content

- **Status:** todo
- **Depends on:** P11-12
- **Size:** S
- **Recommended model:** Opus (a two-way feedback loop between a XAML measure and the editor layout; a mistake shows as a box that never settles, jitters by one pixel, or a gap that stays at the old height. On failure — the diagnostics show the height passes climbing, the gap and the box disagree, or the layout flickers — stop, do not commit, report the diagnostics line and the sequence of heights you saw, and what you tried.)

## Goal

When a box grows or shrinks (a comment added, a long title, a resize that re-wraps its text, later the composer's text box), its zone takes exactly the box's height, the text below moves, and the loop settles in one pass.

## Sources

Sources: none (specified in this card; ADR 0014 decision 4). Read first: `CommentZoneHost.cs` and `CommentThreadWidget.cs` (P11-12), `CommentThreadManager.SetHeight` / `GetHeight` (P11-09), `src/LoomEditor.WinUI/Controls/DiffEditor/DiffEditorViewZones.cs` (`MaxPasses`, the "skip a pass that changes nothing" rule and how it schedules work on the `DispatcherQueue`: **copy that scheduling**), `OverlayHost.Measure`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.cs` (or a new `CommentZoneHost.Height.cs` if the file passes ~250 lines) — change
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs` — change only if a hook is missing
- `samples/LoomEditor.Gallery/FeatureSamples.cs` — change: the temporary sample gets a button **Add a comment to the first thread** (a three-paragraph body) and **Toggle a long title**
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-13"

## Design decisions (already made — do not change)

- **When to measure:** a widget's `SizeChanged`, a change of the available width (`LayoutInfo.ContentWidth` in `Arrange`), the view model's `Changed`, a theme or font change. Each trigger only marks the widget dirty and schedules **one** coalesced pass on the dispatcher (as `DiffEditorViewZones` does); never measure inside `Arrange()` (it runs in the draw).
- **The pass:** for each dirty widget, `OverlayHost.Measure` is **not** used (it lifts `MaxWidth` to infinity): call `element.Measure(new Size(width, double.PositiveInfinity))` with the width `Arrange` uses; target = `(int)Math.Ceiling(element.DesiredSize.Height)`; when it differs from `Manager.GetHeight(thread)` call `Manager.SetHeight(thread, target)`. `SetHeight` already skips equal values (P11-09).
- **Loop guard:** at most 3 passes per dispatcher tick; when a fourth would be needed, stop and record it in the diagnostics (`heightPasses`, `heightLimitHits` counters added to `CommentZoneHost.Diagnostics`). A pass that changes no height ends the loop.
- **No sub-pixel jitter:** heights are integers (the zone is in px); the element is shown with `MaxHeight = zone height` by `Arrange`, so a box that is 0.4 px taller than its zone is cut by at most 1 px; this is the accepted rounding.
- **Minimum height:** none in this card; an empty thread is as tall as its title row.
- The zone keeps its last height while a widget is collapsed (off screen): no measure for collapsed widgets.

## Tests

- [ ] Automated: none for WinUI. If the "which widgets need a pass" bookkeeping is pure, put it in a small View helper with tests (`HeightPassScheduler_MarksAndClears`, `..._LimitStopsAtThree`); otherwise none.
- [ ] Manual (section "P11-13"; temporary sample): (1) **Add a comment to the first thread**: the box and its gap grow together, the line below the box stays touching its bottom edge (no gap, no overlap: 0 px in a screenshot); (2) the diagnostics show `heightPasses` increased by 1 or 2 and `heightLimitHits` = 0; (3) narrow the window so the comment wraps on more rows: the gap follows within one frame after the resize stops; (4) **Toggle a long title** repeatedly: no flicker, `heightLimitHits` stays 0; (5) 200 threads, resize the window: no freeze longer than a visible hitch, `heightLimitHits` = 0; (6) zoom in and out: heights follow the font size.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The manual section is written with the six checks; the card result says the agent did **not** run them
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Animated height changes, a maximum height for a thread (a very long thread makes a tall gap: accepted), the composer (P11-17).

## Suggested commit message

`feat(winui): make the comment zone height follow the widget content`
