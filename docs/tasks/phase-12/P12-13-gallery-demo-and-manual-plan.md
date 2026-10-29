# P12-13 — Gallery: the range demo and the complete manual test plan

- **Status:** done
- **Depends on:** P12-12
- **Size:** S

**Recommended model:** Sonnet

**Note (2026-10-29, step E):** P12-23 replaces the Gallery's `CommentLineRange` calls and the `Lines` log helper, and adds the exact-text demo, buttons and manual sections.

## Goal

The Gallery shows a multi-line thread with a multi-line suggestion in the "Comment threads" sample and in the diff window, answers add requests with the requested range,
and has the buttons every phase 12 manual check names. `docs/testing/phase-12-manual-tests.md` is complete.

## Sources

Sources: none (specified in this card). Read first: `samples/LoomEditor.Gallery/CommentSamples.cs` (`AddThreads`, `Text`, `MaxLine`), `MainWindow.Comments.cs`,
`MainWindow.xaml` (the "Comment threads" `Expander`, lines ~142–152), `DiffDemoWindow.Comments.cs`, `DiffDemoWindow.xaml` (its comment buttons),
`docs/testing/phase-12-manual-tests.md` (sections P12-09 … P12-12: the controls they name), `docs/testing/phase-11-manual-tests.md` (end-to-end sections to imitate),
`docs/tasks/phase-11/P11-23-gallery-demo-and-manual-plan.md`.

## Files to create or change

- `samples/LoomEditor.Gallery/CommentSamples.cs` — change
- `samples/LoomEditor.Gallery/MainWindow.Comments.cs`, `MainWindow.xaml` — change
- `samples/LoomEditor.Gallery/DiffDemoWindow.Comments.cs`, `DiffDemoWindow.xaml` — change
- `docs/testing/phase-12-manual-tests.md` — change: complete it

## Design decisions (already made — do not change)

- `CommentSamples.AddThreads` takes a second delegate `Func<int, int, string?, CommentThread> addRangeThread` and adds **one range thread** on a 4-line block of the sample
  text (pick 4 consecutive lines of `Text` that form a small statement block; add `public const int RangeStart`, `RangeEnd` next to `MaxLine`), titled `null` (so the title
  reads "Comment on lines …"), with one comment by Grace Hopper holding a ```` ```suggestion ```` block that rewrites **one** of the 4 lines and keeps the other 3, so the
  diff shows context rows. Existing threads unchanged. Both callers (main window, diff window) pass `editor.AddCommentThread` overloads (diff: `DiffSide.Original`).
- Main window handlers: `AddCommentRequested` logs `AddCommentRequested, lines {s}–{e}` (or `line {n}` when equal) and calls `AddCommentThread(e.StartLineNumber, e.LineNumber, startComposing: true)`;
  `SuggestionApplied` logs the same way; `CommentSubmitted` / `CommentCancelled` log `lines …` for a range thread.
- Main window buttons in the "Comment threads" expander (one line each in XAML + one handler each): **Comment on selection** (runs `CommentThreadsCommands.Add`, like the
  `Ctrl+Alt+M` accelerator), **Add range 3–6** (`AddCommentThread(3, 6, startComposing: true)`), **Add range 6–3 (error)** (calls `AddCommentThread(6, 3)` inside
  `try` / `catch (ArgumentOutOfRangeException ex)` and logs `ArgumentOutOfRangeException: {ex.ParamName}`). The existing **Add comment at caret** stays.
- Diff window: the `AddCommentRequested` handler uses `e.StartLineNumber`, `e.LineNumber`; **Add on original** / **Add on modified** use the selection's lines of that side
  (`CommentLineRange.FromSelection(editor.Selections[0])`: the Gallery sees View internals through `InternalsVisibleTo`).
- The manual plan: fill the **Machine** / **Gallery setup** paragraphs with the real control names; check every section P12-09 … P12-12 names an existing control; add
  `## End-to-end A — Range review (editor)` (select 4 lines → **Comment on selection** → write a suggestion with the toolbar → Comment → Apply → `Ctrl+Z`), `## End-to-end B —
  Range review (diff)`, `## C — Robustness` (type inside / above / below a range; delete the whole range; `SetValue` with fewer lines; resolve / reopen the highlighted thread;
  word wrap on: the highlight covers every row of the range), `## D — Keyboard only`, `## Findings` (empty table). Add the **self-check** paragraph pointing to P12-14.

## Tests

- [x] `tests/LoomEditor.View.Tests/Comments/CommentsSelfCheckTests.cs` and every other test still green (the Gallery compiles; the samples have no unit tests).
- [x] Manual: the plan is complete (not run by the agent).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Every control named in `docs/testing/phase-12-manual-tests.md` exists in the Gallery XAML (grep each name)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Running the plan, screenshots, a new Gallery page.

## Suggested commit message

`feat(gallery): demo multi-line comment threads and complete the phase 12 manual plan`

## Result

Build and tests green. The manual plan is complete but was not run (agents do not run the Gallery UI).
