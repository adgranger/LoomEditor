# P9-03 — `MyersDiffAlgorithm` and `DynamicProgrammingDiffing`

- **Status:** todo
- **Depends on:** P9-02
- **Size:** M

## Goal

Both sequence diff algorithms exist and give Monaco's results on small inputs: Myers (O(ND), used for large inputs) and the dynamic programming one (an LCS with a score function, used for small inputs and for lines).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/myersDiffAlgorithm.ts` | 12–105 | `MyersDiffAlgorithm.compute` |
| same | 106–176 | `SnakePath`, `FastInt32Array`, `FastArrayNegativeIndices<T>` |
| `src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/dynamicProgrammingDiffing.ts` | 14–107 | `DynamicProgrammingDiffing.compute` (with the optional `equalityScore` callback) |
| `src/vs/editor/common/diff/defaultLinesDiffComputer/utils.ts` | 10–25 | `Array2D<T>` |
| `src/vs/editor/test/node/diffing/defaultLinesDiffComputer.test.ts` | 18–29 | suite `myers` › `1` (it only checks that `compute` does not throw) |

## Files to create or change

- `src/LoomEditor.Core/Diff/Algorithms/MyersDiffAlgorithm.cs`, `DynamicProgrammingDiffing.cs`, `Array2D.cs` — create (`SnakePath` and the two fast arrays are `private` nested types of `MyersDiffAlgorithm`)
- `tests/LoomEditor.Core.Tests/Diff/Algorithms/MyersDiffAlgorithmTests.cs`, `DynamicProgrammingDiffingTests.cs` — create
- `tests/LoomEditor.Core.Tests/Diff/TestHelpers/ArraySequence.cs` — create (shared helper: an `ISequence` over a `string`'s chars or an `int[]`, `GetBoundaryScore` = 0, `IsStronglyEqual` = element equality)

## Design decisions (already made — do not change)

- Both classes implement `IDiffAlgorithm` (`DiffAlgorithmResult Compute(ISequence seq1, ISequence seq2, ITimeout? timeout = null)`; null → `InfiniteTimeout.Instance`). `DynamicProgrammingDiffing.Compute` has the extra optional parameter `Func<int, int, double>? equalityScore = null` (Monaco: a 4th argument).
- `FastInt32Array` / `FastArrayNegativeIndices<T>`: keep the structure (a positive and a negative `int[]` / `List<T>`, doubling growth) — it is part of the algorithm's speed. Use `int[]` and `Array.Resize`.
- Floating point: the DP scores are `double` (`Math.Log`-based callers); do not change to `float`.
- `Array2D<T>`: `Get(x, y)` / `Set(x, y, value)` over one flat `T[]` (`array[x + y * width]`), exactly like Monaco.
- Port the algorithms line by line; **do not simplify the loops** (the fixture tests of P9-10 depend on tie-breaking).
- `<remarks>Threading: stateless; safe to call from any thread, one call per instance at a time is not required.</remarks>`

## Tests

- [ ] Port: `myers` › `1` (build the two sequences with `ArraySequence`; Monaco uses `LinesSliceCharSequence`, which comes in P9-04: this test is re-added there with its real sequences, the name stays in the comment).
- [ ] New (both algorithms, same table of inputs, expected diffs as `SequenceDiff.ToString()` strings): `Compute_EqualSequences_NoDiffs`, `Compute_EmptyAndNonEmpty_OneDiff`, `Compute_BothEmpty_NoDiffs`, `Compute_InsertionInTheMiddle`, `Compute_DeletionAtTheStart`, `Compute_Replace`, `Compute_ResultsAreSortedAndNonOverlapping` (random sequences, `new Random(seed)` with the seed in the failure message; apply the diffs and check they transform seq1 into seq2), `Compute_TimeoutInvalid_HitTimeoutTrue` (`FakeTimeout`), `DynamicProgramming_EqualityScore_PrefersHigherScore`, `Array2D_GetSet`.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated: `myersDiffAlgorithm.ts`, `dynamicProgrammingDiffing.ts`, `utils.ts` (`Array2D`; `LineRangeFragment` comes in P9-09)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Heuristics (P9-05, P9-06), char sequences (P9-04), the line diff driver (P9-08).

## Suggested commit message

`feat(core): port the Myers and dynamic programming diff algorithms`
