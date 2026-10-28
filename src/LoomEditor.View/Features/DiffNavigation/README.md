# DiffNavigation

**Role:** adds the commands that move the cursor of a diff editor to the next / previous change (`Alt+F5` / `Shift+Alt+F5`).

**Monaco source:** `src/vs/editor/browser/widget/diffEditor/diffEditorWidget.ts` (`goToDiff`, `revealFirstDiff`). The keys come from
VS Code's workbench, not from Monaco.

**Files:**
- `DiffNavigationFeature.cs` — registers the two commands and forwards them to the diff control (`IDiffNavigationTarget`).
- `DiffNavigationCommands.cs` — command ids.
- The search itself is the pure `DiffEditor/DiffNavigator.cs`.

**Tests:** `tests/LoomEditor.View.Tests/Features/DiffNavigation/`, `tests/LoomEditor.View.Tests/DiffEditor/DiffNavigatorTests.cs`.

**Known limits:** not in `EditorFeatures.Default`; the diff control adds it to its sub editors (`EditorFeatures.ForDiffEditor`).
