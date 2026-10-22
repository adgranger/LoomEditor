# P5-89 — Folding commands and their keyboard shortcuts

- **Status:** done
- **Depends on:** P5-88
- **Size:** M

## Goal

The folding commands of VS Code exist with their ids and Windows shortcuts: fold / unfold (recursively), toggle,
fold all / unfold all, fold level 1–7, fold all except, fold / unfold all regions, fold all block comments, go to
parent / previous / next fold, create and remove manual folding ranges.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `contrib/folding/browser/folding.ts`.

| Lines | What to port |
|---|---|
| 546–585 | `FoldingAction`: run on the folding model, then `reveal(selection start)`; `getSelectedLines`, `getLineNumbers` |
| 635–697 | `UnfoldAction` (`editor.unfold`, Ctrl+Shift+]) |
| 698–717 | `UnFoldRecursivelyAction` (`editor.unfoldRecursively`, Ctrl+K Ctrl+]) |
| 718–788 | `FoldAction` (`editor.fold`, Ctrl+Shift+[) |
| 789–810 | `ToggleFoldAction` (`editor.toggleFold`, Ctrl+K Ctrl+L) |
| 811–832 | `FoldRecursivelyAction` (`editor.foldRecursively`, Ctrl+K Ctrl+[) |
| 833–854 | `ToggleFoldRecursivelyAction` (`editor.toggleFoldRecursively`, Ctrl+K Ctrl+Shift+L) |
| 855–886 | `FoldAllBlockCommentsAction` (`editor.foldAllBlockComments`, Ctrl+K Ctrl+/) |
| 887–918 | `FoldAllRegionsAction` (`editor.foldAllMarkerRegions`, Ctrl+K Ctrl+8) |
| 919–950 | `UnfoldAllRegionsAction` (`editor.unfoldAllMarkerRegions`, Ctrl+K Ctrl+9) |
| 951–972 | `FoldAllExceptAction` (`editor.foldAllExcept`, Ctrl+K Ctrl+-) |
| 973–993 | `UnfoldAllExceptAction` (`editor.unfoldAllExcept`, Ctrl+K Ctrl+=) |
| 994–1013 | `FoldAllAction` (`editor.foldAll`, Ctrl+K Ctrl+0) |
| 1014–1033 | `UnfoldAllAction` (`editor.unfoldAll`, Ctrl+K Ctrl+J) |
| 1034–1047, 1262–1275 | `FoldLevelAction` (`editor.foldLevel1` … `editor.foldLevel7`, Ctrl+K Ctrl+1 … Ctrl+K Ctrl+7) |
| 1048–1077 | `GotoParentFoldAction` (`editor.gotoParentFold`, no shortcut) |
| 1078–1107 | `GotoPreviousFoldAction` (`editor.gotoPreviousFold`, no shortcut) |
| 1108–1136 | `GotoNextFoldAction` (`editor.gotoNextFold`, no shortcut) |
| 1137–1187 | `FoldRangeFromSelectionAction` (`editor.createFoldingRangeFromSelection`, Ctrl+K Ctrl+,) |
| 1188–1211 | `RemoveFoldRangeFromSelectionAction` (`editor.removeManualFoldingRanges`, Ctrl+K Ctrl+.) |

## Files to create or change

- `src/LoomEditor.View/Features/Folding/FoldingCommands.cs` — create: the command ids (constants)
- `src/LoomEditor.View/Features/Folding/FoldingFeature.Commands.cs` — create (partial): registration and the `Invoke` bodies
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: the rules above, `KeybindingWeight.EditorContrib`,
  `when` = `WhenClauses.EditorTextFocus`, two-key chords with the chord support of **P5-47**
- `src/LoomEditor.View/Commands/KeyCode.cs`, `src/LoomEditor.WinUI/Input/KeyMapping.cs` — change: the key codes still
  missing after P5-47 among `BracketLeft`, `BracketRight`, `Digit0` … `Digit9`, `Minus`, `Equal`, `Comma`, `Period`, `Slash`
  (Monaco names; `VirtualKey` / OEM key mapping for a US layout as VS Code)
- `src/LoomEditor.View/Features/Folding/README.md` — change: the command table
- `tests/LoomEditor.View.Tests/Features/Folding/FoldingCommandsTests.cs` — create
- `tests/LoomEditor.View.Tests/Commands/DefaultKeybindingsTests.cs` — change: a few folding rules

## Design decisions (already made — do not change)

- Commands take **no arguments** (Monaco's `FoldingArguments` `levels` / `direction` / `selectionLines` come from
  `executeCommand` callers only); `getLineNumbers` is `getSelectedLines` (the start line of every selection).
- Monaco's precondition `foldingEnabled` is checked in the command: when folding is disabled or there is no folding
  model, the command does nothing. The keybinding `when` is `editorTextFocus` only.
- Every command goes through `FoldingFeature.GetFoldingModel()` (pending update run at once, P5-88), then
  `reveal(primary selection start)` as `FoldingAction.runEditorCommand` does.
- `foldAllBlockComments`: the indentation provider gives no types, so the regex branch runs:
  `^\s*` + `Strings.EscapeRegExpCharacters(comments.BlockCommentStartToken)` from the language configuration (step B); nothing when the
  language has no block comment. `foldAllMarkerRegions` / `unfoldAllMarkerRegions`: `new Regex(FoldingRules.Markers.Start.ToString(), Markers.Start.Options)`.
- Fold jumping commands set the selection with `viewModel.SetSelections("api", [collapsed position (line, 1)])` and
  reveal as Monaco (`revealInCenterIfOutsideViewport`).
- `editor.toggleImportFold` is **not ported** (needs the `imports` range kind of syntax providers: phase 8).
- Keybindings are added in Monaco's registration order (1242–1275), after the existing contrib rules.

## Tests

No Monaco test covers the actions. New, with the feature attached to a `TestCodeEditor` and the scheduler flushed:

- [x] `Fold_FoldsTheRegionAtTheCursor`, `Unfold_UnfoldsIt`, `ToggleFold_Twice_RestoresTheState`
- [x] `FoldRecursively_FoldsTheRegionAndItsChildren`, `UnfoldRecursively_UnfoldsAll`
- [x] `FoldAll_ThenUnfoldAll`
- [x] `FoldLevel2_FoldsOnlySecondLevelRegions`
- [x] `FoldAllExcept_KeepsTheCursorRegionsOpen`
- [x] `FoldAllMarkerRegions_FoldsRegionMarkers` (C# `#region`)
- [x] `FoldAllBlockComments_FoldsBlockComments` (C# `/* … */`, `/**` doc block)
- [x] `GotoParentFold`, `GotoNextFold`, `GotoPreviousFold` move the cursor to the expected line
- [x] `CreateFoldingRangeFromSelection_FoldsTheSelectedLines`, `RemoveManualFoldingRanges_RemovesIt`
- [x] `FoldingDisabled_CommandsDoNothing`
- [x] Keybindings: `Ctrl+Shift+[` → `editor.fold`; `Ctrl+K Ctrl+0` → `editor.foldAll`; `Ctrl+K Ctrl+7` → `editor.foldLevel7`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `folding.ts` row (actions, P5-89; `toggleImportFold` not ported)
- [x] Card status and `docs/tasks/README.md` updated
- [ ] Manual check (maintainer, Gallery) — left to the maintainer: `Ctrl+K Ctrl+0` folds everything, `Ctrl+K Ctrl+J` unfolds; `Ctrl+Shift+[` on a
      `{` line folds it and the lines below disappear

## Out of scope

The gutter chevrons and clicks (P5-90, P5-91).

## Suggested commit message

`feat(folding): add the folding commands and shortcuts`
