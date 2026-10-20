# P5-39 — Enter with on-enter rules and indentation rules (`_enter`)

- **Status:** todo
- **Depends on:** P5-38
- **Size:** S

## Goal

Enter (and `lineInsertBefore` / `lineInsertAfter` / `lineBreakInsert`) follows the language: on-enter rules (indent,
outdent, indent-outdent, append text) and indentation rules (`increaseIndentPattern`, `decreaseIndentPattern`,
`indentNextLinePattern`, `unIndentedLinePattern`). Fill the two cut points of `EnterOperation._enter` and port the 28
Enter tests of `cursor.test.ts`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 582–658 | `EnterOperation._enter`: line 586 (`isCheapToTokenize`), 591–615 (`getEnterAction` branch), 617–656 (`getIndentForEnter` branch) |
| same | 1091–1099 | `shiftIndent`, `unshiftIndent` (already ported in `TypeEditHelpers`; used by the converter) |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | see Tests | 28 tests of the suite `Editor Controller` |

Step B provides `getEnterAction` (`common/languages/enterAction.ts`) and `getIndentForEnter` with `IIndentConverter`
(`common/languages/autoIndent.ts` 27–38 and 304–358). Find them by their Monaco names.

## Files to create or change

- `src/LoomEditor.View/Cursors/TypeEditOperations.Enter.cs` — change
- `src/LoomEditor.View/Cursors/CursorIndentConverter.cs` — create
- `tests/LoomEditor.View.Tests/Cursors/OnEnterTests.cs` — create

## Design decisions (already made — do not change)

- `const bool isCheapToTokenize = true` becomes `model.Tokenization.IsCheapToTokenize(range.StartLineNumber)`.
- `getEnterAction(config.autoIndent, model, range, config.languageConfigurationService)` → the step B function with
  `config.AutoIndent`, the model, the range and `config.LanguageConfigurationService`. Port the four `IndentAction`
  branches exactly, including `IndentOutdent` with `ReplaceCommandWithoutChangingPosition` (keep position) or
  `ReplaceCommandWithOffsetCursorState(range, typeText, -1, increasedIndent.Length - normalIndent.Length, true)`.
- The object literal `{ shiftIndent, unshiftIndent, normalizeIndentation }` passed to `getIndentForEnter` (and later to
  `getIndentActionForType`, P5-45) is `internal sealed class CursorIndentConverter(CursorConfiguration config) :
  IIndentConverter` (step B interface) calling `TypeEditHelpers.ShiftIndent`, `UnshiftIndent` and
  `config.NormalizeIndentation`.
- `range.setEndPosition(l, c)` → `new Range(range.StartLineNumber, range.StartColumn, l, c)`.
  `strings.firstNonWhitespaceIndex` → `StringClassification.FirstNonWhitespaceIndex` (as `ShiftCommand` does).
- `Math.ceil(oldEndViewColumn / config.indentSize)` is a division of positive integers:
  `(oldEndViewColumn + config.IndentSize - 1) / config.IndentSize`.
- Keep `LeadingIndentationBeforeColumn` (Monaco's clamping `substring`) for the fall-through line 657.
- Tests: `model.getValue(EndOfLinePreference.CRLF)` → `GetValue(EndOfLinePreference.CRLF)`; `viewModel.executeCommand(cmd,
  'autoFormat')` (`issue #6862`) → `ViewModel.ExecuteCommand(command, "autoFormat")` with a local `IEditCommand`
  using `builder.TrackSelection` / `helper.GetTrackedSelection`. Typing is `viewModel.Type("\n", "keyboard")`.
  `model.tokenization.forceTokenization(n)` → `model.Tokenization.ForceTokenization(n)`.
- If a test fails only because a typed letter is not auto-indented (`AutoIndentOperation`, P5-45), move it to P5-45
  and say so in the commit body. Never change an expected value.

## Tests

Port from `cursor.test.ts`, suite `Editor Controller` (helpers of P5-37):

- [ ] `Enter auto-indents with insertSpaces setting 1` (3285), `… 2` (3301), `… 3` (3317)
- [ ] `issue #115033: indent and appendText` (3402)
- [ ] `issue #6862: Editor removes auto inserted indentation when formatting on type` (3430)
- [ ] `Enter honors increaseIndentPattern` (3801), `Enter honors unIndentedLinePattern` (3844),
      `Enter honors indentNextLinePattern` (3862), `Enter honors intential indent` (3915)
- [ ] `Enter supports selection 1` (3935), `Enter supports selection 2` (3956)
- [ ] `Enter honors tabSize and insertSpaces 1` (3977), `… 2` (4001), `… 3` (4025)
- [ ] `Enter supports intentional indentation` (4050)
- [ ] `Enter should not adjust cursor position when press enter in the middle of a line 1` (4073), `… 2` (4093), `… 3` (4113)
- [ ] `Enter should adjust cursor position when press enter in the middle of leading whitespaces 1` (4132), `… 2` (4159),
      `… 3` (4186), `… 4` (4210), `… 5` (4243)
- [ ] `issue microsoft/monaco-editor#108 part 1/2: Auto indentation on Enter with selection is half broken` (4265),
      `issue microsoft/monaco-editor#108 part 2/2: Auto indentation on Enter with selection is half broken` (4288)
- [ ] `issue #57197: indent rules regex should be stateless` (4645)
- [ ] `typing in json` (4673)
- [ ] `issue #111128: Multicursor \`Enter\` issue with indentation` (4735)
- [ ] The already ported `onEnter works if there are no indentation rules` and `… 2` (`TypingTests`) still pass.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `cursorTypeEditOperations.ts` (+ `_enter` complete, P5-39); tests table `cursor.test.ts`
      (`Editor Controller` +28); "Cut points" row `TypeEditOperations.Enter.cs` removed
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Smart Tab (P5-40), auto-indent while typing other characters (P5-45), electric characters (P5-46).

## Suggested commit message

`feat(view): follow on-enter and indentation rules on Enter`
