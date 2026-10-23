# P8-04 — `MarkerDecorations`: markers become model decorations

- **Status:** todo
- **Depends on:** P8-03
- **Size:** M

## Goal

`MarkerDecorations` keeps the model's decorations in step with a `MarkerService`: every marker is a decoration with Monaco's class names (`squiggly-error`, …), z-index, overview ruler and minimap options; ranges follow the edits;
and a decoration can be mapped back to its marker (hover needs it). Drawing the classes is P8-06.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/services/markerDecorationsService.ts` | 20–134 (`MarkerDecorationsService`: `getMarker`, `getLiveMarkers`, `_handleMarkerChange`, `_updateDecorations`), 138–305 (`MarkerDecorations`: `update`, `getMarker`, `getMarkers`, `_createDecorationRange`, `_createDecorationOption`, `_hasMarkerTag`) | `MarkerDecorations` |
| `src/vs/editor/common/model/intervalTree.ts` | 14–22 | `ClassName` constants (`squiggly-hint`, `squiggly-info`, `squiggly-warning`, `squiggly-error`, `squiggly-unnecessary`, `squiggly-inline-unnecessary`, `squiggly-inline-deprecated`) |

Not ported: suppressed ranges (`addMarkerSuppression`), the per-resource maps, `onDidChangeMarker` (the model's `DecorationsChanged` already tells the view), `_onModelRemoved` marker cleanup (the model of an editor never goes away).

## Files to create or change

- `src/LoomEditor.Core/Markers/MarkerDecorations.cs` — create (`internal sealed`)
- `src/LoomEditor.Core/Markers/MarkerClassNames.cs` — create (`internal static`, the `ClassName` constants)
- `src/LoomEditor.Core/Markers/README.md` — change
- `tests/LoomEditor.Core.Tests/Markers/MarkerDecorationsTests.cs` — create (there is **no upstream test** for this file at the pinned commit: the tests are `New:`)

## Design decisions (already made — do not change)

- `internal sealed class MarkerDecorations : IDisposable` with the constructor `MarkerDecorations(TextModel model, MarkerService service, int ownerId)` (the decoration owner id is the editor id: decorations of this owner are removed by `Dispose`).
  It subscribes to `service.MarkersChanged` and runs `Update()` once in the constructor; `Dispose` unsubscribes and removes its decorations (`model.DeltaDecorations(ownerId, ids, [])`).
- `Update()` (Monaco `_updateDecorations` + `MarkerDecorations.update`): `markers = service.Read(take: 500)` (Monaco: first 500); `added = markers not in the current map` and `removed = map entries not in markers`, **compared by reference** (`HashSet<Marker>` with reference comparer — `Marker` is a class);
  if both are empty return; `ids = model.DeltaDecorations(ownerId, removedIds, added.Select(m => new ModelDeltaDecoration(range(m), options(m))))`; the map is `Dictionary<Marker, string>` + the reverse `Dictionary<string, Marker>` (decoration id → marker; Monaco `BidirectionalMap`).
  Look at the existing signature of `TextModel.DeltaDecorations` and `ModelDeltaDecoration` first (`src/LoomEditor.Core/Model/TextModel.Decorations.cs`) and use them as they are.
- `_createDecorationRange` ported literally: `range = new Range(m.StartLineNumber, m.StartColumn, m.EndLineNumber, m.EndColumn)`; a `Hint` without the `Unnecessary` and without the `Deprecated` tag becomes `[start, start + 2 columns]` on the start line;
  `range = model.ValidateRange(range)`; if the range is empty: `maxColumn = model.GetLineLastNonWhitespaceColumn(line)` (0 → `model.GetLineMaxColumn(line)`); if `maxColumn == 1 || range.EndColumn >= maxColumn` keep the range; else if `model.GetWordAtPosition(start)` is not null use its `[StartColumn, EndColumn]` on that line;
  else (non-empty range): if `m.EndColumn == int.MaxValue && m.StartColumn == 1 && single line` then `minColumn = model.GetLineFirstNonWhitespaceColumn(line)`; if `minColumn < range.EndColumn` the range starts at `minColumn` (Monaco also mutates `rawMarker.startColumn`: **do not** mutate the marker here; `Marker` is immutable). Use the existing model methods (`ValidateRange`, `GetLineLastNonWhitespaceColumn`, `GetLineFirstNonWhitespaceColumn`, `GetLineMaxColumn`,
  `GetWordAtPosition`); find their exact names with `grep -n "LineLastNonWhitespaceColumn\|LineFirstNonWhitespaceColumn" src/LoomEditor.Core/Model`.
- `_createDecorationOption` ported literally (`ModelDecorationOptions.Register` / `CreateDynamic` of the existing code: the options of two markers with the same severity and tags are **the same registered instance**, cache them in a static `Dictionary<(MarkerSeverity, bool Unnecessary, bool Deprecated), ModelDecorationOptions>`):
  `Description = "marker-decoration"`, `Stickiness = NeverGrowsWhenTypingAtEdges`, `ShowIfCollapsed = true`, `ZIndex`: Hint 0, Info 10, Warning 20, Error (and any other value) 30; `ClassName`: Hint → `null` when `Deprecated`, else `squiggly-unnecessary` when `Unnecessary`, else `squiggly-hint`;
  Info → `squiggly-info`; Warning → `squiggly-warning`; Error / default → `squiggly-error`; `OverviewRuler` (Info, Warning, Error only) = color `editorOverviewRuler.infoForeground` / `.warningForeground` / `.errorForeground` as `DecorationColor.FromThemeColor(id)` and lane `Right`;
  `Minimap` (Info, Warning, Error only) = `minimap.infoHighlight` / `minimap.warningHighlight` / `minimap.errorHighlight` (`Inline`; these three ids exist in `EditorColorDefaults`; the three `editorOverviewRuler.*Foreground` ids are added to the theme by P8-06);
  `InlineClassName` = `squiggly-inline-unnecessary` when the marker has `Unnecessary`, then `squiggly-inline-deprecated` when it has `Deprecated` (the later wins, like Monaco).
- Queries: `Marker? GetMarker(string decorationId)` (Monaco's `getMarker(uri, decoration)`, keyed by the decoration id); `IReadOnlyList<(Range Range, Marker Marker)> GetLiveMarkers()` (Monaco `getMarkers`: the current range of each decoration via `model.GetDecorationRange(id)`, skipping the ones that returned null).
- `<remarks>Threading: UI thread only.</remarks>`. The class is `internal`: `EditorContext` (P8-05) owns it.
- Header: `// Ported from microsoft/vscode: src/vs/editor/common/services/markerDecorationsService.ts (MarkerDecorationsService, MarkerDecorations)`.

## Tests

- [ ] New (comments `// Monaco: MarkerDecorations.update` …): `Constructor_CreatesOneDecorationPerMarker` (two markers → `GetDecorationsInRange` finds two decorations with owner = ownerId and descriptions `marker-decoration`), `ClassNames_BySeverity` (Error → `squiggly-error`, Warning → `squiggly-warning`, Info → `squiggly-info`, Hint → `squiggly-hint`),
      `ZIndex_BySeverity`, `Hint_IsTwoColumnsWide`, `Hint_Unnecessary_KeepsTheRange_AndGetsTheUnnecessaryClass`, `Hint_Deprecated_HasNoClassButDeprecatedInlineClass`, `EmptyRange_UsesTheWord` (`"let value = 1"`, empty range at column 6 → covers `value`),
      `EmptyRange_AtEndOfLine_StaysEmpty`, `WholeLineMarker_StartsAtFirstNonWhitespace` (`startColumn = 1`, `endColumn = int.MaxValue` on `"    x"` → starts at column 5), `Update_OnlyChangesWhatChanged` (the decoration id of an unchanged marker is the same after another owner changes),
      `Update_RemovedMarker_RemovesDecoration`, `Update_Take500` (600 markers → 500 decorations), `RangesFollowEdits` (insert text before a marker: `GetLiveMarkers` gives the shifted range), `GetMarker_ByDecorationId`, `Dispose_RemovesAllDecorations_AndUnsubscribes`,
      `OverviewRuler_AndMinimap_OnlyForInfoWarningError` (a `Hint` has none), `SameSeverity_SharesOptionsInstance`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `markerDecorationsService.ts` (`MarkerDecorations`; not ported: suppression, per-resource maps, `onDidChangeMarker`) and the `ClassName` constants
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing the classes (P8-06), `EditorContext` wiring (P8-05), hover on markers (P8-09), marker suppression.

## Suggested commit message

`feat(core): turn markers into model decorations`
