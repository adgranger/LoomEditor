# P5-34 — Bracket pair colorization

- **Status:** todo
- **Depends on:** P5-33
- **Size:** M

## Goal

Brackets are drawn in the colors of their nesting level (`editorBracketHighlight.foreground1…6`, cycling over the
colors the theme does not make transparent) and unexpected closing brackets in
`editorBracketHighlight.unexpectedBracket.foreground`, as in VS Code, on by default. The colors come from model
decorations given by a decoration provider of the text model, so they reach the renderer like any other inline
decoration.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/model/bracketPairsTextModelPart/colorizedBracketPairsDecorationProvider.ts` | 20–87 | `ColorizedBracketPairsDecorationProvider` (constructor, `handleDidChangeOptions`, `getDecorationsInRange`, `getAllDecorations`) |
| same | 89–104 | `ColorProvider` (`unexpectedClosingBracketClassName`, `getInlineClassName`, `getInlineClassNameOfLevel`) |
| same | 106–128 | the theming participant: which color each class name gets (becomes `DecorationStyles` rows, see below) |
| `common/model.ts` | 649–652 | `BracketPairColorizationOptions` |
| `common/core/misc/textModelDefaults.ts` | 13–16 | the defaults: `enabled: true`, `independentColorPoolPerBracketType: false` |
| `common/model/textModel.ts` | 289, 359, 388–392, 695–714, 1815–1832, 1871 | where the provider is created, its `onDidChange` → `onDidChangeDecorations`, the option in `updateOptions`, and where its decorations are added to the model's decorations |
| `common/services/modelService.ts` | 255–268 | the editor option `bracketPairColorization` is pushed into the model option (here: the model option is the only setting) |

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/BracketPairColorizationOptions.cs` — create (`internal sealed record BracketPairColorizationOptions(bool Enabled = true, bool IndependentColorPoolPerBracketType = false)`)
- `src/LoomEditor.Core/Model/BracketPairs/ColorizedBracketPairsDecorationProvider.cs` — create (with the private `ColorProvider`)
- `src/LoomEditor.Core/Model/TextModel.BracketPairs.cs` — change: `internal BracketPairColorizationOptions BracketPairColorizationOptions { get; }`, `internal void UpdateBracketPairColorizationOptions(BracketPairColorizationOptions)`, the provider, its `Changed` → `DecorationsChanged`
- `src/LoomEditor.Core/Decorations/IDecorationProvider.cs` — create (port of `common/model/decorationProvider.ts` 9–27; P5-03 left the providers to this card)
- `src/LoomEditor.Core/Model/TextModel.Decorations.cs` (P5-03) — change: the provider lines P5-03 left out (textModel.ts 1821, 1830, 1871): `GetDecorationsInRange`, `GetLineDecorations` / `GetLinesDecorations` and `GetAllDecorations` append the provider's decorations (Monaco's `pushMany`)
- `src/LoomEditor.View/Layout/DecorationStyle.cs` (P5-08) — change: optional `Func<EditorTheme, ThemeColor?>? ForegroundResolver = null` (used instead of `ForegroundColorId` when set), read by P5-09's inline foreground code
- `src/LoomEditor.View/Layout/DecorationStyles.cs` (P5-08) — change: rows `bracket-highlighting-0` … `bracket-highlighting-29` and `unexpected-closing-bracket`
- `src/LoomEditor.View/Layout/BracketPairColorizationColors.cs` — create (port of the theming participant)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/ColorizedBracketPairsDecorationProviderTests.cs` — create
- `tests/LoomEditor.View.Tests/Layout/BracketPairColorizationColorsTests.cs` — create

## Design decisions (already made — do not change)

- **The setting lives in the model**, as in Monaco (`ITextModelCreationOptions.bracketPairColorizationOptions`,
  updated by the model service from `editor.bracketPairColorization`). `TextModelOptions` is public and does not change
  (ADR 0009 decision 12): the option is an internal property of `TextModel` with VS Code's default (enabled). Changing
  it calls `BracketPairs.HandleDidChangeOptions` and the provider's `HandleDidChangeOptions`, then raises
  `DecorationsChanged` (the decorations differ). No public property on the control.
- `IDecorationProvider`: `GetDecorationsInRange(Range range, int ownerId, bool filterOutValidation)` and `GetAllDecorations(int ownerId, bool filterOutValidation)` returning step A's model decorations (no font, minimap or margin filters in phase 5). `TextModel` always
  passes an owner id (default `0`), so Monaco's `ownerId === undefined` check is dead code: not ported (comment it).
  `onlyMinimapDecorations` is not ported (no minimap in phase 5).
- One decoration per bracket of `BracketPairs.GetBracketsInRange(range, onlyColorizedBrackets: true)`: id
  `$"bracket{range}-{nestingLevel}"`, owner `0`, the bracket range, options with `Description = "BracketPairColorization"`
  and `InlineClassName` = `unexpected-closing-bracket` when `IsInvalid`, otherwise `bracket-highlighting-{level % 30}`
  where level is `NestingLevelOfEqualBracketType` if `IndependentColorPoolPerBracketType`, else `NestingLevel`. The
  31 option instances are created once (static, with `ModelDecorationOptions.Register`), not per decoration.
- `bracketPairs.onDidChange` → the provider's `Changed` → `TextModel` raises `DecorationsChanged` (Monaco's
  `beginDeferredEmit` / `fire` / `endDeferredEmit` around it: use the deferral of P5-04).
- **Colors.** The theming participant cannot be a fixed table: VS Code's defaults make `foreground4…6` transparent
  (`#00000000`, editorColorRegistry.ts 82–84), and Monaco cycles over the **non-transparent** colors only.
  `BracketPairColorizationColors.GetLevelColor(EditorTheme theme, int level)` resolves the six
  `editorBracketHighlight.foregroundN` ids with `EditorTheme.GetColor(id)` (P5-05: the theme, then `EditorColorDefaults`), drops
  missing and fully transparent colors (alpha 0), and returns `colors[level % colors.Count]`, or no color when the list
  is empty. `GetUnexpectedBracketColor(theme)` resolves `editorBracketHighlight.unexpectedBracket.foreground`. The
  `DecorationStyles` rows for the 31 class names have a **computed foreground**: their `ForegroundResolver` calls these methods (the
  only computed rows of the table). The resolved list
  is cached per theme instance.
- The inline foreground of a decoration wins over the token color (Monaco: `.monaco-editor .bracket-highlighting-N`
  is more specific than the token class). Step A's renderer does this for every inline foreground; check it with a test.
- The `editorBracketHighlight.*` colors and their VS Code defaults are added by P5-05 (`EditorColorDefaults`): if one
  is missing, add it here with the values of editorColorRegistry.ts 79–86.

## Tests

No upstream test targets the provider (the bracket levels are tested by `colorizedBracketsVSBrackets`, P5-31).

- [ ] New: `GetDecorationsInRange_NestedBrackets_ClassNamesFollowNestingLevel` (`"{ [ ( ) ] }"` →
      `bracket-highlighting-0`, `-1`, `-2`, `-2`, `-1`, `-0`)
- [ ] New: `GetDecorationsInRange_IndependentColorPool_UsesLevelOfEqualBracketType`
- [ ] New: `GetDecorationsInRange_UnexpectedClosingBracket_UsesUnexpectedClassName`
- [ ] New: `GetDecorationsInRange_Disabled_ReturnsNothing`
- [ ] New: `TextModel_GetDecorationsInRange_IncludesBracketColors`
- [ ] New: `UpdateBracketPairColorizationOptions_RaisesDecorationsChanged`
- [ ] New: `Edit_ThatChangesBrackets_RaisesDecorationsChanged`
- [ ] New (View): `GetLevelColor_DefaultDarkColors_CycleOverThreeColors` (levels 0…5 → `#FFD700`, `#DA70D6`,
      `#179FFF`, then again: foreground4…6 are transparent by default)
- [ ] New (View): `GetLevelColor_ThemeSetsSixColors_CycleOverSix`
- [ ] New (View): `GetLevelColor_AllTransparent_NoColor`
- [ ] New (View): the line style of a rendered line with a bracket uses the bracket color, not the token color (with the
      test measurer of the Rendering tests, through P5-09's inline decoration path)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `colorizedBracketPairsDecorationProvider.ts` ✅ (P5-34); behavior difference "bracket
      colors: a computed `DecorationStyles` row instead of generated CSS rules"
- [ ] Card status and `docs/tasks/README.md` updated
- [ ] Manual check in the Gallery (C# and JSON samples, Dark Modern and Light Modern): brackets are colored by level,
      a stray `}` is red, typing a `{` recolors the following brackets at once

## Out of scope

Bracket pair guides (`guidesTextModelPart.ts`, phase 6). The minimap. A public option on the control.

## Suggested commit message

`feat: colorize bracket pairs`
