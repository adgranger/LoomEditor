# P6-12 — `CreateLineBreaksFromPreviousLineBreaks` (the resize fast path)

- **Status:** todo
- **Depends on:** P6-11
- **Size:** M

## Goal

When only the wrapping column changes (the control is resized), the factory computes the new breaks from the previous
ones, like Monaco, and the incremental tests prove both paths give the same result.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/monospaceLineBreaksComputer.ts` | 44–51 | the fast-path condition in `finalize` (`previousLineBreakData && !previousLineBreakData.injectionOptions && !injectedText && !isLineFeedWrappingEnabled`) |
| same | 52–53, 102–103 | `arrPool1` / `arrPool2` (see decisions) |
| same | 105–357 | `createLineBreaksFromPreviousLineBreaks` (the whole function) |
| `src/vs/editor/test/common/viewModel/monospaceLineBreaksComputer.test.ts` | 343–378 | `assertLineBreakDataEqual`, `assertIncrementalLineBreaks` (test helpers) |
| same | 380–436 | `MonospaceLineBreaksComputer incremental 1` |
| same | 438–448 | `issue #95686: CRITICAL: loop forever on the monospaceLineBreaksComputer` |
| same | 450–461 | `issue #110392: Occasional crash when resize with panel on the right` |

## Files to create or change

- `src/LoomEditor.View/Layout/MonospaceLineBreaksComputer.Incremental.cs` — create (partial of the static class; same header)
- `src/LoomEditor.View/Layout/MonospaceLineBreaksComputer.cs` — change: the factory's `Finalize` takes the fast path
- `tests/LoomEditor.View.Tests/Layout/TestHelpers/LineBreaksTestHelpers.cs` — change (add the two helpers)
- `tests/LoomEditor.View.Tests/Layout/MonospaceLineBreaksComputerIncrementalTests.cs` — create

## Design decisions (already made — do not change)

- `arrPool1` / `arrPool2` are module-level arrays reused between calls (and reset in `finalize`). LoomEditor uses two
  **local** `List<int>` / `List<double>` per call instead (no shared mutable static state; the factory may be used by several
  view models). The "returns `null` when nothing breaks" and "copy into the result" logic is kept as Monaco writes it
  (Monaco copies the pooled arrays with `slice`, which becomes `ToArray()`).
- The fast-path condition is `previous is not null && previous.InjectionOptions is null && injectedText is null && !isLineFeedWrappingEnabled`:
  `injectedText is null`, **not** `Count == 0` — JavaScript's `!injectedText` is `false` for an empty array, and the view model's
  context always returns a list (P6-17). Comment it: `// Monaco: !injectedText (an empty list is not null)`.
- The function is long: keep its structure (the `prevIndex` search, the `while` that walks the previous break points, the
  `forcedBreak` logic) and its comments; do not refactor into helpers.
- Variables that hold visible columns are `double` (see P6-09); `breakingColumn` math stays in `double`.
- `assertLineBreakDataEqual` compares `BreakOffsets` and `WrappedTextIndentLength` with `Assert.Equal` and the visible
  columns with a `< 0.001` tolerance, like Monaco.

## Tests

- [ ] Port: `MonospaceLineBreaksComputer incremental 1`
- [ ] Port: `issue #95686: CRITICAL: loop forever on the monospaceLineBreaksComputer` (give the test a 10 s timeout through
      xUnit v3 `[Fact(Timeout = 10000)]` with an `async Task` body, so an infinite loop fails instead of hanging)
- [ ] Port: `issue #110392: Occasional crash when resize with panel on the right` (`columnsForFullWidthChar = 1.6605405405405405`)
- [ ] New: `Finalize_WithPreviousDataAndInjectedText_UsesTheFullComputation` (pass previous data **and** injected text: the
      result must equal `CreateLineBreaks`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: the `monospaceLineBreaksComputer.ts` map row is ✅ (fast path ported, pools replaced by locals —
      add that to "Behavior differences"); the test count is updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The two-cell tests (P6-13).

## Suggested commit message

`feat(view): compute line breaks from the previous ones on resize`
