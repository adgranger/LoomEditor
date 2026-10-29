# P12-16 — `CommentThread` knows its columns: `StartColumn`, `EndColumn`, `IsWholeLines`

- **Status:** done
- **Depends on:** P12-14
- **Size:** S

**Recommended model:** Sonnet

## Goal

`CommentThread` can be anchored on an exact text range: it knows its start and end columns and whether it covers whole lines. The editor sets lines and columns at once
with one internal method; a change of columns alone raises no event. Nothing else uses it yet.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 14 and 16). Read first: `src/LoomEditor.Core/Comments/CommentThread.cs` (the two internal
constructors, `SetLines`), `src/LoomEditor.Core/Text/Range.cs` (the constructor orders the two positions), `tests/LoomEditor.Core.Tests/Comments/CommentThreadTests.cs`,
`docs/tasks/phase-12/P12-01-comment-thread-line-range.md` (the card this one extends).

## Files to create or change

- `src/LoomEditor.Core/Comments/CommentThread.cs` — change
- `src/LoomEditor.Core/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.Core`)
- `tests/LoomEditor.Core.Tests/Comments/CommentThreadTests.cs` — change

## Design decisions (already made — do not change)

- New fields `_startColumn`, `_endColumn`. New public members, get only, XML doc on each:
  - `public int StartColumn => _startColumn;` — "The 1-based column where the commented text starts; 1 for a thread on whole lines. Maintained by the editor; a change of
    columns alone raises no <see cref="Changed"/> event."
  - `public int EndColumn => _endColumn;` — "The 1-based column just after the commented text, on <see cref="LineNumber"/> (exclusive, as in <see cref="Range"/>); for
    a thread on whole lines, the end of its last line. Maintained by the editor; a change of columns alone raises no <see cref="Changed"/> event."
  - `public bool IsWholeLines { get; }` — "Whether the thread covers whole lines (<see langword="true"/>) or an exact text range; fixed at creation."
- New **internal** constructor `CommentThread(string id, Range textRange, string? title = null)`: an exact thread. (`Range` is a struct: no null check)
  `ArgumentOutOfRangeException.ThrowIfLessThan(textRange.StartLineNumber, 1)`, `ThrowIfLessThan(textRange.StartColumn, 1)`; lines and columns from the range;
  `IsWholeLines = false`. It chains to a private constructor shared with the line constructors (no duplicated validation).
- The existing internal constructors keep their signatures and set `IsWholeLines = true`, `StartColumn = 1`, `EndColumn = 1` (the editor sets the real end column right
  after creating the thread with `SetRange`, P12-18; a thread built outside the editor, in tests, keeps 1). Overload resolution: `new CommentThread("a", range)` picks the new
  one; every existing call (`("a", 3)`, `("a", 3, 5)`, `("a", 3, "t")`, `("a", 3, null)`) still picks the old ones — `Range` does not convert from `int` or `string`.
- New `internal void SetRange(int startLineNumber, int startColumn, int lineNumber, int endColumn)`: `Guard.Assert(1 <= startLineNumber && startLineNumber <= lineNumber &&
  startColumn >= 1 && endColumn >= 1 && (startLineNumber < lineNumber || startColumn <= endColumn))`; sets the four fields; raises `Changed` **once** with `LineChanged`
  **only when** `StartLineNumber` or `LineNumber` changed (README decision 16). Same values: nothing.
- `SetLines(int startLineNumber, int lineNumber)` stays (tests and P12-05 use it) and becomes `=> SetRange(startLineNumber, _startColumn, lineNumber, _endColumn);`.
- Class summary: "A discussion anchored on a line, a range of lines or an exact text range: …".
- `PublicAPI.Unshipped.txt`: exactly three lines, `StartColumn.get -> int`, `EndColumn.get -> int`, `IsWholeLines.get -> bool`.

## Tests

- [x] `Ctor_Lines_IsWholeLinesWithColumnOne`, `Ctor_TextRange_SetsLinesAndColumns`, `Ctor_TextRange_IsNotWholeLines`
- [x] `Ctor_TextRange_RightToLeftRange_IsOrdered` (`new Range(4, 9, 4, 5)` gives start column 5, end column 9)
- [x] `SetRange_ColumnsOnly_RaisesNothing` (the columns are updated), `SetRange_LineChanged_RaisesLineChangedOnce`, `SetRange_Same_RaisesNothing`
- [x] `SetLines_KeepsColumns` (an exact thread keeps its columns when `SetLines` moves it)
- [x] All existing `CommentThreadTests` and every View test unchanged and green

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` (Core) gains exactly the three lines; no other public change
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Tracking columns in the manager (P12-18), the normalization rule (P12-17), the public `Range` overloads (P12-21), a new `CommentThreadChangeKind` member.

## Suggested commit message

`feat(core): give comment threads start and end columns`
