# P15-10 — Editor options: margin, line numbers, minimap, whitespace, word wrap, guides, sticky scroll and the small partials

- **Status:** todo
- **Depends on:** P15-09
- **Size:** M (~500 lines, almost all property declarations; allowed above 400 because it is mechanical)

**Recommended model:** Sonnet

## Goal

Every option property of the WinUI `LoomEditor` exists on the Avalonia one, with the same name, default and effect.

## Sources

The WinUI twins (`src/LoomEditor.WinUI/Controls/`): `LoomEditor.Margin.cs`, `LoomEditor.LineNumbers.cs`, `LoomEditor.Minimap.cs`, `LoomEditor.Whitespace.cs`,
`LoomEditor.WordWrap.cs`, `LoomEditor.Guides.cs`, `LoomEditor.StickyScroll.cs`, `LoomEditor.Decorations.cs`, `LoomEditor.HiddenAreas.cs`, `LoomEditor.Snippets.cs`,
`LoomEditor.LanguageFeatures.cs`, `LoomEditor.GotoDefinition.cs`, and the public enums `EditorWordWrap.cs`, `EditorWrappingIndent.cs`, `EditorMinimapSide.cs`,
`EditorBracketPairGuides.cs`, `EditorAccessibilitySupport.cs`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/` — the same 17 files (twins; the 5 enums copied, [README](README.md) decision 3)
- `tests/LoomEditor.Avalonia.Tests/Controls/LoomEditorOptionsTests.cs` — create
- `PublicAPI.Unshipped.txt`, `AvaloniaParityPending.txt`

## Design decisions (already made — do not change)

- `StyledProperty<T>` per WinUI DP, same default, same change handler body. Enum types are the Avalonia copies.
- Where a WinUI partial reads `ActualTheme` or a XAML resource, use the Avalonia equivalent from [README](README.md) decision 3 / 9. Nothing else is adapted.

## Tests

- [ ] New, one per group, on a headless window: `LineNumbersEnabled_False_TextStartsAtLeftMargin` (the x of column 1 is smaller); `MinimapEnabled_DrawsMinimapPixels`
  (non-background pixels in the minimap column); `WordWrap_On_LongLineWraps` (`GetTopForLineNumber(2)` > 1 line height); `RenderWhitespace_All_DrawsDots`
  (pixels between words); `StickyScroll_Scrolled_ShowsHeader` (a class header line drawn at y = 0 after scrolling into its body)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

Host hooks (P15-16), accessibility (P15-14).

## Suggested commit message

`feat(avalonia): the editor option properties of LoomEditor`
