# P5-23 — Bracket pair tree (2/9): AST base, leaves and pairs

- **Status:** done
- **Depends on:** P5-22
- **Size:** M

## Goal

The immutable part of Monaco's bracket pair AST exists: the `AstNode` base, the three leaves (text, bracket, unexpected
closing bracket) and the bracket pair node, with their `canBeReused` rules and min-indentation computation. The list
nodes come in P5-24.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `common/model/bracketPairsTextModelPart/bracketPairsTree/ast.ts` (706 lines).

| Lines | What to port |
|---|---|
| 14–22 | `const enum AstNodeKind`, the `AstNode` union (becomes the abstract base class, see below) |
| 27–84 | `abstract class BaseAstNode` |
| 91–190 | `PairAstNode` (`create`, `kind`, `listHeight`, `childrenLength`, `getChild`, `children`, constructor, `canBeReused`, `flattenLists`, `deepClone`, `computeMinIndentation`) |
| 577–599 | `emptyArray`, `abstract class ImmutableLeafAstNode` |
| 601–635 | `TextAstNode` |
| 637–685 | `BracketAstNode` |
| 687–706 | `InvalidBracketAstNode` |

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/Tree/AstNodeKind.cs` — create
- `src/LoomEditor.Core/Model/BracketPairs/Tree/AstNode.cs` — create (the base class)
- `src/LoomEditor.Core/Model/BracketPairs/Tree/PairAstNode.cs` — create
- `src/LoomEditor.Core/Model/BracketPairs/Tree/LeafAstNodes.cs` — create (`ImmutableLeafAstNode`, `TextAstNode`, `BracketAstNode`, `InvalidBracketAstNode`)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/AstNodeTests.cs` — create

## Design decisions (already made — do not change)

- Monaco's union type `AstNode` and its `BaseAstNode` become **one abstract class `AstNode`** (`BaseAstNode` disappears).
  Code that narrows on `node.kind` keeps a `switch` on `node.Kind` and casts (`(PairAstNode)node`), exactly where
  Monaco narrows. Members (PascalCase): `Kind`, `ChildrenLength`, `GetChild(int)` (returns `AstNode?`), `Children`
  (`IReadOnlyList<AstNode>`, allocates for pairs like Monaco), `MissingOpeningBracketIds`
  (`SmallImmutableSet<int>`), `ListHeight`, `Length`, `CanBeReused(SmallImmutableSet<int> openBracketIds)`,
  `FlattenLists()`, `DeepClone()`, `ComputeMinIndentation(Length offset, TextModel textModel)`.
- Monaco's `protected _length` stays a `protected Length _length` field (the one exception to the `_camelCase` =
  private rule, documented in a comment) so that `ListAstNode.handleChildrenChanged` (P5-24) can update it. `Length`
  is a read-only property over it.
- `getChild` with a bad index throws `ArgumentOutOfRangeException` (Monaco: `Error('Invalid child index')`).
- `Number.MAX_SAFE_INTEGER` (no indentation) is `int.MaxValue`. Monaco's `ITextModel` parameter is `TextModel`:
  `TextAstNode.ComputeMinIndentation` uses `GetLineFirstNonWhitespaceColumn`, `GetLineContent`,
  `Options.TabSize` and `CursorColumns.VisibleColumnFromColumn`.
- `BracketAstNode` keeps its private constructor and `static Create(Length, BracketKind, SmallImmutableSet<int>)`.
  `bracketInfo: BracketKind` is the type of step B for Monaco's `OpeningBracketKind | ClosingBracketKind` union
  (the abstract class `BracketKind` of P5-13, `LoomEditor.Core.Languages.Supports`). `Text` is
  `BracketInfo.BracketText`, `LanguageId` is `BracketInfo.LanguageId`.
- Leaves are immutable: `FlattenLists()` and `DeepClone()` return `this` (Monaco's `this & AstNode`).
- `PairAstNode.FlattenLists()` / `DeepClone()` call the children's methods; a `null` child or closing bracket stays
  `null`. `ListAstNode` does not exist yet: the code compiles because it only uses `AstNode`.
- The emptiness singletons (`emptyArray`) are `static readonly AstNode[]` of length 0 (immutable).
- Every class: `<remarks>Threading: immutable after construction; read on the UI thread.</remarks>` (leaves and pairs
  are immutable if their children are).

## Tests

No upstream test targets these classes alone (they are covered by P5-24 … P5-31).

- [x] New: `PairAstNode_Create_SumsLengthsOfBracketsAndChild` (`{` + text `0,3` + `}` → `Length.Of(0, 5)`)
- [x] New: `PairAstNode_Unclosed_CannotBeReused`
- [x] New: `PairAstNode_MissingIdsIntersectOpenIds_CannotBeReused`
- [x] New: `InvalidBracketAstNode_CanBeReused_OnlyWhenNoOpenedBracketMatches`
- [x] New: `BracketAstNode_CanNeverBeReused`, `TextAstNode_CanAlwaysBeReused`
- [x] New: `TextAstNode_ComputeMinIndentation_SkipsFirstPartialLine` (a model `"ab\n    c\n  d"`, a text node from
      offset `Of(0, 1)` to the end → `2`; from `Of(0, 0)` → `0`)
- [x] New: `Leaf_DeepClone_ReturnsSameInstance`

Brackets for these tests come from the `LanguageBracketsConfiguration` of a `LanguageConfigurationRegistry`
(`new LanguageConfigurationRegistry()` and `Register(languageId, new LanguageConfiguration { Brackets = … })`, P5-14) with the brackets `{` `}`.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `ast.ts` 🔶 in progress (P5-23)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`ListAstNode` and its four implementations (P5-24). Building trees (P5-29).

## Suggested commit message

`feat(core): port the bracket pair AST leaves and pair nodes`
