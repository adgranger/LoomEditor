# P5-44 — Surround the selection, and auto-close at the end of a composition

- **Status:** todo
- **Depends on:** P5-43
- **Size:** M

## Goal

Typing an open bracket or a quote over a non-blank selection surrounds it (`autoSurround`). When an IME or a dead key
composition ends with one character, the editor does what typing that character would have done: surround,
overtype an auto-closed character, or auto-close. Port `surroundSelectionCommand.ts`, `SurroundSelectionOperation`,
`shouldSurroundChar` and the language part of `compositionEndWithInterceptors`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/commands/surroundSelectionCommand.ts` | 12–54 | `SurroundSelectionCommand` |
| same | 56–84 | `CompositionSurroundSelectionCommand` |
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 421–478 | `SurroundSelectionOperation` |
| same | 1101–1108 | `shouldSurroundChar` |
| `src/vs/editor/common/cursor/cursorTypeOperations.ts` | 75–163 | `compositionEndWithInterceptors`: the surround check (98–150) and the two auto-closing steps (152–160) |
| same | 187–190 | `typeWithInterceptors`: the `SurroundSelectionOperation` step |
| `src/vs/editor/common/cursor/cursor.ts` | 550–563 | `endComposition`: pass `prevEditOperationType`, the model, the selections and `getAutoClosedCharacters()` |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | see Tests | 11 tests of the suite `Editor Controller` |

## Files to create or change

- `src/LoomEditor.View/Cursors/Commands/SurroundSelectionCommand.cs` — create (both commands)
- `src/LoomEditor.View/Cursors/TypeEditOperations.Surround.cs` — create: `SurroundSelectionOperation`, `ShouldSurroundChar`
- `src/LoomEditor.View/Cursors/TypeOperations.cs` — change: `TypeWithInterceptors`, `CompositionEndWithInterceptors`
  (Monaco's full parameter list)
- `src/LoomEditor.View/Cursors/CursorsController.Composition.cs` — change: `EndComposition`
- `tests/LoomEditor.View.Tests/Cursors/SurroundSelectionTests.cs` — create
- `tests/LoomEditor.View.Tests/Cursors/CompositionTests.cs` — change (the composition tests below)

## Design decisions (already made — do not change)

- `SurroundSelectionCommand.GetEditOperations`: Monaco passes `this._charAfterSelection || null` to
  `addTrackedEditOperation` so that an empty close text still makes an operation (`issue #206774`). Pass `null` when
  the text is empty, exactly; `ComputeCursorState` reads `GetInverseEditOperations()[1]`.
- `config.surroundingPairs.hasOwnProperty(ch)` → `config.SurroundingPairs.ContainsKey(ch)`.
- `/[^ \t]/.test(selectedText)` → "contains a character other than `' '` and `'\t'`" written as a loop;
  `/^[ \t]+$/.test(deletedText)` → "non-empty and only `' '` / `'\t'`". No `Regex`.
- `isQuote(selectionText)` / `isQuote(composition.deletedText)` → `CursorCommon.IsQuote(string)` (P5-38).
- `CompositionEndWithInterceptors(prevEditOperationType, config, model, compositions, selections, autoClosedCharacters)`:
  replace both phase 3 cut points by Monaco's code; the order of the steps is Monaco's
  (surround check, `AutoClosingOvertypeWithInterceptorsOperation`, `AutoClosingOpenCharTypeOperation.GetEdits(…,
  chIsAlreadyTyped: true, isDoingComposition: false)`, `CompositionEndOvertypeOperation`).
- `EditOperationType.Other` with both undo stops for a surround (Monaco 437–440).
- Tests: `viewModel.compositionType(text, replacePrevCharCnt, 0, 0, 'keyboard')` and
  `viewModel.startComposition()` / `endComposition('keyboard')` → the `ViewModel` methods of P3-49.

## Tests

Port from `cursor.test.ts`, suite `Editor Controller`:

- [ ] `Bug #11476: Double bracket surrounding + undo is broken` (1988)
- [ ] `issue #206774: SurroundSelectionCommand with empty charAfterSelection should not throw` (2007)
- [ ] `autoClosingPairs - auto wrapping is configurable` (5238)
- [ ] `issue #41825: Special handling of quotes in surrounding pairs` (6318)
- [ ] `issue #15825: accents on mac US intl keyboard` (6055)
- [ ] `issue #90016: allow accents on mac US intl keyboard to surround selection` (6073)
- [ ] `issue #2773: Accents (´\`¨^, others?) are inserted in the wrong position (Mac)` (6144)
- [ ] `issue #26820: auto close quotes when not used as accents` (6168)
- [ ] `issue #144690: Quotes do not overtype when using US Intl PC keyboard layout` (6233)
- [ ] `issue #144693: Typing a quote using US Intl PC keyboard layout always surrounds words` (6259)
- [ ] `issue #82701: auto close does not execute when IME is canceled via backspace` (6280)
- [ ] New: `Type_OpenBracketOverWhitespaceOnlySelection_DoesNotSurround`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map rows `surroundSelectionCommand.ts` (✅), `cursorTypeOperations.ts` (✅),
      `cursorTypeEditOperations.ts` (+ surround); tests table `cursor.test.ts` (`Editor Controller` +11); "Cut points"
      row `TypeOperations.cs`: only `AutoIndentOperation` (P5-45) and `InterceptorElectricCharOperation` (P5-46) left
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Overtype mode (`InputMode`): not in phase 5. Auto-indent on type (P5-45).

## Suggested commit message

`feat(view): surround selections and auto-close after a composition`
