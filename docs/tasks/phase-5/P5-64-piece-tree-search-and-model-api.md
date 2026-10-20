# P5-64 — Piece tree search and `TextModel.FindMatches / FindNextMatch / FindPreviousMatch`

- **Status:** todo
- **Depends on:** P5-63
- **Size:** M

## Goal

The model answers find requests like Monaco's `ITextModel`: several search scopes, the 999 default limit, and the fast
path that searches a plain-text query directly in the pieces of the piece tree (`findMatchesLineByLine`). The 3 deferred
`chunk based search` tests of the piece tree pass (84 / 87 → 87 / 87).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/model/pieceTreeTextBuffer/pieceTreeBase.ts` | 690–736 | `findMatchesInNode` |
| same | 738–810 | `findMatchesLineByLine` |
| same | 812–844 | `_findMatchesInLine` |
| same | 1530–1595 | `nodeAt2` returning `null` (see decisions) |
| `common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts` | 520–522 | `findMatchesLineByLine` |
| `common/model/textModel.ts` | 125 | `LIMIT_FIND_COUNT` |
| same | 1174–1263 | `findMatchesLineByLine`, `findMatches`, `findNextMatch`, `findPreviousMatch` |
| `src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` | 2067–2125 | suite `chunk based search` |

## Files to create or change

- `src/LoomEditor.Core/Model/PieceTree/PieceTreeBase.Search.cs` — create (partial: `FindMatchesInNode`, `FindMatchesLineByLine`, `FindMatchesInLine`)
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeBase.Positions.cs` — change: add `TryNodeAt2` (below)
- `src/LoomEditor.Core/Model/ITextBuffer.cs`, `PieceTree/PieceTreeTextBuffer.cs` — change: add `FindMatchesLineByLine`
- `src/LoomEditor.Core/Model/TextModel.Search.cs` — change: `FindMatches` (two overloads), `FindNextMatch`, `FindPreviousMatch`
- `src/LoomEditor.Core/Model/PieceTree/README.md` — update (search is ported)
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeChunkBasedSearchTests.cs` — create
- `tests/LoomEditor.Core.Tests/Model/TextModelSearchApiTests.cs` — create

## Design decisions (already made — do not change)

- **`nodeAt2` and `null`.** Monaco's `nodeAt2` returns `null` past the end (it does in test `#45892`, an empty buffer). Add
  `internal NodePosition? TryNodeAt2(int lineNumber, int column)` with the body of `NodeAt2`, returning `null` where Monaco
  does; `NodeAt2` becomes `TryNodeAt2(...) ?? throw new ArgumentOutOfRangeException(...)` (same message as today).
  `FindMatchesLineByLine` uses `TryNodeAt2` and returns `[]` on `null`, like Monaco lines 743–750.
- **The buffer is a `char[]`, not a string** (`StringBuffer`). `findMatchesInNode` always searches
  `buffer.Substring(start, end - start)` with `offsetInBuffer = offset + start` and `searcher.Reset(0)` — Monaco's
  `wordSeparators` branch (lines 702–705) for both branches. Behavior difference (XML doc + PORTING): without word
  separators Monaco searches the whole buffer from `start` and can report a match that **crosses** the end of the search
  range; here it cannot (same result as `TextModelSearch`'s line-by-line search). Only plain-text queries reach this path,
  so anchors and lookarounds never see the cut.
- The `getLineFeedCnt` of Monaco is the existing `GetLineFeedCount`; `offsetInBuffer`, `positionInBuffer`,
  `getLineContent` exist (phase 1).
- `TextModel.FindMatches`:
  - `FindMatches(string searchString, bool searchOnlyEditableRange, bool isRegex, bool matchCase, string? wordSeparators, bool captureMatches, int limitResultCount = LimitFindCount)`
    searches the full model (Monaco's `boolean` scope);
  - `FindMatches(string searchString, IReadOnlyList<Range> searchScopes, …same…)` validates each range (`ValidateRange`).
  - Sort the ranges with a **stable** sort on (start line, start column) (`OrderBy(...).ThenBy(...)`), then merge with
    Monaco's `reduce`: ranges that intersect (`Range.Intersects`, Monaco `Range.areIntersecting`) are joined with `Plus`.
  - `!isRegex && !searchString.Contains('\n')` → `SearchParams.ParseSearchRequest` once, then `_buffer.FindMatchesLineByLine`
    per range; otherwise `TextModelSearch.FindMatches` per range. Concatenate in range order.
- `FindNextMatch`: port lines 1227–1257 **without** the call to `TextModelSearch.findNextMatch` whose result is discarded
  (line 1241: it has no side effect; dropping it is not an optimization of the algorithm). Note it in a comment.
- All new members are `internal`. `TextModel` stays public; no public search API in phase 5.
- `Constants.MAX_SAFE_SMALL_INTEGER` (used by later cards as a limit) is `1 << 30`: add
  `internal const int MaxSafeSmallInteger = 1 << 30;` to `TextModel.Search.cs` (Monaco `base/common/uint.ts`).

## Tests

- [ ] Port suite `chunk based search` (3): `#45892. For some cases, the buffer is empty but we still try to search`,
      `#45770. FindInNode should not cross node boundary.`, `search searching from the middle`. Build buffers with
      `TestTextBuffers.Create(...)` (P1-09), `new SearchData(new Regex("abc"), WordCharacterClassifierCache.Get(",./", []), "abc")`.
      `/\[/gi` → `new Regex(@"\[", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)`.
- [ ] New: `FindMatches_OverlappingScopes_AreMergedAndSearchedOnce`, `FindMatches_UnsortedScopes_ResultsInDocumentOrder`,
      `FindMatches_PlainText_UsesPieceTreeAndEqualsTextModelSearch` (same ranges as `TextModelSearch.FindMatches` on a
      buffer built from 3 chunks), `FindMatches_DefaultLimit_Is999`, `FindNextMatch_PastLastMatch_WrapsToTop`,
      `FindPreviousMatch_BeforeFirstMatch_WrapsToBottom`, `TryNodeAt2_EmptyBuffer_ReturnsNull`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: rows `pieceTreeBase.ts` and `pieceTreeTextBuffer.ts` ✅ (search P5-64), `textModel.ts` (+ find);
      `ITextBuffer` difference row without "no `findMatchesLineByLine` yet"; behavior difference of `findMatchesInNode`;
      tests table `pieceTreeTextBuffer.test.ts` 87 / 87; remove the "Search in the piece tree is deferred" sentence
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The find widget's search (P5-68 calls these methods), `isTooLargeForHeapOperation`.

## Suggested commit message

`feat(core): search the piece tree and the text model`
