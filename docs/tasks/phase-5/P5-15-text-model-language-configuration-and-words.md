# P5-15 — The model gets the registry: `GetWordAtPosition`, `LanguageConfigurationChanged`

- **Status:** done
- **Depends on:** P5-14
- **Size:** M

## Goal

A `TextModel` is created with its editor's `LanguageConfigurationRegistry`, raises `LanguageConfigurationChanged`
when the configuration of its language changes, and finds the word at a position with the language's
`wordPattern` (Monaco's `getWordAtPosition` / `getWordUntilPosition`). `EditorContext` creates the registry.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/core/wordHelper.ts` | 79–91, 98–175 | `IGetWordAtTextConfig`, the default config, `getWordAtText`, `_findRegexMatchEnclosingPosition` |
| `common/model/tokens/tokenizationTextModelPart.ts` | 127–131 | `handleLanguageConfigurationServiceChange` |
| same | 265–352 | `getWordAtPosition`, `getLanguageConfiguration`, `_findLanguageBoundaries`, `getWordUntilPosition` |
| same | 368–386 | `setLanguageId`: the `onDidChangeLanguageConfiguration` fire (line 385) |
| `common/model/textModel.ts` | 229, 312, 408–411, 2108–2114 | `onDidChangeLanguageConfiguration`, the service parameter, the subscription, `getWordAtPosition` / `getWordUntilPosition` |
| `test/common/model/model.test.ts` | 368–501 | suite `Editor Model - Words` |

## Files to create or change

- `src/LoomEditor.Core/Text/WordHelper.cs` — add `GetWordAtTextConfig`, `GetWordAtText`, `FindRegexMatchEnclosingPosition`
- `src/LoomEditor.Core/Model/TextModel.LanguageConfiguration.cs` — create: `LanguageConfigurationRegistry` property,
  `LanguageConfigurationChanged` event, `GetWordAtPosition`, `GetWordUntilPosition`
- `src/LoomEditor.Core/Model/TextModel.cs`, `TextModel.Tokenization.cs` — the private and internal constructors take
  `LanguageConfigurationRegistry? languageConfigurationRegistry = null` (last parameter); subscribe to its `Changed`
- `src/LoomEditor.Core/Model/Tokens/TokenizationTextModelPart.cs` — add `HandleLanguageConfigurationServiceChange`,
  `GetWordAtPosition`, `GetWordUntilPosition`; raise the event in `SetLanguageId`
- `src/LoomEditor.View/Features/EditorContext.cs` — create one `LanguageConfigurationRegistry` and give it to the model
- `tests/LoomEditor.Core.Tests/Model/ModelWordsTests.cs` — create; `tests/LoomEditor.Core.Tests/Text/WordHelperTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0009, decision 5. `null` registry → `new LanguageConfigurationRegistry()` (the public constructors and
  `FromSnapshot`: plain text with Monaco's `plaintext` defaults). The model exposes it as
  `internal LanguageConfigurationRegistry LanguageConfigurationRegistry { get; }`: this is how the cursor
  configuration (step D) and the other parts of the model (step C) reach it. The tokenization part reads it through
  the model.
- `internal event EventHandler? LanguageConfigurationChanged` (Monaco's `IModelLanguageConfigurationChangedEvent` is
  empty). Raised when the registry's `Changed` affects the model's language, and by `SetLanguageId` right after
  `LanguageChanged` (Monaco's order).
- `internal WordAtPosition? GetWordAtPosition(Position position)` and `internal WordAtPosition GetWordUntilPosition(Position position)`
  on `TextModel`, delegating to the tokenization part. The comparisons with `_position.column` use the position
  **before** validation, like Monaco. `substr(0, n)` → `Substring(0, n)`.
- `GetWordAtText(int column, Regex wordDefinition, string text, int textOffset, GetWordAtTextConfig? config = null)`
  → `WordAtPosition?`:
  - `readonly record struct GetWordAtTextConfig(int MaxLen, int WindowSize, int TimeBudget)` with
    `Default = new(1000, 15, 150)`. `setDefaultGetWordAtTextConfig` is not ported (global mutable state).
  - `maxLen / 2` is integer division (Monaco's only config has an even `maxLen`). `substring(start, end)` clamps `end`
    to the length (trap 6).
  - `wordDefinition.lastIndex = x; exec` → keep `int lastIndex` in a local and call `regex.Match(text, lastIndex)`;
    after a match, `lastIndex = match.Index + match.Length`. After an **empty** match, `lastIndex` moves one more
    (Monaco would loop forever; comment it).
  - The time budget uses `Stopwatch.GetTimestamp()` / `Stopwatch.GetElapsedTime` (never `DateTime`).
- `_findLanguageBoundaries` → a private static method returning `(int StartOffset, int EndOffset)`.
- The `Words` tests: `OuterMode` / `InnerMode` become a small `ITokenizationSupport` in the test file (a
  `TokenizationRegistry`, a `LanguageIdCodec` with both ids, both languages registered with
  `new LanguageConfiguration()` in a `LanguageConfigurationRegistry`). The CSS `wordPattern` of `issue #61296` is
  copied verbatim as a .NET regex.

## Tests

- [x] Port (`model.test.ts`) `Editor Model - Words`: `Get word at position`, `getWordAtPosition at embedded language boundaries`,
  `issue #61296: VS code freezes when editing CSS file with emoji`
- [x] New: `GetWordUntilPosition_CutsTheWordAtTheColumn`, `GetWordUntilPosition_NoWord_ReturnsEmptyWordAtTheColumn`,
  `LanguageConfigurationChanged_RegisterForTheModelLanguage_IsRaised`, `LanguageConfigurationChanged_OtherLanguage_IsNotRaised`,
  `LanguageConfigurationChanged_SetLanguageId_IsRaisedAfterLanguageChanged`, `GetWordAtText_LongLine_SearchesAWindowAroundTheColumn`,
  `GetWordAtText_EmptyMatchPattern_Terminates`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (`wordHelper.ts` complete, `tokenizationTextModelPart.ts` word part); tests table: `model.test.ts` (`Words`) 3 / 3; "Behavior differences": empty-match guard and no `setDefaultGetWordAtTextConfig`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The bracket pair part listening to the registry (step C); the language configurations of the built-in languages
(P5-21).

## Suggested commit message

`feat(core): give the text model its language configuration and word lookup`
