# P12-18 — The manager anchors a thread to the exact text and keeps its columns

- **Status:** todo
- **Depends on:** P12-17
- **Size:** M

**Recommended model:** Sonnet

## Goal

`CommentThreadManager.Add(Range textRange, title)` creates an exact-text thread whose decoration spans exactly that text; edits move, grow, shrink or empty it and the
thread's columns follow; the box still sits under the last line. Whole-line threads now also report their columns (1 → end of the last line). No highlight yet (P12-19).

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 14, 16 and 17). Read first: `src/LoomEditor.View/Comments/CommentThreadManager.cs` (all:
`Add`, `SyncLines`, `UpdateOptions`, `OptionsFor`, `Entry`, the `_busy` guard), `tests/LoomEditor.View.Tests/Comments/CommentThreadManagerTests.cs` (how the model and the
`ViewZones` are built; the P12-05 range tests to imitate), `src/LoomEditor.Core/Comments/CommentThread.cs` (`SetRange`, P12-16), `docs/tasks/phase-12/P12-05-manager-range-anchor.md`.

## Files to create or change

- `src/LoomEditor.View/Comments/CommentThreadManager.cs` — change
- `tests/LoomEditor.View.Tests/Comments/CommentThreadManagerTests.cs` — change (add tests; keep every existing test)

## Design decisions (already made — do not change)

- New `CommentThread Add(Range textRange, string? title = null)`: `ArgumentNullException.ThrowIfNull(textRange)`; the **lines** are validated like the line overload
  (`ArgumentOutOfRangeException.ThrowIfLessThan(textRange.StartLineNumber, 1)`, `ThrowIfGreaterThan(textRange.StartLineNumber, _model.LineCount)`,
  `ThrowIfGreaterThan(textRange.EndLineNumber, _model.LineCount)`); then `Range r = _model.ValidateRange(textRange)` (columns clamped); creates
  `new CommentThread(id, r, title)`, one decoration over `r` with `OptionsFor(thread)`, one zone with `AfterLineNumber = r.EndLineNumber`. It does **not** apply the
  `CommentRange` rule (callers do, P12-20 / P12-21): an exact thread may cover full lines if a caller asks for it.
- Move the shared body of both `Add` methods (zone, decoration, `Entry`, the `Changed` subscription, `ThreadsChanged`) into one private
  `CommentThread AddCore(CommentThread thread, Range decorationRange)`; the line overload becomes: validate (unchanged), create the thread, then
  `thread.SetRange(start, 1, end, _model.GetLineMaxColumn(end))` (raises nothing: the lines did not change), then `AddCore(thread, new Range(start, 1, end, maxColumn(end)))`.
- `Entry.RangeText` keeps the text of the **covering lines** (`GetRangeText(model, StartLineNumber, LineNumber)`) for both kinds: `ThreadLineTextChanged` and the suggestion
  block work on whole lines (README decision 19). No change in `OnContentChanged`.
- `SyncLines` computes, for each entry, `start` / `end` exactly as today, then the columns:
  - whole lines: `startColumn = 1`, `endColumn = _model.GetLineMaxColumn(end)`;
  - exact text: `startColumn = Clamp(range?.StartColumn ?? thread.StartColumn, 1, maxColumn(start))`, `endColumn = Clamp(range?.EndColumn ?? thread.EndColumn, 1, maxColumn(end))`,
    and when `start == end && endColumn < startColumn`, `endColumn = startColumn`.
  When any of the four values differs from the thread: `thread.SetRange(start, startColumn, end, endColumn)` and the entry goes to a `changed` list; when `start` or `end`
  differs it also goes to `moved` (zone layout and `ThreadMoved`, unchanged). Call `UpdateOptions(changed)` (instead of `moved`): P12-19 needs it for a range that empties.
- `ThreadMoved` XML doc: "Raised once per thread whose lines changed because of an edit (not for a change of columns only)." Class summary: "a tracked decoration per thread
  so it follows its lines or its exact text".
- `OptionsFor` is unchanged in this card (an exact thread gets `AnchorOptions`, no class).
- Do not add special cases for edits: the decoration's stickiness gives README decision 17; the tests pin it.

## Tests

Model for the exact tests: `"let value = 1;\nsecond line\nthird"`, thread on `value` = `(1, 5) → (1, 10)`.

- [ ] `AddText_DecorationIsTheExactRange` (`GetDecorationRange(GetDecorationId(t))` equals the range), `AddText_ZoneAfterLastLine`, `AddText_IsNotWholeLines`
- [ ] `AddText_LineOutOfDocument_Throws`, `AddText_ColumnPastLineEnd_IsClamped`, `AddText_Null_Throws`
- [ ] `AddLines_EndColumnIsMaxColumnOfLastLine` (and `StartColumn == 1`)
- [ ] `Text_TypeBeforeOnSameLine_ColumnsShift_NoEvents` (insert `"x"` at `(1, 1)`: columns 6–11; no `ThreadMoved`, no `CommentThread.Changed`)
- [ ] `Text_TypeAtStartEdge_DoesNotGrow` (insert at `(1, 5)`: columns 6–11), `Text_TypeAtEndEdge_DoesNotGrow` (insert at `(1, 10)`: columns 5–10), `Text_TypeInside_Grows`
  (insert at `(1, 7)`: columns 5–11)
- [ ] `Text_DeleteAllItsText_BecomesEmpty_ThreadKept` (delete `(1, 5) → (1, 10)`: `StartColumn == EndColumn == 5`, the thread is still in `Threads`)
- [ ] `Text_EnterInside_BecomesMultiLine_ZoneMoves` (insert `"\n"` at `(1, 7)`: lines 1–2, columns 5 → 4, zone `AfterLineNumber == 2`, `ThreadMoved` once)
- [ ] `Text_EnterAtStartOfLine_MovesDown` (insert `"\n"` at `(1, 1)`: lines 2–2, same columns)
- [ ] `Text_ReplaceWholeDocument_ClampsLinesAndColumns` (`SetValue("ab")`: line 1, columns within `1..3`, `StartColumn <= EndColumn`)
- [ ] `Lines_TypeAtEndOfLastLine_EndColumnFollows_NoEvents` (whole-line thread on line 2: type at its end; `EndColumn` is the new max column; no `Changed`)
- [ ] `RangeText_TextThread_EditOutsideTheTextOnItsLine_RaisesLineTextChanged` (edit `(1, 13)`: `ThreadLineTextChanged` raised — the covering line changed)
- [ ] All existing `CommentThreadManagerTests` unchanged and green

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] No public API change
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The highlight (P12-19), the controller and the command (P12-20), any WinUI change.

## Suggested commit message

`feat(view): anchor comment threads to the exact selected text`
