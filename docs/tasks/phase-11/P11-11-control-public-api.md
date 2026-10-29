# P11-11 — The public API of `LoomEditor`: `AddCommentThread`, `RemoveCommentThread` and the four events

- **Status:** done
- **Depends on:** P11-10
- **Size:** S

## Goal

A host calls `editor.AddCommentThread(line)` and subscribes to `CommentSubmitted`, `CommentCancelled`, `AddCommentRequested` and `SuggestionApplied`. The threads exist and reserve their gap (a `Widget` zone of the default 120 px height) but show **nothing yet**: the element comes with P11-12.

## Sources

Sources: none (specified in this card; ADR 0014 decision 2). Read first: `src/LoomEditor.WinUI/Controls/LoomEditor.cs` (the constructor: where `_context` and `_viewModel` are created, and `Dispose` / unload), `LoomEditor.Snippets.cs` and `LoomEditor.GotoDefinition.cs` (small partials that expose a View feature), `CommentThreadsController.cs` (P11-10), `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` — create (partial class)
- `src/LoomEditor.WinUI/Controls/Comments/CommentEventArgs.cs` — create (the four public args classes)
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: create the controller after the context (`_context.CommentThreads = new CommentThreadsController(_context)`) and dispose it with the context
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)

## Design decisions (already made — do not change)

- **Members** (exactly ADR 0014 decision 2, without `CommentGutterButtonEnabled`, which is P11-21): `public CommentThread AddCommentThread(int lineNumber, string? title = null, bool startComposing = false)` (`ArgumentOutOfRangeException` outside `1..Model.LineCount`), `public bool RemoveCommentThread(CommentThread thread)` (`ArgumentNullException`; `false` for a thread of another editor), `public IReadOnlyList<CommentThread> CommentThreads`, and the events `CommentSubmitted`, `CommentCancelled`, `AddCommentRequested`, `SuggestionApplied` with `sender` = the control.
- **Args classes** (namespace `LoomEditor.WinUI.Controls`, `public sealed`, `EventArgs`, read-only properties, XML doc): `CommentThreadEventArgs(CommentThread thread)`, `CommentSubmittedEventArgs(CommentThread thread, string text)`, `AddCommentRequestedEventArgs(int lineNumber)`, `SuggestionAppliedEventArgs(CommentThread thread, Comment comment, int lineNumber)`.
- The partial subscribes once to the controller's events and re-raises them; nothing else. The controller exists from the constructor on, so the API works before the template is applied and with any `Features` list.
- **Threading:** UI thread only (`<remarks>`); calling from another thread is not checked (same as the rest of the control).
- `IEditorContext` is not changed.

## Tests

- [x] Automated: none new (no WinUI test project; the controller is covered by P11-10). The build proves the public surface (`PublicAPI` analyzer).
- [x] Manual: none in this card (nothing is visible yet).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` (WinUI) lists exactly the members and args classes above
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The widget (P11-12 …), the gutter property (P11-21), the diff editor (P11-22).

## Suggested commit message

`feat(winui): expose comment threads on the editor control`
