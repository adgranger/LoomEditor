# P5-40 — Smart Tab (`_goodIndentForLine`) and `ShiftCommand` in doc blocks

- **Status:** todo
- **Depends on:** P5-39
- **Size:** S

## Goal

Tab on an empty or whitespace-only line indents straight to the indentation the language expects, and indenting lines
inside a doc block comment (` * …`) keeps the extra space. Fill the cut points of `TabOperation` and `ShiftCommand` and
port their 9 tests.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 839–875 | `TabOperation.getCommands`: line 845 (`/^\s*$/` **then** `isCheapToTokenize`) |
| same | 877–916 | `_goodIndentForLine` |
| `src/vs/editor/common/commands/shiftCommand.ts` | 83–92 | constructor: the `ILanguageConfigurationService` parameter |
| same | 144–176 | enter action of the previous line (`extraSpaces`, `appendText`, `removeText`) |
| `src/vs/editor/common/cursor/cursorTypeOperations.ts` | 18–50 | `indent`, `outdent`: pass `config.languageConfigurationService` to `ShiftCommand` |
| `src/vs/editor/test/browser/testCommand.ts` | 17–37 | `testCommand`: the `languageId` and `prepare` parameters |
| `src/vs/editor/test/browser/commands/shiftCommand.test.ts` | 33–54, 56–82 | `DocBlockCommentMode`, `testShiftCommand` / `testUnshiftCommand` with a language, `prepareDocBlockCommentLanguage` |

Step B provides `getInheritIndentForLine` (`autoIndent.ts` 75–217), `getEnterAction` (`enterAction.ts`), `IndentAction`
and the test helper `OnEnterRules` (`javascriptOnEnterRules`).

## Files to create or change

- `src/LoomEditor.View/Cursors/TypeEditOperations.Tab.cs` — change
- `src/LoomEditor.View/Cursors/Commands/ShiftCommand.cs` — change
- `src/LoomEditor.View/Cursors/TypeOperations.cs` — change: `Indent`, `Outdent` pass the registry
- `tests/LoomEditor.View.Tests/Cursors/TestHelpers/CommandTester.cs` — change: `languageId` and `prepare`
- `tests/LoomEditor.View.Tests/Cursors/Commands/ShiftCommandTests.cs` — change
- `tests/LoomEditor.View.Tests/Cursors/TabTests.cs` — change

## Design decisions (already made — do not change)

- `ShiftCommand(Selection range, ShiftCommandOptions opts, LanguageConfigurationRegistry languageConfigurationService)`:
  the registry is required, as in Monaco. Callers pass `config.LanguageConfigurationService`; the existing
  `ShiftCommandTests` pass the registry of their `TestEditorServices`.
- Lines 144–176: replace the cut-point comment by the port. `model.tokenization.isCheapToTokenize(lineNumber - 1)` →
  `model.Tokenization.IsCheapToTokenize(lineNumber - 1)`. `charCodeAt(j) === CharCode.Space` → `== ' '`. Remove the
  `_ = previousLineExtraSpaces;` placeholder.
- `_goodIndentForLine(config, model, lineNumber)`: replace the stub that returns `null`. `strings.lastNonWhitespaceIndex`
  → `Strings.LastNonWhitespaceIndex`. `if (!indentation) return null` → `string.IsNullOrEmpty(indentation)`.
- In `GetCommands`, test `Strings.IsJavaScriptWhitespaceOnly(lineText)` first and then
  `model.Tokenization.IsCheapToTokenize(range.StartLineNumber)` (Monaco's order; remove the phase 4 comment).
- `CommandTester.TestCommand(lines, selection, commandFactory, expectedLines, expectedSelection, options = null,
  languageId = null, prepare = null)`: `prepare` is `Action<TestEditorServices>?`; the model comes from
  `TestEditorServices.CreateTextModel(text, languageId, options)` (P5-37); `commandFactory` receives the services as
  Monaco's `accessor`: `Func<TestEditorServices, Selection, IEditCommand>` (update the existing callers).
- `issue #38261` runs `CoreEditingCommands.Tab` with `autoIndent: 'advanced'` → `EditorAutoIndentStrategy.Advanced`.

## Tests

- [ ] Port `cursor.test.ts` › `Editor Controller`: `bug #16543: Tab should indent to correct indentation spot immediately` (4351)
- [ ] `bug #2938 (1): When pressing Tab on white-space only lines, indent straight to the right spot (similar to empty lines)` (4377)
- [ ] `bug #2938 (2): When pressing Tab on white-space only lines, indent straight to the right spot (similar to empty lines)` (4403)
- [ ] `bug #2938 (3): When pressing Tab on white-space only lines, indent straight to the right spot (similar to empty lines)` (4428)
- [ ] `bug #2938 (4): When pressing Tab on white-space only lines, indent straight to the right spot (similar to empty lines)` (4453)
- [ ] `bug #31015: When pressing Tab on lines and Enter rules are avail, indent straight to the right spotTab` (4478)
- [ ] `issue #38261: TAB key results in bizarre indentation in C++ mode ` (4588; the name ends with a space)
- [ ] Port `shiftCommand.test.ts` › `Editor Commands - ShiftCommand`: `issue #348: indenting around doc block comments` (567),
      `issue #1609: Wrong indentation of block comments` (635)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map rows `cursorTypeEditOperations.ts` (+ `_goodIndentForLine`), `shiftCommand.ts` (✅); tests
      table `cursor.test.ts` (`Editor Controller` +7), `shiftCommand.test.ts` (25 / 25); "Cut points" rows
      `TypeEditOperations.Tab.cs` and `ShiftCommand.cs` removed
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The `Indent With Tab` suite of `contrib/indentation` (step E).

## Suggested commit message

`feat(view): indent to the expected spot on Tab and in doc blocks`
