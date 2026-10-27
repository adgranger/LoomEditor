# P9-05 — Diff heuristics (1/2): `optimizeSequenceDiffs` and `removeShortMatches`

- **Status:** todo
- **Depends on:** P9-04
- **Size:** M

## Goal

The first half of Monaco's post-processing of a raw sequence diff exists: joining diffs by shifting them over equal elements, moving each diff to the best boundary (so that an insertion lands at a word or line boundary), and removing matches that are too short to be worth keeping.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/diff/defaultLinesDiffComputer/heuristicSequenceOptimizations.ts` | 12–33 | `optimizeSequenceDiffs` |
| same | 34–131 | `joinSequenceDiffsByShifting` |
| same | 132–154 | `shiftSequenceDiffs` |
| same | 155–202 | `shiftDiffToBetterPosition` |
| same | 203–221 | `removeShortMatches` |

## Files to create or change

- `src/LoomEditor.Core/Diff/HeuristicSequenceOptimizations.cs` — create (`internal static partial class HeuristicSequenceOptimizations`; this card writes the file `HeuristicSequenceOptimizations.Shifting.cs`, P9-06 writes `HeuristicSequenceOptimizations.Words.cs`)
- `tests/LoomEditor.Core.Tests/Diff/HeuristicSequenceOptimizationsTests.cs` — create

## Design decisions (already made — do not change)

- Signatures (`SequenceDiff` lists, as in Monaco `SequenceDiff[]`): `static IReadOnlyList<SequenceDiff> OptimizeSequenceDiffs(ISequence sequence1, ISequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs)`; `RemoveShortMatches(ISequence sequence1, ISequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs)`.
  Private: `JoinSequenceDiffsByShifting` (it is called **twice** in Monaco, with `shiftSequenceDiffs` between: keep the order, read lines 12–33), `ShiftSequenceDiffs`, `ShiftDiffToBetterPosition`.
- Where Monaco calls `sequence1.getBoundaryScore?.(…)` and the method is absent, the score is 0 (P9-02 made `GetBoundaryScore` a default interface method returning 0): read `shiftDiffToBetterPosition` and keep its exact tie-break (`>` vs `>=` matters: the fixtures of P9-10 check it).
- `joinSequenceDiffsByShifting`'s `Math.floor`-free integer arithmetic is already integral; keep every loop bound and the `continue` statements as written. Do not use LINQ in these hot loops.
- `List<SequenceDiff>` internally, `IReadOnlyList` in signatures.
- `<remarks>Threading: stateless.</remarks>`

## Tests

- [ ] New (the Monaco tests for these functions are the fixtures of P9-10; here small hand-computed cases, using `ArraySequence` from P9-03 with a boundary-score override where needed): `Optimize_JoinsDiffsSeparatedByEqualElements`, `Optimize_ShiftsInsertionToBoundary` (a custom `ISequence` whose `GetBoundaryScore` is highest at offset 3 → the insertion `[1,1)` moves to `[3,3)`), `Optimize_NoDiffs_Empty`, `Optimize_ShiftIsBoundedByStronglyEqual` (elements equal but not strongly equal block the shift), `RemoveShortMatches_MergesDiffsWithTinyEqualGap` (gap `<= 2` merges only when both neighbours are large: read lines 203–221), `RemoveShortMatches_KeepsLongGap`, `Optimize_Result_IsSortedAndNonOverlapping` (random: `new Random(seed)`, seed in the message).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated: `heuristicSequenceOptimizations.ts` (1/2)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The word extension and the "very short matching" removals (P9-06).

## Suggested commit message

`feat(core): port diff shifting heuristics (optimizeSequenceDiffs)`
