# P5-35 — BracketMatching feature: highlight, jump, select, remove

- **Status:** todo
- **Depends on:** P5-34
- **Size:** M

## Goal

The `BracketMatching` feature exists: 50 ms after the cursor moves, the bracket pair at (or, with `matchBrackets:
always`, around) each empty selection is highlighted (`bracket-match` box and background); `editor.action.jumpToBracket`
(Ctrl+Shift+\\), `editor.action.selectToBracket` and `editor.action.removeBrackets` (Ctrl+Alt+Backspace) work, with
Monaco's tests.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/bracketMatching/browser/bracketMatching.ts` | 29–99 | `JumpToBracketAction`, `SelectToBracketAction` (argument `selectBrackets`, default `true`), `RemoveBracketsAction` (ids, keybindings) |
| same | 101–113 | `Brackets`, `BracketsData` |
| same | 115–180 | `BracketMatchingController`: fields, constructor and its event subscriptions |
| same | 182–298 | `jumpToBracket`, `selectToBracket`, `removeBrackets` |
| same | 300–316 | the two decoration options (`bracket-match-overview`, `bracket-match-no-overview`) |
| same | 318–403 | `_updateBrackets`, `_recomputeBrackets` |
| same | 421–429 | theming participant: `bracket-match-inline` foreground |
| `contrib/bracketMatching/browser/bracketMatching.css` | 6–10 | `.bracket-match` (border box, `editorBracketMatch.background`, 1 px border `editorBracketMatch.border`) |
| `common/config/editorOptions.ts` | 6482–6487 | `matchBrackets`: `'always' \| 'near' \| 'never'`, default `'always'` |
| `contrib/bracketMatching/test/browser/bracketMatching.test.ts` | 17–237 | suite `bracket matching` (7 tests) and its helpers |

## Files to create or change

- `src/LoomEditor.View/Features/BracketMatching/BracketMatchingFeature.cs` — create (Monaco's controller; `Id` = `"loom.bracketMatching"`)
- `src/LoomEditor.View/Features/BracketMatching/BracketMatchingCommands.cs` — create (`JumpToBracket = "editor.action.jumpToBracket"`, `SelectToBracket = "editor.action.selectToBracket"`, `RemoveBrackets = "editor.action.removeBrackets"`)
- `src/LoomEditor.View/Features/BracketMatching/MatchBracketsMode.cs` — create (`internal enum MatchBracketsMode { Always, Near, Never }`)
- `src/LoomEditor.View/Features/BracketMatching/README.md` — create (role, Monaco sources, limits)
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: one line
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: Ctrl+Shift+Backslash → jumpToBracket, Ctrl+Alt+Backspace → removeBrackets (`WhenClauses.EditorTextFocus`, `KeybindingWeight.EditorContrib`)
- `src/LoomEditor.View/Commands/KeyCode.cs` — change: add `Backslash`
- `src/LoomEditor.WinUI/Input/KeyMapping.cs` — change: `(VirtualKey)220` (`VK_OEM_5`) → `KeyCode.Backslash`
- `src/LoomEditor.View/Layout/DecorationStyle.cs` (P5-08) — change: `bool Important = false`; P5-09's inline foreground code applies important overrides after the others. Set it on the existing `bracket-match-inline` row of `DecorationStyles.cs`
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change if missing: `bool HasFocus` getter and `event EventHandler? FocusChanged` next to `SetHasFocus`
- `tests/LoomEditor.View.Tests/Features/BracketMatching/BracketMatchingTests.cs` — create
- `tests/LoomEditor.View.Tests/Features/BracketMatching/TestHelpers/BracketEditor.cs` — create (port of `createTextModelWithBrackets` / `createCodeEditorWithBrackets` of the test: a model with the language configuration `brackets: {}, [], ()` in a `TestCodeEditor`)

## Design decisions (already made — do not change)

- Feature conventions of `docs/how-to/add-a-feature.md` and ADR 0009 decisions 10, 12, 13. The feature gets the view
  model with `context.GetViewModel()` (as `MultiCursorFeature`) and does nothing without one.
- **Scheduling**: `new RunOnceScheduler(context.Scheduler, UpdateBrackets, TimeSpan.FromMilliseconds(50))` (P5-01,
  `Features/Scheduling/`); `schedule()` at creation and on: cursor position change (unless `Never`), model
  content change, and language configuration change (`onDidChangeModelLanguageConfiguration` = the model's
  `LanguageConfigurationChanged` of P5-15, also raised after a language change; it clears `_lastBracketsData`), focus
  change (`onDidBlurEditorWidget` / `onDidFocusEditorWidget` → `ViewModel.FocusChanged`). `onDidChangeModel` is not
  ported: the model of a context never changes (`SetText` calls `SetValue`).
- **Option**: `internal MatchBracketsMode MatchBrackets { get; set; }` on the feature, default `Always`. Setting a new
  value does what `onDidChangeConfiguration` does (clear decorations, `_lastBracketsData`, `_lastVersionId = 0`,
  schedule). No public option on the control (ADR 0009 decision 12).
- **Decorations** through `EditorDecorationsCollection` (`EditorContext.CreateDecorationsCollection()` of P5-06, reached like `GetViewModel()`;
  `Set(...)`, `Clear()`). The two options are static `ModelDecorationOptions.Register(...)` with Monaco's
  descriptions, `NeverGrowsWhenTypingAtEdges`, `ClassName = "bracket-match"`, `InlineClassName =
  "bracket-match-inline"`; the overview ruler part is not ported (no overview ruler), so both options differ only by
  their description: keep both (Monaco uses which one to tell a match from an enclosing pair).
- **Styles**: the rows `bracket-match` and `bracket-match-inline` already exist (P5-08). `bracket-match-inline`'s
  foreground (`editorBracketMatch.foreground`, no default color) must win over every other inline foreground,
  including the bracket colors of P5-34 (Monaco's `!important`, bracketMatching.ts 421–429): P5-09 applies overrides
  "in list order, a later one winning" and the colorization decorations come after the model decorations, so the row
  gets `Important = true` and important overrides are applied last.
- `matchBracket(position, 20)` / `findEnclosingBrackets(position, 20)` pass `TimeSpan.FromMilliseconds(20)`.
  `selections.length > 100` and `!hasWidgetFocus()` (`ViewModel.HasFocus`) clear the data as in Monaco.
  `positions.sort(Position.compare)` is a stable sort (`OrderBy`, phase-3 README trap 3).
- **Commands**: `editor.setSelections(…)` → `context.SetSelections`; `editor.revealRange(newSelections[0])` →
  `ViewModel.RevealRange(source, revealHorizontal: true, viewRange, VerticalRevealType.Simple, ScrollType.Smooth)`
  with the model range converted to the view (Monaco's `revealRange` defaults); `pushUndoStop` →
  `Model.PushStackElement()`; `executeEdits(source, edits)` → `ViewModel.ExecuteEdits` with source
  `"editor.action.removeBrackets"`. `selectToBracket` registers a command without argument (`selectBrackets = true`);
  the method `SelectToBracket(bool selectBrackets)` is internal for the test `issue #43371`. `brackets.sort(...)`
  sorts the two ranges with `Range.CompareUsingStarts`.
- Keybinding `Ctrl+Shift+\` uses the OEM_5 virtual key: on layouts where `\` is elsewhere, the shortcut follows the
  key position of a US keyboard (documented in the feature README, like VS Code's scan-code dispatch).

## Tests

- [ ] Port suite `bracket matching` › `issue #183: jump to matching bracket position`, `Jump to next bracket`,
      `Select to next bracket`, `issue #1772: jump to enclosing brackets`, `issue #43371: argument to not select
      brackets`, `issue #45369: Select to Bracket with multicursor`, `Removes brackets`
      (`editor.registerAndInstantiateContribution` → a `BracketMatchingFeature` attached to the test editor's context)
- [ ] New (with `ManualEditorScheduler`): `CursorOnBracket_After50ms_HighlightsBothBrackets`
- [ ] New: `CursorInsidePair_Always_HighlightsEnclosingPair`, `…_Near_HighlightsNothing`, `…_Never_HighlightsNothing`
- [ ] New: `MoreThan100Selections_HighlightsNothing`
- [ ] New: `NoFocus_HighlightsNothing`
- [ ] New: `SameVersion_ReusesPreviousBracketsData` (no new `MatchBracket` call for an unchanged position)
- [ ] New: `Keybindings_CtrlShiftBackslash_And_CtrlAltBackspace_AreBound`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `contrib/bracketMatching/browser/bracketMatching.ts` ✅ (P5-35); tests row
      `bracketMatching.test.ts` 7 / 7
- [ ] Card status and `docs/tasks/README.md` updated
- [ ] Manual check in the Gallery: the brackets around the caret get a box; Ctrl+Shift+\ jumps between them;
      Ctrl+Alt+Backspace removes them (one undo step restores them)

## Out of scope

The overview ruler marker (`editorOverviewRuler.bracketMatchForeground`), the Go menu entry, the double-click
selection (P5-36).

## Suggested commit message

`feat(view): add the bracket matching feature`
