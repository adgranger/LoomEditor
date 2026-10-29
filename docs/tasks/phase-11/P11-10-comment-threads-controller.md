# P11-10 — `CommentThreadsController` (View), the apply-suggestion path and the `CommentThreads` feature

- **Status:** todo
- **Depends on:** P11-06, P11-08, P11-09
- **Size:** M

## Goal

One controller per editor owns the threads (manager + one view model per thread), raises the events the control will expose, and applies a suggestion with one undo step. A small feature registers the command `loom.commentThreads.add`. No WinUI code: everything is unit tested.

## Sources

Sources: none (specified in this card; ADR 0014 decisions 2 and 6). Read first:
- `src/LoomEditor.View/Features/EditorContext.cs` (internal per-editor properties such as `SnippetController`, `Announcer`, `Id`, `ViewModel`) and `EditorContextExtensions.cs` (`GetSnippetController()`): **the controller is stored the same way**;
- `src/LoomEditor.View/Features/LinesOperations/LinesOperationsActions.Case.cs` lines 43–45: the edit path to copy (`Model.PushStackElement()`, `ViewModel.ExecuteEdits(source, edits, _ => null)`, `Model.PushStackElement()`), and `src/LoomEditor.Core/Model/Edits/IdentifiedEditOperation.cs`;
- `src/LoomEditor.View/Commands/EditorCommand.cs` and `CommandRegistry.cs` (a command takes **no argument**: `new EditorCommand(id, Action)`);
- `src/LoomEditor.View/Features/SelectAll/` (the smallest feature), `docs/how-to/add-a-feature.md`, `src/LoomEditor.View/Features/EditorFeatures.cs`, `tests/LoomEditor.Architecture.Tests/ConventionTests.cs` (a feature folder needs `XxxFeature.cs`, `README.md` and `new XxxFeature()` in `EditorFeatures.cs`);
- `tests/LoomEditor.View.Tests/TestHelpers/TestCodeEditor.cs` and a test that builds an `EditorContext` (grep `new EditorContext(` in `tests/LoomEditor.View.Tests`).

## Files to create or change

- `src/LoomEditor.View/Comments/CommentThreadsController.cs`, `ApplySuggestionResult.cs` — create
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `internal CommentThreadsController? CommentThreads { get; set; }` (doc comment: set by the control)
- `src/LoomEditor.View/Features/EditorContextExtensions.cs` — change: `GetCommentThreadsController()`
- `src/LoomEditor.View/Features/CommentThreads/CommentThreadsFeature.cs`, `CommentThreadsCommands.cs`, `README.md` — create
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: one line
- `tests/LoomEditor.View.Tests/Comments/CommentThreadsControllerTests.cs`, `tests/LoomEditor.View.Tests/Features/CommentThreads/CommentThreadsFeatureTests.cs` — create

## Design decisions (already made — do not change)

- **`IEditorContext` (public) is not changed.** The controller lives in `EditorContext.CommentThreads` (internal) and is **created by the control** (P11-11), not by the feature, so the API works with any feature list (ADR 0014 decision 2). In tests, create it directly.
- `internal sealed class CommentThreadsController : IDisposable`, ctor `(EditorContext context)`: builds `CommentThreadManager(context.Model, context.ViewModel.ViewZones, context.Id)`. Members:
  - `CommentThreadManager Manager`; `IReadOnlyList<CommentThread> Threads` (= manager's);
  - `CommentThread Add(int lineNumber, string? title, bool startComposing)` — creates the thread and its `CommentThreadViewModel` (`getLineText` = `() => context.Model.GetLineContent(thread.LineNumber)`); `startComposing` → `StartComposing()` on it; raises `ThreadAdded` (`EventHandler<CommentThread>`) with the thread;
  - `bool Remove(CommentThread thread)` — raises `ThreadRemoving` (`EventHandler<CommentThread>`) **before** removing (the WinUI host moves the focus away first, ADR 0014 decision 7), disposes the view model, removes from the manager;
  - `CommentThreadViewModel GetViewModel(CommentThread thread)` (`ArgumentException` for an unknown thread);
  - `string GetLineText(CommentThread thread)`.
- **Events of the view models are forwarded:** `SubmitRequested` → controller event `CommentSubmitted` (`EventHandler<(CommentThread Thread, string Text)>`); `CancelRequested` → `CommentCancelled` (`EventHandler<CommentThread>`), then, when the outcome is `RemoveThread`, `Remove(thread)`. A handler may call `Remove` or `thread.AddComment` inside the event (test it).
- `void RequestAdd(int lineNumber)` → raises `AddCommentRequested` (`EventHandler<int>`); the controller never adds a thread by itself on a request.
- **`ApplySuggestionResult ApplySuggestion(CommentThread thread, string commentId, int suggestionIndex, string? renderedLineText)`** — `internal enum ApplySuggestionResult { Applied, ReadOnly, Stale, NoOp, NotFound }`, checked in this order:
  1. unknown thread, comment or suggestion index (`SuggestionExtractor.Extract(comment.Body)`) → `NotFound`;
  2. `context.IsReadOnly` → `ReadOnly`;
  3. `renderedLineText != null && SuggestionEdit.IsStale(model, thread.LineNumber, renderedLineText)` → `Stale`;
  4. `SuggestionEdit.IsNoOp(...)` → `NoOp`;
  5. otherwise build the edit with `SuggestionEdit.Create` and apply it exactly like `LinesOperationsActions.Case.cs`: `PushStackElement`, `ViewModel.ExecuteEdits("loom.commentThreads.applySuggestion", [new IdentifiedEditOperation(edit.Range, edit.Text)], _ => [new Selection(line, 1, line, 1)])`, `PushStackElement`; raise `SuggestionApplied` (`EventHandler<(CommentThread Thread, Comment Comment, int LineNumber)>`); announce `CommentStrings.SuggestionApplied` through `context.Announcer`; return `Applied`.
  `Selection` is `LoomEditor.Core.Text.Selection`; check its constructor and the `cursorStateComputer` signature (`Func<IReadOnlyList<ValidEditOperation>, IReadOnlyList<Selection>?>`).
- **Announcements** (through `context.Announcer.Announce`, the existing `IEditorAnnouncer`): `ThreadAdded(line)` only when `startComposing` is `true`; `CommentPosted` when a thread raises `CommentAdded` while its view model was composing; `SuggestionApplied` (above). P11-20 checks they are heard.
- **Feature** (`CommentThreadsFeature : IEditorFeature`, id `"loom.commentThreads"`): registers `CommentThreadsCommands.Add = "loom.commentThreads.add"` with **no default keybinding**; executing it calls `context.GetCommentThreadsController()?.RequestAdd(cursorLine)` (the primary cursor's line; does nothing when there is no controller). It works in a read-only editor (a comment does not edit the text). Listed in `EditorFeatures.cs` next to its neighbours.
- `Dispose()` disposes the view models and the manager.

## Tests

- [ ] Controller: `Add_ReturnsThreadOnLine`, `Add_StartComposing_ViewModelIsComposing`, `Add_RaisesThreadAdded`, `Add_StartComposing_Announces`, `Add_Silent_DoesNotAnnounce`, `Remove_RaisesThreadRemovingBeforeRemoval`, `Remove_DisposesViewModel`, `Submit_RaisesCommentSubmittedWithText`, `Cancel_NoComments_RemovesThread`, `Cancel_WithComments_KeepsThread`, `Cancel_HandlerRemovesThread_NoException`, `CommentAdded_WhileComposing_AnnouncesPosted`, `RequestAdd_RaisesWithLine`, `ApplySuggestion_ReplacesLineAndRaisesEvent`, `ApplySuggestion_OneUndoRestoresText`, `ApplySuggestion_PutsCursorAtLineStart`, `ApplySuggestion_ReadOnly_ReturnsReadOnly`, `ApplySuggestion_StaleLine_ReturnsStale`, `ApplySuggestion_NoOp_ReturnsNoOp`, `ApplySuggestion_UnknownComment_ReturnsNotFound`, `ApplySuggestion_BadIndex_ReturnsNotFound`, `ApplySuggestion_Deletion_RemovesLine`, `ThreadsFollowEdits_ThroughController`
- [ ] Feature: `AddCommand_RaisesAddRequestedForCursorLine`, `AddCommand_NoController_DoesNothing`, `AddCommand_ReadOnly_StillRaises`
- [ ] Architecture tests pass (the new feature folder)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `PublicAPI.Unshipped.txt` of View unchanged except `CommentThreadsFeature` / `CommentThreadsCommands` if they are public like their neighbours (check: other `XxxFeature` / `XxxCommands` are public; follow them and run `./eng/Update-PublicApi.ps1 src/LoomEditor.View`)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The public API of the control (P11-11), any WinUI element, the diff editor.

## Suggested commit message

`feat(view): add the comment threads controller and the add-comment command`
