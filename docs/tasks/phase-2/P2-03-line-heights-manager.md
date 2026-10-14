# P2-03 — `LineHeightsManager`

- **Status:** done
- **Depends on:** P2-02
- **Size:** M

## Goal

Port Monaco's `LineHeightsManager`. It knows the height of every line (a default height, plus "custom" heights set by
decorations) and gives the accumulated height up to a line. `LinesLayout` (P2-04) is built on it. Phase 2 only uses
the default height. Custom heights are ported anyway, with their tests, so that `LinesLayout` stays a faithful port.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewLayout/lineHeights.ts` | 13–45 | `PendingChangeKind`, `PendingChange`, `CustomLine` |
| same | 73–442 | `LineHeightsManager` (all of it) |
| same | 444–451 | `CustomLineHeightData` (constructor only — **not** `fromDecorations`, 453–464: there are no decorations yet) |
| same | 467–493 | `ArrayMap` |
| `src/vs/base/common/arrays.ts` | `binarySearch2` | the binary search used by `_binarySearchOverOrderedCustomLinesArray` |
| `src/vs/editor/test/common/viewLayout/lineHeights.test.ts` | 10–280 | suite `Editor ViewLayout - LineHeightsManager` (19 tests) |
| same | 282–535 | suite `Editor ViewLayout - LineHeightsManager (auto-commit on read)` (19 tests) |

## Files to create or change

- `src/LoomEditor.View/Layout/LineHeightsManager.cs` — create (split into `LineHeightsManager.Changes.cs` if the file goes over ~400 lines)
- `src/LoomEditor.View/Layout/CustomLine.cs`, `CustomLineHeightData.cs` — create
- `tests/LoomEditor.View.Tests/Layout/LineHeightsManagerTests.cs` — create (both suites; split into two files if long)

## Design decisions (already made — do not change)

- All types `internal`. Heights and line numbers are `int`. `CustomLine` rounds with `(int)Math.Round(x, MidpointRounding.AwayFromZero)`,
  which matches JavaScript's `Math.round` for positive values. Heights are never negative.
- `_invalidIndex = Infinity` becomes `int.MaxValue`.
- `PendingChange` (a TypeScript union) becomes a `readonly record struct PendingChange(PendingChangeKind Kind, string? DecorationId, int A, int B, int LineHeight)`
  **or** four small records deriving from an abstract record. Choose the abstract record: it reads like the union.
- `ArrayMap<K, T>` becomes a private nested class over `Dictionary<TKey, List<T>>`. Do not replace it with LINQ.
- `binarySearch2` is ported as a private static method next to its only caller, with a `// Ported from arrays.ts` comment.
  It returns `-(insertionIndex + 1)` when not found, like Monaco. Do not use `Array.BinarySearch` (it returns `~index`,
  which is a different value).
- `CustomLineHeightData` is a `sealed record CustomLineHeightData(string DecorationId, int StartLineNumber, int EndLineNumber, int LineHeight)`.

## Tests

- [x] Port: all 19 tests of `Editor ViewLayout - LineHeightsManager`
- [x] Port: all 19 tests of `Editor ViewLayout - LineHeightsManager (auto-commit on read)`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `editor/common/viewLayout/lineHeights.ts` ✅ ported (without `fromDecorations`), tests counted
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Decorations and `CustomLineHeightData.fromDecorations` (later phase); `LinesLayout` (P2-04).

## Suggested commit message

`feat(view): port LineHeightsManager`
