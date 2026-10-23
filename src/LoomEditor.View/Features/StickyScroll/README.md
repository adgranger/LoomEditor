# StickyScroll

**Role:** keeps the lines that open the current nested scopes (class, method, block) stuck at the top of the editor while scrolling.

**Monaco sources:** `src/vs/editor/contrib/stickyScroll/browser/` (`stickyScrollElement.ts`, `stickyScrollProvider.ts`, `stickyScrollWidget.ts`) and `EditorStickyScroll` in `src/vs/editor/common/config/editorOptions.ts`.

**Files:** `EditorStickyScrollOptions.cs` (options and defaults), `StickyScrollElement.cs` (`StickyRange`, `StickyElement`, `StickyModel`), `StickyLineCandidate.cs`, `StickyScrollWidgetState.cs`, `StickyModelProvider.cs`, `StickyLineCandidateProvider.cs`. The controller and the feature come with P6-58.

**Tests:** `tests/LoomEditor.View.Tests/Features/StickyScroll/`.

**Known limits:** the outline and folding-provider models need language features (phase 8): the indentation model is used instead.
