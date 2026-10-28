# P9-12 — `DiffEditorOptions`, `DiffState` and `DiffMapping`

- **Status:** done
- **Depends on:** P9-11
- **Size:** S

## Goal

The settings of a diff editor (with Monaco's defaults and validation, and the rule that picks side by side or inline from the width) and the immutable result a view model publishes (`DiffState`) exist in `LoomEditor.View`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/config/diffEditor.ts` | 8–41 | `diffEditorDefaultOptions` (the numbers and booleans below) |
| `src/vs/editor/browser/widget/diffEditor/diffEditorOptions.ts` | 17–98 | `DiffEditorOptions`: `renderSideBySide` (derived), `couldShowInlineViewBecauseOfSize`, `renderSideBySideInAutomaticMode`, `readOnly`, the plain options |
| same | 100–168 | `updateOptions`, `setWidth` (without `smoothResizeStartWidth`), `resetWidthBasedLayout` |
| same | 202–238 | `validateDiffEditorOptions` (only the options kept: `splitViewDefaultRatio` clamped to 0.1–0.9, `maxComputationTime`, `renderSideBySideInlineBreakpoint` and the three `hideUnchangedRegions` counts clamped to ≥ 0; booleans are plain `bool`, so nothing to coerce) |
| `src/vs/editor/browser/widget/diffEditor/diffEditorViewModel.ts` | 415–457 | `DiffState` (`fromDiffResult`), `DiffMapping` |

## Files to create or change

- `src/LoomEditor.View/DiffEditor/DiffEditorOptions.cs`, `HideUnchangedRegionsOptions.cs`, `DiffState.cs` (with `DiffMapping`) — create
- `tests/LoomEditor.View.Tests/DiffEditor/DiffEditorOptionsTests.cs`, `DiffStateTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class DiffEditorOptions` with a settable property per option and an event `EventHandler<DiffEditorOptionsChangedEventArgs>? Changed` (carries the set of changed option names as `IReadOnlySet<string>`; names are `nameof(...)`). A setter validates (clamps) and raises `Changed` only when the stored value changes. Options (name = default): `EnableSplitViewResizing = true`, `SplitViewDefaultRatio = 0.5`, `RenderSideBySide = true`, `MaxComputationTimeMs = 5000`, `IgnoreTrimWhitespace = true`, `RenderIndicators = true`, `OriginalEditable = false`, `ReadOnly = false`, `RenderOverviewRuler = true`, `ShowMoves = false`, `ShowEmptyDecorations = true`, `HideOriginalLineNumbers = false`, `RenderSideBySideInlineBreakpoint = 900`, `UseInlineViewWhenSpaceIsLimited = true`, `HideUnchangedRegions` (`HideUnchangedRegionsOptions(Enabled = false, ContextLineCount = 3, MinimumLineCount = 3, RevealLineCount = 20)`, an immutable record replaced as a whole), `IsScreenReaderOptimized = false` (replaces Monaco's `IAccessibilityService`; the control sets it from the accessibility support, P9-22).
  Not ported: `renderMarginRevertIcon`, `renderGutterMenu`, `maxFileSize`, `diffCodeLens`, `diffWordWrap`, `diffAlgorithm`, `accessibilityVerbose`, `experimental.useTrueInlineView`, `isInEmbeddedEditor`, `onlyShowAccessibleDiffViewer`, `compactMode`, `trueInlineDiff*`, `shouldRenderOldRevertArrows`, `shouldRenderGutterMenu`, the `temporaryInlineMode` / smooth resize start width (list them in the XML doc of the class).
- Derived values are **methods / properties computed on demand** (ADR 0012 decision 2): `bool RenderSideBySideEffective` = `RenderSideBySide && _widthBasedLayout == Auto && !(UseInlineViewWhenSpaceIsLimited && CouldShowInlineViewBecauseOfSize && !IsScreenReaderOptimized)`, `bool CouldShowInlineViewBecauseOfSize` = `RenderSideBySide && _width <= RenderSideBySideInlineBreakpoint`, `bool RenderSideBySideInAutomaticMode` (`_width > breakpoint || IsScreenReaderOptimized`; read lines 47–53), `bool ShowMovesEffective` (`ShowMoves && RenderSideBySideEffective`).
  `SetWidth(double width)`: stores the width and, when `width <= breakpoint`, resets the width based layout to `Auto`; the `inline` branch needs `smoothResizeStartWidth` and is **not** ported (the layout is always `Auto`: remove the field and say so in the XML doc). Changing the width raises `Changed` for `RenderSideBySideEffective` only when the effective value flips.
- `DiffState` is a `sealed record DiffState(IReadOnlyList<DiffMapping> Mappings, IReadOnlyList<MovedText> MovedTexts, bool Identical, bool QuitEarly)` with `static DiffState FromDiffResult(DocumentDiff)`; `sealed record DiffMapping(DetailedLineRangeMapping LineRangeMapping)`.
- `<remarks>Threading: UI thread only (options); DiffState is immutable.</remarks>`

## Tests

- [x] New: `Defaults_MatchMonaco` (every default of the list above), `SplitViewDefaultRatio_Clamped_0_1_To_0_9`, `NegativeCounts_ClampedToZero`, `Changed_RaisedOnlyWhenValueChanges`, `Changed_CarriesTheChangedNames`, `RenderSideBySideEffective_WideEditor_True`, `RenderSideBySideEffective_NarrowEditor_False_WhenUseInlineViewWhenSpaceIsLimited`, `RenderSideBySideEffective_NarrowEditor_True_WhenOptionOff`, `RenderSideBySideEffective_ScreenReader_StaysSideBySide`, `RenderSideBySideInlineOption_False_AlwaysInline`, `SetWidth_RaisesChangedWhenEffectiveValueFlips`, `HideUnchangedRegions_Replace_RaisesChanged`, `DiffState_FromDiffResult_WrapsMappings`.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated: `diffEditor.ts` (defaults), `diffEditorOptions.ts` (subset), `DiffState` / `DiffMapping`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The view model (P9-14), the public properties of the control (P9-22).

## Suggested commit message

`feat(view): add DiffEditorOptions and DiffState`
