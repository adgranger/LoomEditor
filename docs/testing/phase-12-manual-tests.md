# Phase 12 — manual test list

What unit tests cannot check: the highlight of the lines of a multi-line comment thread, the box under the last line, the selection gestures
(add command, gutter "+" button) and the N-line suggestion block, under a real UI. Fill the **Result** line of each check (`pass` / `fail` + a note, or `pending`).

**Machine:** Windows 11, a mouse with a wheel. About 30 minutes in total.

**Gallery setup:** pick **Comment threads** in the "Features" picker: the phase 11 threads (lines 8, 10, 24) plus a 4-line thread on lines 12–15 (the `for` loop of `Sum`)
whose comment by Grace Hopper holds a suggestion that rewrites line 14 and keeps the other three. In the **Comment threads** Expander: **Comment on selection**
(the add command, also `Ctrl+Alt+M`), **Add range 3–6**, **Add range 6–3 (error)**, **Add comment at caret**, the **Gutter "+" button** switch and the event log. The
diff window (**Diff demo**) has **Comment sample** (the same threads on the original side), **Add on original**, **Add on modified** (both use the lines of that side's
selection) and the **Gutter "+" button** box. **Self-check:** the logic of this phase also runs without a UI in the Native AOT self-check (P12-14).

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Date and tester:

## P12-09 — Public API

1. `AddCommentThread(3, 6)` (**Add range 3–6**): the box is under line 6 and its title reads "Comment on lines 3 to 6".
   Result:
2. `AddCommentThread(4)` still opens a box titled "Comment on line 4".
   Result:
3. `AddCommentThread(6, 3)` throws (**Add range 6–3 (error)**): the Gallery log shows `ArgumentOutOfRangeException`.
   Result:
4. After **Comment on selection** with a 4-line selection, the event log shows `AddCommentRequested, lines 3–6`.
   Result:
5. After applying a range suggestion, the log shows `SuggestionApplied, lines 3–6`.
   Result:

## P12-10 — Diff editor API

1. A range thread on the modified side (select 4 lines in the modified editor, **Add on modified**): the box is under the last line, the lines are highlighted, the original side is padded by the box height (the line after the box is level on both sides, ±1 px).
   Result:
2. The same on the original side in side-by-side view (**Add on original**, or the 12–15 thread of **Comment sample**).
   Result:
3. Inline view: an original-side range thread is not shown (phase 11 rule).
   Result:
4. With hidden unchanged regions, `startComposing` on a range whose start is hidden reveals it.
   Result:
5. Applying a suggestion on the modified side replaces the range; one `Ctrl+Z` restores it and the diff recomputes.
   Result:

## P12-11 — The widget for a range

1. The box title and Narrator's name read "Comment thread on lines 3 to 6".
   Result:
2. **Suggest a change** in the composer of a 4-line thread inserts a block with the 4 lines selected.
   Result:
3. A 4-line suggestion that changes line 2 shows `3 context + 1 removed + 1 added` rows, in that order.
   Result:
4. **Apply suggestion** replaces the 4 lines; one `Ctrl+Z` restores them.
   Result:
5. Edit a line inside the range, then press an old Apply: the "lines changed" tooltip shows and nothing changes.
   Result:
6. An empty suggestion block on a range shows all lines removed; Narrator's description ends with "Lines deleted."
   Result:

## P12-12 — The gutter "+" button requests the selected lines

1. Select lines 3–6 (mouse drag in the text), hover the gutter of line 4, click **+**: the log shows `AddCommentRequested, lines 3–6` and the box opens under line 6.
   Result:
2. Select lines 3–6 with `Shift+Down` from line 3 (the selection ends at column 1 of line 7): the request is 3–6, not 3–7.
   Result:
3. Hover line 9 (outside the selection), click: the request is line 9 only.
   Result:
4. No selection: phase 11 behavior.
   Result:
5. Narrator reads "Add a comment on lines 3 to 6" on the button inside the selection.
   Result:

## P12-21 — Exact text in the public API

Controls for this section are added by P12-23 (Gallery buttons).

1. Select `value` on a line, **Comment on selection**: the log shows `AddCommentRequested, line N, text N:a–N:b` and the box opens under line N with only the word tinted.
   Result:
2. **Add on selection (Range API)** with a full line selected by mouse drag: a whole-line thread (no tint on a single line, the log shows no `text`).
   Result:
3. **Add text out of document (error)**: the log shows `ArgumentOutOfRangeException`.
   Result:
4. In the diff window, select a word on the modified side, **Add on modified**: only that word is tinted, the original side is padded by the box height.
   Result:
5. **Add range 3–6** still gives a whole-line thread with all 4 lines tinted.
   Result:

## P12-22 — Exact text: box and gutter

Controls for this section are those of P12-23 (Gallery buttons).

1. On the box of a thread on `value`, Accessibility Insights for Windows shows the name "Comment thread on line N" and the `HelpText` "Commented text: value".
   Result:
2. Type inside the word: the `HelpText` follows.
   Result:
3. Select `value`, hover the gutter of that line, click **+**: the log shows `AddCommentRequested, line N, text N:a–N:b`; the button's name is "Add a comment on the selected text on line N".
   Result:
4. Select lines 3–6 with `Shift+Down`, click **+** on line 4: request `lines 3–6`, no `text` (P12-12 unchanged).
   Result:
5. Hover a line outside the selection: the request is for that line only.
   Result:

## End-to-end A — Range review (editor)

| # | Step | Result |
|---|---|---|
| 1 | Select 4 lines (for example 12–15), **Comment on selection**: the box opens under line 15, titled "Comment on lines 12 to 15", the 4 lines are highlighted | pending |
| 2 | Type `Please`, select it, **B**; then `±` (suggest a change): a block holding the 4 lines appears, edit one line | pending |
| 3 | **Preview** shows bold text and the suggested change as `3 context + 1 removed + 1 added` rows; back to the box | pending |
| 4 | **Comment**: the comment appears with the block and an **Apply suggestion** button; the log shows `CommentSubmitted, lines 12–15` | pending |
| 5 | **Apply suggestion**: the 4 lines are replaced, the log shows `SuggestionApplied, lines 12–15`, the thread is resolved; `Ctrl+Z` restores the 4 lines in one step | pending |

## End-to-end B — Range review (diff)

| # | Step | Result |
|---|---|---|
| 1 | **Comment sample**, side by side: the 12–15 thread on the original side is highlighted, the modified side is padded, both sides stay level | pending |
| 2 | Select 4 lines in the modified editor, **Add on modified**: write and post a comment; the log shows the side and `lines …` | pending |
| 3 | Apply a suggestion on the modified side: the 4 lines change, the diff updates, `Ctrl+Z` restores them | pending |
| 4 | Inline view, then side by side: threads, drafts and alignment come back | pending |

## C — Robustness

| # | Check | Result |
|---|---|---|
| 1 | Type inside the range of the 12–15 thread: the range grows and the highlight follows | pending |
| 2 | Type above the range (a new line, then Enter): the thread and its highlight move down with it | pending |
| 3 | Type below the range: nothing changes | pending |
| 4 | Delete the whole range (select 12–15, Delete): the thread follows the Phase 11 rule for a deleted line, with no exception | pending |
| 5 | Load a sample with fewer lines (the Features picker) while range threads exist: no exception, the threads are removed | pending |
| 6 | **Resolve first thread**, then reopen it in the box: the highlight follows the state | pending |
| 7 | Word wrap on: the highlight covers every row of every line of the range | pending |

## D — Keyboard only

| # | Check | Result |
|---|---|---|
| 1 | `Shift+Down` ×3 from line 12, `Ctrl+Alt+M`: a thread opens on 12–14 (not 12–15) with the focus in the composer | pending |
| 2 | `Esc` cancels an empty thread and removes it; the focus returns to the text | pending |
| 3 | Tab through the box: the toolbar, **Comment**, **Cancel**, **Apply suggestion**; the gutter "+" is not a tab stop | pending |

## Findings

| # | Section | Finding | Severity |
|---|---|---|---|
| | | | |
