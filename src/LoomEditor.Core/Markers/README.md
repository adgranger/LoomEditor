# Markers

The problems (errors, warnings, infos, hints) an owner reports for the editor's document (Monaco `platform/markers`).
`MarkerService` stores them per owner, hands out stable `Marker` instances and raises `MarkersChanged`. Drawing them is
done elsewhere (P8-04 turns them into decorations).

Files: `MarkerSeverity`, `MarkerTag`, `MarkerData`, `Marker`, `MarkerStatistics`, `MarkerService`.
