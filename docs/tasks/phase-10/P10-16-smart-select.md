# P10-16 — Smart select: expand and shrink the selection

- **Status:** done
- **Depends on:** P9-33
- **Size:** M

## Goal

`editor.action.smartSelect.expand` (`Shift+Alt+Right`) grows each selection to the next enclosing range (subword, word, bracket pair, whitespace-only line, block lines, the whole document), `editor.action.smartSelect.shrink` (`Shift+Alt+Left`) goes back. Only the two built-in range providers exist (word, bracket); there is no provider registry yet.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/smartSelect/browser/smartSelect.ts` | 33–52 | `SelectionRanges` (`mov` with the skip of equal ranges) |
| `src/vs/editor/contrib/smartSelect/browser/smartSelect.ts` | 54–135 | `SmartSelectController.run` (the state, the filter of ranges inside the selection, prepending the current selection, the listener that forgets the state when the caret moves, `_ignoreSelection`) |
| `src/vs/editor/contrib/smartSelect/browser/smartSelect.ts` | 137–201 | `AbstractSmartSelect`, `GrowSelectionAction` (`Shift+Alt+Right`), `ShrinkSelectionAction` (`Shift+Alt+Left`), ids |
| `src/vs/editor/contrib/smartSelect/browser/smartSelect.ts` | 207–296 | `SelectionRangesOptions` and `provideSelectionRanges` (sort, de-duplicate by containment, the "trivia" ranges for `selectLeadingAndTrailingWhitespace`) |
| `src/vs/editor/contrib/smartSelect/browser/wordSelections.ts` | 13–91 | `WordSelectionRangeProvider` (in-word ranges for `fooBar` / `foo_bar` / `foo-bar`, the word, the whitespace-only line, the whole model) |
| `src/vs/editor/contrib/smartSelect/browser/bracketSelections.ts` | 12–157 | `BracketSelectionRangeProvider` (brackets right then left, up to 2 rounds, builds the inner and outer ranges of each pair) |
| `src/vs/editor/contrib/smartSelect/test/browser/smartSelect.test.ts` | whole file | the tests (the ones that use a registered `SelectionRangeProvider` are adapted, see below) |

## Files to create or change

- `src/LoomEditor.View/Features/SmartSelect/SelectionRanges.cs`, `SmartSelectRanges.cs` (the `provideSelectionRanges` function), `WordSelectionRangeProvider.cs`, `BracketSelectionRangeProvider.cs`, `ISelectionRangeProvider.cs`, `SmartSelectFeature.cs`, `SmartSelectCommands.cs`, `README.md` — create (headers: ported from the matching files)
- `src/LoomEditor.View/Features/EditorFeatures.cs`, `src/LoomEditor.View/Commands/DefaultKeybindings.cs` (`Shift+Alt+Right` / `Left`, when `EditorTextFocus`) — change
- `tests/LoomEditor.View.Tests/Features/SmartSelect/SmartSelectTests.cs` — create

## Design decisions (already made — do not change)

- **Synchronous.** The Monaco code is `async` because providers may be remote; here there are only two local providers, so `ISelectionRangeProvider.ProvideSelectionRanges(TextModel, IReadOnlyList<Position>) : IReadOnlyList<IReadOnlyList<Range>>` is synchronous and `SmartSelectRanges.Provide(...)` calls the providers in the same order as Monaco (`[bracket, word]` when no external provider exists: `providers.unshift(new Bracket…)` then `concat(new Word…)`, so the order is **Bracket, Word**). The interface exists so that a later phase can add a registry; it is `internal`. `SelectionRange` (`{range}`) is reduced to `Range`.
- The bracket provider drops its `setTimeout` yielding: the loop runs to completion **but** stops after 60 ms of wall-clock (`2 rounds × _maxDuration 30 ms`, a `Stopwatch` started at the beginning of each position), keeping what it found; document it in the XML doc and README. Use `model.BracketPairs` (find the C# names of `findNextBracket`, `findPrevBracket`, `findMatchingBracketUp`: `grep -rn "FindNextBracket\|FindPrevBracket" src/LoomEditor.Core`).
- Options: `SmartSelectOptions(bool SelectLeadingAndTrailingWhitespace = true, bool SelectSubwords = true)` on the feature (Monaco: `smartSelect.selectLeadingAndTrailingWhitespace`, `smartSelect.selectSubwords`); no control property in this card.
- State: the feature keeps `_state` per editor and the cursor-position listener exactly as Monaco; `_ignoreSelection` guards its own `SetSelections`. Selections are set with the same reason as `cursorUndo`'s application (an explicit API call, not a mouse), through `EditorContext` / `ViewModel.SetSelections`; reveal the primary cursor afterwards (Monaco does not reveal explicitly; `setSelections` reveals: look at what `ViewModel.SetSelections` does in this code base and match the observable behavior in the test).
- Read-only editors work (selection only).

## Tests

- [x] Port: `smartSelect.test.ts`: the tests that use only the built-in providers (word and bracket: `Smart select`-suite names such as "bracket selection", "word selection", "in-word ranges", "whitespace lines", "trivia"); skip, with a comment `// Monaco: <name> — needs a registered provider (later)`, the ones that register a custom `SelectionRangeProvider`
- [x] New: `Expand_Repeatedly_GrowsToWholeDocument`, `Shrink_AfterExpand_ReturnsToPreviousSelections`, `Expand_MultiCursor_EachGrowsIndependently`, `CaretMove_ForgetsState`, `Expand_InsideBrackets_SelectsInnerThenWithBrackets`, `Expand_CamelCaseWord_SelectsSubwordThenWord`, `Expand_SnakeCaseWord_SelectsPartThenWord`, `Expand_WhitespaceOnlyLine_SelectsTheLine`, `ProvideSelectionRanges_EqualRangesCollapsed`, `Keybindings_ResolveToTheTwoCommands`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: the three `smartSelect` files; "Behavior differences": synchronous, no provider registry, 60 ms budget for brackets
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

A `selectionRangeProvider` registry (and an LSP `selectionRange` binding), the `_executeSelectionRangeProvider` command, folding based ranges.

## Suggested commit message

`feat(view): add smart select (expand and shrink selection)`
