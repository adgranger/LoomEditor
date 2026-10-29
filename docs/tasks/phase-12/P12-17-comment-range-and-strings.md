# P12-17 — `CommentRange`: a selection becomes whole lines or exact text; strings for the selected text

- **Status:** todo
- **Depends on:** P12-16
- **Size:** S

**Recommended model:** Sonnet

## Goal

One pure helper turns a selection or a `Range` into the anchor of a thread — whole lines or the exact text — following README decision 15. `CommentStrings` gains the
three strings step E needs. Nothing uses them yet (P12-20 and later do).

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decisions 15 and 22). Read first: `src/LoomEditor.View/Comments/CommentLineRange.cs` and
`tests/LoomEditor.View.Tests/Comments/CommentLineRangeTests.cs` (the rule this card generalizes; both stay until P12-23), `src/LoomEditor.Core/Model/TextModel.Validation.cs`
(`ValidateRange`), `src/LoomEditor.Core/Text/Selection.cs` (`Range`, `IsEmpty`), `src/LoomEditor.View/Comments/CommentStrings.cs`,
`tests/LoomEditor.View.Tests/Comments/CommentStringsTests.cs`, `tests/LoomEditor.View.Tests/Comments/SuggestionEditTests.cs` (how a `TextModel` is built in a test).

## Files to create or change

- `src/LoomEditor.View/Comments/CommentRange.cs` — create
- `src/LoomEditor.View/Comments/CommentStrings.cs` — change
- `tests/LoomEditor.View.Tests/Comments/CommentRangeTests.cs` — create
- `tests/LoomEditor.View.Tests/Comments/CommentStringsTests.cs` — change

## Design decisions (already made — do not change)

- `internal readonly record struct CommentRange(int StartLineNumber, int LineNumber, Range? TextRange)` (namespace `LoomEditor.View.Comments`, summary "Where a comment
  thread is anchored: whole lines (<see cref=\"TextRange\"/> is null) or the exact text.", `<remarks>Not a port. Threading: pure; FromRange and FromSelection read a
  TextModel: UI thread.</remarks>`). Members:
  - `public bool IsWholeLines => TextRange is null;`
  - `public static CommentRange Lines(int startLineNumber, int lineNumber) => new(startLineNumber, lineNumber, null);` (`Guard.Assert(1 <= start && start <= line)`)
  - `public static CommentRange FromSelection(TextModel model, Selection selection) => FromRange(model, selection.Range);` (direction does not matter; an empty selection
    is its position, so it gives the cursor line)
  - `public static CommentRange FromRange(TextModel model, Range range)`, exactly README decision 15:
    ```csharp
    Range r = model.ValidateRange(range);
    int start = r.StartLineNumber, end = r.EndLineNumber, endColumn = r.EndColumn;
    if (endColumn == 1 && end > start) { end--; endColumn = model.GetLineMaxColumn(end); }
    if (start == end && endColumn <= r.StartColumn) { return Lines(start, start); }
    if (r.StartColumn == 1 && endColumn == model.GetLineMaxColumn(end)) { return Lines(start, end); }
    return new(start, end, new Range(start, r.StartColumn, end, endColumn));
    ```
    (with braces on separate lines as the code rules require). XML doc: the five steps in one sentence each.
- New `CommentStrings` members (README decision 22), `string.Create(CultureInfo.InvariantCulture, …)` like the existing ones:
  - `public const int MaxCommentedTextLength = 80;`
  - `public static string CommentedText(string text)`: replace `"\r\n"`, then `'\n'` by `' '`; when longer than `MaxCommentedTextLength`, keep the first 80 characters and
    append `"…"`; return `"Commented text: " + that`.
  - `public static string AddCommentOnSelectedText(int start, int end)` → `"Add a comment on the selected text on line {start}"` when `start == end`, else
    `"Add a comment on the selected text on lines {start} to {end}"`.
  - `public static string ThreadAddedOnText(int start, int end)` → `"Comment thread added on the selected text on line {start}"` / `"… on lines {start} to {end}"`.
- No existing string changes. `CommentLineRange` is **not** touched.

## Tests

- [ ] `FromSelection_Empty_CursorLine`, `FromSelection_Word_ExactText` (`"let value = 1;"`, select `value`: `TextRange == (1, 5, 1, 10)`, lines 1–1)
- [ ] `FromSelection_RightToLeft_SameAsLeftToRight`
- [ ] `FromSelection_ShiftDownLines_WholeLines` (`(2, 1) → (4, 1)` gives `Lines(2, 3)`, same as `CommentLineRange`), `FromSelection_TwoLinesEndingAtColumnOne_IsOneLine`
- [ ] `FromSelection_FullLineDrag_WholeLine` (`(3, 1) → (3, maxColumn)`), `FromSelection_FullLinesDrag_WholeLines` (`(2, 1) → (4, maxColumn(4))`)
- [ ] `FromSelection_MidLineToColumnOneOfNextLine_EndsAtPreviousLineEnd` (`(2, 3) → (3, 1)` gives exact `(2, 3, 2, maxColumn(2))`)
- [ ] `FromSelection_OnlyTheLineBreak_IsWholeLine` (`(2, maxColumn(2)) → (3, 1)` gives `Lines(2, 2)`)
- [ ] `FromSelection_MultiLinePartial_ExactText` (`(2, 3) → (4, 2)`)
- [ ] `FromRange_ColumnPastLineEnd_IsClamped` (`(1, 5, 1, 999)` on `"abcdefgh"` gives exact `(1, 5, 1, 9)`), `FromRange_EmptyLine_IsWholeLine`
- [ ] Strings: `CommentedText_Short_Prefixed`, `CommentedText_LineBreaks_BecomeSpaces`, `CommentedText_Long_CutWithEllipsis` (81 chars → 80 + `…`), `AddCommentOnSelectedText_OneLine`,
  `AddCommentOnSelectedText_Lines`, `ThreadAddedOnText_OneLine`, `ThreadAddedOnText_Lines`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] No public API change
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Using `CommentRange` (P12-20 command, P12-21 public API, P12-22 gutter), deleting `CommentLineRange` (P12-23), multi-cursor selections.

## Suggested commit message

`feat(view): turn a selection into a whole-line or exact-text comment anchor`
