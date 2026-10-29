# P11-23 — Gallery: the comment threads demo and the manual test plan

- **Status:** done
- **Depends on:** P11-22
- **Size:** M

## Goal

The Gallery shows the feature the way the reference screenshot does, in the editor and in the diff editor, and `docs/testing/phase-11-manual-tests.md` is the complete plan the maintainer runs: one section per card with a manual check (written by the cards) plus end-to-end scenarios.

## Sources

Sources: none (specified in this card). Pattern: `samples/LoomEditor.Gallery/Phase10Samples.cs`, `FeatureSamples.cs`, `DiffSamples.cs`, `DiffDemoWindow.xaml(.cs)`, `MainWindow.xaml(.cs)` (the "Features" picker, the Expander of buttons), `docs/testing/phase-10-manual-tests.md` (format) and P10-29 (the card that did the same for phase 10).

## Files to create or change

- `samples/LoomEditor.Gallery/CommentSamples.cs` — create (sample text, the threads and the handlers, **without WinUI types** where possible)
- `samples/LoomEditor.Gallery/MainWindow.xaml`, a new `MainWindow.Comments.cs` partial — change: a "Comment threads" sample in the Features picker, an Expander section with the buttons below, and the switch for `CommentGutterButtonEnabled`
- `samples/LoomEditor.Gallery/DiffDemoWindow.xaml`, `DiffDemoWindow.xaml.cs` — change: the same buttons for both sides
- `samples/LoomEditor.Gallery/FeatureSamples.cs` — change: remove the temporary "Comment threads (P11-12)" sample and its buttons
- `docs/testing/phase-11-manual-tests.md` — change: complete it (header, machine, build kind, scale, tester; sections of the cards in card order; the new sections below; the Findings table)

## Design decisions (already made — do not change)

- **Sample** (`CommentSamples`): a ~60-line C# file (a small class with a method whose body has the line `int max = 10;`, a loop and a long line), plus three pre-built threads on separate lines: (1) one comment by "Ada Lovelace" two hours ago with Markdown (bold, inline code, a list); (2) a thread with a comment, a reply by "Grace Hopper" and a ` ```suggestion ` block that changes `int max = 10;` to `const int Max = 10;`; (3) a **resolved** thread with two comments. Authors, ids and times are built in code (`DateTimeOffset.UtcNow` minus offsets).
- **Buttons** (in the Expander, like the snippet buttons of phase 10): **Add comment at caret** (`AddCommentThread(caretLine, startComposing: true)`), **Add 20 threads** (stress: one every 3 lines), **Remove all threads**, **Resolve first thread**, **Log events** (a `TextBlock` log showing `CommentSubmitted`, `CommentCancelled`, `AddCommentRequested`, `SuggestionApplied` with the thread line and text), the switch **Gutter "+" button** (`CommentGutterButtonEnabled`).
- **Handlers** (the host's job, shown as the model of use): `CommentSubmitted` → `thread.AddComment(Comment.Create(new CommentAuthor("You"), args.Text))` (the widget's composer closes by itself); `AddCommentRequested` → `AddCommentThread(args.LineNumber, startComposing: true)`; `SuggestionApplied` → `thread.State = Resolved`; `CommentCancelled` → log only.
- **Diff window:** a diff of two versions of the same sample; buttons **Add on original** / **Add on modified** (at the caret line of that side), the gutter switch applied to **both** editors (`Original` and `Modified`), the same handlers wired to the diff editor's events with `GetSide` in the log.
- **Manual plan additions** (sections after the card sections):
  - **End-to-end A — Review flow (editor):** gutter **+** → composer → toolbar bold + suggest → Preview → Comment → the comment appears with the suggestion block → Apply → text changes, `Ctrl+Z` undoes it, thread resolved by the handler.
  - **End-to-end B — Review flow (diff, both sides, side by side and inline).**
  - **C — Robustness:** 100 threads scroll; type 200 lines into a composer; resize; zoom; word wrap on; dark / light / high contrast; delete lines above threads; replace the whole text; read-only editor; two rapid `Add comment at caret` clicks (two zones, no crash).
  - **D — Keyboard only:** add by command (the library has no default key: the Gallery adds a window-level `KeyboardAccelerator` `Ctrl+Alt+M` that calls `Editor.Commands.Execute("loom.commentThreads.add")` — Gallery code only), Tab through the widget, Escape rules, `Ctrl+Enter`.
  - **E — Screen reader:** the P11-20 checks once more end to end with Narrator.
  - **Self-check line:** how to run `--comments-selfcheck=<file>` (P11-24).
- The Gallery must start and the existing self-checks (`--text-selfcheck`, `--diff-selfcheck`, `--uia-selfcheck`) must still work: **guard every new handler with `Editor is not null`** (the start-up crash of P10-30 came from a handler that ran before `Editor` existed).
- The agent does **not** run the Gallery UI. It may run `dotnet build` of the Gallery only.

## Tests

- [x] Automated: none new (Gallery code); `dotnet build` of the solution proves it compiles.
- [x] Manual: the whole plan (not run by the agent).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Every card of this phase that has a manual section (P11-12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22) has a section in the plan, and each step is runnable with the Gallery as it is
- [x] The temporary sample of P11-12 (and the buttons added to it by P11-13 … P11-21) is gone; the diff window buttons of P11-22 are kept and finished
- [x] Card status and `docs/tasks/README.md` updated

## Result

- `CommentSamples.cs` (sample text, its modified version for the diff, the three threads through a `Func<int, string?, CommentThread>`; Core
  types only), `MainWindow.Comments.cs` rewritten (handlers, buttons, `Ctrl+Alt+M` accelerator, diagnostics line kept for P11-12 / P11-13),
  the "Comment threads" Expander and picker entry; `DiffDemoWindow.Comments.cs` finished (**Comment sample**, **Add on original** /
  **Add on modified**, the gutter box applied to both editors, the same handlers with the side in the log).
- Removed: the temporary "Comment threads (P11-12)" sample and every temporary button of P11-12 … P11-21. Loading any picker sample removes the
  current threads first; loading **Comment threads** adds its three.
- New handlers are guarded with `Editor is null` / `DiffEditor is null` checks.
- `docs/testing/phase-11-manual-tests.md` is rewritten as the complete plan: setup, the self-check line, every card section (P11-12 … P11-22,
  their steps moved to the final Gallery), end-to-end A and B, C robustness, D keyboard, E screen reader, Findings.
- The Gallery has no `--uia-selfcheck` command-line switch (only the **UIA self-check** button); the plan refers to the button.
- Not run by the agent: the Gallery UI and the manual plan (only the build and the tests).

## Out of scope

Running the manual plan, screenshots, a Gallery redesign, localizing the Gallery.

## Suggested commit message

`feat(gallery): add the comment threads demo and the phase 11 manual test plan`
