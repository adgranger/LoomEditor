# P5-10 — Language configuration types and scoped line tokens

- **Status:** done
- **Depends on:** P5-09
- **Size:** M

## Goal

Core has Monaco's language configuration data types (`LanguageConfiguration` and its rules, `IndentAction`,
`EnterAction`, the auto-closing pair classes) and `ScopedLineTokens` (the tokens of one language around an offset),
which every later card of step B uses.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/languages/languageConfiguration.ts` | 13–106 | `LineCommentConfig`, `CommentRule`, `LanguageConfiguration` |
| same | 121–203 | `IndentationRule`, `FoldingMarkers`, `FoldingRules`, `OnEnterRule`, `IDocComment` |
| same | 209–282 | `CharacterPair`, `IAutoClosingPair`, `IAutoClosingPairConditional`, `IndentAction`, `EnterAction`, `CompleteEnterAction` |
| same | 287–419 | `StandardAutoClosingPairConditional`, `AutoClosingPairs`, `appendEntry` |
| `common/languages/supports.ts` | 10–100 | `createScopedLineTokens`, `ScopedLineTokens`, `ignoreBracketsInToken` |
| `test/common/modesTestUtils.ts` | 11–35 | `TokenText`, `createFakeScopedLineTokens` (test helper) |
| `test/common/modes/languageConfiguration.test.ts` | 12–94 | the tests below |

## Files to create or change

- `src/LoomEditor.Core/Languages/LanguageConfiguration.cs` — create (`LanguageConfiguration`, `CommentRule`, `LineCommentConfig`, `IndentationRule`, `FoldingMarkers`, `FoldingRules`, `OnEnterRule`, `DocComment`, `CharacterPair`)
- `src/LoomEditor.Core/Languages/EnterAction.cs` — create (`IndentAction`, `EnterAction`, `CompleteEnterAction`)
- `src/LoomEditor.Core/Languages/AutoClosingPairs.cs` — create (`IAutoClosingPair`, `AutoClosingPair`, `AutoClosingPairConditional`, `StandardAutoClosingPairConditional`, `AutoClosingPairs`)
- `src/LoomEditor.Core/Languages/ScopedLineTokens.cs` — create (header: `supports.ts`)
- `tests/LoomEditor.Core.Tests/Languages/TestHelpers/ModesTestUtils.cs` — create (`TokenText`, `CreateFakeScopedLineTokens`)
- `tests/LoomEditor.Core.Tests/Languages/StandardAutoClosingPairConditionalTests.cs`, `ScopedLineTokensTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.Core.Languages` (ADR 0009, decision 2). Everything internal.
- A JavaScript `RegExp` is a `System.Text.RegularExpressions.Regex` (ADR 0009, decision 6). Nothing in this card
  compiles a regex.
- Shapes (Monaco names, PascalCase):
  - `sealed record LanguageConfiguration` with `init` properties, all nullable, `null` = Monaco's `undefined`:
    `Comments` (`CommentRule?`), `Brackets` (`IReadOnlyList<CharacterPair>?`), `WordPattern` (`Regex?`),
    `IndentationRules`, `OnEnterRules` (`IReadOnlyList<OnEnterRule>?`), `AutoClosingPairs`
    (`IReadOnlyList<AutoClosingPairConditional>?`), `SurroundingPairs` (`IReadOnlyList<AutoClosingPair>?`),
    `ColorizedBracketPairs` (`IReadOnlyList<CharacterPair>?`), `AutoCloseBefore` (`string?`), `Folding`
    (`FoldingRules?`), `DocComment` (`DocComment?`, Monaco's `__electricCharacterSupport.docComment`).
    Monaco's `ExplicitLanguageConfiguration` is the same type.
  - `readonly record struct CharacterPair(string Open, string Close)` (Monaco's tuple `[string, string]`).
  - `sealed record LineCommentConfig(string Comment, bool? NoIndent = null)`; `sealed record CommentRule(LineCommentConfig? LineComment = null, CharacterPair? BlockComment = null)`.
    Monaco's `lineComment: string` is `new LineCommentConfig(text)`.
  - `sealed record IndentationRule(Regex DecreaseIndentPattern, Regex IncreaseIndentPattern, Regex? IndentNextLinePattern = null, Regex? UnIndentedLinePattern = null)`.
  - `sealed record FoldingMarkers(Regex Start, Regex End)`; `sealed record FoldingRules(bool OffSide = false, FoldingMarkers? Markers = null)`.
  - `sealed record OnEnterRule(Regex BeforeText, EnterAction Action, Regex? AfterText = null, Regex? PreviousLineText = null)`.
  - `sealed record DocComment(string Open, string? Close = null)`.
  - `enum IndentAction { None = 0, Indent = 1, IndentOutdent = 2, Outdent = 3 }`.
  - `readonly record struct EnterAction(IndentAction IndentAction, string? AppendText = null, int RemoveText = 0)`
    (Monaco's optional `removeText` is only read as `removeText || 0` or as a truth value).
  - `readonly record struct CompleteEnterAction(IndentAction IndentAction, string AppendText, int RemoveText, string Indentation)`.
  - `interface IAutoClosingPair { string Open { get; } string Close { get; } }`, implemented by
    `sealed record AutoClosingPair(string Open, string Close)`, `sealed record AutoClosingPairConditional(string Open, string Close, IReadOnlyList<string>? NotIn = null)`
    and `StandardAutoClosingPairConditional` (TypeScript's structural typing: surrounding pairs may be either).
- `StandardAutoClosingPairConditional(AutoClosingPairConditional source)`: `IsOK(StandardTokenType)`,
  `ShouldAutoClose(ScopedLineTokens context, int column)`, `FindNeutralCharacter()` → `string?` (the cache fields
  stay instance fields). The `switch` in `IsOK` has no default in TypeScript: add `_ => true`.
- `AutoClosingPairs`: the five maps are `IReadOnlyDictionary<string, List<StandardAutoClosingPairConditional>>`
  keyed by a **string** (`charAt` of an empty `open`/`close` is `""`, a valid key), `StringComparer.Ordinal`. Names:
  `AutoClosingPairsOpenByStart`, `AutoClosingPairsOpenByEnd`, `AutoClosingPairsCloseByStart`,
  `AutoClosingPairsCloseByEnd`, `AutoClosingPairsCloseSingleChar`. Lists keep insertion order.
- `ScopedLineTokens`: Monaco's module functions become static members of the class:
  `ScopedLineTokens.Create(LineTokens context, int offset)` (`createScopedLineTokens`) and
  `ScopedLineTokens.IgnoreBracketsInToken(StandardTokenType)` (`ignoreBracketsInToken`: true for comment, string,
  regex). Instance members: `LanguageIdCodec`, `LanguageId`, `FirstCharOffset`, `GetLineContent()`,
  `GetLineLength()`, `GetActualLineContentBefore(int offset)`, `GetTokenCount()`, `FindTokenIndexAtOffset(int)`,
  `GetStandardTokenType(int)`, `ToIViewLineTokens()` (→ `LineTokens.SliceAndInflate(first, last, 0)`).
  `substring` → `Substring(start, end - start)`.
- There is **no** `getScopedLineTokens` at the pinned commit: `ScopedLineTokens.Create` is what Monaco's cursor
  (`cursorCommon.ts` 191, `cursorTypeEditOperations.ts` 309) uses.
- `CreateFakeScopedLineTokens(IReadOnlyList<TokenText>)` builds a `LineTokens` with `new LanguageIdCodec()` and
  metadata `(uint)type << MetadataConsts.TokenTypeOffset`, like Monaco. `readonly record struct TokenText(string Text, StandardTokenType Type)`.

## Tests

- [x] Port (`languageConfiguration.test.ts`) `StandardAutoClosingPairConditional`: `Missing notIn`, `Empty notIn`,
  `Invalid notIn`, `notIn in strings`, `notIn in comments`, `notIn in regex`, `notIn in strings nor comments`,
  `notIn in strings nor regex`, `notIn in comments nor regex`, `notIn in strings, comments nor regex`
  (`language configurations priorities` is P5-14)
- [x] New: `Create_EmbeddedLanguage_ScopesToTheTokensOfThatLanguage`, `GetActualLineContentBefore_IncludesTextBeforeTheScope`,
  `IgnoreBracketsInToken_OnlyOtherIsFalse`, `AutoClosingPairs_IndexesByFirstAndLastCharacters`,
  `FindNeutralCharacter_SkipsCharactersOfThePair`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (`languageConfiguration.ts`, `supports.ts`)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The supports (P5-11, P5-12), the registry (P5-14), reading JSON (P5-20).

## Suggested commit message

`feat(core): add the language configuration types and scoped line tokens`
