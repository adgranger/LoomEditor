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
  operation lists, content-change records and line-start arrays. This is tracked by follow-up card
  [P1-31](tasks/phase-1/P1-31-edit-allocations.md); nothing was optimized in P1-19.
- The mutating benchmarks rebuild the buffer in `[IterationSetup]`, so each iteration is a single invocation
  (`InvocationCount=1`); their numbers include only the edits.
- The benchmark project uses BenchmarkDotNet 0.16.0-preview.2: 0.15.8 cannot run benchmarks on .NET 11
  (`GetRuntimeVersion not implemented for NotRecognized`).
