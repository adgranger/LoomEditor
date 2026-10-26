# P8-03 — `MarkerService`: markers per owner (public API of Core)

- **Status:** done
- **Depends on:** P8-02
- **Size:** S

## Goal

An editor has a `MarkerService`: owners (the LSP client, an application's own checker) set the list of markers (problems: severity, message, range) they currently have; anything can read them and is
told when they change. Nothing is drawn yet (P8-04 turns markers into decorations).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/platform/markers/common/markers.ts` | 41–76 (`IRelatedInformation` not ported, `MarkerTag`, `MarkerSeverity`), 97–122 (`IMarkerData`), 128–144 (`IMarker`), 146–151 (`MarkerStatistics`) | `MarkerTag`, `MarkerSeverity`, `MarkerData`, `Marker`, `MarkerStatistics` |
| `src/vs/platform/markers/common/markerService.ts` | 80–148 (`MarkerStats`), 169–200 (`getStatistics`, `remove`, `changeOne`), 228–265 (`_toMarker`), 332–400 (`read`, `_accept`) | `MarkerService` (single resource: see decisions) |
| `src/vs/platform/markers/test/common/markerService.test.ts` | 33–63 (`query`), 65–81 (`changeOne override`), 83–101 (`changeOne/All clears`), 169–185 (`invalid marker data`), 195–213 (`Error code of zero in markers get removed, #31275`), 215–237 (`modelVersionId is preserved on IMarker when present in IMarkerData`) | tests |

## Files to create or change

- `src/LoomEditor.Core/Markers/MarkerSeverity.cs`, `MarkerTag.cs`, `MarkerData.cs`, `Marker.cs`, `MarkerStatistics.cs`, `MarkerService.cs` — create (**public**; namespace `LoomEditor.Core.Markers`)
- `src/LoomEditor.Core/Markers/README.md` — create
- `src/LoomEditor.Core/PublicAPI.Unshipped.txt` — regenerate
- `tests/LoomEditor.Core.Tests/Markers/MarkerServiceTests.cs` — create

## Design decisions (already made — do not change)

- **One editor = one model = one resource**, so Monaco's `resource` parameter and `ResourceMap` disappear: the service is keyed by **owner** only (`changeOne(owner, resource, markers)` → `Change(owner, markers)`; `remove(owner, [resource])` → `Remove(owner)`;
  `changeAll`, `installResourceFilter`, `IMarkerReadOptions.resource` / `ignoreResourceFilters`, the filtered "info marker" are not ported).
- `public enum MarkerSeverity { Hint = 1, Info = 2, Warning = 4, Error = 8 }` + `public static class MarkerSeverities { public static int Compare(MarkerSeverity a, MarkerSeverity b) => (int)b - (int)a; }` (Monaco `MarkerSeverity.compare`; the display strings are not ported).
  `public enum MarkerTag { Unnecessary = 1, Deprecated = 2 }`.
- `public sealed record MarkerData(MarkerSeverity Severity, string Message, int StartLineNumber, int StartColumn, int EndLineNumber, int EndColumn, string? Code = null, string? Source = null, int? ModelVersionId = null, IReadOnlyList<MarkerTag>? Tags = null)`
  (Monaco `IMarkerData` without `relatedInformation`, `origin`, and with `code` as a plain string).
- `public sealed class Marker`: `Owner` plus the same members as `MarkerData` (flattened, like Monaco's `IMarker`), constructed only by the service (`internal` constructor). **Instances are stable**: `Read` returns the same `Marker` objects as long as their owner did not call `Change`
  (the decorations of P8-04 diff by reference, like Monaco's `MarkerDecorations.update`). `Marker` is a class with reference equality on purpose. `public MarkerData ToData()` is not needed: do not add it.
- `Change(string owner, IReadOnlyList<MarkerData>? markers)`: null or empty → remove the owner's markers (raise `MarkersChanged` only if the owner had some); else replace them with the sanitized markers of `_toMarker` and **always** raise `MarkersChanged` (Monaco fires even when the new list is equal).
  `_toMarker`: an item with a null or empty `Message` is **dropped** (Monaco test `invalid marker data`: `"null"` as a message is valid); `StartLineNumber > 0 ? StartLineNumber : 1`; `StartColumn > 0 ? StartColumn : 1`; `EndLineNumber >= StartLineNumber ? EndLineNumber : StartLineNumber`; `EndColumn > 0 ? EndColumn : StartColumn`.
  Severity is **not** validated (Monaco test `#31275` passes `0`).
- `Remove(string owner)` = `Change(owner, null)`.
- `IReadOnlyList<Marker> Read(string? owner = null, int? severities = null, int take = -1)`: markers of `owner` (all owners when null, in the order the owners first called `Change`, each owner's markers in the order given), keeping the markers whose severity bit is set in `severities` (`(severities & (int)marker.Severity) == (int)marker.Severity`; null keeps all);
  `take > 0` limits the count (Monaco: `!take || take < 0` → unlimited; here `take <= 0` → unlimited).
- `public event EventHandler? MarkersChanged;`
- `public MarkerStatistics GetStatistics()`: `record struct MarkerStatistics(int Errors, int Warnings, int Infos, int Unknowns)` computed on demand from the current markers (`Hint` and any other severity count as unknown, as in Monaco's `MarkerStats._add`: errors, warnings, infos, else unknowns).
- `<remarks>Threading: UI thread only.</remarks>` on the service. Owner comparison is ordinal.

## Tests

- [x] Port (each carries `// Monaco: 'query'` …; Monaco's `changeAll` / resource arguments become `Change(owner, …)`; `randomMarkerData` becomes a helper in the test file creating an `Error` marker at random but valid positions with `new Random(seed)`): `query`, `changeOne override`, `changeOne/All clears`, `invalid marker data`, `Error code of zero in markers get removed, #31275`, `modelVersionId is preserved on IMarker when present in IMarkerData`
- [x] New: `Change_SanitizesPositions` (`0, 0, -1, 0` → `1, 1, 1, 1`), `Read_ReturnsTheSameInstancesUntilTheOwnerChanges` (owner A changes: B's markers are the same objects, A's are new), `Change_Empty_RaisesOnlyWhenOwnerHadMarkers`, `Change_RaisesEvenWhenEqual`, `Read_Take_Limits`,
      `GetStatistics_CountsBySeverity`, `Remove_Unknown_NoEvent`, `Change_NullOwner_Throws`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `./eng/Update-PublicApi.ps1 src/LoomEditor.Core` run, diff checked
- [x] `docs/PORTING.md` map: `markers.ts`, `markerService.ts` (single resource, no filters, no `changeAll`, no related information), the six ported tests
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Decorations (P8-04), `EditorContext` wiring (P8-05), related information, `origin`, resource filters, debounced events (`_onMarkerChanged` is synchronous here).

## Suggested commit message

`feat(core): add the marker service`
