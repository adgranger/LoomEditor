# P5-65 — The Find feature folder, `FindReplaceState` and the selection search string

- **Status:** todo
- **Depends on:** P5-64
- **Size:** M

## Goal

`Features/Find/` exists with the state every find part shares: `FindReplaceState` (search string, replace string,
toggles with their overrides, search scope, "n of m", loop) and its change event, the `find.*` options record, the command
ids, and `getSelectionSearchString` (what Ctrl+F puts in the find box). The feature is listed in `EditorFeatures` but does
nothing yet (commands come with P5-71).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/find/browser/findState.ts` | 11–333 | `FindReplaceStateChangedEvent`, `FindOptionOverride`, `INewFindReplaceState`, `effectiveOptionValue`, `FindReplaceState` (without `filters`) |
| `contrib/find/browser/findModel.ts` | 60–82 | `FIND_IDS`, `MATCHES_LIMIT` (19999), `RESEARCH_DELAY` (240) |
| `contrib/find/browser/findController.ts` | 41–66 | `SEARCH_STRING_MAX_LENGTH`, `getSelectionSearchString` |
| `common/config/editorOptions.ts` | 1746–1914 | `IEditorFindOptions` / `EditorFind` defaults (the subset below) |
| `contrib/find/test/browser/find.test.ts` | 14–89 | suite `Find` |

## Files to create or change

- `src/LoomEditor.View/Features/Find/FindFeature.cs` — create: **public** `FindFeature : IEditorFeature`, `Id = "loom.find"`,
  `Attach` / `Dispose` do nothing yet (P5-71)
- `src/LoomEditor.View/Features/Find/FindCommands.cs` — create: **public** static class with the ids of `FIND_IDS` that phase 5
  registers (see decisions)
- `src/LoomEditor.View/Features/Find/FindReplaceState.cs`, `FindReplaceStateChangedEventArgs.cs`, `NewFindReplaceState.cs`,
  `FindOptionOverride.cs` — create
- `src/LoomEditor.View/Features/Find/EditorFindOptions.cs` — create
- `src/LoomEditor.View/Features/Find/SelectionSearchString.cs` — create (`getSelectionSearchString`)
- `src/LoomEditor.View/Features/Find/README.md` — create (role, Monaco sources, files, limits; later cards extend it)
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: add `() => new FindFeature()` **before** `MultiCursorFeature`
  (P5-75 looks the find controller up when it attaches)
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — via `./eng/Update-PublicApi.ps1 src/LoomEditor.View` (`FindFeature`, `FindCommands`)
- `tests/LoomEditor.View.Tests/Features/Find/FindReplaceStateTests.cs`, `SelectionSearchStringTests.cs` — create

## Design decisions (already made — do not change)

- `FindReplaceState` is `internal sealed class`, not generic (`filters` is a notebook concept: not ported).
  `event EventHandler<FindReplaceStateChangedEventArgs>? Changed` replaces `onFindReplaceStateChange`.
- `FindReplaceStateChangedEventArgs : EventArgs` has `bool` get-only properties named like Monaco's fields: `MoveCursor`,
  `UpdateHistory`, `SearchString`, `ReplaceString`, `IsRevealed`, `IsReplaceRevealed`, `IsRegex`, `WholeWord`, `MatchCase`,
  `PreserveCase`, `SearchScope`, `MatchesPosition`, `MatchesCount`, `CurrentMatch`, `Loop`, `IsSearching`. Build it with
  `init` setters; raise it only when something changed (Monaco `somethingChanged`).
- `INewFindReplaceState` → `internal sealed record NewFindReplaceState` with nullable members (`string? SearchString`,
  `bool? IsRegex`, `FindOptionOverride? IsRegexOverride`, …, `bool? Loop`, `bool? IsSearching`). `searchScope` has three
  states (absent, `null`, ranges): `IReadOnlyList<Range>? SearchScope { get; init; }` whose `init` also sets
  `bool HasSearchScope = true` — writing `SearchScope = null` in an initializer means "clear the scope".
- `change(newState, moveCursor, updateHistory = true)` → `Change(NewFindReplaceState newState, bool moveCursor, bool updateHistory = true)`;
  `changeMatchInfo(position, count, currentMatch | undefined)` → `ChangeMatchInfo(int matchesPosition, int matchesCount, Range? currentMatch, bool setCurrentMatch)`
  (`setCurrentMatch == false` is Monaco's `undefined`). `Range.equalsRange(a, b)` with `null`s → `a == b` on `Range?`.
- The search scope is stored as `IReadOnlyList<Range>`; callers pass `selection.Range`. (Monaco stores `Selection`s, which are
  `Range`s; tests compare ranges.)
- `canNavigateBack`, `canNavigateForward`, `canNavigateInLoop` are ported (the widget uses them, P5-74).
- `EditorFindOptions` (internal sealed record, VS Code defaults, ADR 0009 decision 12): `CursorMoveOnType = true`,
  `FindOnType = true`, `SeedSearchStringFromSelection = SeedSearchStringMode.Always` (enum `Never`, `Always`, `Selection`),
  `AutoFindInSelection = AutoFindInSelectionMode.Never` (enum `Never`, `Always`, `Multiline`), `Loop = true`,
  `CloseOnResult = false`. Not ported: `globalFindClipboard` (macOS only), `addExtraSpaceOnTop` (needs view zones),
  `history`, `replaceHistory` (no search history, ADR 0009 decision 11).
- `getSelectionSearchString(editor, seedSearchStringFromSelection = 'single', seedSearchStringFromNonEmptySelection = false)` →
  `SelectionSearchString.Get(ViewModel viewModel, SeedSearchStringFromSelection mode = Single, bool seedSearchStringFromNonEmptySelection = false)`
  (enum `None`, `Single`, `Multiple` — Monaco's `'none' | 'single' | 'multiple'`). `editor.getConfiguredWordAtPosition` →
  `WordOperations.GetWordAtPosition(model, config.WordSeparators, config.WordSegmenterLocales, position)` (phase 3).
- `FindCommands` constants (Monaco ids): `StartFindAction = "actions.find"`, `StartFindWithSelection = "actions.findWithSelection"`,
  `NextMatchFindAction`, `PreviousMatchFindAction`, `NextSelectionMatchFindAction`, `PreviousSelectionMatchFindAction`,
  `StartFindReplaceAction`, `CloseFindWidgetCommand = "closeFindWidget"`, `ToggleCaseSensitiveCommand`, `ToggleWholeWordCommand`,
  `ToggleRegexCommand`, `ToggleSearchScopeCommand`, `TogglePreserveCaseCommand`, `ReplaceOneAction`, `ReplaceAllAction`,
  `SelectAllMatchesAction`. Not registered in phase 5 (write it in the README): `editor.actions.findWithArgs` (commands
  have no arguments), `editor.action.goToMatchFindAction` (quick input UI).
- `MatchesLimit = 19999` and `ResearchDelay = TimeSpan.FromMilliseconds(240)` are `internal const` / `static readonly` on
  a static class `FindConstants`.

## Tests

- [ ] Port `find.test.ts` › suite `Find` (3): `search string at position`, `search string with selection`,
      `search string with multiline selection` — with `TestCodeEditor.WithTestCodeEditor` (P3-20).
- [ ] New: `Change_SameValues_RaisesNoEvent`, `Change_MatchCaseOverride_ChangesEffectiveValueOnly` (`MatchCase` vs
      `ActualMatchCase`), `Change_SearchScopeNull_ClearsScope`, `ChangeMatchInfo_PositionAboveCount_IsClamped`,
      `CanNavigateForward_LoopOff_FalseOnLastMatch`, `FindFeature_IsListedBeforeMultiCursorFeature`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (including the architecture tests: the folder has
      `FindFeature.cs`, `FindCommands.cs`, `README.md` and is listed in `EditorFeatures.cs`)
- [ ] `PublicAPI.Unshipped.txt` of `LoomEditor.View`: only `FindFeature` and `FindCommands`
- [ ] `docs/PORTING.md`: map rows `contrib/find/browser/findState.ts` ✅ (P5-65), `findController.ts` 🔶 (`getSelectionSearchString`),
      `findModel.ts` 🔶 (ids, limits); tests table `find.test.ts` 3 / 3
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Replace patterns (P5-66), decorations (P5-67), the find model (P5-68), the controller and commands (P5-70, P5-71).

## Suggested commit message

`feat(view): add the find feature folder and the find/replace state`
