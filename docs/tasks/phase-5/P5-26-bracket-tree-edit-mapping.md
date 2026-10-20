# P5-26 — Bracket pair tree (5/9): `TextEditInfo`, `BeforeEditPositionMapper`, `combineTextEditInfos`

- **Status:** todo
- **Depends on:** P5-25
- **Size:** M

## Goal

Text edits can be described in `Length` coordinates, queued and combined, and an offset in the new text can be mapped
back to the old text. The incremental parser (P5-29) uses this to find which old nodes it may reuse.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/model/bracketPairsTextModelPart/bracketPairsTree/beforeEditPositionMapper.ts` | 11–35 | `TextEditInfo` (`fromModelContentChanges`, constructor, `toString`) |
| same | 37–123 | `BeforeEditPositionMapper` (`getOffsetBeforeChange`, `getDistanceToNextChange`, `translateOldToCur`, `translateCurToOld`, `adjustNextEdit`) |
| same | 125–143 | `TextEditInfoCache` |
| `common/model/bracketPairsTextModelPart/bracketPairsTree/combineTextEditInfos.ts` | 10–83 | `combineTextEditInfos` (with `nextS0ToS1MapWithS1LengthOf` and `pushEdit`) |
| same | 85–132 | `LengthMapping` (`splitAt`, `toString`), `toLengthMapping` |
| `test/common/model/bracketPairColorizer/beforeEditPositionMapper.test.ts` | 14–430 | the 9 tests and the helpers `compute`, `TextEdit`, `PositionOffsetTransformer`, `applyLineColumnEdits`, `rightPad` |
| `test/common/model/bracketPairColorizer/combineTextEditInfos.test.ts` | 17–100 | the 50 seeded tests, `runTest`, `getRandomEditInfos`, `getRandomEdit`, `toEdit` |

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/Tree/TextEditInfo.cs` — create
- `src/LoomEditor.Core/Model/BracketPairs/Tree/BeforeEditPositionMapper.cs` — create (with the private `TextEditInfoCache`)
- `src/LoomEditor.Core/Model/BracketPairs/Tree/CombineTextEditInfos.cs` — create (`internal static class TextEditInfoCombiner` with `Combine`, the private `LengthMapping` and `ToLengthMapping`)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/BeforeEditPositionMapperTests.cs` — create
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/CombineTextEditInfosTests.cs` — create

## Design decisions (already made — do not change)

- `TextEditInfo` is an `internal class` (not sealed: the ported test derives `TextEdit` from it, as Monaco does) with
  read-only `StartOffset`, `EndOffset`, `NewLength` (`Length`). `fromModelContentChanges(changes)` →
  `static List<TextEditInfo> FromModelContentChanges(IReadOnlyList<ModelContentChange> changes)`: map each change
  (`Range`, `Text`) then **reverse** the list (Monaco: "must be sorted in ascending order", the model gives them in
  descending order). `ToString()` is Monaco's format.
- `BeforeEditPositionMapper(IReadOnlyList<TextEditInfo> edits)`; `GetDistanceToNextChange` returns `Length?` (`null`:
  no edit any more). The mutable fields (`nextEditIdx`, `deltaOldToNewLineCount`, `deltaOldToNewColumnCount`,
  `deltaLineIdxInOld = -1`) keep Monaco's names (`_nextEditIdx`…). `TextEditInfoCache` holds three `TextLength`s.
- `combineTextEditInfos(first, second)` → `TextEditInfoCombiner.Combine(List<TextEditInfo> first, List<TextEditInfo>
  second)` returning `List<TextEditInfo>`; it returns one of its arguments unchanged when the other is empty (as Monaco:
  callers must not mutate the result).
- Monaco's `ArrayQueue` (base/common/arrays.ts) is used only through `dequeue` and `takeWhile(() => true)`: use a
  `Queue<LengthMapping>`; `takeWhile(v => true) || []` is "drain the queue into a list". Not ported: `ArrayQueue`.
- The sentinel `{ modified: false, lengthBefore: undefined, lengthAfter: undefined }` pushed after the second map
  becomes a final loop iteration with `s1Length == null`: `nextS0ToS1MapWithS1LengthOf(Length? s1Length)` keeps its
  "`undefined` means infinity" contract. `pushEdit`'s object literal is a `new TextEditInfo(...)`.
- `LengthMapping` is a `private sealed record LengthMapping(bool Modified, Length LengthBefore, Length LengthAfter)`;
  `splitAt` returns `(LengthMapping Item, LengthMapping? Remaining)`.

## Tests

- [ ] Port suite `Bracket Pair Colorizer - BeforeEditPositionMapper` › `Single-Line 1`, `Single-Line 2`,
      `Multi-Line Replace 1`, `Multi-Line Replace 2`, `Multi-Line Replace 3`, `Multi-Line Insert 1`,
      `Multi-Line Insert 2`, `Multi-Line Replace/Insert 1`, `Multi-Line Replace/Insert 2` (expected strings copied
      byte for byte, including `∞`; the file is UTF-8)
- [ ] Port suite `combineTextEditInfos` › `test0` … `test49` as one `[Theory]` with `[MemberData]` seeds 0…49 and the
      comment `// Monaco: 'combineTextEditInfos' > 'test' + seed (0…49)`. Monaco's `Random.create(seed)` is replaced by
      `new Random(seed)` (the test checks an invariant — the round trip gives back the original text — not values
      drawn from the generator); the seed is in every failure message. `rng.nextIntRange(a, b)` is `rng.Next(a, b)`
      (both exclude `b`: `nextIntRange(start, endExclusive)`, `test/common/core/random.ts` line 37). `createTextModel` is
      `new TextModel(text)`; `textModel.applyEdits` is `ApplyEdits`.
- [ ] New: `FromModelContentChanges_ReversesTheChanges`
- [ ] New: `Combine_EmptyFirst_ReturnsSecond`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `beforeEditPositionMapper.ts`, `combineTextEditInfos.ts` ✅ (P5-26); tests rows
      `beforeEditPositionMapper.test.ts` 9 / 9, `combineTextEditInfos.test.ts` 50 / 50; behavior difference
      "`ArrayQueue` is a `Queue<T>`"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The parser that consumes the mapper (P5-29); queuing edits on model changes (P5-30).

## Suggested commit message

`feat(core): port the bracket pair edit mapping and edit combining`
