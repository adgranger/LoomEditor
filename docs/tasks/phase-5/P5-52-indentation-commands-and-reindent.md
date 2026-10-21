# P5-52 — Indentation: convert indentation commands and reindent edits

- **Status:** done
- **Depends on:** P5-51
- **Size:** M

## Goal

Port the pure parts of Monaco's indentation contribution: the commands that convert the indentation of the whole
document to spaces or to tabs, `indentUtils.ts`, and `getReindentEditOperations` (re-indents lines with the
language's indentation rules). The `Indentation` feature folder exists with its skeleton.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/indentation/common/indentUtils.ts` | 6–37 | `getSpaceCnt`, `generateIndent` |
| `contrib/indentation/common/indentation.ts` | 16–107 | `getReindentEditOperations` |
| same | 109–115 | `doesLineStartWithString` |
| `contrib/indentation/browser/indentation.ts` | 578–611 | `getIndentationEditOperations` |
| same | 613–643 | `IndentationToSpacesCommand`, `IndentationToTabsCommand` |
| `contrib/indentation/test/browser/indentation.test.ts` | 42–48 | helpers `testIndentationToSpacesCommand`, `testIndentationToTabsCommand` |
| same | 143–230 | suite `Change Indentation to Spaces - TypeScript/Javascript` (4 tests) |
| same | 232–315 | suite `Change Indentation to Tabs -  TypeScript/Javascript` (4 tests; two spaces in the name) |

## Files to create or change

- `src/LoomEditor.View/Features/Indentation/IndentUtils.cs` — create
- `src/LoomEditor.View/Features/Indentation/ReindentOperations.cs` — create (`getReindentEditOperations`, `doesLineStartWithString`)
- `src/LoomEditor.View/Features/Indentation/IndentationToSpacesCommand.cs`, `IndentationToTabsCommand.cs` — create
  (`getIndentationEditOperations` as an `internal static` method of a small `IndentationConversion` class in the first file)
- `src/LoomEditor.View/Features/Indentation/IndentationFeature.cs`, `IndentationCommands.cs`, `README.md` — create (skeleton)
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: add `() => new IndentationFeature()`
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — update
- `tests/LoomEditor.View.Tests/Features/Indentation/IndentationCommandTests.cs`, `ReindentOperationsTests.cs` — create

## Design decisions (already made — do not change)

- Skeleton (required by `ConventionTests`): `IndentationFeature` (public, `Id = "loom.indentation"`, registers
  nothing yet), `IndentationCommands` (public constants, Monaco's ids): `editor.action.indentationToSpaces`,
  `editor.action.indentationToTabs`, `editor.action.indentUsingTabs`, `editor.action.indentUsingSpaces`,
  `editor.action.changeTabDisplaySize`, `editor.action.detectIndentation`, `editor.action.reindentlines`,
  `editor.action.reindentselectedlines`. README: role, sources, "actions in P5-53, paste in P5-54".
- `IndentUtils.GetSpaceCnt(string str, int tabSize)` and `IndentUtils.GenerateIndent(int spacesCnt, int tabSize, bool insertSpaces)`
  are `internal static` (LinesOperations uses them in P5-56). `Math.floor` on a non-negative value → integer division.
- `internal sealed class IndentationToSpacesCommand(Selection selection, int tabSize) : IEditCommand` (and the Tabs one).
  `originalIndentation.replace(/\t/ig, spaces)` → `string.Replace("\t", spaces)`; `replace(new RegExp(spaces, 'gi'), '\t')`
  → `string.Replace(spaces, "\t")` (a run of spaces has no case and no regex metacharacter; same left-to-right,
  non-overlapping result).
- `ReindentOperations.GetReindentEditOperations(TextModel model, LanguageConfigurationRegistry languageConfigurations, int startLineNumber, int endLineNumber)`
  returns `List<IdentifiedEditOperation>` (what `ViewModel.ExecuteEdits` takes). `EditOperation.replaceMove` → an
  operation with `ForceMoveMarkers = true`. It uses step B's `ResolvedLanguageConfiguration.IndentRulesSupport` and
  `new ProcessedIndentRulesSupport(model, indentRulesSupport, languageConfigurations)` (P5-16; `TextModel` is an
  `IVirtualModel`) with `ShouldIgnore`, `ShouldIncrease`, `ShouldIndentNextLine`, `ShouldDecrease`;
  `ShiftCommand.ShiftIndent` / `UnshiftIndent` (exist), `Indentation.NormalizeIndentation` (Core, exists) and
  `Strings.GetLeadingWhitespace` (exists). `count = count || 1` → `count ?? 1` is wrong for `0`: use
  `count is null or 0 ? 1 : count`.
- `doesLineStartWithString`: `model.Tokenization.IsCheapToTokenize`, `GetLineTokens(lineNumber).GetStandardTokenType(0) == StandardTokenType.String`.
- The ported tests use `CommandTester.TestCommand` without a language (Monaco passes `null`).

## Tests

- [x] Port `Change Indentation to Spaces - TypeScript/Javascript` › `single tabs only at start of line`, `multiple tabs at start of line`, `multiple tabs`, `empty lines`
- [x] Port `Change Indentation to Tabs -  TypeScript/Javascript` › `spaces only at start of line`, `multiple spaces at start of line`, `multiple spaces`, `issue #45996`
- [x] New (Monaco has no test for it): `GetReindentEditOperations_JavaScriptRules_IndentsTheBodyOfABlock`
      (P5-16's `TestLanguages.RegisterLanguageConfiguration(registry, TestLanguages.TypeScript)` and
      `TestLanguages.CreateTextModel`, linked into the View tests by P5-37; `"function f() {\nfoo();\n}"` → one edit on
      line 2 to 4 spaces)
- [x] New: `GetReindentEditOperations_NoIndentationRules_ReturnsNoEdit`; `GetReindentEditOperations_EmptyModel_ReturnsNoEdit`
- [x] New: `GetReindentEditOperations_ClosingBraceLine_IsOutdented` (`"{\n    a\n    }"` → line 3 to no indentation)
- [x] New: `IndentUtils_GenerateIndent_NegativeCount_ReturnsEmpty`; `GetSpaceCnt_TabCountsTabSize`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows `contrib/indentation/common/indentUtils.ts`, `contrib/indentation/common/indentation.ts`
      (✅, P5-52), `contrib/indentation/browser/indentation.ts` (🔶 commands only, P5-52); tests table
      `indentation.test.ts` 8 / 85 so far
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The actions (P5-53), `AutoIndentOnPaste` (P5-54), the typing suites of `indentation.test.ts` (P5-61).

## Suggested commit message

`feat(view): port the indentation conversion commands and reindent edits`
