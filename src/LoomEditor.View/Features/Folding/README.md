# Folding

**Role:** folds regions of the text. The ranges come from the indentation (and the `#region` markers of the language); the folded lines are hidden in the view model, and a region unfolds when a cursor ends up inside it.

**Commands and keys:** none yet (P5-89 adds them, P5-90 the gutter, P5-91 the mouse).

**Monaco sources:** `src/vs/editor/contrib/folding/browser/` (`folding.ts`, `foldingModel.ts`, `foldingRanges.ts`, `hiddenRangeModel.ts`, `foldingDecorations.ts`, `indentRangeProvider.ts`, `syntaxRangeProvider.ts` not ported).

**Files:**
- `FoldingFeature.cs` — the controller: computes the ranges after each edit (debounced), keeps the models, hides the folded lines (`ViewModel.SetHiddenAreas`), unfolds a region when a cursor is inside it. `EditorFoldingOptions.cs` — its options (VS Code's defaults, no public option in phase 5). `RangesLimitReporter.cs` — how many ranges were found and whether they were limited.
- `FoldingModel.cs`, `FoldingModelFunctions.cs`, `IDecorationProvider.cs` — the regions, their collapse state and decorations, and the level / jump functions.
- `FoldingRanges.cs`, `FoldingRegions.SanitizeAndMerge.cs`, `FoldingRegion.cs`, `SelectedLines.cs` — the packed region storage and its merge.
- `HiddenRangeModel.cs` — the hidden ranges of the collapsed regions. `FoldingDecorationProvider.cs`, `ShowFoldingControls.cs` — the decoration options.
- `IndentRangeProvider.cs`, `IndentRanges.cs`, `IRangeProvider.cs` — the indentation range provider.
- `FoldingCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/Folding/`.

**Known limits:** indentation strategy only (no syntax providers, no import folding); no view-state memento; the update delay is fixed (300 ms the first time, then 200 ms) instead of adapting to the measured compute time.
