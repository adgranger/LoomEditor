# StickyScroll

**Role:** keeps the lines that open the current nested scopes (class, method, block) stuck at the top of the editor while scrolling.

**Monaco sources:** `src/vs/editor/contrib/stickyScroll/browser/` (`stickyScrollElement.ts`, `stickyScrollProvider.ts`, `stickyScrollWidget.ts`) and `EditorStickyScroll` in `src/vs/editor/common/config/editorOptions.ts`.

**Files:** `EditorStickyScrollOptions.cs` (options and defaults), `StickyScrollElement.cs` (`StickyRange`, `StickyElement`, `StickyModel`), `StickyLineCandidate.cs`, `StickyScrollWidgetState.cs`, `StickyModelProvider.cs`, `StickyLineCandidateProvider.cs`. `StickyScrollFeature.cs` (the controller: `findScrollWidgetState`, triggers), `StickyScrollRenderData.cs` (what the renderer draws, published in `ViewModel.StickyScroll`), `StickyScrollCommands.cs` (command ids, from P6-61).

**Tests:** `tests/LoomEditor.View.Tests/Features/StickyScroll/`.

**Known limits:** the outline and folding-provider models need language features (phase 8): the indentation model is used instead.
