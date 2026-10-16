# P3-30 — Word deletes, word selection and word parts (`WordOperations` 2/2)

- **Status:** done
- **Depends on:** P3-29
- **Size:** M

## Goal

Ctrl+Backspace / Ctrl+Delete delete by word; double-click selects a word (the `word` function, used by the mouse
in P3-44); "word parts" move and delete inside `camelCase` and `snake_case` identifiers. Port the second half of
`cursorWordOperations.ts`, the delete commands of the `wordOperations` contribution, the `wordPartOperations`
contribution, and their tests.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorWordOperations.ts` | 413–717 | `_deleteWordLeftWhitespace`, `deleteWordLeft`, `deleteInsideWord`, `_charAtIsWhitespace`, `_deleteInsideWordWhitespace`, `_deleteInsideWordDetermineDeleteRange`, `_deleteWordPartLeft`, `_findFirstNonWhitespaceChar`, `_deleteWordRightWhitespace`, `deleteWordRight`, `_deleteWordPartRight` |
| same | 719–821 | `_createWordAtPosition`, `getWordAtPosition`, `word` (double-click word selection) |
| same | 824–868 | `class WordPartOperations`, `enforceDefined` |
| `src/vs/editor/common/cursor/cursorMoveCommands.ts` | 236–240 | `CursorMoveCommands.word` |
| `src/vs/editor/contrib/wordOperations/browser/wordOperations.ts` | 317–516 | `DeleteWordOptions`, `DeleteWordCommand`, `DeleteWordLeftCommand`, `DeleteWordRightCommand`, the 6 `DeleteWord…` classes, `DeleteInsideWord` |
| `src/vs/editor/contrib/wordPartOperations/browser/wordPartOperations.ts` | 18–157 | `DeleteWordPartLeft`, `DeleteWordPartRight`, `WordPartLeftCommand`, `CursorWordPartLeft(Select)`, `WordPartRightCommand`, `CursorWordPartRight(Select)` |
| `src/vs/editor/contrib/wordOperations/test/browser/wordOperations.test.ts` | 540–1028 | the delete tests |
| `src/vs/editor/contrib/wordPartOperations/test/browser/wordPartOperations.test.ts` | all (287 lines) | 15 tests |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | 2383 | `issue #832: word right` (suite `Editor Controller`) |

## Files to create or change

- `src/LoomEditor.View/Cursors/WordOperations.Delete.cs`, `WordOperations.Word.cs`, `WordPartOperations.cs` — create
- `src/LoomEditor.View/Cursors/CursorMoveCommands.cs` — change: add `Word`
- `src/LoomEditor.View/Features/WordOperations/DeleteWordCommand.cs` — create; `WordOperationsFeature.cs`, `WordOperationsCommands.cs` — change
- `src/LoomEditor.View/Features/WordPartOperations/WordPartOperationsFeature.cs`, `WordPartOperationsCommands.cs`, `README.md` — create
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: add `() => new WordPartOperationsFeature()`
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `GetCursorAutoClosedCharacters()` returns an empty list (phase 4–5 cut point)
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — update
- `tests/LoomEditor.View.Tests/Features/WordOperations/WordOperationsDeleteTests.cs`,
  `tests/LoomEditor.View.Tests/Features/WordPartOperations/WordPartOperationsTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `deleteWordLeft` calls `DeleteOperations.isAutoClosingPairDelete` (needs auto-closing pairs): **phase 4–5 cut point**,
  treat it as `false`. `DeleteWordContext` keeps the fields `AutoClosingDelete`, `AutoClosingBrackets`,
  `AutoClosingQuotes`, `AutoClosedCharacters`; `autoClosingPairs` is not a field in phase 3 (write the cut comment).
- `deleteWordLeft` / `deleteWordRight` return `Range | null` in Monaco: return `Range?`. They sometimes return the
  `Selection` itself: convert with `selection.Range` (conventions §3). `_deleteWordPartLeft` builds
  `new Range(pos, toPos)` with reversed positions: C# `Range` swaps them like Monaco.
- `enforceDefined` + `candidates[2]` can be `undefined` (fewer than 3 candidates): use `ElementAtOrDefault` and the
  same fallbacks as Monaco (`DeleteWordLeftCommand` → `(1,1)-(1,1)`, `DeleteWordRightCommand` → end of document).
- Monaco sorts candidates; ties compare equal values: use a stable sort anyway (conventions §4.3).
- `DeleteWordCommand.RunEditorCommand(ViewModel viewModel)`: one `ReplaceCommand(deleteRange, "")` per selection,
  `Model.PushStackElement()`, `ViewModel.ExecuteCommands(commands, source: id)`, `Model.PushStackElement()` (Monaco's
  `pushUndoStop` / `executeCommands` / `pushUndoStop`, lines 360–367).
- `DeleteInsideWord` is an `EditorAction` in Monaco with the argument `onlyWord`; in C# register it as
  `deleteInsideWord` (id from the source) with `onlyWord = false`, and expose `internal static void Run(ViewModel, bool onlyWord)`
  for the tests that pass `onlyWord: true`.
- `WordPartOperationsFeature` (public, `Id = "loom.wordPartOperations"`) registers `deleteWordPartLeft`,
  `deleteWordPartRight`, `cursorWordPartLeft`, `cursorWordPartLeftSelect`, `cursorWordPartRight`,
  `cursorWordPartRightSelect` (check the ids in the source). It reuses `MoveWordCommand` / `DeleteWordCommand` (P3-29).
  The Monaco test uses a `StaticServiceAccessor` with a stub language configuration service: not needed in C#.

## Tests

- [x] Port `wordOperations.test.ts` lines 540–1028 (32 tests) **except** `deleteWordLeft - issue #91855` (874): it
      registers a language with auto-closing pairs (phase 4–5) — not ported, reason in the PORTING tests table
- [x] Port `wordPartOperations.test.ts` (15 tests)
- [x] Port `cursor.test.ts` › `issue #832: word right` (2383)
- [x] New: `CursorMoveCommands.Word` on `"hello world"` at `(1,8)` selects `(1,7) → (1,12)`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `PublicAPI.Unshipped.txt` of `LoomEditor.View`: only `WordPartOperationsFeature`, `WordPartOperationsCommands` and
      the new delete ids of `WordOperationsCommands`
- [x] `docs/PORTING.md`: map rows `cursorWordOperations.ts` (✅ except Intl segmentation, P3-30), `wordOperations.ts`,
      `wordPartOperations.ts`; tests table `wordOperations.test.ts` 52 / 56, `wordPartOperations.test.ts` 15 / 15
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Keybindings (P3-39), double-click wiring (P3-44).

## Suggested commit message

`feat(view): port word deletes, word selection and word parts`
