# P12-05 — The manager anchors a thread to a range of lines

- **Status:** todo
- **Depends on:** P12-04
- **Size:** M

**Recommended model:** Sonnet

## Goal

`CommentThreadManager.Add(start, end, title)` creates a thread whose decoration spans lines `start … end`; edits move, grow and shrink the range; the `Widget` zone always
follows the **last** line; the "line text changed" event watches the whole range. A single-line thread behaves exactly as in phase 11.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 2, 3 and 6). Read first: `src/LoomEditor.View/Comments/CommentThreadManager.cs` (all of it:
`Add`, `SyncLines`, `OnContentChanged`, `Entry`, the `_busy` re-entrancy guard), `tests/LoomEditor.View.Tests/Comments/CommentThreadManagerTests.cs` (how the model and
`ViewZones` are built; copy it), `src/LoomEditor.View/Comments/SuggestionEdit.cs` (`GetRangeText`, P12-02), `docs/tasks/phase-11/P11-09-thread-manager-and-widget-zone.md`
(why the zone's `AfterLineNumber` must follow the decoration).

## Files to create or change

- `src/LoomEditor.View/Comments/CommentThreadManager.cs` — change
- `tests/LoomEditor.View.Tests/Comments/CommentThreadManagerTests.cs` — change (add the range tests; keep every existing test)

## Design decisions (already made — do not change)

- New `CommentThread Add(int startLineNumber, int endLineNumber, string? title = null)`: `ArgumentOutOfRangeException` unless `1 <= start <= LineCount` (param
  `startLineNumber`) and `start <= end <= LineCount` (param `endLineNumber`). Creates `new CommentThread(id, start, end, title)` (P12-01), one decoration over
  `(start, 1)` → `(end, GetLineMaxColumn(end))` with the existing `AnchorOptions`, one zone with `AfterLineNumber = end` (all other zone fields unchanged).
- The existing `Add(int lineNumber, string? title = null)` becomes `=> Add(lineNumber, lineNumber, title)`.
- `Entry.LineText` is renamed `RangeText` and holds `SuggestionEdit.GetRangeText(model, start, end)`.
- `SyncLines`: for each entry read `range = _model.GetDecorationRange(id)`; `start = Clamp(range?.StartLineNumber ?? thread.StartLineNumber, 1, LineCount)`,
  `end = Clamp(range?.EndLineNumber ?? thread.LineNumber, start, LineCount)`. When `start` or `end` differs from the thread: `thread.SetLines(start, end)`; when `end` changed,
  also set `zone.AfterLineNumber = end` and lay it out (in the existing single `_zones.Change` for all moved zones). `ThreadMoved` is raised once per thread whose start or end
  changed (XML doc: "Raised once per thread whose lines changed because of an edit.").
- `OnContentChanged`: compares `GetRangeText(model, thread.StartLineNumber, thread.LineNumber)` (both clamped) to `RangeText`; raises `ThreadLineTextChanged` when it differs
  (XML doc: "Raised when the text of a thread's lines changed."). The event keeps its name (WinUI uses it).
- Flush (`SetValue`): both ends clamp; a range past the new end becomes a range ending at the last line (or a single line). Never deleted.
- Behavior of edits is the decoration's (`NeverGrowsWhenTypingAtEdges`), see README decision 6; the tests below pin it. Do not add any special case for it.
- No highlight in this card (P12-06).

## Tests

- [ ] `AddRange_CreatesZoneAfterLastLine`, `AddRange_EndBeforeStart_Throws`, `AddRange_EndPastLastLine_Throws`, `AddRange_StartEqualsEnd_SameAsSingleLine`
- [ ] `Range_InsertLineAbove_BothEndsMoveDown`, `Range_InsertLineInside_GrowsAndZoneMoves`, `Range_DeleteLineInside_ShrinksAndZoneMoves`
- [ ] `Range_EnterAtStartOfFirstLine_MovesWholeRange`, `Range_EnterAtEndOfLastLine_DoesNotGrow`, `Range_TypeAtEdges_DoesNotGrow`
- [ ] `Range_DeleteWholeRange_CollapsesToOneLine`, `Range_ReplaceWholeDocument_ClampsBothEnds`
- [ ] `Range_DeleteFirstLineOfRange_ShrinksFromTop` (range 3–5, delete `(3, 1)` → `(4, 1)`: range 3–4, `ThreadMoved` raised once), `Range_Moved_ThenRecompute_ZoneStaysUnderLastLine`
- [ ] `RangeTextChanged_EditInsideRange_Raised`, `RangeTextChanged_EditOutsideRange_NotRaised`
- [ ] All existing `CommentThreadManagerTests` unchanged and green

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The highlight (P12-06), the controller (P12-07), any WinUI change.

## Suggested commit message

`feat(view): anchor comment threads to a range of lines`
