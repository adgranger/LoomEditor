# P15-06 — `AvaloniaTextMeasurer` and `AvaloniaLineLayout`

- **Status:** todo
- **Depends on:** P15-05
- **Size:** M

**Recommended model:** Opus (every x position of the editor comes from these numbers)

## Goal

`ITextMeasurer` and `ILineLayout` on Avalonia's text stack. They give the same `FontInfo` measurement path, tab stops, styles, caret positions and hit testing as the Win2D
twins.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Rendering/Win2D/Win2DTextMeasurer.cs`, `Win2DLineLayout.cs` (P15-01 moved them). Also read: `src/LoomEditor.Rendering/ITextMeasurer.cs`,
`ILineLayout.cs`, `LineStyle.cs`, `FontFeatureTags.cs`, `src/LoomEditor.View/Layout/FontMeasurements.cs`, `BareFontInfo.cs`, `ICharacterWidthReader`,
[ADR 0013](../../adr/0013-text-options-and-snippets.md) decisions on font options, [ADR 0005](../../adr/0005-viewport-rendering.md) decision 5 (tabs).

## Files to create or change

- `src/LoomEditor.Avalonia/Rendering/AvaloniaTextMeasurer.cs` — create (twin of `Win2DTextMeasurer`)
- `src/LoomEditor.Avalonia/Rendering/AvaloniaLineLayout.cs` — create (twin of `Win2DLineLayout`)
- `src/LoomEditor.View/Layout/BareFontInfo.cs` — add `DefaultMacFontFamily`, `DefaultLinuxFontFamily` ([README](README.md) decision 8; port the two Monaco constants next to the one
  `DefaultWindowsFontFamily` ports, with the file and line in the XML doc) and `static string DefaultFontFamilyForCurrentOS`
- `tests/LoomEditor.Avalonia.Tests/Rendering/AvaloniaTextMeasurerTests.cs`, `AvaloniaLineLayoutTests.cs` — create
- `tests/LoomEditor.Avalonia.Tests/TestFonts/` — a monospace test font with ligatures, under an OFL licence (for example JetBrains Mono, with its `OFL.txt`), embedded as
  `avares://` so that tests do not depend on the fonts installed on the CI machine

## Design decisions (already made — do not change)

- **Text:** one `Avalonia.Media.TextFormatting.TextLayout` per line. Use the constructor overload that takes a `TextParagraphProperties`
  (`GenericTextParagraphProperties`) with `DefaultIncrementalTab = tabSize × FontInfo.SpaceWidth`, `TextWrapping.NoWrap`. The style runs become
  `ValueSpan<TextRunProperties>` overrides: foreground `ImmutableSolidColorBrush`, `FontStyle.Italic`, the bold weight `max(700, baseWeight)` (as Win2D),
  `TextDecorations.Underline` / `Strikethrough`.
- **Font features:** a `FontFeatureCollection` built from `FontFeatureSettings.Parse` on every run. **Letter spacing:** the layout's letter-spacing argument. If the pinned
  Avalonia version has no API for one of them, leave it out, document it in the XML doc and in `docs/PORTING.md` "Behavior differences", and say so in the Result.
- **Family:** Avalonia accepts `"A, B"` fallback lists, but `FontMeasurements` needs **one** family name (same reason as Win2D's `ResolveInstalledFamily`). So pick the first name
  found in `FontManager.Current.SystemFonts` (or an embedded `avares://` family the host registered). If none is found, use the first name of
  `BareFontInfo.DefaultFontFamilyForCurrentOS` that exists, else `FontManager.Current.DefaultFontFamily`.
- **Width and caret:** `Width` = caret x after the last character (`HitTestTextPosition(text.Length).X`), trailing spaces included. `GetCharacterX(i)` =
  `HitTestTextPosition(i).X`. `CharacterWidthReader.GetWidth` = the same caret x on a one-character layout.
- **HitTest:** `HitTestPoint(new Point(x, 0))` → `TextPosition` (+1 when `IsTrailing`, clamped). The ligature rule of the Win2D twin is kept as is (`IsSplittableCluster`,
  `NearestOffset`), copied.
- `GlyphTopOffset` = `(LineHeight − layout("n").Height) / 2`, as Win2D.
- `pixelRatio` is passed in (the control gives `TopLevel.RenderScaling` in P15-08).

## Tests

- [ ] New: `FontInfo_TestFont_MatchesMeasuredSpaceWidth`; `Width_TrailingSpaces_Counted`; `Tab_AtColumn0_AdvancesOneTabStop`; `Tab_AfterTwoChars_AdvancesToNextStop`
  (tabSize 4); `GetCharacterX_IsMonotonic` (ASCII, CJK `"日本語"`, emoji `"a👍b"`); `HitTest_LeftAndRightHalf_OfAChar`; `HitTest_InsideLigature_PicksNearestOffset`
  (`"=>"` with `calt` on); `HitTest_Emoji_NeverSplitsSurrogates`; `Bold_UsesWeight700AtLeast` (width of `"m"` bold ≥ regular); `EmptyLine_HasZeroWidth`
- [ ] New (View): `DefaultFontFamilyForCurrentOS_IsOneOfTheThree`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green; Avalonia tests green in the three CI jobs
- [ ] `docs/PORTING.md` map and "Behavior differences" updated if anything differs
- [ ] Card status, `AvaloniaParityPending.txt` (no public key here) and `docs/tasks/README.md` updated

## Out of scope

Drawing (P15-07), the control (P15-08).

## Suggested commit message

`feat(avalonia): measure and lay out text with Avalonia's TextLayout`
