# P3-29 — Word moves (`WordOperations` 1/2) and the `WordOperations` feature

- **Status:** todo
- **Depends on:** P3-28
- **Size:** M

## Goal

Ctrl+← / Ctrl+→ move by word, using the word separators. Port the word scanning and the word moves of
`cursorWordOperations.ts`, and Monaco's word move commands (`cursorWordLeft`, `cursorWordEndRight`… from the
`wordOperations` contribution) as a LoomEditor **feature** (one folder under `Features/`, ARCHITECTURE rule 5).
Port the move tests of `wordOperations.test.ts`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorWordOperations.ts` | 18–61 | `IFindWordResult`, `WordType`, `WordNavigationType`, `DeleteWordContext` (the type only; used in P3-30) |
| same | 63–209 | `WordOperations`: `_createWord`, `_createIntlWord`, `_findPreviousWordOnLine`, `_doFindPreviousWordOnLine`, `_findEndOfWord`, `_findNextWordOnLine`, `_doFindNextWordOnLine`, `_findStartOfWord` |
| same | 211–411 | `moveWordLeft`, `_moveWordPartLeft`, `moveWordRight`, `_moveWordPartRight` |
| `src/vs/editor/contrib/wordOperations/browser/wordOperations.ts` | 28–316 | `MoveWordOptions`, `MoveWordCommand`, `WordLeftCommand`, `WordRightCommand`, and the 16 `CursorWord…` command classes (ids and options) |
| `src/vs/editor/contrib/wordOperations/test/browser/wordTestUtils.ts` | 8–78 | `deserializePipePositions`, `serializePipePositions`, `testRepeatedActionAndExtractPositions` |
| `src/vs/editor/contrib/wordOperations/test/browser/wordOperations.test.ts` | 24–118 | suite setup and helpers (`runEditorCommand`, `cursorWordLeft`…) |
| same | 119–539 | the move tests (see Tests) |

## Files to create or change

- `src/LoomEditor.View/Cursors/WordOperations.cs` — create (`internal static partial class`; this card: find + moves)
- `src/LoomEditor.View/Cursors/WordNavigationType.cs`, `WordType.cs`, `FindWordResult.cs`, `DeleteWordContext.cs` — create
- `src/LoomEditor.View/Features/WordOperations/WordOperationsFeature.cs`, `WordOperationsCommands.cs`, `README.md` — create
- `src/LoomEditor.View/Features/WordOperations/MoveWordCommand.cs` — create (the command classes)
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: add `() => new WordOperationsFeature()`
- `src/LoomEditor.View/Features/EditorContextExtensions.cs` — create: `internal static ViewModel? GetViewModel(this IEditorContext context)` (`(context as EditorContext)?.ViewModel`)
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — update (the feature and the command id constants are public, like `SelectAll`)
- `tests/LoomEditor.View.Tests/Cursors/TestHelpers/WordTestUtils.cs` — create
- `tests/LoomEditor.View.Tests/Features/WordOperations/WordOperationsMoveTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first — especially §4.2: `_doFindNextWordOnLine` can read `charCodeAt(-1)`
  when `moveWordRight` is called with `WordAccessibility` and column 0; JavaScript returns `NaN`, which the classifier
  treats as `Regular`. In C#, read `-1` instead of indexing out of range (`CharacterClassifier.Get(-1)` returns the
  default, P3-04). Also §3: the column 0 of `WordAccessibility` cannot be a `Position`: keep `int` variables.
- `WordNavigationType { WordStart = 0, WordStartFast = 1, WordEnd = 2, WordAccessibility = 3 }` (check lines 44–49).
- `IFindWordResult` → `internal readonly record struct FindWordResult(int Start, int End, WordType WordType, WordCharacterClass NextCharClass)`.
- The `Intl` paths (`_createIntlWord`, `findPrevIntlWordBeforeOrAtOffset`…) are ported; they never return a word in
  phase 3 (P3-04 stubs return `null`).
- `strings.lastNonWhitespaceIndex(str, startIndex)` → `Strings.LastNonWhitespaceIndex(str, startIndex)` (P3-01).
- **Commands as a feature.** `WordOperationsFeature : IEditorFeature` (public, `Id = "loom.wordOperations"`)
  registers one `EditorCommand` per Monaco command id in `Attach` (it gets the `ViewModel` with
  `context.GetViewModel()`). `WordOperationsCommands` holds the ids as `public const string` with Monaco's values
  (`CursorWordLeft = "cursorWordLeft"`, `CursorWordEndRight = "cursorWordEndRight"`…: copy every id from the
  `super({ id: … })` calls of lines 101–316).
- `MoveWordCommand` is `internal abstract class` with `void RunEditorCommand(ViewModel viewModel)` doing what lines 44–62
  do: `WordCharacterClassifierCache.Get(CursorConfig.WordSeparators, CursorConfig.WordSegmenterLocales)`, map each
  selection, `Model.PushStackElement()`, `SetCursorStates("moveWordCommand", CursorChangeReason.Explicit, …)`, and for
  one cursor `RevealRange("moveWordCommand", true, Range.Collapsed(pos), VerticalRevealType.Simple, ScrollType.Smooth)`
  (Monaco: `editor.revealPosition(pos, ScrollType.Smooth)`).
- `WordTestUtils`: `DeserializePipePositions(string text) → (string Text, Position[] Positions)`,
  `SerializePipePositions(string text, IReadOnlyList<Position> positions)`,
  `TestRepeatedActionAndExtractPositions(string text, Position initialPosition, Action<TestCodeEditor> action, Func<TestCodeEditor, Position> record, Func<TestCodeEditor, bool> stopCondition, TestCodeEditorOptions? options = null)`.
  The initial position can be `(1000, 1000)`: `SetSelections` validates it (as Monaco's `setPosition`). Keep the
  1,000-iteration endless-loop guard.

## Tests

- [ ] Port the move tests of `wordOperations.test.ts`, lines 119–539: `cursorWordLeft - simple` … `cursorWordAccessibilityRight`
      (24 tests) **except**:
  - `cursorWordLeft - Recognize words` (183) and `cursorWordRight - Recognize words` (407): they need `Intl.Segmenter`
    with `wordSegmenterLocales: 'ja'` (not ported, P3-04) — not ported, reason in the PORTING tests table;
  - `issue #51275 - cursorWordStartLeft does not push undo/redo stack element` (305): it types text, so it is ported
    with typing in P3-33.
- [ ] New: the feature registers all 16 move ids; `Commands.Execute("cursorWordLeft")` moves the cursor

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (including the architecture tests for the new feature folder)
- [ ] `PublicAPI.Unshipped.txt` of `LoomEditor.View`: only `WordOperationsFeature` (constructor, `Id`, `Attach`, `Dispose`) and the `WordOperationsCommands` constants
- [ ] `docs/PORTING.md`: map rows `cursorWordOperations.ts` (🔶 moves, P3-29) and `contrib/wordOperations/browser/wordOperations.ts`
      → `Features/WordOperations/` (🔶 moves); tests table `wordOperations.test.ts` (21 / 56 so far)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Word deletes, `deleteInsideWord`, double-click `word` selection, word parts (P3-30); keybindings (P3-39).

## Suggested commit message

`feat(view): port word moves and the word operations feature`
