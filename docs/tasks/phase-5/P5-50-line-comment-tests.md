# P5-50 — Line comment tests: empty lines, block fallback, mixed languages

- **Status:** todo
- **Depends on:** P5-49
- **Size:** S (tests only)

## Goal

Port the four remaining suites of `lineCommentCommand.test.ts` (15 tests): `ignoreEmptyLines = false`, the fallback
to a block comment when the language has no line comment, and toggling in a document that mixes two languages
(JSX-like: the language id changes per line through the tokens).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/comment/test/browser/lineCommentCommand.test.ts` | 709–800 | suite `ignoreEmptyLines false` (5 tests) |
| same | 802–913 | suite `Editor Contrib - Line Comment As Block Comment` (4 tests) |
| same | 915–1111 | suite `Editor Contrib - Line Comment As Block Comment 2` (3 tests) |
| same | 1113–1188 | `OuterMode`, `InnerMode`, `testLineCommentCommand` of suite `Editor Contrib - Line Comment in mixed modes` |
| same | 1189–1255 | its 3 tests |

## Files to create or change

- `tests/LoomEditor.View.Tests/Features/Comment/LineCommentCommandTests.cs` — change: the four suites as nested
  classes or `#region`s (keep one file; split into `LineCommentCommandTests.MixedModes.cs` if it passes ~400 lines)
- `tests/LoomEditor.View.Tests/Features/Comment/TestHelpers/LanguagePerLineTokenizationSupport.cs` — create
- `tests/LoomEditor.View.Tests/Cursors/TestHelpers/CommandTester.cs` — change: `forceTokenization` (below)
- `src/LoomEditor.View/Features/Comment/LineCommentCommand*.cs` — change only if a ported test exposes a porting bug

## Design decisions (already made — do not change)

- `ignoreEmptyLines false` builds `new LineCommentCommand(registry, sel, 4, LineCommentType.Toggle, insertSpace: true, ignoreEmptyLines: false)`.
- `Line Comment As Block Comment` registers `{ lineComment: '', blockComment: ['(', ')'] }`
  (`new CommentRule(new LineCommentConfig(""), new CharacterPair("(", ")"))`); `… 2` registers
  `{ lineComment: null, … }` (`LineComment: null`). P5-14's `_handleComments` drops the empty line comment, so both
  reach the block comment fallback, as in Monaco.
- `CommandTester.TestCommand` (P5-40) gets one more optional last parameter, `bool forceTokenization = false`
  (Monaco's `testCommand` has it): when true, `model.Tokenization.ForceTokenization(model.GetLineCount())` runs after
  the model is created. Existing callers do not change.
- Mixed modes: `LanguagePerLineTokenizationSupport` (an `ITokenizationSupport`) returns one token per line whose
  metadata carries the encoded language id of `innerMode` when the line starts with two spaces (`/^  /`) and
  `outerMode` otherwise, with `ColorId.DefaultForeground`, exactly like `OuterMode.tokenizeEncoded` (lines 1133–1148).
  The test's `prepare` registers both languages (`TestEditorServices.RegisterLanguage`), their configurations
  (`outerMode`: `{ lineComment: '//', blockComment: ['/*', '*/'] }`, `innerMode`: `{ lineComment: null, blockComment: ['{/*', '*/}'] }`)
  and the tokenization support for `outerMode` in the services' `TokenizationRegistry`. The test passes
  `forceTokenization: true`.
- Never change an expected value. If a mixed-mode test fails because tokens are not where Monaco has them, stop and
  report it.

## Tests

Port:

- [ ] `ignoreEmptyLines false` › `does not ignore whitespace lines`, `removes its own`, `works in only whitespace`, `comments single line`, `detects indentation`
- [ ] `Editor Contrib - Line Comment As Block Comment` › `fall back to block comment command`, `fall back to block comment command - toggle`, `bug 9513 - expand single line to uncomment auto block`, `bug 9691 - always expand selection to line boundaries`
- [ ] `Editor Contrib - Line Comment As Block Comment 2` › `no selection => uses indentation`, `can remove`, `issue #993: Remove comment does not work consistently in HTML`
- [ ] `Editor Contrib - Line Comment in mixed modes` › `issue #24047 (part 1): Commenting code in JSX files`, `issue #24047 (part 2): Commenting code in JSX files`, `issue #36173: Commenting code in JSX tag body`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: tests table `lineCommentCommand.test.ts` 39 / 39
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The actions (P5-51).

## Suggested commit message

`test(view): port the remaining line comment tests`
