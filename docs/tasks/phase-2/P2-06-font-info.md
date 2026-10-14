# P2-06 — `FontInfo` and font measurements

- **Status:** todo
- **Depends on:** P2-05
- **Size:** M

## Goal

Port Monaco's font description: `BareFontInfo` (what the user asked for, with the line-height rule) and `FontInfo`
(what was measured: typical character widths, space width, max digit width, monospace or not). The *measuring* logic
is ported too, behind a small interface. Rendering implements that interface with DirectWrite in P2-12, and the tests
use a fake.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/config/fontInfo.ts` | 14–19 | `GOLDEN_LINE_HEIGHT_RATIO` (**Windows value: 1.35**), `MINIMUM_LINE_HEIGHT` |
| same | 28–137 | `BareFontInfo` (`_create`: the line-height rule; keep `fontFamily`, `fontWeight`, `fontSize`, `lineHeight`, `letterSpacing`) |
| same | 141–211 | `FontInfo` (all measured fields, `equals`) |
| same | 222, 234–244 | `DEFAULT_WINDOWS_FONT_FAMILY`, `EDITOR_FONT_DEFAULTS` (Windows values) |
| `src/vs/editor/browser/config/fontMeasurements.ts` | 140–170 | `readFontInfo`: the "measured width <= 2 → retry / use at least 5" fallback |
| same | 179–254 | `_actualReadFontInfo`: which characters are measured, `maxDigitWidth`, the monospace test, `canUseHalfwidthRightwardsArrow` |

## Files to create or change

- `src/LoomEditor.View/Layout/BareFontInfo.cs`, `FontInfo.cs` — create
- `src/LoomEditor.View/Layout/ICharacterWidthReader.cs` — create
- `src/LoomEditor.View/Layout/FontMeasurements.cs` — create (`static FontInfo Read(BareFontInfo, ICharacterWidthReader, double pixelRatio)`)
- `tests/LoomEditor.View.Tests/Layout/FontInfoTests.cs`, `FontMeasurementsTests.cs` — create
- `tests/LoomEditor.View.Tests/Layout/TestHelpers/FakeCharacterWidthReader.cs` — create

## Design decisions (already made — do not change)

- All types `internal`. `BareFontInfo` is a `record` and `FontInfo` a `sealed record` deriving from it, like Monaco.
  Sizes are `double`, `LineHeight` is `int`.
- **Not ported:** `fontFeatureSettings`, `fontVariationSettings`, editor zoom (`ignoreEditorZoom`, `EditorZoom`),
  serialization (`SERIALIZED_FONT_INFO_VERSION`), the measurement cache. So `isMonospace` starts at `true`, because
  Monaco starts it at "ligatures off" and LoomEditor has no ligature option yet.
- `BareFontInfo.Create(string fontFamily, string fontWeight, double fontSize, double lineHeight, double letterSpacing, double pixelRatio)`
  applies Monaco's rule exactly: `0` → `1.35 × fontSize`; `< 8` → multiplier of the font size; then `Math.Round`
  (AwayFromZero); then at least 8. Example: 14 px → 19.
- `ICharacterWidthReader`: `double GetWidth(string character, CharacterStyle style)` with
  `enum CharacterStyle { Regular, Italic, Bold }` (Monaco's `CharWidthRequestType`).
- `FontMeasurements.Read` measures the same characters as Monaco, in the same order, and applies the same fallback
  (lines 145–166): if a key width is `<= 2`, use `Math.Max(width, 5)`. Monaco first clears its cache and measures again;
  LoomEditor has no cache, so the second read is not ported. Document this in the XML doc.
- LoomEditor's own default font stays `"Cascadia Mono, Consolas, Courier New"` at 14 DIPs (phase 0 decision). The
  Monaco defaults are ported as constants but not used by the control.

## Tests

Monaco has no common test for these files (`fontMeasurements.test.ts` needs a browser). New tests:

- [ ] New: line height rule: `(14, 0) → 19`, `(14, 1.5) → 21`, `(14, 22) → 22`, `(4, 0) → 8` (minimum)
- [ ] New: a fake monospace reader (every character 8.4 wide, full-width 16.8) gives `IsMonospace == true`,
      `TypicalHalfwidthCharacterWidth == 8.4`, `MaxDigitWidth == 8.4`
- [ ] New: a fake proportional reader (`i` narrower than `m`) gives `IsMonospace == false`; `MaxDigitWidth` is the widest digit
- [ ] New: a reader returning 1 for `n` gives `TypicalHalfwidthCharacterWidth == 5` (fallback)
- [ ] New: `canUseHalfwidthRightwardsArrow` is false when the half-width arrow is wider than the regular arrow

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `editor/common/config/fontInfo.ts` ✅ (subset), `editor/browser/config/fontMeasurements.ts` ✅ adapted (measurement logic only)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Measuring with DirectWrite (P2-12); font ligatures and variations; editor zoom.

## Suggested commit message

`feat(view): port FontInfo and font measurement logic`
