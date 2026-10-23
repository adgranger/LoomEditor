# Markers

The problems (errors, warnings, infos, hints) an owner reports for the editor's document (Monaco `platform/markers`).
`MarkerService` stores them per owner, hands out stable `Marker` instances and raises `MarkersChanged`. Drawing them is
done elsewhere: `MarkerDecorations` (internal) turns them into model decorations (`squiggly-*` class names, z-index,
overview ruler and minimap options), follows edits and maps a decoration id back to its `Marker`.

Files: `MarkerSeverity`, `MarkerTag`, `MarkerData`, `Marker`, `MarkerStatistics`, `MarkerService`, `MarkerDecorations`, `MarkerClassNames`.
