# P5-24 — Bracket pair tree (3/9): AST list nodes

- **Status:** done
- **Depends on:** P5-23
- **Size:** M

## Goal

The list nodes of the bracket pair AST exist: the abstract `ListAstNode` with its (2,3)-tree nodes (mutable and
immutable) and its array nodes (mutable and immutable). After this card `ast.ts` is complete.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `common/model/bracketPairsTextModelPart/bracketPairsTree/ast.ts`.

| Lines | What to port |
|---|---|
| 192–377 | `abstract class ListAstNode`: `create23`, `create`, `getEmpty`, `kind`, `missingOpeningBracketIds`, `cachedMinIndentation`, constructor, `throwIfImmutable`, `setChild`, `makeLastElementMutable`, `makeFirstElementMutable`, `canBeReused`, `handleChildrenChanged`, `flattenLists`, `computeMinIndentation`, the abstract `toMutable`, `appendChildOfSameHeight`, `unappendChild`, `prependChildOfSameHeight`, `unprependChild` |
| 379–484 | `TwoThreeListAstNode` |
| 489–497 | `Immutable23ListAstNode` |
| 502–562 | `ArrayListAstNode` |
| 567–575 | `ImmutableArrayListAstNode` |

Test: `test/common/model/bracketPairColorizer/concat23Trees.test.ts` 16–59 (`Clone` and its `equals` helper).

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/Tree/ListAstNode.cs` — create (abstract class)
- `src/LoomEditor.Core/Model/BracketPairs/Tree/ListAstNode.Implementations.cs` — create (the four subclasses, `internal sealed` where Monaco has no subclass; `TwoThreeListAstNode` and `ArrayListAstNode` are not sealed)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/TestHelpers/AstAssert.cs` — create (port of `equals`, concat23Trees.test.ts 25–58: `AstAssert.StructurallyEqual(AstNode, AstNode)` returns `bool`)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/ListAstNodeTests.cs` — create

## Design decisions (already made — do not change)

- **Mutable and immutable nodes are kept as in Monaco**: the (2,3) nodes and the array nodes are mutable; the
  `Immutable…` subclasses override `ThrowIfImmutable()` to throw `InvalidOperationException("this instance is
  immutable")` and `ToMutable()` to return a shallow mutable copy. The parser (P5-29) builds immutable lists for a
  first parse and mutable ones for incremental parses, exactly like Monaco: do not simplify to one kind.
- `create23(item1, item2, item3, immutable = false)` and `create(items, immutable = false)` are
  `static ListAstNode Create23(AstNode, AstNode, AstNode?, bool immutable = false)` and
  `static ListAstNode Create(List<AstNode> items, bool immutable = false)`. `Create` copies nothing: the array node
  keeps (and, when mutable, mutates) the list it is given, as Monaco does with `_children`. "Invalid list heights" is an `InvalidOperationException`. `GetEmpty()` returns a new
  immutable empty array node (Monaco allocates each time; keep it).
- `canBeReused` walks to the last child; an empty list inside a list throws `InvalidOperationException` (Monaco's
  `BugIndicatingError`).
- `handleChildrenChanged` writes the base's `_length`, `_missingOpeningBracketIds` and resets `cachedMinIndentation` to
  `-1`.
- `ListHeight` is a constructor argument stored in a read-only property; `MissingOpeningBracketIds` reads the private
  mutable field.
- `ArrayListAstNode` uses `List<AstNode>`: `push` → `Add`, `pop` → remove last, `unshift` → `Insert(0, …)`,
  `shift` → remove first. `unappendChild` / `unprependChild` on an empty list return `null` (Monaco's `undefined`).
  `[...this.children]` in `ImmutableArrayListAstNode.toMutable` is a new `List<AstNode>`.
- `flattenLists` uses `ListAstNode.Create(items)` (mutable), as in Monaco.

## Tests

- [x] Port suite `Bracket Pair Colorizer - mergeItems` › `Clone` (with the `equals` helper)
- [x] New: `Create23_DifferentHeights_Throws`
- [x] New: `Immutable23_AppendChild_Throws`, `ImmutableArray_ToMutable_ReturnsMutableCopy` (the copy accepts
      `AppendChildOfSameHeight`, the original is unchanged)
- [x] New: `TwoThree_PrependThenUnprepend_RestoresChildrenAndLength`
- [x] New: `HandleChildrenChanged_RecomputesLengthAndMissingIds`
- [x] New: `MakeLastElementMutable_ImmutableChildList_IsReplacedByMutableCopy`
- [x] New: `CanBeReused_EmptyList_IsFalse`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `ast.ts` ✅ ported (P5-23, P5-24); tests row `concat23Trees.test.ts` 1 / 6 (P5-24)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`concat23Trees.ts` and the other five tests of its suite (P5-25).

## Suggested commit message

`feat(core): port the bracket pair AST list nodes`
