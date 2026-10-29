# P12-02 — Suggestion edit for a range of lines, the stale check and the range text

- **Status:** done
- **Depends on:** P12-01
- **Size:** S

**Recommended model:** Sonnet

## Goal

`SuggestionEdit` replaces (or deletes) lines `start … end` instead of one line, says when the range text changed, and gives the range text that every later card uses as
"the old text". The single-line members keep working and delegate.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) §1 "range text", decision 8). Read first: `src/LoomEditor.View/Comments/SuggestionEdit.cs`,
`SuggestionBlock.cs`, `tests/LoomEditor.View.Tests/Comments/SuggestionEditTests.cs` (copy how it builds a `TextModel` and applies an edit), `docs/tasks/phase-11/P11-06-suggestion-extract-and-edit.md`
(the single-line rules this card generalizes).

## Files to create or change

- `src/LoomEditor.View/Comments/SuggestionEdit.cs` — change
- `tests/LoomEditor.View.Tests/Comments/SuggestionEditTests.cs` — change (add the range tests; keep every existing test)

## Design decisions (already made — do not change)

- `static string GetRangeText(TextModel model, int startLineNumber, int endLineNumber)`: the contents of the lines joined with `"\n"` (`string.Join('\n', …)` over
  `model.GetLineContent(i)`), never the model's EOL. Precondition `1 <= start <= end <= LineCount` (`Guard.Assert`; callers clamp).
- `static TextEdit? Create(TextModel model, int startLineNumber, int endLineNumber, SuggestionBlock block)` — `null` when `start < 1`, `end > LineCount` or `start > end`.
  The text uses `model.Eol` between lines.
  - Non-deletion: range `(start, 1)` → `(end, GetLineMaxColumn(end))`, text `string.Join(model.Eol, block.Lines)`.
  - Deletion: if `end < LineCount`, `(start, 1)` → `(end + 1, 1)`, empty text; else if `start > 1`, `(start - 1, maxColumn(start - 1))` → `(end, maxColumn(end))`, empty;
    else (the range is the whole document) `(1, 1)` → `(end, maxColumn(end))`, empty.
- `static bool IsStale(TextModel model, int startLineNumber, int endLineNumber, string expectedRangeText)`: `true` when `start < 1`, `end > LineCount`, `start > end`, or
  `GetRangeText(...)` differs (ordinal) from `expectedRangeText`.
- `static bool IsNoOp(TextModel model, int startLineNumber, int endLineNumber, SuggestionBlock block)`: same rule as today's single-line `IsNoOp`, on the range edit.
- The existing single-line members become one-line delegations: `Create(model, line, block) => Create(model, line, line, block)`, `IsStale(model, line, text) =>
  IsStale(model, line, line, text)`, `IsNoOp(model, line, block) => IsNoOp(model, line, line, block)`. Their behavior must not change (the existing tests prove it).
- XML docs: say "lines <paramref name="startLineNumber"/> to <paramref name="endLineNumber"/>, inclusive"; remove "a model line" wording from the class summary.

## Tests

- [x] `GetRangeText_ThreeLines_JoinsWithLf` (on a CRLF model too: still `\n`), `GetRangeText_OneLine_IsLineContent`
- [x] `CreateRange_ReplacesAllLines` (apply with `ApplyEdits`, compare the full text, LF and CRLF), `CreateRange_FewerNewLines_ShrinksDocument`, `CreateRange_MoreNewLines_GrowsDocument`
- [x] `CreateRange_Deletion_MiddleRange_RemovesLinesAndOneEol`, `CreateRange_Deletion_RangeAtEnd_RemovesPreviousEol`, `CreateRange_Deletion_WholeDocument_EmptiesIt`
- [x] `CreateRange_EndBeforeStart_ReturnsNull`, `CreateRange_EndPastLastLine_ReturnsNull`
- [x] `IsStaleRange_LineInsideChanged_ReturnsTrue`, `IsStaleRange_SameText_ReturnsFalse`, `IsNoOpRange_SameLines_ReturnsTrue`
- [x] All existing `SuggestionEditTests` unchanged and green

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The diff rows (P12-03), the controller's apply path (P12-07), any change to `SuggestionExtractor` or `SuggestionBlock`.

## Suggested commit message

`feat(view): build suggestion edits that replace a range of lines`
