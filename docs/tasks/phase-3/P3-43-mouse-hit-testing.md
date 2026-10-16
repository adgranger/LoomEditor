# P3-43 — Mouse hit testing on the canvas

- **Status:** done
- **Depends on:** P3-42
- **Size:** M

## Goal

Given a point in the editor (DIPs, relative to the control), find what is under it: a line number in the gutter, a
position in the text, the empty area after a line or after the last line — the way Monaco's `MouseTargetFactory`
does, but on our own layout (ADR 0006, decision 9): the line comes from `ViewLayout`, the column from the DirectWrite
layout of the line, as in Monaco's GPU path (`viewLinesGpu`).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/editorBrowser.ts` | search `export const enum MouseTargetType` | `MouseTargetType` (subset, see below) |
| `src/vs/editor/browser/controller/mouseTarget.ts` | 310–327 | `HitTestContext.getFullLineRangeAtCoord` |
| same | 392–415 | `BareHitTestRequest` (`mouseVerticalOffset = scrollTop + y`, `mouseContentHorizontalOffset = scrollLeft + x - contentLeft`, `isInMarginArea`) |
| same | 544–566, 1084–1092 | the `stickyTabStops` snap and `_snapToSoftTabBoundary` |
| same | 696–730 | `_hitTestMargin` (glyph margin / line numbers / line decorations) |
| same | 732–822 | `_hitTestViewLines` — the **`viewLinesGpu` branch** (773–806) and the padding / after-lines / empty-line / beyond-end rules |
| same | 866–872 | `_getMouseColumn` |
| `src/vs/editor/browser/gpu/viewLinesGpu.ts` | search `getPositionAtCoordinate` | how a canvas finds the column (nearest character boundary) |

## Files to create or change

- `src/LoomEditor.Rendering/ILineLayout.cs` — change: add `int HitTest(double x)` (offset of the nearest caret position, `0 … Text.Length`)
- `src/LoomEditor.Rendering/Win2D/Win2DLineLayout.cs` — change: implement it with `CanvasTextLayout.HitTest(x, y)` (character index + `isTrailingHit`)
- `tests/LoomEditor.Rendering.Tests/TestHelpers/FakeLineLayout.cs` — change: implement it (nearest boundary, tabs included)
- `src/LoomEditor.View/Controller/MouseTarget.cs` — create: `MouseTargetType`, `MouseTarget`
- `src/LoomEditor.View/Controller/MouseTargetFactory.cs` — create: the hit test, with an `ILineHitTester` dependency
- `src/LoomEditor.View/Controller/ILineHitTester.cs` — create
- `src/LoomEditor.Rendering/TextViewRenderer.HitTest.cs` — create (partial): implements `ILineHitTester` with the layout cache
- `tests/LoomEditor.View.Tests/Controller/MouseTargetFactoryTests.cs`, `tests/LoomEditor.Rendering.Tests/TextViewRendererHitTestTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `MouseTargetType` (C# enum, Monaco's names and values for the members we use): `Unknown = 0`, `GutterGlyphMargin = 2`,
  `GutterLineNumbers = 3`, `GutterLineDecorations = 4`, `ContentText = 6`, `ContentEmpty = 7`, `OutsideEditor = 13`
  (used by mouse drags in P3-45; values checked against `editorBrowser.ts` line 358); scrollbars, widgets, view zones, minimap and overlays are not hit-tested on the canvas (the XAML
  scrollbars receive their own pointer events).
- `MouseTarget` = `internal sealed record MouseTarget(MouseTargetType Type, Position? Position, int MouseColumn, Range? Range, bool IsAfterLines, double? HorizontalDistanceToText)`.
- `ILineHitTester`: `double GetLineWidth(int lineNumber)` and `Position? GetPositionAtCoordinate(int lineNumber, double mouseContentHorizontalOffset)`.
  `TextViewRenderer` implements it with the `LineLayoutCache` (same layouts as the drawing; lines longer than 10,000
  characters are cut as when drawing, ADR 0005).
- `MouseTargetFactory.CreateMouseTarget(ViewModel viewModel, ILineHitTester lines, double x, double y)`: port
  `_hitTestMargin` and the GPU branch of `_hitTestViewLines`; there is no DOM, so the `ElementPath` checks become
  geometry: `x < layoutInfo.ContentLeft` → margin; otherwise view lines. No RTL (`isRtl` is false).
- `_getMouseColumn`: `Math.round` with JavaScript semantics (conventions §4.1).
- `ILineLayout.HitTest`: for a hit on the trailing half of a character return the index **after** it; never return an
  offset inside a surrogate pair (DirectWrite clusters already guarantee it; assert it in Debug).

## Tests

No Monaco test file covers `mouseTarget.ts`. New tests with a fake `ILineHitTester` (fixed 10-DIP characters) and `TestCodeEditor`:

- [x] New: `x` in the line numbers → `GutterLineNumbers`, position at column 1 of the line under `y`
- [x] New: `y` above the first line (top padding) → `ContentEmpty` at `(1,1)`; below the last line → `ContentEmpty` at the
      last line's max column with `IsAfterLines`
- [x] New: on an empty line → `ContentEmpty` at column 1; beyond the end of a line → `ContentEmpty` at the max column
- [x] New: on text, `x` at 12 DIPs into `"abc"` → `ContentText` at column 2 (nearest boundary 10); at 16 → column 3
- [x] New: with `scrollTop`/`scrollLeft` set, the same screen point maps to the scrolled position
- [x] New: `_getMouseColumn(25, 10)` → 4 (`Math.round(2.5) = 3`, JavaScript rounding)
- [x] New (Rendering): `FakeLineLayout.HitTest` and `TextViewRenderer.GetPositionAtCoordinate` agree with `GetCharacterX`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `editor/browser/controller/mouseTarget.ts` (🔶 canvas subset, P3-43); behavior difference
      "hit testing is geometric on the canvas (no DOM)"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Click handling (P3-44, P3-45), the WinUI pointer events (P3-46).

## Suggested commit message

`feat(view): hit-test mouse positions on the canvas`
