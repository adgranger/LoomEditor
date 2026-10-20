# P5-11 — `RichEditBrackets`, `BracketsUtils` and electric characters

- **Status:** done
- **Depends on:** P5-10
- **Size:** M

## Goal

Core has Monaco's bracket regex builders (`RichEditBrackets`, `createBracketOrRegExp`, `BracketsUtils`) and
`BracketElectricCharacterSupport`, which tells when typing a closing bracket should re-indent the line.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/base/common/strings.ts` | 89–91 | `escapeRegExpCharacters` |
| `common/languages/supports/richEditBrackets.ts` | 11–251 | `InternalBracket`, `RichEditBracket`, `groupFuzzyBrackets`, `RichEditBrackets` |
| same | 253–437 | `collectSuperstrings`, `lengthcmp`, `unique`, `getRegexForBracketPair`, `getReversedRegexForBracketPair`, `getRegexForBrackets`, `getReversedRegexForBrackets`, `prepareBracketForRegExp`, `createBracketOrRegExp`, `toReversedString` |
| same | 439–483 | `BracketsUtils` |
| `common/languages/supports/electricCharacter.ts` | 14–78 | `IElectricAction`, `BracketElectricCharacterSupport` |
| `test/common/modes/supports/richEditBrackets.test.ts` | 11–82 | the tests below |
| `test/common/modes/supports/electricCharacter.test.ts` | 15–60 | the tests below |

## Files to create or change

- `src/LoomEditor.Core/Text/Strings.cs` — add `EscapeRegExpCharacters`
- `src/LoomEditor.Core/Languages/Supports/RichEditBrackets.cs` — create (`RichEditBracket`, `RichEditBrackets`, `groupFuzzyBrackets`)
- `src/LoomEditor.Core/Languages/Supports/BracketsUtils.cs` — create (the regex builders as private/internal static methods of `BracketsUtils`, plus `BracketsUtils.CreateBracketOrRegExp`)
- `src/LoomEditor.Core/Languages/Supports/ElectricCharacterSupport.cs` — create (`ElectricAction`, `BracketElectricCharacterSupport`)
- `tests/LoomEditor.Core.Tests/Languages/Supports/RichEditBracketsTests.cs`, `ElectricCharacterTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.Core.Languages.Supports` (folder `Languages/Supports/`).
- `Strings.EscapeRegExpCharacters(string)` escapes exactly `\ { } * + ? | ^ $ . [ ] ( )` with a backslash, like
  JavaScript. Never `Regex.Escape` (it escapes other characters and not `]`, `}`).
- `CreateBracketOrRegExp(IEnumerable<string> pieces)` builds `(p1)|(p2)|…` and returns
  `new Regex(source, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)`: Monaco's `createRegExp` adds the `i`
  flag because `matchCase` is unset. The `options` parameter is dropped (the only option Monaco passes is
  `global`, which a .NET regex does not need). No pieces gives `()`, which matches the empty string, as in Monaco.
- `prepareBracketForRegExp`: the test `/^[\w ]+$/` is ASCII (`[A-Za-z0-9_ ]`, phase-4/README.md trap 4); the
  inserted `\b` stays a .NET `\b`.
- `pieces.sort(lengthcmp); pieces.reverse()` → a **stable** sort by length (`OrderBy`), then `Reverse`.
  `unique` keeps the first occurrence. `toLowerCase` → `ToLowerInvariant`.
- `toReversedString` reverses the UTF-16 code units (like Monaco); its one-entry cache is dropped (no static mutable
  state).
- `RichEditBracket`: `LanguageId`, `Index`, `Open`, `Close` (`IReadOnlyList<string>`), `ForwardRegex`,
  `ReversedRegex`, `IsOpen(string)`, `IsClose(string)`. `RichEditBrackets`: `Brackets`, `ForwardRegex`,
  `ReversedRegex`, `MaxBracketLength`, `TextIsBracket` (`IReadOnlyDictionary<string, RichEditBracket>`),
  `TextIsOpenBracket` (`IReadOnlyDictionary<string, bool>`). Fill both dictionaries with the indexer, in Monaco's
  order (a later write wins, as with object keys). The constructor takes `IReadOnlyList<CharacterPair>`.
- `BracketsUtils.FindPrevBracketInRange(Regex reversedBracketRegex, int lineNumber, string lineText, int startOffset, int endOffset)`
  and `FindNextBracketInRange(...)`, `FindNextBracketInText(...)` return `Range?`. `str.match(regex)` without `g` is
  `regex.Match(text)` (first match).
- `BracketElectricCharacterSupport(RichEditBrackets? richEditBrackets)`: `GetElectricCharacters()` →
  `IReadOnlyList<string>` (distinct, first occurrence order); `OnElectricCharacter(string character, ScopedLineTokens context, int column)`
  → `ElectricAction?` with `readonly record struct ElectricAction(string MatchOpenBracket)`.
  `/^\s*$/.test(textBeforeBracket)` → `string.IsNullOrWhiteSpace` (JavaScript's `\s` and `char.IsWhiteSpace` differ
  only on U+0085 and U+FEFF; say so in a comment). `textIsOpenBracket[x]` that is missing is falsy: `TryGetValue && value`.
- Test regex literals (`/(\{)|(\})/i`) become `new Regex(@"(\{)|(\})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)`.

## Tests

- [x] Port (`richEditBrackets.test.ts`) `richEditBrackets`: `findPrevBracketInToken one char 1`, `findPrevBracketInToken one char 2`,
  `findPrevBracketInToken one char 3`, `findPrevBracketInToken more chars 1`, `findPrevBracketInToken more chars 2`,
  `findPrevBracketInToken more chars 3`, `findNextBracketInToken one char`, `findNextBracketInToken more chars`,
  `findNextBracketInToken with emoty result`, `issue #3894: [Handlebars] Curly braces edit issues`
- [x] Port (`electricCharacter.test.ts`) `Editor Modes - Auto Indentation`: `getElectricCharacters uses all sources and dedups`, `matchOpenBracket`
- [x] New: `EscapeRegExpCharacters_EscapesLikeJavaScript`, `RichEditBrackets_GroupsBracketsWithTheSameOpenOrClose`
  (`['IF','END']`, `['for','end']`, `['{','}']` → two groups, lower-cased), `CreateBracketOrRegExp_WordBrackets_GetWordBoundaries`,
  `CreateBracketOrRegExp_IsCaseInsensitive`, `ForwardRegexForBracketPair_IncludesSuperstrings` (`['begin','end']`, `['if','end if']`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (`richEditBrackets.ts`, `electricCharacter.ts`, `strings.ts` `escapeRegExpCharacters`)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`LanguageBracketsConfiguration` (P5-13), bracket matching in the model (step C).

## Suggested commit message

`feat(core): port the rich edit brackets and the electric character support`
