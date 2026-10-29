# Phase 12 — Multi-line ranges for inline comment threads

**Done when:** in the Gallery a host can anchor a comment thread to a range of lines (GitHub's "start line … line"), not only to one line: the range is highlighted in the editor,
follows edits, and the comment box sits under its last line; the user can create such a thread from a multi-line selection (gutter "+" button or the add command); a
` ```suggestion ` block replaces the whole range (red / green diff of N lines, one undo step); the same works on both sides of the diff editor; the single-line API of phase 11
still compiles unchanged; and the Native AOT self-check passes. — **Not started.**

Not a port (as phase 11): Monaco has no comment widget in `src/vs/editor`. Design: [ADR 0014](../adr/0014-inline-comments.md) and the decisions of
[docs/tasks/phase-12/README.md](../tasks/phase-12/README.md). Task cards: [docs/tasks/phase-12/](../tasks/phase-12/README.md) (15 cards).

## Checklist

- [ ] `CommentThread` has a line range: `StartLineNumber` … `LineNumber` (P12-01)
- [ ] Suggestion edit, stale check and range text for N lines (P12-02)
- [ ] Suggestion diff rows for N old lines (P12-03)
- [ ] Strings for ranges; the toolbar's suggestion block accepts a multi-line seed (P12-04)
- [ ] The manager anchors a thread to a range and follows edits (P12-05)
- [ ] The range is highlighted (P12-06)
- [ ] Controller: add, apply suggestion and add requests with a range (P12-07)
- [ ] The add command uses a multi-line selection (P12-08)
- [ ] Public API: `LoomEditor` (P12-09) and `LoomDiffEditor` (P12-10)
- [ ] The widget: title, accessibility and suggestion block for a range (P12-11)
- [ ] The gutter "+" button with a multi-line selection (P12-12)
- [ ] Gallery demo and manual test plan (P12-13)
- [ ] Native AOT self-check (P12-14)
- [ ] Close the phase (P12-15)

## Not in phase 12

Character-precise ranges (a thread covers whole lines), dragging on the gutter "+" button to select lines, shift-click range selection in the gutter, a gutter bar or
overview-ruler mark for the range, overlapping-range rules (two threads may cover the same lines), and everything phase 11 deferred (reactions, edit / delete UI, mentions,
attachments, avatar images, localization, original-side threads in the inline diff view, "commit suggestion", a comments panel, persistence).
