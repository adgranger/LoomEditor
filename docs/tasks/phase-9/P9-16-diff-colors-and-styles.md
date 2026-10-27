# P9-16 — Diff theme colors and decoration styles

- **Status:** todo
- **Depends on:** P9-12
- **Size:** S

## Goal

The theme knows the colors of the diff editor (with VS Code's defaults) and the decoration style table maps Monaco's diff class names (`line-insert`, `char-delete`, …) to them, so a decoration with one of these class names is drawn correctly by the existing renderer.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/platform/theme/common/colors/editorColors.ts` | 264–335 | the `registerColor` calls: `diffEditor.insertedTextBackground`, `diffEditor.removedTextBackground`, `diffEditor.insertedLineBackground`, `diffEditor.removedLineBackground`, `diffEditorGutter.insertedLineBackground`, `diffEditorGutter.removedLineBackground`, `diffEditorOverview.insertedForeground`, `diffEditorOverview.removedForeground`, `diffEditor.insertedTextBorder`, `diffEditor.removedTextBorder`, `diffEditor.border`, `diffEditor.diagonalFill`, `diffEditor.unchangedRegionBackground`, `diffEditor.unchangedRegionForeground`, `diffEditor.unchangedCodeBackground` (and `defaultInsertColor` / `defaultRemoveColor`, lines 264–266) |
| `src/vs/editor/browser/widget/diffEditor/registrations.contribution.ts` | 14–96 | the decoration option objects: which class name each has, `isWholeLine`, `marginClassName`, `linesDecorationsClassName`, `shouldFillLineOnLineBreak` |
| `src/vs/editor/browser/widget/diffEditor/style.css` | 274–285, 327–346, 373–425 | what each class draws (background color ids, the 1 px border color ids, `diff-range-empty` = a 3 px left border, `inline-deleted-text` = background + line-through) |

## Files to create or change

- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: one row per color above (follow the existing rows: the comment gives the upstream line, `(dark, light, hcDark, hcLight)`; `defaultInsertColor` is `rgba(155,185,85,0.2)` = `#9bb95533`, `defaultRemoveColor` is `rgba(255,0,0,0.2)` = `#ff000033`; `diffEditor.unchangedRegionBackground` = `"sideBar.background"`, `diffEditor.unchangedRegionForeground` = `"foreground"`: check these two ids exist in the table, add rows copied from upstream if they do not)
- `src/LoomEditor.Themes/EditorTheme.cs` (or wherever the ids are listed: `grep -rn "editor.findMatchBackground" src --include=*.cs`) — change only if the ids must be listed there
- `src/LoomEditor.View/Layout/DecorationStyles.cs` — change: the rows of the table below
- `src/LoomEditor.View/DiffEditor/DiffDecorationOptions.cs` — create (the `ModelDecorationOptions` instances)
- `tests/LoomEditor.Themes.Tests/…` (the existing test class for the defaults) and `tests/LoomEditor.View.Tests/Layout/DecorationStylesTests.cs` — change / create

## Design decisions (already made — do not change)

- Style rows (`DecorationStyle` is the existing record; the fallback chains `var(a, var(b))` of the CSS are **not** ported — every id has its own default, so the chain only matters for a custom theme that sets the text color and not the line color; list that in the XML doc):
  `line-insert` → background `diffEditor.insertedLineBackground`, border `diffEditor.insertedTextBorder`; `line-delete` → `diffEditor.removedLineBackground` / `diffEditor.removedTextBorder`; `char-insert` → `diffEditor.insertedTextBackground` / `diffEditor.insertedTextBorder`; `char-delete` → `diffEditor.removedTextBackground` / `diffEditor.removedTextBorder`;
  `char-insert diff-range-empty` and `char-delete diff-range-empty` (the class name is the whole string as Monaco registers it) → a 3 px left border in `diffEditor.insertedTextBackground` / `diffEditor.removedTextBackground` (use the existing border options; if `DecorationStyle` cannot express "left border only", add `LeftBorderWidth` (default 0) to the record and draw it in `TextViewRenderer.Decorations.cs`: read how `BorderWidth` is drawn and extend it; keep the change under 30 lines);
  `gutter-insert` → background `diffEditorGutter.insertedLineBackground` falling back **in the table** to `diffEditor.insertedLineBackground` (a resolver, as `BracketPairColorizationColors` does for the unexpected-bracket color); `gutter-delete` likewise;
  `inline-deleted-text` → background `diffEditor.removedTextBackground` and a strike-through (`DecorationStyle` has no strike-through: add `bool StrikeThrough = false` and draw it in the line renderer next to the underline code; used by P9-24);
  `insert-sign` / `delete-sign` (the lines-decoration class of `renderIndicators`) → `GutterIcon.Plus` / `GutterIcon.Minus` (add the two values to the existing `GutterIcon` enum and draw them like the chevrons in `TextViewRenderer.GutterDecorations.cs`: a `+` / `−` of two strokes, color `foreground`).
- `DiffDecorationOptions` (internal static readonly fields, same names as Monaco's exports): `LineAddBackgroundWithIndicator`, `LineDeleteBackgroundWithIndicator`, `LineAddBackground`, `LineDeleteBackground`, `AddDecoration`, `WholeLineAddDecoration`, `AddDecorationEmpty`, `DeleteDecoration`, `WholeLineDeleteDecoration`, `DeleteDecorationEmpty`; built with `ModelDecorationOptions` (existing class: find the factory the other features use, e.g. in `FindDecorations.cs`), `Description` set as upstream. `ShouldFillLineOnLineBreak` exists in `ModelDecorationOptionsData`; the renderer does not honor it yet — that is fine (the filled line-break area is not drawn: say so in the XML doc and in `PORTING.md`).
- High contrast: the dashed border style is drawn solid (the same difference as the table's header already states).
- `<remarks>Threading: immutable statics.</remarks>`

## Tests

- [ ] New: `EditorColorDefaults_DiffColors_DarkLightHighContrast` (each id resolves to the upstream value for each theme type: the table of the card), `DecorationStyles_DiffClasses_HaveRows`, `DecorationStyles_GutterInsert_FallsBackToLineBackground`, `DiffDecorationOptions_ClassNamesAndFlags` (e.g. `LineAddBackgroundWithIndicator` has `IsWholeLine`, `MarginClassName == "gutter-insert"`, `LinesDecorationsClassName == "insert-sign"` — the codicon part of Monaco's string is dropped), and a rendering test in `tests/LoomEditor.Rendering.Tests` for the new `LeftBorderWidth`, `StrikeThrough` and the two gutter icons using the recording surface the existing decoration tests use (`grep -rln "RecordingSurface\|FakeSurface" tests/LoomEditor.Rendering.Tests`).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `editorColors.ts` (diff colors), `registrations.contribution.ts`, `style.css` (diff rules) and the "Behavior differences" rows (no fallback chains, line-break fill not drawn)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Producing the decorations from a diff (P9-17), the zone styles (P9-19), `diffEditor.move.border` / `moveActive.border` / `unchangedRegionShadow` (not used in phase 9).

## Suggested commit message

`feat(themes): add the diff editor colors and decoration styles`
