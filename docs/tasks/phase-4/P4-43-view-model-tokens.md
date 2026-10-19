# P4-43 — View model: tokens in the viewport data, visible lines, tokens-changed events

- **Status:** todo
- **Depends on:** P4-42
- **Size:** M

## Goal

The view model gives the renderer the tokens of each visible line, tells the model which lines are visible (so they
are tokenized first), and turns the model's `TokensChanged` into a view event that makes the editor redraw.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/editor/common/viewModel/viewModelImpl.ts` | 236–245 | `visibleLinesStabilized`, `_handleVisibleLinesChanged` (the attached view's `setVisibleLines`) |
| vscode: same file | 513–527 | the `onDidChangeTokens` handler → `ViewTokensChangedEvent` |
| vscode: same file | 891–925 | `getViewLineRenderingData`, `_getViewLineRenderingData` (the token part), `getViewLineData` |
| vscode: `src/vs/editor/common/viewModel/modelLineProjection.ts` | 263–348 | `IdentityModelLineProjection.getViewLineData` (`getLineTokens(…).inflate()`) — view lines are model lines (ADR 0005, decision 6) |
| vscode: `src/vs/editor/common/viewEvents.ts` | search `ViewTokensChangedEvent` | `ViewTokensChangedEvent` |

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModel.cs` (or a new partial `ViewModel.Tokens.cs`) — change: attach a view to the
  model, `SetVisibleLines` when the viewport changes, `GetViewLineTokens(lineNumber)`
- `src/LoomEditor.View/Layout/ViewportData.cs` — change: the tokens of each visible line (`IViewLineTokens`)
- `src/LoomEditor.View/Events/ViewEvent.cs` — change: `ViewTokensChangedEvent` (ranges of lines)
- `tests/LoomEditor.View.Tests/Layout/ViewModelTokensTests.cs` — create

## Design decisions (already made — do not change)

- The view model attaches **one** view to the model when it is created and detaches it when disposed (Monaco:
  `onBeforeAttached` / `onBeforeDetached` per view).
- The visible lines are reported when the viewport changes (scroll, resize, edit) with `stabilized = false`, and once
  more with `stabilized = true` after the frame has been drawn (Monaco's `visibleLinesStabilized`), through the
  existing animation-frame scheduler of `ViewModel`.
- `ViewTokensChangedEvent` goes through the existing `ViewModelEventDispatcher` (P3-17).
- `ViewportData` gets the tokens lazily per line (`GetTokens(lineNumber)`), not an array built for every frame.

## Tests

New:

- [ ] `Viewport_ReportsVisibleLinesToTheModel`
- [ ] `ModelTokensChanged_RaisesViewTokensChangedEvent`
- [ ] `ViewportData_GetTokens_ReturnsInflatedLineTokens`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing the colors (P4-44).

## Suggested commit message

`feat(view): give the renderer the tokens of the visible lines`
