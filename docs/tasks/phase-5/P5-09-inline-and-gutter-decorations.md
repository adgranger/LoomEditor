# P5-09 — Drawing inline decorations, after content and gutter decorations

- **Status:** done
- **Depends on:** P5-08
- **Size:** M

## Goal

The renderer draws the rest of what phase 5 decorations need: the text color of `inlineClassName` decorations
(merged into the token colors of the line), the after content of `afterContentClassName` decorations (the folded `⋯`),
and the decorations of the gutter (`marginClassName` over the whole margin, `linesDecorationsClassName` and
`firstLineDecorationClassName` in the lines-decorations column).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewLayout/lineDecorations.ts` | 11–21, 63–92 | `LineDecoration` (fields), `LineDecoration.filter` |
| `src/vs/editor/browser/viewParts/glyphMargin/glyphMargin.ts` | 20–119 | `DecorationToRender`, `LineDecorationToRender`, `VisibleLineDecorationsToRender`, `DedupOverlay._render` |
| `src/vs/editor/browser/viewParts/linesDecorations/linesDecorations.ts` | 72–112 | `_getDecorations` (`linesDecorationsClassName`, `firstLineDecorationClassName`), `prepareRender` (left / width of the column) |
| `src/vs/editor/browser/viewParts/marginDecorations/marginDecorations.ts` | 57–89 | `_getDecorations` (`marginClassName`), `prepareRender` (whole margin) |
| `src/vs/editor/browser/view.ts` | 227–231 | margin overlay order: current line, margin decorations, lines decorations, line numbers |
| `src/vs/editor/contrib/folding/browser/folding.css` | 33–40 | `.inline-folded:after`: `margin-left: 0.2em`, the `⋯` character |
| `src/vs/editor/test/common/viewLayout/lineDecorations.test.ts` | 43–52 | `issue #3661` |

## Files to create or change

- `src/LoomEditor.View/Layout/LineDecoration.cs` — create (`LineDecoration` record + `Filter`)
- `src/LoomEditor.Rendering/LineStyle.cs` — change: `Create` takes optional foreground overrides
- `src/LoomEditor.Rendering/TextViewRenderer.Decorations.cs` — change: inline overrides, after content
- `src/LoomEditor.Rendering/TextViewRenderer.GutterDecorations.cs` — create (partial: the `DedupOverlay` port and the two margin overlays)
- `src/LoomEditor.Rendering/TextViewRenderer.Gutter.cs` — change: draw margin and lines decorations between the margin
  line highlight and the line number
- `tests/LoomEditor.View.Tests/Layout/LineDecorationTests.cs` — create
- `tests/LoomEditor.Rendering.Tests/LineStyleTests.cs` — change
- `tests/LoomEditor.Rendering.Tests/TextViewRendererDecorationsTests.cs` — change (new tests below)

## Design decisions (already made — do not change)

- **`LineDecoration`**: `sealed record LineDecoration(int StartColumn, int EndColumn, string ClassName, InlineDecorationType Type)`
  and `static IReadOnlyList<LineDecoration> Filter(IReadOnlyList<InlineDecoration> decorations, int lineNumber, int minLineColumn, int maxLineColumn)`
  in `LoomEditor.View.Layout`. `equalsArr`, `extractWrapped` (word wrap: phase 6) and `LineDecorationsNormalizer` are
  not ported.
- **Inline foreground.** For each visible line, `data.GetInlineDecorations(line)` → `LineDecoration.Filter(…, 1, maxColumn)`;
  every `Regular` / `RegularAffectingLetterSpacing` decoration whose `DecorationStyles.Get(className)` has a
  `ForegroundColorId` that resolves gives a `StyleOverride(int Start, int End, uint ForegroundArgb)` (0-based offsets,
  `End` exclusive, clamped to the laid-out length). `LineStyle.Create(tokens, length, colorMap, defaultForeground,
  IReadOnlyList<StyleOverride>? overrides = null)` applies them **in list order, a later one winning** on the
  characters it covers, then merges equal neighbours as today; the font style of the tokens is kept. Behavior
  difference (XML doc + PORTING.md): Monaco splits the line into parts with `LineDecorationsNormalizer` and lets the
  CSS cascade pick the color of overlapping classes; here the last decoration of the line's list wins. The style is
  part of the layout cache key, so a changed decoration re-lays the line out.
- **After content.** For each `After` inline decoration of a visible line whose style has `AfterContentText`: lay the
  text out with `_cache.Get(text)` and draw it at `ContentLeft + GetCharacterX(column - 1) + 0.2 × FontSize - scrollLeft`
  (CSS `margin-left: 0.2em`) in the resolved `AfterContentColorId` (nothing when it resolves to `null`). It is drawn
  after the text of the line and takes no room: text after it is not pushed right (behavior difference; phase 5 only
  uses it at the end of a line). `Before` decorations draw nothing (`beforeContentClassName` has no row in phase 5:
  `// Phase 6 cut point: before content`).
- **Gutter.** Port `DedupOverlay._render` (sort by class name ordinal, then start line, then end line; same class on
  a line only once; stable sort). Margin decorations fill `0 … ContentLeft`; lines decorations fill
  `DecorationsLeft … DecorationsLeft + DecorationsWidth`; both draw the row's background then border, like P5-08.
  Order per line: margin line highlight, margin decorations, lines decorations, line number. `zIndex` is carried but,
  as in Monaco's `DedupOverlay`, does not reorder. Tooltips are not ported.
- `GlyphMarginClassName` is not drawn: `// Phase 6 cut point: glyph margin decorations` (no phase 5 feature uses it;
  Monaco's lanes and widgets are big).

## Tests

- [x] Port (`lineDecorations.test.ts` › `Editor ViewLayout - ViewLineParts`): `issue #3661: Link decoration bleeds to next line when wrapping`.
  Not ported: `Bug 9827:Overlapping inline decorations can cause wrong inline class to be applied`,
  `issue #3462: no whitespace shown at the end of a decorated line`, `empty width only decorations are kept, empty regular ones are not`,
  `width only decorations come before other decorations at the same position`, `width only decorations are not turned into pseudo elements`,
  `ViewLineParts` (normalizer, injected text), `issue #37401: Allow both before and after decorations on empty line`
  (a range with column 0 cannot be built in C#).
- [x] New (`LineDecorationTests`): `Filter_DecorationOnOtherLine_IsDropped`, `Filter_EmptyRegular_IsDropped_EmptyAfter_IsKept`,
  `Filter_MultiLine_UsesMinAndMaxColumnOnInnerLines`
- [x] New (`LineStyleTests`): `Create_Override_SplitsTheTokenRun`, `Create_OverlappingOverrides_LastWins`,
  `Create_OverridesPastTheLength_AreClamped`, `Create_OverrideOnPlainLine_GivesRuns`
- [x] New (`TextViewRendererDecorationsTests`): `Render_InlineClassNameWithForeground_ColorsTheText` (`findMatchInline`
  with a theme loaded by `EditorTheme.Load` that sets `editor.findMatchForeground`: its default is `null`),
  `Render_InlineClassNameWithoutRow_KeepsTokenColors`, `Render_AfterContent_DrawnAfterTheLineEnd`
  (`inline-folded` on a whole line: a `⋯` layout at the expected x and color),
  `Render_MarginClassName_FillsTheMargin`, `Render_LinesDecorationsClassName_FillsTheDecorationsColumn`,
  `Render_FirstLineDecorationClassName_OnlyOnTheFirstLine`, `Render_SameGutterClassTwiceOnALine_DrawnOnce`
  (the gutter tests use existing rows, e.g. `folded-background` as `marginClassName` and `bracket-match` as
  `linesDecorationsClassName`: the table is keyed by class, whatever option carries it; no test-only row)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (`lineDecorations.ts` subset, `linesDecorations.ts`, `marginDecorations.ts`,
      `DedupOverlay`); "Behavior differences": last inline decoration wins, after content takes no room
- [x] Card status and `docs/tasks/README.md` updated
- [x] No manual check yet (first visual check: bracket matching in step C, folding in step G)

## Out of scope

Folding icons in the gutter (step G), bracket pair colors (step C), glyph margin, injected text.

## Suggested commit message

`feat(rendering): draw inline, after-content and gutter decorations`
