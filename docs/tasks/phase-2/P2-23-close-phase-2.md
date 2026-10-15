# P2-23 — Close phase 2

- **Status:** done
- **Depends on:** all other phase 2 cards
- **Size:** S (docs only)

## Goal

Make sure the documentation tells the truth about phase 2, and that the phase's "done when" criterion is met and
recorded: *a 100 MB file scrolls fluidly* (P2-22 numbers).

## Files to create or change

- `docs/PORTING.md` — map complete for every file touched in phase 2 (`scrollable.ts`, `lineHeights.ts`,
  `linesLayout.ts`, `viewLayout.ts`, `viewModel.ts` types, `viewLinesViewportData.ts`, `fontInfo.ts`,
  `fontMeasurements.ts`, `editorOptions.ts` subset, `scrollableElement.ts` subset, `viewModelImpl.ts` subset,
  `viewLines.ts`, `lineNumbers.ts`, `currentLineHighlight.ts`, `viewCursors.ts`, `viewCursor.ts`, `viewLineRenderer.ts`
  subset); "Behavior differences" complete; a "Monaco tests ported in phase 2" table (file, ported / total, why not);
  a short "Done in phase 2" paragraph pointing to `docs/tasks/phase-2/`
- `docs/roadmap/phase-2-rendering.md` — tick every item, with card numbers
- `docs/ARCHITECTURE.md` — "How a key press becomes pixels" → add "How a frame is drawn (phase 2)": wheel →
  `MouseWheelScrolling` → `ViewLayout` → `Scroll` event → `Invalidate` → `TextViewRenderer` (`ViewportData`, layout
  cache) → `Win2DRenderSurface`; "Where do I find…?" rows for scrolling, layout, caret, font measurement
- `docs/GLOSSARY.md` — add missing terms: *viewport*, *view line*, *scroll beyond last line*, *bigNumbersDelta*,
  *whitespace (view zone gap)*, *layout info*, *content width / scroll width*, *line layout cache*, *pixel ratio*
- `docs/adr/0002-rendering-backend.md` — under "Consequences", one line: "Superseded in part by ADR 0005 (no `CanvasVirtualControl`)"
- `src/LoomEditor.View/Layout/README.md`, `src/LoomEditor.Rendering/README.md` (create if missing) — describe what is there now
- `README.md` — status line and roadmap table ("we are here" → phase 3)
- `docs/tasks/README.md` — all phase 2 cards `done`

## Checks

- [x] Count the ported Monaco tests per file (by `test(` name, as in phase 1) and fill the table in `docs/PORTING.md`
- [x] Every `internal` class added in phase 2 has a threading note (`<remarks>Threading: ...</remarks>`)
- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the app starts

## Out of scope

Writing phase 3 task cards (done by the maintainer with the planning model, not here).

## Suggested commit message

`docs: close phase 2`
