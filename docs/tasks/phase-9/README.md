# Phase 9 — read this before any card

Phase 9 brings the **diff editor** of the roadmap ([phase-9-diff-editor.md](../../roadmap/phase-9-diff-editor.md)): the diff algorithm of `editor/common/diff` and
the side-by-side and inline diff views of `editor/browser/widget/diffEditor`, with a generic overview ruler and an accessible diff viewer. The design is in
[ADR 0012](../../adr/0012-diff-editor.md): **read it first**. This page lists the conventions that every phase 9 card relies on; a card never repeats them. The conventions of
[phase-3/README.md](../phase-3/README.md) (sections 1, 3–6), [phase-5/README.md](../phase-5/README.md) (sections 1–5), [phase-6/README.md](../phase-6/README.md) (sections 1, 2),
[phase-7/README.md](../phase-7/README.md) (sections 5–7) and [phase-8/README.md](../phase-8/README.md) (sections 1, 2, 4) still apply.

## Order of the cards

Take the cards in table order of [../README.md](../README.md) (each card's "Depends on" is satisfied by the cards above it). Steps:

- **A — The diff algorithm** (P9-01 … P9-11, `LoomEditor.Core`): ranges, sequences, Myers and dynamic programming, heuristics, range mappings, `DefaultLinesDiffComputer`, moved lines, Monaco's fixture tests, the document diff provider.
- **B — The diff model** (P9-12 … P9-17, `LoomEditor.View/DiffEditor`): options, `DiffState`, unchanged regions, `DiffEditorViewModel`, line alignment, decorations. No UI.
- **C — Editor infrastructure** (P9-18 … P9-21): view zones (model, drawing), read-only mode, the control hooks a host needs.
- **D — The control** (P9-22 … P9-26): `LoomDiffEditor`, side by side, inline, hidden unchanged regions, next / previous change.
- **E — Overview ruler** (P9-27, P9-28): `OverviewZoneManager`, the generic ruler and the diff overview.
- **F — Accessible diff viewer** (P9-29, P9-30): model, UI.
- **G — Prove it and close** (P9-31 … P9-33): Gallery demo and manual plan, the Native AOT self-check and benchmarks, close the phase.

Steps A and B need no UI (everything is unit tested). P9-10, P9-23 and P9-24 are flagged **Recommended model: Opus** (reason in the card; on failure the card says what to report).
P9-19, P9-22 … P9-26, P9-28, P9-30 and P9-31 have manual checks for the maintainer (`docs/testing/phase-9-manual-tests.md`, written in full by P9-31).

## Out of scope for phase 9

Moved blocks drawing (the computation is ported), the gutter menu and revert buttons, `diffCodeLens`, the embedded diff editor, `compactMode`, true inline view
(deleted words inside the line), the legacy diff computer and algorithm selection, `serializeState`, accessibility sounds, mapping the old diff through edits while the new one is
computed (ADR 0012 decision 4), replacing the model of an editor (`setModel`), breadcrumbs. Anything the manual plan finds is a follow-up card, not a change to a closed one.

## 1. Upstream sources

Everything comes from `microsoft/vscode` at `0c9c0a4295a9222e12a605537738719455ae3056`
(`https://raw.githubusercontent.com/microsoft/vscode/0c9c0a4295a9222e12a605537738719455ae3056/<path>`; paths in the cards are full, `src/vs/…`). Line numbers are hints, the
function or class **name** is authoritative. A card with no Monaco counterpart says **"Sources: none (specified in this card)"**. The header of a ported file is the two-line
header of `CLAUDE.md` (checked by an architecture test).

## 2. Monaco concepts → LoomEditor (new in phase 9)

| Monaco | LoomEditor |
|---|---|
| `ILinesDiffComputer`, `DefaultLinesDiffComputer` | `ILinesDiffComputer`, `DefaultLinesDiffComputer` (Core, internal) |
| `LineRange` (`[start, endExclusive)` of line numbers), `OffsetRange` | `LineRange` (`LoomEditor.Core.Text`, `readonly record struct`), `OffsetRange` (existing) |
| `LineRangeMapping`, `DetailedLineRangeMapping`, `RangeMapping` | same names (`LoomEditor.Core.Diff`) |
| `IDocumentDiffProvider` + web worker | `IDocumentDiffProvider`, `DocumentDiffProvider` (`Task.Run`) |
| `DiffEditorViewModel` (observables) | `DiffEditorViewModel` (events, ADR 0012 decision 2) |
| `IViewZone`, `changeViewZones` | `ViewZone`, `ViewZones.Change` (View, P9-18) |
| `DiffEditorWidget` | `LoomDiffEditor` (WinUI control, public) |
| `ThemeColor` `diffEditor.*` | the same color ids in `EditorColorDefaults` (P9-16) |

## 3. Conventions

1. **Namespaces.** Core: `LoomEditor.Core.Diff` (`.Algorithms`); tests mirror `src/`. View: `LoomEditor.View.DiffEditor`. Ported tests keep Monaco's name in a `// Monaco:` comment.
2. **Line numbers are 1-based, offsets 0-based** exactly as in Monaco; `LineRange` is `[StartLineNumber, EndLineNumberExclusive)`. Do not convert.
3. **AOT**: delegates are fine; no reflection, no `dynamic`. Sequences are `ISequence` objects (no generic math).
4. **Deterministic**: the algorithm must give the same result as Monaco on the fixtures (P9-10); do not "improve" tie-breaking, iteration order or the floating point formulas.
5. **Time**: the algorithms take an `ITimeout`; tests that need a timeout use a fake `ITimeout` with a counter, never `Thread.Sleep`.
6. **Internal visibility**: Core's `InternalsVisibleTo` already lists View, Rendering, WinUI and the test projects; a new test project is not needed in phase 9.
