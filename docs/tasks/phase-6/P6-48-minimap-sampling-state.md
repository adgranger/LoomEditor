# P6-48 — `MinimapSamplingState`: which view lines a sampled minimap shows

- **Status:** done
- **Depends on:** P6-46
- **Size:** S

## Goal

When the document has more lines than the minimap can show (`size: fill` / `fit` with sampling), the minimap shows a sample
of the view lines; the sampling state computes that sample, keeps it stable across edits (lines inserted / deleted) and maps
model lines to minimap lines.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/viewParts/minimap/minimap.ts`.

| Lines | What to port |
|---|---|
| 596–614 | the `SamplingState*Event` types |
| 616–723 | `MinimapSamplingState.compute` (with `oldSamplingState`, the halving / insertion of samples and the emitted events) |
| 725–810 | the constructor, `modelLineToMinimapLine`, `modelLineRangeToMinimapLineRange`, `decorationLineRangeToMinimapLineRange`, `onLinesDeleted`, `onLinesInserted` |

## Files to create or change

- `src/LoomEditor.View/Minimap/MinimapSamplingState.cs` — create (header `minimap.ts (MinimapSamplingState)`)
- `tests/LoomEditor.View.Tests/Minimap/MinimapSamplingStateTests.cs` — create

## Design decisions (already made — do not change)

- The three event kinds become a small class hierarchy (`abstract record SamplingStateEvent` with `SamplingStateLinesInserted(int InsertFromLineNumber, int InsertToLineNumber)`,
  `SamplingStateLinesDeleted(int DeleteFromLineNumber, int DeleteToLineNumber)`, `SamplingStateFlush`) — Monaco's tagged union.
- `compute` returns `(MinimapSamplingState? State, IReadOnlyList<SamplingStateEvent> Events)`.
- `binarySearch` (from `base/common/arrays`) → `Array.BinarySearch` / `List.BinarySearch` **only** if the not-found result encoding
  matches Monaco's (`-(insertionIndex + 1)` in both: it does — `~index` is `-(index + 1)`); keep Monaco's arithmetic on the result.
- `onLinesDeleted` / `onLinesInserted` take `(int fromLineNumber, int toLineNumber)` instead of view events.

## Tests

No upstream unit tests. New:

- [x] `Compute_NotSampling_ReturnsNullState`
- [x] `Compute_Sampling_TakesEvenlySpacedLines`
- [x] `Compute_FromOldState_KeepsCommonSamplesAndEmitsEvents`
- [x] `ModelLineToMinimapLine_FindsTheClosestSample`
- [x] `OnLinesInserted_ShiftsTheSamplesAfter`; `OnLinesDeleted_RemovesAndShifts`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `minimap.ts` row (+ `MinimapSamplingState`)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Rendering (P6-49 …).

## Suggested commit message

`feat(view): sample view lines for the minimap`
