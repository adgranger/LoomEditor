# P1-31 — Reduce per-edit allocations in `ApplyEdits`

- **Status:** done
- **Depends on:** P1-19
- **Size:** S

## Goal

Meet the target of `docs/performance.md` ("no allocation per edit beyond the inserted text") or document precisely
why it cannot be met. Found by the benchmarks of P1-19: `TypeAtEnd10K` allocates ~650 B per typed character and
`RandomEdits10K` ~940 B per edit (1 MB buffer), for a 1-character / 1–10-character payload.

## Monaco sources

None: this is about the C# implementation only. Monaco's algorithm must not change.

## Files to create or change

- `src/LoomEditor.Core/Model/PieceTree/PieceTreeTextBuffer.ApplyEdits.cs` — change (allocation hot spots only)
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeBase.Insert.cs` / `PieceTreeBase.Delete.cs` — only if profiling shows allocations there
- `tests/LoomEditor.Benchmarks/PieceTreeBenchmarks.cs` — add a benchmark that calls `PieceTreeBase.Insert`/`Delete` directly, to
  separate the cost of the tree from the cost of `ApplyEdits`
- `docs/performance.md` — update the numbers

## Design decisions (already made — do not change)

- First measure where the bytes come from (`[MemoryDiagnoser]` + the benchmark above, or a allocation profiler); fix
  only what the measurement shows.
- Behavior must not change: all existing tests (including the randomized ones) stay green and unmodified.
- Do not change the public/internal API surface of `ITextBuffer`.

## Tests

- [x] All existing tests pass unchanged
- [x] `TypeAtEnd10K` allocation per character is reduced, or the remaining allocations are explained in `docs/performance.md`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Before/after numbers in `docs/performance.md`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Rendering performance (phase 2); changing the text-buffer algorithm.

## Suggested commit message

`perf(core): reduce allocations per edit in PieceTreeTextBuffer.ApplyEdits`
