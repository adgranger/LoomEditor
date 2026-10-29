# GotoError

**Role:** go to the next / previous problem (error, warning, info) of the model. `MarkerList` holds the sorted problems of the
editor's `MarkerService` and does the "next one" arithmetic, wrapping around.

**Monaco source:** `src/vs/editor/contrib/gotoError/browser/markerNavigationService.ts` (`MarkerList`, `MarkerCoordinate`),
`gotoError.ts` (`MarkerController`).

**Files:**
- `MarkerList.cs`, `MarkerCoordinate.cs`, `MarkerSortOrder.cs` — the sorted problems, `Move`, `Find`, `Changed`.
- `MarkerController.cs` — the session: `Navigate`, `Close`; follows the caret (the index resets when it leaves the marker).
- `GotoErrorFeature.cs`, `GotoErrorCommands.cs` — registers `editor.action.marker.next|prev|nextInFiles|prevInFiles` (`Alt+F8`, `Shift+Alt+F8`, `F8`, `Shift+F8`) and `closeMarkersNavigation` (`Escape`).

**Tests:** `tests/LoomEditor.View.Tests/Features/GotoError/`.

**Known limits:** no marker widget (peek view), no related information; a single resource (a `MarkerService` belongs to one model); the sort order is a constructor argument, not the
`problems.sortOrder` setting; no `IMarkerNavigationService`.
