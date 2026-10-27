# P9-32 — Native AOT self-check of the diff stack and diff benchmarks

- **Status:** todo
- **Depends on:** P9-31
- **Size:** M

## Goal

The published (Native AOT) Gallery can prove, without a human, that the diff algorithm, the document diff provider and the diff view model work in the AOT build (`--diff-selfcheck=<file>` writes a pass / fail report and exits 0 or 1); and BenchmarkDotNet numbers for the diff computation exist in `docs/performance.md`.

## Monaco sources

Sources: none (specified in this card; the pattern is P8-37's `--lsp-selfcheck` and ADR 0010's `UiaSelfCheck`).

## Files to create or change

- `samples/LoomEditor.Gallery/DiffSelfCheck.cs` — create (`internal static class`; **no WinUI types** except the dispatcher it is given, so the checks run on the UI thread of the real app)
- `samples/LoomEditor.Gallery/MainWindow.Diff.cs` — change: the `--diff-selfcheck=<file>` argument: run `DiffSelfCheck.RunAsync` after the window is loaded, write the report, `Application.Current.Exit()` with the exit code (copy how `MainWindow.Lsp.cs` handles `--lsp-selfcheck=`: read it first, same file argument rules, same hang guard / timeout)
- `tests/LoomEditor.Benchmarks/DiffBenchmarks.cs` — create
- `docs/performance.md` — change: a "Diff" section with the measured numbers (dated, with the machine line the file already uses)
- `tests/LoomEditor.View.Tests/DiffEditor/DiffSelfCheckTests.cs` — create (links `DiffSelfCheck.cs` and `DiffSamples.cs` and runs the checks against a synchronous test dispatcher: the pure checks must pass in the unit test run too)

## Design decisions (already made — do not change)

- Checks (each one `PASS name — detail` or `FAIL name — reason`; an exception is a FAIL, never a crash): `algorithm` (`DefaultLinesDiffComputer` on `DiffSamples` gives the expected number of changes and the same text when the inner changes are applied to the original — the `assertDiffCorrectness` of P9-10), `moves` (`ComputeMoves = true` runs without exception and returns a list), `large` (two generated 3 000-line texts with 1 % changed finish within 5 s: uses the Myers path), `timeout` (`MaxComputationTimeMs = 1` on the large texts completes and reports `QuitEarly == true` **or** finished in time: both are acceptable, only an exception fails), `provider` (`DocumentDiffProvider.ComputeDiffAsync` on two `TextModel`s completes on the pool thread and returns the same changes as the direct call), `provider-cancel` (a cancelled token → a cancelled / quit-early result), `viewmodel` (a `DiffEditorViewModel` on two models with the real dispatcher: `WaitForDiffAsync` completes, `Diff` is set; an edit marks it stale and a new diff arrives; `HideUnchangedRegions` gives regions), `alignment` (`LineRangeAlignments.Compute` on that diff returns blocks whose heights add up: every original range is covered), `accessible-model` (the accessible viewer model builds groups and the header label of group 1 equals the expected text).
- Report: lines `PASS|FAIL  <name> — <detail>` then `SUMMARY: <n> passed, <m> failed`, written to the file named by the argument (UTF-8); exit code 0 when `m == 0`.
- **AOT specifics to watch**: `Task.Run` over delegates, `Dictionary<string,int>` hashing, `JsonDocument` is **not** used in the Gallery (the fixtures are a test-only thing): do not add any. If a check fails only in the published build, the card's failure report must include the trimmed / AOT warning list (`dotnet publish ... -warnaserror` output) — it points to the cause.
- **Benchmarks** (`DiffBenchmarks`, BenchmarkDotNet, same conventions as `ViewportBenchmarks.cs` of P2-22): `[Params]` on line counts 200 / 2 000 / 20 000 with 1 % changed lines (generated with a fixed seed, `new Random(42)`), methods `ComputeDiff` (default options), `ComputeDiff_WithMoves`, `ComputeDiff_IgnoreWhitespaceOff`, and `DocumentDiffProvider_TwoModels` (models of the same size). Record mean time and allocated memory in `docs/performance.md`; the **target** to write there: 2 000 lines / 1 % changes under 100 ms, 20 000 lines under 1 s on the reference machine (these are the card's targets, **not** upstream numbers; if the measurement misses them, say so in the card result and open a follow-up card in the index, do not tune the algorithm here).
- `<remarks>Threading: UI thread only (the checks that use models and the dispatcher).</remarks>`

## Tests

- [ ] New: `SelfCheck_Algorithm_Passes`, `SelfCheck_Large_Passes`, `SelfCheck_ProviderMatchesDirectComputation`, `SelfCheck_AccessibleModelLabel`, `SelfCheck_AllChecks_Pass_WithSynchronousDispatcher`, `Report_Format_PassFailAndSummary`.
- [ ] Not automated: the published app run — `Gallery.exe --diff-selfcheck=%TEMP%\diff-selfcheck.txt` after `dotnet publish -p:PublishAot=true`; paste the report in the card result.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` and the self-check run — result stated (`PASS` lines or `NOT verified` with the reason, never ticked if not run)
- [ ] `dotnet run --project tests/LoomEditor.Benchmarks -c Release -- --filter "*Diff*"` — numbers in `docs/performance.md` (or `NOT measured` with the reason)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Tuning the algorithm, UI automation tests of the diff control.

## Suggested commit message

`test(gallery): add the diff self-check and benchmarks`
