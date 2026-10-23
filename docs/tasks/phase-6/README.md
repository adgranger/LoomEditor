# Phase 6 — read this before any card

Phase 6 brings the view extras of the roadmap ([phase-6-view-extras.md](../../roadmap/phase-6-view-extras.md)): word wrap
(the line-break projection of view lines), injected text, indent and bracket pair guides, the decorations overview ruler,
the minimap and sticky scroll, plus the upstream tests that phases 3–5 left because they need word wrap or injected text.
This page lists the conventions that **every** phase 6 card relies on; a card never repeats them. The conventions of
[phase-3/README.md](../phase-3/README.md) (sections 1, 3, 4, 5 and 6) and [phase-5/README.md](../phase-5/README.md) (sections
1–5) still apply. "Cut point" comments for what phase 6 leaves out read `// Phase 7 cut point: …` / `// Phase 8 cut point: …`
(the phase the card names).

## 1. Upstream sources

Everything comes from `microsoft/vscode` at `0c9c0a4295a9222e12a605537738719455ae3056`. Card paths are full
(`src/vs/…`). Line numbers were checked against that commit when the cards were written; the function or class **name**
stays authoritative if a range looks off by a few lines.

## 2. Where things go

| What | Where |
|---|---|
| Injected text, guides, overview ruler and minimap options of decorations (`ModelDecorationOptions`, `TextModel` queries) | `LoomEditor.Core` (`Decorations/`, `Model/`, `Model/Guides/`, `Model/Events/`) |
| Wrapping options, layout, line-break computer, projections, view lines, view-model APIs | `LoomEditor.View/Layout/` |
| Pure minimap logic (char renderer, sampling, layout, pixel buffers) | `LoomEditor.View/Minimap/` (a view part, not a feature) |
| Sticky scroll | the feature `LoomEditor.View/Features/StickyScroll/` (listed in `EditorFeatures.cs`, with a README) |
| Drawing (guides, overview ruler, minimap bitmaps, sticky lines) | `LoomEditor.Rendering/TextViewRenderer.<Part>.cs` |
| Public properties (`WordWrap`, guides, minimap, sticky scroll) | `LoomEditor.WinUI/Controls/LoomEditor.<Part>.cs` + `PublicAPI.Unshipped.txt` |
| Theme color defaults | `LoomEditor.Themes/EditorColorDefaults.cs` (with the upstream file and line in a comment) |

Core knows nothing about themes: a decoration color is a `DecorationColor` (theme color id or literal, P6-36), resolved in the
view by `DecorationColorResolver` (P6-37).

## 3. Monaco concepts → LoomEditor (new in phase 6)

| Monaco | LoomEditor |
|---|---|
| `editor.wordWrap`, `wordWrapColumn`, `wrappingIndent`, `wordBreak`… | `EditorWrappingOptions` on `EditorLayoutOptions.Wrapping` (P6-01) |
| `wrappingStrategy: 'advanced'` (`DOMLineBreaksComputer`) | not ported: only the monospace computer |
| `ModelLineProjection` (class) | `WrappedModelLineProjection` (P6-15); `ModelLineProjection` is the static factory |
| `InjectedTextOptions` / `ModelDecorationInjectedTextOptions` | `InjectedTextOptions` (raw) / `ModelDecorationInjectedTextOptions` (normalized) (P6-03); `tokens` not ported |
| `ModelInjectedTextChangedEvent` (pushed to view models) | `TextModel.InjectedTextChanged` (P6-06) |
| `textModel.guides` | `TextModel.Guides` (`GuidesTextModelPart`, P6-28) |
| `editor.guides.*` | `EditorGuidesOptions` on `EditorLayoutOptions.Guides` (P6-32) |
| `editor.minimap.*` | `EditorMinimapOptions` on `EditorLayoutOptions.Minimap` (P6-40) |
| `editor.stickyScroll.*` | `EditorStickyScrollOptions` on `EditorLayoutOptions.StickyScroll` (P6-55, P6-58) |
| canvas `ImageData` / `Uint8ClampedArray` | `MinimapImageData` (RGBA `byte[]`) and `ClampToByte` (round half to even, clamp) (P6-44) |
| view events `ViewLinesChanged/Inserted/Deleted/Flushed` | not in LoomEditor: the minimap treats edits as a flush (P6-49) |
| outline model, document symbol provider, click-link gesture | phase 8 (sticky scroll uses the indentation model) |

## 4. JavaScript traps specific to phase 6

1. **Fractional columns.** `columnsForFullWidthChar` is a ratio of font widths, not always 2: visible columns of the line
   breaker are `double` (P6-09). `BreakOffsetsVisibleColumn` is `double[]`.
2. **`!array` is false for an empty array.** The line-break fast path tests `injectedText is null`, never `Count == 0`
   (P6-12, P6-17).
3. **`Array.prototype.sort` is stable.** Guides, overview ruler groups and minimap decorations sort with a stable sort
   (`OrderBy`), never `List.Sort`.
4. **`Math.round`** rounds .5 up (`MidpointRounding.AwayFromZero`); **`Uint8ClampedArray`** rounds half to even on store.
5. **`x | 0`** truncates toward zero: `(int)x`.

## 5. Test helpers shared by many cards

| Helper | Created by | Port of |
|---|---|---|
| `tests/LoomEditor.View.Tests/Layout/TestHelpers/LineBreaksTestHelpers.cs` (`ParseAnnotatedText`, `ToAnnotatedText`, `GetLineBreakData`, `AssertLineBreaks`, `AssertIncrementalLineBreaks`, `Injected`) | P6-10, P6-11, P6-12 | `monospaceLineBreaksComputer.test.ts` helpers |
| `tests/LoomEditor.View.Tests/Layout/TestHelpers/ProjectionTestHelpers.cs` (`CreateSplitLine`, `CreateLineBreakData`) | P6-15 | `modelLineProjection.test.ts` helpers |
| `tests/LoomEditor.View.Tests/Layout/TestHelpers/RecordingLineBreaksComputerFactory.cs` | P6-17 | — |
| `TestCodeEditorOptions.Wrapping`, `.EditorId` (existing helper, extended) | P6-22 | `testViewModel.ts` options |

A card that needs a helper missing from this table creates it in the `TestHelpers/` folder next to its tests and adds a row here.

## 6. Manual checks

WinUI and drawing cards add their manual checks to `docs/testing/phase-6-manual-tests.md` (created by P6-26, completed by
P6-62). Native AOT publish is required by the cards that add public API.

## 7. Commands and commit

Same as every card (see `CLAUDE.md`).
