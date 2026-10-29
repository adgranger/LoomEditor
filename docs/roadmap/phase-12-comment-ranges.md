# Phase 12 — Multi-line ranges for inline comment threads

**Done when:** in the Gallery a host can anchor a comment thread to a range of lines (GitHub's "start line … line"), not only to one line: the range is highlighted in the editor,
follows edits, and the comment box sits under its last line; the user can create such a thread from a multi-line selection (gutter "+" button or the add command); a
` ```suggestion ` block replaces the whole range (red / green diff of N lines, one undo step); the same works on both sides of the diff editor; the single-line API of phase 11
still compiles unchanged; and the Native AOT self-check passes. — **Built and covered by automated tests; the manual plan has not been run; the Native AOT self-check of the logic passes, the WinUI widget is NOT verified by it** (see "Status of the done-when line").

Not a port (as phase 11): Monaco has no comment widget in `src/vs/editor`. Design: [ADR 0014](../adr/0014-inline-comments.md) and the decisions of
[docs/tasks/phase-12/README.md](../tasks/phase-12/README.md). Task cards: [docs/tasks/phase-12/](../tasks/phase-12/README.md) (15 cards).

## Checklist

Ticked 2026-10-29 (the last card, P12-15) = built and covered by automated tests where the card has logic; the WinUI cards (P12-09 … P12-12) are covered by the build and a written manual section only, which no agent ran.

- [x] `CommentThread` has a line range: `StartLineNumber` … `LineNumber` (P12-01)
- [x] Suggestion edit, stale check and range text for N lines (P12-02)
- [x] Suggestion diff rows for N old lines (P12-03)
- [x] Strings for ranges; the toolbar's suggestion block accepts a multi-line seed (P12-04)
- [x] The manager anchors a thread to a range and follows edits (P12-05)
- [x] The range is highlighted (P12-06)
- [x] Controller: add, apply suggestion and add requests with a range (P12-07)
- [x] The add command uses a multi-line selection (P12-08)
- [x] Public API: `LoomEditor` (P12-09) and `LoomDiffEditor` (P12-10)
- [x] The widget: title, accessibility and suggestion block for a range (P12-11)
- [x] The gutter "+" button with a multi-line selection (P12-12)
- [x] Gallery demo and manual test plan (P12-13)
- [x] Native AOT self-check (P12-14)
- [x] Close the phase (P12-15)

## Not in phase 12

Character-precise ranges (a thread covers whole lines), dragging on the gutter "+" button to select lines, shift-click range selection in the gutter, a gutter bar or
overview-ruler mark for the range, overlapping-range rules (two threads may cover the same lines), and everything phase 11 deferred (reactions, edit / delete UI, mentions,
attachments, avatar images, localization, original-side threads in the inline diff view, "commit suggestion", a comments panel, persistence).

## Status of the done-when line

- **Built and covered by automated tests**: the line range of `CommentThread`, the N-line suggestion edit / diff / stale check, the strings and the toolbar seed, the manager (range decoration, edits, highlight options), the controller (add, apply, add requests) and the add command with `CommentLineRange.FromSelection` (tests in `tests/LoomEditor.Core.Tests/Comments`, `tests/LoomEditor.View.Tests/Comments`, `Features/CommentThreads`, `tests/LoomEditor.Rendering.Tests/Layout/DecorationStylesTests.cs`; counts in `docs/PORTING.md`).
- **Built, not run**: the WinUI part (public API on `LoomEditor` and `LoomDiffEditor`, widget title / accessibility / N-line suggestion block, gutter "+" button with a selection, Gallery demo). There is no WinUI test project; the manual plan [phase-12-manual-tests.md](../testing/phase-12-manual-tests.md) has **not** been run by the agents.
- **Native AOT (P12-14, 2026-10-29)**: `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` succeeded; the published exe with `--comments-selfcheck` ended with `15 of 15 checks passed` (exit 0), `--text-selfcheck` with `10 of 10 checks passed` and `--diff-selfcheck` with `SUMMARY: 9 passed, 0 failed`. This covers the **logic** only; the WinUI widget is **NOT verified** under Native AOT beyond the successful publish.
- **Not verified**: the manual plan; Narrator and NVDA (the range title and description); the range highlight in high contrast; word wrap with a range; the diff editor (a range on each side, alignment); the widget layer.

## Open risks

- Two threads may cover the same lines (no overlap rule); each keeps its own box under its own last line.
- Deleting a whole range collapses the thread to one line instead of removing it (as phase 11 never deletes a thread on an edit).
- A selection that ends at column 1 of a later line drops that last line (`FromSelection`): intended, but not checked by hand.
- The range highlight is whole-line only; no gutter bar or overview-ruler mark.
