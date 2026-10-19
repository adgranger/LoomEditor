# P4-07 — plist grammars (`plist.ts`)

- **Status:** todo
- **Depends on:** P4-06
- **Size:** M

## Goal

`.tmLanguage` / `.plist` grammars (XML property lists) are read into the same `RawGrammar` objects as JSON grammars.
VS Code ships one (the JavaScript regex grammar) and several vscode-textmate test cases use them.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/plist.ts` | 5–496 | `ChCode`, `State`, `parseWithLocation` (without locations), `parsePLIST`, `_parse` |
| textmate: `src/parseRawGrammar.ts` | 24–29 | `parsePLISTGrammar` |

## Files to create or change

- `src/LoomEditor.TextMate/Parsing/PlistParser.cs` (+ `PlistParser.Values.cs` if the file passes ~400 lines) — create
- `src/LoomEditor.TextMate/Parsing/RawGrammarReader.cs` — change: plist branch
- `tests/LoomEditor.TextMate.Tests/Parsing/PlistParserTests.cs` — create

## Design decisions (already made — do not change)

- The parser is a faithful port of the hand-written `_parse` (no `XmlReader`: entity handling and error behavior must
  stay vscode-textmate's).
- It produces a `System.Text.Json.Nodes.JsonNode` tree, exactly like upstream produces a JavaScript object:
  `<dict>` → `JsonObject` (document order), `<array>` → `JsonArray`, `<string>`/`<date>`/`<data>` → string,
  `<integer>`/`<real>` → number, `<true/>`/`<false/>` → boolean. `RawGrammarReader.FromNode` (P4-06) maps it: there is
  one mapping for both formats.
- `<integer>` keys for captures behave like JSON keys (numeric sort, trap 2).

## Tests

- [ ] New: `ParsePlistGrammar_EverySuite1PlistFixture_Parses` (theory over the `.plist` and `.tmLanguage` files of `TestCases/suite1/fixtures`)
- [ ] New: `ParsePlist_EntitiesAndCData_AreDecoded`
- [ ] New: `ParsePlist_SameGrammarAsJson_GivesEqualRawGrammar` (a small grammar written both ways in the test)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`.tmTheme` themes (not supported, ADR 0008 decision 8).

## Suggested commit message

`feat(textmate): read plist grammars`
