# P8-07 — A Markdown subset parser for hover and documentation text

- **Status:** done
- **Depends on:** P8-06
- **Size:** M

## Goal

`MarkdownParser.Parse(string markdown)` turns the Markdown that language servers send (paragraphs, fenced code, inline code, bold, italic, links, lists, headings) into a small immutable document that a UI can draw without any HTML engine.
The hover popup (P8-11) and the completion documentation use it.

## Monaco sources

Sources: none (specified in this card). Monaco renders Markdown with `marked` into HTML (`markdownRenderer.ts`); LoomEditor has no HTML engine, so this is a deliberately small, pure parser. Its input is what `MarkdownString.FromText` produces
(P8-01: the escapes of `escapeMarkdownSyntaxTokens`) and what servers like `csharp-ls` send (code fences, inline code, bold, italic, links, lists).

## Files to create or change

- `src/LoomEditor.View/Markdown/MarkdownDocument.cs` — create (the record types)
- `src/LoomEditor.View/Markdown/MarkdownParser.cs` — create (blocks); `src/LoomEditor.View/Markdown/MarkdownInlineParser.cs` — create (inlines)
- `src/LoomEditor.View/Markdown/README.md` — create
- `tests/LoomEditor.View.Tests/Markdown/MarkdownParserTests.cs`, `MarkdownInlineParserTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.View.Markdown`, everything `internal`, XML doc, `<remarks>Threading: pure and thread-safe.</remarks>`.
- **Types** (`MarkdownDocument.cs`):
  `internal sealed record MarkdownDocument(IReadOnlyList<MarkdownBlock> Blocks)`;
  `internal abstract record MarkdownBlock;` and the sealed records `MarkdownParagraph(IReadOnlyList<MarkdownInline> Inlines)`, `MarkdownHeading(int Level, IReadOnlyList<MarkdownInline> Inlines)`, `MarkdownCodeBlock(string? Language, string Code)`,
  `MarkdownList(bool Ordered, IReadOnlyList<IReadOnlyList<MarkdownInline>> Items)`, `MarkdownRule`;
  `internal sealed record MarkdownInline(string Text, bool Bold = false, bool Italic = false, bool Code = false, string? LinkUrl = null, bool IsLineBreak = false)` (a `IsLineBreak` inline has `Text == ""`).
  Lists of inlines are never empty-text runs: adjacent inlines with the same flags and url are **merged** into one (the parser guarantees it; tests compare structures).
- **Input**: `\r\n` and `\r` are normalized to `\n` first. Empty / null input → a document with no blocks (`ArgumentNullException` for null is **not** thrown: `Parse(null)` is treated as empty; the parameter is `string?`).
- **Blocks**, line by line, first matching rule wins (a "block start" below means any of rules 1, 3, 4, 5 on a line):
  1. **Fenced code**: a line matching `^ {0,3}(`{3,}|~{3,}) *([^\s`]*)` opens a fence (the info string is only its first word: `Language` = that word or `null` when empty); lines up to (not including) the first line that, after at most 3 spaces, consists only of the same fence character repeated at least as many times as the opening, are the code (joined with `\n`, no trailing newline, **not** trimmed, no inline parsing);
     an unclosed fence ends at the end of the input.
  2. A **blank line** (empty or only spaces / tabs) ends the current paragraph or list.
  3. **Rule**: `^ {0,3}([-*_])( *\1){2,} *$` → `MarkdownRule` (checked **before** lists, so `---` and `***` are rules).
  4. **Heading**: `^ {0,3}(#{1,6}) +(.+?) *$` → `MarkdownHeading(level, inlines of the text)`; a trailing run of `#` is **not** removed.
  5. **List item**: `^ {0,3}([-*+]|\d{1,9}[.)]) +(.*)$` starts a list (or adds an item to the current list; the list is `Ordered` when the **first** item's marker is digits; a different marker kind does not start a new list). The lines that follow, which are not blank and not a block start, are **continuation** lines of the current item: appended to the item text after one space.
  6. Otherwise a **paragraph**: consecutive non-blank lines that are not block starts, joined with `\n`; the inlines parser turns a `\n` into a space (soft break), except that a line ending with two or more spaces, or with a backslash, is a hard break (`IsLineBreak` inline; the backslash is removed).
- **Inlines** (`MarkdownInlineParser.Parse(string text) → IReadOnlyList<MarkdownInline>`), scanning left to right:
  1. `\` + one of `` \ ` * _ { } [ ] ( ) # + - . ! ~ > `` → that character as literal text; `\` + `\n` → a line break; any other `\` is a literal backslash.
  2. Entities `&nbsp;` → U+00A0, `&amp;` → `&`, `&lt;` → `<`, `&gt;` → `>`, `&quot;` → `"`; any other `&` is literal.
  3. **Code span**: a run of `n` backticks opens it; it closes at the next run of **exactly** `n` backticks; the content is taken literally, with **one** leading and one trailing space removed if both exist and the content is not only spaces; no closing run → the backticks are literal text.
  4. **Emphasis**: `**x**` and `__x__` → `Bold`; `*x*` and `_x_` → `Italic`; `***x***` → both. A delimiter run of length 2 closes at the next unescaped run of the same two characters, a run of length 1 at the next unescaped single one; the content must be non-empty. For `*` the content must not start or end with a space.
     For `_`, the opening must be at the start of the text or preceded by a non-alphanumeric character and the closing at the end of the text or followed by a non-alphanumeric character (so `snake_case_name` is plain text). The content is parsed recursively and **inherits** the flags (bold inside italic gives both). An opener without a matching closer is literal text.
  5. **Link**: `[text](url)` or `[text](url "title")`; `text` is parsed recursively (flags inherited, `LinkUrl` set on every inline it produces); `url` is the text up to the first space or `)` (a `<…>` url is unwrapped); the title is ignored; if the shape does not match, `[` is literal.
  6. Everything else is text. A `\n` inside the text (soft break) becomes one space.
- No HTML, no images (`![x](y)` is literal text `!` then a link), no tables, no nested lists, no blockquotes (`> x` is a paragraph starting with `>`), no reference links, no autolinks: documented in the README (the hover shows them as text).
- `MarkdownDocument.ToPlainText()` (needed by P8-11 and the screen reader): the text of every block, paragraphs and headings as their inline texts concatenated (line breaks as `\n`), code blocks as their code, list items as `- text` / `1. text` lines, rules as an empty line;
  blocks separated by a blank line (`\n\n`). Tests below.

## Tests

- [x] New (`MarkdownParserTests`; helpers in the test file: `P(params MarkdownInline[])`, `T(text)`, `B(text)`, `I(text)`, `C(code)`, `L(text, url)`, `Doc(params MarkdownBlock[])`; compare with `Assert.Equal` on the record structure (use a small recursive formatter to a string for readable failures)):
      `Empty_NoBlocks`, `Null_NoBlocks`, `SingleParagraph`, `TwoParagraphs_BlankLine`, `SoftBreak_BecomesSpace` (`"a\nb"` → `T("a b")`), `HardBreak_TwoSpaces` and `HardBreak_Backslash`, `Crlf_Normalized`,
      `FencedCode_WithLanguage` (```` ```csharp\nint x;\n``` ````), `FencedCode_NoLanguage_NullLanguage`, `FencedCode_Unclosed_RunsToEnd`, `FencedCode_KeepsIndentationAndBlankLines`, `FencedCode_TildeFence`, `FencedCode_LongerClosingFenceCloses`, `FencedCode_ShorterInnerFenceDoesNotClose`,
      `Heading_Levels` (`#` … `######`, 7 `#` is a paragraph), `Heading_InlineParsed`, `Rule_ThreeDashes`, `Rule_BeforeList` (`---` after a paragraph is a rule, not a list), `List_Unordered`, `List_Ordered`, `List_ContinuationLine`, `List_EndsAtBlankLine`, `List_MixedMarkers_StayOneList`,
      `ParagraphThenCode_NoBlankLine` (a fence interrupts a paragraph), `Blockquote_IsAParagraph`, `Html_IsLiteralText` (`"<b>x</b>"` stays text)
- [x] New (`MarkdownInlineParserTests`): `Plain`, `InlineCode`, `InlineCode_DoubleBackticks` (``` ``a`b`` ``` → `C("a`b")`), `InlineCode_StripsOneSpace`, `InlineCode_Unclosed_Literal`, `Bold_Stars`, `Bold_Underscores`, `Italic_Star`, `Italic_Underscore`, `BoldItalic_TripleStar`, `Italic_InsideBold` (`**a *b* c**` → bold `a `, bold+italic `b`, bold ` c`),
      `Snake_Case_IsPlain`, `Star_WithSpaces_IsPlain` (`"a * b * c"`), `Emphasis_Unmatched_Literal`, `Link`, `Link_WithTitle`, `Link_TextHasEmphasis`, `Link_Malformed_Literal` (`"[a](b"`), `Link_AngleUrl`, `Escape_Tokens` (`"\\*a\\*"` → `T("*a*")`), `Escape_Backslash_Literal_WhenNotEscapable` (`"\\q"` → `T("\\q")`),
      `Entities` (`"a&nbsp;b &amp; &lt;"` → the text `a`, a no-break space (U+00A0), `b & <`), `Entity_Unknown_Literal`, `AdjacentSameStyle_Merged`, `FromText_RoundTrips` (for `"a*b_c"` and `"(x) [y] {z}"`: `Parse(MarkdownString.FromText(s).Value)` is one paragraph whose single inline text is `s`), `FromText_DashLine_StaysAParagraph` (`FromText("- dash")` is `"\-&nbsp;dash"`, which parses as a paragraph with the text `-`, U+00A0, `dash`),
      `FromText_NumberedLine_IsAList` (`FromText("1. item")` parses as an **ordered list with one item**: `FromText` does not escape `.`; a known limitation, documented in the README and in `MarkdownString.FromText`'s XML doc)
- [x] New (`MarkdownDocument.ToPlainText`): `ToPlainText_Paragraphs`, `ToPlainText_CodeBlockAndList`, `ToPlainText_LineBreak`, `ToPlainText_Empty`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `src/LoomEditor.View/Markdown/README.md` lists the supported subset and what is not supported (copy the "No HTML, no images…" decision)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing (P8-11), syntax highlighting of code blocks, images, tables, nested lists, HTML, links being clickable (a link is drawn as text with the link color; opening it is a follow-up).

## Suggested commit message

`feat(view): add a Markdown subset parser`
