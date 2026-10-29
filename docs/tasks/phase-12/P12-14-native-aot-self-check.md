# P12-14 — Native AOT self-check of the phase 12 logic

- **Status:** todo
- **Depends on:** P12-13
- **Size:** S

**Recommended model:** Sonnet

## Goal

`--comments-selfcheck` also runs the range logic in the published Native AOT exe: four new checks, so the report ends with `15 of 15 checks passed`.

## Sources

Sources: none (specified in this card). Read first: `samples/LoomEditor.Gallery/CommentsSelfCheck.cs` (all: `Run`, `Check`, `Require`, the `ThreadManager` and
`Controller` checks to imitate), `tests/LoomEditor.View.Tests/Comments/CommentsSelfCheckTests.cs`, `docs/tasks/phase-11/P11-24-native-aot-self-check.md` (the publish and
run commands, and how its Result section reports them).

## Files to create or change

- `samples/LoomEditor.Gallery/CommentsSelfCheck.cs` — change (4 checks)
- `tests/LoomEditor.View.Tests/Comments/CommentsSelfCheckTests.cs` — change only if it pins the number 11 (it counts `Results.Count` today: expected no change)

## Design decisions (already made — do not change)

- Four new `Check(...)` lines **after** the existing eleven, each a private static method returning a one-line success text, failing through `Require`:
  1. `"range-suggestion-edit"`: model `"a\nb\nc\nd"`, `SuggestionEdit.Create(model, 2, 3, block "X")` applied with `ApplyEdits` gives `"a\nX\nd"`; `GetRangeText(model, 1, 2)` on a
     CRLF model is `"a\nb"`.
  2. `"range-suggestion-diff"`: old `["a", "b", "c"]`, block `"a\nX\nc"` → rows `Context a, Removed b, Added X, Context c`.
  3. `"range-manager"`: `CommentThreadManager.Add(2, 3)` on a 4-line model; insert a line above → thread `3–4`, its zone's `AfterLineNumber == 4`; the decoration's options have
     the class `comment-thread-range`; resolving the thread removes it.
  4. `"range-controller"`: `controller.Add(2, 3, null, false)`, a suggestion `"X"`, `ApplySuggestion(thread, id, 0, "two\nthree")` → `Applied`, text `"one\nX\nfour"`, one
     `Undo` restores it; `FromSelection` of a selection `(2, 1) → (4, 1)` is `(2, 3)`.
- No reflection, no `IEnumerable<T>` collection expressions (CsWinRT analyzer, P11-24 note), no new file.
- Publish and run, as P11-24: `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror`, then the published
  `LoomEditor.Gallery.exe --comments-selfcheck=<report file>`; exit code 0, last line `15 of 15 checks passed`. Also re-run `--text-selfcheck` and `--diff-selfcheck`
  (unchanged results expected). Copy the three last lines into the card's Result section, verbatim.

## Tests

- [ ] `CommentsSelfCheckTests` green with the four new checks (it runs `Run()` in-process)
- [ ] Native AOT publish succeeds with `-warnaserror`; the exe prints `15 of 15 checks passed` (result copied in the card)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` — succeeds
- [ ] The published exe's `--comments-selfcheck` exits 0 with `15 of 15 checks passed`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Checking the WinUI widget under AOT (manual plan only), a new self-check switch.

## Suggested commit message

`test(gallery): add the phase 12 range checks to the comments self-check`
