# P5-19 — Indentation guesser, `DetectIndentation`, `NormalizeIndentation`

- **Status:** done
- **Depends on:** P5-18
- **Size:** M

## Goal

A model can guess whether its text is indented with tabs or spaces and with which size (Monaco's
`guessIndentation`), apply the guess to its options (`detectIndentation`), and normalize an indentation string with
its options (`normalizeIndentation`). The 11 deferred `textModel.test.ts` tests pass.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/model/indentationGuesser.ts` | 9–221 | `SpacesDiffResult`, `spacesDiff`, `IGuessedIndentation`, `guessIndentation` |
| `common/model/textModel.ts` | 718–731 | `detectIndentation`, `normalizeIndentation` |
| `test/common/model/textModel.test.ts` | 18–72 | `testGuessIndentation`, `assertGuess` (test helpers) |
| same | 241–777, 1000–1051, 1124–1139 | the tests below |

## Files to create or change

- `src/LoomEditor.Core/Model/IndentationGuesser.cs` — create (ADR 0009, decision 2)
- `src/LoomEditor.Core/Model/TextModel.Options.cs` — add `DetectIndentation`, `NormalizeIndentation`
- `tests/LoomEditor.Core.Tests/Model/TextModelIndentationTests.cs` — create (the guess and normalize tests)
- `tests/LoomEditor.Core.Tests/Model/TextModelOptionsTests.cs` — add `issue #168836`

## Design decisions (already made — do not change)

- `IndentationGuesser.GuessIndentation(ITextBuffer source, int defaultTabSize, bool defaultInsertSpaces)` →
  `readonly record struct GuessedIndentation(bool InsertSpaces, int TabSize)`. `SpacesDiffResult` is a private
  mutable class reused across lines, as in Monaco. `charCodeAt` / `getLineCharCode` → `char`. The float test
  `spacesDiffCount[2] >= spacesDiffCount[4] * 2 / 3` becomes `spacesDiffCount[2] * 3 >= spacesDiffCount[4] * 2`
  (no integer division).
- `internal void DetectIndentation(bool defaultInsertSpaces, int defaultTabSize)`: `UpdateOptions(new TextModelUpdateOptions(TabSize: g.TabSize, IndentSize: g.TabSize, InsertSpaces: g.InsertSpaces))`
  (Monaco's `indentSize: guessedIndentation.tabSize`, so the indent size stops following the tab size).
- `internal string NormalizeIndentation(string str)` → `Indentation.NormalizeIndentation(str, Options.IndentSize, Options.InsertSpaces)`.
- No `detectIndentation` **creation** option: `TextModelOptions` is public and does not change. The test helper
  `testGuessIndentation` creates the model with `new TextModelOptions(tabSize: defaultTabSize, insertSpaces: defaultInsertSpaces)`
  and calls `DetectIndentation(defaultInsertSpaces, defaultTabSize)`; it then reads `Options.InsertSpaces` and
  `Options.TabSize`, which is what Monaco's `resolveOptions` (`textModel.ts` 205–216) produces. `assertGuess` keeps
  Monaco's `undefined` / `[number]` cases as `bool?` and an `int?` plus a `bool tabSizeOnlyWithSpaces` flag.
- `normalizeIndentation 1` uses `new TextModelOptions(tabSize: 4, indentSize: 4, insertSpaces: false)`;
  `normalizeIndentation 2` uses the default options. `issue #168836`: `originalIndentSize === 'tabSize'` is
  `Options.IndentSizeIsTabSize`.
- The editor option `detectIndentation` (ADR 0009, decision 12) that calls `DetectIndentation` when the text is set
  is not part of this card.

## Tests

- [x] Port (`textModel.test.ts`) `Editor Model - TextModel`: `guess indentation 1`,
  `issue #44991: Wrong indentation size auto-detection`, `issue #55818: Broken indentation detection`,
  `issue #70832: Broken indentation detection`, `issue #62143: Broken indentation detection`,
  `issue #84217: Broken indentation detection`, `issue #65668: YAML file indented with 2 spaces`,
  `issue #249040: 4-space indent should win over 2-space when predominant`, `normalizeIndentation 1`,
  `normalizeIndentation 2`, `issue #168836: updating tabSize should also update indentSize when indentSize is set to "tabSize"`
- [x] New: `DetectIndentation_RaisesOptionsChanged`, `GuessIndentation_EmptyText_KeepsTheDefaults`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (`indentationGuesser.ts`, `textModel.ts` options part); tests table: `textModel.test.ts` (`Editor Model - TextModel`) 25 / 26 (only `TextModel does not use events internally` left)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The `detectIndentation` editor option and its wiring in `EditorContext` (step E or I); the indentation commands
(step E).

## Suggested commit message

`feat(core): guess and normalize the indentation of a model`
