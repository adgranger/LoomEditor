# P5-48 — `BlockCommentCommand`

- **Status:** todo
- **Depends on:** P5-47
- **Size:** S

## Goal

Port Monaco's block comment command (`Shift+Alt+A` wraps the selection in `/* … */` or removes it) and its test file.
The command reads the block comment tokens of the language at the selection from the language configuration.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/comment/browser/blockCommentCommand.ts` | 15–29 | `class BlockCommentCommand`: fields, constructor |
| same | 31–60 | `_haystackHasNeedleAtOffset` |
| same | 62–121 | `_createOperationsForBlockComment` |
| same | 123–167 | `_createRemoveBlockCommentOperations`, `_createAddBlockCommentOperations` |
| same | 169–206 | `getEditOperations`, `computeCursorState` |
| `contrib/comment/test/browser/blockCommentCommand.test.ts` | 16–31 | helpers `_testCommentCommand`, `testBlockCommentCommand` |
| same | 33–524 | suite `Editor Contrib - Block Comment Command` (12 tests) |

## Files to create or change

- `src/LoomEditor.View/Features/Comment/BlockCommentCommand.cs` — create
- `src/LoomEditor.View/Features/Comment/CommentFeature.cs`, `CommentCommands.cs`, `README.md` — create (skeleton)
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: add `() => new CommentFeature()`
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — update
- `tests/LoomEditor.View.Tests/Features/Comment/BlockCommentCommandTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.View.Features.Comment` (ADR 0009 decision 14). The architecture tests
  (`ConventionTests`) require every feature folder to have `<Name>Feature.cs`, `<Name>Commands.cs`, `README.md` and a
  line in `EditorFeatures.cs`, so this card creates the **skeleton**: `CommentFeature` (public, `Id = "loom.comment"`,
  `Attach` registers nothing yet, `Dispose` does nothing), `CommentCommands` (public, the four ids of P5-51 as
  constants: `editor.action.commentLine`, `editor.action.addCommentLine`, `editor.action.removeCommentLine`,
  `editor.action.blockComment`), a README (role, Monaco sources, "actions come in P5-51") and
  `() => new CommentFeature()` in `EditorFeatures.Default`. Update `PublicAPI.Unshipped.txt` of `LoomEditor.View`.
- `internal sealed class BlockCommentCommand(Selection selection, bool insertSpace, LanguageConfigurationRegistry languageConfigurations) : IEditCommand`.
  `getLanguageConfiguration(languageId).comments` is step B's `ResolvedLanguageConfiguration.Comments`
  (`CommentsConfiguration?` record of P5-14: `LineCommentToken`, `LineCommentNoIndent`, `BlockCommentStartToken`,
  `BlockCommentEndToken`). `!config.blockCommentStartToken` → `string.IsNullOrEmpty`.
- `model.tokenization.tokenizeIfCheap` → `model.Tokenization.TokenizeIfCheap`; `getLanguageIdAtPosition` →
  `model.Tokenization.GetLanguageIdAtPosition` (both exist).
- `_haystackHasNeedleAtOffset`, `_createRemoveBlockCommentOperations` and `_createAddBlockCommentOperations` are
  `internal static` (`LineCommentCommand` calls them, P5-49). Keep Monaco's names without the underscore:
  `HaystackHasNeedleAtOffset`, `CreateRemoveBlockCommentOperations`, `CreateAddBlockCommentOperations`. They return
  `List<(Range Range, string? Text)>` (Monaco's `ISingleEditOperation` without `forceMoveMarkers`, always false here).
- `computeCursorState` reads `helper.GetInverseEditOperations()` (exists): `range` is `ValidEditOperation.Range`.
- **Test helper.** `CommandTester.TestCommand(lines, selection, commandFactory, expectedLines, expectedSelection, options = null, languageId = null, prepare = null)`
  with `commandFactory: Func<TestEditorServices, Selection, IEditCommand>` and `prepare: Action<TestEditorServices>?`
  is step D's (P5-40, on `TestEditorServices` of P5-37). The factory passes the services' language configuration
  registry to the command. `_testCommentCommand`'s `prepare` registers language `commentMode` and
  `new LanguageConfiguration { Comments = new CommentRule(new LineCommentConfig("!@#"), new CharacterPair("<0", "0>")) }`
  (P5-10 types) in that registry.

## Tests

Port, suite `Editor Contrib - Block Comment Command` (12):

- [ ] `empty selection wraps itself`, `invisible selection ignored`, `bug9511`
- [ ] `one line selection`, `one line selection toggle`, `multi line selection`, `multi line selection toggle`
- [ ] `fuzzy removes`, `bug #30358`, `issue #34618`
- [ ] `insertSpace false`, `insertSpace false does not remove space`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `contrib/comment/browser/blockCommentCommand.ts` → `Features/Comment/BlockCommentCommand.cs`
      (✅, P5-48); tests table `blockCommentCommand.test.ts` 12 / 12
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`LineCommentCommand` (P5-49), the actions and keybindings (P5-51).

## Suggested commit message

`feat(view): port BlockCommentCommand`
