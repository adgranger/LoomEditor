# P3-07 — Interval tree (3/3): edits (`acceptReplace`, `nodeAcceptEdit`)

- **Status:** todo
- **Depends on:** P3-06
- **Size:** S

## Goal

When text is replaced, every interval in the tree moves or grows according to its stickiness, exactly as Monaco's
decorations do. After this card the interval tree is complete and can hold tracked ranges (P3-08).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/model/intervalTree.ts`. If a function is not exactly at the given line, search for its name.

| Lines | What to port |
|---|---|
| 306–333 | `IntervalTree.acceptReplace(offset, length, textLength, forceMoveMarkers)` |
| 390–394 | `const enum MarkerMoveSemantics` |
| 396–410 | `adjustMarkerBeforeColumn` |
| 412–490 | `nodeAcceptEdit` (exported: the test calls it directly) |
| 492–562 | `searchForEditing` |
| 564–632 | `noOverlapReplace` |

Tests: `src/vs/editor/test/common/model/intervalTree.test.ts`, suite `IntervalTree 2`, test `nodeAcceptEdit`
(line 571 to the end of the suite: 207 `assertNodeAcceptEdit('A.000', …)` calls in two groups: A "collapsed decoration" and B "non collapsed decoration").

## Files to create or change

- `src/LoomEditor.Core/Decorations/IntervalTree.Edits.cs` — create (partial)
- `tests/LoomEditor.Core.Tests/Decorations/IntervalTreeNodeAcceptEditTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `public void AcceptReplace(long offset, long length, long textLength, bool forceMoveMarkers)` on `IntervalTree`.
- `internal static void NodeAcceptEdit(IntervalNode node, long start, long end, long textLength, bool forceMoveMarkers)`
  as a static method of `IntervalTree` (Monaco exports it; the test uses it on a node that is not in a tree).
- `MarkerMoveSemantics` is a `private enum` inside `IntervalTree` with Monaco's values.
- Port line by line, including the comments that explain the "two markers" behavior.
- `nodeAcceptEdit` has a test: port **every** `assertNodeAcceptEdit` call with its message (`'A.000'`…) as the assertion
  message, in **two** xUnit tests, one per group (`NodeAcceptEdit_CollapsedDecoration` for A, `NodeAcceptEdit_NonCollapsedDecoration` for B), each with
  `// Monaco: 'nodeAcceptEdit' (group A)`. Do not change any number. Use a local helper
  `AssertNodeAcceptEdit(string msg, long nodeStart, long nodeEnd, TrackedRangeStickiness stickiness, long start, long end, long textLength, bool forceMoveMarkers, long expectedNodeStart, long expectedNodeEnd)`
  that creates `new IntervalNode(string.Empty, nodeStart, nodeEnd)`, sets the stickiness and calls `NodeAcceptEdit`.

## Tests

- [ ] Port suite `IntervalTree 2` › `nodeAcceptEdit` (all groups, every call)
- [ ] New: a tree with `[0,5]` (AlwaysGrows), `[10,15]` (NeverGrows), `[20,20]` (GrowsOnlyWhenTypingAfter):
      `AcceptReplace(3, 0, 2, false)` (insert 2 characters inside the first interval) gives `[0,7] [12,17] [22,22]`
      in `GetAllInOrder()` and the P3-05 invariants hold
- [ ] New: `AcceptReplace(10, 5, 0, false)` (delete the whole second interval) collapses it to `[10,10]`
- [ ] New: 1,000 random `AcceptReplace` calls (seeded `[Theory]`, seed in the message) on a tree of 50 random intervals:
      invariants hold after each call and every node satisfies `0 <= Start <= End`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `intervalTree.ts` ✅ ported (P3-05 … P3-07); tests table `intervalTree.test.ts` 25 / 25
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Using the tree from `TextModel` (P3-08).

## Suggested commit message

`feat(core): port interval tree edits (acceptReplace, nodeAcceptEdit)`
