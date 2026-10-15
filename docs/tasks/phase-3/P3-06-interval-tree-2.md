# P3-06 — Interval tree (2/3): searching, and the `IntervalTree 1` test suite

- **Status:** done
- **Depends on:** P3-05
- **Size:** S

## Goal

Find the nodes of the interval tree that intersect a range, list all nodes in order, and resolve the absolute
offsets of one node. Then port Monaco's first interval tree test suite (random operations checked against an oracle,
and the Cormen searches).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/model/intervalTree.ts`. If a function is not exactly at the given line, search for its name.

| Lines | What to port |
|---|---|
| 253–279 | `IntervalTree.intervalSearch`, `search`, `collectNodesFromOwner`, `collectNodesPostOrder` |
| 291–304 | `IntervalTree.resolveNode` |
| 335–337 | `IntervalTree.getAllInOrder` |
| 638–870 | `collectNodesFromOwner`, `collectNodesPostOrder`, `search`, `intervalSearch` (the free functions) |

Tests: `src/vs/editor/test/common/model/intervalTree.test.ts`, suite `IntervalTree 1` (lines 21–559):
`TestState`, `Oracle`, `testIntervalTree(ops)`, suite `generated` (`gen01` … `gen18`, `force delta overflow`) and suite
`searching` (`cormen 1->2`, `cormen 4->8`, `cormen 10->15`, `cormen 21->25`, `cormen 24->24`).

## Files to create or change

- `src/LoomEditor.Core/Decorations/IntervalTree.Search.cs` — create (partial)
- `tests/LoomEditor.Core.Tests/Decorations/IntervalTreeTests.cs` — create (the suite)
- `tests/LoomEditor.Core.Tests/Decorations/TestHelpers/IntervalTreeTestState.cs` — create (`Oracle`, `TestState`, operations)

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- Signatures (Monaco's, `long` offsets as in P3-05):
  - `List<IntervalNode> IntervalSearch(long start, long end, int filterOwnerId, bool filterOutValidation, bool filterFontDecorations, int cachedVersionId, bool onlyMarginDecorations)`
  - `List<IntervalNode> Search(int filterOwnerId, bool filterOutValidation, bool filterFontDecorations, int cachedVersionId, bool onlyMarginDecorations)`
  - `List<IntervalNode> CollectNodesFromOwner(int ownerId)`, `List<IntervalNode> CollectNodesPostOrder()`
  - `void ResolveNode(IntervalNode node, int cachedVersionId)`, `List<IntervalNode> GetAllInOrder()`
- The free functions use the `IsVisited` metadata bit as an explicit-stack traversal marker, exactly as Monaco.
  Port the loops as they are (no recursion, no LINQ). Every traversal must leave all `IsVisited` bits cleared.
- The test operations (`{ type: 'insert', begin, end }`…) become a C# record
  `IntervalTreeOperation(string Type, long Begin, long End, int Id)`; each generated test is a `[Fact]` with its
  operation list copied **exactly** (same numbers), and the `// Monaco: 'gen01'` comment.
- `force delta overflow`: port it with the same numbers (`long`). In Monaco it only overflows when the line
  `// FORCE_OVERFLOWING_TEST: this.delta = start;` of the `IntervalNode` constructor is enabled; do **not** enable it.
- The `AutoTest` random generator of the test file (with `TEST_COUNT = 0`) is not ported: P3-05 already has a seeded
  randomized test.

## Tests

- [x] Port suite `IntervalTree 1` › `generated` (19 tests) and › `searching` (5 tests), with `// Monaco:` comments.
      `TestState.AcceptOp` checks the invariants (P3-05 helper) and compares `GetAllInOrder()` with the oracle after
      every insert, delete and change, and `IntervalSearch` with the oracle for every search.
- [x] New: after `Search` and `IntervalSearch`, no node has `IsVisited == true`
- [x] New: `ResolveNode` gives the same absolute offsets as `GetAllInOrder` for every node of the Cormen tree

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row updated (P3-06); phase 3 tests table row `intervalTree.test.ts` (`IntervalTree 1`: 24 / 24)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Edits (`acceptReplace`, `nodeAcceptEdit`, P3-07).

## Suggested commit message

`feat(core): add interval tree search and port its test suite`
