# P5-67 — `FindDecorations` and the find colors

- **Status:** done
- **Depends on:** P5-66
- **Size:** M

## Goal

Find matches, the current match, its line highlight and the find scope are model decorations drawn with VS Code's
colors: `FindDecorations` (Monaco's class without the overview ruler and minimap parts), the four decoration styles and
their theme color defaults.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/find/browser/findDecorations.ts` | 15–155 | `FindDecorations`: fields, `dispose`, `reset`, `getCount`, `getFindScope`, `getFindScopes`, `getStartPosition`, `setStartPosition`, `_getDecorationIndex`, `getDecorationRangeAt`, `getCurrentMatchesPosition`, `setCurrentFindMatch` |
| same | 157–227 | `set` — **without** the overview-ruler approximation (lines 163–196, 209–210) |
| same | 229–284 | `matchBeforePosition`, `matchAfterPosition`, `_allDecorations` |
| same | 286–347 | the decoration options; `overviewRuler` and `minimap` members are dropped |
| `contrib/find/browser/findWidget.css` | 226–241 | `.findMatch`, `.currentFindMatch`, `.findScope` (→ `DecorationStyles`) |
| `src/vs/editor/browser/widget/codeEditor/editor.css` | 32–36 | `.rangeHighlight` |
| `src/vs/platform/theme/common/colors/editorColors.ts` | 148–170 | `editor.findMatchBackground`, `editor.findMatchHighlightBackground`, `editor.findRangeHighlightBackground`, `editor.findMatchBorder` |
| `common/core/editorColorRegistry.ts` | 17–18 | `editor.rangeHighlightBackground`, `editor.rangeHighlightBorder` |

## Files to create or change

- `src/LoomEditor.View/Features/Find/FindDecorations.cs` — create
- `src/LoomEditor.View/Layout/DecorationStyles.cs` — **verify** the rows `findMatch`, `currentFindMatch`, `findScope` added by
  P5-08 against the list below (fix a wrong value, never duplicate a row); add `rangeHighlight` only if P5-08 did not
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: the color rows of the list below that P5-08 did not add (one
  line each, with the upstream line in a comment)
- `src/LoomEditor.View/Features/Find/README.md` — update
- `tests/LoomEditor.View.Tests/Features/Find/FindDecorationsTests.cs` — create
- `tests/LoomEditor.TextMate.Tests/Themes/EditorColorDefaultsTests.cs` — change: add the test below

## Design decisions (already made — do not change)

- `FindDecorations(ViewModel viewModel)` — `internal sealed class : IDisposable`. `editor.changeDecorations(cb)` →
  `viewModel.Model.ChangeDecorations(ownerId, accessor => …)` with the editor's owner id (P5-A cards: `TextModel.DeltaDecorations` /
  `ChangeDecorations`, `ModelDecorationOptions.Register(new ModelDecorationOptionsData { … })`); `getModel().getDecorationRange(id)`
  → `TextModel.GetDecorationRange(id)`; `getDecorationsInRange(range)` → `TextModel.GetDecorationsInRange(range)`. Decoration
  ids stay `string`s. Options are compared **by reference** (`ReferenceEquals`), like Monaco's `===` on registered options.
- The options are `static readonly ModelDecorationOptions` created with `ModelDecorationOptions.Register(...)`:
  `CurrentFindMatchDecoration` (`current-find-match`, `NeverGrowsWhenTypingAtEdges`, `ZIndex = 13`, `ClassName = "currentFindMatch"`,
  `InlineClassName = "currentFindMatchInline"`, `ShowIfCollapsed = true`), `FindMatchDecoration` (`find-match`, `ZIndex = 10`,
  `findMatch` / `findMatchInline`), `FindMatchNoOverviewDecoration` (`find-match-no-overview`, `findMatch`, no z-index, no inline
  class — keep Monaco's difference), `RangeHighlightDecoration` (`find-range-highlight`, `rangeHighlight`, `IsWholeLine`),
  `FindScopeDecoration` (`find-scope`, `findScope`, `IsWholeLine`, default stickiness). `FindMatchOnlyOverviewDecoration` is
  not ported (overview ruler only).
- `set(findMatches, findScopes)`: above 1000 matches use `FindMatchNoOverviewDecoration` as Monaco does; the merged
  "approximate" overview decorations and `_overviewRulerApproximateDecorations` are not ported (overview ruler: phase 6).
  `editor.getLayoutInfo()` is therefore not needed.
- **Styles** (ADR 0009 decision 4; names of `DecorationStyle` members are step A's):
  `findMatch` → background `editor.findMatchHighlightBackground`; `currentFindMatch` → background `editor.findMatchBackground`,
  border `editor.findMatchBorder` 2 px; `findScope` → background `editor.findRangeHighlightBackground`, whole line;
  `rangeHighlight` → background `editor.rangeHighlightBackground`, border `editor.rangeHighlightBorder` 1 px, whole line.
  `currentFindMatchInline` / `findMatchInline` get no row (their CSS only sets a foreground that defaults to none).
- **Color defaults** (dark, light, hcDark, hcLight): `editor.findMatchBackground` `#515C6A`, `#A8AC94`, null, null;
  `editor.findMatchHighlightBackground` `#EA5C0055`, `#EA5C0055`, null, null; `editor.findRangeHighlightBackground`
  `#3a3d4166`, `#b4b4b44d`, null, null; `editor.findMatchBorder` null, null, `contrastActiveBorder`, `contrastActiveBorder`;
  `editor.rangeHighlightBackground` `#ffffff0b`, `#fdff0033`, null, null; `editor.rangeHighlightBorder` null, null,
  `contrastActiveBorder`, `contrastActiveBorder`.

## Tests

No upstream test file (the class is covered by `findModel.test.ts`, P5-68/P5-69). New tests:

- [x] `Set_AddsOneFindMatchDecorationPerMatch`, `Set_MoreThan1000Matches_UsesNoOverviewOptions`,
      `Set_FindScopes_AddsWholeLineScopeDecorations`, `SetCurrentFindMatch_ReturnsOneBasedPositionAndSwapsOptions`,
      `SetCurrentFindMatch_MultilineMatchEndingAtColumn1_RangeHighlightStopsOnPreviousLine`,
      `MatchAfterPosition_AfterLastMatch_WrapsToFirst`, `MatchBeforePosition_BeforeFirstMatch_WrapsToLast`,
      `Reset_RemovesNothingAndForgetsIds`, `Dispose_RemovesAllDecorations`
- [x] New (Themes): `Resolve_FindColors_DarkAndLightDefaults`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `contrib/find/browser/findDecorations.ts` 🔶 (no overview ruler / minimap, P5-67)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Overview ruler and minimap marks (phase 6), the find model that calls this class (P5-68).

## Suggested commit message

`feat(view): add find decorations and their colors`
