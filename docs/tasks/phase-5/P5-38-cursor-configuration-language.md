# P5-38 — `CursorConfiguration` reads the language configuration

- **Status:** todo
- **Depends on:** P5-37
- **Size:** S

## Goal

`CursorConfiguration` knows the language of the model and exposes what Monaco's typing reads from the language
configuration: auto-closing pairs, surrounding pairs, the block comment start token, "auto close before" sets,
electric characters. The view model recreates it when the language or its configuration changes. Nothing uses the new
members yet (P5-39 … P5-46 do).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursorCommon.ts` | 47–49 | `autoCloseAlways`, `autoCloseNever`, `autoCloseBeforeWhitespace` |
| same | 76–84, 110–168 | fields and constructor: `autoClosingPairs`, `surroundingPairs`, `blockCommentStartToken`, `shouldAutoCloseBefore`, `_languageId`, `languageConfigurationService` |
| same | 170–181 | `electricChars` (lazy) |
| same | 190–197 | `onElectricCharacter` |
| same | 203–223 | `_getShouldAutoClose`, `_getLanguageDefinedShouldAutoClose` |
| same | 433–435 | `isQuote` (string overload) |
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 88, 528–538 | the language id and the registry given to `CursorConfiguration`; recreate it on `onDidChangeLanguageConfiguration` and `onDidChangeLanguage` |

## Files to create or change

- `src/LoomEditor.View/Cursors/CursorConfiguration.cs` — change (remove the phase 4–5 cut point remark)
- `src/LoomEditor.View/Cursors/CursorCommon.cs` — change: `IsQuote(string)`
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs`, `ViewModel.cs` — change: pass the language id and the registry;
  recreate the configuration on language and language-configuration changes
- `tests/LoomEditor.View.Tests/Cursors/TestHelpers/CursorTestContext.cs` — change: the new constructor arguments
- `tests/LoomEditor.View.Tests/Cursors/CursorConfigurationTests.cs` — change

## Design decisions (already made — do not change)

- Constructor: `CursorConfiguration(string languageId, TextModelOptions modelOptions, EditorCursorOptions options,
  FontInfo fontInfo, EditorLayoutInfo layoutInfo, LanguageConfigurationRegistry languageConfigurationService)`.
  The view model passes `_model.Tokenization.GetLanguageId()` and the `LanguageConfigurationRegistry` that step B gives
  the `TextModel` (ADR 0009 decision 5; Monaco injects the same instance into the view model). Tests that build a
  configuration without a model pass `"plaintext"` and `new LanguageConfigurationRegistry()`.
- Members (Monaco's names, PascalCase):
  - `LanguageConfigurationService` (`LanguageConfigurationRegistry`, Monaco's public field);
  - `AutoClosingPairs` (step B `AutoClosingPairs`, from `ResolvedLanguageConfiguration.GetAutoClosingPairs()`);
  - `SurroundingPairs`: `IReadOnlyDictionary<string, string>` (ordinal), open → close, from `GetSurroundingPairs()`;
    a later pair with the same open replaces the earlier one, like the JavaScript object;
  - `BlockCommentStartToken`: `string?` (`Comments?.BlockCommentStartToken`);
  - `ShouldAutoCloseBefore`: a record `AutoCloseBeforePredicates(Func<char, bool> Quote, Func<char, bool> Bracket,
    Func<char, bool> Comment)`. Monaco's predicates take a one-character string (`lineAfter.charAt(0)`): `char` is
    enough. The three static lambdas are static readonly fields; `languageDefined` captures
    `GetAutoCloseBeforeSet(forQuotes)` and tests `set.Contains(c)` (Monaco `indexOf(c) !== -1`);
  - `ElectricChars`: `IReadOnlySet<string>` (ordinal), computed on first read from
    `ElectricCharacter?.GetElectricCharacters()` (Monaco's lazy getter; a plain null-checked field, no `Lazy<T>`);
  - `OnElectricCharacter(string character, LineTokens context, int column)` → the step B electric action or `null`:
    `Supports.CreateScopedLineTokens(context, column - 1)` (Monaco `createScopedLineTokens`), then the electric
    character support of the scoped language id.
- The language configuration is resolved in the constructor, as in Monaco (no caching across configurations).
- `ViewModel` subscribes to `TextModel.LanguageChanged` and to the model's language-configuration change event
  (Monaco `model.onDidChangeLanguageConfiguration`, which step B adds to the model). If step B did not add that event,
  this card adds it to `TextModel.Tokenization.cs` as the port of `tokenizationTextModelPart.ts` 127–131
  (`handleLanguageConfigurationServiceChange`: raise it when the registry change affects the model's language) and
  `textModel.ts` 408–411 (subscription). Both handlers call `UpdateCursorConfiguration()`. The view event
  `ViewLanguageConfigurationEvent` and the outgoing events of lines 529 and 532/537 are not ported (nothing listens).
- `IsQuote(string ch)`: `ch is "'" or "\"" or "`"`; keep the `char` overload.

## Tests

- [ ] New: `Constructor_LanguageWithAutoClosingPairs_ExposesThem` (`autoClosingPairs: [{ '(' , ')' }]` →
      `AutoClosingPairs.AutoClosingPairsOpenByEnd` has `(`)
- [ ] New: `Constructor_NoSurroundingPairs_UsesAutoClosingPairs` (step B's `CharacterPairSupport` default)
- [ ] New: `ShouldAutoCloseBefore_LanguageDefined_UsesAutoCloseBeforeSet`, `…_BeforeWhitespace_OnlySpaceAndTab`,
      `…_Always_True`, `…_Never_False`
- [ ] New: `ElectricChars_LanguageWithBrackets_ContainsLastCharacterOfEachClose`
- [ ] New: `BlockCommentStartToken_LanguageWithBlockComment_IsOpenToken`
- [ ] New: `ViewModel_LanguageChanged_RecreatesCursorConfig`, `ViewModel_LanguageConfigurationRegistered_RecreatesCursorConfig`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `cursorCommon.ts` (✅ language configuration part, P5-38); remove `CursorConfiguration.cs`
      from the "Cut points for phases 4–5" row it shares
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Every operation that reads these members (P5-39 … P5-46). `shouldRecreate` (still: always recreate).

## Suggested commit message

`feat(view): give the cursor configuration the language configuration`
