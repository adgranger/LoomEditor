# P5-46 — Electric characters and bracket balance

- **Status:** todo
- **Depends on:** P5-45, P5-31 (`TextModel.BracketPairs`), P5-32 (legacy `FindMatchingBracketUp`)
- **Size:** S

## Goal

Typing a closing bracket alone on its line re-indents the line to match its opening bracket (electric characters), and
typing an open bracket does not auto-close when an unmatched closing bracket already follows. Port
`InterceptorElectricCharOperation` and fill the step C cut point of P5-43. After this card the interceptor chain of
`typeWithInterceptors` is complete.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 480–546 | `InterceptorElectricCharOperation` (`getEdits`, `_isTypeInterceptorElectricChar`, `_typeInterceptorElectricChar`) |
| same | 285–292 | `getAutoClosingPair`: `model.bracketPairs.hasUnmatchedClosingBracketAfter` (replaces the P5-43 cut point) |
| `src/vs/editor/common/cursor/cursorTypeOperations.ts` | 192–195 | `typeWithInterceptors`: the `InterceptorElectricCharOperation` step |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | see Tests | 15 tests of the suite `Editor Controller` |

## Files to create or change

- `src/LoomEditor.View/Cursors/TypeEditOperations.Electric.cs` — create
- `src/LoomEditor.View/Cursors/TypeEditOperations.AutoClosing.cs` (or `…AutoClosingOpen.cs`) — change: the cut point
- `src/LoomEditor.View/Cursors/TypeOperations.cs` — change (remove the last cut-point comment)
- `tests/LoomEditor.View.Tests/Cursors/ElectricCharacterTests.cs` — create
- `tests/LoomEditor.View.Tests/Cursors/AutoClosingTests.cs` — change: `issue #6841`

## Design decisions (already made — do not change)

- `config.electricChars.hasOwnProperty(ch)` → `config.ElectricChars.Contains(ch)` (P5-38).
- `config.onElectricCharacter(…)` is called **without** Monaco's `try` / `catch (onUnexpectedError)`: as for
  `ExecuteEdit` in phase 3, an exception in a port must fail the tests. Say so in a comment.
- `(lineTokens.getLineContent() + ch).lastIndexOf(matchOpenBracket) + 1` →
  `(lineTokens.GetLineContent() + ch).LastIndexOf(matchOpenBracket, StringComparison.Ordinal) + 1`.
- `model.bracketPairs.findMatchingBracketUp(bracket, { lineNumber, column: endColumn }, 500)` → step C's method with
  a `maxDuration` of 500 ms, as Monaco. The returned range's `StartLineNumber` is compared with the cursor line.
- `model.getLineFirstNonWhitespaceColumn(n) || position.column`: `0` means "no content" → use `position.Column`.
- `hasUnmatchedClosingBracketAfter(new Position(lineNumber, beforeColumn), pair.open)` replaces the `false` constant
  of P5-43; the "Cut points" row of P5-43 is removed.
- `issue #6841` calls `model.bracketPairs.getBracketsInRange(model.getFullModelRange()).toArray()` first (it makes
  Monaco build the tree): call step C's `GetBracketsInRange(model.FullModelRange)` and enumerate it.

## Tests

Port from `cursor.test.ts`, suite `Editor Controller`:

- [ ] `ElectricCharacter - does nothing if no electric char` (4767)
- [ ] `ElectricCharacter - indents in order to match bracket` (4781)
- [ ] `ElectricCharacter - unindents in order to match bracket` (4795)
- [ ] `ElectricCharacter - matches with correct bracket` (4809)
- [ ] `ElectricCharacter - does nothing if bracket does not match` (4825)
- [ ] `ElectricCharacter - matches bracket even in line with content` (4841)
- [ ] `ElectricCharacter - is no-op if bracket is lined up` (4855)
- [ ] `ElectricCharacter - is no-op if there is non-whitespace text before` (4869)
- [ ] `ElectricCharacter - is no-op if pairs are all matched before` (4883)
- [ ] `ElectricCharacter - is no-op if matching bracket is on the same line` (4898)
- [ ] `ElectricCharacter - is no-op if the line has other content` (4917)
- [ ] `ElectricCharacter - appends text` (4932)
- [ ] `ElectricCharacter - appends text 2` (4946)
- [ ] `ElectricCharacter - issue #23711: Replacing selected text with )]} fails to delete old text with backwards-dragged selection` (4960)
- [ ] `issue #6841: Auto closing brackets should balance brackets` (5608)
- [ ] Any test moved here by P5-39 or P5-43 because of the step C cut point.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `cursorTypeEditOperations.ts` (✅); tests table `cursor.test.ts` (`Editor Controller`
      +15: 162 / 171, the 9 left are word wrap, phase 6); "Cut points" row `TypeOperations.cs` removed
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Bracket matching highlights and the bracket pair tree itself (step C).

## Suggested commit message

`feat(view): re-indent electric characters and balance auto-closed brackets`
