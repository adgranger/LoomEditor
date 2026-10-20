# P5-16 — Indentation line processor and the test languages

- **Status:** todo
- **Depends on:** P5-15
- **Size:** M

## Goal

Core can evaluate indentation rules on "processed" lines, where the brackets inside strings, comments and regexes
are removed (`IndentationLineProcessor`, `ProcessedIndentRulesSupport`, `IndentationContextProcessor`). The
`IVirtualModel` abstraction of `autoIndent.ts` exists, and the test helpers that register Monaco's test languages
(TypeScript, Ruby, PHP, Go, C++, HTML, VB, LaTeX, Lua) with their rules and fake tokens are ported for steps B, C
and E.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/languages/autoIndent.ts` | 17–25 | `IVirtualModel` |
| `common/languages/supports/indentationLineProcessor.ts` | 22–68 | `ProcessedIndentRulesSupport` |
| same | 77–166 | `IndentationContextProcessor` |
| same | 172–236 | `IndentationLineProcessor`, `isLanguageDifferentFromLineStart` |
| `contrib/indentation/test/browser/indentation.test.ts` | 30–40, 50–141 | `Language`, `registerLanguage`, `registerLanguageConfiguration`, `StandardTokenTypeData`, `registerTokenizationSupport` (test helpers; not the command helpers 42–48) |
| `test/common/modes/supports/bracketRules.ts` | 8–106 | all exports (test helper) |
| `test/common/modes/supports/indentationRules.ts` | 6–50 | all exports (test helper) |
| `test/common/modes/supports/autoClosingPairsRules.ts` | 8–41 | all exports (test helper) |
| `contrib/indentation/test/browser/indentationLineProcessor.test.ts` | 20–256 | the tests below |

## Files to create or change

- `src/LoomEditor.Core/Languages/IVirtualModel.cs` — create (header: `autoIndent.ts`)
- `src/LoomEditor.Core/Model/TextModel.LanguageConfiguration.cs` — `TextModel` implements `IVirtualModel`
- `src/LoomEditor.Core/Languages/Supports/IndentationLineProcessor.cs` — create (the three classes and `IsLanguageDifferentFromLineStart`)
- `tests/LoomEditor.Core.Tests/Languages/TestHelpers/BracketRules.cs`, `IndentationRules.cs`, `AutoClosingPairsRules.cs` — create
- `tests/LoomEditor.Core.Tests/Languages/TestHelpers/TestLanguages.cs` — create (port of `indentation.test.ts` 30–40, 50–141)
- `tests/LoomEditor.Core.Tests/Languages/Supports/IndentationLineProcessorTests.cs` — create

## Design decisions (already made — do not change)

- `IVirtualModel` is flat (Monaco's `tokenization` sub-object is inlined): `IViewLineTokens GetLineTokens(int lineNumber)`,
  `string GetLanguageId()`, `string GetLanguageIdAtPosition(int lineNumber, int column)`,
  `void ForceTokenization(int lineNumber)` (Monaco's optional member: an implementation without it does nothing),
  `string GetLineContent(int lineNumber)`. `TextModel` implements it **explicitly** (except `GetLineContent`, which
  already exists) by delegating to `Tokenization`.
- Monaco calls `getLanguageIdAtPosition(lineNumber, 0)`: a C# `Position` cannot have column 0. Callers pass column
  **1** (`validatePosition` turns 0 into 1 in Monaco, so the result is the same); comment each place.
- `ProcessedIndentRulesSupport(IVirtualModel model, IndentRulesSupport indentRulesSupport, LanguageConfigurationRegistry registry)`:
  `ShouldIncrease`, `ShouldDecrease`, `ShouldIgnore`, `ShouldIndentNextLine` — `(int lineNumber, string? newIndentation = null)`.
- `IndentationContextProcessor(TextModel model, LanguageConfigurationRegistry registry)`:
  `GetProcessedTokenContextAroundRange(Range range)` → `readonly record struct ProcessedTokenContext(IViewLineTokens BeforeRangeProcessedTokens, IViewLineTokens AfterRangeProcessedTokens, IViewLineTokens PreviousLineProcessedTokens)`.
  `IsLanguageDifferentFromLineStart(TextModel model, Position position)` is a public static member of
  `IndentationContextProcessor` (Monaco's module function).
- `IndentationLineProcessor` (internal, Monaco's file-private class): `GetProcessedLine(int lineNumber, string? newIndentation)`,
  `GetProcessedTokens(IViewLineTokens tokens)` → `LineTokens` via `LineTokens.CreateFromTextAndMetadata`.
  `text.replace(bracketsRegExp, '')` with the `g` flag → `bracketsRegExp.Replace(text, string.Empty)` on
  `BracketsNew.GetBracketRegExp()` (case-insensitive, P5-11).
- Test helpers (names follow Monaco):
  - `BracketRules`, `IndentationRules`, `AutoClosingPairsRules`: static classes of `static readonly` fields
    (`TypescriptBracketRules`, `JavascriptIndentationRules`, `JavascriptAutoClosingPairsRules`, …). Regex literals are
    copied verbatim as `new Regex(@"…", RegexOptions.CultureInvariant)` (+ `IgnoreCase` for `/i`). `IndentationRules`
    fields are `IndentationRule` records. `latexAutoClosingPairsRules` is `IReadOnlyList<AutoClosingPair>`; the others
    are `IReadOnlyList<AutoClosingPairConditional>`.
  - `TestLanguages`: string constants `TypeScript = "ts-test"`, `Ruby = "ruby-test"`, … (Monaco's `Language` enum);
    `RegisterLanguage(LanguageIdCodec codec, string language)`;
    `IDisposable RegisterLanguageConfiguration(LanguageConfigurationRegistry registry, string language)` (the
    `switch` of lines 54–112); `readonly record struct StandardTokenTypeData(int StartIndex, StandardTokenType StandardTokenType)`;
    `IDisposable RegisterTokenizationSupport(TokenizationRegistry registry, ILanguageIdCodec codec, StandardTokenTypeData[][] tokens, string languageId)`
    (the line counter `lineIndex++` is kept: each line is tokenized once, in order);
    `TextModel CreateTextModel(string text, string languageId, TokenizationRegistry tokenization, LanguageIdCodec codec, LanguageConfigurationRegistry configurations)`
    (internal constructor without a scheduler, then `Tokenization.SetLanguageId`).
  - Monaco's tests create the model, then register the tokenization support: keep that order (P4-29 resets the
    tokenization when the model's language is registered). `withTestCodeEditor` is not needed: the processor only
    reads the model.
  - Steps D and E use these helpers from `LoomEditor.View.Tests` by linking the files:
    `<Compile Include="..\LoomEditor.Core.Tests\Languages\TestHelpers\*.cs" LinkBase="Languages\TestHelpers" />`
    (added by the first card of those steps that needs them).

## Tests

- [ ] Port (`indentationLineProcessor.test.ts`) `Indentation Context Processor - TypeScript/JavaScript`:
  `brackets inside of string`, `brackets inside of comment`, `brackets inside of regex`
- [ ] Port (same) `Processed Indent Rules Support - TypeScript/JavaScript`: `should increase`, `should decrease`,
  `should increase next line`
- [ ] New: `IsLanguageDifferentFromLineStart_EmbeddedLanguageInTheMiddle_IsTrue`,
  `GetProcessedLine_NewIndentation_ReplacesTheLeadingWhitespace`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (`indentationLineProcessor.ts`, `autoIndent.ts` `IVirtualModel`); tests table: `indentationLineProcessor.test.ts` 6 / 6
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`autoIndent.ts` functions (P5-17, P5-18); `indentation.test.ts` suites (step E).

## Suggested commit message

`feat(core): port the indentation line processor`
