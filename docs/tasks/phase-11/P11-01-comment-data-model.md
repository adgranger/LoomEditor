# P11-01 — Comment data model: `Comment`, `CommentAuthor`, `CommentThread`

- **Status:** done
- **Depends on:** P10-31
- **Size:** S

## Goal

The public data types of phase 11 exist in `LoomEditor.Core`: a thread on a line with comments, authors, timestamps and a state, with one `Changed` event. No UI, no editor.

## Sources

Sources: none (specified in this card; ADR 0014 decision 3). Read first: `src/LoomEditor.Core/Markers/` (a small public record family and its `PublicAPI.Unshipped.txt` lines) to match style.

## Files to create or change

- `src/LoomEditor.Core/Comments/CommentAuthor.cs`, `Comment.cs`, `CommentThreadState.cs`, `CommentThread.cs`, `CommentThreadChangedEventArgs.cs` — create
- `src/LoomEditor.Core/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.Core`)
- `tests/LoomEditor.Core.Tests/Comments/CommentThreadTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.Core.Comments`. File-scoped, XML doc on every public member, `<remarks>Threading: …</remarks>` on every class.
- `public sealed record CommentAuthor(string Name, string? AvatarUrl = null)`; `Name` must not be null or blank (`ArgumentException`).
- `public sealed record Comment`: `string Id`, `CommentAuthor Author`, `string Body` (Markdown), `DateTimeOffset CreatedAt`, `DateTimeOffset? UpdatedAt = null`. Not a positional record: write an explicit constructor (so it can validate) that throws `ArgumentException` for a blank `Id` and `ArgumentNullException` for `Author` / `Body`. `Body` may be empty.
- `public static Comment Comment.Create(CommentAuthor author, string body, DateTimeOffset? createdAt = null)` — `Id = Guid.NewGuid().ToString("N")`, `CreatedAt = createdAt ?? DateTimeOffset.UtcNow`.
- `public enum CommentThreadState { Open, Resolved }`.
- `public sealed class CommentThread`:
  - `string Id { get; }`; `int LineNumber { get; internal set; }` (1-based; setting a different value raises `Changed` with `Kind = LineChanged`); `string? Title { get; set; }` (raises `TitleChanged` when different); `CommentThreadState State { get; set; }` (raises `StateChanged` when different).
  - `IReadOnlyList<Comment> Comments { get; }` (a stable read-only view; never `null`).
  - `void AddComment(Comment comment)` — `ArgumentNullException`; `ArgumentException` when a comment with the same `Id` exists; raises `CommentAdded`.
  - `bool RemoveComment(string id)` — `false` when unknown; raises `CommentRemoved`.
  - `bool ReplaceComment(Comment comment)` — replaces the comment with the same `Id` in place (keeps order); `false` when unknown; raises `CommentReplaced`.
  - `event EventHandler<CommentThreadChangedEventArgs>? Changed`.
  - `internal CommentThread(string id, int lineNumber, string? title = null)` — validates `lineNumber >= 1`. There is **no** public constructor (ADR 0014: a thread is created by `AddCommentThread`).
- `public enum CommentThreadChangeKind { CommentAdded, CommentRemoved, CommentReplaced, StateChanged, TitleChanged, LineChanged }`; `public sealed class CommentThreadChangedEventArgs(CommentThreadChangeKind kind, Comment? comment) : EventArgs` with `Kind` and `Comment` (the added / removed / new comment; `null` for the other kinds).
- Handlers that throw are not caught. The event is raised **after** the state changed, on the caller's thread (UI thread, documented).
- No `INotifyPropertyChanged`, no `ObservableCollection`, no LINQ-heavy code in the hot paths (lists are tiny).

## Tests

- [x] New: `Comment_BlankId_Throws`, `Comment_Create_AssignsIdAndTimestamp`, `Author_BlankName_Throws`, `AddComment_Duplicate_Throws`, `AddComment_RaisesCommentAdded`, `RemoveComment_Unknown_ReturnsFalse`, `RemoveComment_RaisesCommentRemoved`, `ReplaceComment_KeepsOrder`, `ReplaceComment_Unknown_ReturnsFalse`, `State_SameValue_RaisesNothing`, `State_Changed_RaisesStateChanged`, `Title_Changed_RaisesTitleChanged`, `LineNumber_Changed_RaisesLineChanged`, `Constructor_LineZero_Throws`, `Comments_IsNotModifiableByCaller`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` lists exactly the public members above
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Anchoring to a line (P11-09), the editor API (P11-10), any UI, edit / delete of comments by the user, reactions.

## Suggested commit message

`feat(core): add the comment thread data model`
