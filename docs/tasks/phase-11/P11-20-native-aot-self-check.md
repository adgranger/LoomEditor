# P11-20 — Native AOT self-check of the phase 11 logic

- **Status:** todo
- **Depends on:** P11-19
- **Size:** M

## Goal

The published (Native AOT) Gallery proves, without a human, that the phase 11 logic works in the AOT build: `--comments-selfcheck=<file>` runs the Markdown extensions, the toolbar edits, suggestion extraction, edit and diff, the thread model and view model, the thread manager over a real model, and relative time; it writes a pass / fail report and exits 0 or 1.

## Sources

Sources: none (specified in this card; the pattern is `TextSelfCheck` of P10-30: **read `samples/LoomEditor.Gallery/TextSelfCheck.cs`, `MainWindow.TextCheck.cs` and `tests/LoomEditor.View.Tests/Features/Snippet/TextSelfCheckTests.cs` first and copy their structure**, and the **Result section** of `docs/tasks/phase-10/P10-30-native-aot-self-check.md` for the exact publish command).

## Files to create or change

- `samples/LoomEditor.Gallery/CommentsSelfCheck.cs` — create (`internal static class`; no WinUI types)
- `samples/LoomEditor.Gallery/MainWindow.CommentsCheck.cs` — create: the `--comments-selfcheck=<file>` argument handled like `--text-selfcheck=`; call its initializer where the other self-check initializers are called (**guard with the same null checks**)
- `tests/LoomEditor.View.Tests/Comments/CommentsSelfCheckTests.cs` — create (links `CommentsSelfCheck.cs` and runs the checks in the normal test run, one test per check name)
- `docs/testing/phase-11-manual-tests.md` — change: the line telling how to run the self-check (added by P11-19: make sure it is exact)
- `src/LoomEditor.Core/LoomEditor.Core.csproj` — change only if `InternalsVisibleTo LoomEditor.Gallery` is missing for a Core internal the check needs (one line)

## Design decisions (already made — do not change)

- Checks, each `PASS name — detail` or `FAIL name — reason` (an exception is a `FAIL`, never a crash): `thread-model` (add / replace / remove comments, events raised, duplicate id rejected), `markdown-extended` (`> q`, `~~s~~`, `<u>u</u>` parse with the right shape under `Extended` and as plain paragraphs without it), `toolbar-inline` (bold on a selection; unwrap; link with URL selection), `toolbar-block` (numbered list on three lines; code block; suggestion with seed), `suggestion-extract` (a body with two suggestion fences and one C# fence gives two blocks, an empty block is a deletion), `suggestion-edit` (apply on a CRLF `TextModel`: single line, multi-line, deletion of the last line; the final text is the expected one), `suggestion-diff` (rows and `Describe()` for replacement, deletion, no-op), `view-model` (draft, blank cannot submit, submit keeps the draft, comment added clears it, Escape rules), `relative-time` (minutes, hours, yesterday, ISO date), `thread-manager` (over a real `TextModel` and `ViewZones` **if** the Gallery can build them without a WinUI control — look at how the View tests do; add a line to the card result saying which way): a thread follows an inserted line and its zone height changes with `SetHeight`; if building `ViewZones` needs more than a few lines, the check uses the model decorations alone and the result section says the zone part is covered by unit tests only.
- The report format and the exit code are exactly those of `TextSelfCheck`: first line `LoomEditor comments self-check`, one line per check, last line `n of m checks passed`.
- No `Regex`, no reflection in the check itself; it exercises the same code paths the control uses.
- Run it once in the card, in the background, with the published exe, **and kill the process** afterwards (`Stop-Process -Name LoomEditor.Gallery -Force`); the card result states the real outcome and the date. If the AOT publish fails for a reason outside this card, report it, do not hack around it. Also run `--text-selfcheck` and `--diff-selfcheck` once on the same exe (they must still pass: the start-up path was touched).

## Tests

- [ ] New: `CommentsSelfCheckTests` runs every check and asserts all pass; one test per check name so a failure names the check; a test for the report format
- [ ] Manual: none (the self-check is the check)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` (the exact command line of P10-30's Result section) succeeds, and the published exe run with `--comments-selfcheck=<file>` ends with `n of n checks passed` and exit code 0 (stated in the card's Result section, with the date)
- [ ] No Gallery process left running
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

UI automation of the widget, benchmarks, running the manual plan.

## Suggested commit message

`test(gallery): add the Native AOT self-check of the phase 11 logic`
