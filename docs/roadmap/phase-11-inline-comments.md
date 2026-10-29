# Phase 11 — Inline comment threads (GitHub review style)

**Done when:** in the Gallery a host can open a comment box under any line of the editor, or of either side of the diff editor: a title, a Markdown toolbar, a multi-line text box,
a Preview toggle and Comment / Cancel buttons; submitted comments are shown as rendered Markdown; a ` ```suggestion ` block is shown as a small red / green diff with an
**Apply suggestion** button that edits the text; threads follow their line when the text above changes; the widget is usable with the keyboard and a screen reader; and the Native AOT self-check passes.
— **Not started.**

Not a port: Monaco has no comment widget in `src/vs/editor` (VS Code's Comments API is in the workbench). It reuses two Monaco concepts: the **view zone** and the **content widget**.
Design: [ADR 0014](../adr/0014-inline-comments.md). Task cards: [docs/tasks/phase-11/](../tasks/phase-11/README.md) (21 cards).

## Checklist

- [ ] Public data model: `Comment`, `CommentAuthor`, `CommentThread`, `CommentThreadState` (P11-01)
- [ ] Markdown extensions for comments: blockquote, strikethrough, underline (P11-02)
- [ ] Markdown toolbar as pure text edits: bold, italic, underline, strikethrough, inline code, link, heading, lists, quote, code block, suggestion (P11-03, P11-04)
- [ ] Suggestion blocks: extraction, the model edit, the red / green diff rows (P11-05, P11-06)
- [ ] Thread view model: draft, preview, submit / cancel, relative time, strings (P11-07)
- [ ] Thread tracking in the editor: decorations, `Widget` view zones, heights (P11-08)
- [ ] Control API and the `CommentThreads` feature (`AddCommentThread`, events, commands) (P11-09)
- [ ] Overlay hosting of the widget, repositioned on scroll, height follows content (P11-10)
- [ ] Focus and key routing (Tab, Escape, `Ctrl+Enter`) (P11-11)
- [ ] The widget: shared Markdown builder, comments list, composer, suggestion block (P11-12 … P11-15)
- [ ] Accessibility: UIA names, announcements, high contrast (P11-16)
- [ ] "+" button in the gutter (P11-17)
- [ ] Diff editor: threads on the original and the modified side, level alignment (P11-18)
- [ ] Gallery demo and manual test plan (P11-19)
- [ ] Native AOT self-check of the phase 11 logic (P11-20)
- [ ] Close the phase (P11-21)

## Not delivered (ADR 0014)

Multi-line thread ranges, reactions, comment edit / delete UI, mentions, attachments, avatar images, localization, original-side threads in the inline diff view, "commit suggestion",
a comments panel, persistence (the host stores threads).
