# Phase 11 — Inline comment threads (GitHub review style)

**Done when:** in the Gallery a host can open a comment box under any line of the editor, or of either side of the diff editor: a title, a Markdown toolbar, a multi-line text box,
a Preview toggle and Comment / Cancel buttons; submitted comments are shown as rendered Markdown; a ` ```suggestion ` block is shown as a small red / green diff with an
**Apply suggestion** button that edits the text; threads follow their line when the text above changes; the widget is usable with the keyboard and a screen reader; and the Native AOT self-check passes.
— **Built and covered by automated tests; the manual plan has not been run; the Native AOT self-check of the logic passes, the WinUI widget is NOT verified by it** (see "Status of the done-when line").

Not a port: Monaco has no comment widget in `src/vs/editor` (VS Code's Comments API is in the workbench). It reuses two Monaco concepts: the **view zone** and the **content widget**.
Design: [ADR 0014](../adr/0014-inline-comments.md). Task cards: [docs/tasks/phase-11/](../tasks/phase-11/README.md) (25 cards).

## Checklist

Ticked 2026-10-29 (the last card, P11-25) = built and covered by automated tests where the card has logic; the WinUI cards (P11-12 … P11-22) are covered by the build and a written manual section only, which no agent ran.

- [x] Public data model: `Comment`, `CommentAuthor`, `CommentThread`, `CommentThreadState` (P11-01)
- [x] Markdown extensions for comments: blockquote, strikethrough, underline (P11-02)
- [x] Markdown toolbar as pure text edits (P11-03, P11-04)
- [x] Strings, relative time, avatar initials (P11-05)
- [x] Suggestion blocks: extraction, the model edit, the red / green diff rows (P11-06, P11-07)
- [x] Thread view model: draft, preview, submit / cancel / Escape (P11-08)
- [x] Thread tracking in the editor: decorations, `Widget` view zones, heights (P11-09)
- [x] Controller with the apply-suggestion path, the add command, the control API (P11-10, P11-11)
- [x] Zone-widget layer: an element over each zone, positioned in the draw pass (P11-12)
- [x] Zone height follows the content (P11-13)
- [x] Focus and keys (Tab, Escape, `Ctrl+Enter`, IME) (P11-14)
- [x] The widget: shared Markdown builder, comments, composer, toolbar, suggestion block (P11-15 … P11-19)
- [x] Accessibility: UIA names, announcements, high contrast (P11-20)
- [x] "+" button in the gutter (P11-21)
- [x] Diff editor: threads on the original and the modified side, level alignment (P11-22)
- [x] Gallery demo and manual test plan (P11-23)
- [x] Native AOT self-check of the phase 11 logic (P11-24; the WinUI widget is covered by the manual plan only)
- [x] Close the phase (P11-25)

## Not delivered in phase 11 (ADR 0014)

Multi-line thread ranges, reactions, comment edit / delete UI, mentions, attachments, avatar images, localization, original-side threads in the inline diff view, "commit suggestion",
a comments panel, persistence (the host stores threads).

## Status of the done-when line

- **Built and covered by automated tests**: the data model, the Markdown extensions, the toolbar edits, strings, suggestion extraction / edit / diff, the thread view model, the thread manager (decorations, `Widget` zones), the controller (apply suggestion, events) and the zone heights in the diff editor alignment (tests in `tests/LoomEditor.Core.Tests/Comments`, `tests/LoomEditor.View.Tests/Comments`, `Features/CommentThreads`, `DiffEditor/AdditionalLineHeightsCommentZoneTests`; counts in `docs/PORTING.md`).
- **Built, not run**: the whole WinUI part (zone-widget layer, height loop, focus and keys, composer, toolbar, suggestion view, gutter button, diff editor threads, Gallery demo). There is no WinUI test project; the manual plan [phase-11-manual-tests.md](../testing/phase-11-manual-tests.md) has **not** been run by the agents.
- **Native AOT (P11-24, 2026-10-29)**: `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` succeeded; the published exe with `--comments-selfcheck` ended with `11 of 11 checks passed` (exit 0), `--text-selfcheck` with `10 of 10 checks passed` and `--diff-selfcheck` with `SUMMARY: 9 passed, 0 failed`. This covers the **logic** only; the WinUI widget is **NOT verified** under Native AOT beyond the successful publish.
- **Not verified**: Narrator, NVDA and Accessibility Insights; IME composition in the widget text box; high contrast; the diff editor alignment under heavy typing; the widget layer (position, clipping, height loop, focus, IME), which only the manual plan covers.

## Open risks

- A very long thread makes a tall gap (no maximum height; accepted in P11-13).
- Two comment zones after the same line give two entries in the alignment; the heights still add up (P11-22).
- A disabled "Apply suggestion" button may not show its tooltip on hover (WinUI; P11-19).
- Links in comments are drawn, never opened.
- Colors `descriptionForeground`, `textBlockQuote.border`, `button.*` are not in the built-in defaults; fallbacks are used (P11-16, P11-17).
