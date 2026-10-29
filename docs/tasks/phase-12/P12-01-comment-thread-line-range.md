# P12-01 — `CommentThread` covers a range of lines: `StartLineNumber`

- **Status:** done
- **Depends on:** P11-25
- **Size:** S

**Recommended model:** Sonnet

## Goal

`CommentThread` knows the first line of its range (`StartLineNumber`) in addition to `LineNumber` (the last line, where the box is). The editor sets both at once with one
internal method that raises `LineChanged` once. Nothing else uses it yet.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 2–4). Read first: `src/LoomEditor.Core/Comments/CommentThread.cs`,
`CommentThreadChangeKind.cs`, `tests/LoomEditor.Core.Tests/Comments/CommentThreadTests.cs`, `src/LoomEditor.View/Comments/CommentThreadManager.cs` (the only caller of the
internal constructor and of the `LineNumber` setter).

## Files to create or change

- `src/LoomEditor.Core/Comments/CommentThread.cs` — change
- `src/LoomEditor.Core/Comments/CommentThreadChangeKind.cs` — change: XML doc of `LineChanged` only ("The line or the range of lines changed.")
- `src/LoomEditor.Core/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.Core`)
- `src/LoomEditor.View/Comments/CommentThreadManager.cs` — change: the one line in `SyncLines` that sets `entry.Thread.LineNumber` calls `SetLines(line, line)` instead
- `tests/LoomEditor.Core.Tests/Comments/CommentThreadTests.cs` — change

## Design decisions (already made — do not change)

- New field `_startLineNumber`. New **internal** constructor `CommentThread(string id, int startLineNumber, int lineNumber, string? title = null)`:
  `ArgumentOutOfRangeException.ThrowIfLessThan(startLineNumber, 1)`, `ThrowIfLessThan(lineNumber, startLineNumber)`. The existing internal constructor
  `(string id, int lineNumber, string? title = null)` becomes `: this(id, lineNumber, lineNumber, title)`. Watch the overload resolution: `new CommentThread("a", 3, 5)`
  must pick the new one and `new CommentThread("a", 3, "t")` the old one (it does: `int` does not convert to `string?`); `new CommentThread("a", 3, null)` picks the old one.
- `public int StartLineNumber { get; }` (get only, backed by the field). XML doc: "The first 1-based line the thread covers; equal to <see cref="LineNumber"/> for a
  single-line thread. Maintained by the editor."
- `LineNumber`'s XML doc becomes: "The 1-based line the thread's box is under: the last line of its range (GitHub's <c>line</c>). Maintained by the editor."
  The class summary says "anchored on a line or a range of lines".
- New `internal void SetLines(int startLineNumber, int lineNumber)`: `Guard.Assert(1 <= start && start <= line)`; when neither value changes, does nothing; otherwise sets
  both fields and raises `Changed` **once** with `LineChanged`.
- The internal `LineNumber` setter is **removed** (`SetLines` replaces it); fix its only caller in `CommentThreadManager.SyncLines` (`entry.Thread.SetLines(line, line)` —
  P12-05 makes it a real range). Keep `LineNumber { get; }` public and unchanged otherwise.
- `PublicAPI.Unshipped.txt`: one line `LoomEditor.Core.Comments.CommentThread.StartLineNumber.get -> int`.

## Tests

- [x] `Ctor_SingleLine_StartEqualsLine`, `Ctor_Range_SetsBothLines`, `Ctor_EndBeforeStart_Throws`, `Ctor_StartZero_Throws`
- [x] `SetLines_ChangesBoth_RaisesLineChangedOnce`, `SetLines_OnlyStartChanges_RaisesLineChanged`, `SetLines_Same_RaisesNothing`
- [x] The existing `CommentThreadTests` and `tests/LoomEditor.View.Tests/Comments/CommentThreadManagerTests.cs` still pass unchanged

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` (Core) lists `StartLineNumber`; no other public change
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Tracking a range in the manager (P12-05), any WinUI change, a new `CommentThreadChangeKind` member.

## Suggested commit message

`feat(core): give comment threads a start line for multi-line ranges`
