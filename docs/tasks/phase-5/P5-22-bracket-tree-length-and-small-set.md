# P5-22 — Bracket pair tree (1/9): `Length`, `TextLength`, `SmallImmutableSet`

- **Status:** todo
- **Depends on:** P5-21
- **Size:** M

## Goal

Start the port of Monaco's bracket pair AST (`bracketPairsTree/*`, ADR 0009 decision 7) with its two value types:
`Length` (a line count and a column count packed in one number) and `SmallImmutableSet` (a bit set of opening bracket
ids, with `DenseKeyProvider`). Every later card of the tree uses them.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

Folder: `common/model/bracketPairsTextModelPart/bracketPairsTree/`.

| File | Lines | What to port |
|---|---|---|
| `bracketPairsTree/length.ts` | 14–202 | everything: `lengthDiff`, `Length`, `lengthZero`, `lengthIsZero`, `factor`, `toLength`, `lengthToObj`, `lengthGetLineCount`, `lengthGetColumnCountIfZeroLineCount`, `lengthAdd`, `sumLengths`, `lengthEquals`, `lengthDiffNonNegative`, `lengthLessThan`, `lengthLessThanEqual`, `lengthGreaterThanEqual`, `lengthToPosition`, `positionToLength`, `lengthsToRange`, `lengthOfRange`, `lengthCompare`, `lengthOfString`, `lengthOfStringObj`, `lengthMax` (not `lengthHash`: see below) |
| `common/core/text/textLength.ts` | 13–14, 65–72, 144–146 | `TextLength`: `zero`, constructor, `isZero`, `toString` only |
| `bracketPairsTree/smallImmutableSet.ts` | 1–176 | `SmallImmutableSet`, `IDenseKeyProvider`, `identityKeyProvider`, `DenseKeyProvider` |
| `test/common/model/bracketPairColorizer/length.test.ts` | 10–64 | suite `Bracket Pair Colorizer - Length` |
| `test/common/model/bracketPairColorizer/smallImmutableSet.test.ts` | 10–51 | suite `Bracket Pair Colorizer - ImmutableSet` |

## Files to create or change

- `src/LoomEditor.Core/Text/TextLength.cs` — create
- `src/LoomEditor.Core/Model/BracketPairs/Tree/Length.cs` — create
- `src/LoomEditor.Core/Model/BracketPairs/Tree/SmallImmutableSet.cs` — create
- `src/LoomEditor.Core/Model/BracketPairs/Tree/DenseKeyProvider.cs` — create (`IDenseKeyProvider<T>`, `IdentityKeyProvider`, `DenseKeyProvider<T>`)
- `src/LoomEditor.Core/Model/BracketPairs/README.md` — create (what the folder holds, the Monaco folder it ports)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/LengthTests.cs` — create
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/SmallImmutableSetTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.Core.Model.BracketPairs.Tree` (the folder), everything `internal`.
- **`Length` is `internal readonly record struct Length`** wrapping one private `long` (Monaco's JS number). The packing
  is Monaco's: `value = lineCount * 2^26 + columnCount` (`Factor = 1L << 26`), so a `long` holds it exactly and
  `lengthLessThan`, `lengthCompare`, `lengthMax` stay plain comparisons of the packed value. The record struct gives
  `==` (Monaco's `===` / `lengthEquals`). Same limits as Monaco: no overflow check (columns ≥ 2^26 are not supported,
  as in Monaco). Floor divisions become integer divisions (`value / Factor`, the value is never negative).
- The functions become static members of `Length`, Monaco names in PascalCase without the `length` prefix:
  `Length.Zero`, `IsZero`, `Length.Of(lineCount, columnCount)` (`toLength`), `ToObj()` (returns `TextLength`),
  `GetLineCount()`, `GetColumnCountIfZeroLineCount()`, `Length.Add(a, b)` (and `operator +` is **not** defined: the
  add is not commutative), `Length.Sum<T>(items, Func<T, Length>)`, `Length.Diff(startLine, startCol, endLine, endCol)`,
  `Length.DiffNonNegative(a, b)`, `LessThan`, `LessThanEqual`, `GreaterThanEqual`, `Length.Compare(a, b)` (returns
  `int`, sign only), `Length.Max(a, b)`, `ToPosition()`, `Length.FromPosition(Position)` (`positionToLength`),
  `Length.ToRange(start, end)` (`lengthsToRange`), `Length.OfRange(Range)` (`lengthOfRange`, returns `TextLength`),
  `Length.OfString(string)`, `Length.OfStringObj(string)` (both with `Strings.SplitLines`). `lengthHash` is not ported:
  `GetHashCode` of the record struct does the job. `ToString()` returns `ToObj().ToString()` (debugging only).
- `TextLength` is `internal readonly record struct TextLength(int LineCount, int ColumnCount)` in `LoomEditor.Core.Text`
  with `Zero`, `IsZero` and `ToString()` = `"{LineCount},{ColumnCount}"` (invariant culture). The rest of
  `textLength.ts` is not needed by phase 5.
- **`SmallImmutableSet<T>`** is an `internal sealed class` implementing `IEquatable<SmallImmutableSet<T>>` (Monaco's
  `equals`; `GetHashCode` from `items` and the additional items). Fields: `int _items` (bits 0–31) and
  `int[] _additionalItems` (Monaco's `number[]` of 32-bit words). Shifts are `1 << key` on `int` exactly as in Monaco.
  Keep the reference check against the shared empty array in `merge` (`ReferenceEquals(_additionalItems, EmptyArr)`).
- In `add`, JavaScript grows the array implicitly (`newItems[idx] |= …` on a missing index): in C#, copy into a
  `List<int>`, pad with `0` until `Count > idx`, then set the bit. In `has`, `intersects` and `merge`, an index past
  the end reads `0` (JavaScript's `|| 0`).
- **The cache of the 129 small sets** (`SmallImmutableSet.cache`) is filled **eagerly** in the static constructor
  (one instance per `items` value 0…128), so it is never written after type initialization: no static mutable state,
  thread-safe. `create` reads it for `items <= 128` without additional items, as in Monaco. One cache per closed
  generic type (Monaco shares one across `T`; the sets are immutable values, so this changes nothing).
- `IDenseKeyProvider<T>` has `int GetKey(T value)`. `IdentityKeyProvider` is a `sealed class` implementing
  `IDenseKeyProvider<int>` with a static `Instance` (Monaco's `identityKeyProvider`). The opening bracket id type
  (`OpeningBracketId = number`) is `int`.
- `DenseKeyProvider<T>` (where `T : notnull`): a `Dictionary<T, int>` for lookups **and** a `List<T>` in insertion
  order, because `reverseLookupSet` and `keys` iterate Monaco's `Map` in insertion order (phase-4 README trap 3).
  Port `GetKey`, `ReverseLookup(int)` (returns `T?`, default when missing), `ReverseLookupSet(SmallImmutableSet<T>)`,
  `Keys`.
- Threading note on every class: immutable value (`Length`, `TextLength`, `SmallImmutableSet`) / single-threaded
  (`DenseKeyProvider`, used by one tree).

## Tests

- [ ] Port suite `Bracket Pair Colorizer - Length` › `Basic`, `lengthDiffNonNeg` (`toStr` = `ToObj().ToString()`)
- [ ] Port suite `Bracket Pair Colorizer - ImmutableSet` › `Basic`, `Many Elements`
- [ ] New: `Add_LinesInSecondLength_DropsFirstColumns` (`Add(Of(10, 5), Of(20, 3)) == Of(30, 3)`, the comment of length.ts 76–77)
- [ ] New: `ToRange_And_FromPosition_RoundTrip` (`Length.ToRange(Of(0, 4), Of(2, 1))` is `[1,5 -> 3,2]`; `FromPosition(new Position(3, 2)) == Of(2, 1)`)
- [ ] New: `OfString_CountsLinesAndLastLineLength` (`"ab\ncd\r\nefg"` → `2,3`)
- [ ] New: `Add_KeyAbove31_UsesAdditionalItems` and `Merge_SetsWithAdditionalItems_ContainsBoth` (keys 0, 31, 32, 70)
- [ ] New: `GetEmpty_And_SmallSets_AreCachedInstances` (`ReferenceEquals` for two `Add(3)` on the empty set)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `length.ts`, `smallImmutableSet.ts` ✅ (P5-22), `textLength.ts` 🔶 subset; tests table rows `length.test.ts` 2 / 2, `smallImmutableSet.test.ts` 2 / 2; behavior difference "`Length` is a packed `long`"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The AST nodes (P5-23, P5-24). `lengthHash`. The rest of `textLength.ts`.

## Suggested commit message

`feat(core): port bracket pair lengths and small immutable sets`
