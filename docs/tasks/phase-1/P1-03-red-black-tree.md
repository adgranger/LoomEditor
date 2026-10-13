# P1-03 — Red-black tree

- **Status:** done
- **Depends on:** P1-02
- **Size:** M

## Goal

The balanced tree that holds the pieces, with its augmented metadata (size and line-feed count of each left
subtree), usable and tested **on its own**, before `PieceTreeBase` exists.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts` | 8–78 | `TreeNode` (`next`, `prev`, `detach`) |
| same | 80–89 | `NodeColor`, `SENTINEL` |
| same | 91–123 | `leftest`, `righttest`, `calculateSize`, `calculateLF`, `resetSentinel` |
| same | 125–170 | `leftRotate`, `rightRotate` |
| same | 172–331 | `rbDelete` |
| same | 333–376 | `fixInsert` |
| same | 378–425 | `updateTreeMetadata`, `recomputeTreeMetadata` |
| `src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` | 1815–1869 | `rbInsertRight`, `rbInsertLeft` |
| same | 101–114 | `NodePosition` |
| `src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` | 164–212 | `assertTreeInvariants`, `depth`, `assertValidNode`, `assertValidTree` (test helpers) |

## Files to create or change

`src/LoomEditor.Core/Model/PieceTree/`:
- `NodeColor.cs` — `internal enum NodeColor { Black = 0, Red = 1 }`
- `TreeNode.cs` — node: `Parent`, `Left`, `Right`, `Color`, `Piece`, `SizeLeft` (Monaco `size_left`),
  `LineFeedCountLeft` (`lf_left`), `IsDetached`
- `RedBlackTree.cs` — the tree: `Root`, `Sentinel`, and every function listed above as instance methods
- `NodePosition.cs` — `readonly record struct NodePosition(TreeNode Node, int Remainder, int NodeStartOffset)`

`tests/LoomEditor.Core.Tests/Model/PieceTree/`:
- `TestHelpers/TreeInvariants.cs` — port of the Monaco assertion helpers (`AssertValid(RedBlackTree tree)`)
- `RedBlackTreeTests.cs`

## Design decisions (already made — do not change)

- **One sentinel per tree, not a global one.** Monaco uses a single global mutable `SENTINEL` and even writes to it
  during deletes (`resetSentinel`). In .NET, two trees used on two threads (or two xUnit tests running in parallel)
  would corrupt each other. So `RedBlackTree` owns `public TreeNode Sentinel { get; }` (black, its links point to
  itself, `SizeLeft = LineFeedCountLeft = 0`), and every comparison `x === SENTINEL` becomes
  `ReferenceEquals(x, Sentinel)`. Document this in `docs/PORTING.md` → "Behavior differences".
- `TreeNode.Next()` / `Prev()` need the sentinel: implement them as `RedBlackTree.Next(TreeNode)` /
  `RedBlackTree.Prev(TreeNode)`; same for `Leftest`/`Righttest`.
- **Detach:** Monaco sets `parent/left/right` to `null` and later tests `node.parent === null`. Here `Detach()` sets
  `IsDetached = true` and points the three links to the tree's sentinel. (The search cache in P1-04 uses `IsDetached`.)
- `PieceTreeBase` (P1-05) will **own** a `RedBlackTree` (composition). Monaco's functions take `tree: PieceTreeBase`
  only to read/write `tree.root`, which `RedBlackTree.Root` replaces.
- `RedBlackTree` API used later: `InsertRight(TreeNode? node, Piece piece)`, `InsertLeft(TreeNode? node, Piece piece)`
  (both return the new node; `null` means "insert as root / at the edge", as in Monaco), `Delete(TreeNode node)`
  (Monaco `rbDelete`), `UpdateTreeMetadata(TreeNode, int delta, int lineFeedCountDelta)`,
  `RecomputeTreeMetadata(TreeNode)`, `LeftRotate`, `RightRotate`, `FixInsert`.
- `TreeInvariants.AssertValid` checks: root black, sentinel unchanged (black, self-linked, zero metadata), no red
  node with a red child, same black depth on both sides, and `SizeLeft`/`LineFeedCountLeft` equal to the real sums.

## Tests

- [x] Empty tree: `Root` is the sentinel, invariants hold
- [x] Insert 1, 2, 3, 100 pieces with `InsertRight` (append) → in-order traversal returns pieces in order; invariants hold after each insert
- [x] Insert with `InsertLeft` before the first node → order correct
- [x] Random inserts at random positions (seeded `Random`, 500 operations) → order matches a `List<Piece>` reference; invariants hold
- [x] Random deletes (seeded) until empty → order matches the reference; invariants hold after each delete
- [x] `UpdateTreeMetadata` after changing a piece's length/line feeds → `SizeLeft`/`LineFeedCountLeft` of ancestors correct
- [x] Two trees used from two threads in parallel (`Parallel.For`) → both stay valid (guards the per-tree sentinel)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row for `rbTreeBase.ts` (ported) + "Behavior differences" row for the per-tree sentinel and `IsDetached`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Anything about text: the tree only stores `Piece` objects here.

## Suggested commit message

`feat(core): port the piece tree red-black tree`
