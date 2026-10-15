# P3-08 — Tracked ranges in `TextModel`

- **Status:** todo
- **Depends on:** P3-07
- **Size:** S

## Goal

A tracked range is a range of the model that moves with the text when the text is edited. The cursor stores each of
its selections as a tracked range, so that after an edit (typing, undo, another cursor's edit) it can read the
selection back at its new place. Port Monaco's internal `_setTrackedRange` / `_getTrackedRange` on top of the interval
tree (P3-05 … P3-07). ADR 0006, decision 4.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/model/textModel.ts`. If a function is not exactly at the given line, search for its name.

| Lines | What to port |
|---|---|
| 1741–1775 | `_getTrackedRange`, `_setTrackedRange` |
| 1976–2070 | `_deltaDecorationsImpl` — **only** the path used by `_setTrackedRange` for a new node (owner 0, no old ids, one new decoration, `suppressEvents = true`): id generation `` `${this._instanceId};${++this._lastDecorationId}` ``, `new IntervalNode(id, 0, 0)`, validate the range, offsets, `node.ownerId`, `node.reset`, `node.setOptions`, insert |
| 925–984 | `_validateRangeRelaxedNoAllocations` (like `validateRange`, but a position inside a surrogate pair is allowed) |
| 1503–1520 | `_doApplyEdits`: the first loop that calls `this._decorationsTree.acceptReplace(change.rangeOffset, change.rangeLength, change.text.length, change.forceMoveMarkers)` for each change, **before** the raw change events are built |
| 525–527 | `_setValueFromTextBuffer`: "Destroy all my decorations" (new tree, empty map) |
| 1886–1888 | `getRangeAt(start, end)` |
| 2197–2345 | `class DecorationsTrees` — **only** `_decorationsTree0` is needed (tracked ranges are neither in the overview ruler nor injected text): `insert`, `delete`, `getNodeRange`, `_resolveNode`, `acceptReplace` |
| 2573–2578 | `TRACKED_RANGE_OPTIONS` (one `ModelDecorationOptions` per stickiness) |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModel.TrackedRanges.cs` — create (partial): `SetTrackedRange`, `GetTrackedRange`, `ValidateRangeRelaxed`, `GetRangeAt`
- `src/LoomEditor.Core/Decorations/DecorationsTrees.cs` — create (one tree in phase 3; see below)
- `src/LoomEditor.Core/Model/TextModel.Edits.cs` — change: call `AcceptReplace` in `DoApplyEdits`; reset in `SetValue`
- `tests/LoomEditor.Core.Tests/Model/TextModelTrackedRangeTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- **Visibility.** Today `LoomEditor.Core` gives `InternalsVisibleTo` only to `LoomEditor.Core.Tests` and
  `LoomEditor.Benchmarks` (`src/Directory.Build.props`). The cursor in `LoomEditor.View` needs the internal members of
  this card and of P3-01 … P3-04. Add to `src/LoomEditor.Core/LoomEditor.Core.csproj`:
  `<InternalsVisibleTo Include="LoomEditor.View" />` and `<InternalsVisibleTo Include="LoomEditor.View.Tests" />`
  (ADR 0006, consequences).
- Signatures (all `internal`):
  - `internal string? SetTrackedRange(string? id, Range? newRange, TrackedRangeStickiness stickiness)` — returns the
    id, or `null` when `newRange` is `null` (deleted).
  - `internal Range? GetTrackedRange(string id)` — `null` when the id is unknown.
  - `internal Range ValidateRangeRelaxed(Range range)` (Monaco: `_validateRangeRelaxedNoAllocations`).
  - `internal Range GetRangeAt(int start, int end)` → `_buffer.GetRangeAt(start, end - start)`.
- `DecorationsTrees` is `internal sealed class` with **one** `IntervalTree` in phase 3, and the methods `Insert`,
  `Delete`, `GetNodeRange(TextModel host, IntervalNode node)`, `AcceptReplace(...)`. Write a comment that Monaco has
  three trees (normal, overview ruler, injected text) and that the other two come with decorations.
- The model keeps `Dictionary<string, IntervalNode> _decorations` and `int _lastDecorationId`; the instance id is a
  static counter (`Interlocked.Increment`) formatted like Monaco (`$"{_instanceId};{++_lastDecorationId}"`).
- Offsets: the tree uses `long` (P3-05); the model uses `int`. Convert with `(int)` when reading
  `CachedAbsoluteStart`/`CachedAbsoluteEnd` back (they never exceed the document length).
- `GetNodeRange` caches the `Range` per version id exactly like lines 2327–2336 (`node.Range` is cleared by
  `SetCachedOffsets` when the version changes).
- `SetValue` (flush) destroys all tracked ranges, like Monaco: `GetTrackedRange` of an old id returns `null` after it.
- Undo and redo go through `DoApplyEdits` too, so tracked ranges follow undo/redo with no extra code. Check it with a test.

## Tests

- [ ] New: a range `(1,3)-(1,5)` with `AlwaysGrowsWhenTypingAtEdges`; insert `"xx"` at `(1,1)` → `(1,5)-(1,7)`;
      insert `"y"` at `(1,7)` (its end) → `(1,5)-(1,8)` (it grows)
- [ ] New: same with `NeverGrowsWhenTypingAtEdges` → inserting at its end leaves `(1,5)-(1,7)`
- [ ] New: deleting lines 2–3 moves a range on line 5 to line 3; deleting the text that contains a range collapses it
- [ ] New: `SetTrackedRange(id, null, …)` deletes it; `GetTrackedRange(id)` is then `null`; `SetTrackedRange(null, null, …)` returns `null`
- [ ] New: `SetTrackedRange(existingId, otherRange, …)` moves it and keeps the same id
- [ ] New: `SetValue` forgets every tracked range
- [ ] New: `PushEditOperations` then `Undo` puts a range back where it was
- [ ] New: a range given with a column inside a surrogate pair is accepted by `ValidateRangeRelaxed` and kept as is

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `textModel.ts` mentions tracked ranges (P3-08); behavior difference "one decorations
      tree in phase 3 (no overview-ruler or injected-text tree, no public decorations API)"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`deltaDecorations`, `getDecorationsInRange`, decoration events, options other than stickiness (later phases).

## Suggested commit message

`feat(core): add tracked ranges to the text model`
