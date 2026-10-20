# P5-25 — Bracket pair tree (4/9): `concat23Trees` and `NodeReader`

- **Status:** done
- **Depends on:** P5-24
- **Size:** M

## Goal

Lists of AST nodes can be concatenated into balanced (2,3) trees, and an old tree can be walked to find the longest
reusable node at an offset. These are the two building blocks of the incremental parser (P5-29).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

Folder: `common/model/bracketPairsTextModelPart/bracketPairsTree/`.

| File | Lines | What to port |
|---|---|---|
| `concat23Trees.ts` | 14–65 | `concat23Trees` (with its local `readNode`) |
| `concat23Trees.ts` | 67–86 | `concat23TreesOfSameHeight` |
| `concat23Trees.ts` | 88–196 | `heightDiff`, `concat`, `append`, `prepend` |
| `nodeReader.ts` | 13–120 | `NodeReader` (`readLongestNodeAt`, `nextNodeAfterCurrent`) |
| `nodeReader.ts` | 122–136 | `getNextChildIdx`, `lastOrUndefined` |
| `test/common/model/bracketPairColorizer/concat23Trees.test.ts` | 52–94 | `testMerge` helper and the 5 tests after `Clone` |

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/Tree/Concat23Trees.cs` — create (`internal static class`)
- `src/LoomEditor.Core/Model/BracketPairs/Tree/NodeReader.cs` — create
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/Concat23TreesTests.cs` — create (the `Clone` test of P5-24 stays where it is; this file holds the other five, with the same suite name in the `// Monaco:` comments)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/NodeReaderTests.cs` — create

## Design decisions (already made — do not change)

- `concat23Trees(items)` → `Concat23Trees.Concat(List<AstNode> items)` returning `AstNode?`;
  `concat23TreesOfSameHeight(items, createImmutableLists = false)` → `Concat23Trees.ConcatOfSameHeight(List<AstNode>
  items, bool createImmutableLists = false)`. **`ConcatOfSameHeight` overwrites `items` in place** as Monaco does
  (`items[i] = …`): callers must not reuse the list. `items.slice(start, i)` is `items.GetRange(start, i - start)`;
  when the slice is the whole list, the list itself is passed (Monaco's `start === 0 && i === items.length`).
- `readNode` becomes a local function capturing `i` (a `ref` local or a field of a small private struct: choose a
  local function over a captured variable, no allocation per call needed).
- `append` / `prepend` keep Monaco's explicit stack (`const parents: ListAstNode[] = []`) as a `List<ListAstNode>`;
  the casts `as ListAstNode` become C# casts (they cannot fail when the heights differ).
- `NodeReader` keeps its three parallel stacks as `List<AstNode>`, `List<Length>`, `List<int>`.
  `readLongestNodeAt(offset, predicate)` → `AstNode? ReadLongestNodeAt(Length offset, Func<AstNode, bool> predicate)`;
  "Invalid offset" is an `InvalidOperationException`. `lengthAdd(curNodeOffset, curNode.length) <= offset` (a raw
  number comparison in Monaco) is `Length.LessThanEqual(Length.Add(…), offset)`.
- `getNextChildIdx(node, curIdx = -1)` and `lastOrUndefined` are private static helpers of `NodeReader`.

## Tests

- [x] Port suite `Bracket Pair Colorizer - mergeItems` › `Empty List`, `Same Height Lists`, `Different Height Lists 1`,
      `Different Height Lists 2`, `Different Height Lists 3` (with `testMerge` and the `AstAssert` helper of P5-24)
- [x] New: `ConcatOfSameHeight_SevenLeaves_GivesBalancedTreeOfHeight2`
- [x] New: `Concat_ManyRandomHeights_FlattensToSameLeaves` (seeded `[Theory]` 1…20, seed in the message: random lists
      of text nodes of heights 0–3, `Concat(...).FlattenLists()` equals `ListAstNode.Create(items).FlattenLists()`)
- [x] New: `ReadLongestNodeAt_OffsetAtNodeStart_ReturnsLongestMatchingNode`
- [x] New: `ReadLongestNodeAt_OffsetGoesBackwards_Throws`
- [x] New: `ReadLongestNodeAt_PredicateRejectsAll_ReturnsNull`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `concat23Trees.ts`, `nodeReader.ts` ✅ (P5-25); tests row `concat23Trees.test.ts` 6 / 6
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The parser (P5-29).

## Suggested commit message

`feat(core): port concat23Trees and the bracket pair node reader`
