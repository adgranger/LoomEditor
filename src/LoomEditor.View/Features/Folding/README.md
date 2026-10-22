# Folding

**Role:** folds regions of the text. The ranges come from the indentation (and the `#region` markers of the language); the folded lines are hidden in the view model, and a region unfolds when a cursor ends up inside it.

**Commands and keys (Windows):** all need the editor text focus; they do nothing when folding is off.

| Command | Keys |
|---|---|
| `editor.fold` (fold the region at the cursor, or its first unfolded parent) | `Ctrl+Shift+[` |
| `editor.unfold` | `Ctrl+Shift+]` |
| `editor.toggleFold` | `Ctrl+K Ctrl+L` |
| `editor.foldRecursively` / `editor.unfoldRecursively` | `Ctrl+K Ctrl+[` / `Ctrl+K Ctrl+]` |
| `editor.toggleFoldRecursively` | `Ctrl+K Ctrl+Shift+L` |
| `editor.foldAll` / `editor.unfoldAll` | `Ctrl+K Ctrl+0` / `Ctrl+K Ctrl+J` |
| `editor.foldLevel1` … `editor.foldLevel7` | `Ctrl+K Ctrl+1` … `Ctrl+K Ctrl+7` |
| `editor.foldAllExcept` / `editor.unfoldAllExcept` | `Ctrl+K Ctrl+-` / `Ctrl+K Ctrl+=` |
| `editor.foldAllMarkerRegions` / `editor.unfoldAllMarkerRegions` | `Ctrl+K Ctrl+8` / `Ctrl+K Ctrl+9` |
| `editor.foldAllBlockComments` | `Ctrl+K Ctrl+/` |
| `editor.createFoldingRangeFromSelection` / `editor.removeManualFoldingRanges` | `Ctrl+K Ctrl+,` / `Ctrl+K Ctrl+.` |
| `editor.gotoParentFold`, `editor.gotoPreviousFold`, `editor.gotoNextFold` | none |

**Monaco sources:** `src/vs/editor/contrib/folding/browser/` (`folding.ts`, `foldingModel.ts`, `foldingRanges.ts`, `hiddenRangeModel.ts`, `foldingDecorations.ts`, `indentRangeProvider.ts`, `syntaxRangeProvider.ts` not ported).

**Files:**
- `FoldingFeature.cs` — the controller: computes the ranges after each edit (debounced), keeps the models, hides the folded lines (`ViewModel.SetHiddenAreas`), unfolds a region when a cursor is inside it. `EditorFoldingOptions.cs` — its options (VS Code's defaults, no public option in phase 5). `RangesLimitReporter.cs` — how many ranges were found and whether they were limited.
- `FoldingModel.cs`, `FoldingModelFunctions.cs`, `IDecorationProvider.cs` — the regions, their collapse state and decorations, and the level / jump functions.
- `FoldingRanges.cs`, `FoldingRegions.SanitizeAndMerge.cs`, `FoldingRegion.cs`, `SelectedLines.cs` — the packed region storage and its merge.
- `HiddenRangeModel.cs` — the hidden ranges of the collapsed regions. `FoldingDecorationProvider.cs`, `ShowFoldingControls.cs` — the decoration options.
- `IndentRangeProvider.cs`, `IndentRanges.cs`, `IRangeProvider.cs` — the indentation range provider.
- `FoldingCommands.cs` — command ids. `FoldingFeature.Commands.cs` — the command bodies (no arguments; `editor.toggleImportFold` is not ported).

**Tests:** `tests/LoomEditor.View.Tests/Features/Folding/`.

**Known limits:** indentation strategy only (no syntax providers, no import folding); no view-state memento; the update delay is fixed (300 ms the first time, then 200 ms) instead of adapting to the measured compute time.
