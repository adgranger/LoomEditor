# P5-37 — Test editor with a language (`createCodeEditorServices`, the `Editor Controller` languages)

- **Status:** done
- **Depends on:** P5-36
- **Size:** S (test code only)

## Goal

The cursor tests can create a model with a language id, a language configuration and a tokenizer, like Monaco's
`createCodeEditorServices` + `instantiateTextModel`. The setup of Monaco's suite `Editor Controller` (its five test
languages, the auto-closing tokenizer, `usingCursor`, `assertType`) is ported once, so that the cards P5-39 … P5-46
only port tests.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/test/browser/controller/cursor.test.ts` | 1366–1421 | suite `Editor Controller` setup: the ids `surroundingLanguage`, `indentRulesLanguage`, `electricCharLanguage`, `autoClosingLanguage`, `emptyClosingSurroundLanguage` and their configurations |
| same | 1423–1444 | `setupOnEnterLanguage`, `setupIndentRulesLanguage` |
| same | 1446–1466 | `setupAutoClosingLanguage` |
| same | 1468–1590 | `setupAutoClosingLanguageTokenization` (`BaseState`, `StringState`, `BlockCommentState`, the tokenizer) |
| same | 1592–1605 | `setAutoClosingLanguageEnabledSet` |
| same | 1607–1639 | `createTextModel`, `withTestCodeEditor`, `usingCursor` (`ICursorOpts`) |
| same | 1641–1672 | `AutoClosingColumnType`, `extractAutoClosingSpecialColumns`, `assertType` |
| same | 2822–2851 | test `issue #46314: ViewModel is out of sync with Model!` |
| `src/vs/editor/test/browser/testCodeEditor.ts` | `createCodeEditorServices`, `instantiateTextModel` | subset: a language id codec, a tokenization registry, a language configuration registry |

## Files to create or change

- `tests/LoomEditor.View.Tests/LoomEditor.View.Tests.csproj` — change: compile the step B helpers of
  `tests/LoomEditor.Core.Tests/Languages/TestHelpers/**/*.cs` as linked files
  (`<Compile Include="..\LoomEditor.Core.Tests\Languages\TestHelpers\**\*.cs" Link="Languages\TestHelpers\%(RecursiveDir)%(Filename)%(Extension)" />`)
- `tests/LoomEditor.View.Tests/TestHelpers/TestEditorServices.cs` — create
- `tests/LoomEditor.View.Tests/Cursors/TestHelpers/EditorControllerLanguages.cs` — create
- `tests/LoomEditor.View.Tests/Cursors/TestHelpers/AutoClosingLanguageTokenizationSupport.cs` — create
- `tests/LoomEditor.View.Tests/Cursors/EditorControllerTokensTests.cs` — create
- `docs/tasks/phase-5/README.md` — change: add the three helpers to the table of section 5

## Design decisions (already made — do not change)

- **How a test editor gets a language.** Exactly as in Monaco: through its model. `TestEditorServices`
  (`IDisposable`, port of the subset of `createCodeEditorServices`) owns one `LanguageIdCodec`, one
  `TokenizationRegistry` and one `LanguageConfigurationRegistry` (Monaco `TestLanguageConfigurationService`; no separate helper, P5-14).
  It has:
  - `RegisterLanguage(string languageId)` → `LanguageIdCodec.Register` (Monaco `languageService.registerLanguage`);
  - `Add(IDisposable)` (Monaco `disposables.add`), disposed with the services;
  - `CreateTextModel(string text, string? languageId = null, TextModelOptions? options = null)` (Monaco
    `instantiateTextModel`): the internal `TextModel` constructor that takes the tokenization registry, the codec and
    the language configuration registry (step B, ADR 0009 decision 5), no tokenization scheduler; then
    `model.Tokenization.SetLanguageId(languageId)` when `languageId` is not `null`.
  The existing `TestCodeEditor.WithTestCodeEditor(TextModel, …)` is used unchanged: the view model reads the language
  and the registry from the model (P5-38). Editor options (`autoIndent`, `autoClosingBrackets`…) go through
  `TestCodeEditorOptions.CursorOptions`, model options through `TextModelOptions`.
- Linked files, not a project reference: a test project cannot reference another xUnit v3 test project (both are
  executables). The linked helpers keep their namespace `LoomEditor.Core.Tests.Languages.TestHelpers`.
- `EditorControllerLanguages` (`IDisposable`) is the suite fixture: its constructor creates a `TestEditorServices` and
  runs the setup of lines 1379–1416 (each test creates its own instance with `using`, like Monaco's `setup` /
  `teardown`). Members keep Monaco's names: `SurroundingLanguageId`… (constants), `SetupOnEnterLanguage(IndentAction)`,
  `SetupIndentRulesLanguage(string, IndentationRule)`, `SetupAutoClosingLanguageTokenization()`,
  `SetAutoClosingLanguageEnabledSet(string)`, `CreateTextModel(…)`, `WithTestCodeEditor(…)`,
  `UsingCursor(CursorOpts, Action<TestCodeEditor, TextModel, ViewModel>)`, static `ExtractAutoClosingSpecialColumns`
  and `AssertType`. `CursorOpts` is a record (`Text`, `LanguageId`, `ModelOpts`, `EditorOpts`).
- JavaScript regex literals of the configurations become `new Regex(@"…", RegexOptions.CultureInvariant)` (ADR 0009,
  decision 6); the `m` flag adds `RegexOptions.Multiline`, the `g` flag is dropped (a .NET `Regex` keeps no state). Copy
  the patterns character for character.
- `__electricCharacterSupport: { docComment }` is passed through the step B `LanguageConfiguration` member of the same
  name (Monaco's internal option). If step B did not port it, add nothing: `docComment` has no effect in Monaco either
  (`BracketElectricCharacterSupport` ignores it) — say so in a comment.
- `AutoClosingLanguageTokenizationSupport` is a faithful port: `/^[^'"`{}/]+/` and the other tests on `line` become
  ordinal string checks or `Regex` with `\G`-free patterns on the remaining string (keep Monaco's `line = line.substring(length)`
  loop). The metadata is `(encodedLanguageId << MetadataConsts.LanguageIdOffset) | ((uint)type << MetadataConsts.TokenTypeOffset)`.
  Keep Monaco's bug in `BlockCommentState.Equals` (`other is StringState`) with a comment.
- `issue #46314`: two `TestCodeEditor`s on one model; `editor1.onDidChangeCursorPosition` is
  `viewModel.EventDispatcher.OnEvent` filtered on `CursorStateChangedEvent`; `model.applyEdits` is `TextModel.ApplyEdits`.
  The tokenizer returns an empty token array and `NullState.Instance`.

## Tests

- [x] Port: `cursor.test.ts` › `Editor Controller` › `issue #46314: ViewModel is out of sync with Model!` (2822)
- [x] New: `ExtractAutoClosingSpecialColumns_AnnotatedLine_MarksColumns` (`'a|b!c'` → columns 2 and 3)
- [x] New: `AutoClosingLanguageTokenizationSupport_StringAndComment_GivesStandardTokenTypes` (`var a = 'x'; // c` →
      other, string, other, comment)
- [x] New: `CreateTextModel_WithLanguageId_SetsLanguageOfModel`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: tests table `cursor.test.ts` (`Editor Controller` +1)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Any production code. The tests that type in these languages (P5-39 … P5-46).

## Suggested commit message

`test(view): give the test editor a language, a language configuration and a tokenizer`
