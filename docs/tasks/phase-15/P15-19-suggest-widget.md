# P15-19 — The suggest (completion) widget

- **Status:** todo
- **Depends on:** P15-18
- **Size:** M

**Recommended model:** Sonnet

## Goal

Completion (triggered by typing or Ctrl+Space) shows the suggest list in the Avalonia `LoomEditor`, with icons by kind, keyboard navigation, accept and details, as WinUI.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/Suggest/SuggestPresenter.cs`, `SuggestWidget.cs`, `SuggestWidget.Rows.cs`, `SuggestWidget.Theme.cs`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/Suggest/` — the same 4 files (twins)
- `src/LoomEditor.Avalonia/Themes/Icons.axaml` — only if WinUI draws kind icons with a font glyph: then a `TextBlock` with `GlyphFontFamily` as WinUI's glyph, else nothing
- `tests/LoomEditor.Avalonia.Tests/Controls/SuggestWidgetTests.cs` — create
- `docs/testing/phase-15-manual-tests.md` — section "P15-19"

## Design decisions (already made — do not change)

- `ListView` → `ListBox` with `VirtualizingStackPanel`, rows built in code as WinUI (no data template with reflection bindings).
- The widget does not take focus; Up/Down/Enter/Tab/Escape reach it through the editor's key path, as WinUI.

## Tests

- [ ] New: `CtrlSpace_ShowsSuggestions` (fake provider of 3 items); `Down_Enter_AcceptsSecond`; `Typing_FiltersList`; `Escape_Hides`; `List_Of_10000_Items_IsVirtualized`
  (fewer than 50 row controls created)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Parameter hints if WinUI has none.

## Suggested commit message

`feat(avalonia): the suggest widget`
