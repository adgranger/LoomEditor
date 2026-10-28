# P10-04 — Font options in DirectWrite: weight, letter spacing, ligatures

- **Status:** todo
- **Depends on:** P10-03
- **Size:** M

## Goal

`Win2DTextMeasurer` applies `EditorFontOptions` (weight, letter spacing, typography features, the `wght` variation) to the text format, to every line layout and to the character width measurements, so a font such as Cascadia Code draws `=>` and `!=` as ligatures when asked to and as separate characters when not.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

Sources: none (specified in this card; the options are P10-03, the decisions are ADR 0013 decisions 2 to 5).

## Files to create or change

- `src/LoomEditor.Rendering/Win2D/FontFeatureTags.cs` — create: `static uint Pack(string tag)` = `tag[0] | tag[1] << 8 | tag[2] << 16 | tag[3] << 24` (DirectWrite's `DWRITE_MAKE_FONT_FEATURE_TAG`); `Pack` throws `ArgumentException` unless the tag is 4 ASCII characters
- `src/LoomEditor.Rendering/Win2D/Win2DTextMeasurer.cs` — change: a new constructor parameter `EditorFontOptions fontOptions` and `double zoomLevel = 0` (after `pixelRatio`); `BareFontInfo.Create` receives them
- `src/LoomEditor.Rendering/Win2D/Win2DLineLayout.cs` — change: `HitTest` refinement (below)
- `src/LoomEditor.WinUI/Controls/LoomEditor.Font.cs` — change: `CreateTextMeasurer` passes `EditorFontOptions.Default` and `0` for now (P10-05 passes the real values) — the build must stay green
- `tests/LoomEditor.Rendering.Tests/FontFeatureTagsTests.cs` — create

## Design decisions (already made — do not change)

- **Typography.** In `CreateLineLayout`, build **one** `CanvasTypography` per measurer (cached in a field, disposed in `Dispose`): for each `FontFeature` of `FontFeatureSettings.Parse(options.FontFeatureSettings)` add `new CanvasTypographyFeature((CanvasTypographyFeatureName)FontFeatureTags.Pack(tag), (uint)value)`. Then `textLayout.SetTypography(0, text.Length, _typography)` on every layout (skip when `text.Length == 0`). The cast works because Win2D's enum values are the packed tags; the test checks `Pack("liga") == (uint)CanvasTypographyFeatureName.StandardLigatures` and `Pack("calt") == (uint)CanvasTypographyFeatureName.ContextualAlternates`.
- **Weight.** The base weight is `FontWeight` of the options (`normal` = 400, `bold` = 700, else the number, clamped to 1–999 for DirectWrite). `CreateFormat` uses it for the regular style; a bold token (`FontStyle.Bold`) uses `Math.Max(700, base)`. The `'wght' N` variation (`FontVariationSettings` after `BareFontInfo.Create`'s translation, P10-03) is applied by **parsing it with a small private `TryGetWght`** and using N as the base weight; every other axis is ignored (document in the XML doc).
- **Letter spacing.** When `LetterSpacing != 0`: `textLayout.SetCharacterSpacing(0, text.Length, 0, (float)LetterSpacing, 0)` on every layout, and `CharacterWidthReader.GetWidth` calls the same method on the one-character layout (so `FontInfo` widths include the spacing and `IncrementalTabStop = tabSize × SpaceWidth` stays right). `GlyphTopOffset` is unaffected.
- **Measuring the width table with ligatures** needs nothing special: the reader measures single characters.
- **`Win2DLineLayout.HitTest` inside a ligature.** DirectWrite returns a whole cluster, so a click on the `>` of a `=>` ligature would land before or after the pair. After `TextLayout.HitTest`, when `region.CharacterCount > 1` **and** every UTF-16 unit of the cluster is a BMP character that is not a surrogate and whose `CharUnicodeInfo.GetUnicodeCategory` is not `NonSpacingMark`, `SpacingCombiningMark` or `EnclosingMark` (so graphemes, emoji and combining sequences are **not** split), return the offset in `[CharacterIndex, CharacterIndex + CharacterCount]` whose `GetCharacterX` is closest to `x` (ties → the lower offset). Otherwise keep the current behavior. Keep the existing `Debug.Assert`.
- Nothing else in the renderer changes: caret, selection and decorations already use `GetCharacterX`.

## Tests

- [ ] New: `FontFeatureTagsTests`: `Pack_Liga_EqualsStandardLigatures`, `Pack_Calt_EqualsContextualAlternates`, `Pack_Ss01_EqualsStylisticSet1`, `Pack_NotFourCharacters_Throws`, `Pack_NonAscii_Throws`
- [ ] No automated test creates a Win2D device in this repository: the rest is verified by the manual checks below (P10-05 adds the Gallery switches).

## Manual checks (add to `docs/testing/phase-10-manual-tests.md`, section "P10-04")

With Cascadia Code installed (`Cascadia Code` is on every Windows 11), in the Gallery once P10-05 exists: ligatures on → `=>`, `!=`, `->`, `<=` are fused; off → separate characters. Caret: `Left` / `Right` across `=>` takes two key presses and the caret is drawn between the two halves. Click on the right half of `=>` places the caret between `=` and `>`. Letter spacing 2 widens the text, the caret and the selection follow.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] "Behavior differences" row in `docs/PORTING.md`: only the `wght` axis is applied; ligature caret positions are DirectWrite's
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The control properties (P10-05), zoom (P10-06), the minimap font (it keeps its own sampler), word wrap exactness with ligatures.

## Suggested commit message

`feat(rendering): apply the font options in DirectWrite (weight, letter spacing, ligatures)`
