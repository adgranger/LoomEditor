# P10-19 — Snippet syntax (1/3): the scanner and the text, placeholder and choice markers

- **Status:** done
- **Depends on:** P10-02
- **Size:** M

## Goal

The tokenizer of the TextMate snippet syntax and the first part of its tree exist in `LoomEditor.Core`: `SnippetScanner`, the base `SnippetMarker`, `TextMarker`, `Placeholder` and `Choice`, with `ToString`, `ToTextmateString`, `Clone` and the offset arithmetic. Nothing parses a string yet (P10-21).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 8–31 | `TokenType` (the 15 values), `Token` |
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 33–128 | `Scanner` (`isDigitCharacter`, `isVariableCharacter`, `next`, `tokenText`, the static `_table`) |
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 130–222 | `Marker` (abstract: `_markerId`, `parent`, `_children`, `appendChild`, `replace`, `children`, `snippet`, `len`, `toString`, `toTextmateString`, `clone`) and `Text` (`escape`, `merge`, `toTextmateString`) |
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 224–283 | `TransformableMarker`, `Placeholder` (`compareByIndex`, `isFinalTabstop`, `index`, `choice`, `toTextmateString`, `clone`) |
| `src/vs/editor/contrib/snippet/browser/snippetParser.ts` | 285–316 | `Choice` (`options`, `appendChild`, `toString`, `toTextmateString`, `clone`) |
| `src/vs/editor/contrib/snippet/test/browser/snippetParser.test.ts` | 13–127 | the `Scanner` test |

## Files to create or change

- `src/LoomEditor.Core/Snippets/SnippetScanner.cs`, `SnippetToken.cs` (`TokenType` + the token record) — create
- `src/LoomEditor.Core/Snippets/SnippetMarker.cs`, `TextMarker.cs`, `Placeholder.cs`, `Choice.cs` — create
- `tests/LoomEditor.Core.Tests/Snippets/SnippetScannerTests.cs`, `SnippetMarkerTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.Core.Snippets`, every type `internal`. Monaco's `Marker` is `SnippetMarker` (there is already `LoomEditor.Core.Markers.Marker`, a diagnostic) and `Text` is `TextMarker`; the other names are unchanged.
- `SnippetMarker`: `abstract class` with `int Id` taken from a `static int` counter (Monaco's `_markerId`; `Interlocked.Increment` is not needed: not thread-safe, one thread), `SnippetMarker? Parent`, `IReadOnlyList<SnippetMarker> Children`, `AppendChild(SnippetMarker)` (merges nothing: that is `TextMarker`'s business in the parser), `Replace(SnippetMarker child, IReadOnlyList<SnippetMarker> others)` and `Snippet` returning the root (`TextmateSnippet`: **P10-20 defines it**; for now `Snippet` is typed `SnippetMarker` and P10-20 narrows it in `TextmateSnippet` — keep a `// P10-20` comment), `Length` (the C# of `len()`), `ToString()` (children concatenated), `ToTextmateString()` (abstract, as Monaco), `Clone()` (abstract). Port `_markerId` and `_children` as in Monaco, including the private `_replace` logic of lines 150–175 (read it: it re-parents the new markers).
- `TokenType` is an `enum` with Monaco's member names in PascalCase (`Dollar`, `Colon`, `Comma`, `CurlyOpen`, `CurlyClose`, `Backslash`, `Forwardslash`, `Pipe`, `Int`, `VariableName`, `Format`, `Plus`, `Dash`, `QuestionMark`, `EOF`: 15 values, in that order) and `SnippetToken` a `readonly record struct (TokenType Type, int Pos, int Len)`. The scanner's static table is a `Dictionary<int, TokenType>` initialized once (a `static readonly` field).
- `TextMarker.Escape` replaces `\`, `$` and `}` exactly as Monaco's regex does, with `string.Replace` calls (no `Regex`).
- `Placeholder.Index` is an `int` (Monaco: `number`, may be `0` for the final tabstop; `isFinalTabstop` is `index === 0`).
- `Choice.Options` is `List<TextMarker>`. `ToString()` of a `Choice` is `Options[0].Value` (line 298; the first option is what is inserted, ADR 0013 decision 10). `AppendChild` on a `Choice` only accepts a `TextMarker` (anything else is ignored), sets its parent and adds it to `Options`.
- `<remarks>Threading: not thread-safe (a snippet is owned by one session).</remarks>` on every class.

## Tests

- [x] Port: `Scanner` (`snippetParser.test.ts` 13–127), asserting every token (the file shows the pattern: scan, assert type and position)
- [x] New: `TextMarker_Escape_EscapesBackslashDollarBrace`, `TextMarker_ToTextmateString_Escapes`, `Placeholder_CompareByIndex_FinalTabstopLast` (Monaco: `0` sorts after the others), `Placeholder_Clone_IsDeepAndEqual`, `Choice_ToString_FirstOption`, `Choice_ToTextmateString_JoinsWithComma`, `Marker_Replace_ReparentsChildren`, `Marker_Snippet_ReturnsRoot`, `Marker_Ids_AreUnique`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `snippetParser.ts` row (scanner, markers; the file is `🔶` until P10-21)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`Transform`, `FormatString`, `Variable`, `TextmateSnippet` (P10-20), parsing (P10-21).

## Suggested commit message

`feat(core): port the snippet scanner and the text, placeholder and choice markers`
