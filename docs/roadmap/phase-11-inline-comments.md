# Phase 11 — Inline comment threads (GitHub review style)

**Done when:** in the Gallery a host can open a comment box under any line of the editor, or of either side of the diff editor: a title, a Markdown toolbar, a multi-line text box,
a Preview toggle and Comment / Cancel buttons; submitted comments are shown as rendered Markdown; a ` ```suggestion ` block is shown as a small red / green diff with an
**Apply suggestion** button that edits the text; threads follow their line when the text above changes; the widget is usable with the keyboard and a screen reader; and the Native AOT self-check passes.
— **Not started.**

Not a port: Monaco has no comment widget in `src/vs/editor` (VS Code's Comments API is in the workbench). It reuses two Monaco concepts: the **view zone** and the **content widget**.
Design: [ADR 0014](../adr/0014-inline-comments.md). Task cards: [docs/tasks/phase-11/](../tasks/phase-11/README.md) (25 cards).

## Checklist

- [ ] Public data model: `Comment`, `CommentAuthor`, `CommentThread`, `CommentThreadState` (P11-01)
- [ ] Markdown extensions for comments: blockquote, strikethrough, underline (P11-02)
- [ ] Markdown toolbar as pure text edits (P11-03, P11-04)
- [ ] Strings, relative time, avatar initials (P11-05)
- [ ] Suggestion blocks: extraction, the model edit, the red / green diff rows (P11-06, P11-07)
- [ ] Thread view model: draft, preview, submit / cancel / Escape (P11-08)
- [ ] Thread tracking in the editor: decorations, `Widget` view zones, heights (P11-09)
- [ ] Controller with the apply-suggestion path, the add command, the control API (P11-10, P11-11)
- [ ] Zone-widget layer: an element over each zone, positioned in the draw pass (P11-12)
- [ ] Zone height follows the content (P11-13)
- [ ] Focus and keys (Tab, Escape, `Ctrl+Enter`, IME) (P11-14)
- [ ] The widget: shared Markdown builder, comments, composer, toolbar, suggestion block (P11-15 … P11-19)
- [ ] Accessibility: UIA names, announcements, high contrast (P11-20)
- [ ] "+" button in the gutter (P11-21)
- [ ] Diff editor: threads on the original and the modified side, level alignment (P11-22)
- [ ] Gallery demo and manual test plan (P11-23)
- [ ] Native AOT self-check of the phase 11 logic (P11-24; the WinUI widget is covered by the manual plan only)
- [ ] Close the phase (P11-25)

## Not delivered (ADR 0014)

Multi-line thread ranges, reactions, comment edit / delete UI, mentions, attachments, avatar images, localization, original-side threads in the inline diff view, "commit suggestion",
a comments panel, persistence (the host stores threads).
