# P5-29 — Bracket pair tree (8/9): parser, result types, `CallbackIterable`

- **Status:** done
- **Depends on:** P5-28
- **Size:** M

## Goal

A token stream can be parsed into a bracket pair AST, from scratch or incrementally (reusing the nodes of the previous
tree that the edits did not touch). The result types of the bracket pairs API (`BracketInfo`, `BracketPairInfo`,
`FoundBracket`) and Monaco's push-based `CallbackIterable` exist, so the tree (P5-30) can be written.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/model/bracketPairsTextModelPart/bracketPairsTree/parser.ts` | 14–179 | `parseDocument`, `Parser` (`nodesConstructed`, `nodesReused`, `parseDocument`, `parseList`, `tryReadChildFromCache`, `parseChild`) |
| `common/textModelBracketPairs.ts` | 75–130 | `IFoundBracket`, `BracketInfo`, `BracketPairInfo`, `BracketPairWithMinIndentationInfo` (the interface `IBracketPairsTextModelPart`, 13–73, is the API of P5-31) |
| `src/vs/base/common/arrays.ts` | 686–693, 822–893 | `Comparator`, `compareBy`, `CallbackIterable` (`empty`, `iterate`, `forEach`, `toArray`, `filter`, `map`, `some`, `findFirst`, `findLast`, `findLastMaxBy`) |

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/Tree/Parser.cs` — create (`internal static class BracketPairParser` with `ParseDocument`, and the private nested `Parser`)
- `src/LoomEditor.Core/Model/BracketPairs/FoundBracket.cs` — create
- `src/LoomEditor.Core/Model/BracketPairs/BracketInfo.cs` — create
- `src/LoomEditor.Core/Model/BracketPairs/BracketPairInfo.cs` — create (`BracketPairInfo`, `BracketPairWithMinIndentationInfo`)
- `src/LoomEditor.Core/Model/BracketPairs/CallbackIterable.cs` — create
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/ParserTests.cs` — create
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/CallbackIterableTests.cs` — create

## Design decisions (already made — do not change)

- `parseDocument(tokenizer, edits, oldNode, createImmutableLists)` →
  `BracketPairParser.ParseDocument(ITokenizer, IReadOnlyList<TextEditInfo>, AstNode? oldNode, bool createImmutableLists)`.
  "Not supported" (an old node with immutable lists) is an `InvalidOperationException`; "unexpected" token kind too.
  `NodesConstructed` / `NodesReused` stay (the tests read them).
- **Recursion depth.** `parseList` ↔ `parseChild` recurse once per nesting level; Monaco stops nesting at
  `level > 300` (an opening bracket becomes a `TextAstNode`). Keep it: about 600 C# frames, far below the 1 MB stack
  of the UI thread. Do not make the parser iterative.
- `items` of `parseList` is a `List<AstNode>`; it goes to `Concat23Trees.Concat` (with an old node reader) or
  `Concat23Trees.ConcatOfSameHeight(items, createImmutableLists)` (without), as in Monaco. A `null` result becomes
  `ListAstNode.GetEmpty()`.
- `IFoundBracket` → `internal readonly record struct FoundBracket(Range Range, BracketKind BracketInfo)` (the
  `OpeningBracketKind | ClosingBracketKind` union is the abstract class `BracketKind` of P5-13).
- `BracketInfo` → `internal sealed record BracketInfo(Range Range, int NestingLevel, int NestingLevelOfEqualBracketType,
  bool IsInvalid)`.
- `BracketPairInfo` → `internal class` (not sealed: `BracketPairWithMinIndentationInfo` derives from it) with `Range`,
  `OpeningBracketRange`, `ClosingBracketRange` (`Range?`), `NestingLevel`, `NestingLevelOfEqualBracketType`, the
  private `PairAstNode` and the computed `OpeningBracketInfo` (`OpeningBracketKind`) / `ClosingBracketInfo`
  (`ClosingBracketKind?`). `BracketPairWithMinIndentationInfo` adds `MinVisibleColumnIndentation` (`-1` when not
  requested).
- **`CallbackIterable<T>`** is ported (not replaced by `IEnumerable<T>`): the tree pushes items from recursive
  functions and stops when the callback returns `false`. Constructor takes `Action<Func<T, bool>> iterate`;
  `Empty` is a static read-only instance per `T`. Members: `ForEach(Action<T>)`, `ToArray()` (returns `T[]`),
  `Filter`, `Map<TResult>`, `Some`, `FindFirst`, `FindLast`, `FindLastMaxBy(Comparison<T>)` — Monaco's logic
  exactly: `findLastMaxBy` replaces its result only when an item is **strictly** greater (`isGreaterThan`), so of
  equal maxima the first one is kept, whatever the name says. `T` results that may be absent return `T?` with
  `where T : class` (all users are classes).
- `compareBy(selector, comparator)` → a private static helper where it is used (P5-31), not a general utility.

## Tests

No upstream test targets the parser alone (it is covered by P5-30 and P5-31).

- [x] New: `ParseDocument_FastTokenizer_BuildsNestedPairs` (`"{ ( [] ) }"` with `{}`, `()`, `[]`: one pair whose
      child holds a pair whose child holds a pair; lengths add up to the text length)
- [x] New: `ParseDocument_UnclosedBracket_PairWithoutClosingBracket`
- [x] New: `ParseDocument_UnexpectedClosingBracket_IsInvalidBracketNode`
- [x] New: `ParseDocument_MismatchedClosing_ClosesOuterPair` (`"{ ( }"`: the `(` pair is unclosed, the `{` pair is closed)
- [x] New: `ParseDocument_10000OpeningBrackets_DoesNotOverflowTheStack` (nesting stops at level 300)
- [x] New: `ParseDocument_CreateImmutableLists_ListsAreImmutable`
- [x] New: `ParseDocument_WithOldNodeAndNoEdit_ReusesNodes` (`NodesReused > 0`, same structure as a fresh parse)
- [x] New: `CallbackIterable_FindLastMaxBy_KeepsFirstOfEqualMaxima`, `…_FindFirst_StopsIterating`, `…_Empty_ToArrayIsEmpty`
      (named `FindLastMaxBy_EqualMaxima_KeepsFirstOfEqualMaxima`, `FindFirst_StopsIterating`, `Empty_ToArray_IsEmpty` in
      `CallbackIterableTests`, with 10 more; `ParserTests` adds `…_EmptyText_IsAnEmptyList`,
      `Constructor_OldNodeWithImmutableLists_Throws` and `ParseDocument_WithOldNodeAndEdit_ReusesUntouchedPairsOnly`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `parser.ts` ✅ (P5-29), `textModelBracketPairs.ts` 🔶 (types), `base/common/arrays.ts`
      🔶 (`CallbackIterable`, `compareBy`)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`BracketPairsTree` (P5-30), the model part and its API (P5-31).

## Suggested commit message

`feat(core): port the bracket pair parser and result types`
