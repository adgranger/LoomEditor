# P9-06 — Diff heuristics (2/2): word extension and very short matches

- **Status:** done
- **Depends on:** P9-05
- **Size:** M

## Goal

The second half of the post-processing exists: extending a character diff to whole words (or sub-words) when that is more readable, merging diff lists, and dropping very short matching lines or text between large diffs.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/diff/defaultLinesDiffComputer/heuristicSequenceOptimizations.ts` | 222–300 | `extendDiffsToEntireWordIfAppropriate` (with its `findParent` callback and the `forSubWords` flag) |
| same | 301–324 | `mergeSequenceDiffs` |
| same | 325–371 | `removeVeryShortMatchingLinesBetweenDiffs` (takes `LineSequence`) |
| same | 372–473 | `removeVeryShortMatchingTextBetweenLongDiffs` (takes `LinesSliceCharSequence`) |

## Files to create or change

- `src/LoomEditor.Core/Diff/HeuristicSequenceOptimizations.Words.cs` — create (same `partial` class as P9-05)
- `tests/LoomEditor.Core.Tests/Diff/HeuristicSequenceOptimizationsWordsTests.cs` — create

## Design decisions (already made — do not change)

- Signatures: `ExtendDiffsToEntireWordIfAppropriate(LinesSliceCharSequence sequence1, LinesSliceCharSequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs, Func<LinesSliceCharSequence, int, OffsetRange?> findParent, bool forceSubWords = false)`; `RemoveVeryShortMatchingLinesBetweenDiffs(LineSequence, LineSequence, IReadOnlyList<SequenceDiff>)`; `RemoveVeryShortMatchingTextBetweenLongDiffs(LinesSliceCharSequence, LinesSliceCharSequence, IReadOnlyList<SequenceDiff>)`. `MergeSequenceDiffs` is private.
- The `do { … } while (shouldRepeat)` loop of `removeVeryShortMatchingLinesBetweenDiffs` and the `counter` (max 10 repeats) of `removeVeryShortMatchingTextBetweenLongDiffs` are kept as written.
- `removeVeryShortMatchingTextBetweenLongDiffs` uses `sequence.getText(range)` and a regex-free character count (`Monaco: .length`, `.trim().length`, `/[^\s]/`-style helpers: read the file): `LinesSliceCharSequence.GetText(OffsetRange)` exists since P9-04; for "non whitespace count" use `char.IsWhiteSpace` **only if** Monaco uses `\s` (it does: `trim()` and `\s`); document this in a comment.
- No behavior change: the thresholds (`5`, `20`, `130`, `2`, the `cap`, etc.) are copied from the source, not named.
- `<remarks>Threading: stateless.</remarks>`

## Tests

- [x] New (hand-built `LinesSliceCharSequence` / `LineSequence` inputs): `ExtendToWord_PartialWordChange_ExtendsToWholeWord` (`foobar` → `foobaz`: char diff `[5,6)` becomes `[0,6)` / `[0,6)`), `ExtendToWord_SubWords_CamelCase` (the `forceSubWords` variant), `ExtendToWord_NothingToExtend_Unchanged`, `RemoveVeryShortMatchingLines_MergesAcrossOneBlankLine`, `RemoveVeryShortMatchingLines_KeepsLongMatchingBlock`, `RemoveVeryShortMatchingText_MergesAcrossShortEqualText`, `RemoveVeryShortMatchingText_KeepsEqualTextBetweenSmallDiffs`, `Merge_InterleavesAndJoinsOverlaps` (through `ExtendDiffsToEntireWordIfAppropriate` results, since `Merge` is private).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated: `heuristicSequenceOptimizations.ts` (2/2) → ✅
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The driver that chains the heuristics (P9-08); fixtures (P9-10).

## Suggested commit message

`feat(core): port diff word extension and short-match heuristics`
