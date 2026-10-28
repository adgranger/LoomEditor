# ADR 0012 — Diff editor: the diff algorithm, the diff model, view zones, the `LoomDiffEditor` control

- **Status:** accepted
- **Date:** 2026-10-27

## Context

Phase 9 ([roadmap](../roadmap/phase-9-diff-editor.md)) adds Monaco's diff editor: two texts compared line by line and character by character,
shown side by side or inline, with the changes highlighted, unchanged regions collapsible, an overview ruler and an accessible
diff viewer. The Monaco code has four layers, and each one gets its own decision here:

1. `editor/common/diff` — the **diff algorithm** (`DefaultLinesDiffComputer`: Myers and dynamic programming, heuristics, moved lines): pure code on `string[]`.
2. `editor/browser/widget/diffEditor` — the **diff model** (`DiffEditorViewModel`, `UnchangedRegion`, view zone alignment) and the **widget** that wires two code editors together.
3. The editor features the widget needs and LoomEditor does not have yet: **view zones**, a **read-only** mode, a generic **overview ruler** (`overviewZoneManager.ts`).
4. The **WinUI control**.

## Decisions

1. **Where the code goes.**

   | What | Where |
   |---|---|
   | ranges, sequences, algorithms, heuristics, mappings, `DefaultLinesDiffComputer`, moved lines, `DocumentDiffProvider` | `LoomEditor.Core/Diff/` (namespace `LoomEditor.Core.Diff`; `Algorithms/` for the two algorithms); `LineRange` / `LineRangeSet` in `LoomEditor.Core/Text/` |
   | options, `DiffState`, `UnchangedRegion`, `DiffEditorViewModel`, range alignment, decorations, navigation, the accessible viewer model | `LoomEditor.View/DiffEditor/` |
   | view zones (model), `OverviewZoneManager` | `LoomEditor.View/Layout/` |
   | drawing of zones and of the overview ruler | `LoomEditor.Rendering` |
   | `LoomDiffEditor` (the control), the sash, the read-only property, the accessible viewer UI | `LoomEditor.WinUI/Controls/` (`DiffEditor/` sub-folder) |

   Everything is `internal` except what card P9-22 says is public (`LoomDiffEditor` and the few enums / properties of its API). The layering rule is unchanged:
   `WinUI → Rendering → View → Core`; the diff algorithm knows nothing about models or UI.

2. **No observables.** Monaco's widget is written with `IObservable` / `autorun` (`base/common/observable`), which LoomEditor does not have and does not port. Each
   class of the diff model is a plain object that raises an event (`EventHandler`) when its state changes; the control subscribes and recomputes what depends on it
   (the same style as `ViewModel`). Where Monaco computes a `derived` value, the card names a method that computes it on demand.

3. **The diff runs off the UI thread.** `DefaultLinesDiffComputer.ComputeDiff(string[], string[], options)` is pure. `DocumentDiffProvider` takes the two line arrays on the UI
   thread, runs the computation with `Task.Run` (no reflection, AOT-safe), and the view model receives the result through `UiContinuation.Then` (phase 8, P8-05) with a
   `CancellationToken` that the next request cancels. The time limit is the algorithm's own `maxComputationTimeMs` (`ITimeout`), as in Monaco.

4. **Stale diff after an edit.** Monaco maps the previous diff through the edit (`applyModifiedEdits`, `applyOriginalEdits`, `TextEditInfo`) so the highlights follow the text
   while the new diff is computed (debounce 200 ms). Phase 9 does **not** port this: until the new result arrives (`IsDiffUpToDate == false`) the old mappings are
   kept and clamped to the model's line count. Behavior difference, listed in `PORTING.md`; a follow-up card can add it.

5. **The control owns two `LoomEditor` controls and their `TextModel`s.** `LoomDiffEditor` creates the original and the modified editor; the application sets the text
   (`OriginalText`, `ModifiedText`, `Language`) or edits `Original.Model` / `Modified.Model` directly. There is no `setModel` (Monaco's `IDiffEditorModel`): the `LoomEditor` control
   does not support replacing its model, and phase 9 does not add that.

6. **View zones are whitespaces with a purpose.** `ViewLayout` already has whitespaces (P2-04). P9-18 adds a model-line based `ViewZone` on top (`Id`, `AfterLineNumber`, `HeightInPx`, `Kind`,
   `ShowInHiddenAreas`) and P9-19 draws them. A zone has no DOM node (there is no per-zone element): its **kind** decides what the renderer paints:
   `Empty` (the diagonal fill that aligns the two sides), `DeletedLines` (inline mode: lines of the original model, drawn with that model's tokens and the deleted-text decoration),
   `Collapsed` (the "N hidden lines" bar of an unchanged region). Zones are drawn entirely by the renderer (no WinUI element per zone); interactive zones (the unchanged region bar: click, drag) get their pointer input through the zone pointer events of the editor control (P9-21), and the diff control acts on them.

7. **Scroll synchronization is by equal `ScrollTop`.** Because the alignment zones give both sides the same height for corresponding line ranges, keeping `ScrollTop` equal
   (and `ScrollLeft` equal) is the whole synchronization; Monaco's top padding for moved-block comparison is not ported.

8. **Side by side and inline.** Both are supported (`RenderSideBySide`); `useInlineViewWhenSpaceIsLimited` is supported (width below the breakpoint → inline). In inline mode
   there is **one** visible editor (the modified one) and the deleted lines are `DeletedLines` zones; Monaco's "true inline" (`useTrueInlineView`, deleted words inside the line) is not ported.

9. **Not in phase 9** (listed in the README of the phase): moved blocks drawing (the computation is ported; `showMoves` is experimental in Monaco), the gutter menu and revert buttons
   (`gutterFeature.ts`, `revertButtonsFeature.ts`), `diffCodeLens`, the embedded diff editor, `compactMode`, the legacy diff computer (`legacyLinesDiffComputer.ts`), diff algorithm
   selection (always the advanced one), `serializeState` / `restoreSerializedState`, accessibility signals (sounds), breadcrumbs of unchanged regions. `OriginalEditable`
   is supported as a property (default `false`).

## Consequences

- The algorithm is a faithful port with Monaco's fixture tests (P9-10), so it can be trusted without a UI.
- The widget differs structurally from Monaco (events instead of observables, one control assembling two controls); every difference that changes behavior goes into `PORTING.md`.
- View zones and read-only mode are general editor features that later phases can use.

## Changes during implementation

Written when phase 9 was closed (2026-10-28). The decisions above held; these are the places where a card did something else or more. The per-card differences with
Monaco are in the "Behavior differences" rows of [PORTING.md](../PORTING.md) (`(P9-nn)`).

- **Decision 1 (where the code goes).** The navigation commands are a feature folder, `LoomEditor.View/Features/DiffNavigation/` (P9-26), that is **not** in `EditorFeatures.Default`: the diff
  control appends it to the features of its two editors (`EditorFeatures.ForDiffEditor`), and its keys resolve only under the new `isInDiffEditor` condition. `OffsetRange.Clip` /
  `ClipCyclic` (Core) were added for the accessible viewer model (P9-29). `Win2DRenderSurface` accepts a null text measurer, so the overview strip can fill shapes without one (P9-28).
- **Decision 5 (the control).** The language property is `LanguageId` (like `LoomEditor`), not `Language`.
- **Decision 9 (not in phase 9).** Held. The `F7` / `Shift+F7` keys of the accessible viewer shadow the word highlighter's `F7` / `Shift+F7` inside a diff editor (the rules are registered
  after it); outside a diff editor nothing changed.
- **The accessible diff viewer** (P9-29): Monaco builds a group range with the original and modified ranges swapped, so its header announces the modified numbers as the original ones and its
  initial group is picked on the original side; the port builds it the right way round. A deleted row reveals its block in the modified editor in the inline view (the original editor is hidden).
- **Overview ruler** (P9-27, P9-28): colors are `ThemeColor` values (the sort order of equal zones compares ARGB, not strings); the editors' own overview ruler is off in a diff editor.
- **Gallery** (P9-31): the demo is a separate window; the earlier in-window preview switch stays. The manual plan is written and **not run**.

What phase 9 did not verify: the manual plan, Narrator, the hatching at high DPI. The Native AOT publish of the Gallery and the diff self-check (`--diff-selfcheck`) passed in P9-31 / P9-32.
