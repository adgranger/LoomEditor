# P2-21 — Gallery: large file, long line, Unicode and a scroll test

- **Status:** todo
- **Depends on:** P2-20
- **Size:** S

## Goal

Make phase 2 visible and measurable in the Gallery: load a 100 MB document, a single 1 MB line, and a Unicode sample
(tabs, CJK, emoji, combining marks). A "Scroll test" button scrolls through the document for 10 seconds and shows
frame statistics. Its numbers prove the phase 2 "done when": *a 100 MB file scrolls fluidly*.

## Monaco sources

None (sample app only).

## Files to create or change

- `samples/LoomEditor.Gallery/MainWindow.xaml(.cs)` — update the title to phase 2; a second button row (or a
  `CommandBar` overflow) with **Large file (100 MB)**, **Long line (1 MB)**, **Unicode sample**, **Scroll test**;
  the status line shows the last action's timing
- `samples/LoomEditor.Gallery/LargeDocuments.cs` — create: `static string Create100MB()`, `static string CreateLongLine()`,
  `static string Unicode` (deterministic: `new Random(42)`)
- `samples/LoomEditor.Gallery/ScrollTest.cs` — create: drives the editor and collects the frame intervals
- `samples/LoomEditor.Gallery/FrameStatistics.cs` — create: pure computation (count, average fps, p50 / p95 / max interval, frames over 33.3 ms)

## Design decisions (already made — do not change)

- `Create100MB` builds about 100 × 1024 × 1024 UTF-16 characters of code-like lines (indentation with tabs and
  spaces, identifiers, numbers; length 0–120, average ~40) with a `StringBuilder` of known capacity. Same seed →
  same text.
- Load timing: `Stopwatch` from the click to the first `CompositionTarget.Rendering` after `Editor.Text` is set.
  Show `Loaded 2,6xx,xxx lines in N ms`. Run the generation on a background thread (`Task.Run`) and show "Generating…".
  Only the assignment to `Editor.Text` happens on the UI thread (threading rule of `ARCHITECTURE.md`).
- Scroll test: for 10 seconds, on each `CompositionTarget.Rendering`, `Editor.ScrollTop += 3 × lineHeight`
  (≈ 57 DIPs per frame, wrapping to 0 at the end). Record `RenderingEventArgs.RenderingTime` deltas. Then show
  `FrameStatistics` in the status line: `Frames: n · avg 59.8 fps · p95 16.9 ms · max 33.1 ms · >33 ms: 0`.
  It uses only the public API (`ScrollTop`, `Text`, `Model`).
- The phase 1 buttons stay. If the row gets too wide, move the phase 1 buttons into a `CommandBar` with overflow.
  Do not remove them.
- No reflection, plain `Click` handlers (Native AOT), as in P1-32.

## Tests

- [ ] Existing tests pass
- [ ] Manual: Large file loads, the status shows the line count and the load time; scrolling by scrollbar drag,
      wheel and Scroll test all work; memory stays stable over 3 scroll tests (Task Manager)
- [ ] Manual: Long line: shows 10,000 characters + "Show more (…)"; horizontal scrolling works; the UI does not freeze
- [ ] Manual: Unicode sample: CJK takes two columns' width, emoji are in color, combining marks stack, tabs align

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the published (AOT) app passes the manual checks
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Recording the reference numbers (P2-22), opening real files from disk (no file picker in phase 2), any optimization.

## Suggested commit message

`feat(gallery): add large-file, long-line and Unicode samples with a scroll test`
