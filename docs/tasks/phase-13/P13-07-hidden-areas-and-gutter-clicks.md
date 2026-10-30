# P13-07 — `SetHiddenAreas` and the `GutterClicked` event

- **Status:** done
- **Depends on:** P13-06
- **Size:** S

**Recommended model:** Sonnet

## Goal

For a unified diff with staging, a host hides the unchanged lines it does not want to show (`SetHiddenAreas`) and ticks a line when the user clicks its gutter
(`GutterClicked`, with `Handled` to keep the editor from selecting the line).

## Sources

Sources: none (specified in this card; [phase-13/README.md](README.md) decisions 6, 14 and 16). Read first: `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs`
(internal `SetHiddenAreas(ranges, source)`), `src/LoomEditor.View/Layout/ViewModel.HiddenAreas.cs` (sources merged by `HiddenAreasModel`),
`tests/LoomEditor.View.Tests/Layout/ViewModelHiddenAreasTests.cs`, `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` (`OnPointerPressed`: the target, `targetIsGutter`,
`_mouseDown.Start`, `EmitMouseDown`), `src/LoomEditor.View/Controller/MouseTarget.cs` and `MouseTargetFactory.cs` (is `Position` a model position? check; Monaco's
`IMouseTarget.position` is), `src/LoomEditor.WinUI/Controls/Comments/CommentEventArgs.cs` (shape of the control's event args).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.HiddenAreas.cs` — create
- `src/LoomEditor.WinUI/Controls/GutterClickedEventArgs.cs` — create (the args and the `GutterArea` enum)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` — change
- `tests/LoomEditor.View.Tests/Layout/ViewModelHiddenAreasTests.cs` — change (one test, only if no test already covers two sources)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change
- `docs/testing/phase-13-manual-tests.md` — change (section "P13-07")

## Design decisions (already made — do not change)

- **`SetHiddenAreas(IReadOnlyList<Range> ranges)`** (public, new file `LoomEditor.HiddenAreas.cs`): `ArgumentNullException` for null; each range goes through
  `Model.ValidateRange` (clamped, never rejected); then `_viewModel.SetHiddenAreas(validated, _hostHiddenAreasSource)` with `private readonly object _hostHiddenAreasSource =
  new();`. XML doc: "Hides whole lines: each range hides its lines `StartLineNumber` … `EndLineNumber` (Monaco: `setHiddenAreas`). Replaces the ranges of the previous call;
  an empty list shows them again. Folding keeps its own hidden ranges. On a side of a `LoomDiffEditor`, 'hide unchanged regions' also hides lines: combining the two is not
  supported." The internal overload `(ranges, source)` stays as it is.
- **`GutterArea`** (`public enum`): `GlyphMargin`, `LineNumbers`, `LineDecorations` (XML doc on each; "the part of the gutter that was clicked").
- **`GutterClickedEventArgs`** (`public sealed class : EventArgs`, internal constructor `(int lineNumber, GutterArea area)`): `int LineNumber { get; }` (1-based model line),
  `GutterArea Area { get; }`, `bool Handled { get; set; }` ("set to `true` so the editor does nothing else with the press: no line selection, no fold toggle"). Threading: UI
  thread only.
- **`public event EventHandler<GutterClickedEventArgs>? GutterClicked;`** XML doc per README decision 14. In `OnPointerPressed`, right after `target` and `targetIsGutter` are
  computed and **before** the `_mouseDown.Start` branch: if `targetIsGutter && mouseEvent.LeftButton && !target.IsAfterLines && target.Position is { } position`, raise it with
  the model line (`position.LineNumber`, converted from view to model coordinates **only if** your reading of `MouseTargetFactory` shows it is a view position) and the area
  mapped from `target.Type`; if `args.Handled`: `e.Handled = true; return;` (the press does not reach `_mouseDown` nor `EmitMouseDown`). Clicks on the comment "+" button
  never get here (overlay element).
- `PublicAPI.Unshipped.txt` (WinUI) gains exactly: `SetHiddenAreas`, `GutterClicked`, the enum and its three members, the args class and its four accessors
  (`LineNumber.get`, `Area.get`, `Handled.get`, `Handled.set`).
- **Manual section "P13-07"**: (1) **Hide unchanged** (Gallery, P13-09) hides lines 20–40 (line 19 is followed by line 41; the blame column has no rows for them); **Show all**
  brings them back; (2) folding a region while host areas are set, then **Show all**: the folded region stays folded; (3) a left click on the line decorations of line 12 in
  the Gallery's staging demo toggles its "staged" tint and does **not** move the caret; (4) a click on the line number of line 12 (handler does not set `Handled`) selects
  the line as before and the log shows `GutterClicked line 12 LineNumbers`; (5) a right click raises nothing; a click below the last line raises nothing; (6) the folding
  chevron still folds when the handler does not set `Handled`.

## Tests

- [x] New (`ViewModelHiddenAreasTests`, only if missing): two sources hide two ranges; clearing one source shows its lines and keeps the other's.
- [x] Manual: section "P13-07" written (not run by the agent).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` (WinUI) gains exactly the lines above
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`GetHiddenAreas`, a hover or right-click event for the gutter, gutter icons drawn by the host (use a decoration class with a background), glyph-margin lanes, events on
`LoomDiffEditor`.

## Suggested commit message

`feat(winui): host hidden areas and a gutter click event`

## Result

Build 0 warnings, all tests green. No new View test: `SetHiddenAreas_FromOverlappingSources_ProducesAValidMergedResult` already covers two sources. `MouseTarget.Position` is a view position, so the line is converted to a model line. The manual section "P13-07" is written and was not run by the agent.
