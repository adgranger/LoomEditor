# P12-20 — Controller and add command: threads and requests on the exact selected text

- **Status:** todo
- **Depends on:** P12-19
- **Size:** M

**Recommended model:** Sonnet

## Goal

`loom.commentThreads.add` with a word selected asks for a thread on **that word**; with whole lines (or nothing) selected it asks for lines, as before. The controller can add
an exact-text thread, announces it, gives the view model the commented text, and its add request carries the exact range. Suggestions keep working on the covering lines.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 15, 19, 20 and 22). Read first: `src/LoomEditor.View/Comments/CommentThreadsController.cs`
(all: `Add`, `GetRangeText`, `GetLines`, `RequestAdd`, `AddCommentRequested`, `ApplySuggestion`), `CommentThreadViewModel.cs` (constructor, `SuggestionSeed`),
`CommentRange.cs` and `CommentStrings.cs` (P12-17), `src/LoomEditor.View/Features/CommentThreads/CommentThreadsFeature.cs`, `CommentThreadsCommands.cs`,
`src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` (`InitializeCommentThreads`: reads `e.StartLineNumber`, `e.LineNumber` of the request),
`src/LoomEditor.WinUI/Controls/Comments/CommentGutterController.cs` (calls `RequestAdd(int, int)`), `tests/LoomEditor.View.Tests/Comments/CommentThreadsControllerTests.cs`,
`CommentThreadViewModelTests.cs`, `tests/LoomEditor.View.Tests/Features/CommentThreads/CommentThreadsFeatureTests.cs`.

## Files to create or change

- `src/LoomEditor.View/Comments/CommentThreadsController.cs` — change
- `src/LoomEditor.View/Comments/CommentThreadViewModel.cs` — change
- `src/LoomEditor.View/Features/CommentThreads/CommentThreadsFeature.cs` — change
- `src/LoomEditor.View/Features/CommentThreads/CommentThreadsCommands.cs` — change: XML doc of `Add` only
- `tests/LoomEditor.View.Tests/Comments/CommentThreadsControllerTests.cs`, `CommentThreadViewModelTests.cs`, `tests/LoomEditor.View.Tests/Features/CommentThreads/CommentThreadsFeatureTests.cs` — change

## Design decisions (already made — do not change)

- **Add.** New `CommentThread Add(CommentRange range, string? title, bool startComposing)` holds today's body of `Add(int, int, …)`, with two differences:
  `CommentThread thread = range.TextRange is { } text ? Manager.Add(text, title) : Manager.Add(range.StartLineNumber, range.LineNumber, title);` and the `startComposing`
  announcement is `range.IsWholeLines ? CommentStrings.ThreadAdded(start, line) : CommentStrings.ThreadAddedOnText(start, line)`. `Add(int, int, …)` becomes
  `=> Add(CommentRange.Lines(startLineNumber, endLineNumber), title, startComposing)`; `Add(int, …)` is unchanged (it calls the two-line overload).
- **Commented text.** New `public string GetCommentedText(CommentThread thread)`: `""` when `thread.IsWholeLines`; else
  `_context.Model.GetValueInRange(_context.Model.ValidateRange(new Range(thread.StartLineNumber, thread.StartColumn, thread.LineNumber, thread.EndColumn)), EndOfLinePreference.LF)`.
- **View model.** The constructor gains an optional last parameter `Func<string>? getCommentedText = null` (existing calls compile unchanged). New
  `public string? CommentedText => Thread.IsWholeLines || _getCommentedText is null ? null : _getCommentedText();` (XML doc: "The current text of an exact-text thread
  (lines joined with <c>\n</c>); <see langword=\"null\"/> for a thread on whole lines."). The controller passes `() => GetCommentedText(thread)`. `Title` and
  `SuggestionSeed` are **unchanged** (README decisions 19 and 20).
- **Requests.** `AddCommentRequested` becomes `event EventHandler<CommentRange>?` (was a `(StartLineNumber, LineNumber)` tuple; `CommentRange` has the same two property
  names, so `InitializeCommentThreads` in WinUI compiles **unchanged** — do not edit it; P12-21 passes the exact range on). New `void RequestAdd(CommentRange range)`; the
  existing `RequestAdd(int)` and `RequestAdd(int, int)` become `=> RequestAdd(CommentRange.Lines(…))` (the gutter controller keeps compiling; P12-22 changes it).
- **Command.** `CommentThreadsFeature`: `context.GetCommentThreadsController()?.RequestAdd(CommentRange.FromSelection(context.Model, context.Selection));`. `CommentLineRange`
  is no longer used by the feature (it is deleted by P12-23). `CommentThreadsCommands.Add` XML doc: "Asks the host for a thread on the selected text, on the selected whole
  lines, or on the cursor line (see CommentRange)."
- **Apply.** `ApplySuggestion` is **not** changed: it already works on `StartLineNumber` … `LineNumber` (README decision 19). The tests below pin it for an exact thread.

## Tests

- [ ] `AddText_ManagerHasExactDecoration` (`Add(CommentRange.FromRange(model, new Range(1, 5, 1, 10)), null, false)` on `"let value = 1;"`: thread not whole lines,
  columns 5–10)
- [ ] `AddText_StartComposing_AnnouncesSelectedText` (`"Comment thread added on the selected text on line 1"`), `AddText_TitleSaysLine` (`"Comment on line 1"`)
- [ ] `GetCommentedText_Text_ReturnsTheText` (`"value"`), `GetCommentedText_MultiLine_JoinsWithLf` (CRLF model), `GetCommentedText_WholeLines_Empty`
- [ ] View model: `CommentedText_Text_FollowsEdits` (type inside the word: the property returns the new text), `CommentedText_WholeLines_Null`, `CommentedText_NoDelegate_Null`
- [ ] `ApplySuggestion_TextThread_ReplacesTheCoveringLine` (thread on `value`, suggestion `"let total = 2;"`, rendered text `"let value = 1;"` → `Applied`, the line is
  replaced, one `Undo` restores it), `ApplySuggestion_TextThread_StaleWhenTheLineChangedOutsideTheText`
- [ ] `RequestAdd_Text_RaisesTheExactRange`; the existing request tests adapt to `CommentRange` (`l.LineNumber`, `(l.StartLineNumber, l.LineNumber)`) and assert
  `l.TextRange` is `null`
- [ ] Feature: `AddCommand_WordSelection_RequestsTextRange`, `AddCommand_ShiftDownLines_RequestsWholeLines`; the existing feature tests stay green
- [ ] All other existing tests green

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] No public API change (`PublicAPI.Unshipped.txt` files untouched); no file under `src/LoomEditor.WinUI` changed
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The public API (P12-21), the widget and the gutter button (P12-22), a suggestion that replaces only the selected text (README decision 19), deleting `CommentLineRange` (P12-23).

## Suggested commit message

`feat(view): request and add comment threads on the exact selected text`
