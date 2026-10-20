# P5-14 — `LanguageConfigurationRegistry` and `ResolvedLanguageConfiguration`

- **Status:** done
- **Depends on:** P5-13
- **Size:** M

## Goal

An editor can register language configurations by language id (with priorities) and get the resolved
configuration of a language: comments, brackets, word definition, auto-closing and surrounding pairs, on-enter and
indentation rules, folding rules, electric characters and the brackets configuration.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/languages/languageConfigurationRegistry.ts` | 28–55 | `ICommentsConfiguration`, `ILanguageConfigurationService` (shape only), `LanguageConfigurationServiceChangeEvent` |
| same | 59–144 | `LanguageConfigurationService` (cache, `onDidChange`, `register`, `getLanguageConfiguration`), `computeConfig` — without the configuration service |
| same | 178–185 | `getIndentationAtPosition` |
| same | 187–349 | `ComposedLanguageConfiguration`, `combineLanguageConfigurations`, `LanguageConfigurationContribution`, `LanguageConfigurationChangeEvent`, `LanguageConfigurationRegistry` (with the `plaintext` defaults) |
| same | 354–478 | `ResolvedLanguageConfiguration` |
| `common/core/wordHelper.ts` | 37–76 | `createWordRegExp`, `DEFAULT_WORD_REGEXP`, `ensureValidWordDefinition` |
| `test/common/modes/languageConfiguration.test.ts` | 96–105 | `language configurations priorities` |

## Files to create or change

- `src/LoomEditor.Core/Languages/LanguageConfigurationRegistry.cs` — create (Monaco's service and registry in one class)
- `src/LoomEditor.Core/Languages/ResolvedLanguageConfiguration.cs` — create (+ `CommentsConfiguration`)
- `src/LoomEditor.Core/Languages/LanguageConfigurationChangedEventArgs.cs` — create
- `src/LoomEditor.Core/Text/WordHelper.cs` — add `CreateWordRegExp`, `DefaultWordRegExp`, `EnsureValidWordDefinition`
- `tests/LoomEditor.Core.Tests/Languages/LanguageConfigurationRegistryTests.cs`, `ResolvedLanguageConfigurationTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0009, decision 5: `LanguageConfigurationRegistry` is an **instance** (one per editor), not a singleton. It
  merges Monaco's `LanguageConfigurationService` (the cache of resolved configurations and its change event) and
  `LanguageConfigurationRegistry` (the prioritized entries). API:
  - `LanguageConfigurationRegistry()` registers the `plaintext` defaults of Monaco's registry constructor (priority 0).
  - `IDisposable Register(string languageId, LanguageConfiguration configuration, int priority = 0)`; disposing twice
    does nothing.
  - `ResolvedLanguageConfiguration GetLanguageConfiguration(string languageId)` — never `null`; cached per language
    until a `Register` or a dispose for that language. A language without entries gets
    `new ResolvedLanguageConfiguration(languageId, new LanguageConfiguration())` (cached too).
  - `event EventHandler<LanguageConfigurationChangedEventArgs>? Changed` raised after the cache entry is dropped.
    `LanguageConfigurationChangedEventArgs(string LanguageId)` with `bool Affects(string languageId)`.
  - `static string GetIndentationAtPosition(TextModel model, int lineNumber, int column)` (Monaco's module function).
- Not ported (a behavior difference in `docs/PORTING.md`): the configuration service part — `customizedLanguageConfigKeys`,
  `getCustomizedLanguageConfig`, `validateBracketPairs` (settings `editor.language.brackets` and
  `editor.language.colorizedBracketPairs`) — and the language service check of `computeConfig`.
- `combineLanguageConfigurations`: `entry.X || result.X` → `entry.X ?? result.X`, except `AutoCloseBefore`, where an
  empty string is falsy: `string.IsNullOrEmpty(entry.AutoCloseBefore) ? result.AutoCloseBefore : entry.AutoCloseBefore`.
  Entries are sorted with `LanguageConfigurationContribution.cmp` (priority, then registration order): the order is
  total, so `List.Sort` is safe here.
- `ResolvedLanguageConfiguration(string languageId, LanguageConfiguration underlyingConfig)`, members:
  `LanguageId`, `UnderlyingConfig`, `Comments` (`CommentsConfiguration?`), `CharacterPair` (`CharacterPairSupport`),
  `WordDefinition` (`Regex`), `IndentRulesSupport` (`IndentRulesSupport?`), `IndentationRules` (`IndentationRule?`),
  `FoldingRules` (`FoldingRules`, `new FoldingRules()` when missing), `BracketsNew` (`LanguageBracketsConfiguration`),
  `GetWordDefinition()`, `Brackets` (`RichEditBrackets?`, created on first use), `ElectricCharacter`
  (`BracketElectricCharacterSupport`, created on first use), `OnEnter(EditorAutoIndentStrategy, string, string, string)`
  → `EnterAction?`, `GetAutoClosingPairs()` → a new `AutoClosingPairs`, `GetAutoCloseBeforeSet(bool forQuotes)`,
  `GetSurroundingPairs()`.
- **Monaco mutates the configuration.** `new OnEnterSupport(this.underlyingConfig)` runs `opts.brackets = opts.brackets || […]`
  on the configuration object itself, before `characterPair`, `bracketsNew` and `brackets` read it. So a
  configuration with indentation rules or on-enter rules but no brackets ends up with the default brackets (and
  auto-closing pairs, electric characters…). Reproduce it: when the on-enter support is created and
  `Brackets is null`, `UnderlyingConfig = config with { Brackets = OnEnterSupport.DefaultBrackets }` before anything
  else reads it. Comment it in the code.
- `CommentsConfiguration(string? LineCommentToken, bool? LineCommentNoIndent, string? BlockCommentStartToken, string? BlockCommentEndToken)`
  (record). `_handleComments`: `if (commentRule.lineComment)` → skip a `LineComment` whose `Comment` is empty and
  `NoIndent` is null (the string form `''`).
- `WordHelper.CreateWordRegExp(string allowInWords = "")` builds Monaco's source with a backslash before each
  separator, but writes `\d` as `[0-9]` and `\w` as `[a-zA-Z0-9_]` (phase-4/README.md trap 4); keep `\s`.
  `DefaultWordRegExp` is `static readonly`. `EnsureValidWordDefinition(Regex? wordDefinition)` returns the regex or
  `DefaultWordRegExp` (a .NET regex has no `global` flag and no `lastIndex` to reset).
- Threading: UI thread. `ResolvedLanguageConfiguration` is immutable except its two lazy members.
- No `TestLanguageConfigurationService` helper: the C# registry already is the service, tests use
  `new LanguageConfigurationRegistry()`.

## Tests

- [x] Port (`languageConfiguration.test.ts`) `StandardAutoClosingPairConditional`: `language configurations priorities`
- [x] New: `GetLanguageConfiguration_Plaintext_HasTheDefaultBracketsAndSurroundingPairs`,
  `GetLanguageConfiguration_UnknownLanguage_IsEmpty`, `Register_RaisesChanged_AndRecomputes`,
  `DisposeRegistration_RaisesChanged_AndRestoresTheOtherEntry`, `Combine_EmptyAutoCloseBefore_FallsBack`,
  `Resolved_IndentationRulesWithoutBrackets_GetTheDefaultBrackets`, `Resolved_LineCommentObject_KeepsNoIndent`,
  `Resolved_NoWordPattern_UsesTheDefaultWordRegExp`, `GetIndentationAtPosition_CutsAtTheColumn`,
  `DefaultWordRegExp_MatchesNumbersAndWords` (`-1.5e3`, `foo_bar`, not `a.b`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (`languageConfigurationRegistry.ts`, `wordHelper.ts` partly); "Behavior differences": the settings-based bracket customization is not ported
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The model and the editor using it (P5-15), `getWordAtText` (P5-15), reading JSON files (P5-20).

## Suggested commit message

`feat(core): add the language configuration registry`
