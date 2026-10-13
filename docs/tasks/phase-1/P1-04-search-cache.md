# P1-04 — Piece tree search cache

- **Status:** todo
- **Depends on:** P1-03
- **Size:** S

## Goal

The tiny cache that remembers the last node found for an offset or a line number, so that consecutive lookups
(the common case when typing) skip the tree walk.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` | 201–205 | `CacheEntry` |
| same | 207–266 | `PieceTreeSearchCache` (`get`, `get2`, `set`, `validate`) |

## Files to create or change

- `src/LoomEditor.Core/Model/PieceTree/PieceTreeSearchCache.cs` — create (`CacheEntry` nested or in its own file)
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeSearchCacheTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed record CacheEntry(TreeNode Node, int NodeStartOffset, int? NodeStartLineNumber)`.
- Method names: `Get(int offset)` (Monaco `get`), `GetByLine(int lineNumber)` (Monaco `get2`), `Set(CacheEntry)`,
  `Validate(int offset)`. `Get`/`GetByLine` return `null` when nothing matches.
- Monaco's `node.parent === null` check becomes `node.IsDetached` (see P1-03).
- Keep Monaco's exact comparison operators (`<=`, `>=`, `<`): they matter at piece boundaries.
- Storage: a `List<CacheEntry>`; `Set` removes the oldest entry when `Count >= limit`.

## Tests

- [ ] `Get` hits when `NodeStartOffset <= offset <= NodeStartOffset + piece length`, misses otherwise (both edges)
- [ ] `GetByLine` requires `NodeStartLineNumber` set and `start < line <= start + piece line feeds`
- [ ] `Set` beyond the limit evicts the oldest entry
- [ ] `Validate(offset)` removes entries with `NodeStartOffset >= offset` and entries whose node is detached, keeps the others

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Using the cache (P1-05/P1-06).

## Suggested commit message

`feat(core): port the piece tree search cache`
