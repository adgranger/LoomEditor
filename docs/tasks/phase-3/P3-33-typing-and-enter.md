# P3-33 — Typing and Enter (`TypeOperations`, `type`, `lineBreakInsert`)

- **Status:** done
- **Depends on:** P3-32
- **Size:** M

## Goal

Typed characters replace every selection; Enter inserts a line break and keeps the indentation of the current line;
undo groups typing the way VS Code does (a new undo step after a space, between typing and deleting…). Port the
plain-text part of Monaco's typing: the interceptor chain keeps its shape, but only Enter and the simple character
are active in phase 3 (no language configuration).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 548–564 | `SimpleCharacterTypeOperation` |
| same | 566–700 | `EnterOperation`: `getEdits`, `_enter` (see the cuts), `lineInsertBefore`, `lineInsertAfter`, `lineBreakInsert` |
| same | 822–835 | `TypeWithoutInterceptorsOperation` |
| same | 1004–1039 | `getTypingOperation`, `shouldPushStackElementBetween`, `normalizeOperationType`, `isTypingOperation` |
| same | 1083–1099 | `typeCommand`, `shiftIndent`, `unshiftIndent` |
| `src/vs/editor/common/cursor/cursorTypeOperations.ts` | 52–58 | `shiftIndent`, `unshiftIndent` |
| same | 165–202 | `typeWithInterceptors` (skeleton, see below), `typeWithoutInterceptors` |
| `src/vs/editor/common/cursor/cursor.ts` | 564–587 | `CursorsController.type` |
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | search `public type(` | `ViewModel.type` |
| `src/vs/editor/browser/coreCommands.ts` | 1981–1999 | `LineBreakInsert` (`lineBreakInsert`) |

## Files to create or change

- `src/LoomEditor.View/Cursors/TypeOperations.cs` — create (`internal static class`, the entry points of `cursorTypeOperations.ts` ported so far)
- `src/LoomEditor.View/Cursors/TypeEditOperations.cs` — create (`SimpleCharacterTypeOperation`, `EnterOperation`,
  `TypeWithoutInterceptorsOperation`, and the free functions; split into `TypeEditOperations.Enter.cs` if needed)
- `src/LoomEditor.View/Cursors/CursorsController.Type.cs` — create (partial): `Type`
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `Type(string text, string? source = null)`
- `src/LoomEditor.View/Commands/CoreEditingCommands.cs`, `CoreCommandRegistration.cs` — change: `lineBreakInsert`
- `tests/LoomEditor.View.Tests/Cursors/TypingTests.cs`, `UndoStopsTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `typeWithInterceptors` keeps Monaco's order of interceptors as comments, and only two are active:
  ```
  EnterOperation.GetEdits(...)            // active
  // Phase 4–5 cut point: AutoIndentOperation (cursorTypeEditOperations.ts 29–113)
  // Phase 4–5 cut point: AutoClosingOvertypeOperation (115–152)
  // Phase 4–5 cut point: AutoClosingOpenCharTypeOperation (154–404)
  // Phase 4–5 cut point: SurroundSelectionOperation (421–478)
  // Phase 4–5 cut point: InterceptorElectricCharOperation (480–546)
  SimpleCharacterTypeOperation.GetEdits(...) // active
  ```
  Monaco's `getEdits` return `undefined` when they do not apply: use `null` (`EditOperationResult?`).
- `_enter` (lines 582–658), for a model with no language configuration:
  - `autoIndent == None` → `typeCommand(range, "\n", keepPosition)` (port).
  - `!isCheapToTokenize || autoIndent == Keep` → keep the leading whitespace (port). `isCheapToTokenize` is always
    `true` in phase 3 (no tokenizer): write it as a local `const bool isCheapToTokenize = true; // Phase 4 cut point`.
  - `getEnterAction` (591–615) and `getIndentForEnter` (620–656): **cut points**. Without a language configuration
    Monaco gets `null` from both and falls through to the last line (657):
    `typeCommand(range, "\n" + config.NormalizeIndentation(indentation), keepPosition)` with
    `indentation = Strings.GetLeadingWhitespace(lineText).Substring(0, range.StartColumn - 1)` — careful: Monaco's
    `substring(0, n)` clamps `n` to the string length; C# `Substring` throws. Use `Math.Min`.
- `typeCommand` creates commands with `insertsAutoWhitespace: true`: this is what makes `trimAutoWhitespace` (P3-11) work.
- `CursorsController.Type(collector, text, source)`, port of lines 564–587: when `source == "keyboard"`, iterate
  `Strings.NextCharLength` graphemes and call `TypeOperations.TypeWithInterceptors` for each (`text.Substring(offset, charLength)`,
  Monaco's `substr`); otherwise `TypeWithoutInterceptors` for the whole text. `isDoingComposition` is `false` until P3-49
  (`_compositionState` does not exist yet: write `false` with a comment). `autoClosedCharacters` is the empty list of P3-30.
- `ReplaceCommand(selections[i], ch)`: Monaco passes a `Selection` as a `Range`: use `selections[i].Range`.
- `EditOperationType` numbers are kept (there is no 1) and `normalizeOperationType` returns either a type or `'space'`:
  return an `int` key: `-1` for the two space types, otherwise `(int)type` (comment: "Monaco returns the string 'space'").
- Undo stops: `shouldPushStackElementBetween` decides `ShouldPushStackElementBefore`; `CursorsController.ExecuteEditOperation`
  (P3-19) does the rest.

## Tests

Port from `src/vs/editor/test/browser/controller/cursor.test.ts` (with `TestCodeEditor`; typing is
`viewModel.Type(text, "keyboard")`):

- [x] (15 of these 17: `removeAutoWhitespace on: removes only whitespace the cursor added 2` and `removeAutoWhitespace on: test 1` use the Tab command and moved to P3-34) Suite `Editor Controller`: `issue #23539: Setting model EOL isn't undoable` (1754), `issue #46208: Allow empty
      selections in the undo/redo stack` (1794), `issue #12950: Cannot Double Click To Insert Emoji Using OSX Emoji Panel`
      (2324), `issue #37967: problem replacing consecutive characters` (2853), `issue #15761: Cursor doesn't move in a
      redo operation` (2885), `issue #42783: API Calls with Undo Leave Cursor in Wrong Position` (2918), `Cursor honors
      insertSpaces configuration on new line` (3195), `issue #148256` (3333 and 3345), `removeAutoWhitespace off` (3360),
      `removeAutoWhitespace on: removes only whitespace the cursor added 1` (3384), `… added 2` (3467: it registers a
      language **without** configuration, use a plain model), `removeAutoWhitespace on: test 1` (3511), `issue #90973:
      Undo brings back model alternative version` (3777), `onEnter works if there are no indentation rules` (4311) and
      `… 2` (4333), `issue #111513: Text gets automatically selected when typing at the same location in another editor` (6436:
      two `TestCodeEditor`s on one `TextModel`)
- [x] Suite `Undo stops` (lines 6461–6816, all 11 tests)
- [x] `wordOperations.test.ts` › `issue #51275 - cursorWordStartLeft does not push undo/redo stack element` (305)
- [x] New: typing `"a👍🏽b"` with source `"keyboard"` makes 3 typing operations (one per grapheme) in one undo step

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows `cursorTypeOperations.ts`, `cursorTypeEditOperations.ts` (🔶 plain typing and Enter, P3-33);
      tests table `cursor.test.ts` (+17 in `Editor Controller`, `Undo stops` 11 / 11), `wordOperations.test.ts` (53 / 56)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Tab (P3-34), paste (P3-35), composition (P3-49), auto-closing, auto-indent, electric characters, surround (phase 4–5),
overtype.

## Suggested commit message

`feat(view): port typing and Enter`
