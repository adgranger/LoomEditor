# P5-20 — `LanguageConfigurationReader`: `language-configuration.json` → `LanguageConfiguration`

- **Status:** done
- **Depends on:** P5-19
- **Size:** M

## Goal

LoomEditor reads a VS Code `language-configuration.json` (JSON with comments and trailing commas) into a Core
`LanguageConfiguration`, validating every entry like VS Code and turning its JavaScript regexes into .NET regexes.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/workbench/contrib/codeEditor/common/languageConfigurationExtensionPoint.ts` | 22–87 | `IRegExp`, `IIndentationRules`, `IEnterAction`, `IOnEnterRule`, `ILanguageConfiguration` (shape only), `isStringArr`, `isCharacterPair` |
| same | 133–150 | `_readConfigFile`: only the parse and the "JSON object expected" check |
| same | 152–436 | `_extractValidCommentRule`, `_extractValidBrackets`, `_extractValidAutoClosingPairs`, `_extractValidSurroundingPairs`, `_extractValidColorizedBracketPairs`, `_extractValidOnEnterRules`, `extractValidConfig` |
| same | 443–495 | `_parseRegex`, `_mapIndentationRules` |

## Files to create or change

- `src/LoomEditor.TextMate/Tokenization/LanguageConfigurationReader.cs` — create (ADR 0009, decision 2)
- `src/LoomEditor.TextMate/Tokenization/LanguageConfigurationReader.Regex.cs` — create (partial: `_parseRegex` and the regex translation)
- `tests/LoomEditor.TextMate.Tests/Tokenization/LanguageConfigurationReaderTests.cs` — create

## Design decisions (already made — do not change)

- `internal static LanguageConfiguration Read(string json, string languageId, Action<string>? warn = null)`.
  `warn` gets VS Code's messages (`[languageId]: language configuration: expected …`); the default writes them with
  `Debug.WriteLine` (as P4-34). Tests pass a list.
- Parsing: `JsonNode.Parse(json, null, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })`
  (ADR 0009, decision 5; no serializer, Native AOT). Invalid JSON or a root that is not an object → a warning and an
  empty `LanguageConfiguration` (VS Code's tolerant `jsonc` parser keeps what it could read: a documented behavior
  difference).
- Type checks: `types.isObject(x)` → `x is JsonObject`; `typeof x === 'string'` → a string `JsonValue`;
  `Array.isArray` → `JsonArray`; `typeof removeText === 'number'` → a number `JsonValue`, read as `double` then
  cast to `int`. Truth values: `if (action.appendText)` → a non-empty string; `if (action.removeText)` → a non-zero
  number; `if (onEnterRule.afterText)` → the node is not null. `folding.offSide` is `true` only for JSON `true`;
  `lineComment.noIndent` is the JSON boolean or `null`. A string `lineComment` → `new LineCommentConfig(text)`.
- `extractValidConfig` returns `DocComment = null` (Monaco's `__electricCharacterSupport: undefined`).
  `_handleConfig` (register with priority 50) is done by the caller, P5-21.
- `_parseRegex` (ADR 0009, decision 6): a string is a pattern without flags; an object needs a string `pattern` and
  optional string `flags`. Flags: `i` → `IgnoreCase`, `m` → `Multiline`, `s` → `Singleline`; `g`, `u`, `y`, `d` are
  ignored (`y` and `d` have no .NET equivalent; the code that uses the regexes never relies on them); any other
  letter → a warning and no regex (JavaScript throws). Always `RegexOptions.CultureInvariant`; never
  `RegexOptions.ECMAScript`. A pattern .NET rejects (`ArgumentException`) → the same warning as VS Code
  (`Invalid regular expression in …`) and no regex.
- Translation before compiling (`TranslateJavaScriptPattern`, a small scanner that skips escaped characters and
  knows whether it is inside `[...]`): only the JavaScript-only Unicode property names the bundled files use
  (checked: every other pattern of the ten files compiles unchanged in .NET):
  - `\p{Number}` → `\p{N}`; `\p{Nonspacing_Mark}` → `\p{Mn}`;
  - `\p{Alphabetic}` → `[\p{L}\p{Nl}\p{Mn}\p{Mc}]` outside a class, `\p{L}\p{Nl}\p{Mn}\p{Mc}` inside one (Unicode
    Alphabetic is L + Nl + Other_Alphabetic, which .NET does not have; Other_Alphabetic is mostly Mn / Mc marks: an
    approximation, documented).
  Everything else is left as is. Documented behavior differences: .NET `\w`, `\d` and `\b` are Unicode-aware where
  JavaScript's are ASCII; the `u` flag (code-point matching) has no effect, so astral letters do not match `\p{L}`.
- Pairs: an array must be exactly two strings (`isCharacterPair`); auto-closing and surrounding pairs accept an
  array or an object; each invalid entry is skipped with its warning, as in VS Code.

## Tests

No upstream unit tests. New (inline JSON):

- [x] `Read_AllSections_AreRead` (a TypeScript-like file: comments, brackets, pairs, wordPattern, indentation rules, on-enter rules, folding)
- [x] `Read_CommentsAndTrailingCommas_AreAccepted`
- [x] `Read_PairsAsArraysOrObjects_AreRead`
- [x] `Read_InvalidEntries_AreSkippedWithAWarning`
- [x] `Read_LineCommentObject_KeepsNoIndent`
- [x] `Read_RegexObjectWithFlags_MapsIgnoreCase`
- [x] `Read_UnknownRegexFlag_IsAWarning`
- [x] `Read_InvalidRegex_IsAWarning`
- [x] `Read_IndentationRulesWithoutDecreasePattern_AreDropped`
- [x] `Read_FoldingMarkers_NeedStartAndEnd`
- [x] `Read_OnEnterAction_IndentNamesAndRemoveText`
- [x] `Read_NotAnObject_GivesAnEmptyConfiguration`
- [x] `TranslateJavaScriptPattern_UnicodePropertyNames_InsideAndOutsideClasses`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated (`languageConfigurationExtensionPoint.ts`); "Behavior differences": the regex translation, Unicode `\w` / `\d` / `\b`, the `u` / `y` / `d` flags, strict JSON
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The bundled files and their registration (P5-21); the JSON schema of the extension point (lines 498–886).

## Suggested commit message

`feat(textmate): read VS Code language configuration files`
