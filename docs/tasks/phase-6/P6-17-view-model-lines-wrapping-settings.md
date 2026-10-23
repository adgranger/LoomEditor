# P6-17 — `ViewModelLinesFromProjectedModel`: wrapping settings and wrapped construction

- **Status:** done
- **Depends on:** P6-16, P6-13, P6-05
- **Size:** M

## Goal

The view lines are built with line breaks: `ViewModelLinesFromProjectedModel` owns the wrapping settings, creates a
line-breaks computer (with the model's injected text for its editor) and builds a wrapped projection per model line.
`SetWrappingSettings` rebuilds the lines (reusing the previous breaks when only the column changed).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/viewModel/viewModelLines.ts`.

| Lines | What to port |
|---|---|
| 22–58 | `IViewModelLines`: `setWrappingSettings`, `createLineBreaksComputer` (informative: no interface is ported) |
| 61–116 | the fields and the constructor (the wrapping fields) |
| 126–170 | `_constructLines` with `previousLineBreaks` and the line-breaks computer |
| 270–279 | `setTabSize` (it rebuilds with `previousLineBreaks = null`) |
| 281–312 | `setWrappingSettings` (`onlyWrappingColumnChanged` → previous breaks) |
| 314–329 | `createLineBreaksComputer` (the default context reads `getLineContent` and `getLineInjectedText(lineNumber, editorId)`) |

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModelLinesFromProjectedModel.cs` — change (fields, constructor, `ConstructLines`, `SetTabSize`)
- `src/LoomEditor.View/Layout/ViewModelLinesFromProjectedModel.Wrapping.cs` — create (partial: `SetWrappingSettings`,
  `CreateLineBreaksComputer`, the private context class)
- `tests/LoomEditor.View.Tests/Layout/ViewModelLinesWrappingTests.cs` — create
- `tests/LoomEditor.View.Tests/Layout/TestHelpers/RecordingLineBreaksComputerFactory.cs` — create (wraps a real factory and records
  the `previousLineBreakData` it receives)

## Design decisions (already made — do not change)

- New primary constructor signature:
  `ViewModelLinesFromProjectedModel(int editorId, TextModel model, ILineBreaksComputerFactory monospaceLineBreaksComputerFactory, FontInfo fontInfo, int tabSize, int wrappingColumn, WrappingIndent wrappingIndent, WordBreak wordBreak, bool wrapOnEscapedLineFeeds, bool useTwoCellFullwidthCharacters)`.
  Monaco's `domLineBreaksComputerFactory` and `wrappingStrategy` are not ported (P6-01). The existing
  `(int editorId, TextModel model, int tabSize)` constructor stays as a convenience overload for the tests: it uses
  `MonospaceLineBreaksComputerFactory.Create(new EditorWrappingOptions())`, `FontInfo.Default`, `wrappingColumn: -1`,
  `WrappingIndent.None`, `WordBreak.Normal`, `false`, `false` (no wrapping: identical behavior to today).
- `SetWrappingSettings(FontInfo fontInfo, int wrappingColumn, WrappingIndent wrappingIndent, WordBreak wordBreak, bool useTwoCellFullwidthCharacters)`
  returns `bool` (changed). `fontInfo.equals` → record equality (`FontInfo` is a record).
- `ConstructLines(bool resetHiddenAreas, ModelLineProjectionData?[]? previousLineBreaks)`; it asks the computer for every line
  (Monaco reads `getLinesContent()` only for the count: use `_model.LineCount`), then
  `ModelLineProjection.Create(linesBreaks[i], !isInHiddenArea)`.
- `CreateLineBreaksComputer(ILineBreaksComputerContext? context = null)`: the default context is a private sealed class
  holding the model and the editor id; `GetLineInjectedText` returns `_model.GetLineInjectedText(lineNumber, _editorId)`, a
  list that is never `null` (possibly empty), like Monaco's array. Consequence, faithful to Monaco: the factory's fast path
  tests `injectedText is null` (JavaScript `!injectedText` is `false` for an empty array, P6-12), so with the model context the
  fast path is never taken and `SetWrappingSettings` passes previous breaks that the factory ignores. Do not "fix" this.
- The model change methods keep calling `ModelLineProjection.Create(bool)` in this card (P6-18 passes line breaks).

## Tests

New (the upstream wrapping tests come in P6-19):

- [x] `Constructor_WrappingColumn_WrapsLongLines` (column 10, a 25-char line → 3 view lines; `GetViewLineContent` of each)
- [x] `Constructor_NoWrapping_HasOneViewLinePerModelLine`
- [x] `SetWrappingSettings_SameSettings_ReturnsFalse`
- [x] `SetWrappingSettings_OnlyColumnChanged_PassesThePreviousBreaks` (with the recording factory: it records what
      `AddRequest` receives)
- [x] `SetWrappingSettings_IndentChanged_DoesNotPassPreviousBreaks`
- [x] `SetWrappingSettings_KeepsHiddenAreas`
- [x] `SetTabSize_RebuildsWithoutPreviousBreaks`
- [x] `Constructor_InjectedTextOfAnotherEditor_IsIgnored` (a decoration with `After` owned by editor 2, lines of editor 1)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green (the existing `ViewModelLines*` tests keep passing)
- [x] `docs/PORTING.md`: `viewModelLines.ts` map row mentions the wrapping settings
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Model changes with line breaks (P6-18), the view model (P6-20).

## Suggested commit message

`feat(view): build wrapped view lines`
