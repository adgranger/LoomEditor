# P5-07 — View model decorations: viewport decorations, inline decorations, decorations-changed view event

- **Status:** todo
- **Depends on:** P5-06
- **Size:** M

## Goal

The view model gives the renderer the decorations of the viewport (in view coordinates) and, per visible line, the
inline decorations (`inlineClassName`, `beforeContentClassName`, `afterContentClassName`). When the model's decorations
change, the view model forgets its cache and sends a `ViewDecorationsChangedEvent`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/viewModelDecoration.ts` | 10–78 | `ViewModelDecoration`, `isModelDecorationVisible`, `isModelDecorationInComment`, `isModelDecorationInString`, `testTokensInRange` |
| `src/vs/editor/common/viewModel/inlineDecorations.ts` | 12–192 | `InlineDecorationType`, `InlineDecoration`, `IViewDecorationsCollection`, `InlineModelDecorationsComputer` (not 194–290: injected text) |
| `src/vs/editor/common/viewModel/viewModelDecorations.ts` | 16–83 | `ViewModelDecorations` (not `getMinimapDecorationsInRange`) |
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 148, 178, 360–362, 564–568, 851–853, 884–888 | creation, dispose, `reset` on flush, the `onDidChangeDecorations` handler, `getDecorationsInViewport`, the decorations part of `getViewportViewLineRenderingData` |
| `src/vs/editor/common/viewModel/viewModelLines.ts` | 1271–1273 | `ViewModelLinesFromModelAsIs.getDecorationsInRange` (identity: the model's query) |
| `src/vs/editor/common/viewLayout/viewLinesViewportData.ts` | 84–86 | `getDecorationsInViewport` |
| `src/vs/editor/common/viewEvents.ts` | 72–95 | `ViewDecorationsChangedEvent` |
| `src/vs/editor/test/common/viewModel/inlineDecorations.test.ts` | 1–264 | the tests listed below |
| `src/vs/editor/test/browser/viewModel/viewModelDecorations.test.ts` | 219–245 | `issue #37401` (adapted) |

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModelDecoration.cs` — create (`ViewModelDecoration` + the visibility helpers)
- `src/LoomEditor.View/Layout/InlineDecorations.cs` — create (`InlineDecorationType`, `InlineDecoration`, `ViewDecorationsCollection`, `InlineModelDecorationsComputer`)
- `src/LoomEditor.View/Layout/ViewModelDecorations.cs` — create
- `src/LoomEditor.View/Layout/ViewModel.Decorations.cs` — create (partial)
- `src/LoomEditor.View/Layout/ViewModel.cs` — change: create / dispose the decorations, `Reset()` on `ModelRawFlush`, pass them to `ViewportData`
- `src/LoomEditor.View/Layout/ViewportData.cs` — change (see below)
- `src/LoomEditor.View/Events/ViewEvent.cs` — change: `ViewDecorationsChangedEvent`
- `tests/LoomEditor.View.Tests/Layout/InlineModelDecorationsComputerTests.cs`, `ViewModelDecorationsTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0009 decision 2: everything in `LoomEditor.View/Layout/`, internal.
- **Types.** `ViewModelDecoration` is a `sealed class` (reference identity matters: the cache test compares instances)
  with `Range Range` and `ModelDecorationOptions Options`. `InlineDecoration` is a
  `sealed record InlineDecoration(Range Range, string InlineClassName, InlineDecorationType Type)` (value equality,
  like the tests' `deepStrictEqual`). `ViewDecorationsCollection` (Monaco: `IViewDecorationsCollection`) is a
  `sealed record` with `IReadOnlyList<ViewModelDecoration> Decorations`,
  `IReadOnlyList<IReadOnlyList<InlineDecoration>> InlineDecorations` (one list per line of the range) and
  `IReadOnlyList<bool> HasVariableFonts` (always `false`: font decorations are not ported).
- **`InlineModelDecorationsComputer(Func<Range, IReadOnlyList<ModelDecoration>> getModelDecorations, TextModel model, ICoordinatesConverter converter)`**:
  the context interface becomes a delegate. `GetDecorations(Range viewRange)` drops Monaco's `onlyMinimapDecorations`
  and `onlyMarginDecorations` arguments (minimap: phase 6; margin-only: glyph margin lanes, not ported). The cache is a
  `Dictionary<string, ViewModelDecoration>` keyed by decoration id.
- **`ViewModelDecorations(int editorId, TextModel model, ICoordinatesConverter converter)`**: `Reset()`,
  `OnModelDecorationsChanged()`, `OnLineMappingChanged()`, `GetDecorationsViewportData(Range viewRange)` (cached per
  range, `==` on `Range`), `GetDecorationsOnLine(int lineNumber)`, `Dispose()`. The model query is
  `model.GetDecorationsInRange(viewRange, editorId)` (`ViewModelLinesFromModelAsIs`, view lines = model lines). Write
  `// Phase 5 cut point (hidden areas, step G): ViewModelLinesFromProjectedModel.getDecorationsInRange` there:
  folding replaces this call. `filterValidationDecorations` is `false` (comment, as in P5-06).
- **`isModelDecorationVisible`** reads `model.Tokenization.GetLineTokens(line)` (`FindTokenIndexAtOffset`,
  `GetStartOffset`, `GetStandardTokenType`); only called when `HideInCommentTokens` / `HideInStringTokens` is set.
- **View model.** `ViewModel.Decorations.cs` subscribes to `TextModel.DecorationsChanged`:
  `_decorations.OnModelDecorationsChanged()` then `EventDispatcher.EmitSingleViewEvent(new ViewDecorationsChangedEvent(e))`.
  Monaco's outgoing `ModelDecorationsChangedEvent` is not ported (collections listen to the model, P5-06). Public
  members: `IReadOnlyList<ViewModelDecoration> GetDecorationsInViewport(Range visibleRange)` and
  `IReadOnlyList<InlineDecoration> GetViewportInlineDecorations(Range visibleRange, int lineNumber)` (Monaco: the
  `inlineDecorations` of `getViewportViewLineRenderingData`).
- **`ViewDecorationsChangedEvent(ModelDecorationsChangedEventArgs? source)`**: Monaco's four flags; `null` → all true.
- **`ViewportData`** gets the `ViewModelDecorations` in its constructor and two members:
  `IReadOnlyList<ViewModelDecoration> GetDecorationsInViewport()` and
  `IReadOnlyList<InlineDecoration> GetInlineDecorations(int lineNumber)`; both read
  `GetDecorationsViewportData(VisibleRange)` (cached by `ViewModelDecorations`). Update the class remarks
  (`getDecorationsInViewport` is now ported).

## Tests

- [ ] Port (`inlineDecorations.test.ts` › `InlineModelDecorationsComputer`, 11 of 12; `getDecorations(range, false, false)` →
  `GetDecorations(range)`; `IdentityCoordinatesConverter` exists): `no decorations`, `inline class name decoration on a single line`,
  `inlineClassName with affectsLetterSpacing`, `beforeContentClassName decoration`, `afterContentClassName decoration`,
  `all decoration types combined`, `decoration spanning multiple lines`, `multiple decorations on different lines`,
  `decoration cache is used for same decoration id`, `reset clears decoration cache`,
  `getInlineDecorations returns inline decorations for a model line`.
  Not ported: `decoration with affectsFont sets hasVariableFonts` (font decorations), the
  `InjectedTextInlineDecorationsComputer` suite (21 tests, injected text: ADR 0009 decision 4).
- [ ] Port (`viewModelDecorations.test.ts` › `ViewModelDecorations`): `issue #37401: Allow both before and after decorations on empty line`
  (adapted: `GetViewportInlineDecorations(new Range(1, 1, 1, 1), 1)` instead of `getViewportViewLineRenderingData(…).inlineDecorations`).
  Not ported: `getDecorationsViewportData`, `issue #17208: Problem scrolling in 1.8.0` (word wrap: phase 6).
- [ ] New: `DecorationsChanged_EmitsViewDecorationsChangedEvent`
- [ ] New: `GetDecorationsInViewport_OtherEditorsDecorations_AreHidden` (owner filter with `EditorId`)
- [ ] New: `GetDecorationsInViewport_HideInCommentTokens_HidesDecorationInAComment` (a tokenization support that marks
  the line as a comment, like `TextViewRendererTokensTests`)
- [ ] New: `SetValue_ResetsTheCache`
- [ ] New: `ViewportData_GetInlineDecorations_ReturnsTheLinesInlineDecorations`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (`viewModelDecoration.ts`, `inlineDecorations.ts` subset, `viewModelDecorations.ts`,
      `viewEvents.ts` `ViewDecorationsChangedEvent`); tests table rows for the two test files
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing (P5-08, P5-09). Minimap and overview ruler decorations. `getTextDirection`.

## Suggested commit message

`feat(view): give the renderer the decorations of the viewport`
