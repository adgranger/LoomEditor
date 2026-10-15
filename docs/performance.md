# Performance

## Targets

| Scenario | Target | Phase |
|---|---|---|
| Open a 100 MB file | the first screen is displayed in < 1 s; scrolling stays at 60 fps | 1–2 |
| 10,000 random edits on a 1 MB document | < 100 ms total, no allocation per edit beyond the inserted text | 1 |
| Tokenize a 50,000-line file | in the background, never blocking typing; visible lines first | 4 |

## How to run the benchmarks

```powershell
dotnet run -c Release --project tests/LoomEditor.Benchmarks -- --filter "*"
dotnet run -c Release --project tests/LoomEditor.Benchmarks -- --filter "*Range*"
```

Results go to `BenchmarkDotNet.Artifacts/` (ignored by git). Copy the summary table here when it is a new reference.

## Reference results

### Piece tree (card P1-19)

- **Machine:** 11th Gen Intel Core i7-1165G7 @ 2.80 GHz (4 cores / 8 threads), 31.7 GB RAM, Windows 11 25H2 (10.0.26200)
- **Runtime:** .NET SDK 11.0.100-rc.1.26425.128, .NET 11.0.0-rc.1 (X64 RyuJIT x86-64-v4), BenchmarkDotNet 0.16.0-preview.2
- **Commit:** `7cbb33d` (+ the benchmarks of P1-19)
- **Command:** `dotnet run -c Release --project tests/LoomEditor.Benchmarks -- --filter "*PieceTree*"`

| Benchmark | Mean | Allocated | Per operation |
|---|---:|---:|---|
| `Open100MB` (100 MB, 64 KB chunks) | 212.7 ms (StdDev 14 ms, 3 iterations) | 218.28 MB | — |
| `RandomEdits10K` (1 MB buffer) | 11.85 ms | 9.22 MB | ~1.2 µs, ~940 B per edit |
| `TypeAtEnd10K` (1 MB buffer) | 1.99 ms | 6.47 MB | ~0.2 µs, ~650 B per typed character |
| `GetLineContentRandom` (100 MB buffer, 100,000 calls) | 33.29 ms | 22.57 MB | ~0.33 µs, ~225 B (the returned string) |
| `GetPositionAtRandom` (100 MB buffer, 100,000 calls) | 20.21 ms | 0 B | ~0.2 µs |

Reading of the numbers:

- The time targets of the table above are met: `Open100MB` is well below 1 s and 10,000 random edits take ~12 ms
  (target: < 100 ms).
- Reads and edits are sub-linear: they stay in the microsecond range on a 100 MB / 1 MB buffer.
- **The "no allocation per edit beyond the inserted text" target is not met**: every edit allocates 650–940 bytes
  (typing one character allocates ~650 B). The edits go through `PieceTreeTextBuffer.ApplyEdits`, which builds
  operation lists, content-change records and line-start arrays. This was measured and partly reduced by follow-up card
  [P1-31](tasks/phase-1/P1-31-edit-allocations.md), see below.
- The mutating benchmarks rebuild the buffer in `[IterationSetup]`, so each iteration is a single invocation
  (`InvocationCount=1`); their numbers include only the edits.
- The benchmark project uses BenchmarkDotNet 0.16.0-preview.2: 0.15.8 cannot run benchmarks on .NET 11
  (`GetRuntimeVersion not implemented for NotRecognized`).

### Allocations per edit (card P1-31)

Before/after on the same machine and runtime as above (`dotnet run -c Release --project tests/LoomEditor.Benchmarks -- --filter "*EditBenchmarks*"`).
`PieceTreeDirectEditBenchmarks` calls `PieceTreeBase.Insert`/`Delete` directly, to separate the tree from `ApplyEdits`.

| Benchmark | Before | After |
|---|---:|---:|
| `RandomEdits10K` (through `ApplyEdits`) | 9.22 MB (~940 B/edit) | 8.07 MB (~846 B/edit) |
| `TypeAtEnd10K` (through `ApplyEdits`) | 6.47 MB (~650 B/char) | 5.33 MB (~533 B/char) |
| `DirectRandomEdits10K` (tree only) | 3.93 MB (~393 B/edit) | unchanged |
| `DirectTypeAtEnd10K` (tree only) | 1.44 MB (~144 B/char) | unchanged |

What was removed (no behavior change): the candidate list for auto-whitespace trimming is only allocated when
`recordTrimAutoWhitespace` is set; `DoApplyEdits` sorts the operation list in place instead of copying it; the list of
content changes is pre-sized.

Why the target "no allocation per edit beyond the inserted text" is still **not fully met**:

- The tree itself allocates ~144 B per typed character (a new piece and a red-black tree node) and ~393 B per random
  edit (a split creates several pieces and nodes). This is the piece tree algorithm and must not change.
- The rest of the `ApplyEdits` cost (~290 B for one edit, of which ~96 B come from the benchmark building its
  `ValidAnnotatedEditOperation` and a one-element array) is the objects that make up the result: the validated
  operation, the lists, `ModelContentChange`, `InternalModelContentChange` and `ApplyEditsResult`. They are part of the
  `ITextBuffer.ApplyEdits` contract that `TextModel` consumes, and the card forbids changing that surface.
- Removing them would need a struct-based result or a single-edit fast path; that is a larger change, to be decided
  when phase 3 (typing) shows it matters. Time is far below target anyway (~1.2 µs per random edit).

### Scrolling (card P2-22)

Per-frame CPU work that does not depend on DirectWrite (`ViewportBenchmarks`), and real frames measured by the Gallery
scroll test (card P2-21).

- **Machine:** 11th Gen Intel Core i7-1165G7 @ 2.80 GHz (4 cores / 8 threads), 31.7 GB RAM, Windows 11 25H2 (10.0.26200)
- **Runtime:** .NET SDK 11.0.100-rc.1.26425.128, .NET 11.0.0-rc.1 (X64 RyuJIT x86-64-v4), BenchmarkDotNet 0.16.0-preview.2
- **Commit:** `037610a` (+ the benchmarks of P2-22)
- **Command:** `dotnet run -c Release --project tests/LoomEditor.Benchmarks -- --filter "*Viewport*"`

| Benchmark | Mean | Allocated | Criterion |
|---|---:|---:|---|
| `OpenAndLayout100MB` (`new TextModel` + `ViewModel` + viewport 1600×1000 + one `GetViewportData`) | 459 ms (StdDev 58 ms, 3 iterations) | 226,250 KB | < 1 s: met |
| `ScrollFrame100MB` (per frame: 1,000 steps of 57 DIPs + 25 random jumps; scroll, viewport data, read of every visible line) | 9.46 µs per frame | 18.7 KB per frame | < 1 ms: met |
| `EditWhileScrolled` (insert `"x\n"` in the middle, then one `GetViewportData`) | 4.24 µs | 11.1 KB | none: the layout is not rebuilt |

Reading of the numbers:

- `OpenAndLayout100MB` includes building the text model (`Open100MB` above is ~213 ms for the buffer alone), so the
  view model and the first viewport add ~250 ms, well under the 1 s target. DirectWrite is not included.
- A frame costs ~9.5 µs of CPU, ~100 times below the 1 ms criterion and far below the 16.7 ms frame budget.
- An edit in the middle of a 100 MB document does not rebuild the layout: edit plus frame take ~4 µs.

#### Gallery scroll test (AOT build)

Criteria: average ≥ 58 fps, p95 ≤ 20 ms, no interval > 50 ms after the first second.
`dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64`, "Large file (100 MB)", then "Scroll test"
(maximized window, 100 % scale), three runs; the median run is recorded.

| Run | Frames | Average | p95 | Max | > 33 ms |
|---|---:|---:|---:|---:|---:|
| 1 (median) | 601 | 60.0 fps | 17.0 ms | 18.5 ms | 0 |
| 2 | 600 | 60.0 fps | 17.0 ms | 18.2 ms | 0 |
| 3 | 600 | 60.0 fps | 17.0 ms | 20.4 ms | 0 |

All three runs meet the criteria (average ≥ 58 fps, p95 ≤ 20 ms, no interval > 50 ms: the largest is 20.4 ms, including
the first second). The median run is the one with the median longest interval. The frame rate is capped at the 60 Hz
refresh rate of the display. The intervals are read with a `Stopwatch` in each `CompositionTarget.Rendering` callback:
casting the event arguments to read `RenderingTime` fails in a Native AOT build.
