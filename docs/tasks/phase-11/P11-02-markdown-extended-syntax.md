# P11-02 — Markdown: blockquote, strikethrough and underline (opt-in)

- **Status:** done
- **Depends on:** P10-31
- **Size:** M

## Goal

`MarkdownParser.Parse(markdown, MarkdownParserOptions.Extended)` also understands `> quote`, `~~strike~~` and `<u>…</u>` / `<ins>…</ins>`, which the comment toolbar produces. Without the option the output is **identical** to today's (the hover is unchanged).

## Sources

Sources: none (specified in this card; ADR 0014 decision 5). Read first, completely: `src/LoomEditor.View/Markdown/README.md`, `MarkdownDocument.cs`, `MarkdownParser.cs`, `MarkdownInlineParser.cs` and their tests in `tests/LoomEditor.View.Tests/Markdown/`.

## Files to create or change

- `src/LoomEditor.View/Markdown/MarkdownParserOptions.cs` — create
- `src/LoomEditor.View/Markdown/MarkdownDocument.cs` — change: `MarkdownQuote` block; two new optional members of `MarkdownInline`; `ToPlainText` handles the quote
- `src/LoomEditor.View/Markdown/MarkdownParser.cs`, `MarkdownInlineParser.cs` — change
- `src/LoomEditor.View/Markdown/README.md` — change: a section "Extended syntax (comments)" and update "Not supported"
- `tests/LoomEditor.View.Tests/Markdown/MarkdownExtendedTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed record MarkdownParserOptions(bool Extended = false)` with `static readonly MarkdownParserOptions Default = new()` and `Extended = new(true)`. `Parse(string? markdown, MarkdownParserOptions? options = null)`; `MarkdownInlineParser.Parse` gets the same optional parameter. All existing callers keep compiling and behaving the same.
- `internal sealed record MarkdownQuote(IReadOnlyList<MarkdownBlock> Blocks) : MarkdownBlock` (only produced when `Extended`).
- `MarkdownInline` gets two **trailing** optional parameters: `bool Strikethrough = false, bool Underline = false` (existing positional uses stay valid).
- **Blockquote** (Extended only): a run of consecutive lines whose first non-space character is `>` (up to 3 spaces of indent) forms one quote; the marker and one following space are removed; the remaining lines are parsed **recursively** with the same options (so a quote can hold paragraphs, lists, a fenced block, another quote) with a depth limit of 3 (deeper `>` stays text). A blank line ends the quote. A `>` inside a fenced block is not a quote (the fence logic runs first, as today).
- **Strikethrough** (Extended only): `~~text~~`, same rules as the existing `**bold**` handling (nesting inherits flags; an unclosed `~~` is literal text; `~~` followed by a space or preceded by a word character is still a delimiter — keep it simple and **mirror what the bold code does**).
- **Underline** (Extended only): the exact tags `<u>` / `</u>` and `<ins>` / `</ins>`, ASCII case-insensitive. Content between them gets `Underline = true` and the other flags apply inside. An unclosed or unmatched tag is literal text. No other HTML tag is interpreted.
- With `Extended = false`, `> x`, `~~x~~` and `<u>x</u>` parse exactly as before (paragraph text): a test proves it by comparing against the same inputs through the default overload.
- `MarkdownDocument.ToPlainText` includes quoted text (each quoted block as it does for the unquoted ones).
- Hand written code only: **no new `Regex`** (the existing `[GeneratedRegex]` fence matcher stays).

## Tests

- [x] New: `Quote_SingleLine_IsQuoteWithParagraph`, `Quote_MultiLine_JoinsLines`, `Quote_WithList_ParsesListInside`, `Quote_Nested_UpToDepthThree`, `Quote_InsideFence_IsCode`, `Quote_EndsAtBlankLine`, `Quote_NotExtended_IsParagraph`, `Strikethrough_Simple`, `Strikethrough_InsideBold_InheritsFlags`, `Strikethrough_Unclosed_IsLiteral`, `Strikethrough_NotExtended_IsLiteral`, `Underline_U_And_Ins`, `Underline_CaseInsensitive`, `Underline_Unmatched_IsLiteral`, `Underline_OtherHtml_IsLiteral`, `Extended_SuggestionFence_ParsedAsCodeBlockWithLanguage` (` ```suggestion ` gives `MarkdownCodeBlock { Language: "suggestion" }`), `Default_SameOutputAsBefore` (a few inputs: every existing hover test still passes)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green (the existing Markdown and hover tests unchanged and passing)
- [x] README of the folder updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing (P11-15), nested lists, tables, task lists, images, autolinks, raw HTML other than the two underline tags.

## Suggested commit message

`feat(view): add blockquote, strikethrough and underline to the Markdown subset`

## Deviation

The static `MarkdownParserOptions.Extended` could not coexist with a bool property of the same name (CS0102), so the record's property is `IsExtended`; the `Default` / `Extended` statics are as specified.
