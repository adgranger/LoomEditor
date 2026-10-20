# Phase 5 — read this before any card

Phase 5 brings the editor features: model decorations, Monaco's language configuration (auto-closing pairs, on-enter
rules, indentation rules, comments, brackets), the bracket pair tree and bracket matching, auto-indentation, toggle
comment, line operations, find / replace with its widget, Find-based multi-cursor, folding (with hidden areas in the
view model) and word highlight. The decisions are in [ADR 0009](../../adr/0009-editor-features.md). This page lists the
conventions that **every** phase 5 card relies on. A card never repeats them. The conventions of
[phase-3/README.md](../phase-3/README.md) (sections 1, 3, 4, 5 and 6) still apply; "cut point" now reads
`// Phase 6 cut point: …` (or the phase the card names) for what phase 5 leaves out on purpose.

## 1. Upstream sources

Everything comes from `microsoft/vscode` at `0c9c0a4295a9222e12a605537738719455ae3056`:
`https://raw.githubusercontent.com/microsoft/vscode/0c9c0a4295a9222e12a605537738719455ae3056/<path>`. Paths in the
cards are relative to `src/vs/editor/` unless they start with `src/` or `extensions/`. Line numbers are hints; the
function or class **name** is authoritative.

## 2. Where things go

The table is in ADR 0009, decision 2. Namespaces follow the folders. Tests mirror `src/`. Everything is `internal`
unless the card says **public**.

## 3. Monaco concepts → LoomEditor (new in phase 5)

| Monaco | LoomEditor |
|---|---|
| `model.deltaDecorations(old, new, ownerId)`, `changeDecorations(accessor => …)` | `TextModel.DeltaDecorations(ownerId, oldIds, newDecorations)`, `TextModel.ChangeDecorations(ownerId, accessor => …)` |
| `editor.createDecorationsCollection()` (`IEditorDecorationsCollection`) | `EditorDecorationsCollection` (View), created from `IEditorContext` |
| `ModelDecorationOptions.register({ description, className, … })` | `ModelDecorationOptions.Register(new ModelDecorationOptionsData { … })` (cached static instances, as in Monaco) |
| a CSS class name on a decoration | a key of `DecorationStyles` (ADR 0009, decision 4) |
| `ILanguageConfigurationService` | `LanguageConfigurationRegistry` (instance per editor; `GetLanguageConfiguration(languageId)` returns a `ResolvedLanguageConfiguration`) |
| `RegExp` in a language configuration | `System.Text.RegularExpressions.Regex` (ADR 0009, decision 6) |
| `RunOnceScheduler`, `Delayer`, `TimeoutTimer`, `setTimeout` in a contrib | `RunOnceScheduler` / `Delayer` of `Features/Scheduling/` over `IEditorContext.Scheduler` (ADR 0009, decision 10) |
| `IContextKey` (`findWidgetVisible`, `foldingEnabled`…) | a `bool` read by a `when` clause predicate of `WhenClauses` (phase 3) |
| `EditorAction` / `registerEditorAction` with `kbOpts` | a command in the feature's `<Name>Commands.cs` + a `KeybindingRule` in `DefaultKeybindings.cs` (VS Code's Windows shortcut) |
| `EditorContributionInstantiation` / `getContribution` | the feature instance (one per editor, created by `EditorFeatureHost`) |
| `editor.getOption(EditorOption.x)` | the option record of the feature or `EditorCursorOptions` (ADR 0009, decision 12) |
| `IViewModel.setHiddenAreas(ranges)` | `ViewModel.SetHiddenAreas(ranges)` (ADR 0009, decision 8) |

## 4. JavaScript traps specific to phase 5 (in addition to phase-3/README.md section 4)

1. **`RegExp.lastIndex` and the `g` flag.** Monaco reuses global regexes and moves `lastIndex` by hand (find,
   `wordHelper`). In C# use `regex.Match(text, startIndex)` and keep the index in a local; never share a `Match`.
2. **Empty regex matches.** Monaco advances by one code point after an empty match (`strings.getNextCodePoint`).
   .NET's `Match.NextMatch` advances by one `char`: port Monaco's loop, never use `NextMatch` for find.
3. **`String.prototype.replace` with a pattern** in replace strings: `ReplacePattern` builds the string itself (no
   `Regex.Replace`), so `$0`, `$&`, `\n`, `\u`, `\U`… behave like Monaco.
4. **Case-insensitive search** in Monaco is JavaScript's `i` flag (simple case folding). Use
   `RegexOptions.IgnoreCase | RegexOptions.CultureInvariant`. Plain text is escaped with `Strings.EscapeRegExpCharacters`
   (P5-11) exactly where Monaco calls `strings.escapeRegExpCharacters`, **never** `Regex.Escape`: it escapes spaces and
   `#` too, and the escaped text shows in the find box.
5. **`Map<number, …>` keyed by line numbers** in folding and bracket code: `Dictionary<int, …>` for lookups only;
   iterate in the order the card says.
6. **Bit-packed arrays.** `FoldingRanges` packs start / end lines and flags in `Uint32Array`s; `length.ts` packs a
   line count and a column count in one `number` (up to 2^26 lines). Use `uint[]` and a `long` (or the card's type),
   never `int` where the value can exceed 2^31.

## 5. Test helpers shared by many cards

| Helper | Created by | Port of |
|---|---|---|
| `tests/LoomEditor.View.Tests/Features/TestHelpers/ManualEditorScheduler.cs` (runs scheduled actions on demand, fake time) | P5-01 | — |
| `tests/LoomEditor.Core.Tests/Languages/TestHelpers/ModesTestUtils.cs` | P5-10 | `test/common/modesTestUtils.ts` |
| `tests/LoomEditor.Core.Tests/Languages/TestHelpers/OnEnterRules.cs` | P5-12 | `test/common/modes/supports/onEnterRules.ts` |
| `tests/LoomEditor.Core.Tests/Languages/TestHelpers/` (`BracketRules`, `IndentationRules`, `AutoClosingPairsRules`, `TestLanguages`) | P5-16 | `test/common/modes/supports/*Rules.ts`, the test languages of `indentation.test.ts` |
| `tests/LoomEditor.Core.Tests/Languages/TestHelpers/TestIndentConverter.cs` | P5-17 | — |
| `tests/LoomEditor.View.Tests/TestHelpers/TestEditorServices.cs` (language id codec, tokenization and language configuration registries, `CreateTextModel(text, languageId)`) | P5-37 | `createCodeEditorServices` (subset) |
| `tests/LoomEditor.View.Tests/Cursors/TestHelpers/EditorControllerLanguages.cs`, `AutoClosingLanguageTokenizationSupport.cs` | P5-37 | the languages of `cursor.test.ts` |
| `CommandTester.TestCommand(…, languageId, prepare)` (existing helper, extended) | P5-40 | `testCommand.ts` |
| `tests/LoomEditor.Core.Tests/Model/BracketPairs/TestHelpers/` (`TokenizedDocument`, `BracketPairsTestModels`, `AnnotatedDocument`, `BracketAssert`; `Tree/TestHelpers/AstAssert`) | P5-24, P5-28, P5-30, P5-31, P5-32 | the helpers of `bracketPairColorizer/*.test.ts` |
| `tests/LoomEditor.Core.Tests/Model/Search/TestHelpers/SearchAssert.cs` | P5-63 | `textModelSearch.test.ts` helpers |
| `tests/LoomEditor.View.Tests/Features/Find/TestHelpers/` (`RegexMatches`, `FindModelTestHelpers`, `TestFindWidget`) | P5-66, P5-68, P5-70 | `findModel.test.ts`, `findController.test.ts` helpers |
| `tests/LoomEditor.View.Tests/Features/MultiCursor/TestHelpers/MultiCursorTestHelpers.cs` | P5-75 | `multicursor.test.ts` helpers |
| `tests/LoomEditor.View.Tests/Layout/TestHelpers/FixedTokensTokenizationSupport.cs` | P5-79 | — |
| `tests/LoomEditor.View.Tests/Features/Folding/TestHelpers/TestDecorationProvider.cs` | P5-85 | `foldingModel.test.ts` helpers |

There is **no** `TestLanguageConfigurationService`: the C# `LanguageConfigurationRegistry` is already an instance, tests
use `new LanguageConfigurationRegistry()` (P5-14). View tests use the Core test helpers as linked files
(`<Compile Include=… LinkBase=…>`, P5-37): both test projects are xUnit v3 executables and cannot reference each other.

The cards that create a helper say so in "Files to create or change"; later cards reuse it. A card that needs a
helper missing from this table creates it in the `TestHelpers/` folder next to its tests and adds a row here.

## 6. Commands and commit

Same as every card (see `CLAUDE.md`). Cards with a WinUI part have a manual check for the maintainer (find widget,
folding controls, the Gallery demo).
