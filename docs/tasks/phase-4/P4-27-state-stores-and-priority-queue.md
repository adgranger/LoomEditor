# P4-27 — Core model tokens: `FixedArray`, tokenization state stores, `RangePriorityQueueImpl`

- **Status:** done
- **Depends on:** P4-26
- **Size:** M

## Goal

The model can remember the end state of every line and which lines are invalid, follow edits, and keep a queue of line
ranges to tokenize first. `textModelTokens.test.ts` passes.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/common/model/fixedArray.ts` | 12–77 | `FixedArray<T>`, `arrayFill` |
| vscode: `src/vs/editor/common/model/textModelTokens.ts` | 216–325 | `TrackingTokenizationStateStore<TState>`, `TokenizationStateStore<TState>` |
| vscode: same file | 327–423 | `RangePriorityQueue`, `RangePriorityQueueImpl` |
| vscode: `src/vs/editor/common/core/ranges/offsetRange.ts` | 8–228 | `IOffsetRange`, `OffsetRange` — only the members used by the code of this card and of P4-28 … P4-38 (at least `fromTo`, `addRange`, constructor, `isEmpty`, `length`, `delta`, `contains`, `intersects`, `intersect`, `join`, `equals`, `toString`); add the others when a later card needs them |
| vscode: `src/vs/editor/test/common/model/textModelTokens.test.ts` | 11–99 | `'addRange'`, `'addRangeAndResize'` |

## Files to create or change

- `src/LoomEditor.Core/Model/Tokens/FixedArray.cs`, `TokenizationStateStore.cs`, `TrackingTokenizationStateStore.cs`, `RangePriorityQueue.cs` — create
- `src/LoomEditor.Core/Text/OffsetRange.cs` — create (`readonly record struct`; `addRange` is a static method on a `List<OffsetRange>`)
- `tests/LoomEditor.Core.Tests/Model/Tokens/RangePriorityQueueTests.cs`, `TokenizationStateStoreTests.cs` — create

## Design decisions (already made — do not change)

- `OffsetRange` throws `ArgumentException` when `start > endExclusive`, as Monaco's `BugIndicatingError`.
- `TState` is constrained to `class, IState`; "no state" is `null`.
- `acceptChanges` takes `IReadOnlyList<ModelContentChange>` and counts lines with `EolCounter`.

## Tests

- [x] Port: `'RangePriorityQueueImpl'` › `'addRange'`, `'addRangeAndResize'`
- [x] New: `TrackingStore_SetEndStateChanged_InvalidatesNextLine`
- [x] New: `TrackingStore_AcceptChange_InsertedLinesAreInvalid`
- [x] New: `FixedArray_Replace_GrowsAndShrinks`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map and ported-tests list updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Tokenizers that use the stores (P4-28).

## Suggested commit message

`feat(core): port tokenization state stores and the range priority queue`

## Notes (done)

- Monaco's `LineRange` does not exist in Core: line ranges are `OffsetRange`s of line numbers (`Start`, `EndExclusive`,
  `Length`), which is exactly what the stores use.
- `RangePriorityQueueImpl` is `RangePriorityQueue` (the private upstream interface is not ported).
