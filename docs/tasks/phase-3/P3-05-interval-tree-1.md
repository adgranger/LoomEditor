# P3-05 — Interval tree (1/3): nodes, red-black insert and delete

- **Status:** todo
- **Depends on:** P3-04
- **Size:** M

## Goal

Start the port of Monaco's interval tree (`intervalTree.ts`), the structure that stores decorations and **tracked
ranges**. The cursor uses tracked ranges to follow its selections across edits (ADR 0006, decision 4). This card
ports the node, its packed metadata, and the red-black insert/delete with the `delta` / `maxEnd` bookkeeping.
Searching (P3-06) and edits (P3-07) come next.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/model/intervalTree.ts` (1,281 lines). If a function is not exactly at the given line,
search for its name.

| Lines | What to port |
|---|---|
| 24–79 | `NodeColor`, `const enum Constants` (bit masks, `MIN_SAFE_DELTA`, `MAX_SAFE_DELTA`) |
| 81–139 | metadata getters/setters (`getNodeColor`, `setNodeColor`, `…IsVisited`, `…IsForValidation`, `…IsInGlyphMargin`, `…AffectsFont`, `…Stickiness`, `…CollapseOnReplaceEdit`, `setNodeStickiness`) |
| 141–235 | `class IntervalNode` (constructor, `reset`, `setOptions`, `setCachedOffsets`, `detach`) |
| 237 | `SENTINEL` (see design decisions: one per tree) |
| 243–252, 281–289, 335–347 | `class IntervalTree`: fields, `insert`, `delete`, `getAllInOrder` (needs `search`: see below), `_normalizeDeltaIfNecessary` |
| 349–388 | `normalizeDelta` |
| 871–1156 | `rbTreeInsert`, `treeInsert`, `rbTreeDelete` |
| 1157–1281 | `leftest`, `resetSentinel`, `leftRotate`, `rightRotate`, `computeMaxEnd`, `recomputeMaxEnd`, `recomputeMaxEndWalkToRoot`, `intervalCompare` |
| `src/vs/editor/common/model.ts` 681–686 | `enum TrackedRangeStickiness` |

The test helpers are in `src/vs/editor/test/common/model/intervalTree.test.ts`: `assertTreeInvariants`,
`depth`, `assertValidNode`, `assertValidTree` (the last ~60 lines of the file).

## Files to create or change

- `src/LoomEditor.Core/Model/TrackedRangeStickiness.cs` — create: `internal enum TrackedRangeStickiness { AlwaysGrowsWhenTypingAtEdges = 0, NeverGrowsWhenTypingAtEdges = 1, GrowsOnlyWhenTypingBefore = 2, GrowsOnlyWhenTypingAfter = 3 }` (check the values in model.ts)
- `src/LoomEditor.Core/Decorations/ModelDecorationOptions.cs` — create (minimal, see below)
- `src/LoomEditor.Core/Decorations/IntervalNode.cs` — create (node + metadata accessors)
- `src/LoomEditor.Core/Decorations/IntervalTree.cs` — create (class, fields, `Insert`, `Delete`, `NormalizeDelta`)
- `src/LoomEditor.Core/Decorations/IntervalTree.RedBlack.cs` — create (partial: insert/delete/rotations/maxEnd)
- `src/LoomEditor.Core/Decorations/README.md` — update: what is there now
- `tests/LoomEditor.Core.Tests/Decorations/TestHelpers/IntervalTreeInvariants.cs` — create (port of the assert helpers)
- `tests/LoomEditor.Core.Tests/Decorations/IntervalTreeInsertDeleteTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- Namespace `LoomEditor.Core.Decorations`, everything `internal`. Look at `LoomEditor.Core/Model/PieceTree/RedBlackTree*.cs`
  (phase 1): it solved the same problems (sentinel, rotations) and is the style to follow.
- **One sentinel per tree** (as in the piece tree, see `docs/PORTING.md` → `RedBlackTree`): `IntervalTree.Sentinel`
  is an instance field created in the constructor; every function that used the global `SENTINEL` uses the tree's
  sentinel. `resetSentinel` resets the tree's own sentinel. A new `IntervalNode` points to itself
  (`parent = left = right = this`) as in Monaco; `rbTreeInsert` replaces these links with the tree's sentinel.
- **Offsets are `long`**: `Start`, `End`, `Delta`, `MaxEnd`, `CachedAbsoluteStart`, `CachedAbsoluteEnd`.
  Monaco uses JavaScript numbers (53-bit), and the ported test `force delta overflow` (P3-06) inserts values above
  2^31. `MinSafeDelta = -(1L << 30)`, `MaxSafeDelta = 1L << 30` (same as Monaco).
- `IntervalNode` fields are public fields or auto-properties named like Monaco in PascalCase: `Metadata` (`int`),
  `Parent`, `Left`, `Right`, `Start`, `End`, `Delta`, `MaxEnd`, `Id` (`string`), `OwnerId` (`int`), `Options`,
  `CachedVersionId` (`int`), `CachedAbsoluteStart`, `CachedAbsoluteEnd`, `Range` (`Range?`).
  Use fields (not properties) for `Parent`, `Left`, `Right`, `Start`, `End`, `Delta`, `MaxEnd`, `Metadata`: the
  algorithms assign them constantly. Field naming rule: public fields are PascalCase (no underscore).
- The metadata bit layout is Monaco's (color bit 0, visited bit 1, for-validation bit 2, stickiness bits 3–4,
  collapse-on-replace bit 5, in-glyph-margin bit 6, affects-font bit 7). Port the getters/setters as instance
  properties of `IntervalNode`:
  `Color`, `IsVisited`, `IsForValidation`, `Stickiness`, `CollapseOnReplaceEdit`, `IsInGlyphMargin`, `AffectsFont`
  that read and write `Metadata` with the same masks.
- `ModelDecorationOptions` is minimal in phase 3 (decorations come later): `internal sealed class` with
  `TrackedRangeStickiness Stickiness`, `bool CollapseOnReplaceEdit`, `string? ClassName`, `string? GlyphMarginClassName`,
  `bool AffectsFont`, all `init`-only. `IntervalNode.SetOptions` is ported as in lines 207–219 (`IsForValidation` is
  true for the class names `squiggly-error`, `squiggly-warning`, `squiggly-info`: keep these three strings as constants).
- `getAllInOrder` needs `search` (P3-06). In this card, write the tests with a test helper that walks the tree in
  order and resolves offsets (`IntervalTreeInvariants.InOrder(tree)`).

## Tests

- [ ] Port the invariant helpers `assertTreeInvariants`, `depth`, `assertValidNode`, `assertValidTree` to
      `IntervalTreeInvariants` (xUnit `Assert`). They check: sentinel unchanged, root black, equal black depth,
      red nodes have black children, ordering by `intervalCompare` with deltas, and `MaxEnd`.
- [ ] New: insert the 10 intervals of the Cormen tree (`[16,21] [8,9] [25,30] [5,8] [15,23] [17,19] [26,26] [0,3]
      [6,10] [19,20]`); invariants hold after each insert; in-order list is sorted by `(start, end)`
- [ ] New: randomized test with `new Random(seed)` (seeds 1…20 in a `[Theory]`, seed in the failure message): 200
      random inserts and deletes of intervals in `[0, 1000)`, invariants checked after each operation, in-order list
      compared with a sorted `List<(long, long)>` oracle (port of the `Oracle` class at the top of the test file)
- [ ] New: deleting the root, a leaf and a node with two children keeps the invariants

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `editor/common/model/intervalTree.ts` → `LoomEditor.Core/Decorations/` (🔶 in progress, P3-05);
      behavior differences: "one sentinel per interval tree" and "interval offsets are `long`"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`search`, `intervalSearch`, `collectNodes*`, `resolveNode` (P3-06); `acceptReplace`, `nodeAcceptEdit` (P3-07);
the decorations API of `TextModel`.

## Suggested commit message

`feat(core): port the interval tree nodes and red-black operations`
