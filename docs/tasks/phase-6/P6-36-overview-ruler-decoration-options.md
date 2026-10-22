# P6-36 — Overview ruler options of decorations and `GetOverviewRulerDecorations`

- **Status:** todo
- **Depends on:** P5-96
- **Size:** S

## Goal

A decoration can ask to be marked in the overview ruler (`overviewRuler: { color, darkColor, position }`), the text model
lists those decorations, and the decorations-changed event reports `AffectsOverviewRuler` precisely.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model.ts` | 33–41 | `enum OverviewRulerLane` (`Left = 1, Center = 2, Right = 4, Full = 7`) |
| same | 92–105 | `IDecorationOptions` (`color`, `darkColor`: `string | ThemeColor`) |
| same | 118–126 | `IModelDecorationOverviewRulerOptions` (`position`) |
| `src/vs/editor/common/model/textModel.ts` | 1835–1837 | `getOverviewRulerDecorations` |
| same | 2276–2290 (read `getAll`) | the `overviewRulerOnly` argument |
| same | 2180–2182 | `isNodeInOverviewRuler` |
| same | 2359–2368 | `DecorationOptions` (`color \|\| ''`, `darkColor \|\| ''`) |
| same | 2370–2405 | `ModelDecorationOverviewRulerOptions` (`position` default `Center`; `getColor` is the view's job, see decisions) |
| same | 2508, 2548 | `ModelDecorationOptions.overviewRuler` |
| same | 2658–2664 | `checkAffectedAndFire`: `affectsOverviewRuler ||= !!options.overviewRuler?.color` |

## Files to create or change

- `src/LoomEditor.Core/Decorations/OverviewRulerLane.cs` — create (header `model.ts`)
- `src/LoomEditor.Core/Decorations/DecorationColor.cs` — create (header `model.ts`)
- `src/LoomEditor.Core/Decorations/ModelDecorationOverviewRulerOptions.cs` — create (header `textModel.ts`)
- `src/LoomEditor.Core/Decorations/ModelDecorationOptionsData.cs`, `ModelDecorationOptions.cs` — change: `OverviewRuler`
  (update the "Not ported" remark)
- `src/LoomEditor.Core/Decorations/DecorationsTrees.cs`, `src/LoomEditor.Core/Model/TextModel.Decorations.cs` — change:
  `GetOverviewRulerDecorations(int ownerId = 0, bool filterOutValidation = false, bool filterFontDecorations = false)`
- `src/LoomEditor.Core/Model/TextModel.DecorationsEmitter.cs` — change: `CheckAffectedAndFireDecorations` sets the overview ruler flag
- `tests/LoomEditor.Core.Tests/Decorations/ModelDecorationOverviewRulerOptionsTests.cs`, `tests/LoomEditor.Core.Tests/Model/TextModelOverviewRulerTests.cs` — create

## Design decisions (already made — do not change)

- Core knows nothing about themes: `DecorationColor` is `internal sealed record DecorationColor(string? Value, string? ThemeColorId)`
  with `static DecorationColor FromValue(string css)` (a hex color like `#RRGGBB(AA)`) and `FromThemeColor(string id)`
  (Monaco's `themeColorFromId`). Exactly one is set. Monaco's `color || ''` → a `null` `DecorationColor?`.
- `ModelDecorationOverviewRulerOptions(DecorationColor? Color, DecorationColor? DarkColor, OverviewRulerLane Position)` as an
  `internal sealed record`; the raw data uses the same record (no separate data type). `Position` default `Center`.
  `getColor(theme)` / `invalidateCachedColor` are **not** in Core: the view resolves colors (P6-37).
- `OverviewRulerLane` is a `[Flags]`-free plain enum with Monaco's values (the renderer indexes arrays with them).
- LoomEditor has one tree: `GetOverviewRulerDecorations` searches all and keeps the nodes with `Options.OverviewRuler?.Color is not null`
  (Monaco's separate tree holds exactly those). Note it in the XML doc.

## Tests

- [ ] New: `OverviewRulerOptions_DefaultPosition_IsCenter`
- [ ] New: `GetOverviewRulerDecorations_OnlyDecorationsWithARulerColor`
- [ ] New: `GetOverviewRulerDecorations_FiltersByOwner`
- [ ] New: `DeltaDecorations_WithRulerColor_SetsAffectsOverviewRuler`; `…_WithoutRulerColor_DoesNotSetIt`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: decorations rows (overview ruler options); the `ModelDecorationOptions` row of "Cut points for phase 6"
      keeps only the minimap part
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Minimap options (P6-42), view-side color resolution (P6-37).

## Suggested commit message

`feat(core): add overview ruler options to decorations`
