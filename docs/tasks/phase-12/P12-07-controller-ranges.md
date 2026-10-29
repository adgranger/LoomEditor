# P12-07 — Controller: add, apply a suggestion and request a thread on a range

- **Status:** done
- **Depends on:** P12-06
- **Size:** M

**Recommended model:** Sonnet

## Goal

`CommentThreadsController` creates range threads, gives each view model the range text, applies a suggestion over the whole range (one undo step), and its add-request and
suggestion-applied events carry the first and the last line. The WinUI wiring keeps compiling with the old meaning (P12-09 exposes the new data).

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 2, 8 and 10). Read first: `src/LoomEditor.View/Comments/CommentThreadsController.cs` (all),
`CommentThreadViewModel.cs` (`Title`, `SuggestionSeed`, the `getLineText` delegate), `SuggestionEdit.cs` (P12-02), `CommentStrings.cs` (P12-04),
`tests/LoomEditor.View.Tests/Comments/CommentThreadsControllerTests.cs`, `CommentThreadViewModelTests.cs`, `tests/LoomEditor.View.Tests/Features/CommentThreads/CommentThreadsFeatureTests.cs`,
`src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` (`InitializeCommentThreads`: the two lambdas this card adapts).

## Files to create or change

- `src/LoomEditor.View/Comments/CommentThreadsController.cs` — change
- `src/LoomEditor.View/Comments/CommentThreadViewModel.cs` — change: `Title` and XML docs only
- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` — change: the two lambdas of `InitializeCommentThreads` only (compile fix)
- `tests/LoomEditor.View.Tests/Comments/CommentThreadsControllerTests.cs`, `CommentThreadViewModelTests.cs`, `tests/LoomEditor.View.Tests/Features/CommentThreads/CommentThreadsFeatureTests.cs` — change

## Design decisions (already made — do not change)

- **Add.** New `CommentThread Add(int startLineNumber, int endLineNumber, string? title, bool startComposing)`; the existing `Add(int lineNumber, string? title, bool startComposing)`
  becomes `=> Add(lineNumber, lineNumber, title, startComposing)`. It calls `Manager.Add(start, end, title)`. The view model's delegate returns
  `SuggestionEdit.GetRangeText(model, s, e)` with `s = Clamp(thread.StartLineNumber, 1, LineCount)`, `e = Clamp(thread.LineNumber, s, LineCount)`. The `startComposing`
  announcement is `CommentStrings.ThreadAdded(start, end)` (equals the phase 11 string for one line).
- `GetLineText(CommentThread)` is renamed **`GetRangeText`** (same clamping); update its callers (grep; today only the controller's own code and tests).
- **View model.** `Title => Thread.Title ?? CommentStrings.ThreadTitle(Thread.StartLineNumber, Thread.LineNumber)`. `SuggestionSeed`'s XML doc: "The current text of the
  thread's lines (joined with <c>\n</c>)". No other change (the constructor keeps `Func<string>`).
- **Requests.** `event EventHandler<(int StartLineNumber, int LineNumber)>? AddCommentRequested` (was `EventHandler<int>`). New `void RequestAdd(int startLineNumber, int lineNumber)`
  (`Guard.Assert(start <= line)`); the existing `RequestAdd(int lineNumber)` becomes `=> RequestAdd(lineNumber, lineNumber)` so the gutter controller and the feature compile unchanged.
- **Apply.** `ApplySuggestion(CommentThread thread, string commentId, int suggestionIndex, string? renderedRangeText)` (parameter renamed; same position and type):
  start/end = the thread's lines (clamped as above); `SuggestionEdit.IsStale(model, start, end, renderedRangeText)`, `IsNoOp(model, start, end, block)`,
  `Create(model, start, end, block)`; caret `(Min(start, LineCount), 1)` after the edit; still `PushStackElement` before and after (one undo step).
- `event EventHandler<(CommentThread Thread, Comment Comment, int StartLineNumber, int LineNumber)>? SuggestionApplied` — the lines **before** the edit.
- **WinUI compile fix** (`InitializeCommentThreads`): `(_, e) => AddCommentRequested?.Invoke(this, new AddCommentRequestedEventArgs(e.LineNumber))` and
  `new SuggestionAppliedEventArgs(e.Thread, e.Comment, e.LineNumber)`. Nothing else in WinUI (P12-09 adds the start line).
- `samples/LoomEditor.Gallery/CommentsSelfCheck.cs` calls `ApplySuggestion(…, "two")` positionally: it keeps compiling; do not touch it (P12-14).

## Tests

- [x] `AddRange_ViewModelSeedIsRangeText`, `AddRange_StartComposing_AnnouncesRange`, `AddRange_TitleSaysLines`
- [x] `ApplySuggestion_Range_ReplacesAllLinesInOneUndoStep` (apply, then `Undo` once restores the full text), `ApplySuggestion_Range_CaretAtStart`
- [x] `ApplySuggestion_Range_StaleWhenALineInsideChanged`, `ApplySuggestion_Range_NoOp`, `ApplySuggestion_Range_Deletion_RemovesLines`
- [x] `ApplySuggestion_Range_RaisesWithStartAndEndBeforeEdit`
- [x] `RequestAddRange_RaisesBothLines`; the existing `RequestAdd_RaisesWithLine` asserts `(2, 2)`
- [x] View model: `Title_Range_SaysLines`, `Title_SingleLine_Unchanged`
- [x] Feature tests: adapt the two handlers to the tuple (`l.LineNumber`); behavior unchanged
- [x] All other existing tests green

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] No public API change (`PublicAPI.Unshipped.txt` files untouched)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The command with a selection (P12-08), the public API (P12-09, P12-10), the widget (P12-11).

## Suggested commit message

`feat(view): let the comment threads controller work on ranges of lines`
