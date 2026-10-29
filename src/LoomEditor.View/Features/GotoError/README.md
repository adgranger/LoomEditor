# GotoError

**Role:** go to the next / previous problem (error, warning, info) of the model. `MarkerList` holds the sorted problems of the
editor's `MarkerService` and does the "next one" arithmetic, wrapping around.

**Monaco source:** `src/vs/editor/contrib/gotoError/browser/markerNavigationService.ts` (`MarkerList`, `MarkerCoordinate`),
`gotoError.ts` (later card).

**Files:**
- `MarkerList.cs`, `MarkerCoordinate.cs`, `MarkerSortOrder.cs` — the sorted problems, `Move`, `Find`, `Changed`.
- `GotoErrorFeature.cs`, `GotoErrorCommands.cs` — placeholders until the commands and the keys (P10-28; registered in `EditorFeatures.Default`).

**Tests:** `tests/LoomEditor.View.Tests/Features/GotoError/`.

**Known limits:** a single resource (a `MarkerService` belongs to one model); the sort order is a constructor argument, not the
`problems.sortOrder` setting; no `IMarkerNavigationService`.
