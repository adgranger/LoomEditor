# StickyScroll

**Role:** keeps the lines that open the current nested scopes (class, method, block) stuck at the top of the editor while scrolling.

**Monaco sources:** `src/vs/editor/contrib/stickyScroll/browser/` (`stickyScrollElement.ts`, `stickyScrollProvider.ts`, `stickyScrollWidget.ts`) and `EditorStickyScroll` in `src/vs/editor/common/config/editorOptions.ts`.

**Files:** `EditorStickyScrollOptions.cs` (options and defaults), `StickyScrollElement.cs` (`StickyRange`, `StickyElement`, `StickyModel`), `StickyLineCandidate.cs`, `StickyScrollWidgetState.cs`, `StickyModelProvider.cs`, `StickyLineCandidateProvider.cs`. `StickyScrollFeature.cs` (the controller: `findScrollWidgetState`, triggers), `StickyScrollRenderData.cs` (what the renderer draws, published in `ViewModel.StickyScroll`), `StickyScrollFeature.Mouse.cs` (reveal on click, shift-click, folding chevron, hover), `StickyScrollCommands.cs` (`editor.action.toggleStickyScroll`).

**Drawing and control:** `LoomEditor.Rendering/TextViewRenderer.StickyScroll.cs` draws `ViewModel.StickyScroll` (`ViewModel.StickyScroll.cs`); the control properties (`StickyScrollEnabled`, `StickyScrollMaxLineCount`) are in `LoomEditor.WinUI/Controls/LoomEditor.StickyScroll.cs`.

**Tests:** `tests/LoomEditor.View.Tests/Features/StickyScroll/`.

**Known limits:** the outline and folding-provider models need language features (phase 8): the indentation model is used instead. Not ported: the click-link gesture and the keyboard navigation of the sticky lines (see "Cut points for later phases" in `docs/PORTING.md`).
