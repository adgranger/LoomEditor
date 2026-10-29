# SmartSelect

**Role:** adds `editor.action.smartSelect.expand` (`Shift+Alt+Right`) and `editor.action.smartSelect.shrink` (`Shift+Alt+Left`): each selection grows to the next enclosing range (sub-word, word, bracket pair, whitespace-only line, block lines, the whole document) and goes back.

**Monaco sources:** `src/vs/editor/contrib/smartSelect/browser/smartSelect.ts`, `wordSelections.ts`, `bracketSelections.ts`.

**Files:**
- `SmartSelectFeature.cs` — the state per editor, the two commands, forgets the state when the cursor moves.
- `SmartSelectRanges.cs` — `provideSelectionRanges` (sort, de-duplicate, leading and trailing whitespace ranges).
- `SelectionRanges.cs`, `SmartSelectOptions.cs`, `SmartSelectCommands.cs`, `ISelectionRangeProvider.cs`.
- `WordSelectionRangeProvider.cs`, `BracketSelectionRangeProvider.cs` — the two built-in providers.

**Tests:** `tests/LoomEditor.View.Tests/Features/SmartSelect/`.

**Known limits:** synchronous; no provider registry (and no `_executeSelectionRangeProvider`); the bracket search stops after 60 ms per position and keeps what it found.
