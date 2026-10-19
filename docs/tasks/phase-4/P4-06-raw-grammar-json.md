# P4-06 — Raw grammar types and the JSON grammar reader

- **Status:** done
- **Depends on:** P4-05
- **Size:** M

## Goal

A `.tmLanguage.json` file becomes a `RawGrammar` object graph, read with `System.Text.Json` (AOT-safe, no
reflection). vscode-textmate's test cases are copied into the test project.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/rawGrammar.ts` | 8–76 | `IRawGrammar`, `IRawRepositoryMap`, `IRawRepository`, `IRawRule`, `IRawCaptures`, `IRawCapturesMap` (not `ILocation`/`ILocatable`) |
| textmate: `src/parseRawGrammar.ts` | 10–22 | `parseRawGrammar` (extension → JSON or plist), `parseJSONGrammar` |
| textmate: `src/utils.ts` | 7–35 | `clone` semantics, as `RawGrammar.DeepClone()` / `RawRule.DeepClone()` |
| textmate: `test-cases/first-mate/`, `test-cases/suite1/` | — | copy verbatim to `tests/LoomEditor.TextMate.Tests/TestCases/` |

## Files to create or change

- `src/LoomEditor.TextMate/Parsing/RawGrammar.cs`, `RawRule.cs`, `RawRepository.cs`, `RawCaptures.cs` — create
- `src/LoomEditor.TextMate/Parsing/RawGrammarReader.cs` — create (`ParseRawGrammar(string content, string? filePath)`, `ParseJsonGrammar`)
- `tests/LoomEditor.TextMate.Tests/TestCases/**` — copy; `tests/LoomEditor.TextMate.Tests/LoomEditor.TextMate.Tests.csproj` copies them to the output (`CopyToOutputDirectory`)
- `.gitattributes` — add `tests/LoomEditor.TextMate.Tests/TestCases/** -text`
- `tests/LoomEditor.TextMate.Tests/TestHelpers/TestCaseFiles.cs` — create
- `tests/LoomEditor.TextMate.Tests/Parsing/RawGrammarReaderTests.cs` — create
- `NOTICE.md` — vscode-textmate (MIT) entry, mentioning the test cases

## Design decisions (already made — do not change)

- ADR 0008, decision 3: `JsonNode.Parse` (`System.Text.Json.Nodes`) with `JsonDocumentOptions { CommentHandling = Skip,
  AllowTrailingCommas = true }`; one mapping function `RawGrammarReader.FromNode(JsonNode)` walks the nodes by hand.
  P4-07 builds the same `JsonNode` tree from a plist, so there is **one** mapping for both formats. No `JsonSerializer`,
  no `JsonSerializerContext` (the raw grammar is not a fixed shape: `applyEndPatternLast` is a boolean **or** a number,
  captures are objects keyed by numbers). Update `src/LoomEditor.TextMate/Json/README.md` to say so.
- The raw types are sealed classes with **mutable** properties, because vscode-textmate stores rule ids in them
  (`RawRule.Id`, the `ruleIdSymbol` of `rule.ts` line 15) and `initGrammar` adds `$self` / `$base` to the repository.
  `DeepClone()` copies everything, including `Id`.
- `RawCaptures` keeps its entries **sorted by numeric key** when the key is an integer, then the other keys in document
  order (phase-4/README.md, trap 2). The repository and the injections keep document order.
- Unknown properties are ignored. A property of the wrong JSON type is ignored too (vscode-textmate reads raw objects
  without validation), except a root that is not an object: `FormatException`.
- `applyEndPatternLast`: `true`, or a number other than 0, is `true`.
- `.json` → JSON reader; anything else → the plist reader of P4-07 (until then: `NotSupportedException`).

## Tests

- [x] New: `ParseJsonGrammar_EveryFirstMateFixture_Parses` (theory over `TestCases/first-mate/fixtures/*.json`)
- [x] New: `ParseJsonGrammar_CapturesAreSortedNumerically` (`"10"` after `"2"`)
- [x] New: `ParseJsonGrammar_ApplyEndPatternLastAsNumber_IsTrue`
- [x] New: `ParseJsonGrammar_CommentsAndTrailingCommas_AreAccepted`
- [x] New: `DeepClone_CopiesNestedRepositoriesAndIds`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map updated; "Behavior differences": no `json.ts`, no location metadata
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

plist (P4-07), using the grammar (P4-12 and later).

## Suggested commit message

`feat(textmate): read raw TextMate grammars from JSON`
