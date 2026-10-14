# P2-12 — Real text layout: `ITextMeasurer`, `ILineLayout`, DirectWrite

- **Status:** todo
- **Depends on:** P2-11
- **Size:** M

## Goal

Replace the phase 0 assumption "every character is as wide as `M`" with real text layout. Each line becomes a
DirectWrite text layout. Tabs use tab stops, wide characters and proportional fonts get their real width, and the
x position of any character can be queried. The font is measured with Monaco's rules (`FontMeasurements`, P2-06)
and gives a `FontInfo`.

## Monaco sources

None to port in this card. It uses `FontMeasurements` and `FontInfo` from P2-06. The vertical centering below follows
the CSS rule Monaco relies on (`line-height` puts half of the extra space above the glyphs).

## Files to create or change

- `src/LoomEditor.Rendering/ITextMeasurer.cs`, `ILineLayout.cs` — create
- `src/LoomEditor.Rendering/IRenderSurface.cs` — remove `FontMetrics` and `DrawText`; add `DrawLineLayout` and `DrawRectangle` (see below)
- `src/LoomEditor.Rendering/FontMetrics.cs` (or the record inside `IRenderSurface.cs`) — delete
- `src/LoomEditor.Rendering/Win2D/Win2DTextMeasurer.cs` — create (replaces `Win2DFont.cs`, which is deleted)
- `src/LoomEditor.Rendering/Win2D/Win2DLineLayout.cs` — create
- `src/LoomEditor.Rendering/Win2D/Win2DRenderSurface.cs` — update
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — update to draw through `ILineLayout` (same output as before: first lines, gutter, selection)
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — create a `Win2DTextMeasurer` instead of a `Win2DFont`
- `tests/LoomEditor.Rendering.Tests/TestHelpers/FakeTextMeasurer.cs`, `FakeLineLayout.cs` — create (fixed width per character; `\t` jumps to the next tab stop)
- `tests/LoomEditor.Rendering.Tests/TextViewRendererTests.cs` — update the characterization tests to the new calls

## Design decisions (already made — do not change)

- Interfaces (all `internal`):
  ```csharp
  internal interface ITextMeasurer : IDisposable
  {
      FontInfo FontInfo { get; }          // LoomEditor.View.Layout.FontInfo (P2-06)
      int TabSize { get; }
      ILineLayout CreateLineLayout(string text);
  }

  internal interface ILineLayout : IDisposable
  {
      string Text { get; }
      double Width { get; }               // advance width of the whole text, in DIPs
      double GetCharacterX(int index);    // x of the leading edge of the UTF-16 unit at index; index == Text.Length → Width
  }
  ```
- `IRenderSurface` becomes: `Clear`, `FillRectangle`, `DrawRectangle(float x, float y, float width, float height, ThemeColor color, float strokeWidth)`,
  `DrawLineLayout(ILineLayout layout, float x, float lineTop, ThemeColor color)`. `lineTop` is the top of the line box
  (`FontInfo.LineHeight` high). The implementation centers the glyphs in it, half the extra space above and half below.
- `Win2DTextMeasurer(ICanvasResourceCreator resourceCreator, string fontFamilies, double fontSize, int tabSize, double pixelRatio)`:
  - keeps phase 0's `ResolveInstalledFamily` (first installed family of the comma-separated list);
  - one `CanvasTextFormat` with `WordWrapping = NoWrap`, `Options = EnableColorFont`, and
    `IncrementalTabStop = tabSize × spaceWidth` (ADR 0005, decision 5);
  - measures with `FontMeasurements.Read(BareFontInfo.Create(...), reader, pixelRatio)`. The reader measures one
    character with a `CanvasTextLayout` (`LayoutBounds.Width`), and uses bold / italic formats for
    `CharacterStyle.Bold` / `Italic`;
  - computes the glyph top offset once: `(FontInfo.LineHeight - layoutHeightOf("n")) / 2`.
- `Win2DLineLayout` wraps one `CanvasTextLayout`. `GetCharacterX` uses `GetCaretPosition(index, false)`. Disposing
  it disposes the layout.
- `TextViewRenderer` keeps its phase 0 behavior in this card (no scrolling yet). The gutter width is computed from
  `FontInfo.MaxDigitWidth` instead of `CharacterWidth`, and line numbers are right-aligned using `ILineLayout.Width`.
  It creates and disposes layouts in each frame (caching comes in P2-13).

## Tests

- [ ] Update: the characterization tests of P2-11 now check `DrawLineLayout` calls
- [ ] New: with `FakeTextMeasurer` (char width 10, tab size 4), the selection rectangle of `"\tab"` from column 2 to 3
      starts at `x = textLeft + 40` (tab) and is 10 wide
- [ ] Manual: Gallery shows the sample text correctly with tabs (add a tab-indented line to `SampleText.HelloWorld` if
      there is none) and with a CJK line (`你好，世界`), each character taking its real width

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the app starts
- [ ] `docs/PORTING.md`: "Behavior differences": tabs use DirectWrite tab stops (ADR 0005, decision 5)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Layout cache (P2-13), scrolling (P2-14), font changes at run time (P2-20).

## Suggested commit message

`feat(rendering): lay out lines with DirectWrite instead of a fixed character width`
