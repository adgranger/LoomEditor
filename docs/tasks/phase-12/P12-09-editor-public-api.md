# P12-09 — Public API of `LoomEditor`: a thread on a range of lines

- **Status:** todo
- **Depends on:** P12-08
- **Size:** S

**Recommended model:** Sonnet

## Goal

A host calls `editor.AddCommentThread(12, 15)` to open a thread on lines 12 to 15, and receives the first line in `AddCommentRequested` and `SuggestionApplied`. Every phase 11
call still compiles and behaves the same. The phase 12 manual test file exists.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 2, 4 and 5). Read first: `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs`
(`AddCommentThread`, `InitializeCommentThreads`), `src/LoomEditor.WinUI/Controls/Comments/CommentEventArgs.cs`, `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt`,
`eng/Update-PublicApi.ps1`, `docs/testing/phase-11-manual-tests.md` (header to copy), `docs/tasks/phase-11/P11-11-control-public-api.md`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` — change
- `src/LoomEditor.WinUI/Controls/Comments/CommentEventArgs.cs` — change
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `docs/testing/phase-12-manual-tests.md` — create (header + section "P12-09")

## Design decisions (already made — do not change)

- New method, XML doc on each parameter, the `<exception>` of README decision 5:
  ```csharp
  public CommentThread AddCommentThread(int startLineNumber, int endLineNumber, string? title = null, bool startComposing = false)
  ```
  It validates (decision 5), calls `_commentThreads.Add(start, end, title, startComposing)` and, with `startComposing`, `_commentZoneHost?.FocusComposer(thread)` — same as the
  single-line method. The single-line `AddCommentThread(int lineNumber, …)` keeps its signature and body; its XML summary adds "Use the overload with two lines for a range."
- RS0026 / RS0027: README decision 4 (suppress on the named members only, with the given justification; never change a signature).
- `AddCommentRequestedEventArgs`: new constructor `(int startLineNumber, int lineNumber)` (`ArgumentOutOfRangeException` when `startLineNumber > lineNumber`), new property
  `public int StartLineNumber { get; }` ("The first line of the requested range; equals <see cref="LineNumber"/> for one line."). The old constructor sets both to `lineNumber`.
  `LineNumber`'s doc: "The last line of the requested range (1-based)…".
- `SuggestionAppliedEventArgs`: new constructor `(CommentThread thread, Comment comment, int startLineNumber, int lineNumber)`, new `StartLineNumber`; the old constructor
  sets it to `lineNumber`; `LineNumber`'s doc: "The last line that was replaced (before the edit)".
- `InitializeCommentThreads` uses the new constructors with the tuple fields from P12-07 (`e.StartLineNumber`, `e.LineNumber`).
- `PublicAPI.Unshipped.txt` gains exactly: the method, the two constructors, the two `StartLineNumber.get`.
- `docs/testing/phase-12-manual-tests.md`: title "Phase 12 — manual test list", a short intro (what unit tests cannot check: the range highlight, the box under the last line,
  the selection gestures, the N-line suggestion block), **Machine** and **Gallery setup** paragraphs saying "the Gallery controls are added by P12-13", the blank build / date
  lines copied from phase 11, then `## P12-09 — Public API` with numbered checks, each with a measurable expectation and a "Result:" blank:
  (1) `AddCommentThread(3, 6)` (Gallery button, P12-13): the box is under line 6, its title reads "Comment on lines 3 to 6"; (2) `AddCommentThread(4)` still opens "Comment on
  line 4"; (3) `AddCommentThread(6, 3)` throws (the Gallery log shows `ArgumentOutOfRangeException`); (4) the event log shows `AddCommentRequested, lines 3–6` after the command
  with a 4-line selection; (5) after applying a range suggestion the log shows `SuggestionApplied, lines 3–6`.

## Tests

- [ ] None automated (no WinUI test project). The View tests of P12-07 cover the logic; the build checks the API file.
- [ ] Manual: section "P12-09" written (not run by the agent).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `PublicAPI.Unshipped.txt` (WinUI) lists the five new members; no phase 11 line changed
- [ ] The manual section is written; the card's Result section says it was not run
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`LoomDiffEditor` (P12-10), the widget (P12-11), the gutter (P12-12), Gallery buttons (P12-13).

## Suggested commit message

`feat(winui): add comment threads on a range of lines to the editor API`
