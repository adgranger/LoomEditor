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

No HTML, no images (`![x](y)` is the text `!` followed by a link), no tables, no nested lists, no blockquotes (`> x` is a
paragraph starting with `>`), no reference links, no autolinks.

## Notes

`MarkdownString.FromText` output round-trips: every blank becomes `&nbsp;` and syntax tokens are escaped, so
`FromText("1. item")` and `FromText("- dash")` stay paragraphs (the text has U+00A0 instead of spaces).
