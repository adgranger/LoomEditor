# P9-17 — Diff decorations: from a `DiffState` to decorations of the two models

- **Status:** done
- **Depends on:** P9-16, P9-14
- **Size:** S

## Goal

`DiffDecorationsComputer` turns a `DiffState` into the list of model decorations for the original and for the modified editor (whole-line backgrounds, character level changes, empty-range markers), and `DiffDecorations` keeps them applied to the two models when the diff or the options change.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/components/diffEditorDecorations.ts` | 18–31 | `DiffEditorDecorations` (applies the decorations to both editors) |
| same | 33–104 | the `_decorations` derived value, **without** the `movedTextToCompare` branches and the `useInlineDiff` branch (no moves display, no true inline view) |
| same | 105–126 | moved blocks decorations — **not ported** (listed in the card's out of scope) |
| `src/vs/editor/browser/widget/diffEditor/utils.ts` | 60–73 | `applyObservableDecorations` (what it does: `deltaDecorations` on change) |

## Files to create or change

- `src/LoomEditor.View/DiffEditor/DiffDecorationsComputer.cs`, `DiffDecorations.cs` — create
- `tests/LoomEditor.View.Tests/DiffEditor/DiffDecorationsComputerTests.cs`, `DiffDecorationsTests.cs` — create

## Design decisions (already made — do not change)

- `static (IReadOnlyList<ModelDeltaDecoration> Original, IReadOnlyList<ModelDeltaDecoration> Modified) DiffDecorationsComputer.Compute(DiffState diff, bool renderIndicators, bool showEmptyDecorations, int originalLineCount, int modifiedLineCount)`. The line counts clamp stale ranges (ADR 0012 decision 4): every range is clamped to the model with the existing `ValidateRange` of the decoration target **or** by dropping decorations whose line range lies entirely beyond the last line and clamping the end of the others (write the clamp as a pure function here, tested).
- For every `DiffMapping m` (line mapping `c`):
  - `!c.Original.IsEmpty` → original decoration over `c.Original.ToInclusiveRange()`: `renderIndicators ? LineDeleteBackgroundWithIndicator : LineDeleteBackground`; `!c.Modified.IsEmpty` → modified: `LineAddBackgroundWithIndicator` / `LineAddBackground`.
  - `c.Modified.IsEmpty || c.Original.IsEmpty` → whole-line decorations: `WholeLineDeleteDecoration` on the original range when not empty, `WholeLineAddDecoration` on the modified range when not empty.
  - otherwise for every inner change `i`: if `c.Original.Contains(i.OriginalRange.StartLineNumber)` → original `(i.OriginalRange.IsEmpty && showEmptyDecorations) ? DeleteDecorationEmpty : DeleteDecoration`; if `c.Modified.Contains(i.ModifiedRange.StartLineNumber)` → modified `(IsEmpty && showEmptyDecorations) ? AddDecorationEmpty : AddDecoration`.
- `DiffDecorations(IEditorDecorationTarget original, IEditorDecorationTarget modified)`: the target is the small interface (`internal interface IDiffDecorationTarget { void SetDecorations(IReadOnlyList<ModelDeltaDecoration> decorations); }`) implemented in P9-21 by the control over `EditorContext.CreateDecorationsCollection` (existing, `EditorDecorationsCollection.Set`). `void Update(DiffState? diff, DiffEditorOptions options, int originalLineCount, int modifiedLineCount)` computes and sets both sides (a null diff clears them); the control calls it on `DiffChanged` and on option changes (`RenderIndicators`, `ShowEmptyDecorations`).
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [x] New (the expected decorations are listed by hand: range + options instance): `Compute_NoDiff_Empty`, `Compute_Insertion_ModifiedGetsLineAndWholeLineAdd`, `Compute_Deletion_OriginalGetsLineAndWholeLineDelete`, `Compute_Replacement_InnerChangesBecomeCharDecorations`, `Compute_EmptyInnerRange_UsesEmptyOption_WhenShowEmptyDecorations`, `Compute_EmptyInnerRange_UsesNormalOption_WhenNotShowEmptyDecorations`, `Compute_InnerChangeOutsideLineRange_Skipped` (Monaco: "Don't show empty markers outside the line range"), `Compute_RenderIndicatorsFalse_PlainBackgrounds`, `Compute_StaleRangesBeyondModel_ClampedOrDropped`, `Update_SetsBothSides`, `Update_NullDiff_ClearsBoth`.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `diffEditorDecorations.ts` (subset: no moved blocks, no true inline)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Moved text decorations and `movedTextToCompare`, true inline `before` decorations, the inline view's deleted lines (P9-24).

## Suggested commit message

`feat(view): compute the diff decorations`
