# P5-56 — `MoveLinesCommand` (with auto-indent)

- **Status:** done
- **Depends on:** P5-55
- **Size:** M

## Goal

Port Monaco's move lines command (`Alt+↑` / `Alt+↓`). With `editor.autoIndent` at `full`, the moved lines are
re-indented with the language's on-enter and indentation rules, exactly as Monaco does. Port its test file.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/linesOperations/browser/moveLinesCommand.ts` | 20–41 | `class MoveLinesCommand`: fields, constructor |
| same | 43–62 | `createVirtualModel` |
| same | 64–254 | `getEditOperations` |
| same | 256–305 | `buildIndentConverter`, `parseEnterResult` |
| same | 306–362 | `matchEnterRuleMovingDown`, `matchEnterRule` |
| same | 363–408 | `trimStart`, `shouldAutoIndent`, `getIndentEditsOfMovingBlock` |
| same | 410–422 | `computeCursorState` |
| `contrib/linesOperations/test/browser/moveLinesCommand.test.ts` | 22–54 | helpers `testMoveLines…Command`, `testMoveLinesUpOrDown…` |
| same | 56–284 | suite `Editor Contrib - Move Lines Command` (10 tests) |
| same | 286–303, 305–398 | `IndentRulesMode` and suite `Editor contrib - Move Lines Command honors Indentation Rules` (3 tests) |
| same | 400–420, 422–463 | `EnterRulesMode` and suite `Editor - contrib - Move Lines Command honors onEnter Rules` (1 test) |

## Files to create or change

- `src/LoomEditor.View/Features/LinesOperations/MoveLinesCommand.cs` + `MoveLinesCommand.AutoIndent.cs` (lines 256–408) — create
- `tests/LoomEditor.View.Tests/Features/LinesOperations/MoveLinesCommandTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed partial class MoveLinesCommand(Selection selection, bool isMovingDown, EditorAutoIndentStrategy autoIndent, LanguageConfigurationRegistry languageConfigurations) : IEditCommand`.
  `EditorAutoIndentStrategy` exists (`Cursors/EditorCursorEnums.cs`).
- From step B: `AutoIndent.GetGoodIndentForLine(autoIndent, IVirtualModel, languageId, lineNumber, IIndentConverter, registry)`
  (P5-17), `AutoIndent.GetIndentMetadata(model, lineNumber, registry)` → `int?` (P5-18), `IndentConsts` (P5-12),
  `EnterActions.GetEnterAction(autoIndent, model, range, registry)` → `CompleteEnterAction?` (P5-17),
  `CompleteEnterAction`, `IndentAction` (P5-10). No wrappers.
- `createVirtualModel` returns a private nested class implementing P5-16's flat `IVirtualModel`
  (`GetLineTokens`, `GetLanguageId`, `GetLanguageIdAtPosition`, `ForceTokenization`, `GetLineContent`) over the real
  model, a line-number mapper (applied to `GetLineTokens` and `GetLineContent`, as Monaco does) and an optional content
  override (`Func<int, string?>`; `undefined` → `null`). `buildIndentConverter` returns a private nested
  `IIndentConverter`.
- `IndentUtils.GetSpaceCnt` / `GenerateIndent` (P5-52), `ShiftCommand.ShiftIndent` / `UnshiftIndent`,
  `Strings.GetLeadingWhitespace`, `StringClassification.FirstNonWhitespaceIndex` exist.
- `builder.addEditOperation(new Range(1, 1, 1, 1), null)` (the "force run" no-op) is kept as is.
- `_moveEndPositionDown?: boolean` → `bool`; `s.setEndPosition(…)` → `new Selection(…)` built from the fields (phase 3 table).
- Tests: without language, `MoveLinesCommand(sel, down, EditorAutoIndentStrategy.Advanced, registry)` and
  `CommandTester.TestCommand` without language; with a language (`moveLinesIndentMode`, `moveLinesEnterMode`),
  `EditorAutoIndentStrategy.Full` and `TestCommand(…, languageId, prepare)` of P5-40; the factory passes the
  `TestEditorServices`' registry (Monaco passes the test's own `TestLanguageConfigurationService`). `IndentRulesMode` registers
  the four patterns of lines 310–313 (JavaScript regexes → .NET, ADR 0009 decision 6); `EnterRulesMode` registers
  lines 411–416.

## Tests

- [x] Port `Editor Contrib - Move Lines Command` › `move first up / last down disabled`, `move first line down`, `move 2nd line up`, `issue #1322a: move 2nd line up`, `issue #1322b: move last line up`, `issue #1322c: move last line selected up`, `move last line up`, `move 4th line down`, `move multiple lines down`, `invisible selection is ignored`
- [x] Port `Editor contrib - Move Lines Command honors Indentation Rules` › `first line indentation adjust to 0`, `move lines across block`, `move line should still work as before if there is no indentation rules`
- [x] Port `Editor - contrib - Move Lines Command honors onEnter Rules` › `issue #54829. move block across block`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `contrib/linesOperations/browser/moveLinesCommand.ts` (✅, P5-56); tests table
      `moveLinesCommand.test.ts` 14 / 14
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The move actions and keybindings (P5-57).

## Suggested commit message

`feat(view): port MoveLinesCommand`
