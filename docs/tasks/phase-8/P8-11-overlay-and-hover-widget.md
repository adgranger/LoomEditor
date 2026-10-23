# P8-11 — The overlay layer and the hover popup (WinUI)

- **Status:** todo
- **Depends on:** P8-10
- **Size:** M
- **Recommended model:** Opus (first widget layer in the control template under Native AOT; pointer routing between the canvas and a child element and the UIA-geometry anchoring cannot be diagnosed from this card. On failure: stop, do not commit, report the exact build / publish / runtime error and what you tried)

## Goal

Resting the mouse on text that has a marker or a provider answer shows a popup under (or over) the word with the message and the Markdown, and it goes away when the mouse leaves, the text changes or `Esc` is pressed. This card also creates the **overlay canvas** of the control template
that the completion list (P8-22) reuses, and a pure placement function.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/hover/browser/contentHoverWidget.ts` | 40–200 (`ContentHoverWidget`, its position preference: below the anchor, above when there is no room, `_layout`) | the placement rules → `OverlayPlacement` |
| `src/vs/editor/contrib/hover/browser/hover.css` (and `src/vs/base/browser/ui/hover/hover.css`) | the `.monaco-hover` rules (padding, border, max width, `hover-row` separators, `code` font) | sizes and look |
| `src/vs/editor/contrib/hover/browser/markerHoverParticipant.ts` | 114–160 (`_renderMarkerHover`) | the marker part: message, then `source(code)` dim |

## Files to create or change

- `src/LoomEditor.View/Layout/OverlayPlacement.cs` — create (`internal static`, pure)
- `src/LoomEditor.WinUI/Themes/Generic.xaml` — change: `PART_OverlayCanvas`
- `src/LoomEditor.WinUI/Controls/Overlay/OverlayHost.cs` — create (`internal sealed`): owns the canvas, adds / removes / positions elements
- `src/LoomEditor.WinUI/Controls/Hover/HoverWidget.cs`, `HoverWidget.Markdown.cs`, `HoverWidget.Theme.cs` — create (`internal sealed partial class HoverWidget : UserControl`, **code only: no XAML file**)
- `src/LoomEditor.WinUI/Controls/Hover/HoverPresenter.cs` — create (`internal sealed class HoverPresenter : IHoverPresenter`)
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` and `LoomEditor.Pointer.cs` — change: template part lookup, `IsFromOverlay` body, assign `_context.HoverPresenter`, theme applied on theme change
- `src/LoomEditor.WinUI/Controls/LoomEditor.Hover.cs` — create: `internal` glue (`InitializeHover`, `ApplyHoverTheme`); **no new public member**
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: ids below
- `tests/LoomEditor.View.Tests/Layout/OverlayPlacementTests.cs` — create
- `docs/testing/phase-8-manual-tests.md` — change: section 2 below; `src/LoomEditor.WinUI/Controls/Hover/README.md` — create

## Design decisions (already made — do not change)

- **Colors** (`EditorColorDefaults`, values read from the upstream definitions: `editorHoverWidget.*` at `editorColors.ts` 187–198; `textLink.foreground`, `textLink.activeForeground`, `textCodeBlock.background` in `src/vs/platform/theme/common/colors/baseColors.ts`, search the ids):
  `editorHoverWidget.background` = `"editorWidget.background"` (all four), `editorHoverWidget.foreground` = `"editorWidget.foreground"`, `editorHoverWidget.border` = `"editorWidget.border"`, plus `textLink.foreground` and `textCodeBlock.background` with their four values. Check each id is not already defined first.
- **`OverlayPlacement`** (View, pure; `LoomEditor.View.Layout`): `internal readonly record struct OverlayRect(double X, double Y, double Width, double Height)`;
  `internal static OverlayRect Place(double anchorX, double anchorTop, double anchorBottom, double width, double height, double containerWidth, double containerHeight, bool preferBelow = true)` — all in the container's DIPs.
  Rules: `width = min(width, containerWidth)`; `x = clamp(anchorX, 0, containerWidth - width)`; `spaceBelow = containerHeight - anchorBottom`, `spaceAbove = anchorTop`; if `preferBelow`: below when `height <= spaceBelow`, else above when `height <= spaceAbove`, else the side with **more** space (ties: below), with `height` cut to that space;
  if not `preferBelow` the same with the sides swapped. Below: `y = anchorBottom`; above: `y = anchorTop - height`. A negative or NaN input throws `ArgumentOutOfRangeException`. Header: `// Inspired by microsoft/vscode: src/vs/editor/contrib/hover/browser/contentHoverWidget.ts (layout)`.
- **Overlay canvas**: in `Generic.xaml` add `<Canvas x:Name="PART_OverlayCanvas" />` **right after `PART_FindWidget`** (above the canvas control and the find widget, below the scroll bars), **no `Background`** (so empty areas are not hit-testable and the pointer events reach the text), `IsHitTestVisible` default. `OverlayHost` wraps it:
  `Show(FrameworkElement element, OverlayRect rect)` (adds if needed, sets `Canvas.Left/Top`, width as `MaxWidth`, height as `MaxHeight`, `Visibility.Visible`), `Hide(FrameworkElement element)` (collapses), `bool Contains(DependencyObject? d)` (walks `VisualTreeHelper.GetParent` up to the canvas) and `double Width`, `Height` (`ActualWidth`, `ActualHeight` of the canvas).
  The control looks the part up in `OnApplyTemplate` like the other parts (a missing part = no overlay widgets, nothing crashes). `IsFromOverlay(source)` (declared in P8-08) now returns `_overlay?.Contains(source as DependencyObject) == true`; `OnPointerPressed`, `OnPointerMoved`, `OnPointerReleased` return early for it **without** `Handled` (the widget's own handlers run).
- **`HoverWidget`** (`UserControl`, code only; `partial`, `internal sealed`): `Content` = a `Border` (`BorderThickness` 1, `CornerRadius` 4, padding `8,4`) holding a vertical `StackPanel` inside a `ScrollViewer` (`VerticalScrollBarVisibility` auto, horizontal disabled) with `MaxWidth` 600 and `MaxHeight` 250 DIPs. `IsTabStop = false`, `AllowFocusOnInteraction = false`.
  `void Update(IReadOnlyList<IHoverPart> parts, FontFamily font, double fontSize)` rebuilds the children, one `RichTextBlock` (`IsTextSelectionEnabled = false`, `TextWrapping = Wrap`, no `{Binding}`) per part, rows separated by a 1-DIP `Rectangle` in the border brush:
  - `MarkerHoverPart`: a paragraph with a `Run` of `marker.Data`'s message (`Message` as is, line breaks kept: `Run.Text` with `\n` is rendered as is by splitting on `\n` into `LineBreak`s), then, if the marker has `Source` or `Code`, a dim run (opacity 0.6, left margin 6): `source(code)` when both, else `source`, else `(code)` (Monaco `_renderMarkerHover`).
  - `MarkdownHoverPart`: for each `MarkdownString`, `MarkdownParser.Parse(value)` (P8-07) → one `Paragraph` per block: `MarkdownParagraph` / `MarkdownHeading` (bold, size +2 per level up to +4) with one `Run` per inline (`Bold` → `FontWeights.SemiBold`, `Italic` → `FontStyle.Italic`, `Code` → the editor font family, `LinkUrl` → the `textLink.foreground` brush and underline: **not clickable**), `IsLineBreak` → `LineBreak`;
    `MarkdownCodeBlock` → a `Paragraph` with the editor font family and a top / bottom margin of 4 (no background, no highlighting); `MarkdownList` → one `Paragraph` per item, text prefixed with `• ` (or `1. `, `2. ` for an ordered list) and a left margin of 12; `MarkdownRule` → an empty 1-DIP-high `Paragraph` is not possible, use a `Rectangle` row (1 DIP, border brush).
  - `LoadingHoverPart` → the text "Loading…" in italic, opacity 0.6.
  The widget's font size is the editor's (`FontSize`), its font family `Segoe UI Variable` / `Segoe UI` for prose (`FontFamily("Segoe UI Variable, Segoe UI")`), the editor's family for code. All brushes come from `HoverWidget.Theme.cs` `ApplyTheme(EditorTheme)` (like `FindWidget.Theme.cs`: brushes created once in the constructor, `.Color` set on change): `editorHoverWidget.background`, `.foreground`, `.border`, `textLink.foreground`.
- **`HoverPresenter(OverlayHost overlay, LoomEditorControl owner)`**: `IsVisible`, `IsMouseOver` (set by the widget's `PointerEntered` / `PointerExited`), `Show(HoverContent content)`:
  1. the anchor rectangle: `owner.AutomationGeometry.GetBoundingRectangles(range)` (P7-11; **editor-relative DIPs**, the same coordinate space as the overlay canvas because both are inside the template's root grid) where `range = content.Anchor.Range` widened to one column when empty and the line is not empty; use the first rectangle (`X`, `Y`, `Height`);
     if there is none, `Hide()` and return (nothing is shown rather than a popup in the wrong place);
  2. `widget.Update(content.Parts, owner.FontFamily, owner.FontSize)`, `widget.Measure(new Size(overlay.Width, overlay.Height))`, `OverlayPlacement.Place(rect.X, rect.Y, rect.Y + rect.Height, desired.Width, desired.Height, overlay.Width, overlay.Height)`, `overlay.Show(widget, placed)`.
  `Hide()` collapses and resets `IsMouseOver = false`. The widget's `PointerExited` runs `owner.Context.Commands.Execute(HoverCommands.HideHover)` (the command of P8-10; use whatever the control already calls to run a command by id: `grep -n "Commands.Execute" src/LoomEditor.WinUI/Controls/*.cs | head -3`), so the controller and the popup stay in step.
- The control assigns `_context.HoverPresenter = new HoverPresenter(...)` when the template is applied and the overlay exists, and calls `ApplyTheme` for the widget when the control's theme changes (find where `FindWidget.ApplyTheme` is called and add the hover next to it).
- No public API. No UIA work for the popup (the screen reader mode announces it in a follow-up; listed in the README).
- Native AOT: no reflection, no `{Binding}`, no XAML file for the widget; `partial` on `HoverWidget` for CsWinRT. If the publish prints a CsWinRT or trimming warning about the new classes, fix the cause, never suppress it.

## Tests

- [ ] New (`OverlayPlacementTests`; comment `// Monaco: ContentHoverWidget layout`): `Place_Below_WhenItFits`, `Place_Above_WhenOnlyThereItFits`, `Place_MoreSpaceSide_WhenNeither` (height cut), `Place_Tie_PrefersBelow`, `Place_XClampedToTheRight`, `Place_XClampedToTheLeft`, `Place_WiderThanContainer_WidthCut`,
      `Place_PreferAbove_Mirrored`, `Place_NegativeInput_Throws`, `Place_NaN_Throws`
- [ ] No other unit tests (WinUI code); the manual checks below and the AOT publish are the verification

## Manual checks (add to `docs/testing/phase-8-manual-tests.md` as "2. Hover popup"; use a marker from the Setup line and the Gallery's **Demo providers** of P8-37 once they exist, until then only the marker rows)

| # | Steps | Expected |
|---|---|---|
| 2.1 | Rest the mouse on a word that has an error marker for about a second | A popup with the message appears under the word, with the theme's widget colors; moving the mouse off the word makes it go away |
| 2.2 | Move the mouse from the word onto the popup | The popup stays; leaving the popup closes it |
| 2.3 | With the popup open, press `Esc`; then scroll the wheel over the text; then type a character | Each of them closes it |
| 2.4 | `Ctrl+K` then `Ctrl+I` with the caret on the word | The popup shows at once |
| 2.5 | A word on the last visible line / near the bottom edge | The popup opens above the word |
| 2.6 | A hover with Markdown (bold, code, a code block, a list) once P8-37 provides one | Rendered as text with bold / italic / monospace code; links are blue but do nothing |
| 2.7 | Click and drag to select text across a word with a marker | No popup while dragging |

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` succeeds, and the published `LoomEditor.Gallery.exe` is still running after 10 seconds (README §7), then killed. Record the result in this card; never tick it if it was not run
- [ ] The manual rows 2.1–2.5 and 2.7 were tried by running the Gallery with a marker set from code (a temporary line in `MainWindow.xaml.cs` is allowed **only while testing**; remove it before the commit) and the result is written in the card; rows that could not be tried are marked `pending` in the manual file, not ticked
- [ ] `docs/PORTING.md` map: `contentHoverWidget.ts` (layout only), `hover.css` (look) with the behavior differences (no resizing, no status bar, not clickable links, no focus into the popup)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Resizing the popup, a status bar ("View Problem"), clickable links, keyboard focus in the popup, screen reader announcement of the hover, syntax highlighting in code blocks, the completion list (P8-22).

## Suggested commit message

`feat(winui): add the overlay layer and the hover popup`
