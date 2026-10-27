# P9-09 — Moved lines: `ComputeMovedLines` and `computeMoves`

- **Status:** todo
- **Depends on:** P9-08
- **Size:** M

## Goal

With `ComputeMoves = true`, `DefaultLinesDiffComputer` also reports blocks of lines that were moved (not just deleted in one place and added in another), each with its own character level diff.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/diff/defaultLinesDiffComputer/computeMovedLines.ts` | 17–44 | `computeMovedLines` (the length filter: `>= 15` characters and `>= 2` lines of 2+ chars) |
| same | 45–54 | `countWhere` |
| same | 55–97 | `computeMovesFromSimpleDeletionsToSimpleInsertions` |
| same | 98–256 | `computeUnchangedMoves` (the three-line hash search) |
| same | 257–291 | `areLinesSimilar` |
| same | 292–317 | `joinCloseConsecutiveMoves` |
| same | 318–329 | `removeMovesInSameDiff` |
| `src/vs/editor/common/diff/defaultLinesDiffComputer/utils.ts` | 26–74 | `isSpace`, `LineRangeFragment` (`computeSimilarity`) |
| `src/vs/editor/common/diff/defaultLinesDiffComputer/defaultLinesDiffComputer.ts` | 175–204 | `DefaultLinesDiffComputer.computeMoves` (the `movesWithDiffs` mapping with `refineDiff`) |
| `src/vs/base/common/map.ts` | `SetMap<K, V>` | a small `SetMap` (a `Dictionary<K, HashSet<V>>` with `Add` and `Get`) |

## Files to create or change

- `src/LoomEditor.Core/Diff/ComputeMovedLines.cs`, `LineRangeFragment.cs` — create (`isSpace` is a `private static` of `LineRangeFragment`)
- `src/LoomEditor.Core/Diff/DefaultLinesDiffComputer.cs` — change: `ComputeMoves` filled in
- `tests/LoomEditor.Core.Tests/Diff/ComputeMovedLinesTests.cs`, `LineRangeFragmentTests.cs` — create

## Design decisions (already made — do not change)

- `static IReadOnlyList<LineRangeMapping> ComputeMovedLines(IReadOnlyList<DetailedLineRangeMapping> changes, string[] originalLines, string[] modifiedLines, int[] hashedOriginalLines, int[] hashedModifiedLines, ITimeout timeout)`.
- **Thread safety:** Monaco's `LineRangeFragment.chrKeys` is a **static** mutable map. The diff runs on a worker thread (ADR 0012 decision 3), so a static `Dictionary` would be a data race. Make it an instance the caller owns: `sealed class LineRangeFragmentContext { Dictionary<char, int> Keys }`, created once per `ComputeMovedLines` call and passed to every `LineRangeFragment`; the histogram indices are only compared inside one call, so results are identical. Behavior difference: none (document it in the class XML doc).
- Key strings of the three-line hash (`$"{a}:{b}:{c}"`) are kept (they are the `SetMap` keys); `changes.sort(compareBy(c => c.modified.startLineNumber))` sorts the **list passed in** in Monaco: sort a copy (`OrderBy` is stable, JS sort is stable too) — the caller's list must not be modified.
- `refineDiff` for a move uses the same private method as P9-08; `lineRangeMappingFromRangeMappings(…, true)` — the last argument is the `dontAssertStartLine` flag: read `rangeMapping.ts` 322–360 and make sure P9-07's port has it (add the parameter there if it was left out).
- `areLinesSimilar` uses the timeout argument as Monaco does (`timeout.isValid()`); a null-free `ITimeout` is always passed.
- `<remarks>Threading: stateless apart from the per-call context.</remarks>`

## Tests

- [ ] New: `ComputeMovedLines_NoChanges_NoMoves`, `ComputeMovedLines_BlockMovedDown_OneMove` (30 distinct lines of text, a 5-line block cut from the top and pasted at the bottom: one move `[1,6)->[26,31)`), `ComputeMovedLines_ShortBlock_Ignored` (a block under 15 characters), `ComputeMovedLines_TwoLinesOnlyOneLong_Ignored`, `ComputeMovedLines_TwoMovesCloseTogether_Joined`, `ComputeMovedLines_MoveInsideSameDiff_Removed`, `LineRangeFragment_ComputeSimilarity_IdenticalOne_DisjointZero`, `LineRangeFragment_ComputeSimilarity_Partial`, `DefaultLinesDiffComputer_ComputeMoves_ReportsMoveWithInnerChanges` (a moved block with one edited word → the move has `Changes` with one inner change), `DefaultLinesDiffComputer_ComputeMoves_False_NoMoves`. Remove the P9-08 test `ComputeDiff_ComputeMovesTrue_ReturnsEmptyMoves_UntilP9_09`.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `computeMovedLines.ts`, `utils.ts` (`LineRangeFragment`) and the "Behavior differences" row for the per-call key map
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Fixtures with moves (P9-10), drawing moved blocks (not in phase 9).

## Suggested commit message

`feat(core): port the moved lines computation of the diff`
