# Phase 12 — manual test list

What unit tests cannot check: the highlight of the lines of a multi-line comment thread, the box under the last line, the selection gestures
(add command, gutter "+" button) and the N-line suggestion block, under a real UI. Fill the **Result** line of each check (`pass` / `fail` + a note, or `pending`).

**Machine:** Windows 11, a mouse with a wheel. About 30 minutes in total.

**Gallery setup:** the Gallery controls for ranges are added by P12-13. Until then, the checks that need a Gallery button are `pending`.

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Date and tester:

## P12-09 — Public API

1. `AddCommentThread(3, 6)` (Gallery button, P12-13): the box is under line 6 and its title reads "Comment on lines 3 to 6".
   Result:
2. `AddCommentThread(4)` still opens a box titled "Comment on line 4".
   Result:
3. `AddCommentThread(6, 3)` throws: the Gallery log shows `ArgumentOutOfRangeException`.
   Result:
4. After the add command with a 4-line selection, the event log shows `AddCommentRequested, lines 3–6`.
   Result:
5. After applying a range suggestion, the log shows `SuggestionApplied, lines 3–6`.
   Result:

## P12-10 — Diff editor API

1. A range thread on the modified side (Gallery, P12-13): the box is under the last line, the lines are highlighted, the original side is padded by the box height (the line after the box is level on both sides, ±1 px).
   Result:
2. The same on the original side in side-by-side view.
   Result:
3. Inline view: an original-side range thread is not shown (phase 11 rule).
   Result:
4. With hidden unchanged regions, `startComposing` on a range whose start is hidden reveals it.
   Result:
5. Applying a suggestion on the modified side replaces the range; one `Ctrl+Z` restores it and the diff recomputes.
   Result:
