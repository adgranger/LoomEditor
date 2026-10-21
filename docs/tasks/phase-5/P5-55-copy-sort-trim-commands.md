# P5-55 — `CopyLinesCommand`, `SortLinesCommand`, `TrimTrailingWhitespaceCommand`

- **Status:** done
- **Depends on:** P5-54
- **Size:** M

## Goal

Port three small line commands with their test files: copy the selected lines up or down, sort them, and trim
trailing whitespace (skipping strings and regexes when asked). The `LinesOperations` feature folder exists with its
skeleton.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/linesOperations/browser/copyLinesCommand.ts` | 11–94 | `CopyLinesCommand` |
| `contrib/linesOperations/browser/sortLinesCommand.ts` | 14–112 | `SortLinesCommand` (`canRun`), `getSortData`, `sortLines` |
| `common/commands/trimTrailingWhitespaceCommand.ts` | 15–126 | `TrimTrailingWhitespaceCommand`, `trimTrailingWhitespace` |
| `contrib/linesOperations/test/browser/copyLinesCommand.test.ts` | 14–203 | helpers + suite `Editor Contrib - Copy Lines Command` (8 tests) |
| `contrib/linesOperations/test/browser/sortLinesCommand.test.ts` | 11–169 | helpers + suite `Editor Contrib - Sort Lines Command` (7 tests) |
| `src/vs/editor/test/browser/commands/trimTrailingWhitespaceCommand.test.ts` | 24–56 | helpers `createInsertDeleteSingleEditOp`, `createSingleEditOp`, `assertTrimTrailingWhitespaceCommand`, `assertTrimTrailingWhitespace` |
| same | 57–end | suite `Editor Commands - Trim Trailing Whitespace Command` (2 tests) |

## Files to create or change

- `src/LoomEditor.View/Features/LinesOperations/CopyLinesCommand.cs`, `SortLinesCommand.cs` — create
- `src/LoomEditor.View/Cursors/Commands/TrimTrailingWhitespaceCommand.cs` — create (ADR 0009: `common/commands/*` → `Cursors/Commands/`)
- `src/LoomEditor.View/Features/LinesOperations/LinesOperationsFeature.cs`, `LinesOperationsCommands.cs`, `README.md` — create (skeleton)
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: add `() => new LinesOperationsFeature()`
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — update
- `tests/LoomEditor.View.Tests/Features/LinesOperations/CopyLinesCommandTests.cs`, `SortLinesCommandTests.cs` — create
- `tests/LoomEditor.View.Tests/Cursors/Commands/TrimTrailingWhitespaceCommandTests.cs` — create

## Design decisions (already made — do not change)

- Skeleton (required by `ConventionTests`): `LinesOperationsFeature` (public, `Id = "loom.linesOperations"`, registers
  nothing yet), `LinesOperationsCommands` (public constants with Monaco's ids, all of them now so later cards only
  register): `editor.action.copyLinesUpAction`, `editor.action.copyLinesDownAction`, `editor.action.duplicateSelection`,
  `editor.action.moveLinesUpAction`, `editor.action.moveLinesDownAction`, `editor.action.sortLinesAscending`,
  `editor.action.sortLinesDescending`, `editor.action.removeDuplicateLines`, `editor.action.reverseLines`,
  `editor.action.trimTrailingWhitespace`, `editor.action.deleteLines`, `editor.action.indentLines`,
  `editor.action.outdentLines`, `editor.action.insertLineBefore`, `editor.action.insertLineAfter`, `deleteAllLeft`,
  `deleteAllRight`, `editor.action.joinLines`, `editor.action.transpose`, `editor.action.transformToUppercase`,
  `editor.action.transformToLowercase`, `editor.action.transformToTitlecase`, `editor.action.transformToSnakecase`,
  `editor.action.transformToCamelcase`, `editor.action.transformToPascalcase`, `editor.action.transformToKebabcase`.
- `internal sealed class CopyLinesCommand(Selection selection, bool isCopyingDown, bool noop = false) : IEditCommand`;
  `SelectionDirection` → `Selection.Direction` / `Selection.FromRange(range, direction)` (phase 3 table).
- **Sort order.** `Intl.Collator().compare` (default locale, default options) →
  `string.Compare(a, b, CultureInfo.InvariantCulture, CompareOptions.None)` with a **stable** sort (`OrderBy`).
  Behavior difference (XML doc + `docs/PORTING.md`): the invariant culture, not the user's locale. `SortLinesCommand.CanRun`
  is `internal static`; `getSortData` returns `null` → a nullable record.
- `TrimTrailingWhitespaceCommand(Selection selection, IReadOnlyList<Position> cursors, bool trimInRegexesAndStrings)`
  and `internal static List<(Range Range, string? Text)> TrimTrailingWhitespace(TextModel model, List<Position> cursors, bool trimInRegexesAndStrings)`.
  Monaco sorts and splices the caller's array in place: copy it first (`new List<Position>(cursors)`), sort with
  `Position.CompareTo` (stable `OrderBy`), then remove duplicates per line as Monaco does.
  `strings.lastNonWhitespaceIndex` → `Strings.LastNonWhitespaceIndex` (Core, exists).
  `hasAccurateTokensForLine`, `getLineTokens`, `findTokenIndexAtOffset`, `getStandardTokenType` exist on
  `TokenizationTextModelPart` / `LineTokens`.
- The copy and sort tests use `CommandTester.TestCommand` (no language). The trim tests use
  `CommandTester.GetEditOperation` and, for `skips strings and regex if configured`, a `TestEditorServices` (P5-37) model whose
  language has a test `ITokenizationSupport` (registered in the services' `TokenizationRegistry`) returning the three token arrays of the test (metadata with
  `BALANCED_BRACKETS_MASK`, as in Monaco).

## Tests

- [x] Port `Editor Contrib - Copy Lines Command` › `copy first line down`, `copy first line up`, `copy last line down`, `copy last line up`, `issue #1322: copy line up`, `issue #1322: copy last line up`, `copy many lines up`, `ignore empty selection`
- [x] Port `Editor Contrib - Sort Lines Command` › `no op unless at least two lines selected 1`, `no op unless at least two lines selected 2`, `sorting two lines ascending`, `sorting first 4 lines ascending`, `sorting all lines ascending`, `sorting first 4 lines descending`, `sorting all lines descending`
- [x] Port `Editor Commands - Trim Trailing Whitespace Command` › `remove trailing whitespace`, `skips strings and regex if configured`
- [x] New: `TrimTrailingWhitespace_DoesNotSortTheCallersList`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows for the three files (✅, P5-55); behavior difference "sort uses the invariant
      culture"; tests tables `copyLinesCommand.test.ts` 8 / 10 (Duplicate Selection in P5-57), `sortLinesCommand.test.ts`
      7 / 7, `trimTrailingWhitespaceCommand.test.ts` 2 / 2
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The actions and keybindings (P5-57), `MoveLinesCommand` (P5-56).

## Suggested commit message

`feat(view): port the copy, sort and trim trailing whitespace commands`
