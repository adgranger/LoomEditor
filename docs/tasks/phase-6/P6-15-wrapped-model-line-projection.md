# P6-15 — `ModelLineProjection` for wrapped lines and injected text (content and positions)

- **Status:** todo
- **Depends on:** P6-14
- **Size:** M

## Goal

A model line with line-break data is shown as several view lines: its content, lengths, columns and the model ↔ view
position mapping come from `ModelLineProjectionData`. `GetViewLinesData` (tokens) is P6-16.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/modelLineProjection.ts` | 16–40 | `IModelLineProjection`: the members LoomEditor lacks (`getProjectionData`, `getInjectedTextAt`) |
| same | 53–63 | `createModelLineProjection` (the `lineBreakData !== null` branch) |
| same | 65–158 | `class ModelLineProjection`: constructor, `isVisible`, `setVisible`, `getProjectionData`, `getViewLineCount`, `getViewLineContent`, `getViewLineLength`, `getViewLineMinColumn`, `getViewLineMaxColumn`, `getViewLineContinuesWithWrappedLine` |
| same | 225–257 | `getModelColumnOfViewPosition`, `getViewPositionOfModelPosition`, `getViewLineNumberOfModelPosition`, `normalizePosition`, `getInjectedTextAt`, `_assertVisible` |
| same | 263–352 | `IdentityModelLineProjection`: add `getProjectionData` (null) and `getInjectedTextAt` (null) |
| same | 353–424 | `HiddenModelLineProjection`: add `getProjectionData` (null) and `getInjectedTextAt` (throws) |
| same | 426–438 | `spaces` / `_makeSpaces` (cached indentation strings) |
| `src/vs/editor/test/browser/viewModel/modelLineProjection.test.ts` | 30–98 | `SplitLine` |
| same | 986–1025 | `pos`, `createSplitLine`, `createLineBreakData`, `createModel` (test helpers) |

## Files to create or change

- `src/LoomEditor.View/Layout/ModelLineProjection.cs` — change: interface members, `Create(ModelLineProjectionData?, bool)`,
  the identity and hidden additions; update the "Phase 6 cut point" remarks
- `src/LoomEditor.View/Layout/WrappedModelLineProjection.cs` — create (header `modelLineProjection.ts (ModelLineProjection)`):
  `internal sealed partial class WrappedModelLineProjection : IModelLineProjection` — everything but `GetViewLinesData` /
  `GetViewLineData`, which throw `NotImplementedException("P6-16")` in this card
- `tests/LoomEditor.View.Tests/Layout/TestHelpers/ProjectionTestHelpers.cs` — create (`CreateSplitLine`, `CreateLineBreakData`)
- `tests/LoomEditor.View.Tests/Layout/ModelLineProjectionTests.cs` — change: port `SplitLine`

## Design decisions (already made — do not change)

- Monaco's class `ModelLineProjection` is named `WrappedModelLineProjection` in C#, because `ModelLineProjection` is already the
  static factory class (P5-78). Document the rename in the XML doc.
- `ModelLineProjection.Create(bool isVisible)` stays (identity / hidden) and a new overload
  `Create(ModelLineProjectionData? lineBreakData, bool isVisible)` returns the wrapped projection when the data is not null.
  `ViewModelLinesFromProjectedModel` keeps calling the old overload until P6-17.
- `SetVisible` on the wrapped projection mutates and returns `this` (Monaco does the same; the instance is owned by one slot).
- `ModelLineProjectionData? GetProjectionData()` and `InjectedText? GetInjectedTextAt(int outputLineIndex, int outputColumn)` are
  added to `IModelLineProjection`.
- `getViewLineContent` with injections: `LineInjectedText.ApplyInjectedText(model.GetLineContent(n), injectedTexts)` where the
  list is built from the injection offsets / options with line and owner 0, column `offset + 1`, order 0, exactly like
  lines 107–119. Without injections it uses `model.GetValueInRange(new Range(n, start + 1, n, end + 1))`.
- `spaces(count)`: a private static `List<string>` cache like Monaco's `_spaces` array (UI thread only: no locking).
- `GetViewPositionOfModelPosition(int deltaLineNumber, int inputColumn, PositionAffinity affinity = PositionAffinity.None)`:
  the wrapped projection's default is `None` (Monaco); the interface default stays `Left` (identity). C# takes the default of
  the **static type**: callers in LoomEditor always pass the affinity explicitly — check the existing callers in
  `ViewModelLinesFromProjectedModel.Conversions.cs` and make every call pass it.
- `getViewLineMinColumn` takes `ITextModel` in Monaco but ignores it: keep LoomEditor's `(TextModel model, int modelLineNumber, int outputLineIndex)`.
- Test model: Monaco's fake `ISimpleModel` (lines 1002–1025) becomes a real one-line `TextModel` created with the test
  helper the other View tests use (`TestTextModels.Create(...)` or whatever `ModelLineProjectionTests.cs` already uses).

## Tests

- [ ] Port: `Editor ViewModel - SplitLinesCollection` › `SplitLine`
- [ ] New: `Create_WithData_ReturnsTheWrappedProjection`; `Create_WithNullData_ReturnsIdentityOrHidden`
- [ ] New: `WrappedProjection_Hidden_ThrowsOnContent` and `…_HasZeroViewLines`
- [ ] New: `WrappedProjection_WithInjectedText_ContentIncludesIt` (one injection, no wrapping: breaks `[len + injected]`)
- [ ] New: `WrappedProjection_GetInjectedTextAt_ReturnsTheOptions`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: the `modelLineProjection.ts` map row lists `WrappedModelLineProjection` (rename noted); the
      `modelLineProjection.test.ts` count is 4 / 6
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`getViewLinesData` / `getViewLineData` of the wrapped projection and `getLineTokensWithInjections` (P6-16); building
projections from the view model lines (P6-17).

## Suggested commit message

`feat(view): project wrapped model lines`
