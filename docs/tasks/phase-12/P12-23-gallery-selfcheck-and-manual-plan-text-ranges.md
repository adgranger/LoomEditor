# P12-23 — Gallery, Native AOT self-check and manual plan for exact text ranges

- **Status:** todo
- **Depends on:** P12-22
- **Size:** M

**Recommended model:** Sonnet

## Goal

The Gallery shows an exact-text thread in the "Comment threads" sample, answers add requests with the exact range, and has every control the P12-21 / P12-22 manual
sections name. `--comments-selfcheck` checks the step E logic in the published Native AOT exe. The manual plan covers exact text end to end. `CommentLineRange` is deleted.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) section 3). Read first: `samples/LoomEditor.Gallery/CommentSamples.cs` (`AddThreads`, `Text`,
`RangeStart`, `RangeEnd`), `MainWindow.Comments.cs` (handlers, `Lines`, the buttons' handlers), `MainWindow.xaml` (the "Comment threads" `Expander`, lines ~142–155),
`DiffDemoWindow.Comments.cs` (`AddOnSelection`, `Lines`, `OnLoadCommentSample`), `CommentsSelfCheck.cs` (all: `Run`, `Check`, `Require`, `RangeManager`, `RangeController`),
`tests/LoomEditor.View.Tests/Comments/CommentsSelfCheckTests.cs`, `docs/testing/phase-12-manual-tests.md` (all), `docs/tasks/phase-12/P12-13-gallery-demo-and-manual-plan.md` and
`P12-14-native-aot-self-check.md` (the publish / run commands and how the Result section reports them).

## Files to create or change

- `samples/LoomEditor.Gallery/CommentSamples.cs` — change
- `samples/LoomEditor.Gallery/MainWindow.Comments.cs`, `MainWindow.xaml` — change
- `samples/LoomEditor.Gallery/DiffDemoWindow.Comments.cs` — change
- `samples/LoomEditor.Gallery/CommentsSelfCheck.cs` — change
- `src/LoomEditor.View/Comments/CommentLineRange.cs`, `tests/LoomEditor.View.Tests/Comments/CommentLineRangeTests.cs` — **delete** (`CommentRange` and its tests replace them)
- `docs/testing/phase-12-manual-tests.md` — change

## Design decisions (already made — do not change)

- **Sample.** `CommentSamples.AddThreads` takes a third delegate `Func<Range, string?, CommentThread> addTextThread` and adds **one exact-text thread** on one identifier
  of a line of `Text` that no other sample thread covers (add `public const int TextLine` and `public const string TextWord` next to `RangeStart`; the columns are computed
  from the line content: `start = line.IndexOf(TextWord, StringComparison.Ordinal) + 1`, `end = start + TextWord.Length`), title `null`, with one comment by Grace Hopper
  holding a ```` ```suggestion ```` block that rewrites that **whole line** with a better name for the word (README decision 19: the suggestion replaces the line). Both
  callers pass the new overloads (`(r, t) => Editor.AddCommentThread(r, t)`; diff window: the original side, like the range thread).
- **Logs.** Replace `Lines(start, end)` in both windows by `Where(int start, int end, Range? text)`: `Lines(start, end)` followed, when `text` is not null, by
  `$", text {text.StartLineNumber}:{text.StartColumn}–{text.EndLineNumber}:{text.EndColumn}"`. For a thread, `text` is `thread.IsWholeLines ? null : new Range(thread.StartLineNumber,
  thread.StartColumn, thread.LineNumber, thread.EndColumn)` (one private helper `TextOf(CommentThread)`). Requests log `e.TextRange`.
- **Main window.** `AddCommentRequested`: `e.TextRange is { } text ? Editor.AddCommentThread(text, startComposing: true) : Editor.AddCommentThread(e.StartLineNumber,
  e.LineNumber, startComposing: true)`. Two new buttons in the "Comment threads" expander, after **Add range 6–3 (error)** (one XAML line + one handler each):
  **Add on selection (Range API)** → `Editor.AddCommentThread(Editor.Selections[0].Range, startComposing: true)`; **Add text out of document (error)** →
  `Editor.AddCommentThread(new Range(n + 1, 1, n + 1, 2))` with `n = Editor.Model.LineCount`, inside `try` / `catch (ArgumentOutOfRangeException ex)` logging
  `ArgumentOutOfRangeException: {ex.ParamName}` (as **Add range 6–3 (error)** does).
- **Diff window.** The `AddCommentRequested` handler chooses the overload the same way (with `e.Side`); `AddOnSelection` becomes
  `DiffEditor.AddCommentThread(side, editor.Selections[0].Range, startComposing: true)` (the `Range` overload applies README decision 15). No `CommentLineRange` left.
- **Self-check.** Two new `Check(...)` lines after the existing fifteen, each a private static method returning a one-line success text, failing through `Require`:
  1. `"text-range"`: on `"let value = 1;\nsecond\nthird"`: `CommentRange.FromSelection` of `value` is exact `(1, 5, 1, 10)`; of `(2, 1) → (3, 1)` is whole line 2;
     `Manager.Add(new Range(1, 5, 1, 10))`, insert `"x"` at `(1, 1)` → columns 6–11 and no `ThreadMoved`; the decoration's class is `comment-thread-text`; delete the word
     → `StartColumn == EndColumn` and the class is gone.
  2. `"text-controller"`: `controller.Add(CommentRange.FromRange(model, new Range(1, 5, 1, 10)), null, false)`; `GetCommentedText` is `"value"`; a suggestion
     `"let total = 1;"` applied with rendered text `"let value = 1;"` → `Applied`, line 1 is `"let total = 1;"`; one `Undo` restores it.
  `RangeController`'s last check uses `CommentRange.FromSelection(context.Model, new Selection(2, 1, 4, 1))` (lines 2–3, `IsWholeLines`) instead of `CommentLineRange`.
  Same AOT rules as P12-14 (no reflection, no `IEnumerable<T>` collection expressions).
- **Publish and run** exactly as P12-14: `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror`, then the published exe with
  `--comments-selfcheck=<report file>` (exit code 0, last line `17 of 17 checks passed`), `--text-selfcheck` and `--diff-selfcheck` (unchanged results). Copy the last
  lines into the card's Result section, verbatim.
- **Manual plan.** Update **Gallery setup** with the two new buttons and the sample's exact thread; the self-check paragraph says `17 of 17`. Check that every control named
  by sections P12-21 and P12-22 exists. Add `## End-to-end E — Word review (editor)` (select a word → `Ctrl+Alt+M` → only the word is tinted, the box is under its line → write
  a comment → **Suggest a change** seeds the **whole line** → Comment → Apply replaces the line → one `Ctrl+Z`), `## End-to-end F — Word review (diff)` (same on the modified
  side; the sides stay aligned) and, in `## C — Robustness`, numbered checks for: typing just before / just after / inside the word; deleting the word (tint gone, box
  stays); `Enter` inside the word (tint on two partial lines, the box moves down one line); a selection from mid-line to column 1 of a later line (the tint ends at the end of
  the previous line); `SetValue` with shorter lines; resolve / reopen (tint off / on); word wrap on (the tint covers only the word's fragment on its visual row); high
  contrast (the border is visible). Each check has a measurable expectation and a blank "Result:".

## Tests

- [ ] `CommentsSelfCheckTests` green with the two new checks (it runs `Run()` in-process); every other test green after deleting `CommentLineRangeTests`
- [ ] Native AOT publish succeeds with `-warnaserror`; the exe prints `17 of 17 checks passed` (result copied in the card)
- [ ] Manual: the plan is complete (not run by the agent)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` — succeeds; `--comments-selfcheck` exits 0 with `17 of 17 checks passed`
- [ ] `grep -rn CommentLineRange src samples tests` finds nothing
- [ ] Every control named in `docs/testing/phase-12-manual-tests.md` exists in the Gallery XAML (grep each name)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Running the manual plan, screenshots, a new Gallery page, closing the phase (P12-15).

## Suggested commit message

`feat(gallery): demo exact-text comment threads and extend the phase 12 checks`
