# P9-26 — Go to next / previous change

- **Status:** done
- **Depends on:** P9-25
- **Size:** S

## Goal

`LoomDiffEditor.GoToNextChange()` / `GoToPreviousChange()` (keys `Alt+F5` / `Shift+Alt+F5` inside either editor) move the cursor of the modified editor to the next / previous change and reveal it, wrapping around; the first change can be revealed after the diff is computed.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/diffEditorWidget.ts` | 724–757 | `_goTo`, `goToDiff('previous' \| 'next')` (the wrap-around and the "last line" rule), without the accessibility signal sounds |
| same | 758–770 | `revealFirstDiff` |

The keys have no Monaco counterpart (VS Code binds `Alt+F5` / `Shift+Alt+F5` in the workbench: `workbench.action.compareEditor.nextChange`); Monaco's own `F7` / `Shift+F7` belong to the accessible diff viewer (P9-30).

## Files to create or change

- `src/LoomEditor.View/DiffEditor/DiffNavigator.cs` — create (pure)
- `src/LoomEditor.View/Features/DiffNavigation/DiffNavigationFeature.cs`, `DiffNavigationCommands.cs`, `README.md` — create (**all three required by `ConventionTests`**, as for every feature; `Id = "loom.diffNavigation"`; the feature is **not** added to `EditorFeatures.Default`: the diff control appends it to its sub editors; check `EditorFeatures.cs` and the architecture test that lists features, and follow what it requires for a feature that is not in the default list)
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs`, `WhenClauses.cs`, `KeybindingContext.cs` — change: `WhenClauses.InDiffEditor` (`bool IsInDiffEditor`, default false, set by the diff control for its sub editors; Monaco: `isInDiffEditor`), the two bindings (`KeyCode F5` with `Alt` / `Shift+Alt`), weight `EditorContrib`
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.Navigation.cs` — create: public `GoToNextChange()`, `GoToPreviousChange()`, `Task RevealFirstChangeAsync(CancellationToken = default)`; the diff control appends `() => new DiffNavigationFeature(…)` to each sub editor's `Features` list
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — regenerate
- `tests/LoomEditor.View.Tests/DiffEditor/DiffNavigatorTests.cs` — create

## Design decisions (already made — do not change)

- `static DiffMapping? DiffNavigator.Find(IReadOnlyList<DiffMapping> mappings, int currentModifiedLine, int modifiedLineCount, NavigationDirection direction)` (`enum NavigationDirection { Previous, Next }`): `mappings` empty → null; `Next`: when `currentModifiedLine == modifiedLineCount` → `mappings[0]`, else the first mapping with `Modified.StartLineNumber > currentModifiedLine`, or `mappings[0]`; `Previous`: the last mapping with `Modified.StartLineNumber < currentModifiedLine`, or the last mapping (Monaco `findLast(...) ?? diffs[length-1]`).
- Going to a mapping (`_goTo`): set the modified editor's cursor to `(Modified.StartLineNumber, 1)` and reveal the range `[StartLineNumber, 1 → EndLineNumberExclusive, 1]` **in the center** (`RevealRangeInCenter`: use the existing reveal API, `LoomEditor.Reveal.cs`, with `VerticalRevealType.Center`). A mapping whose `Modified` range is empty (pure deletion) goes to its start line (clamped to the model). Selection changes reveal hidden unchanged lines through P9-25 automatically.
- The command works while either sub editor has focus; focus moves to the modified editor after the jump.
- `RevealFirstChangeAsync` awaits `WaitForDiffAsync` and jumps to `mappings[0]` if any (no wrap logic).
- No sounds (Monaco plays accessibility signals): not ported.
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [x] New (`DiffNavigatorTests`): `Find_Empty_Null`, `Find_Next_FirstAfterCursor`, `Find_Next_AtLastLine_WrapsToFirst`, `Find_Next_NoneAfter_WrapsToFirst`, `Find_Next_CursorOnChangeStart_GoesToTheNextOne`, `Find_Previous_LastBeforeCursor`, `Find_Previous_NoneBefore_WrapsToLast`, `Find_Previous_CursorOnChangeStart_GoesToThePreviousOne`.
- [x] New (feature, EditorContext-driven like the other features' tests): `Command_Next_InvokesNavigatorAndMovesCursor` (with a fake navigator target), `Keybinding_AltF5_ResolvesWhenInDiffEditor`, `Keybinding_AltF5_NotResolvedOutsideDiffEditor`.
- [ ] Manual (P9-31's plan): `Alt+F5` cycles through the changes and wraps; a change inside a hidden unchanged region is revealed.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green (architecture tests included)
- [x] `./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI` run, diff checked
- [x] `docs/PORTING.md`: `goToDiff`, `revealFirstDiff` (keys from VS Code's workbench, not Monaco)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The accessible diff viewer (P9-29, P9-30), change counters in a status bar.

## Suggested commit message

`feat: go to next and previous change in the diff editor`
