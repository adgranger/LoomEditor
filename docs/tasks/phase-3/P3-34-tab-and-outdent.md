# P3-34 — Tab and Shift+Tab (`tab`, `outdent`)

- **Status:** todo
- **Depends on:** P3-33
- **Size:** S

## Goal

Tab inserts spaces up to the next indent stop (or a tab character), indents the selected lines, or fixes the
indentation of a whitespace-only line; Shift+Tab outdents. Port `TabOperation`, `TypeOperations.indent/outdent/tab`
and the core commands `tab` and `outdent`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 837–875 | `TabOperation.getCommands` |
| same | 877–916 | `_goodIndentForLine` — **cut point**, see below |
| same | 918–932 | `_replaceJumpToNextIndent` |
| `src/vs/editor/common/cursor/cursorTypeOperations.ts` | 18–50, 64–66 | `indent`, `outdent`, `tab` |
| `src/vs/editor/browser/coreCommands.ts` | 2001–2045 | `Outdent` (`outdent`), `Tab` (`tab`) |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | see Tests | 12 tests of the suite `Editor Controller` (10 + 2 moved from P3-33) |

## Files to create or change

- `src/LoomEditor.View/Cursors/TypeEditOperations.Tab.cs` — create (partial): `TabOperation`
- `src/LoomEditor.View/Cursors/TypeOperations.cs` — change: `Indent`, `Outdent`, `Tab`
- `src/LoomEditor.View/Commands/CoreEditingCommands.cs`, `CoreCommandRegistration.cs` — change: `tab`, `outdent`
- `tests/LoomEditor.View.Tests/Cursors/TabTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `/^\s*$/.test(lineText)` → `Strings.IsJavaScriptWhitespaceOnly(lineText)` (P3-31). `isCheapToTokenize` is `true` (phase 4 cut point).
- `_goodIndentForLine`: it calls `getInheritIndentForLine` and `getEnterAction`, which both return `null` without a
  language configuration; the method then returns `null`. Port it as
  `private static string? GoodIndentForLine(...) => null; // Phase 4–5 cut point: cursorTypeEditOperations.ts 877–916`.
  `getCommands` then uses `"\t"`, normalized, exactly as Monaco.
- `new ShiftCommand(selection, {...}, config.languageConfigurationService)` → `new ShiftCommand(selection, new ShiftCommandOptions(…))` (P3-31).
- The commands: `Model.PushStackElement()`, `ViewModel.ExecuteCommands(TypeOperations.Tab(...), source: "tab")`,
  `Model.PushStackElement()` (lines 2040–2044); same for `outdent`.
- `TypeOperations.Indent(config, model, selections)` (lines 18–35) accepts `null` model/selections in Monaco and
  returns `[]`: use nullable parameters.

## Tests

Port from `cursor.test.ts`, suite `Editor Controller`:

- [ ] `issue microsoft/monaco-editor#443: Indentation of a single row deletes selected text in some cases` (1674)
- [ ] `Bug 9121: Auto indent + undo + redo is funky` (1694)
- [ ] `Bug #18293:[regression][editor] Can't outdent whitespace line` (1869)
- [ ] `issue #95591: Unindenting moves cursor to beginning of line` (1886)
- [ ] `Bug #16657: [editor] Tab on empty line of zero indentation moves cursor to position (1,1)` (1903)
- [ ] `issue #3071: Investigate why undo stack gets corrupted` (2273)
- [ ] `issue #3463: pressing tab adds spaces, but not as many as for a tab` (2345)
- [ ] `issue #4312: trying to type a tab character over a sequence of spaces results in unexpected behaviour` (2363)
- [ ] `Cursor honors insertSpaces configuration on tab` (3212)
- [ ] `PR #5423: Auto indent + undo + redo is funky` (3712)
- [ ] `removeAutoWhitespace on: removes only whitespace the cursor added 2` (3467; plain model, no language) and `removeAutoWhitespace on: test 1` (3511), moved from P3-33 because they use the Tab command

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `cursorTypeEditOperations.ts` (+ `TabOperation`, P3-34); tests table `cursor.test.ts` (+10)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The Tab key's `when` clause (`tabDoesNotMoveFocus`) and keybindings (P3-39); language-aware indentation (phase 4–5).

## Suggested commit message

`feat(view): port Tab and Shift+Tab`
