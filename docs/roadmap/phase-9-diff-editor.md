# Phase 9 — Diff editor

Task cards: [docs/tasks/phase-9/](../tasks/phase-9/README.md). Design: [ADR 0012](../adr/0012-diff-editor.md).

- [ ] Port the diff algorithm (`editor/common/diff`) — P9-01 … P9-11 (ranges, Myers and dynamic programming, heuristics, range mappings, `DefaultLinesDiffComputer`, moved lines, Monaco's fixture tests, the document diff provider)
- [ ] Side-by-side and inline diff views — P9-22 … P9-24 (the `LoomDiffEditor` control, aligned view zones and synchronized scrolling, deleted lines in the inline view)

Also part of the phase (what the two lines above need, and what earlier phases left for it):

- [ ] The diff model: options, `DiffState`, unchanged regions, `DiffEditorViewModel`, line alignment, decorations and theme colors — P9-12 … P9-17
- [ ] Editor infrastructure: view zones (model and drawing), read-only mode, control hooks — P9-18 … P9-21
- [ ] Hide unchanged regions — P9-25; go to next / previous change — P9-26
- [ ] The generic `OverviewRuler` / `OverviewZoneManager` (left by phase 6) and the diff overview — P9-27, P9-28
- [ ] The accessible diff viewer (left by phase 7) — P9-29, P9-30
- [ ] Gallery demo and manual test plan — P9-31; Native AOT self-check and benchmarks — P9-32; close the phase — P9-33
- [ ] The manual test plan ([testing/phase-9-manual-tests.md](../testing/phase-9-manual-tests.md), written by P9-31) run by the maintainer

Not in phase 9 (listed in [ADR 0012](../adr/0012-diff-editor.md) decision 9): moved blocks drawing, the gutter menu and revert buttons, `diffCodeLens`, the embedded diff editor, `compactMode`, true inline view, the legacy diff computer, `serializeState`, accessibility sounds, mapping the old diff through edits, `setModel`, breadcrumbs.
