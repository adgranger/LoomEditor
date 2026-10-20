# P5-88 — The `Folding` feature: the folding controller

- **Status:** todo
- **Depends on:** P5-87
- **Size:** M

## Goal

Folding works in an editor without any UI yet: the `FoldingFeature` (Monaco's `FoldingController`) computes the ranges
with the indentation provider after each edit (debounced), keeps the `FoldingModel` and the `HiddenRangeModel`, hides
the folded lines through `ViewModel.SetHiddenAreas`, moves the cursors out of hidden lines, and unfolds a region when a
cursor ends up inside it (typing, Find, Go to line…). The feature is listed in `EditorFeatures.cs`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `contrib/folding/browser/folding.ts`.

| Lines | What to port |
|---|---|
| 68–183 | `FoldingController`: fields, constructor (options `folding`, `showFoldingControls`, `foldingHighlight`, `foldingMaximumRegions`, `unfoldOnClickAfterEndOfLine`), `limitReporter` |
| 227–298 | `onModelChanged`, `onFoldingStrategyChanged`, `getRangeProvider` (indentation only), `getFoldingModel` |
| 299–394 | `onDidChangeModelContent`, `triggerFoldingModelChanged`, `onHiddenRangesChanges`, `onCursorPositionChanged`, `revealCursor` |
| 512–515 | `reveal` |
| 517–544 | `RangesLimitReporter` |
| `common/config/editorOptions.ts` 6383, 6399, 6406–6410, 6412, 6743–6745 | the defaults: `folding` true, `foldingHighlight` true, `foldingMaximumRegions` 5000 (10…65000), `unfoldOnClickAfterEndOfLine` false, `showFoldingControls` `mouseover` |
| `common/services/languageFeatureDebounce.ts` 130–150 | the update delay: `min = 200`, first value `min * 1.5` (see decisions) |

## Files to create or change

- `src/LoomEditor.View/Features/Folding/FoldingFeature.cs` — create (`Id = "loom.folding"`)
- `src/LoomEditor.View/Features/Folding/EditorFoldingOptions.cs` — create (internal record with Monaco's defaults)
- `src/LoomEditor.View/Features/Folding/RangesLimitReporter.cs` — create
- `src/LoomEditor.View/Features/Folding/README.md` — create (role, Monaco sources, files, tests, known limits)
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: `() => new FoldingFeature()`
- `tests/LoomEditor.View.Tests/Features/Folding/FoldingFeatureTests.cs` — create

## Design decisions (already made — do not change)

- `FoldingFeature.Attach(context)`: gets the `ViewModel` (`context.GetViewModel()`; nothing to do without one), the
  scheduler (`context.Scheduler`, step A) and `Model.LanguageConfigurationRegistry` (P5-15). It runs
  Monaco's `onModelChanged` once (LoomEditor has one model per editor; `SetText` is a flush, handled as a content change).
  `Dispose` runs the `localToDispose` part: cancel the schedulers, dispose the models (which removes the decorations and
  the hidden areas: `SetHiddenAreas([], this)`), unsubscribe.
- `EditorFoldingOptions` (ADR 0009, decision 12): `Folding = true`, `ShowFoldingControls = Mouseover`,
  `FoldingHighlight = true`, `FoldingMaximumRegions = 5000`, `UnfoldOnClickAfterEndOfLine = false`. Internal:
  `FoldingFeature.SetOptions(EditorFoldingOptions)` reproduces Monaco's `onDidChangeConfiguration` branches. No public
  property in phase 5. `foldingStrategy` is always `indentation`; `foldingImportsByDefault` and the `Imports` kind are
  not ported (no syntax provider before phase 8).
- No folding for `Model.IsTooLargeForTokenization` (Monaco: "huge files get no view model").
- **Timers** (ADR 0009, decision 10): `updateScheduler` is a `Delayer` (step A) whose delay is **300 ms** for the first
  computation (Monaco's `min * 1.5`) and **200 ms** afterwards. Monaco adapts the delay to the measured compute time
  (`languageFeatureDebounce.ts`, clamped to 200…40 000 ms); LoomEditor does not measure time: documented behavior
  difference. `cursorChangedScheduler` is a `RunOnceScheduler` of **200 ms**.
- The range provider is synchronous (P5-84), so `triggerFoldingModelChanged` computes the ranges in the `Delayer`
  callback and calls `foldingModel.Update(ranges, SelectedLines.From(selections))` directly; there is no promise and no
  cancellation. `GetFoldingModel()` returns the model **after running a pending update at once** (Monaco awaits the
  pending promise; commands of P5-89 and the mouse of P5-91 go through it).
- Events: `Model.ContentChanged` → `HiddenRangeModel.NotifyChangeModelContent` + trigger; the cursor change is the
  outgoing `CursorStateChangedEvent` of `ViewModel.EventDispatcher` (Monaco `onDidChangeCursorPosition`);
  `Model.LanguageChanged` and `Model.LanguageConfigurationChanged` (P5-15) → `onFoldingStrategyChanged`.
- `onHiddenRangesChanges`: adjust the model selections (`HiddenRangeModel.AdjustSelections`), set them with
  `viewModel.SetSelections("api", …)` when changed, then `viewModel.SetHiddenAreas(ranges, source: this)`.
- `reveal(position)` = `viewModel.RevealRange("api", true, Range.Collapsed(viewPosition), VerticalRevealType.CenterIfOutsideViewport, ScrollType.Smooth)`
  with the position converted to view coordinates (Monaco `revealPositionInCenterIfOutsideViewport`).
- **Find and folding:** no extra wiring. When Find selects a match inside a folded region, the cursor change runs
  `revealCursor` 200 ms later and unfolds it, as in Monaco.
- `RangesLimitReporter.Limit` reads `FoldingMaximumRegions`; `Changed` is a plain event (no status bar UI in LoomEditor).

## Tests

No Monaco test file covers `folding.ts`. New, with `TestCodeEditor` + `ManualEditorScheduler` (step A) and the feature
attached through an `EditorContext`-like helper (reuse whatever step A tests use to attach a feature):

- [ ] `Attach_ComputesRangesAfterTheFirstDelay` (nothing at 299 ms, regions at 300 ms)
- [ ] `Edit_RecomputesRangesAfter200Ms`
- [ ] `FoldRegion_HidesItsLinesInTheViewModel` (toggle the region at line 1 → `ViewModel.GetLineCount()` drops,
      `GetHiddenAreas()` is the region minus its first line)
- [ ] `FoldRegion_CursorInside_MovesToTheHeaderLine` (`AdjustSelections` path)
- [ ] `CursorMovedIntoHiddenLines_UnfoldsAfter200Ms` (`SetSelections` into a folded region: still folded at 199 ms,
      unfolded at 200 ms)
- [ ] `TypingAboveAFold_KeepsItFolded` (the decorations move with the edit)
- [ ] `FoldingDisabled_NoRangesNoHiddenAreas` (`SetOptions(Folding = false)` clears everything)
- [ ] `MoreRangesThanTheLimit_ReportsTheLimit` (`FoldingMaximumRegions = 10`)
- [ ] `Dispose_RemovesHiddenAreasAndDecorations`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: row `contrib/folding/browser/folding.ts` (🔶 controller, P5-88); behavior difference
      "folding update delay is fixed (300 ms, then 200 ms)"
- [ ] `Features/Folding/README.md` lists the Monaco sources and the known limits (indentation only, no view-state
      memento, no import folding, fixed delays)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Commands and keybindings (P5-89), gutter drawing (P5-90), mouse (P5-91).

## Suggested commit message

`feat(folding): add the folding feature`
