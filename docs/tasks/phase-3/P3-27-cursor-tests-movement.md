# P3-27 — Port the cursor movement tests of `cursor.test.ts`

- **Status:** done
- **Depends on:** P3-26
- **Size:** S (tests only, plus fixes)

## Goal

Port the first suite of Monaco's big cursor test file (`Editor Controller - Cursor`), the movement and state tests,
and fix the port where they fail. After this card, the cursor core and the moves are checked against Monaco's own
expectations.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/test/browser/controller/cursor.test.ts` (6,996 lines).

| Lines | What |
|---|---|
| 36–125 | helpers `moveTo`, `moveLeft`, `moveRight`, `moveDown`, `moveUp`, `moveToBeginningOfLine`, `moveToEndOfLine`, `moveToBeginningOfBuffer`, `moveToEndOfBuffer`, `assertCursor` |
| 127–148 | suite `Editor Controller - Cursor`: the 5-line text (`LINE1` … `LINE5`, mixed `\r\n` / `\n`) and `runTest` |
| 149–448 | `cursor initialized` … `issue #44465: cursor position not correct when move` (27 tests) |
| 567–907 | `move to beginning of line` … `Independent model edit 1` (35 tests) |
| 939–977 | `grapheme breaking` |
| 1332–1364 | `setSelection / setPosition with source` |
| suite `Editor Controller` 2552–2587 | `issue #23983: Calling model.setEOL does not reset cursor position`, `issue #23983: Calling model.setValue() resets cursor position` |
| suite `Editor Controller` 2748–2771 | `issue #22717: Moving text cursor cause an incorrect position in Chinese` |

## Files to create or change

- `tests/LoomEditor.View.Tests/Cursors/TestHelpers/CursorTestHelpers.cs` — create (the helpers of lines 36–125)
- `tests/LoomEditor.View.Tests/Cursors/CursorMovementTests.cs` — create
- any `src/LoomEditor.View/Cursors/*` or `src/LoomEditor.Core/*` file — fix the port when a test fails
  (and **only** to match Monaco)

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- The helpers call the core commands of P3-26 exactly like Monaco: `moveTo` → `CoreNavigationCommands.MoveTo` /
  `MoveToSelect` with `MoveCommandArgs`; `moveLeft` → `CursorLeft` / `CursorLeftSelect`; and so on.
  `assertCursor` accepts a `Position`, a `Selection` or a `Selection[]`, as in Monaco (three overloads).
- The model text keeps its mixed line breaks: `LINE1 + "\r\n" + LINE2 + "\n" + LINE3 + "\n" + LINE4 + "\r\n" + LINE5`.
  Copy `LINE1` … `LINE5` exactly (they contain tabs and spaces).
- `runTest` creates the editor with `TestCodeEditor.WithTestCodeEditor` (P3-20).
- Event tests (`no move doesn't trigger event`, `move eventing`, `move in selection mode eventing`) subscribe to
  `viewModel.EventDispatcher.OnEvent` (Monaco: `viewModel.onEvent`).
- If a ported test fails: find the difference with Monaco in the port (a wrong `Math.round`, an unstable sort, a
  missing grapheme step…) and fix the port. **Never change the expected values.** If you believe a test cannot pass,
  stop and report it with the failing assertion.

## Tests

- [x] Port lines 149–448 (27 tests)
- [x] Port lines 567–907 (35 tests), including `select all` (through `CursorMoveCommands.SelectAll` +
      `SetCursorStates`, or `Commands.Execute("editor.action.selectAll")` if the test uses the command — follow the Monaco code)
- [x] Port `grapheme breaking` (939) and `setSelection / setPosition with source` (1332)
- [x] Port suite `Editor Controller` › `issue #23983` (2 tests) and › `issue #22717`
- [x] **Not ported now** (write each in the PORTING tests table with its reason):
      `issue #144041` (449) and `issue #140195` (500): word wrap / injected text (phase 6);
      the 6 column selection tests (908, 978, 1007, 1065, 1110, 1125): ported in P3-28

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: phase 3 tests table row `cursor.test.ts` (suite `Editor Controller - Cursor`: ported / total, and the reasons)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Typing, deleting, paste (later cards port the matching tests of the suite `Editor Controller`).

## Suggested commit message

`test(view): port the cursor movement tests of cursor.test.ts`
