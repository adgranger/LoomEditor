# P13-09 — Gallery: the "Host API" window and the complete manual test plan

- **Status:** done
- **Depends on:** P13-08
- **Size:** M

**Recommended model:** Sonnet

## Goal

A **Host API demo** window in the Gallery uses **only public members**, as VIGO would: a fake blame column synced with the editor, host decoration styles, conflict and
staging tints, hidden unchanged lines, a conflict header in a view zone, gutter clicks that stage a line, the language picked from a file name, and a text reset. Every
control named by the manual sections P13-04 … P13-08 exists, and the manual plan is complete.

## Sources

Sources: none (specified in this card). Read first: `samples/LoomEditor.Gallery/DiffDemoWindow.xaml`, `DiffDemoWindow.xaml.cs`, `MainWindow.Diff.cs` (how a demo window is
opened with the current theme), `MainWindow.xaml` (where the "Diff editor window" button is), `SampleText.cs`, `docs/testing/phase-13-manual-tests.md` (all sections),
`docs/tasks/phase-12/P12-13-gallery-demo-and-manual-plan.md` (how the plan is completed), [phase-13/README.md](README.md) section 2.

## Files to create or change

- `samples/LoomEditor.Gallery/HostApiDemoWindow.xaml` — create
- `samples/LoomEditor.Gallery/HostApiDemoWindow.xaml.cs` — create (window, log, language, text)
- `samples/LoomEditor.Gallery/HostApiDemoWindow.Blame.cs` — create (blame column)
- `samples/LoomEditor.Gallery/HostApiDemoWindow.Diff.cs` — create (styles, decorations, hidden areas, zones, gutter)
- `samples/LoomEditor.Gallery/HostApiSamples.cs` — create (the sample text and fake blame data; WinUI-free)
- `samples/LoomEditor.Gallery/MainWindow.xaml`, `MainWindow.Diff.cs` — change (one button **Host API demo** next to "Diff editor window")
- `docs/testing/phase-13-manual-tests.md` — change

## Design decisions (already made — do not change)

- **Public API only.** The four new files use no `internal` member of any LoomEditor assembly (the Gallery can see some internals; this window must not: it is the host's
  point of view). Add a comment at the top of `HostApiDemoWindow.xaml.cs` saying so.
- **Layout** (XAML, no bindings): a top bar of buttons and switches, below it a `Grid` with a 220-DIP `Canvas` **blame column** (clipped) on the left and a `LoomEditor` on the
  right, and an event log `ListView` (last 50 lines, newest first) at the bottom.
- **`HostApiSamples`**: `public const string FileName = "Conflicts.cs"`; `Text` = a C# file of ≥ 60 lines with a conflict block on lines 10–16 (`<<<<<<<` on line 9, `=======`
  on 13, `>>>>>>>` on 17: current side 10–12, incoming side 14–16); `Blame(int line)` returns `(string Commit, string Author, int AgeDays)` from a fixed table (a new commit every
  7 lines, ages 1 … 400 days).
- **Language:** on load `Editor.LanguageId = BuiltInLanguages.GetLanguageIdByFileName(FileName) ?? "plaintext"`; a `TextBox` **File name** + button **Apply language**
  does the same for the typed name and logs `language <id or null>`.
- **Blame column** (`HostApiDemoWindow.Blame.cs`): redraw on `ScrollChanged` and `LayoutChanged`; for each model line from the first visible one (binary search on
  `GetTopForLineNumber(line, true) - ScrollTop`) until the top passes the editor's height, a `TextBlock` (`"{commit} {author}"`, pooled) at `top =
  GetTopForLineNumber(line, true) - ScrollTop`, height = next line's top − this top (skip rows of height 0: hidden lines). Its background comes from the age (three
  shades), its font size follows `LineHeight`. A check box **Line numbers** (checked) drives `Editor.LineNumbersEnabled`.
- **Styles** (`HostApiDemoWindow.Diff.cs`, registered once in the window's constructor): `gallery-blame-recent` (background, light and dark colors), `gallery-staged` (left bar:
  `Border`, `BorderWidth = 0`, `LeftBorderWidth = 3`), `gallery-conflict-current` and `gallery-conflict-incoming` (backgrounds + `OverviewRulerColor` on the options).
- **Buttons and switches** (exact labels, used by the manual plan): **Conflict colors** (decorations on 10–12 with `line-insert` and on 14–16 with `line-delete`, plus the two
  `gallery-conflict-*` classes on the marker lines 9, 13, 17), **Clear decorations**, **Hide unchanged** (`SetHiddenAreas([new Range(20, 1, 40, 1)])`), **Show all**
  (`SetHiddenAreas([])`), **Add conflict header** (a zone after line 8, `LineHeight` high, content = a `StackPanel` with **Accept current** and **Accept incoming** buttons that
  log their click), **Add text box zone** (a zone after line 30, two lines high, content = a `TextBox`), **Remove zones** (`RemoveViewZone` for each id, logging the result,
  then once more for the first id to log `false`), **Reload same text** (`Editor.Text = HostApiSamples.Text` then `Editor.ResetText(HostApiSamples.Text)`), the **Line
  numbers** check box.
- **Gutter:** `GutterClicked` logs `GutterClicked line {n} {area}`; for `LineDecorations` it toggles a `gallery-staged` decoration on that line (its own
  `DecorationsCollection`) and sets `Handled = true`; other areas are only logged.
- **Theme:** the window takes the main window's theme (as `DiffDemoWindow`); the blame column uses the editor background and foreground of the theme.
- **Manual plan:** complete **Gallery setup** (the window, each control by its exact label); check that every control named by sections P13-04 … P13-08 exists (fix the section
  text where a label differs, never the reverse without a reason); add `## End-to-end A — Blame` (open the demo → scroll, zoom, wrap, fold → the column stays aligned →
  **Line numbers** off), `## End-to-end B — Conflicts` (**Conflict colors** → **Add conflict header** → **Accept current** → **Clear decorations** → **Remove zones**) and
  `## End-to-end C — Staging` (**Hide unchanged** → click the line decorations of three lines → each toggles its bar, the caret does not move → **Show all**), each step
  with a measurable expectation and a blank "Result:". The self-check paragraph names P13-10 (`--hostapi-selfcheck`).

## Tests

- [x] None automated (Gallery UI). `HostApiSamples` is WinUI-free: P13-10 links it into the View tests.
- [x] Manual: the plan is complete (not run by the agent).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `grep -n "internal\|_viewModel\|_context" samples/LoomEditor.Gallery/HostApiDemoWindow*.cs` finds nothing that reaches an internal member
- [x] Every control named in `docs/testing/phase-13-manual-tests.md` exists in `HostApiDemoWindow.xaml` (grep each label)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

A real git backend, a real three-way merge, screenshots, running the manual plan, the self-check (P13-10).

## Suggested commit message

`feat(gallery): add the host API demo window and the phase 13 manual plan`
