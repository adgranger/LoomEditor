# P5-36 — Double-click inside brackets or quotes selects their content

- **Status:** todo
- **Depends on:** P5-35
- **Size:** S

## Goal

The last phase 3 cut point of the mouse is resolved: a double-click right after an opening bracket or right before a
closing bracket selects the content of the pair, and a double-click right after an opening quote or right before a
closing quote of a string token selects the string content (`doubleClickSelectsBlock`, default on). Elsewhere a
double-click still selects the word.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `browser/view/viewController.ts`.

| Lines | What to port |
|---|---|
| 135–158 | `_trySelectBracketContent` |
| 160–215 | `_trySelectStringContent` |
| 262–276 | the double-click branch of `dispatchMouse` (already ported in phase 3; only check it) |

No upstream test covers these two methods (checked: no `*.test.ts` of the pinned commit mentions
`doubleClickSelectsBlock` or `_trySelect…`).

## Files to create or change

- `src/LoomEditor.View/Controller/ViewController.cs` — change: replace the two `// Phase 4–5 cut point` stubs and update
  the class remarks
- `src/LoomEditor.View/Cursors/EditorCursorOptions.cs` — change: the XML doc of `DoubleClickSelectsBlock` (no more "need phase 4–5")
- `tests/LoomEditor.View.Tests/Controller/ViewControllerTests.cs` — change: new tests

## Design decisions (already made — do not change)

- Both methods stay `private static` and take the `TextModel` (`viewModel.Model`) like Monaco's `(model, pos)`.
- Bracket content: `model.BracketPairs.MatchBracket(new Position(pos.LineNumber, pos.Column - 1))` and `MatchBracket(pos)` (P5-31);
  `pair[0]` / `pair[1]` are the two items of the returned tuple in Monaco's order. `pos.column <=
  model.getLineMaxColumn(...)` is `GetLineMaxColumn`.
- String content: `hasAccurateTokensForLine`, `isCheapToTokenize`, `forceTokenization`, `getLineTokens`,
  `findTokenIndexAtOffset`, `getStandardTokenType`, `getStartOffset`, `getEndOffset` are the phase 4 members of
  `TextModel.Tokenization` and `LineTokens`. `lineContent.charAt(i)` out of range returns `""` in JavaScript: guard the
  index (phase-3 README trap 2; an out-of-range index means "not a quote"). `containsRTL(content)` is
  `StringClassification.ContainsRtl`. The quotes are exactly `"`, `'` and `` ` ``.
- The returned `Selection` is Monaco's (`Selection.fromPositions(openEnd, closeStart)` and
  `new Selection(lineNumber, tokenStart + 2, lineNumber, tokenEnd)`).
- Remove the line "brackets and strings need phase 4–5" from the docs and the cut point row of `ViewController` in
  `docs/PORTING.md` ("Cut points for phases 4–5"): mark it done (P5-36).

## Tests

- [ ] New: `DispatchMouse_DoubleClickAfterOpeningBracket_SelectsPairContent` (`"f(abc)"`, double-click at column 3 →
      selection `(1, 3) → (1, 6)`)
- [ ] New: `DispatchMouse_DoubleClickBeforeClosingBracket_SelectsPairContent`
- [ ] New: `DispatchMouse_DoubleClickInsideWordBetweenBrackets_SelectsTheWord`
- [ ] New: `DispatchMouse_DoubleClickAfterOpeningQuote_SelectsStringContent` (a tokenizer support that returns a string
      token for `"hello"`)
- [ ] New: `DispatchMouse_DoubleClickInStringWithRtlText_SelectsTheWord`
- [ ] New: `DispatchMouse_DoubleClickSelectsBlockOff_SelectsTheWord`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `viewController.ts` row updated; the `ViewController` cut point done (P5-36)
- [ ] Card status and `docs/tasks/README.md` updated
- [ ] Manual check in the Gallery: double-click just inside `(…)` and inside `"…"` of the C# sample

## Out of scope

Smart select (`contrib/smartSelect`).

## Suggested commit message

`feat(view): double-click inside brackets or quotes selects the content`
