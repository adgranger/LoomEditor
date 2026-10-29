# P11-15 — Share the Markdown → XAML builder between the hover and the comment widget

- **Status:** todo
- **Depends on:** P11-02, P11-14
- **Size:** M

## Goal

The code that turns a `MarkdownDocument` into XAML rows moves out of `HoverWidget` into `MarkdownElementBuilder`, which the hover **and** the comment widget use. It also draws the new inline flags (strikethrough, underline) and blockquotes, and lets the caller draw a code block itself (the suggestion block, P11-19). The hover looks and behaves exactly as before.

## Sources

Sources: none (specified in this card; ADR 0014 decision 5). Read first, completely: `src/LoomEditor.WinUI/Controls/Hover/HoverWidget.cs`, `HoverWidget.Markdown.cs`, `HoverWidget.Theme.cs`, `HoverPresenter.cs`, the completion documentation view in `Controls/Suggest/` if it also builds Markdown (grep `MarkdownParser`), and `src/LoomEditor.View/Markdown/MarkdownDocument.cs` (including `MarkdownQuote` and the new inline flags of P11-02).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Markdown/MarkdownElementBuilder.cs`, `MarkdownElementBuilder.Inlines.cs`, `MarkdownTheme.cs` — create
- `src/LoomEditor.WinUI/Controls/Hover/HoverWidget.Markdown.cs`, `HoverWidget.cs`, `HoverWidget.Theme.cs` — change: use the builder; keep marker rows where they are
- `src/LoomEditor.WinUI/Controls/Suggest/*` — change only if it duplicates the same code (then use the builder too)
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-15"

## Design decisions (already made — do not change)

- `internal sealed class MarkdownElementBuilder` with `MarkdownTheme` (`Foreground`, `LinkBrush`, `CodeBackground`, `QuoteBar`, `Separator` brushes — what `HoverWidget` already uses, as a small class) and `IReadOnlyList<UIElement> Build(MarkdownDocument document, double fontSize, FontFamily codeFont, MarkdownBuildOptions? options = null)`.
- `MarkdownBuildOptions`: `Func<MarkdownCodeBlock, UIElement?>? CustomCodeBlock` — called for **every** code block; when it returns an element that element is used instead of the default code paragraph. The hover passes none.
- **Same output for the hover:** the rows, fonts, margins, bullet glyph, heading size formula and link handling of `HoverWidget.Markdown.cs` are moved as they are. If the move would change a pixel, **stop** and report; do not "improve" the hover in this card. The marker row (`MarkerRow`) stays in the hover.
- **New inline rendering** (visible only with the extended parser): strikethrough → `Run.TextDecorations = TextDecorations.Strikethrough`, underline → `TextDecorations.Underline`. Check the exact enum / property in the Windows App SDK version the project uses (build will tell); both may combine with bold / italic / code / link.
- **Blockquote** → a `Border` with a 3 px left border in `QuoteBar`, 8 px left padding, dimmed foreground (opacity 0.8), whose child is a `StackPanel` of the rows built recursively from `MarkdownQuote.Blocks`.
- Links are drawn as `Hyperlink`-styled runs **as the hover does today**; nothing opens them (ADR 0014 decision 5).
- No `x:Bind`, no reflection. The builder holds no per-call state (safe to reuse); UI thread only.

## Tests

- [ ] Automated: none (no WinUI test project); the build and the existing tests prove nothing broke at compile level.
- [ ] Manual (section "P11-15", Gallery with a language server hover or the marker sample of phase 8): the hover popup of a marker, of a Markdown hover with code, list, heading, link and bold text, and the completion documentation look the same as before the card (compare with a screenshot taken **before** starting the card: take it first and keep it out of the repo).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (`HoverControllerTests`, `HoverWidget`-adjacent tests, Markdown tests unchanged)
- [ ] `HoverWidget.Markdown.cs` no longer contains inline / block building code (only `MarkerRow` and calls to the builder)
- [ ] The manual section is written; the card result says it was not run by the agent
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Any comment widget code (P11-16), images, tables, selectable text in the hover, opening links.

## Suggested commit message

`refactor(winui): extract the Markdown element builder from the hover widget`
