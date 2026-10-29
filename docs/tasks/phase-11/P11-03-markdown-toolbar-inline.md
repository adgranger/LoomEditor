# P11-03 — Markdown toolbar (1/2): inline formats as pure text edits

- **Status:** todo
- **Depends on:** P11-02
- **Size:** M

## Goal

`MarkdownToolbar.Apply(...)` turns "the user clicked **B** with this selection in this text" into the new text and selection, for bold, italic, underline, strikethrough, inline code and link. Pure string work: the WinUI text box only calls it (P11-14).

## Sources

Sources: none (specified in this card; ADR 0014). The behavior is that of the GitHub comment toolbar, simplified.

## Files to create or change

- `src/LoomEditor.View/Comments/MarkdownToolbarAction.cs` — create (enum, **all twelve** actions, see below)
- `src/LoomEditor.View/Comments/ToolbarSelection.cs`, `ToolbarEdit.cs` — create
- `src/LoomEditor.View/Comments/MarkdownToolbar.cs` (entry point) and `MarkdownToolbar.Inline.cs` — create
- `tests/LoomEditor.View.Tests/Comments/MarkdownToolbarInlineTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.View.Comments`, `internal`, pure and thread-safe (`<remarks>`).
- `enum MarkdownToolbarAction { Heading, Bold, Italic, Underline, Strikethrough, InlineCode, CodeBlock, Link, BulletList, NumberedList, Quote, Suggestion }` (the order of the toolbar).
- `readonly record struct ToolbarSelection(int Start, int End)` — 0-based UTF-16 offsets into the text, `0 <= Start <= End <= text.Length` (validated: `ArgumentOutOfRangeException`). `Length => End - Start`; `IsEmpty`.
- `readonly record struct ToolbarEdit(string Text, ToolbarSelection Selection)` — the **whole** new text and the new selection.
- `static ToolbarEdit MarkdownToolbar.Apply(MarkdownToolbarAction action, string text, ToolbarSelection selection, string? suggestionSeed = null)`. The text uses `\n` only (the widget converts WinUI's `\r`, P11-14). In this card the **block** actions (`Heading`, `CodeBlock`, `BulletList`, `NumberedList`, `Quote`, `Suggestion`) return `new ToolbarEdit(text, selection)` unchanged, with a `// P11-04` comment; P11-04 replaces that branch.
- **Markers:** bold `**`, italic `_`, underline `<u>` … `</u>`, strikethrough `~~`, inline code `` ` ``.
- **Wrap (non-empty selection):**
  1. Leading and trailing spaces / tabs of the selection stay **outside** the markers (`" a "` → `" **a** "`); a selection of only blanks is treated as empty.
  2. A selection that spans several lines is wrapped **line by line**: each non-blank line of the selection gets its own markers (its own leading / trailing blanks outside); blank lines are left alone. (Emphasis cannot cross a paragraph break.)
  3. The new selection covers the wrapped text including the markers when it spans one line, and the whole changed region otherwise.
- **Unwrap (toggle):** when the (trimmed) selection is already wrapped — either the markers sit just **outside** the selection (`**|a|**`) or the selection itself starts with the open and ends with the close marker and is longer than both — the markers are removed and the selection stays on the inner text. A single-line selection only; for several lines each line is toggled independently (all lines wrapped → unwrap all, else wrap those that are not).
- **Empty selection:** insert both markers and put the caret between them (`**|**`); if the caret is already between an empty marker pair, remove the pair (toggle).
- **Link:** non-empty selection → `[selection](url)` with the `url` placeholder text **selected**; when the selection starts with `http://` or `https://` → `[](selection)` with the caret between the brackets; empty selection → `[](url)` with the caret between the brackets. Link is not toggled (a second click wraps again).
- Never throws for valid input; never produces `\r`.

## Tests

- [ ] New: `Bold_Selection_Wraps`, `Bold_Selection_WrappedOutside_Unwraps`, `Bold_Selection_IncludesMarkers_Unwraps`, `Bold_EmptySelection_InsertsPairWithCaretBetween`, `Bold_EmptyPair_Removes`, `Bold_EdgeSpaces_StayOutside`, `Bold_OnlyBlanks_ActsAsEmpty`, `Bold_MultiLine_WrapsEachNonBlankLine`, `Bold_MultiLine_AllWrapped_UnwrapsAll`, `Italic_UsesUnderscore`, `Underline_UsesUTags`, `Strikethrough_UsesTildes`, `InlineCode_UsesBacktick`, `Link_Selection_SelectsUrlPlaceholder`, `Link_UrlSelection_PutsItInParens`, `Link_Empty_CaretInBrackets`, `Apply_InvalidSelection_Throws`, `Apply_BlockAction_ReturnsInputUnchanged` (temporary), `Apply_NeverProducesCarriageReturn`
- [ ] Round trip with the parser of P11-02: `Bold`, `Italic`, `Underline`, `Strikethrough`, `InlineCode` output, parsed by `MarkdownParser.Parse(text, MarkdownParserOptions.Extended)`, gives one inline with the matching flag (`RoundTrip_EveryInlineAction_ParsesToItsFlag`, a `[Theory]`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Block formats (P11-04), the buttons and the text box (P11-14), keyboard shortcuts, undo of a toolbar edit.

## Suggested commit message

`feat(view): add the Markdown toolbar inline formats`
