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

None yet: the first reference results will be recorded with the piece tree (phase 1).
Always record the machine (CPU, RAM, OS), the .NET SDK version and the commit.
