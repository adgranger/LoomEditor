# P9-31 — Gallery: the diff editor demo and the manual test plan

- **Status:** todo
- **Depends on:** P9-30, P9-28
- **Size:** M

## Goal

The Gallery has a **Diff editor** window with a pair of sample files and switches for every option of phase 9, and `docs/testing/phase-9-manual-tests.md` lists, in full, what the maintainer checks by hand (the manual checks promised by the earlier cards).

## Monaco sources

Sources: none (specified in this card).

## Files to create or change

- `samples/LoomEditor.Gallery/DiffDemoWindow.xaml`, `DiffDemoWindow.xaml.cs` — create (a separate `Window`; XAML in the style of `MainWindow.xaml`: event attributes, code-behind, **no `{Binding}`**)
- `samples/LoomEditor.Gallery/DiffSamples.cs` — create (the two sample texts; they use no WinUI types so the self-check card can link them)
- `samples/LoomEditor.Gallery/MainWindow.xaml`, `MainWindow.Diff.cs` (new `partial` file) — change / create: a **Diff editor** button that opens `DiffDemoWindow`
- `docs/testing/phase-9-manual-tests.md` — create (full plan, below)
- `samples/LoomEditor.Gallery/README.md` or the header comment of `MainWindow.Diff.cs` — change: a "Diff editor" paragraph (follow where P8-37 documented the language server demo)

## Design decisions (already made — do not change)

- **Samples** (`DiffSamples.Original`, `DiffSamples.Modified`): C#, about 60 and 70 lines, built so that every feature has something to show: an unchanged block of 20+ lines at the top (hidden unchanged regions), a method whose body is rewritten (replacement with inner character changes), a method deleted, a method added (pure insertion), a block moved to the other end of the file (moves are computed but not drawn: it must simply appear as one deletion and one insertion), a one-word rename inside a long line (char-level highlight), a whitespace-only change on one line (shows with `IgnoreTrimWhitespace` off and disappears with it on), 25+ unchanged lines at the end. Write both texts as C# raw string literals (`"""`), LF line endings.
- **Window**: the `LoomDiffEditor` fills the window; a toolbar row with `ToggleSwitch`es / `CheckBox`es: *Side by side* (`RenderSideBySide`), *Ignore trim whitespace*, *Hide unchanged regions*, *Render indicators*, *Original editable*, *Read-only modified*, *Overview ruler*; buttons *Previous change*, *Next change* (`GoToPreviousChange` / `GoToNextChange`), *Accessible viewer* (`OpenAccessibleDiffViewer`), *Collapse all*, *Show all*, *Swap sides* (swaps `OriginalText` and `ModifiedText`), *Reset texts*, *Large files* (fills both sides with 5 000 generated lines, 1 % of them changed, from a fixed seed: `new Random(7)`); a status line: `"{ChangeCount} change(s) — diff up to date: {IsDiffUpToDate}"` updated on `DiffChanged`. The window is resizable (the narrow-width switch to inline is testable by resizing it below 900 DIPs).
- The language is `csharp` (`LanguageId`) and the theme follows the main window's current theme (find how `MainWindow` sets `Editor.Theme`; reuse the same helper or copy the one line).
- Keep the main window and the other demos untouched.

## The manual test plan (`docs/testing/phase-9-manual-tests.md`)

Write it in the style of `docs/testing/phase-8-manual-tests.md` (header with how to run, a table per section: *Step*, *Expected*, *Result* column left empty). Sections and checks (each check one row; the **card** that promises it is named in a *Card* column):

1. **Basic diff** (P9-22): the two texts show side by side; changes are highlighted (green / red line backgrounds, darker char level highlight in the rewritten method); the sash drags, double click resets; editing the modified text updates the highlights after about 0.2 s; `OriginalText` / `ModifiedText` follow.
2. **Alignment and scrolling** (P9-19, P9-23): hatched gaps where one side has no lines; lines stay level while scrolling with the wheel, the vertical scrollbar of either side and the horizontal scrollbar; resizing keeps the alignment; inserting lines in the modified text re-aligns without flicker.
3. **Inline view** (P9-24): switch off *Side by side*: deleted blocks appear in red with old line numbers and a `−` sign; shrinking the window below 900 DIPs switches to inline, growing it switches back; deleted text is not selectable (known difference).
4. **Hide unchanged regions** (P9-25): the long unchanged blocks collapse with 3 context lines and a bar "N hidden lines"; click the bar → all lines; click the top / bottom edge → 20 more lines; drag the edge → line by line; `Ctrl+End`, `Ctrl+F` and *Next change* into a hidden area reveal it; works in inline mode; *Collapse all* / *Show all*.
5. **Navigation** (P9-26): `Alt+F5` / `Shift+Alt+F5` and the buttons cycle through the changes with wrap-around and center the change.
6. **Overview ruler** (P9-27, P9-28): red and green marks line up with the changes; the slider follows the scroll; click and drag the strip scrolls both sides; the wheel over the strip scrolls; *Overview ruler* off hides it.
7. **Read-only** (P9-20): the original side does not accept typing, paste, delete, undo; *Original editable* enables it; *Read-only modified* disables typing there; selection, copy, find, scrolling still work in read-only editors.
8. **Accessible diff viewer with Narrator** (P9-30): `F7` opens it; the header and every row are read with the labels of the card; arrows move; `Enter` reveals and closes; `Esc` closes and returns focus; the editors are not announced while it is open.
9. **Options** (P9-12 … P9-17): *Ignore trim whitespace* off shows the whitespace-only line; *Render indicators* off removes `+` / `−` signs; high contrast theme keeps everything readable (the dashed borders are solid: known difference).
10. **Performance feel** (P9-32): press *Large files* (two 5 000-line texts) and scroll, type and resize: no visible lag, the status line reaches *up to date* in a few seconds.
11. **Known differences to confirm** (from `PORTING.md`): no moved block drawing; deleted text in the inline view cannot be selected; the old diff is kept (stale) for a moment while typing; no sounds; no revert buttons.

## Tests

- [ ] `tests/LoomEditor.View.Tests/DiffEditor/DiffSamplesSanityTests.cs` (links `DiffSamples.cs` the way P8-37 linked its Gallery files): `Samples_ProduceChanges_WithInnerChanges` (the default computer on the two texts gives ≥ 4 changes, at least one with inner changes and one pure insertion and one pure deletion), `Samples_HideUnchangedRegions_ProducesAtLeastTwoRegions` (`UnchangedRegion.FromDiffs` with 3 / 3), `Samples_WhitespaceOnlyLine_OnlyWithoutIgnoreTrim` (with `IgnoreTrimWhitespace = true` the number of changes is one smaller).
- [ ] Manual: run the plan sections you can; record in the card which sections were run and by whom (the agent runs none: it states that in the card result).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` — result stated in the card (`succeeded` + the app still running after 10 s, or `NOT verified` with the reason; never ticked if not run)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The self-check and the benchmarks (P9-32), docs of the phase (P9-33).

## Suggested commit message

`feat(gallery): add the diff editor demo and the phase 9 manual test plan`
