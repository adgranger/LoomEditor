# P9-01 — `LineRange` and `LineRangeSet`

- **Status:** todo
- **Depends on:** P8-39
- **Size:** M

## Goal

The two line-number range types the whole diff code is written in exist in Core: `LineRange` (`[start, endExclusive)` of 1-based line numbers) and `LineRangeSet` (a sorted set of disjoint line ranges).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/core/ranges/lineRange.ts` | 15–248 | `LineRange` (all members, including `ofLength`, `fromRange`, `fromRangeInclusive`, `compareByStart`, `subtract`, `joinMany`, `join`, `deserialize`, `delta`, `deltaLength`, `contains`, `intersect`, `intersectsStrict`, `overlapOrTouch`, `toInclusiveRange`, `toExclusiveRange`, `mapToLineArray`, `forEach`, `serialize`, `includes`, `toOffsetRange`, `addMargin`, `equals`, `toString`) |
| `src/vs/editor/common/core/ranges/lineRange.ts` | 249–414 | `LineRangeSet` (`ranges`, `addRange`, `contains`, `intersects`, `getUnion`, `getIntersection`, `getWithDelta`, `subtractFrom`, `toString`, `getIntersectionRange`, `getFirstRangeAfter`...) — port every member of the class |
| `src/vs/editor/test/common/core/lineRange.test.ts` | whole file | the tests (`contains`, `addRange`, `getUnion`, `intersects`, `getIntersection`...) |

## Files to create or change

- `src/LoomEditor.Core/Text/LineRange.cs` — create
- `src/LoomEditor.Core/Text/LineRangeSet.cs` — create
- `tests/LoomEditor.Core.Tests/Text/LineRangeTests.cs`, `LineRangeSetTests.cs` — create

## Design decisions (already made — do not change)

- `internal readonly record struct LineRange(int StartLineNumber, int EndLineNumberExclusive)`. The constructor throws `ArgumentException` when `StartLineNumber > EndLineNumberExclusive` (Monaco: `BugIndicatingError`).
  `LineRange` is the same kind of type as the existing `OffsetRange` (`src/LoomEditor.Core/Text/OffsetRange.cs`): look at it and follow its style (`Length`, `IsEmpty`, static factory names in PascalCase).
- Monaco's `IRange` argument of `fromRange` is `LoomEditor.Core.Text.Range` (never `System.Range`). `toInclusiveRange()` returns `Range?` (null when empty): `new Range(StartLineNumber, 1, EndLineNumberExclusive - 1, int.MaxValue)` (Monaco: `Number.MAX_SAFE_INTEGER` as the end column).
- `toOffsetRange()` returns the existing `OffsetRange` (`new OffsetRange(StartLineNumber - 1, EndLineNumberExclusive - 1)`). `mapToLineArray<T>` / `forEach` become `IEnumerable`-free helpers that take a `Func<int, T>` / `Action<int>`.
- `LineRangeSet` is a `sealed class` with a private `List<LineRange> _normalizedRanges` and `IReadOnlyList<LineRange> Ranges`; `ISerializedLineRange` (tuple type) is **not** ported (only `Serialize()` / `Deserialize` returning `(int, int)`, kept because `UnchangedRegion` state may use them later — if a member has no use in phase 9 it is still ported, it is small).
- `<remarks>Threading: immutable (LineRange); LineRangeSet is not thread-safe.</remarks>`.

## Tests

- [ ] Port (`// Monaco: 'contains'` etc.): every `test(...)` of `lineRange.test.ts`.
- [ ] New: `OfLength_StartAndLength`, `Ctor_StartAfterEnd_Throws`, `Subtract_*` (no overlap, inside, left part, right part, covers), `Join_Many`, `ToInclusiveRange_Empty_Null`, `Equals_*`, `ToString_Format` (`[2,5)`).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated: `lineRange.ts`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`OffsetRangeSet`, `LineRangeMapping` (P9-07), anything to do with models.

## Suggested commit message

`feat(core): port LineRange and LineRangeSet`
