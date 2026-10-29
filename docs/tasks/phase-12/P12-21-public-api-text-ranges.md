# P12-21 — Public API: threads on the exact text in `LoomEditor` and `LoomDiffEditor`

- **Status:** done
- **Depends on:** P12-20
- **Size:** S

**Recommended model:** Sonnet

## Goal

A host calls `editor.AddCommentThread(new Range(4, 5, 4, 10))` (or the diff editor's `(DiffSide, Range)` overload) to open a thread on exact text, and receives the exact
range in `AddCommentRequested` (`e.TextRange`, `null` for whole lines). Every earlier call compiles and behaves the same.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 4, 5, 15 and 21). Read first: `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs`
(both `AddCommentThread` overloads and their RS0026 suppression, `InitializeCommentThreads`), `src/LoomEditor.WinUI/Controls/Comments/CommentEventArgs.cs`
(`AddCommentRequestedEventArgs`), `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.CommentThreads.cs` (`AddCommentThread`, `Reveal`, `OnSideAddCommentRequested`),
`DiffAddCommentRequestedEventArgs.cs`, `src/LoomEditor.View/Comments/CommentRange.cs` (P12-17), `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt`, `eng/Update-PublicApi.ps1`,
`docs/tasks/phase-12/P12-09-editor-public-api.md` and `P12-10-diff-editor-public-api.md`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.CommentThreads.cs` — change
- `src/LoomEditor.WinUI/Controls/Comments/CommentEventArgs.cs` — change: `AddCommentRequestedEventArgs` only
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.CommentThreads.cs` — change
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffAddCommentRequestedEventArgs.cs` — change
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `docs/testing/phase-12-manual-tests.md` — change: section "P12-21", inserted after "P12-12"

## Design decisions (already made — do not change)

- `LoomEditor`, new method, XML doc on each parameter (the `<summary>` states README decision 15 in one sentence: "a range over whole lines, or an empty range, gives a
  thread on whole lines"), RS0026 suppression as the two existing overloads:
  ```csharp
  public CommentThread AddCommentThread(Range range, string? title = null, bool startComposing = false)
  ```
  `ArgumentNullException.ThrowIfNull(range)`; the lines as README decision 5 (`ThrowIfLessThan(range.StartLineNumber, 1)`, `ThrowIfGreaterThan(range.StartLineNumber,
  Model.LineCount)`, `ThrowIfGreaterThan(range.EndLineNumber, Model.LineCount)`; the `Range` constructor already orders start and end); then
  `_commentThreads.Add(CommentRange.FromRange(Model, range), title, startComposing)` and, with `startComposing`, `_commentZoneHost?.FocusComposer(thread)`.
- `LoomDiffEditor`, new method with the same shape: `AddCommentThread(DiffSide side, Range range, string? title = null, bool startComposing = false)`: null check, the lines
  validated against that side's `Model.LineCount`, the reveal of `range.StartLineNumber` then (when different) `range.EndLineNumber` exactly as the line overload does,
  then `EditorOf(side).AddCommentThread(range, title, startComposing)`. To avoid duplicating the reveal, move it into a private `RevealForComposing(DiffSide side, int start,
  int end)` used by both overloads.
- `AddCommentRequestedEventArgs`: new constructor `(Range textRange)` (`ArgumentNullException.ThrowIfNull`; `StartLineNumber = textRange.StartLineNumber`, `LineNumber =
  textRange.EndLineNumber`), new `public Range? TextRange { get; }` — "The exact text the user asked to comment on; <see langword=\"null\"/> when the request is for whole
  lines (<see cref=\"StartLineNumber\"/> to <see cref=\"LineNumber\"/>). Pass it to <c>AddCommentThread(Range, …)</c>." The two old constructors leave it `null`.
- `DiffAddCommentRequestedEventArgs`: new constructor `(DiffSide side, Range textRange)` and `TextRange`, same docs.
- `InitializeCommentThreads`: `(_, e) => AddCommentRequested?.Invoke(this, e.TextRange is { } text ? new AddCommentRequestedEventArgs(text) : new AddCommentRequestedEventArgs(e.StartLineNumber, e.LineNumber))`.
  `OnSideAddCommentRequested`: the same choice with the side.
- `PublicAPI.Unshipped.txt` gains exactly six lines: the two methods, the two constructors, the two `TextRange.get`. No existing line changes.
- No change to `SuggestionAppliedEventArgs` (suggestions work on lines, README decision 19).
- Manual section `## P12-21 — Exact text in the public API` (controls added by P12-23; each check measurable, with a blank "Result:"):
  (1) select `value` on a line, **Comment on selection**: the log shows `AddCommentRequested, line N, text N:a–N:b` and the box opens under line N with only the word tinted;
  (2) **Add on selection (Range API)** with a full line selected by mouse drag: a whole-line thread (no tint on a single line, log shows no `text`);
  (3) **Add text out of document (error)**: the log shows `ArgumentOutOfRangeException`; (4) in the diff window, select a word on the modified side, **Add on modified**: only
  that word is tinted, the original side is padded by the box height; (5) **Add range 3–6** still gives a whole-line thread with all 4 lines tinted.

## Tests

- [x] None automated (no WinUI test project). P12-17 and P12-20 cover the logic; the build checks the API file.
- [x] Manual: section "P12-21" written (not run by the agent).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` (WinUI) gains exactly the six members; no earlier line changed
- [x] The manual section is written; the card's Result section says it was not run
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The widget and the gutter button (P12-22), Gallery buttons (P12-23), a `Range` on `SuggestionAppliedEventArgs`.

## Suggested commit message

`feat(winui): add comment threads on the exact text to the editor and diff editor APIs`

## Result

- Manual section "P12-21" written in `docs/testing/phase-12-manual-tests.md`; **not run** (no WinUI test project, the Gallery buttons come with P12-23).
- Deviation: no `ArgumentNullException.ThrowIfNull(range)` (`Range` is a struct). The WinUI project has no global `Range` alias, so each touched file has `using Range = LoomEditor.Core.Text.Range;`.
