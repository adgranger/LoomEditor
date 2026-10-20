# P5-43 — Auto-close when typing an open character (`AutoClosingOpenCharTypeOperation`)

- **Status:** todo
- **Depends on:** P5-42
- **Size:** M (≈ 260 lines of production code, 30 ported tests)

## Goal

Typing `(`, `[`, `{`, a quote or a multi-character opening (`/**`, `begin`) inserts the closing text when the language
and the options allow it: not inside a string or comment the pair excludes, not before a word, not after a word for
quotes, longest pair first, contained pairs handled. Port `AutoClosingOpenCharTypeOperation` and
`TypeWithAutoClosingCommand`, and the 30 auto-closing tests that do not need surround, composition, auto-indent or
bracket matching.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 154–164 | `AutoClosingOpenCharTypeOperation.getEdits` |
| same | 166–183 | `_runAutoClosingOpenCharType` |
| same | 185–207 | `typeAndAutoCloseWithUndoStop` (the `followUp`) |
| same | 209–336 | `getAutoClosingPair` — **line 289 is a cut point**, see below |
| same | 342–366 | `_findContainedAutoClosingPair` |
| same | 368–391 | `_findAutoClosingPairOpen` |
| same | 393–404 | `_isBeforeClosingBrace` |
| same | 957–971 | `TypeWithAutoClosingCommand` |
| `src/vs/editor/common/cursor/cursorTypeOperations.ts` | 182–185 | `typeWithInterceptors`: the `AutoClosingOpenCharTypeOperation` step |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | see Tests | 30 tests of the suite `Editor Controller` |

Step B provides `StandardAutoClosingPairConditional` (`shouldAutoClose`, `findNeutralCharacter`, `isOK`),
`AutoClosingPairs` and `createScopedLineTokens` (`supports.ts`).

## Files to create or change

- `src/LoomEditor.View/Cursors/TypeEditOperations.AutoClosing.cs` — change (split into
  `TypeEditOperations.AutoClosingOpen.cs` if it passes ~400 lines)
- `src/LoomEditor.View/Cursors/TypeOperations.cs` — change
- `tests/LoomEditor.View.Tests/Cursors/AutoClosingTests.cs` — create (partial `AutoClosingTests.Overtype.cs` for the
  `issue #37315` group if the file passes ~400 lines)

## Design decisions (already made — do not change)

- **Cut point, line 289** (`model.bracketPairs.hasUnmatchedClosingBracketAfter`): bracket pairs are step C. Write
  `const bool hasUnmatchedClosingBracketAfter = false; // Phase 5 step C cut point: model.bracketPairs.hasUnmatchedClosingBracketAfter (bracketPairsImpl.ts 121–133).`
  `false` is what Monaco answers when it has no bracket pair tree. P5-46 fills it and ports `issue #6841`.
- `{ open, close }` results are a `readonly record struct AutoClosingPairResult(string Open, string Close)`; Monaco's
  `null` is a nullable value.
- `positions` (`lineNumber`, `beforeColumn`, `afterColumn`) is a private `readonly record struct`.
- `_findAutoClosingPairOpen`: `position.column - candidate.open.length + 1` can be below 1 (a long candidate at the
  start of a line); Monaco's `getValueInRange` clamps it, a C# `Range` throws. Use `Math.Max(1, …)`.
- `lineText.substring(0, beforeColumn - 1)`, `substring(afterColumn - 1)`: columns are valid here; use `Substring`.
  `lineAfter.charAt(0)` is read only when `lineAfter.Length > 0` (as Monaco).
- `getMapForWordSeparators(config.wordSeparators, [])` → `WordCharacterClassifierCache.Get(config.WordSeparators, [])`;
  `wordSeparators.get(c) === WordCharacterClass.Regular` → the classifier's `Get(c) == WordCharacterClass.Regular`.
- `pair.open.includes(config.blockCommentStartToken)` → `Contains(…, StringComparison.Ordinal)`.
- `model.tokenization.isCheapToTokenize`, `forceTokenization`, `getLineTokens`, `getTokenTypeIfInsertingCharacter` →
  the `TokenizationTextModelPart` methods of phase 4.
- `typeAndAutoCloseWithUndoStop` uses `EditOperationResult.FollowUp` (phase 3) and `model.VersionId`.
- `TypeWithAutoClosingCommand : BaseTypeWithAutoClosingCommand` (P5-41). Its `ComputeCursorState` uses
  `helper.GetInverseEditOperations()[0].Range`.
- Tests: `editor.trigger('keyboard', 'type', { text })` → `viewModel.Type(text, "keyboard")`;
  `editor.runCommand(CoreEditingCommands.DeleteLeft, null)` → `CursorTestHelpers.DeleteLeft`;
  `model.onDidChangeContent` → `TextModel.ContentChanged`; `model.onDidChangeDecorations` → step A's
  `DecorationsChanged`; `model.getAllDecorations()` → `model.GetDecorationsInRange(model.FullModelRange)` (all owners);
  `typeCharacters` (local helper of `issue #25658`) stays local.
- A test that fails only because of the step C cut point moves to P5-46 (say so in the commit body). Never change an
  expected value.

## Tests

Port from `cursor.test.ts`, suite `Editor Controller`:

- [ ] `issue #61070: backtick (\`) should auto-close after a word character` (4975)
- [ ] `issue #132912: quotes should not auto-close if they are closing a string` (4985)
- [ ] `autoClosingPairs - open parens: default` (4999), `autoClosingPairs - open parens: whitespace` (5040)
- [ ] `autoClosingPairs - open parens disabled/enabled open quotes enabled/disabled` (5084)
- [ ] `autoClosingPairs - configurable open parens` (5146), `autoClosingPairs - auto-pairing can be disabled` (5191)
- [ ] `autoClosingPairs - quote` (5329), `autoClosingPairs - multi-character autoclose` (5372)
- [ ] `issue #338693: undo after multi-character autoclose first removes only the auto-closed characters` (5392)
- [ ] `issue #338693: multi-character autoclose does not insert an orphan close when a listener reentrantly removes the typed character` (5419)
- [ ] `issue #338693: multi-character autoclose does not use stale selections after a reentrant edit` (5441)
- [ ] `autoClosingPairs - doc comments can be turned off` (5501)
- [ ] `issue #72177: multi-character autoclose with conflicting patterns` (5519)
- [ ] `issue #55314: Do not auto-close when ending with open` (5558)
- [ ] `issue #27937: Trying to add an item to the front of a list is cumbersome` (5597)
- [ ] `issue #25658 - Do not auto-close single/double quotes after word characters` (5711)
- [ ] `issue #37315 - overtypes only those characters that it inserted` (5775)
- [ ] `issue #37315 - stops overtyping once cursor leaves area` (5803)
- [ ] `issue #37315 - it overtypes only once` (5822)
- [ ] `issue #37315 - it can remember multiple auto-closed instances` (5844)
- [ ] `issue #205598 - retains auto-closed actions created by a reentrant decorations listener` (5868)
- [ ] `issue #118270 - auto closing deletes only those characters that it inserted` (5903)
- [ ] `issue #78527 - does not close quote on odd count` (5936)
- [ ] `issue #85983 - editor.autoClosingBrackets: beforeWhitespace is incorrect for Python` (5962)
- [ ] `issue #78833 - Add config to use old brackets/quotes overtyping` (6026)
- [ ] `issue #53357: Over typing ignores characters after backslash` (6093)
- [ ] `issue #84998: Overtyping Brackets doesn't work after backslash` (6117)
- [ ] `issue #20891: All cursors should do the same thing` (6298)
- [ ] `All cursors should do the same thing when deleting left` (6348)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `cursorTypeEditOperations.ts` (+ auto-closing open characters); tests table
      `cursor.test.ts` (`Editor Controller` +30); "Cut points" table: the step C cut point of `getAutoClosingPair`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Surround selection and the end of a composition (P5-44), auto-indent on type (P5-45), bracket balance and electric
characters (P5-46).

## Suggested commit message

`feat(view): auto-close pairs when typing an open character`
