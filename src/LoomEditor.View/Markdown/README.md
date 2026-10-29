# Markdown subset

`MarkdownParser.Parse` turns the Markdown that language servers send into a small immutable `MarkdownDocument` that the
hover popup and the completion documentation draw without an HTML engine. It is specified by task card P8-07, not
ported from Monaco (which renders with `marked`).

## Supported

- Blocks: paragraphs (soft break = space, two trailing spaces or a backslash = hard break), fenced code (``` and `~~~`,
  language = first word of the info string), ATX headings (`#` to `######`), flat lists (`-`, `*`, `+`, `1.`, `1)` with
  continuation lines), horizontal rules.
- Inlines: inline code, `**bold**` / `__bold__`, `*italic*` / `_italic_`, `***both***` (nesting inherits flags),
  `[text](url "title")` links (the title is ignored), backslash escapes, the entities `&nbsp; &amp; &lt; &gt; &quot;`.

## Not supported (shown as text)

No HTML (except the two underline tags in extended mode), no images (`![x](y)` is the text `!` followed by a link), no tables, no nested lists, no blockquotes by default (`> x` is a
paragraph starting with `>`), no reference links, no autolinks.

## Notes

`MarkdownString.FromText` output round-trips: every blank becomes `&nbsp;` and syntax tokens are escaped, so
`FromText("1. item")` and `FromText("- dash")` stay paragraphs (the text has U+00A0 instead of spaces).

## Extended syntax (comments)

`MarkdownParser.Parse(markdown, MarkdownParserOptions.Extended)` (used by the comment widget, ADR 0014) adds:

- `> quote` blocks: consecutive lines starting with `>` (up to 3 spaces of indent); the marker and one space are
  removed and the rest is parsed recursively (lists, fences, nested quotes up to depth 3; deeper `>` stays text). A
  blank line ends the quote. `MarkdownQuote` holds the blocks.
- `~~strike~~` (`MarkdownInline.Strikethrough`); an unclosed `~~` is literal.
- `<u>…</u>` and `<ins>…</ins>`, ASCII case-insensitive (`MarkdownInline.Underline`); unmatched tags and any other
  HTML stay literal.

Without the option the output is unchanged, so hovers are not affected.
