# P10-03 — Font options: weight, letter spacing, ligatures, variations (the model)

- **Status:** done
- **Depends on:** P10-02
- **Size:** M

## Goal

`EditorFontOptions` and the parsing of the CSS-like font feature / variation strings exist in `LoomEditor.View`, `BareFontInfo` / `FontInfo` carry them, and `FontMeasurements` sets `IsMonospace` to `false` when ligatures are on. Nothing is drawn differently yet (P10-04).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/config/editorOptions.ts` | 1918–1966 | `EditorFontLigatures` (`OFF`, `ON`, `validate`) |
| `src/vs/editor/common/config/editorOptions.ts` | 1970–2030 | `EditorFontVariations` (`OFF`, `TRANSLATE`, `validate`) |
| `src/vs/editor/common/config/editorOptions.ts` | 2167–2203 | `EditorFontWeight.validate` (`normal`, `bold`, else an int clamped to 1–1000, default `normal`) |
| `src/vs/editor/common/config/fontInfo.ts` | 30–75 | `BareFontInfo._create`: the `FONT_VARIATION_TRANSLATE` branch (weight → `'wght' N`, weight `normal`), the fields `fontFeatureSettings`, `fontVariationSettings` |
| `src/vs/editor/browser/config/fontMeasurements.ts` | 215–250 | `isMonospace = (fontFeatureSettings === EditorFontLigatures.OFF)` and the fields copied into `FontInfo` |
| `src/vs/editor/test/browser/config/fontMeasurements.test.ts` | whole file | the tests that apply (read it; port the ones about ligatures / monospace) |

## Files to create or change

- `src/LoomEditor.View/Layout/EditorFontOptions.cs` — create: the record, `Validate*` statics and the constants
- `src/LoomEditor.View/Layout/FontFeatureSettings.cs` — create: `Parse(string) → IReadOnlyList<FontFeature>`, `FontFeature(string Tag, int Value)`
- `src/LoomEditor.View/Layout/BareFontInfo.cs`, `FontInfo.cs`, `FontMeasurements.cs` — change
- `tests/LoomEditor.View.Tests/Layout/EditorFontOptionsTests.cs`, `FontFeatureSettingsTests.cs`, and the existing `FontMeasurements` tests — create / change

## Design decisions (already made — do not change)

- `internal sealed record EditorFontOptions(string FontWeight = "normal", double LetterSpacing = 0, string FontFeatureSettings = Off, string FontVariationSettings = "normal")` with `public const string LigaturesOff = "\"liga\" off, \"calt\" off"`, `LigaturesOn = "\"liga\" on, \"calt\" on"`, `VariationsOff = "normal"`, `VariationsTranslate = "translate"`.
  `public static EditorFontOptions Default { get; } = new();`.
  Static `ValidateLigatures(string? input)` (`null` → off; `"false"` or `""` → off; `"true"` → on; any other string unchanged), `ValidateVariations(string? input)` (`"false"` → off, `"true"` → translate), `ValidateWeight(string? input)` (as Monaco; a non-number gives `"normal"`). They take strings only: the control converts its `bool` to `"true"` / `"false"`.
- `FontFeatureSettings.Parse` accepts what CSS accepts, **no more**: a comma-separated list of `'tag'` or `"tag"` (exactly 4 ASCII characters) optionally followed by `on`, `off` or a non-negative integer (default 1; `on` = 1, `off` = 0). A malformed item is skipped (never throws). The result keeps the order. `normal` or an empty string gives an empty list (and the font's own defaults).
- `BareFontInfo` gets two new positional members **after** `LetterSpacing`: `string FontFeatureSettings`, `string FontVariationSettings`; `Create` gets the two parameters (and `double zoomLevel = 0`: applies `1 + zoomLevel * 0.1` to `fontSize` and `lineHeight` **after** the rounding, exactly as `fontInfo.ts` 48–50; P10-06 passes the real value). `Create` also does the `translate` branch: weight `normal` / `bold` → variation settings `normal`; a numeric weight → `'wght' <n>` and weight `normal`. Update every caller (`grep -rn "BareFontInfo.Create\|new FontInfo(" src tests`): they pass `LigaturesOff` / `VariationsOff`.
- `FontInfo.IsMonospace` is computed as in Monaco: starts at `FontFeatureSettings == LigaturesOff` (note: only the exact off string counts, so `"liga" 0, "calt" 0` is "ligatures on" for this test; document it in the XML doc). Remove the comment "LoomEditor has no ligature option yet…" in `FontMeasurements.Read`.
- Threading remarks: immutable.

## Tests

- [x] Port: the ligature / monospace cases of `fontMeasurements.test.ts` that can run on a fake `ICharacterWidthReader`
- [x] New: `ValidateLigatures_*` (null, `""`, `"false"`, `"true"`, `"'ss01' 1"`), `ValidateVariations_*`, `ValidateWeight_*` (`normal`, `bold`, `"700"`, `"0"` → `"1"`, `"2000"` → `"1000"`, `"abc"` → `"normal"`)
- [x] New: `Parse_*` (`'liga' on, "calt" 0` → two items; `'ss01'` alone → value 1; `'abc'` skipped; `'liga' -1` skipped; unterminated quote skipped; empty → empty)
- [x] New: `Create_TranslateVariations_NumericWeight_GivesWght`, `Create_ZoomLevel_ScalesSizeAndLineHeight` (level 5 → ×1.5), `Read_LigaturesOn_IsNotMonospace`, `Read_LigaturesOff_MonospaceFont_IsMonospace`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: rows for `editorOptions.ts` (font options), `fontInfo.ts`, `fontMeasurements.ts` updated; "Behavior differences": `IsMonospace` uses the exact off string
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

DirectWrite (P10-04), the control (P10-05), the zoom state (P10-06: here `zoomLevel` is only a parameter), `fullwidthCharacterWidth`.

## Suggested commit message

`feat(view): add the font options (weight, letter spacing, ligatures, variations)`
