# P10-21 — Snippet syntax (3/3): `SnippetParser`

- **Status:** done
- **Depends on:** P10-20
- **Size:** M

## Goal

`SnippetParser.Parse(template)` turns a snippet string (`for (${1:i} = 0; $1 < ${2:n}; $1++) {\n\t$0\n}`, `${TM_FILENAME/(.*)\..+$/$1/}`, `${1|a,b,c|}`…) into a `TextmateSnippet`, with Monaco's rules for escapes, nested placeholders, variables, transforms, choices and the final tabstop. All the upstream parser tests pass.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 648–743 | `SnippetParser` (`escape`, `guessNeedsClipboard`, `parse`, `ensureFinalTabstop`, `text`, `_accept`, `_backTo`, `_until`) |
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 781–1154 | `_parse`, `_parseEscaped`, `_parseTabstopOrVariableName`, `_parseComplexPlaceholder`, `_parseComplexVariable`, `_parseTransform`, `_parseFormatString`, `_parseAnything` |
| `src/vs/editor/contrib/snippet/test/browser/snippetParser.test.ts` | 128–881 | every test after `Scanner` (about 50): escapes, text, TM text, placeholders, literal code, variables, transforms, choices, `toTextmateString`, nested placeholders, `TextmateSnippet#…` tests, real world snippets |

## Files to create or change

- `src/LoomEditor.Core/Snippets/SnippetParser.cs` (+ `SnippetParser.Placeholders.cs`, `SnippetParser.Transforms.cs` to stay under ~400 lines each) — create
- `tests/LoomEditor.Core.Tests/Snippets/SnippetParserTests.cs` (+ `SnippetParserTests.TextmateSnippet.cs`) — create

## Design decisions (already made — do not change)

- Port the recursive-descent structure **as is**: `_accept(type)` / `_accept(type, value: true)` become `Accept(TokenType)` and `bool TryAcceptText(TokenType, out string)`, `_backTo(token)` returns `false` after restoring the scanner position (store the `SnippetToken` and the scanner offset), `_until(type)` returns `string?`. Keep the order of the alternatives in `_parseAnything` and in `_parse` (it decides ambiguous inputs).
- `SnippetParser.Parse(string value, bool insertFinalTabstop = false, bool enforceFinalTabstop = false)` returns a `TextmateSnippet`; with `insertFinalTabstop` and no `$0` it appends one (`ensureFinalTabstop`), exactly as lines 669–742. `Escape(string)`, `GuessNeedsClipboard(string)` and `Text(string)` are static.
- A variable transform / placeholder transform with an invalid regex pattern: `new Regex` throws `ArgumentException`; catch it in the one place Monaco's `try/catch` is (`_parseTransform`, read it) and `_backTo` the token, so the whole construct is text.
- Placeholder ids and choice text follow Monaco: `${1|one,two|}` → a `Placeholder(1)` with a `Choice`; `\,` `\|` `\\` are escapes inside a choice (tests `31521` and `58494`).
- Errors never throw: malformed input falls back to text (Monaco's behavior; the tests check it).
- The `Scanner` is created per `Parse` call (it has the position state).
- `<remarks>Threading: not thread-safe; create one per call or per thread.</remarks>`

## Tests

- [x] Port: **every** test of `snippetParser.test.ts` after the scanner test, with its title in a `// Monaco:` comment. Expectations that use `assert.deepStrictEqual(snippet.children[…] instanceof Text, …)` become `Assert.IsType<TextMarker>` or `Assert.Equal`. The ones about `TextmateSnippet#offset`, `#replace`, `#enclosingPlaceholders`, `#insertSnippet`-style helpers use the methods of P10-20: if a test uses a Monaco method that LoomEditor did not port (the diff says which), port that method in this card and mention it in the commit body.
- [x] New: `Parse_Empty_ReturnsEmptySnippet`, `Parse_InsertFinalTabstop_AppendsDollarZero`, `Parse_InsertFinalTabstop_ExistingFinalKept`, `Parse_InvalidRegexInTransform_FallsBackToText`, `Parse_UnterminatedPlaceholder_FallsBackToText`, `Escape_Roundtrips`, `GuessNeedsClipboard_True_ForClipboardVariable`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `snippetParser.ts` ✅
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Resolving variables (P10-22), sessions (P10-23, P10-24).

## Suggested commit message

`feat(core): port the snippet parser`
