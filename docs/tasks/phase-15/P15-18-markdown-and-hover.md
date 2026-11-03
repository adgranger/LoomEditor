# P15-18 — Markdown rendering and the hover widget

- **Status:** todo
- **Depends on:** P15-17
- **Size:** M

**Recommended model:** Sonnet

## Goal

Hovering a symbol with a hover provider shows the hover widget with its Markdown (headings, bold, italic, code, code blocks, links) in the Avalonia `LoomEditor`.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/Markdown/MarkdownElementBuilder.cs`, `MarkdownElementBuilder.Inlines.cs`, `MarkdownTheme.cs`,
`Controls/Hover/HoverPresenter.cs`, `HoverWidget.cs`, `HoverWidget.Markdown.cs`, `HoverWidget.Theme.cs`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/Markdown/` and `Controls/Hover/` — the same 7 files (twins)
- `tests/LoomEditor.Avalonia.Tests/Controls/MarkdownElementBuilderTests.cs`, `HoverWidgetTests.cs` — create
- `docs/testing/phase-15-manual-tests.md` — section "P15-18"

## Design decisions (already made — do not change)

- `RichTextBlock` → `SelectableTextBlock` per paragraph, inside a `StackPanel`; WinUI `Paragraph` → one `SelectableTextBlock`; `Run` / `Bold` / `Italic` / `Span` / `LineBreak` → the
  Avalonia inlines of the same names. Inline code: `Run` with the code font and the theme's code background; code blocks: a `Border` with a `SelectableTextBlock`.
- Links: WinUI `Hyperlink` → an `InlineUIContainer` holding a `HyperlinkButton` with the same click handler (opens through the same host callback). If the pinned Avalonia has a
  hyperlink inline, use it instead and say so in the Result.
- Pointer delay and hide rules: copied from `HoverPresenter` unchanged (`DispatcherTimer`).

## Tests

- [ ] New: `Markdown_BoldItalicCode_BuildsInlines`; `Markdown_CodeBlock_BuildsBorder`; `Markdown_Link_IsClickable`; `Hover_ProviderResult_ShowsWidgetNearPosition`;
  `Hover_PointerLeaves_HidesAfterDelay`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Syntax-highlighted code blocks if WinUI has none.

## Suggested commit message

`feat(avalonia): Markdown rendering and the hover widget`
