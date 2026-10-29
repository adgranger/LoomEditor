# P11-06 — Suggestion blocks (1/2): extraction and the model edit

- **Status:** todo
- **Depends on:** P11-02
- **Size:** M

## Goal

From a comment body, `SuggestionExtractor` lists its ` ```suggestion ` blocks; `SuggestionEdit` turns one of them into the `TextEdit` that replaces a model line, and says when a suggestion is stale. Pure logic plus a `TextModel` in the tests.

## Sources

Sources: none (specified in this card; ADR 0014 decision 6). Read first: `src/LoomEditor.Core/Text/TextEdit.cs`, `src/LoomEditor.Core/Model/TextModel.Edits.cs` (`ApplyEdits`), `TextModel.Eol` (the model's end of line sequence, `TextModel.cs`), `tests/LoomEditor.Core.Tests` or `View.Tests` helpers that build a `TextModel` from a string.

## Files to create or change

- `src/LoomEditor.View/Comments/SuggestionBlock.cs`, `SuggestionExtractor.cs`, `SuggestionEdit.cs` — create
- `tests/LoomEditor.View.Tests/Comments/SuggestionExtractorTests.cs`, `SuggestionEditTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.View.Comments`, `internal`, `<remarks>Threading: pure and thread-safe</remarks>` (`SuggestionEdit` reads a `TextModel`: UI thread).
- `sealed record SuggestionBlock(int Index, string Text)`: `Index` is the 0-based order among the **suggestion** blocks of the body; `Text` is the code of the block exactly as the parser returns it (no trailing newline). `IReadOnlyList<string> Lines` is `Text.Split('\n')`, or an **empty** list when `Text` is empty. `IsDeletion => Text.Length == 0`.
- `static IReadOnlyList<SuggestionBlock> SuggestionExtractor.Extract(string? body)`: `MarkdownParser.Parse(body, MarkdownParserOptions.Extended)`, then the **top-level** `MarkdownCodeBlock` blocks whose `Language` equals `"suggestion"` (ordinal, case-sensitive: that is GitHub's syntax). Suggestions inside a quote are ignored. `null` / empty → empty list. A block that is not closed is what the parser makes of it (no special rule). `\r` in the body is normalized to `\n` by the parser already.
- **An empty block means "delete the line"** (GitHub does the same). Replacing a line by a *blank* line is therefore not expressible in phase 11 (documented in the XML doc and in the README of the folder, a known limit).
- `static TextEdit? SuggestionEdit.Create(TextModel model, int lineNumber, SuggestionBlock block)` — `null` when `lineNumber` is outside `1..LineCount`. The text uses `model.Eol` between lines.
  - Non-deletion: the range is the whole content of the line (`lineNumber, 1` → `lineNumber, maxColumn`), the text is `string.Join(eol, block.Lines)`.
  - Deletion: if a next line exists, the range is `(lineNumber, 1)` → `(lineNumber + 1, 1)` with empty text; else if `lineNumber > 1`, the range is the end of the previous line → the end of this line; else (the only line) the range is the whole line, empty text.
- `static bool SuggestionEdit.IsStale(TextModel model, int lineNumber, string expectedLineContent)` — `true` when the line no longer exists or its content differs (ordinal).
- `static bool SuggestionEdit.IsNoOp(TextModel model, int lineNumber, SuggestionBlock block)` — the replacement would not change the model.
- The apply path (undo stop, read-only check, event) is **P11-10**, not here.

## Tests

- [ ] Extractor: `Extract_TwoSuggestionsAndOneCsharpFence_ReturnsTwoWithIndexes`, `Extract_Empty_ReturnsEmpty`, `Extract_NullBody_ReturnsEmpty`, `Extract_EmptyBlock_IsDeletion`, `Extract_MultiLineBlock_SplitsLines`, `Extract_InsideQuote_Ignored`, `Extract_WrongCase_Ignored`, `Extract_TildeFence_Works`
- [ ] Edit (against a real `TextModel`): `Create_SingleLine_ReplacesLineContent`, `Create_MultiLine_UsesModelEol` (CRLF model gives `\r\n`), `Create_LineOutOfRange_ReturnsNull`, `Create_Deletion_MiddleLine_RemovesLineAndEol`, `Create_Deletion_LastLine_RemovesPreviousEol`, `Create_Deletion_OnlyLine_EmptiesDocument`, `Create_AppliedEdit_GivesExpectedText` (apply with `ApplyEdits` and compare the full text, for LF and CRLF), `IsStale_ContentChanged_ReturnsTrue`, `IsStale_SameContent_ReturnsFalse`, `IsNoOp_SameText_ReturnsTrue`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The diff rows (P11-07), applying through the editor with undo (P11-10), suggestions that replace a range of lines.

## Suggested commit message

`feat(view): extract suggestion blocks and build the edit that applies one`
