# P8-22 — The completion list (WinUI)

- **Status:** todo
- **Depends on:** P8-21
- **Size:** M

## Goal

The completion list is on screen: rows with a kind badge, the label with the matched letters highlighted, the label details, a detail strip with the selected item's detail and documentation; it opens under (or over) the word being typed, follows Up / Down, accepts on a click, scrolls with the wheel and closes with `Esc`, using the overlay layer of P8-11.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/languages.ts` | 400–446 (`CompletionItemKinds.toLabel`) | `SuggestKindBadges.ToLabel` (English names, same strings) |
| `src/vs/editor/contrib/suggest/browser/suggestWidget.ts` | 43–52 (the `editorSuggestWidget.*` colors), the row and list sizes (search `itemHeight`, `defaultSize`, `maxVisibleSuggestions`), `_isDetailsVisible`, `showSuggestions` | look, sizes, colors |
| `src/vs/editor/contrib/suggest/browser/suggestWidgetRenderer.ts` | the `ItemRenderer.renderElement` (search `highlights`, `createMatches`, `deprecated`, `labelDetail`, `description`, `detailsLabel`) | what a row shows |
| `src/vs/platform/theme/common/colors/listColors.ts`, `quickpickColors.ts` | search `list.highlightForeground`, `list.focusHighlightForeground`, `quickInputList.focusBackground`, `quickInputList.focusForeground`, `quickInputList.focusIconForeground` | color chains |

## Files to create or change

- `src/LoomEditor.View/Suggest/SuggestKindBadges.cs`, `SuggestRowModel.cs` — create (`internal static` / `internal sealed record`)
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: the ids below
- `src/LoomEditor.WinUI/Controls/Suggest/SuggestWidget.cs`, `SuggestWidget.Rows.cs`, `SuggestWidget.Theme.cs` (`internal sealed partial class SuggestWidget : UserControl`, **code only, no XAML file**), `SuggestPresenter.cs` (`internal sealed class SuggestPresenter : ISuggestPresenter`), `README.md` — create
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` / `LoomEditor.Hover.cs` — change: where P8-11 assigns the hover presenter, assign `_context.SuggestPresenter` too (put both in one `InitializeOverlayWidgets()` in `LoomEditor.Overlay.cs`, a new file; move P8-11's hover glue there)
- `tests/LoomEditor.View.Tests/Suggest/SuggestKindBadgesTests.cs`, `SuggestRowModelTests.cs` — create
- `docs/testing/phase-8-manual-tests.md` — change: section 3 below

## Design decisions (already made — do not change)

- **Colors** (`EditorColorDefaults`; read each definition at the files above, resolve the chains, and add every id that a chain needs and the table lacks; one line per id with the upstream line in a comment): `editorSuggestWidget.background` (= `editorWidget.background`), `.border` (= `editorWidget.border`), `.foreground` (= `editor.foreground`), `.selectedForeground`, `.selectedBackground`, `.highlightForeground`, `.focusHighlightForeground`
  (plus `quickInputList.focusForeground`, `quickInputList.focusBackground`, `list.highlightForeground`, `list.focusHighlightForeground`, `list.activeSelectionBackground`, `list.activeSelectionForeground` as the chains require). An id that is already defined is not defined again. A test of the existing theme tests that lists every id must still pass.
- **`SuggestKindBadges`** (View, pure): `static string Glyph(CompletionItemKind kind)` — a two-letter badge (no codicon font exists): Method `Me`, Function `Fn`, Constructor `Ct`, Field `Fd`, Variable `Va`, Class `Cl`, Struct `St`, Interface `If`, Module `Mo`, Property `Pr`, Event `Ev`, Operator `Op`, Unit `Un`, Value `Vl`, Constant `Cn`, Enum `En`, EnumMember `EM`, Keyword `Kw`, Text `Tx`, Color `Co`, File `Fi`, Reference `Rf`, Customcolor `Cc`, Folder `Fo`, TypeParameter `Tp`, User `Us`, Issue `Is`, Tool `To`, Snippet `Sn`;
  `static string ToLabel(CompletionItemKind kind)` — port of `CompletionItemKinds.toLabel` (`Method`, `Function`, `Constructor`, `Field`, `Variable`, `Class`, `Struct`, `Interface`, `Module`, `Property`, `Event`, `Operator`, `Unit`, `Value`, `Constant`, `Enum`, `Enum Member`, `Keyword`, `Text`, `Color`, `File`, `Reference`, `Custom Color`, `Folder`, `Type Parameter`, `User`, `Issue`, `Tool`, `Snippet`; an unknown value → `""`).
- **`SuggestRowModel`** (View, pure): `sealed record SuggestRowModel(string Label, IReadOnlyList<FilterMatch> Matches, string? LabelDetail, string? LabelDescription, string BadgeGlyph, string KindLabel, bool IsDeprecated, bool IsSelected)`; `static SuggestRowModel Create(SuggestItem item, bool isSelected)`:
  `Matches = FuzzyScore.IsDefault(item.Score) ? [] : Fuzzy.CreateMatches(item.Score)` (Monaco highlights `createMatches(element.score)` over the label), `IsDeprecated = item.Completion.Tags?.Contains(CompletionItemTag.Deprecated) == true`. `static string DetailText(SuggestItem item)`: the strip text = `Detail` (if any), then a blank line, then the documentation as plain text (`MarkdownParser.Parse(documentation.Value).ToPlainText()`, P8-07), the whole string cut to **400 characters** with `…` appended when cut; empty when there is neither.
- **`SuggestWidget`** (`UserControl`, code only; `AllowFocusOnInteraction = false`, `IsTabStop = false`): fixed width **430 DIPs**, row height **22 DIPs**, `SuggestListState.MaxVisibleRows` rows. Content: a `Border` (1 DIP, 4 DIP corner radius) with a vertical `StackPanel`: a `Grid` of `MaxVisibleRows` **pre-created row elements** (each a horizontal `Grid`: badge `Border` 24 DIPs wide with a centered `TextBlock` of the glyph at font size 10, then the label `TextBlock` (`IsTextSelectionEnabled = false`, `TextTrimming = CharacterEllipsis`, with `Run`s: highlighted letters in `.highlightForeground` — `.focusHighlightForeground` on the selected row — and semi-bold),
  then the dim `LabelDetail` run, then the right-aligned dim `LabelDescription`), and below it the **detail strip** (a `TextBlock`, wrap, dim, max 4 lines, collapsed when `DetailText` is empty). No `ListView`, no `ItemsSource`, no binding, no data template (ADR 0011 decision 8).
  `void Refresh(SuggestListState state)`: for each of the `MaxVisibleRows` slots: collapsed when beyond `VisibleRowCount`, else filled from `SuggestRowModel.Create(state.VisibleItems[i], isSelected: state.FirstVisibleIndex + i == state.SelectedIndex)`; the selected row gets `.selectedBackground` / `.selectedForeground`; a deprecated label has `TextDecorations.Strikethrough`; the row's `ToolTip` is `KindLabel`. The font is the editor's font family and size.
  Row pointer events: `PointerPressed` on a row → `state.Select(firstVisibleIndex + i)` then the presenter executes `SuggestCommands.AcceptSelectedSuggestion` through the editor's commands (the same call P8-11 uses for `HideHover`); `PointerWheelChanged` on the widget → `state.ScrollBy(delta > 0 ? -3 : 3)`, `e.Handled = true`.
- **`SuggestPresenter(OverlayHost overlay, LoomEditorControl owner) : ISuggestPresenter`**: `Show(state, anchor)`: subscribes once to `state.Changed` (calls `Refresh` then re-places), `Refresh`, the anchor rectangle through `owner.AutomationGeometry.GetBoundingRectangles(new Range(anchor.LineNumber, anchor.Column, anchor.LineNumber, anchor.Column + 1))` (as P8-11; use the first rectangle; none → `Hide()` and return),
  `Measure`, `OverlayPlacement.Place(rect.X, rect.Y, rect.Y + rect.Height, 430, desiredHeight, overlay.Width, overlay.Height)`, `overlay.Show`. **The anchor is the start of the word being completed**, so the list does not move while the user types more letters of the word: `Show` is called again by the controller after each `DidSuggest` with the same anchor and only re-places when the anchor changed.
  `Hide()`: collapse, unsubscribe. `IsVisible`. The theme: `SuggestWidget.ApplyTheme(EditorTheme)` is called where the hover widget's theme is applied (P8-11's glue).
- No public API. No screen reader support for the list (follow-up, README of the widget says so): it is a mouse / keyboard sighted-user feature in phase 8.
- Native AOT: no reflection, no `{Binding}`; `partial` on the widget for CsWinRT; no CsWinRT / trimming warning may be suppressed.

## Tests

- [ ] Port-ish (`SuggestKindBadgesTests`; comment `// Monaco: CompletionItemKinds.toLabel`): `ToLabel_AllKinds` (a `[Theory]` over the 29 names above), `ToLabel_Unknown_Empty`, `Glyph_AllKinds_TwoLettersAndDistinct` (all 29 glyphs are non-empty and unique), `Glyph_Spot` (Method `Me`, EnumMember `EM`, Snippet `Sn`)
- [ ] New (`SuggestRowModelTests`): `Create_NoWord_NoMatches` (default score), `Create_WithWord_HasMatches` (an item scored against `fo`: matches `[0,2)` for `foo`), `Create_Deprecated`, `Create_Selected`, `Create_LabelDetails_Copied`, `DetailText_DetailAndDocumentation` (`"detail\n\ndoc text"`), `DetailText_OnlyDetail`, `DetailText_Empty`, `DetailText_Markdown_PlainText` (`"**bold** `code`"` → `"bold code"`), `DetailText_CutsAt400` (a 1000-character documentation → 400 characters + `…`)
- [ ] No other unit tests (WinUI code); the manual checks and the AOT publish are the verification

## Manual checks (add to `docs/testing/phase-8-manual-tests.md` as "3. Completion list"; they need a provider: the Gallery's **Demo providers** of P8-37 or a temporary one; mark rows `pending` if you could not run them)

| # | Steps | Expected |
|---|---|---|
| 3.1 | Type a letter of a word the provider knows | The list opens under the word after a very short delay, with the matching items first and the typed letters highlighted |
| 3.2 | Press `Down` / `Up` / `PageDown` / `PageUp` | The selection moves, wraps at the ends, and the list scrolls when the selection leaves the 12 visible rows |
| 3.3 | Press `Enter` (then repeat with `Tab`) | The word is replaced by the item's text, the caret is after it, the list closes; one `Ctrl+Z` undoes the completion |
| 3.4 | Press `Esc`; click elsewhere; scroll the text | The list closes in each case |
| 3.5 | Click a row | That item is inserted |
| 3.6 | Wheel over the list with more than 12 items | The list scrolls and the text does not |
| 3.7 | Select an item that has documentation | The strip under the list shows the detail and the first part of the documentation |
| 3.8 | `Ctrl+Space` on an empty word | The list opens with every item |
| 3.9 | A word near the bottom of the editor | The list opens above the word |
| 3.10 | Change the theme | The list's colors follow the theme |

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` succeeds and the exe is still running after 10 seconds (README §7), then killed; result recorded in this card (never ticked if not run)
- [ ] The manual rows were tried where possible and the result is written in the card; others stay `pending`
- [ ] `docs/PORTING.md` map: `suggestWidget.ts` / `suggestWidgetRenderer.ts` (look and list behavior only; not ported: details side pane, status bar, icons toggle, resizing, loading state, `selectionMode`), `toLabel`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Screen reader support of the list, the details side pane, resizing, kind icons (codicons), hover-selects-row, `editor.suggest.*` options.

## Suggested commit message

`feat(winui): add the completion list`
