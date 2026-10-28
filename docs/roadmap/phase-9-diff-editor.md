# Phase 9 — Diff editor

**Done when:** the Gallery shows a diff of two files side by side and inline, with unchanged regions collapsible, navigation, an overview ruler and an accessible viewer. — **Built and covered by automated tests; the manual plan has not been run** (see "Status of the done-when line").

Task cards: [docs/tasks/phase-9/](../tasks/phase-9/README.md). Design: [ADR 0012](../adr/0012-diff-editor.md). How to use it: [how-to/use-the-diff-editor.md](../how-to/use-the-diff-editor.md).

## Checklist (ticked 2026-10-28 = built and covered by automated tests)

- [x] Port the diff algorithm (`editor/common/diff`) — P9-01 … P9-11 (ranges, Myers and dynamic programming, heuristics, range mappings, `DefaultLinesDiffComputer`, moved lines, Monaco's fixture tests (P9-10), the document diff provider)
- [x] Side-by-side and inline diff views — P9-22 … P9-24 (the `LoomDiffEditor` control, aligned view zones and synchronized scrolling, deleted lines in the inline view)

Also part of the phase (what the two lines above need, and what earlier phases left for it):

- [x] The diff model: options, `DiffState`, unchanged regions, `DiffEditorViewModel`, line alignment, decorations and theme colors — P9-12 … P9-17
- [x] Editor infrastructure: view zones (model and drawing), read-only mode, control hooks — P9-18 … P9-21
- [x] Hide unchanged regions — P9-25; go to next / previous change — P9-26
- [x] The generic `OverviewRuler` / `OverviewZoneManager` (left by phase 6) and the diff overview — P9-27, P9-28
- [x] The accessible diff viewer (left by phase 7) — P9-29, P9-30
- [x] Gallery demo and manual test plan — P9-31; Native AOT self-check and benchmarks — P9-32; close the phase — P9-33
- [ ] The manual test plan ([testing/phase-9-manual-tests.md](../testing/phase-9-manual-tests.md), written by P9-31) run by the maintainer: **pending**

## Status of the done-when line

- **Native AOT**: the Gallery publish (`dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror`) **succeeded** in P9-31 and again in P9-32, and the published exe stayed
  up after 10 s (P9-31, main window only). P9-33 changed no code and did not run it again.
- **The diff stack in the published build**: the self-check (`LoomEditor.Gallery.exe --diff-selfcheck=<file>`, P9-32) ran in the Native AOT exe and passed 9 of 9 (algorithm, moves, large, timeout, provider,
  provider-cancel, viewmodel, alignment, accessible-model), exit code 0. It does not open the diff window.
- **Not verified**: the diff control itself under Native AOT with a human (drawing, pointer, scrolling), the manual plan (an agent runs none of it), Narrator reading the accessible viewer, the hatching at high DPI.
- **Performance** (P9-32, [performance.md](../performance.md)): 2,000 lines with 1 % changed in about 5 ms and 20,000 lines in about 35 ms (card targets: 100 ms and 1 s: met). The typing and scrolling feel of the
  **Large files** button (5,000 lines) is part of the manual plan.
- **Tests**: 465 tests in the 47 test files created in phase 9 (counted; see [PORTING.md](../PORTING.md) "Done in phase 9"); 6,527 tests in the solution on 2026-10-28, the build has 0 warnings.
  Timing dependent tests failed on some full runs during the card work and passed when run again, among them `SyntaxHighlighterTests.SetTheme_ChangesTokenColors` (phase 4, four times), and a `HoverController` and two `Suggest` tests (once each); nobody investigated, they look sensitive to machine load.

## Not delivered (ADR 0012 decision 9 and the phase README)

Moved blocks drawing (the computation is ported), the gutter menu and revert buttons, `diffCodeLens`, the embedded diff editor, `compactMode`, true inline view, the legacy diff computer and algorithm selection,
`serializeState`, accessibility sounds, mapping the old diff through edits while a new one is computed, `setModel`, breadcrumbs. They are the section E of the [phase 10 backlog](phase-10-editor-completeness.md).

## Open risks

- The **stale diff while typing**: the old highlights stay for about 200 ms plus the computation (ADR 0012 decision 4).
- The deleted text of the **inline view cannot be selected**.
- The **hatching** is rasterized at 1 pixel per DIP: slightly soft at high DPI.
- The **performance numbers** of P9-32 are a 3-iteration measurement with wide error bars, on one machine; they do not cover the inner (character) diff of a very large change nor the drawing.
- **Narrator** was not run by an agent; the accessible viewer is built to the labels of Monaco but the real reading is unverified.
- `F7` in a diff editor opens the accessible viewer instead of moving through the word highlights.
