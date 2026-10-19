# P4-49 — Tokenization benchmarks and reference numbers

- **Status:** todo
- **Depends on:** P4-48
- **Size:** S

## Goal

We know how fast we tokenize, compared with nothing yet but our own future: lines per second for each language, the
cost of the viewport tokenization on the UI thread, and the time for the worker to finish a large file. The numbers go
into `docs/performance.md`.

## Sources

None to port. `tests/LoomEditor.Benchmarks/` (existing BenchmarkDotNet project, P1-19, P2-22) and `docs/performance.md`.

## Files to create or change

- `tests/LoomEditor.Benchmarks/Tokenization/GrammarBenchmarks.cs` — create (`TokenizeLine2` over each golden sample repeated to ~10 000 lines)
- `tests/LoomEditor.Benchmarks/Tokenization/ViewportTokenizationBenchmarks.cs` — create (`SetVisibleLines` for 60 lines in the middle of a 100 000-line C# file, heuristic path)
- `tests/LoomEditor.Benchmarks/Tokenization/WorkerBenchmarks.cs` — create (time until `BackgroundTokenizationState.Completed` for a 100 000-line C# file)
- `docs/performance.md` — a "Tokenization (phase 4)" section with the machine, the commands and the numbers

## Design decisions (already made — do not change)

- Measure, do not optimize, in this card (PORTING.md, "Don't optimize while porting"). If the viewport tokenization
  of 60 lines takes more than 8 ms, write a follow-up card proposal in the report instead of changing code.
- `[MemoryDiagnoser]` on every benchmark.

## Tests

None (benchmarks). `dotnet run -c Release --project tests/LoomEditor.Benchmarks -- --filter *Tokenization*`.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/performance.md` updated with the numbers
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Optimizations.

## Suggested commit message

`perf(textmate): benchmark tokenization`
