# P9-08 — `DefaultLinesDiffComputer` (without moved lines)

- **Status:** done
- **Depends on:** P9-07
- **Size:** M

## Goal

`new DefaultLinesDiffComputer().ComputeDiff(originalLines, modifiedLines, options)` returns Monaco's line and character changes (moves are computed by P9-09 and plugged in there).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/diff/defaultLinesDiffComputer/defaultLinesDiffComputer.ts` | 23–174 | `DefaultLinesDiffComputer.computeDiff` (the small-file shortcuts, line hashing, the `< 1700` switch between DP and Myers, `scanForWhitespaceChanges`, the final `assertFn` validation) |
| same | 175–204 | `computeMoves` (**not ported here**: a private method that returns an empty list; P9-09 fills it) |
| same | 205–260 | `refineDiff` (the character pass: `< 500` switch, the heuristic chain, `extendToSubwords`) |
| same | 266–271 | `toLineRangeMapping` |
| `src/vs/editor/common/diff/linesDiffComputers.ts` | 1–16 | `linesDiffComputers.getDefault()` |

## Files to create or change

- `src/LoomEditor.Core/Diff/DefaultLinesDiffComputer.cs` — create
- `src/LoomEditor.Core/Diff/LinesDiffComputers.cs` — create (`static ILinesDiffComputer GetDefault()`)
- `tests/LoomEditor.Core.Tests/Diff/DefaultLinesDiffComputerTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class DefaultLinesDiffComputer : ILinesDiffComputer`. It holds one `DynamicProgrammingDiffing` and one `MyersDiffAlgorithm` (fields, as in Monaco).
- The perfect hash is a `Dictionary<string, int>` keyed by the trimmed line. JavaScript `trim()` and `string.Trim()` differ (`.NET` also trims U+0085 and U+001C–U+001F, JS also trims U+FEFF): use a private `TrimJs(string)` helper that trims exactly `	 
 v  `, space, U+00A0, U+1680, U+2000–U+200A, U+2028, U+2029, U+202F, U+205F, U+3000 and U+FEFF, so there is no behavior difference.
- The `equalityScore` lambda passed to the DP algorithm uses `Math.Log(1 + length)` (double). Keep `0.1`, `0.99` and `1 + Math.Log(1 + len)`.
- `maxComputationTimeMs == 0` → `InfiniteTimeout.Instance`, otherwise `new DateTimeout(ms)`.
- The `assertFn` validation block becomes `Guard.Assert(() => …)` if `Guard.Assert` accepts a lambda; otherwise a private `static bool ValidateChanges(...)` called inside `Guard.Assert(ValidateChanges(...))` (Debug only, as in `CLAUDE.md`).
- `hitTimeout` is carried exactly as in Monaco (`lineAlignmentResult.hitTimeout`, then OR-ed with the character passes).
- `<remarks>Threading: the instance only holds two stateless algorithm objects, so ComputeDiff may be called concurrently.</remarks>`

## Tests

- [x] New (expected values are computed by hand or taken from the fixture dirs of P9-10 `1.txt` / `2.txt` of `equals`, `deletion`; do **not** copy the output of your own implementation as expectation): `ComputeDiff_BothEmptySingleLine_NoChanges`, `ComputeDiff_IdenticalLines_NoChanges`, `ComputeDiff_OriginalEmptyFile_WholeFileAdded` (the shortcut at lines 31–43 returns one mapping with one inner change), `ComputeDiff_InsertedLine_OneLineMapping` (`[2,2)->[2,3)`), `ComputeDiff_DeletedLine`, `ComputeDiff_ChangedWord_InnerChangeCoversWord`, `ComputeDiff_IgnoreTrimWhitespace_TrimmedEqualLinesNotReported`, `ComputeDiff_WhitespaceOnlyChange_Reported_WhenNotIgnoring`, `ComputeDiff_LargeInput_UsesMyersPath` (two 1000-line inputs differing in 3 lines: three changes), `ComputeDiff_TimeoutZero_NoTimeout`, `ComputeDiff_ChangesAreSortedAndMappingsConsistent` (random edits on random lines: for every change the number of unchanged lines before it is equal in both files; seeds in messages), `ComputeDiff_ComputeMovesTrue_ReturnsEmptyMoves_UntilP9_09` (deleted by P9-09).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated: `defaultLinesDiffComputer.ts`, `linesDiffComputers.ts` (`getDefault` only: no `getLegacy`)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Moves (P9-09), Monaco's fixture tests (P9-10), the legacy computer.

## Suggested commit message

`feat(core): port DefaultLinesDiffComputer`
