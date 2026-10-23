# P7-20 — Contrast ratio helper and the contrast of the bundled themes

- **Status:** todo
- **Depends on:** P7-19
- **Size:** S

## Goal

The WCAG contrast ratio between two theme colors can be computed, and a test pins that the bundled high contrast themes (and the two default Modern themes) keep a readable editor foreground on the editor background.

## Sources

WCAG 2.1 "contrast ratio": `(L1 + 0.05) / (L2 + 0.05)` with `L1 >= L2` the relative luminances. The luminance itself is `Color.GetRelativeLuminance()` (`src/LoomEditor.Themes/Color.cs`, port of Monaco's `color.ts`), already in the repository. No Monaco counterpart.

## Files to create or change

- `src/LoomEditor.Themes/HighContrast/ContrastRatio.cs` — create (`internal static class ContrastRatio`; namespace `LoomEditor.Themes.HighContrast`)
- `tests/LoomEditor.TextMate.Tests/Themes/ContrastRatioTests.cs` — create (the Themes tests live in `LoomEditor.TextMate.Tests`: `LoomEditor.Themes` has `InternalsVisibleTo` for it)
- `tests/LoomEditor.TextMate.Tests/Themes/BuiltInThemesContrastTests.cs` — create

## Design decisions (already made — do not change)

- Everything `internal` (no public API change): `internal static double Between(ThemeColor foreground, ThemeColor background)`:
  1. flatten the foreground over the background when it is translucent: each channel `c = fg.c * a + bg.c * (1 - a)` with `a = fg.A / 255.0` (the background is taken as opaque: its alpha is ignored);
  2. `l1 = Color.FromThemeColor(flattenedForeground).GetRelativeLuminance()`, `l2` the same for the background;
  3. `(Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05)`.
  `GetRelativeLuminance` rounds to 4 decimals: the result is therefore approximate; tests use tolerances (below), never exact equality except for `1` and `21`.
- Constants: `internal const double AaNormalText = 4.5;`, `internal const double AaaNormalText = 7.0;`, `internal const double NonText = 3.0;`.
- The built-in themes are read with `EditorTheme.BuiltIn` (ten themes: index 8 = "Dark High Contrast", 9 = "Light High Contrast", 3 = "Dark Modern", 5 = "Light Modern"; select by `Name`, not index).
- No other theme is asserted: some bundled themes (the 2026 pair, the Visual Studio ones) were never meant to reach AAA and the test would be about VS Code's colors, not ours.

## Tests

- [ ] New (`ContrastRatioTests`): `Between_BlackOnWhite_Is21` (`Assert.Equal(21, ratio, precision: 1)`), `Between_WhiteOnBlack_IsSymmetric`, `Between_SameColor_IsOne`, `Between_TranslucentForeground_IsFlattenedOverTheBackground`
      (`#FFFFFF80` over `#000000` equals `#808080` over `#000000` within 0.05), `Between_BackgroundAlphaIsIgnored`, `Between_KnownPair` (`#767676` on `#FFFFFF` ≈ `4.54` within 0.05: the classic AA-limit gray), `Constants_HaveTheWcagValues`
- [ ] New (`BuiltInThemesContrastTests`): theory over the names `"Dark High Contrast"`, `"Light High Contrast"`: `HighContrastTheme_ForegroundOnBackground_MeetsAaa` (`>= 7`), `HighContrastTheme_LineNumbersOnBackground_MeetsAaa` (`LineNumberForeground`),
      `HighContrastTheme_CursorOnBackground_MeetsNonTextContrast` (`>= 3`), `HighContrastTheme_TypeIsHighContrast` (`ThemeType.HighContrastDark` / `HighContrastLight`);
      theory over `"Dark Modern"`, `"Light Modern"`: `ModernTheme_ForegroundOnBackground_MeetsAa` (`>= 4.5`)
      (If one of these assertions fails with the bundled colors, **do not change the theme file**: write the measured ratio in the card's commit body, weaken that single row to the measured floor rounded down, and list it in `docs/PORTING.md` "Behavior differences" as an accepted upstream contrast.)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Changing any bundled theme, contrast checks of token colors, the system high contrast theme (P7-22).

## Suggested commit message

`feat(themes): add the WCAG contrast ratio and pin the contrast of the high contrast themes`
