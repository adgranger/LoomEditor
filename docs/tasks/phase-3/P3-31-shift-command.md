# P3-31 — `ShiftCommand` (indent and outdent lines)

- **Status:** todo
- **Depends on:** P3-30
- **Size:** S

## Goal

Tab on a multi-line selection indents every line; Shift+Tab outdents. Port Monaco's `ShiftCommand` and its test file.
The only language-dependent part (re-aligning lines after an "enter action") is a phase 4–5 cut point.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/commands/shiftCommand.ts` | 17–24 | `IShiftCommandOpts` |
| same | 26–39 | `cachedStringRepeat` (see below) |
| same | 41–278 | `class ShiftCommand`: `unshiftIndent`, `shiftIndent`, constructor, `_addEditOperation`, `getEditOperations`, `computeCursorState` — **cut lines 144–176** (see below) |
| `src/vs/editor/test/browser/commands/shiftCommand.test.ts` | 25–76 | helpers `createSingleEditOp`, `testShiftCommand`, `testUnshiftCommand` |
| same | 84–999 | suite `Editor Commands - ShiftCommand` (25 tests) |

## Files to create or change

- `src/LoomEditor.View/Cursors/Commands/ShiftCommand.cs` — create
- `src/LoomEditor.Core/Text/Strings.cs` — change: add `IsJavaScriptWhitespaceOnly(string text)` (see below)
- `tests/LoomEditor.View.Tests/Cursors/Commands/ShiftCommandTests.cs` — create
- `tests/LoomEditor.Core.Tests/Text/StringsTests.cs` — change: tests for the new helper

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `internal sealed record ShiftCommandOptions(bool IsUnshift, int TabSize, int IndentSize, bool InsertSpaces, bool UseTabStops, EditorAutoIndentStrategy AutoIndent)`.
- Constructor `ShiftCommand(Selection range, ShiftCommandOptions opts)`: Monaco's third parameter (the language
  configuration service) is not ported.
- **Cut point, lines 144–176** (`model.tokenization.isCheapToTokenize` + `getEnterAction` to handle "miss-aligned"
  lines): replace the block with
  `// Phase 4–5 cut point: shiftCommand.ts 144–176 (enter action of the previous line). Without a language configuration Monaco gets no enter action and does nothing here.`
  Keep `previousLineExtraSpaces` / `extraSpaces` variables so the phase 4 port fits back in.
- `/^\s*$/.test(line)` (lines ~117 and in `TabOperation` later) uses **JavaScript's** `\s`. Add to `Strings` (Core):
  `internal static bool IsJavaScriptWhitespaceOnly(string text)` → true when every char is one of
  `\t \n \v \f \r ' '      -          　 ﻿` (empty string → true).
  Do not use `char.IsWhiteSpace` or .NET `Regex` `\s` (different sets).
- `cachedStringRepeat` is a global mutable cache in Monaco: do **not** port the cache; use `new string(' ', n)` /
  a `StringBuilder` repeating `"\t"`. Write a behavior-difference row (performance only, same result).
- `strings.firstNonWhitespaceIndex` → the existing `StringClassification.FirstNonWhitespaceIndex`.
- The test helpers become private methods of the test class, using `CommandTester.TestCommand` (P3-14) with a factory
  `sel => new ShiftCommand(sel, new ShiftCommandOptions(IsUnshift: false, TabSize: 4, IndentSize: 4, InsertSpaces: false, UseTabStops: useTabStops, AutoIndent: EditorAutoIndentStrategy.Full))`.
  `bug #16815` uses `getEditOperation`: `CommandTester.GetEditOperation`.

## Tests

- [ ] Port the 25 tests of `shiftCommand.test.ts` **except** `issue #348: indenting around doc block comments` (567)
      and `issue #1609: Wrong indentation of block comments` (635): they register a language with doc-block on-enter
      rules (phase 4–5) — not ported, reason in the PORTING tests table. 23 tests.
- [ ] New: `Strings.IsJavaScriptWhitespaceOnly` — `""`, `" \t"`, `" 　"`, `"﻿"` → true; `"​"` (zero
      width space, not `\s` in JavaScript) → false; `" a"` → false

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `editor/common/commands/shiftCommand.ts` (✅ except the enter-action re-alignment,
      P3-31); behavior difference "no `cachedStringRepeat` cache"; tests table `shiftCommand.test.ts` 23 / 25
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The Tab and Shift+Tab commands that create `ShiftCommand`s (P3-34).

## Suggested commit message

`feat(view): port ShiftCommand`
