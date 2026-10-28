# P10-05 — Control properties for the font options, and Gallery switches

- **Status:** done
- **Depends on:** P10-04
- **Size:** S

## Goal

`LoomEditor` has the properties `FontLigatures`, `FontFeatureSettings`, `FontVariationSettings` and `LetterSpacing`, honors the inherited `FontWeight`, and the Gallery can switch them: an application can turn ligatures on, set a weight and a letter spacing.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

Sources: none (specified in this card; the options are `fontLigatures`, `fontVariations`, `fontWeight`, `letterSpacing` of `editorOptions.ts`, validated by P10-03).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.FontOptions.cs` — create: the dependency properties and `CurrentFontOptions`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Font.cs` — change: `InitializeFont` registers `FontWeightProperty` like `FontFamilyProperty`; `CreateTextMeasurer` passes `CurrentFontOptions` (the zoom level stays `0` until P10-06)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`)
- `samples/LoomEditor.Gallery/MainWindow.xaml`, `MainWindow.xaml.cs` — change (below)
- `docs/testing/phase-10-manual-tests.md` — create if missing, add section "P10-05"

## Design decisions (already made — do not change)

- Properties (follow `LoomEditor.WordWrap.cs` for the pattern, validation and `PropertyChangedCallback`):
  `FontLigatures` (`bool`, default `false`), `FontFeatureSettings` (`string`, default `""`; when not empty it **replaces** `FontLigatures`: the effective value is `EditorFontOptions.ValidateLigatures(FontFeatureSettings)`, otherwise `ValidateLigatures(FontLigatures ? "true" : "false")`), `FontVariationSettings` (`string`, default `""`, `""` is off: the effective value is `ValidateVariations(value == "" ? "false" : value)`), `LetterSpacing` (`double`, default `0`; a `NaN` or infinite value throws `ArgumentOutOfRangeException` and the previous value is kept).
  `FontWeight` is the inherited XAML property (`Windows.UI.Text.FontWeight`): the effective string is `"normal"` for 400, `"bold"` for 700, else `Weight.ToString()`, then `ValidateWeight`.
- Every change calls `RecreateTextMeasurer()` (it returns early while the canvas does not exist yet).
- `CurrentFontOptions` (private) builds the `EditorFontOptions` from the properties.
- XML docs: say that only the `wght` axis of the variations is applied and that ligature support depends on the font (Cascadia Code, Fira Code, JetBrains Mono have them; Consolas does not).
- **Gallery:** in the font section next to `FontFamilyBox` add the item `Cascadia Code` to the font family box (as the existing items), a `ToggleSwitch` "Ligatures" (`FontLigaturesSwitch` → `Editor.FontLigatures`), a `ComboBox` "Font weight" (Normal 400, Medium 500, SemiBold 600, Bold 700 → `Editor.FontWeight`) and a `Slider` "Letter spacing" 0–5 step 0.5 (`Editor.LetterSpacing`). Handlers in `MainWindow.xaml.cs` are one-liners like `OnWordWrapToggled`. The sample text must contain a line with `=> != -> <= >= == === !== |> <|> www` (add it to the end of an existing sample in `SampleText.cs` if no sample has it).

## Tests

- [x] No automated test (control + DirectWrite). Validation of the strings is covered by P10-03.
- [x] Manual (section "P10-05" of the manual plan): the checks of P10-04, plus font weight 700 on Cascadia Code is visibly bolder, letter spacing 5 widens and the caret / selection stay aligned, switching a property keeps the scroll position (`RecreateTextMeasurer` does that).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Public API file updated; `docs/PORTING.md` not needed (no port)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Zoom (P10-06), per-token fonts, the minimap font, a settings object.

## Suggested commit message

`feat(winui): add the font ligatures, feature settings, variations and letter spacing properties`
