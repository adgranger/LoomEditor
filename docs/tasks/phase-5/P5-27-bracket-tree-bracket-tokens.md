# P5-27 — Bracket pair tree (6/9): bracket tokens per language

- **Status:** todo
- **Depends on:** P5-26
- **Size:** S

## Goal

For each language, the bracket pair tree knows its bracket tokens: one `Token` per opening and closing bracket text of
the language configuration (`bracketsNew`), with the ids of the opening brackets it opens or closes, and one regex
that finds any of them in a line.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/model/bracketPairsTextModelPart/bracketPairsTree/tokenizer.ts` | 25–51 | `TokenKind`, `OpeningBracketId`, `Token` |
| `common/model/bracketPairsTextModelPart/bracketPairsTree/brackets.ts` | 13–98 | `BracketTokens` (`createFromLanguage`, `getRegExpStr`, `regExpGlobal`, `getToken`, `findClosingTokenText`, `isEmpty`) |
| same | 100–111 | `prepareBracketForRegExp` |
| same | 113–140 | `LanguageAgnosticBracketTokens` |
| `test/common/model/bracketPairColorizer/brackets.test.ts` | 14–84 | suite `Bracket Pair Colorizer - Brackets`, helper `tokenToObject` |

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/Tree/Token.cs` — create (`TokenKind`, `Token`)
- `src/LoomEditor.Core/Model/BracketPairs/Tree/BracketTokens.cs` — create (`BracketTokens`, `LanguageAgnosticBracketTokens`)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/BracketTokensTests.cs` — create

## Design decisions (already made — do not change)

- `Token` is an `internal sealed class` (Monaco shares token instances between reads): `Length Length`,
  `TokenKind Kind`, `int BracketId` (`-1` for text), `SmallImmutableSet<int> BracketIds`, `AstNode? AstNode`
  (a `BracketAstNode` or `TextAstNode`, Monaco's `astNode`). The `Tokenizer` interface and the tokenizers are P5-28.
- The language configuration comes from step B: `Func<string, ResolvedLanguageConfiguration> getLanguageConfiguration`
  and `configuration.BracketsNew` (`LanguageBracketsConfiguration`: `OpeningBrackets`, `ClosingBrackets`,
  `ClosingBracketKind.GetOpeningBrackets()`). The bracket id key is Monaco's string `$"{languageId}:::{bracketText}"`.
- **The map keeps insertion order** (`findClosingTokenText` returns the first closing text in that order): use
  `OrderedDictionary<string, Token>`. Setting an existing key keeps its position, like a JavaScript `Map`.
- **The regex.** `getRegExpStr`: keys sorted with `string.CompareOrdinal` (JavaScript's default `sort`) then reversed
  (longer brackets with a common prefix come first), each one escaped, joined with `|`. `regExpGlobal` (flags `gi`) is
  built once, lazily, with `RegexOptions.IgnoreCase | RegexOptions.CultureInvariant` (never `ECMAScript`; this regex
  is internal, not a language configuration rule). `getToken(value)` looks up `value.ToLowerInvariant()`.
- **Escaping and `\b`.** `escapeRegExpCharacters` is `Strings.EscapeRegExpCharacters` (P5-11), never `Regex.Escape`. JavaScript's `\w` and `\b` are ASCII (phase-4
  README trap 4): `prepareBracketForRegExp` tests the first / last character with `[A-Za-z0-9_ ]` (Monaco's
  `/^[\w ]+/` and `/[\w ]+$/`) and adds an **ASCII word boundary**, written once as a constant
  `AsciiWordBoundary = @"(?:(?<=[A-Za-z0-9_])(?![A-Za-z0-9_])|(?<![A-Za-z0-9_])(?=[A-Za-z0-9_]))"`, instead of `\b`.
  Comment both differences in the code.
- `LanguageAgnosticBracketTokens(DenseKeyProvider<string>, Func<string, ResolvedLanguageConfiguration>)` caches one
  `BracketTokens` per language in a `Dictionary<string, BracketTokens>`; `DidLanguageChange(languageId)` returns
  whether the language is cached (Monaco's behavior: any configuration change of a used language counts).
- Threading: single-threaded (UI thread), like the tree that owns it.

## Tests

- [ ] Port suite `Bracket Pair Colorizer - Brackets` › `Basic` (with `new LanguageConfigurationRegistry()` and `Register` (P5-14) and
      the Verilog / LaTeX brackets of the test; `bracketIds` compared with `SmallImmutableSet.Equals`, `length` with
      `Length.Of(0, n)`)
- [ ] New: `RegExpGlobal_LongerBracketFirst` (`begin` / `beginning` style: with `["end", "end if"]`, the regex
      matches `end if` in `"end if;"`)
- [ ] New: `RegExpGlobal_WordBrackets_NeedAsciiWordBoundaries` (`begin` matches in `"x begin y"`, not in `"xbegin"`;
      `é` before `begin` counts as a boundary, as in JavaScript)
- [ ] New: `GetToken_IsCaseInsensitive`
- [ ] New: `RegExpGlobal_NoBrackets_IsNull`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `brackets.ts` ✅, `tokenizer.ts` 🔶 (P5-27); tests row `brackets.test.ts` 1 / 1; behavior
      difference "bracket regex: .NET regex with an explicit ASCII word boundary"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`TextBufferTokenizer`, `FastTokenizer` (P5-28).

## Suggested commit message

`feat(core): port the bracket tokens of the bracket pair tree`
