# P5-28 — Bracket pair tree (7/9): `TextBufferTokenizer` and `FastTokenizer`

- **Status:** todo
- **Depends on:** P5-27
- **Size:** M

## Goal

The bracket pair tree can read a document as a stream of bracket and text tokens, either from the model's syntax
tokens (`TextBufferTokenizer`: brackets only inside "other" tokens that may contain balanced brackets, per embedded
language) or from the raw text of a model without tokens (`FastTokenizer`: one regex over the whole text).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `common/model/bracketPairsTextModelPart/bracketPairsTree/tokenizer.ts` (396 lines).

| Lines | What to port |
|---|---|
| 14–23 | `interface Tokenizer` |
| 53–61 | `interface ITokenizerSource` |
| 63–126 | `TextBufferTokenizer` |
| 128–280 | `NonPeekableTextBufferTokenizer` |
| 282–396 | `FastTokenizer` |
| `test/common/model/bracketPairColorizer/tokenizer.test.ts` 20–216 | suite `Bracket Pair Colorizer - Tokenizer`, helpers `readAllTokens`, `toArr`, `tokenToObj`, `TokenizedDocument`, `TokenInfo` |

## Files to create or change

- `src/LoomEditor.Core/Model/BracketPairs/Tree/ITokenizer.cs` — create (`ITokenizer`, `ITokenizerSource`)
- `src/LoomEditor.Core/Model/BracketPairs/Tree/TextBufferTokenizer.cs` — create (with the private nested `NonPeekableTextBufferTokenizer`)
- `src/LoomEditor.Core/Model/BracketPairs/Tree/FastTokenizer.cs` — create
- `src/LoomEditor.Core/Model/TextModel.BracketPairs.cs` — create: `TextModel` implements `ITokenizerSource` (explicit implementation)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/TestHelpers/TokenizedDocument.cs` — create (`TokenizedDocument`, `TokenInfo`: port of tokenizer.test.ts 131–216; P5-31 reuses it)
- `tests/LoomEditor.Core.Tests/Model/BracketPairs/Tree/TokenizerTests.cs` — create

## Design decisions (already made — do not change)

- Monaco's `Tokenizer` interface is `internal interface ITokenizer` (`Offset`, `Length`, `Read()`, `Peek()` returning
  `Token?`, `Skip(Length)`, `GetText()`).
- `ITokenizerSource` has `GetValue()`, `GetLineCount()`, `GetLineLength(int)` and `IViewLineTokens
  GetLineTokens(int lineNumber)` (Monaco's `tokenization.getLineTokens`, flattened). `TextModel` implements it
  explicitly in `TextModel.BracketPairs.cs` by calling `GetValue()`, `GetLineCount()`, `GetLineLength()` and
  `Tokenization.GetLineTokens()`. The file gets the bracket pairs part in P5-31.
- Token metadata is read with `TokenMetadata.GetTokenType` and `TokenMetadata.ContainsBalancedBrackets` (phase 4).
- **`TextBufferTokenizer`**: port line by line, including the length heuristics (`+= 33` per line, break at 1000 at
  a line end, break at 1500 anywhere) and the "skip identical tokens" loop. `regexp.lastIndex = 0; regexp.exec(text)`
  is `regex.Match(text)`; `match.index` is `Match.Index`, `match[0]` is `Match.Value`. `this.line.substring(a, b)` is
  `line[a..b]` (`substring`, not `substr`). `lineTokens!` stays a non-null assertion on a nullable field.
- **`FastTokenizer`**: the regex is `getRegExpStr() + "|\n"` with the options of P5-27. The `while ((match =
  regexp.exec(text)) !== null)` loop is `for (Match m = regex.Match(text); m.Success; m = regex.Match(text, m.Index +
  m.Length))`: matches are never empty (brackets are non-empty, `\n` is one character). The two caches of 60 small
  text tokens (`smallTextTokens0Line`, `smallTextTokens1Line`) are built per instance as in Monaco. `skip` throws
  `NotSupportedException` (Monaco's `NotSupportedError`). `read()` past the end returns `null`.
- `brackets.getToken(value)!` in `FastTokenizer` is a non-null assertion (the regex only matches known brackets).
- `TokenizedDocument.GetTokenizationSupport()` returns an `ITokenizationSupport` (phase 4) whose state is the line
  number (`State : IState` with `Clone` / `Equals`); `TokenInfo.GetMetadata()` builds the metadata with
  `MetadataConsts` (`LanguageIdOffset`, `TokenTypeOffset`, `BalancedBracketsMask`) as `uint`.
- Threading: single-threaded (UI thread).

## Tests

- [ ] Port suite `Bracket Pair Colorizer - Tokenizer` › `Basic` (`createModelServices` / `instantiateTextModel` →
      a `TokenizationRegistry`, a `LanguageIdCodec`, a `LanguageConfigurationRegistry` (P5-14) and the
      internal `TextModel` constructor that takes them (phase 4 + step B); `toArr` / `tokenToObj` compare text,
      bracket ids through `DenseKeyProvider.ReverseLookup` / `ReverseLookupSet`, and kind)
- [ ] New: `FastTokenizer_SplitsTextAndBrackets` (`"a{b\n}c"` with `{}` → text `0,1`, `{`, text `1,0`, `}`, text
      `0,1`; `Length` of the tokenizer `1,2`)
- [ ] New: `FastTokenizer_NoBrackets_IsOneTextToken`
- [ ] New: `FastTokenizer_Skip_Throws`
- [ ] New: `TextBufferTokenizer_CommentToken_HasNoBrackets` (a `{` inside a comment token is text)
- [ ] New: `TextBufferTokenizer_LongLine_TextTokensAreCutAt1500` (a 4000-character line without brackets gives text
      tokens of at most ~1500 columns)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `tokenizer.ts` ✅ (P5-27, P5-28); tests row `tokenizer.test.ts` 1 / 1
- [ ] Card status and `docs/tasks/README.md` updated
- [ ] `docs/tasks/phase-5/README.md` section 5: row for `TokenizedDocument` (created by P5-28)

## Out of scope

The parser (P5-29).

## Suggested commit message

`feat(core): port the bracket pair tokenizers`
