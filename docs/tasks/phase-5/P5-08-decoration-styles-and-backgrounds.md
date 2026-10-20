# P5-08 — `DecorationStyles` and drawing decoration backgrounds and borders

- **Status:** done
- **Depends on:** P5-07
- **Size:** M

## Goal

A class name of a decoration says what it draws: `DecorationStyles` is the hand-written table that replaces Monaco's
CSS (ADR 0009, decision 4). The renderer draws the `className` decorations of the viewport — whole-line and range
backgrounds and borders — under the text, above the selection, like Monaco's `DecorationsOverlay`. The control
redraws when decorations change.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/decorations/decorations.ts` | 67–228 | `prepareRender` (keep `className` decorations, sort), `_renderWholeLineDecorations`, `_renderNormalDecorations` (merge touching ranges of the same class, `showIfCollapsed` end fix), `_renderNormalDecoration` (`showIfCollapsed` widening, `shouldFillLineOnLineBreak`) |
| `src/vs/editor/browser/viewParts/viewLines/viewLines.ts` | 423–480 | `linesVisibleRangesForRange` (per line: start column, end column or line end when the range continues; `includeNewLines` adds `typicalHalfwidthCharacterWidth`) |
| `src/vs/editor/browser/view.ts` | 218–224 | the order of the content overlays (current line, selections, indent guides, **decorations**, then the text) |
| `src/vs/editor/contrib/find/browser/findWidget.css` | 226–241 | `.findMatch`, `.currentFindMatch`, `.findScope` |
| `src/vs/editor/contrib/find/browser/findWidget.ts` | 1474–1497 | theming: borders of `.findMatch` / `.findScope`, `.findMatchInline`, `.currentFindMatchInline` |
| `src/vs/editor/browser/widget/codeEditor/editor.css` | 32–39 | `.rangeHighlight` |
| `src/vs/editor/contrib/wordHighlighter/browser/highlightDecorations.css` | 6–40 | `.selectionHighlight`, `.wordHighlight`, `.wordHighlightStrong`, `.wordHighlightText` |
| `src/vs/editor/contrib/bracketMatching/browser/bracketMatching.css` | 6–10 | `.bracket-match` |
| `src/vs/editor/contrib/bracketMatching/browser/bracketMatching.ts` | 422–429 | `.bracket-match-inline` |
| `src/vs/editor/contrib/folding/browser/folding.css` | 33–44 | `.inline-folded:after`, `.folded-background` |

## Files to create or change

- `src/LoomEditor.View/Layout/DecorationStyle.cs`, `DecorationStyles.cs` — create (internal)
- `src/LoomEditor.Rendering/TextViewRenderer.Decorations.cs` — create (partial)
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — change: draw the decorations between the line highlight and the
  text; `Theme` setter clears the resolved-color cache
- `src/LoomEditor.WinUI/Controls/LoomEditor.Highlighting.cs` — change: the view event handler also invalidates on
  `ViewDecorationsChangedEvent`
- `tests/LoomEditor.View.Tests/Layout/DecorationStylesTests.cs` — create
- `tests/LoomEditor.Rendering.Tests/TextViewRendererDecorationsTests.cs` — create

## Design decisions (already made — do not change)

- **`DecorationStyle`**: `sealed record DecorationStyle(string? BackgroundColorId = null, string? BorderColorId = null,
  float BorderWidth = 1, string? ForegroundColorId = null, string? AfterContentText = null, string? AfterContentColorId = null)`.
  Colors are VS Code color ids, resolved by the renderer with `EditorTheme.GetColor` (P5-05); an id that resolves to
  `null` draws nothing (CSS with an undefined variable). `ForegroundColorId` and the after content are drawn by P5-09.
- **`DecorationStyles`**: `internal static class` with one private `Dictionary<string, DecorationStyle>` written as a
  literal (`StringComparer.Ordinal`), one row per CSS class with the upstream file and line in a comment, and
  `static DecorationStyle? Get(string? className)`: a decoration's class name is a space-separated list (Monaco keeps
  lists, e.g. `codicon codicon-folding-expanded`); `Get` returns the row of the **first** listed class that has one,
  else `null` (unknown classes draw nothing). Feature cards that need a new class add their row to this table.
- **Rows added now** (CSS above):

  | Class | Background | Border (width) | Foreground | After content |
  |---|---|---|---|---|
  | `findMatch` | `editor.findMatchHighlightBackground` | `editor.findMatchHighlightBorder` (1) | | |
  | `currentFindMatch` | `editor.findMatchBackground` | `editor.findMatchBorder` (2) | | |
  | `findScope` | `editor.findRangeHighlightBackground` | `editor.findRangeHighlightBorder` (1) | | |
  | `rangeHighlight` | `editor.rangeHighlightBackground` | `editor.rangeHighlightBorder` (1) | | |
  | `findMatchInline` | | | `editor.findMatchForeground` | |
  | `currentFindMatchInline` | | | `editor.findMatchHighlightForeground` | |
  | `selectionHighlight` | `editor.selectionHighlightBackground` | `editor.selectionHighlightBorder` (1) | | |
  | `wordHighlight` | `editor.wordHighlightBackground` | `editor.wordHighlightBorder` (1) | | |
  | `wordHighlightStrong` | `editor.wordHighlightStrongBackground` | `editor.wordHighlightStrongBorder` (1) | | |
  | `wordHighlightText` | `editor.wordHighlightTextBackground` | `editor.wordHighlightTextBorder` (1) | | |
  | `bracket-match` | `editorBracketMatch.background` | `editorBracketMatch.border` (1) | | |
  | `bracket-match-inline` | | | `editorBracketMatch.foreground` | |
  | `folded-background` | `editor.foldBackground` | | | |
  | `inline-folded` | | | | `"⋯"` in `editor.foldPlaceholderForeground` |

  `findMatchInline` / `currentFindMatchInline` keep Monaco's crossed colors (`findWidget.ts` 1489–1496): say so in a
  comment. `.focused .selectionHighlight` is the row (the unfocused `transparent(0.5)` rule of
  `highlightDecorations.ts` 101–106 is not ported: behavior difference). High-contrast `dotted` / `dashed` borders
  are drawn solid, and `currentFindMatch`'s `padding: 1px` is ignored (behavior differences). Not added here: the
  folding gutter icons (step G adds its rows and how to draw them) and `bracket-highlighting-0…29` /
  `unexpected-closing-bracket` (step C: their color depends on how many of the six colors are transparent, Monaco
  `colorizedBracketPairsDecorationProvider.ts` 105–128).
- **Drawing** (`TextViewRenderer.Decorations.cs`, Monaco's `DecorationsOverlay`): take
  `data.GetDecorationsInViewport()`, keep the ones with `ClassName`, sort with a **stable** sort (LINQ `OrderBy`) by
  `ZIndex`, then `ClassName` (ordinal), then `Range.CompareUsingStarts`. Whole-line decorations first: from
  `ContentLeft` to `layoutInfo.Width`, one line high, on each visible line of the range. Then normal decorations,
  merged like Monaco (same class, same `ShowIfCollapsed`, `IntersectsOrTouches`). Per line: start x =
  `GetCharacterX(startColumn - 1)`; end x = `GetCharacterX(endColumn - 1)` on the last line of the range, else the line
  width (plus `TypicalHalfwidthCharacterWidth` when the class name is exactly `findMatch`: Monaco's
  `includeNewLines`); columns are clamped to the laid-out text (very long lines). `ShowIfCollapsed` widens a range
  narrower than `TypicalHalfwidthCharacterWidth` around its center; `ShouldFillLineOnLineBreak` on a line the range
  continues past draws to the right edge. Background first (`FillRectangle`), then the border (`DrawRectangle` inset
  by half the width, like `DrawLineHighlight`: CSS `box-sizing: border-box`). x positions subtract `scrollLeft`.
- **Order in `Render`**: clear, selections, current line highlight, **decorations**, text, composition, caret, gutter
  (Monaco draws the decorations overlay after the selections overlay, under the text).
- **Colors**: the renderer resolves each id once per theme (`Dictionary<string, ThemeColor?>`, cleared when `Theme`
  is set).

## Tests

No Monaco test (Monaco renders HTML). New:

- [x] `DecorationStylesTests`: `Get_KnownClass_ReturnsItsRow`, `Get_ClassList_ReturnsTheFirstKnownClass`,
  `Get_UnknownOrNull_ReturnsNull`. `DecorationStyles` exposes `internal static IEnumerable<string> ColorIds` (every id
  of the table) for the next test.
- [x] `TextViewRendererDecorationsTests` (decorations added with `Model.DeltaDecorations(harness.ViewModel.EditorId, …)`,
  a theme from `EditorTheme.Load` with known colors):
  `DecorationStyles_EveryColorId_IsAKnownThemeColor` (`EditorColorDefaults.IsKnown`: the Rendering tests see the
  Themes internals since P5-05),
  `Render_RangeDecoration_FillsItsBackgroundUnderTheText` (the fill comes before the `DrawLineLayout` of that line),
  `Render_RangeDecoration_DrawsItsBorderInside`, `Render_WholeLineDecoration_FillsTheLine`,
  `Render_MultiLineDecoration_ContinuesToTheLineEnd`, `Render_FindMatch_IncludesTheNewLine`,
  `Render_TouchingDecorationsOfTheSameClass_AreMerged`, `Render_ShowIfCollapsed_EmptyRange_IsWidened`,
  `Render_UnknownClassName_DrawsNothing`, `Render_ZIndex_DrawsHigherLast`, `Render_DecorationAboveTheSelection`
  (the decoration fill comes after the selection fill), `Render_HorizontalScroll_ShiftsDecorations`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (`decorations.ts` → `TextViewRenderer.Decorations.cs`); "Behavior differences":
      the CSS rows above (solid borders, no padding, focused-only selection highlight)
- [x] Card status and `docs/tasks/README.md` updated
- [x] No manual check yet: no feature makes decorations (the first visual check is bracket matching, step C)

## Out of scope

Inline foreground, after content, gutter decorations (P5-09). Overview ruler and minimap marks.

## Suggested commit message

`feat(rendering): draw decoration backgrounds and borders`
