# P12-03 — Suggestion diff rows for N old lines

- **Status:** todo
- **Depends on:** P12-02
- **Size:** S

**Recommended model:** Sonnet

## Goal

`SuggestionDiff.Compute` compares a list of old lines (the range) to the lines of a suggestion block and returns `Context` / `Removed` / `Added` rows from a line LCS.
For one old line the rows are exactly those of phase 11.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decision 8). Read first: `src/LoomEditor.View/Comments/SuggestionDiff.cs` (the single-line rules:
removed first, the **first** equal new line is `Context`), `tests/LoomEditor.View.Tests/Comments/SuggestionDiffTests.cs`, `src/LoomEditor.View/Comments/CommentStrings.cs`.

## Files to create or change

- `src/LoomEditor.View/Comments/SuggestionDiff.cs` — change
- `src/LoomEditor.View/Comments/CommentStrings.cs` — change: add `public const string DescribeLinesDeleted = "Lines deleted.";`
- `tests/LoomEditor.View.Tests/Comments/SuggestionDiffTests.cs` — change (add tests; keep every existing test)

## Design decisions (already made — do not change)

- New `static SuggestionDiffResult Compute(IReadOnlyList<string> oldLines, SuggestionBlock block)`; the existing `Compute(string oldLine, SuggestionBlock block)` becomes
  `=> Compute([oldLine], block)` and its rows must stay identical (the existing tests prove it). `oldLines` has at least one item (`ArgumentException` when empty).
- Rules, in order (`lines = block.Lines`, comparison ordinal):
  1. `lines.Count == 0` → one `Removed` row per old line, `IsDeletion = true`, `IsNoOp = false`.
  2. `lines` equals `oldLines` item by item → one `Context` row per line, `IsNoOp = true`.
  3. Otherwise, a **suffix LCS table** `int[n + 1, m + 1]` (`n = oldLines.Count`, `m = lines.Count`; `dp[i, j]` = LCS length of `oldLines[i..]` and `lines[j..]`), then a
     forward walk from `(0, 0)`: equal lines → `Context`, `i++`, `j++`; else if `dp[i + 1, j] >= dp[i, j + 1]` → `Removed(oldLines[i])`, `i++`; else `Added(lines[j])`, `j++`;
     then the rest of the old lines as `Removed` and the rest of the new lines as `Added`. This emits removals before additions in each gap and matches the **first** equal
     line, which is what phase 11 did for one line.
  4. Guard: when `(long)n * m > 250_000`, skip the table: all old lines `Removed`, then all new lines `Added` (a comment never holds that much; the guard bounds memory).
- `SuggestionDiffResult.Describe()`: when `IsDeletion` and more than one row is removed, end with `CommentStrings.DescribeLinesDeleted` instead of `DescribeLineDeleted`;
  everything else unchanged (removed rows then "Added: a; b.").
- `SuggestionDiffResult` keeps its shape (`Rows`, `IsNoOp`, `IsDeletion`); no intra-line highlights. XML doc of `SuggestionDiff`: "Computes the diff of a range of lines
  replaced by a suggestion block (line LCS)."

## Tests

- [ ] `Compute_ThreeLinesOneChanged_ContextRemovedAddedContext` (old `a b c`, new `a X c` → `a`, `-b`, `+X`, `c`)
- [ ] `Compute_RangeDeletion_AllRemovedAndIsDeletion`, `Compute_RangeIdentical_IsNoOpAllContext`
- [ ] `Compute_LinesInserted_AddedBetweenContext` (old `a c`, new `a b c`), `Compute_AllDifferent_RemovedThenAdded`
- [ ] `Compute_SingleOldLine_SameRowsAsPhase11` (the string overload and the list overload give equal rows for 4 inputs)
- [ ] `Compute_HugeInput_FallsBackWithoutContext` (n = 600, m = 600)
- [ ] `Compute_EmptyOldLines_Throws`
- [ ] `Describe_RangeDeletion_SaysLinesDeleted`
- [ ] All existing `SuggestionDiffTests` unchanged and green

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Reusing the diff editor's `DefaultLinesDiffComputer` (too heavy for a comment, and its output is not rows), intra-line highlights, the WinUI view (P12-11).

## Suggested commit message

`feat(view): diff a suggestion against a range of lines`
