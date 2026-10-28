# P10-06 — Editor zoom: the state, the three commands and the `ZoomLevel` property

- **Status:** todo
- **Depends on:** P10-05
- **Size:** S

## Goal

An editor has a zoom level (−5 … 20); `editor.action.fontZoomIn`, `fontZoomOut` and `fontZoomReset` change it, the font size and the line height follow (`× (1 + level × 0.1)`), and an application can read and set `LoomEditor.ZoomLevel`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/config/editorZoom.ts` | 1–34 | `EditorZoom` (`getZoomLevel`, `setZoomLevel` with the clamp to `[-5, 20]` and no event when unchanged, `onDidChangeZoomLevel`) as `EditorZoomState` |
| `src/vs/editor/contrib/fontZoom/browser/fontZoom.ts` | 8–58 | `EditorFontZoomIn`, `EditorFontZoomOut`, `EditorFontZoomReset` (ids `editor.action.fontZoomIn`, `editor.action.fontZoomOut`, `editor.action.fontZoomReset`) |
| `src/vs/editor/common/config/fontInfo.ts` | 48–50 | the multiplier (already a parameter of `BareFontInfo.Create` since P10-03) |

## Files to create or change

- `src/LoomEditor.View/Layout/EditorZoomState.cs` — create (header: ported from `editorZoom.ts`)
- `src/LoomEditor.View/Features/FontZoom/FontZoomFeature.cs`, `FontZoomCommands.cs`, `README.md` — create (header: ported from `fontZoom.ts`); add the feature to `EditorFeatures.Default` (`EditorFeatures.cs`)
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `public EditorZoomState Zoom { get; } = new();`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Zoom.cs` — create: the `ZoomLevel` property
- `src/LoomEditor.WinUI/Controls/LoomEditor.Font.cs` — change: `CreateTextMeasurer` passes `_context.Zoom.Level`; subscribe to `Zoom.Changed` once in `InitializeFont`
- `tests/LoomEditor.View.Tests/Layout/EditorZoomStateTests.cs`, `tests/LoomEditor.View.Tests/Features/FontZoom/FontZoomFeatureTests.cs` — create
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt`, `samples/LoomEditor.Gallery/MainWindow.xaml(.cs)` — change

## Design decisions (already made — do not change)

- `EditorZoomState` is a `sealed class` (Level `double`, `Changed` event `EventHandler`, `SetLevel(double)`): **one per editor**, not a singleton (ADR 0013 decision 6). `SetLevel` clamps with `Math.Min(Math.Max(-5, level), 20)` and fires only on change (NaN is ignored).
- The commands (`FontZoomCommands.FontZoomIn` … ids above) are registered by `FontZoomFeature` like `SelectAllFeature` does. They have **no default key** (Monaco has none; the host binds keys). `fontZoomIn` is `SetLevel(Level + 1)`, `fontZoomOut` `SetLevel(Level - 1)`, `fontZoomReset` `SetLevel(0)`.
- `ZoomLevel` (`double`, default `0`) is a two-way property: setting it calls `Zoom.SetLevel`, and `Zoom.Changed` (from a command or the wheel, P10-07) writes the clamped value back into the dependency property (guard against re-entrancy: compare before `SetValue`), then `RecreateTextMeasurer()`. The Monaco clamp applies: `ZoomLevel = 50` reads back `20`.
- Line height follows the zoom through `BareFontInfo.Create` (the card P10-03 made it a parameter): `Win2DTextMeasurer` already receives `zoomLevel` (P10-04). The first visible line is kept (`RecreateTextMeasurer` does it). The minimap, the gutter font and the line numbers use `FontInfo`, so they follow.
- Gallery: two buttons "A−" and "A+" and a "100 %" button next to the font controls (`Editor.ZoomLevel -= 1` etc. via the commands: `Editor.ExecuteCommand`, grep how another Gallery button runs a command).

## Tests

- [ ] Port: none (no upstream test for `EditorZoom`)
- [ ] New: `SetLevel_AboveMax_ClampsTo20`, `SetLevel_BelowMin_ClampsToMinus5`, `SetLevel_Same_DoesNotFire`, `SetLevel_NaN_Ignored`, `Changed_FiresOnce_WithNewLevel`
- [ ] New: `FontZoomIn_IncrementsLevel`, `FontZoomOut_DecrementsLevel`, `FontZoomReset_SetsZero`, `FontZoomIn_AtMax_StaysAtMax`, `Feature_DoesNotRegisterKeybindings`
- [ ] New (View): `FontInfo` created with level 10 has font size and line height doubled (`BareFontInfo.Create` is tested in P10-03; here test that `EditorZoomState.Level` feeds a `FontInfo` through the helper that builds it in `ViewHarness`, if one exists; otherwise skip)
- [ ] Manual (section "P10-06"): A+ ×3 → text 30 % bigger, line height too, scroll position kept; A− ×5 stops at −5 (50 %); "100 %" resets.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `editorZoom.ts`, `fontZoom.ts` rows; "Behavior differences": zoom is per editor
- [ ] Public API file updated; card status and `docs/tasks/README.md` updated

## Out of scope

`Ctrl` + wheel (P10-07), zoom shared between editors or with the diff editor's two sides, persisting the level, a status bar.

## Suggested commit message

`feat(view): add the editor zoom (font zoom commands and ZoomLevel)`
