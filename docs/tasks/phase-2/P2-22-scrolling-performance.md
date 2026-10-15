# P2-22 — Scrolling performance: benchmarks and reference numbers

- **Status:** done
- **Depends on:** P2-21
- **Size:** S

## Goal

Measure, and write down, that phase 2 meets its target: a 100 MB document opens with a first screen in < 1 s and
scrolls at 60 fps (`docs/performance.md`, "Targets"). There are two kinds of measures. BenchmarkDotNet measures the
per-frame CPU work that does not depend on DirectWrite. The Gallery scroll test (P2-21) measures real frames.

## Monaco sources

None.

## Files to create or change

- `tests/LoomEditor.Benchmarks/LoomEditor.Benchmarks.csproj` — reference `LoomEditor.View`
- `tests/LoomEditor.Benchmarks/ViewportBenchmarks.cs` — create
- `docs/performance.md` — new section "Scrolling (card P2-22)" with both tables, machine, commit, commands

## Design decisions (already made — do not change)

- Benchmarks use the same 100 MB text as `Open100MB` of P1-19: `string.Concat(PieceTreeBenchmarkData.GenerateChunks(...))`
  (reuse it, do not write a second generator). `ViewModel` is `internal`; `LoomEditor.Benchmarks` already sees the
  internals of `src/` projects (`src/Directory.Build.props`).
  - `OpenAndLayout100MB`: `new TextModel(text)` + `new ViewModel(...)` + `SetViewportSize(1600, 1000)` + one
    `GetViewportData`. Reported next to `Open100MB` to show the layout overhead.
  - `ScrollFrame100MB`: 1,000 successive positions (`ScrollTop += 57`, then jumps to 25 random positions with
    `new Random(42)`). Per position: `SetScrollPosition(Immediate)`, `GetViewportData`, and `GetLineContent` (or the
    10,000-character prefix) of each visible line, which is what the renderer reads. Report the time **per frame**.
  - `EditWhileScrolled`: one insert of `"x\n"` in the middle of the document, then one `GetViewportData`. Shows that
    an edit does not rebuild the layout.
  - `[MemoryDiagnoser]` on the class.
- Pass criteria, checked by hand and written in `docs/performance.md`:
  - `ScrollFrame100MB` < 1 ms per frame;
  - `OpenAndLayout100MB` < 1 s (it is the "first screen" target, minus DirectWrite);
  - Gallery scroll test on the large file, AOT build, at 100 % scale, maximized window: average ≥ 58 fps,
    p95 ≤ 20 ms, no interval > 50 ms after the first second.
- If a criterion is not met, **stop**. Commit the benchmarks and the numbers anyway, mark the card `blocked` in the
  index, and propose a follow-up card with a profile (dotnet-trace or Visual Studio profiler) that shows where the
  time goes. Do not optimize in this card.

## Tests

- [x] Benchmarks run: `dotnet run -c Release --project tests/LoomEditor.Benchmarks -- --filter "*Viewport*"`
- [x] Gallery scroll test run 3 times on the 100 MB file; the median run is recorded

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/performance.md` has the benchmark table, the Gallery scroll test numbers, the machine and the commit
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Optimizations (follow-up cards if needed), GPU timing, swap-chain experiments (ADR 0005, consequences).

## Suggested commit message

`perf(view): add viewport benchmarks and phase 2 scrolling numbers`
