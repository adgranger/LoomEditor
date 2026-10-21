# P5-42 — Overtype auto-closed characters, delete auto-closed pairs

- **Status:** done
- **Depends on:** P5-41
- **Size:** M

## Goal

Typing a closing character in front of the same character that the editor auto-closed types over it; Backspace
between an auto-closed pair deletes both characters; Ctrl+Backspace does the same. Port the readers of the tracked
characters: `AutoClosingOvertypeOperation`, `AutoClosingOvertypeWithInterceptorsOperation`, `isAutoClosingOvertype`,
`isAutoClosingPairDelete` and the word-delete variant.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 115–137 | `AutoClosingOvertypeOperation` |
| same | 139–152 | `AutoClosingOvertypeWithInterceptorsOperation` (called by P5-44) |
| same | 1041–1081 | `isAutoClosingOvertype` |
| `src/vs/editor/common/cursor/cursorTypeOperations.ts` | 165–198 | `typeWithInterceptors`: the `autoClosedCharacters` parameter and the `AutoClosingOvertypeOperation` step (177–180) |
| `src/vs/editor/common/cursor/cursor.ts` | 564–587 | `type`: pass `getAutoClosedCharacters()` |
| `src/vs/editor/common/cursor/cursorDeleteOperations.ts` | 75–149 | `isAutoClosingPairDelete` |
| same | 151–164 | `_runAutoClosingPairDelete` |
| same | 166–169 | `deleteLeft`: the `autoClosedCharacters` parameter and the guard |
| `src/vs/editor/browser/coreCommands.ts` | 2063 | `DeleteLeft`: pass `viewModel.getCursorAutoClosedCharacters()` |
| `src/vs/editor/common/cursor/cursorWordOperations.ts` | 52–62, 433–436 | `DeleteWordContext.autoClosingPairs`; `deleteWordLeft` deletes the pair |
| `src/vs/editor/contrib/wordOperations/browser/wordOperations.ts` | 336–360 | `DeleteWordCommand.runEditorCommand`: the auto-closing pairs of the model's language |
| `src/vs/editor/contrib/wordOperations/test/browser/wordOperations.test.ts` | 874–890 | test `deleteWordLeft - issue #91855` |

## Files to create or change

- `src/LoomEditor.View/Cursors/TypeEditOperations.AutoClosing.cs` — change: the two overtype operations and `IsAutoClosingOvertype`
- `src/LoomEditor.View/Cursors/TypeOperations.cs` — change: `TypeWithInterceptors(…, IReadOnlyList<Range> autoClosedCharacters, string ch)`
- `src/LoomEditor.View/Cursors/CursorsController.Type.cs` — change
- `src/LoomEditor.View/Cursors/DeleteOperations.cs` — change
- `src/LoomEditor.View/Commands/CoreEditingCommands.cs` — change: `DeleteLeft`
- `src/LoomEditor.View/Cursors/DeleteWordContext.cs`, `WordOperations.Delete.cs` — change
- `src/LoomEditor.View/Features/WordOperations/DeleteWordCommand.cs` — change
- `tests/LoomEditor.View.Tests/Cursors/AutoClosingOvertypeTests.cs` — create
- `tests/LoomEditor.View.Tests/Features/WordOperations/WordOperationsDeleteTests.cs` — change

## Design decisions (already made — do not change)

- Insert the overtype step in `TypeWithInterceptors` at Monaco's place (after `AutoIndentOperation`, which is still a
  cut-point comment until P5-45). The parameter order is Monaco's (`autoClosedCharacters` before `ch`).
- `config.autoClosingPairs.autoClosingPairsCloseSingleChar.has(ch)` → `ContainsKey(ch)` on the step B map
  (keyed by `string`). `lineText.charAt(position.column - 1)`: JavaScript returns `""` past the end; compare with a
  guarded index (`column - 1 < lineText.Length`). `beforeCharacter` is `'\0'` when `column <= 2` (Monaco `CharCode.Null`).
- `isAutoClosingPairDelete` keeps Monaco's parameter list (strategies, `autoClosingPairsOpen` map, model, selections,
  `autoClosedCharacters`) and is `public static` (word operations call it). The guard of lines 167–169 replaces the
  phase 3 comment.
- `DeleteOperations.DeleteLeft(prevEditOperationType, config, model, selections, autoClosedCharacters)`;
  `CoreEditingCommands.DeleteLeft` passes `viewModel.GetCursorAutoClosedCharacters()`. `Cut` keeps its phase 3 code.
- `DeleteWordContext` gets `AutoClosingPairs AutoClosingPairs` after `AutoClosingQuotes` (Monaco's order).
  `DeleteWordCommand` reads it with
  `config.LanguageConfigurationService.GetLanguageConfiguration(model.Tokenization.GetLanguageId()).GetAutoClosingPairs()`.
  Remove the two word-operation cut points.
- `issue #78975` runs `viewModel.ExecuteEdits("snippet", …)`: the only producer of tracked characters before P5-43.

## Tests

- [x] Port `cursor.test.ts` › `Editor Controller`: `issue #78975 - Parentheses swallowing does not work when parentheses are inserted by autocomplete` (6006)
- [x] Port `wordOperations.test.ts` › `WordOperations`: `deleteWordLeft - issue #91855: Matching (quote, bracket, paren) doesn't get deleted when hitting Ctrl+Backspace` (874)
- [x] New: `Type_CloseCharacterNotAutoClosed_DoesNotOvertype` (default `autoClosingOvertype: auto`)
- [x] New: `Type_AutoClosingOvertypeNever_DoesNotOvertype`
- [x] New: `Type_QuoteAfterBackslash_DoesNotOvertype`
- [x] New: `DeleteLeft_InsideSnippetAutoClosedPair_DeletesBothCharacters`
- [x] New: `DeleteLeft_AutoClosingDeleteNever_DeletesOneCharacter`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows `cursorDeleteOperations.ts` (✅, P5-42), `cursorWordOperations.ts`,
      `cursorTypeEditOperations.ts` (+ overtype); tests table `cursor.test.ts` (`Editor Controller` +1),
      `wordOperations.test.ts` (54 / 56); "Cut points" row of `DeleteOperations.cs`, `DeleteWordContext.cs`,
      `WordOperations.Delete.cs`, `DeleteWordCommand.cs` removed
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Auto-closing when typing an open character (P5-43); the composition path (P5-44).

## Suggested commit message

`feat(view): overtype and delete auto-closed characters`
