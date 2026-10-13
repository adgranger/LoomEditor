# P1-19 — Piece tree benchmarks and reference numbers

- **Status:** todo
- **Depends on:** P1-18
- **Size:** S

## Goal

Measure the piece tree on the scenarios of `docs/performance.md`, record reference numbers, and find out whether
anything is unexpectedly slow. **No optimization in this card** — only measurement and a short report.

## Monaco sources

None (Monaco's own benchmarks are browser-based and not ported).

## Files to create or change

- `tests/LoomEditor.Benchmarks/PieceTreeBenchmarks.cs` — create
- `tests/LoomEditor.Benchmarks/LoomEditor.Benchmarks.csproj` — nothing to add if it already references Core (it does)
- `docs/performance.md` — add the results table

## Design decisions (already made — do not change)

Benchmarks (`[MemoryDiagnoser]`, data generated in `[GlobalSetup]` with a fixed-seed `Random`):

| Benchmark | What it does |
|---|---|
| `Open100MB` | build a buffer from a generated 100 MB text (lines of 40–120 chars) through the builder in 64 KB chunks |
| `RandomEdits10K` | on a 1 MB buffer: 10,000 random inserts/deletes of 1–10 chars |
| `TypeAtEnd10K` | 10,000 single-character inserts at the end (simulates typing) |
| `GetLineContentRandom` | 100,000 `GetLineContent` calls on random lines of the 100 MB buffer |
| `GetPositionAtRandom` | 100,000 `GetPositionAt` calls on random offsets |

- `Open100MB` is slow by nature: use `[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]` for it.
- Write the machine (CPU, RAM, OS), .NET SDK version and commit in `docs/performance.md` next to the numbers.
- If something looks wrong (e.g. typing allocates far more than the typed text, or any operation is not
  sub-linear), **open a follow-up card** in `docs/tasks/phase-1/` describing it; do not fix it here.

## Tests

- [ ] `dotnet run -c Release --project tests/LoomEditor.Benchmarks -- --filter "*PieceTree*"` runs to completion

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] Results table in `docs/performance.md`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Optimizations; rendering performance (phase 2).

## Suggested commit message

`perf(core): add piece tree benchmarks and reference results`
