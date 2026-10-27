# P9-22 — `LoomDiffEditor`: the control, its two editors, the sash and the public API

- **Status:** todo
- **Depends on:** P9-21, P9-17, P9-15
- **Size:** M

## Goal

A public `LoomDiffEditor` control exists: it hosts an original and a modified `LoomEditor` side by side with a resizable sash, takes `OriginalText` / `ModifiedText` / `LanguageId` / `Theme`, owns a `DiffEditorViewModel` and the options, and applies the diff decorations. Scrolling alignment, the inline mode and the overview ruler come in the next cards.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/diffEditorWidget.ts` | 62–140, 141–340 | the composition: which parts exist (editors, options, sash, view zones, decorations, overview ruler, hidden regions feature), the sizes (`_rootSizeObserver`, `_sash`), `setModel`'s order (**not** `setModel` itself: ADR 0012 decision 5) |
| `src/vs/editor/browser/widget/diffEditor/components/diffEditorEditors.ts` | 24–224 | `_adjustOptionsForSubEditor`, `_adjustOptionsForLeftHandSide`, `_adjustOptionsForRightHandSide`: the options the two editors get (folding off, minimap off, no sticky scroll when hiding unchanged regions, `readOnly = !originalEditable` for the original, the modified gets room for the overview ruler) |
| `src/vs/editor/browser/widget/diffEditor/components/diffEditorSash.ts` | 11–98 | `SashLayout` (`computeSashLeft`: ratio, minimum editor width 100, `enableSplitViewResizing`, `resetSash`), `DiffEditorSash` (drag, double click resets) |

## Files to create or change

- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.cs` (+ `LoomDiffEditor.Options.cs`, `LoomDiffEditor.Layout.cs`) — create (**public** `sealed partial class LoomDiffEditor : Control`)
- `src/LoomEditor.WinUI/Controls/DiffEditor/SashLayout.cs` — create (pure, **internal**)
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffEditorSash.cs` — create (a small `Control` or a `Border` with pointer handling; `internal`)
- `src/LoomEditor.WinUI/Themes/Generic.xaml` — change: a `Style TargetType="controls:LoomDiffEditor"` with a `ControlTemplate`: a `Grid` with `PART_Original` (`controls:LoomEditor`), `PART_Modified`, `PART_Sash`; all required parts are looked up in `OnApplyTemplate` and a missing part throws `InvalidOperationException` (same rule as `PART_Canvas`)
- `src/LoomEditor.View/DiffEditor/DiffDecorations.cs`'s target adapter: `src/LoomEditor.WinUI/Controls/DiffEditor/EditorDecorationTarget.cs` — create (`IDiffDecorationTarget` over `LoomEditor.CreateDecorationsCollection`)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — regenerate
- `tests/LoomEditor.WinUI.Tests/…` — does not exist: `SashLayout` is tested in `tests/LoomEditor.View.Tests/DiffEditor/SashLayoutTests.cs` **by linking the source file** (`<Compile Include="..\..\src\LoomEditor.WinUI\Controls\DiffEditor\SashLayout.cs" Link="…" />`, the pattern P8-37 used for Gallery files); `SashLayout` therefore uses no WinUI type

## Design decisions (already made — do not change)

- **Public API** (and nothing else): `LoomDiffEditor` with dependency properties `OriginalText` (string), `ModifiedText` (string), `LanguageId` (string, forwarded to both editors), `Theme` (`EditorTheme`, forwarded), `RenderSideBySide` (bool, default true), `IgnoreTrimWhitespace` (true), `RenderIndicators` (true), `OriginalEditable` (false), `IsReadOnly` (false; the modified side), `HideUnchangedRegions` (bool, false), `UseInlineViewWhenSpaceIsLimited` (true), `EnableSplitViewResizing` (true), `RenderOverviewRuler` (true), `MaxComputationTimeMs` (int, 5000); read-only properties `LoomEditor Original`, `LoomEditor Modified` (the controls, for advanced use: `Original.Model`, selections, commands), `bool IsDiffUpToDate`, `int ChangeCount`; events `DiffChanged` (`EventHandler`). Methods: `Task WaitForDiffAsync(CancellationToken = default)`, and (added by P9-26) `GoToNextChange()`, `GoToPreviousChange()`. Property changes update `DiffEditorOptions`; `OriginalText` / `ModifiedText` set the text of the models (as the `Text` property of `LoomEditor` does) and a change made by the user in `Modified` is reflected back into `ModifiedText` (two-way, with a re-entrancy guard, as `LoomEditor.Text` does).
- Template-based (like `LoomEditor`, see `Generic.xaml`): the three parts above are created by the template; code creates the view model after the template is applied and tears it down on `Unloaded`.
- The sub-editors are configured like Monaco's `_adjustOptionsFor*`: `MinimapEnabled = false`, `StickyScrollEnabled = false` (Monaco turns it off for both only when hiding unchanged regions: do that), folding feature removed: `Features = EditorFeatures.Default` minus `FoldingFeature` (`grep -n "Folding" src/LoomEditor.View/Features/EditorFeatures.cs`; build the list once in a `static readonly IReadOnlyList<Func<IEditorFeature>>`), `Original.IsReadOnly = !OriginalEditable`, `Modified.IsReadOnly = IsReadOnly`, `OverviewRulerEnabled = false` on both (the diff editor draws its own, P9-28). The modified editor reserves `OverviewRulerWidth` (30 DIPs: `OverviewRulerFeature.ENTIRE_DIFF_OVERVIEW_WIDTH`) on its right side when `RenderOverviewRuler` (a `Margin` on the part: see P9-28).
- **Layout**: `SashLayout(double width, double splitViewDefaultRatio, bool enableSplitViewResizing)` → `double ComputeSashLeft(double desiredRatio)` = Monaco's `_computeSashLeft` (min editor width 100: `contentWidth <= 200` → midpoint; clamps); `double? Ratio` set by dragging (and `ResetSash()` on double click); the original part gets width `sashLeft`, the modified the rest minus the sash (1 DIP border drawn with `diffEditor.border` when it has a value). In inline mode the original part is collapsed (`Visibility.Collapsed`, 0 width) and the sash hidden.
  `RenderSideBySideEffective` (P9-12) decides the mode; `SizeChanged` calls `DiffEditorOptions.SetWidth` and re-applies the layout when the effective mode flips.
- Decorations: `DiffDecorations.Update(...)` runs on `DiffChanged`, on `RenderIndicators` / `ShowEmptyDecorations` changes and on model line count changes. The diff view model's own models are `Original.Model` and `Modified.Model`; `OriginalText = …` before the first layout is applied immediately to the models (the `LoomEditor` control already supports text before it is loaded: see how `Text` is handled with `_pendingScrollTop`).
- `<remarks>Threading: UI thread only.</remarks>` on every class.
- Native AOT: no reflection; `DependencyProperty.Register` follows the style of the existing properties (`typeof(LoomDiffEditor)`, `PropertyMetadata`).

## Tests

- [ ] New (`SashLayoutTests`): `ComputeSashLeft_DefaultRatio_IsMidpoint`, `ComputeSashLeft_ClampsToMinimumEditorWidth`, `ComputeSashLeft_NarrowerThanTwoMinimums_Midpoint`, `ComputeSashLeft_ResizingDisabled_AlwaysMidpoint`, `ResetSash_RestoresDefaultRatio`.
- [ ] Manual (recorded in the card result; part of P9-31's plan): the control shows two editors with the texts, the sash drags and double-click resets, editing the modified side updates `ModifiedText`, the decorations appear after a moment (no alignment yet).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI` run, diff checked
- [ ] `docs/PORTING.md`: `diffEditorWidget.ts`, `diffEditorEditors.ts`, `diffEditorSash.ts` (subset)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

View zone alignment and scroll sync (P9-23), inline mode (P9-24), hidden regions UI (P9-25), navigation (P9-26), the overview ruler (P9-28), accessibility of the diff editor (P9-30), embedding in the Gallery (P9-31).

## Suggested commit message

`feat(winui): add the LoomDiffEditor control shell`
