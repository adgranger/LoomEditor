# P11-09 — The `CommentThreads` feature and the public API of `LoomEditor`

- **Status:** todo
- **Depends on:** P11-05, P11-07, P11-08
- **Size:** M

## Goal

A host can call `editor.AddCommentThread(line)`, `RemoveCommentThread`, read `CommentThreads` and subscribe to `CommentSubmitted`, `CommentCancelled`, `AddCommentRequested` and `SuggestionApplied`; applying a suggestion edits the model with one undo step. The threads exist and reserve their gap (a `Widget` zone of the default height) but show **nothing yet**: the element comes with P11-10.

## Sources

Sources: none (specified in this card; ADR 0014 decisions 2 and 6). Read first: `docs/how-to/add-a-feature.md`; `src/LoomEditor.View/Features/Hover/HoverFeature.cs` and `HoverCommands.cs` (a feature that creates a controller from `context.GetViewModel()`); `src/LoomEditor.View/Features/Comment/` (toggle line comment: **how a feature applies an edit with an undo stop** — copy that path; do not touch the folder); `src/LoomEditor.View/Features/IEditorContext.cs`; `src/LoomEditor.View/Features/EditorFeatures.cs`; `src/LoomEditor.WinUI/Controls/LoomEditor.Snippets.cs` and `LoomEditor.GotoDefinition.cs` (small control partials that expose a feature); `src/LoomEditor.WinUI/Controls/LoomEditor.ReadOnly.cs`.

## Files to create or change

- `src/LoomEditor.View/Features/CommentThreads/CommentThreadsFeature.cs`, `CommentThreadsController.cs`, `CommentThreadsCommands.cs`, `README.md` — create
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: one line
- `src/LoomEditor.View/Features/IEditorContext.cs` and the context implementation — change only if the control needs to reach the controller through the context (follow what `HoverController` / the snippet controller do)
- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` — create (the public API, partial class)
- `src/LoomEditor.WinUI/Controls/Comments/CommentEventArgs.cs` — create (the four public args classes)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `tests/LoomEditor.View.Tests/Features/CommentThreads/CommentThreadsControllerTests.cs` — create

## Design decisions (already made — do not change)

- **Public API (exactly ADR 0014 decision 2)** on `LoomEditor`: `CommentThread AddCommentThread(int lineNumber, string? title = null, bool startComposing = false)`, `bool RemoveCommentThread(CommentThread thread)`, `IReadOnlyList<CommentThread> CommentThreads`, events `CommentSubmitted` (`EventHandler<CommentSubmittedEventArgs>`), `CommentCancelled` (`EventHandler<CommentThreadEventArgs>`), `AddCommentRequested` (`EventHandler<AddCommentRequestedEventArgs>`), `SuggestionApplied` (`EventHandler<SuggestionAppliedEventArgs>`). The four args classes are public sealed in `LoomEditor.WinUI.Controls` namespace: `CommentThreadEventArgs(CommentThread Thread)`, `CommentSubmittedEventArgs(CommentThread Thread, string Text)`, `AddCommentRequestedEventArgs(int LineNumber)`, `SuggestionAppliedEventArgs(CommentThread Thread, Comment Comment, int LineNumber)`. `CommentGutterButtonEnabled` is **not** in this card (P11-17).
- `AddCommentThread` throws `ArgumentOutOfRangeException` for a line outside the model. `startComposing: true` calls `StartComposing()` on the thread's view model **when the widget exists** (P11-10 onward); here it only records the request on the controller (`CommentThreadsController.GetOrCreateViewModel(thread).StartComposing()`), so the widget finds the state ready.
- **Controller** (`internal sealed class CommentThreadsController : IDisposable`, View): owns the `CommentThreadManager` and one `CommentThreadViewModel` per thread (created on `Add`, disposed on `Remove`; `getLineText` reads the model). It exposes `Add`, `Remove`, `Threads`, `GetViewModel(thread)`, and the **internal** raising methods the widget calls: `RaiseSubmitted(thread, text)`, `RaiseCancelled(thread)` (removes the thread when the view model says `RemoveThread`), `RaiseAddRequested(line)`; plus the `bool ApplySuggestion(CommentThread thread, string commentId, int suggestionIndex)` below. The control forwards controller events to its public events.
- **`ApplySuggestion`**: finds the comment and the block (`SuggestionExtractor`), returns `false` (and changes nothing) when: the editor is read-only (`IEditorContext` read-only flag), the comment or block is unknown, `SuggestionEdit.Create` gives `null`, or `SuggestionEdit.IsStale(model, line, <the line text the UI rendered against>)` — the widget passes that text in an optional parameter `string? renderedLineText` (when `null` no stale check). Otherwise applies the single `TextEdit` **through the editor's edit path with an undo stop** (as the toggle comment feature does) so one `Ctrl+Z` undoes it, puts the cursor at the start of the replaced line, raises `SuggestionApplied` and returns `true`. A no-op suggestion returns `false` without an edit.
- **Commands** (ids in `CommentThreadsCommands`, **no default keybinding**): `loom.commentThreads.add` (raises `AddCommentRequested` for the cursor's line; it works in a read-only editor too: a comment does not edit the text) and `loom.commentThreads.applySuggestion` is **not** a registered command (a command has no arguments in this code base: read `CommandRegistry`; the widget calls the controller method directly). If `CommandRegistry` does support arguments, register it and say so in the card's result.
- The feature is listed in `EditorFeatures.cs` (one line, alphabetical like its neighbours). The control creates the controller where it creates the other feature objects.
- A thread added while the control has no `PART_OverlayCanvas` (no template yet / template without the part) still exists and still reserves its gap: the widget is optional UI.
- Events are raised on the UI thread; a handler may call `thread.AddComment(...)` / `RemoveCommentThread(...)` inside the event (test the controller for re-entrancy: removing the thread inside `CommentCancelled`).

## Tests

- [ ] Controller (View tests, real model and zones): `Add_ReturnsThreadOnLine`, `Add_StartComposing_ViewModelIsComposing`, `Remove_RemovesViewModelAndZone`, `Submitted_RaisedWithText`, `Cancelled_NoComments_RemovesThread`, `Cancelled_WithComments_KeepsThread`, `Cancelled_HandlerRemovesThread_NoException`, `AddRequested_CommandRaisesForCursorLine`, `ApplySuggestion_ReplacesLineAndRaisesEvent`, `ApplySuggestion_OneUndoRestores`, `ApplySuggestion_ReadOnly_ReturnsFalse`, `ApplySuggestion_StaleLine_ReturnsFalse`, `ApplySuggestion_NoOp_ReturnsFalse`, `ApplySuggestion_UnknownComment_ReturnsFalse`, `ApplySuggestion_Deletion_RemovesLine`, `ApplySuggestion_CursorMovesToLine`, `ThreadsFollowEdits_ThroughController`
- [ ] Architecture tests still pass (feature folder rules: `XxxFeature.cs`, `XxxCommands.cs`, `README.md`; listed in `EditorFeatures.cs`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `PublicAPI.Unshipped.txt` (WinUI) lists exactly the public members above
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The WinUI element (P11-10 …), the gutter button and `CommentGutterButtonEnabled` (P11-17), the diff editor (P11-18).

## Suggested commit message

`feat(view): add the comment threads feature and the editor API`
