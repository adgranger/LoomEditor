# P10-30 — Native AOT self-check of the phase 10 logic

- **Status:** done
- **Depends on:** P10-29
- **Size:** M

## Goal

The published (Native AOT) Gallery proves, without a human, that the phase 10 logic works in the AOT build: `--text-selfcheck=<file>` runs the snippet parser (including runtime-built `Regex` transforms), a snippet insertion with Tab stops, the whitespace computer, control character substitution, the font feature parser, the zoom state, overtype typing, smart select and the marker list, writes a pass / fail report and exits 0 or 1.

## Monaco sources

Sources: none (specified in this card; the pattern is `DiffSelfCheck` of P9-32 and `--lsp-selfcheck` of P8-37: **read `samples/LoomEditor.Gallery/DiffSelfCheck.cs` and `MainWindow.Diff.cs` first and copy their structure**).

## Files to create or change

- `samples/LoomEditor.Gallery/TextSelfCheck.cs` — create (`internal static class`; no WinUI types except the dispatcher it is given)
- `samples/LoomEditor.Gallery/MainWindow.Diff.cs` or a new `MainWindow.TextCheck.cs` — change / create: the `--text-selfcheck=<file>` argument handled like `--diff-selfcheck=`
- `tests/LoomEditor.View.Tests/Features/Snippet/TextSelfCheckTests.cs` — create (links `TextSelfCheck.cs` and runs the checks against the test harness: the pure checks must pass in the normal test run)
- `docs/testing/phase-10-manual-tests.md` — change: a line telling how to run the self-check

## Design decisions (already made — do not change)

- Checks, each one `PASS name — detail` or `FAIL name — reason` (an exception is a `FAIL`, never a crash): `snippet-parse` (`for (${1:i} = 0; $1 < ${2:n}; $1++) {\n\t$0\n}` gives 3 distinct indices and a final tabstop), `snippet-transform` (`${TM_FILENAME/(.*)\..+$/$1/}`-style: a variable with a regex transform resolves with a `ISnippetVariableHost` double — this is the **runtime `Regex` under Native AOT** check and the main reason for the card), `snippet-insert` (insert into a `TextModel`-backed editor context, `Next` visits the placeholders, the final text is the expected one), `whitespace` (`RenderWhitespaceComputer` on `"\ta  b "` in `All` and `Boundary`), `control-characters` (`ControlCharacters.Substitute` on a line with `\u0001` and `\u007F`), `font-features` (`FontFeatureSettings.Parse("'liga' 1, \"calt\" 0")`), `zoom` (`EditorZoomState` clamps), `overtype` (typing `a` over `123` in overtype mode gives `a23`), `smart-select` (expand twice from inside a word in `foo(bar)` gives the word, then the brackets' content), `marker-list` (next from the start with three markers).
- The report format and the exit code are exactly those of `DiffSelfCheck` (first line `LoomEditor text self-check`, one line per check, last line `n of m checks passed`).
- The checks use only internal types the Gallery already reaches (`InternalsVisibleTo Gallery` exists for View; if Core internals are needed, check `src/LoomEditor.Core/LoomEditor.Core.csproj` for `InternalsVisibleTo` and add `LoomEditor.Gallery` there, one line).
- Run it once in the card, in the background, with the published exe, **and kill the process** afterwards (`Stop-Process -Name LoomEditor.Gallery -Force`); the card result states the real outcome. If the AOT publish fails for a reason outside this card, report it, do not hack around it.

## Tests

- [x] New: `TextSelfCheckTests` runs every check and asserts all pass; one test per check name so a failure names the check
- [x] Manual: none (the self-check is the check)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` — succeeds (use the exact command line that `docs/tasks/phase-9/P9-32-diff-self-check-and-benchmarks.md` and its Result section record; the SDK flags differ between .NET 11 previews), and the published exe run with `--text-selfcheck=<file>` ends with `n of n checks passed` and exit code 0 (stated in the card's Result section, with the date)
- [x] No Gallery process left running
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

UI automation of the new features, benchmarks.

## Suggested commit message

`test(gallery): add the Native AOT self-check of the phase 10 logic`

## Result

- Unit tests: `TextSelfCheckTests` (12: one per check, the check list, the report format) pass in the normal test run; the ten checks pass.
- Native AOT publish: `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` **succeeded** (exit 0).
- Native AOT run (2026-10-29, after the start-up fix below): `LoomEditor.Gallery.exe --text-selfcheck=%TEMP%\text-selfcheck.txt` on the published exe ended with `10 of 10 checks passed` and exit code 0; `--diff-selfcheck` ended with `SUMMARY: 9 passed, 0 failed`, exit code 0.
- Start-up crash found on the way: the published exe (and the normal build) exited with `0xc000027b` (`XamlParseException`, "Failed to assign to property ToggleSwitch.IsOn", logged in `%TEMP%\LoomEditor.Gallery.log`). Cause: `OnControlCharactersToggled` (P10-10) used `Editor` while `InitializeComponent` set `IsOn="True"`, before `Editor` existed. Fix: the same `Editor is not null` guard as the other handlers.
- The report format is the one the card spells out (`LoomEditor text self-check`, `PASS|FAIL name — detail`, `n of m checks passed`), not the `SUMMARY:` line of `DiffSelfCheck`.
- No Gallery process was left running.
