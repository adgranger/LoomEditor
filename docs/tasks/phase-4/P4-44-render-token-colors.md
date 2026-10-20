# P4-44 — Rendering: draw token colors and font styles

- **Status:** done
- **Depends on:** P4-43
- **Size:** M

## Goal

The renderer draws each token with its theme color and font style (italic, bold, underline, strikethrough). Lines whose
text and token styles did not change keep their cached layout.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/common/viewLayout/viewLineRenderer.ts` | 582–609 | `transformAndRemoveOverflowing` — **inspiration** only (`// Inspired by`): tokens become runs cut at the rendered length |
| vscode: `src/vs/editor/common/encodedTokenAttributes.ts` | 113–124, 171–183 | `getFontStyle`, `getForeground`, `getPresentationFromMetadata` (already in Core, P4-23) |

## Files to create or change

- `src/LoomEditor.Rendering/LineStyle.cs` — create (`readonly record struct StyleRun(int Start, int Length, uint ForegroundArgb, FontStyle FontStyle)`, and `LineStyle` = the runs of a line with sequence equality and hash)
- `src/LoomEditor.Rendering/ITextMeasurer.cs` — change: `CreateLineLayout(string text, LineStyle style)`
- `src/LoomEditor.Rendering/LineLayoutCache.cs` — change: key = text + `LineStyle`
- `src/LoomEditor.Rendering/Win2D/Win2DTextMeasurer.cs` — change: `CanvasTextLayout.SetColor`, `SetFontStyle`,
  `SetFontWeight`, `SetUnderline`, `SetStrikethrough` per run
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — change: build the `LineStyle` of each visible line from the view
  model's tokens and the color map
- `tests/LoomEditor.Rendering.Tests/LineStyleTests.cs`, `LineLayoutCacheTests.cs` (change), `TextViewRendererTests.cs` (change) — tests

## Design decisions (already made — do not change)

- ADR 0008 decision 9. Consecutive tokens with the same foreground and font style are merged into one run (fewer
  DirectWrite ranges); a line with one run of the default foreground is the plain-text style `LineStyle.Plain`, so plain
  text keeps today's behavior and cache keys.
- The color map comes from `ViewModel`/`SyntaxHighlighter` as `IReadOnlyList<uint>` (P4-23); the default foreground is
  the theme's `Foreground`.
- Very long lines (`OverflowLabel`, P2-13): runs past the rendered length are dropped, like `transformAndRemoveOverflowing`.
- The renderer does not tokenize: it only reads tokens.

## Tests

New (with the fake measurer of the Rendering tests, which records the style it was given):

- [x] `LineStyle_MergesRunsWithSameStyle`
- [x] `LineStyle_PlainLine_IsPlain`
- [x] `Cache_SameTextDifferentStyle_CreatesNewLayout`, `…_SameTextSameStyle_ReusesLayout`
- [x] `Render_TokensOfVisibleLines_AreGivenToTheMeasurer`
- [x] `Render_LongLine_RunsAreCutAtRenderedLength`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The control's `Language` / `Theme` (P4-45). Selection foreground colors, whitespace rendering (phase 6).

## Suggested commit message

`feat(rendering): draw token colors and font styles`
