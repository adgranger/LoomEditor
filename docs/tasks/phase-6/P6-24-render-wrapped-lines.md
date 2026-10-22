# P6-24 — Drawing, hit testing and folding on wrapped lines

- **Status:** done
- **Depends on:** P6-23
- **Size:** S

## Goal

The renderer, the hit testing and the folding controls are proven correct on wrapped lines (they already work on view
lines since P5-82); the few gaps are fixed and the accepted differences are documented.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to check against |
|---|---|---|
| `src/vs/editor/browser/viewParts/lineNumbers/lineNumbers.ts` | search `_getLineRenderLineNumber` | no number on the continuation of a wrapped line (already done in `TextViewRenderer.Gutter.cs`) |
| `src/vs/editor/browser/viewParts/linesDecorations/linesDecorations.ts` | search `_getDecorations` | `firstLineDecorationClassName` only on the first view line (already done in `TextViewRenderer.GutterDecorations.cs`) |
| `src/vs/editor/common/viewLayout/viewLineRenderer.ts` | search `startVisibleColumn` | how Monaco expands a tab in a continuation line from the model line's visible column |

## Files to create or change

- `tests/LoomEditor.Rendering.Tests/TextViewRendererWordWrapTests.cs` — create (follow `TextViewRendererHiddenAreasTests.cs`
  for the recording surface and the view model setup, with `EditorLayoutOptions { Wrapping = … }`)
- `src/LoomEditor.Rendering/TextViewRenderer*.cs` — change only where a test fails
- `docs/PORTING.md` — "Behavior differences": the tab rendering row (see decisions)

## Design decisions (already made — do not change)

- **Tabs in a continuation line:** DirectWrite tab stops are measured from the start of each drawn layout, while Monaco
  expands a tab with `tabSize - ((startVisibleColumn + visibleColumn) % tabSize)`. LoomEditor keeps the DirectWrite
  tab stops (no per-line tab rewriting: it would break the column ↔ character mapping of the hit testing). Document it as a
  behavior difference: "a tab in the continuation of a wrapped line may render narrower or wider than in Monaco". No code.
- **The word wrap indicator** (`wordWrapIndicator`) is only reserved in the layout (P6-02); drawing it is not ported: say so
  in the `EditorWrappingOptions.WordWrapIndicator` XML doc.
- `StopRenderingLineAfter` applies to the view line (Monaco: `stopRenderingLineAfter` is per view line too): no change.

## Tests

- [x] New: `WrappedLine_DrawsEveryViewLine` (a model line wrapping in 3 view lines: 3 text draws with the expected texts,
      continuation lines start with the wrapped indent spaces)
- [x] New: `WrappedLine_LineNumberOnlyOnTheFirstViewLine`
- [x] New: `WrappedLine_FoldingChevronOnlyOnTheFirstViewLine` (a foldable region whose first line wraps)
- [x] New: `FoldingAWrappedRegion_HidesAllItsViewLines`
- [x] New: `HitTest_OnAContinuationLine_ReturnsItsViewPosition` (through `CreateLineHitTester`)
- [x] New: `SelectionAcrossAWrapSeam_HighlightsBothViewLines`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` behavior differences updated; the `Features/Folding/` row of "Cut points for phase 6" removed (folding
      with wrapped lines is covered)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Injected text drawing (P6-25), the control's word wrap property (P6-26).

## Suggested commit message

`test(rendering): cover wrapped lines in the renderer`
