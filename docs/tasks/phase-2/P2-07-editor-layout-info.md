# P2-07 — `EditorLayoutInfo` (where the gutter ends and the text starts)

- **Status:** done
- **Depends on:** P2-06
- **Size:** S

## Goal

Port `EditorLayoutInfoComputer.computeLayout`: from the control size, the font and a few options, it computes the
horizontal layout: glyph margin, line numbers, line decorations, content (text) area, scrollbar sizes and the
overview ruler. This replaces the phase 0 rule "gutter = digits + 3 characters".

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/config/editorOptions.ts` | 2476–2493 | `OverviewRulerPosition` |
| same | 2504–2583 | `EditorLayoutInfo` — **without** `minimap`, `isWordWrapMinified`, `isViewportWrapping`, `wrappingColumn` |
| same | 2605–2617 | `EditorLayoutInfoComputerEnv` — **without** `memory`, `isDominatedByLongLines`, `viewLineCount` |
| same | 2922–3068 | `computeLayout`, without the word-wrap, accessibility and minimap parts (see Design decisions) |
| same | 3357 | `lineDecorationsWidth` default (10) |
| same | 6470–6473 | `lineNumbersMinChars` default (5) |
| same | 4218–4224 | scrollbar defaults: horizontal 12, vertical 14 |
| `src/vs/editor/test/browser/config/editorLayoutProvider.test.ts` | 125–814 | `EditorLayoutProvider 1` … `EditorLayoutProvider 8 - rounds floats` (10 tests, all `minimap: false`, no word wrap) |

## Files to create or change

- `src/LoomEditor.View/Layout/EditorLayoutInfo.cs` — create (`EditorLayoutInfo`, `OverviewRulerPosition`)
- `src/LoomEditor.View/Layout/EditorLayoutInfoComputer.cs` — create (`static EditorLayoutInfo ComputeLayout(EditorLayoutOptions options, EditorLayoutInfoComputerEnv env)`)
- `src/LoomEditor.View/Layout/EditorLayoutOptions.cs` — create
- `tests/LoomEditor.View.Tests/Layout/EditorLayoutInfoComputerTests.cs` — create

## Design decisions (already made — do not change)

- All types `internal sealed record`s with `int` fields, except the inputs `TypicalHalfwidthCharacterWidth`,
  `MaxDigitWidth` and `PixelRatio`, which are `double`.
- `EditorLayoutOptions` holds only the options `computeLayout` reads: `GlyphMargin`, `LineNumbers` (`bool`, on/off —
  relative and interval numbers are out of scope), `LineNumbersMinChars`, `LineDecorationsWidth`, `Folding`,
  `ShowFoldingDecoration`, `VerticalScrollbarSize`, `HorizontalScrollbarSize`, `VerticalHasArrows`, `ArrowSize`.
- **Defaults that differ from Monaco:** `GlyphMargin = false` and `Folding = false` (Monaco: `true`). No feature
  draws a glyph or a folding arrow yet, and a 16–19 DIP empty strip would look like a bug. The phases that add
  breakpoints or folding turn them on. Document this in `docs/PORTING.md` ("Behavior differences").
- Minimap: treated as disabled (`minimapWidth = 0`, nothing moves to the right). Word wrap: treated as `off` (the
  `viewportColumn` is still computed). Accessibility "dominated by long lines": not ported. Phase 6 brings them back.
- `Math.round` → `Math.Round(x, MidpointRounding.AwayFromZero)` (`lineNumbersWidth` in test "8 - rounds floats").

## Tests

- [x] Port: `EditorLayoutProvider 1`, `1.1`, `2`, `3`, `4`, `5`, `6`, `7`, `8`, `8 - rounds floats`. Compare every
      field that exists in the C# record; skip the `minimap` and word-wrap fields of the expected objects.
- [x] New: with LoomEditor defaults (line numbers on, min 5 chars, decorations 10, no glyph margin, no folding),
      `maxDigitWidth = 8.4` and 1,000 lines → `LineNumbersWidth == 42`, `ContentLeft == 52`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map entry for `editorOptions.ts` (`EditorLayoutInfoComputer`, subset) and behavior differences (glyph margin, folding defaults)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Minimap, word wrap, the rest of `editorOptions.ts` (there is no options system yet), drawing the gutter (P2-15).

## Suggested commit message

`feat(view): port the editor layout computation`
