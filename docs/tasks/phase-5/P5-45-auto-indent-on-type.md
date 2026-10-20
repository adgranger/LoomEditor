# P5-45 — Auto-indent while typing (`AutoIndentOperation`)

- **Status:** todo
- **Depends on:** P5-44
- **Size:** S

## Goal

With `autoIndent: full`, typing a character that makes the line match an indentation rule (`}` against
`decreaseIndentPattern`, `end` in Ruby…) re-indents the line at once, and auto-closes in the same edit when the
character opens a pair. Port `AutoIndentOperation` and `TypeWithIndentationAndAutoClosingCommand`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 29–113 | `AutoIndentOperation` (`getEdits`, `_isAutoIndentType`, `_findActualIndentationForSelection`, `_getIndentationAndAutoClosingPairEdits`, `_getEditFromIndentationAndSelection`) |
| same | 973–1002 | `TypeWithIndentationAndAutoClosingCommand` |
| `src/vs/editor/common/cursor/cursorTypeOperations.ts` | 172–175 | `typeWithInterceptors`: the `AutoIndentOperation` step |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | see Tests | 8 tests of the suite `Editor Controller` |

Step B provides `getIndentActionForType` (`autoIndent.ts` 360–433) and `getIndentationAtPosition`
(`languageConfigurationRegistry.ts` 178–185).

## Files to create or change

- `src/LoomEditor.View/Cursors/TypeEditOperations.AutoIndent.cs` — create
- `src/LoomEditor.View/Cursors/TypeEditOperations.AutoClosing.cs` — change: `TypeWithIndentationAndAutoClosingCommand`
- `src/LoomEditor.View/Cursors/TypeOperations.cs` — change
- `tests/LoomEditor.View.Tests/Cursors/AutoIndentOnTypeTests.cs` — create

## Design decisions (already made — do not change)

- `getIndentActionForType(config, model, selection, ch, converter, service)`: Monaco passes the whole
  `CursorConfiguration` but reads only `autoIndent`; call the step B function with what it takes (`config.AutoIndent`
  or the configuration, as step B declared it), the `CursorIndentConverter` of P5-39 and
  `config.LanguageConfigurationService`.
- `config.autoIndent < EditorAutoIndentStrategy.Full` compares the enum values (`None` 0 … `Full` 4, as Monaco).
- `{ selection, indentation }` pairs and `{ range, text }` edits are private `readonly record struct`s.
- `TypeWithIndentationAndAutoClosingCommand` overrides `GetEditOperations` (two tracked operations) and
  `ComputeCursorState`; Monaco's `throw new Error('There should be two inverse edit operations!')` →
  `throw new InvalidOperationException("There should be two inverse edit operations!")`. `range1.plusRange(range2)` →
  `range1.Plus(range2)`.
- `model.getLineFirstNonWhitespaceColumn` → `model.GetLineFirstNonWhitespaceColumn`; `startLine.substring(a, b)` →
  `Substring(a, b - a)`.
- Remove the `AutoIndentOperation` cut-point comment of `TypeWithInterceptors`.

## Tests

Port from `cursor.test.ts`, suite `Editor Controller`:

- [ ] `Type honors decreaseIndentPattern` (3826)
- [ ] `Enter honors indentNextLinePattern 2` (3889; it types `console.log();` between two Enters)
- [ ] `type honors indentation rules: ruby keywords` (4503)
- [ ] `Auto indent on type: increaseIndentPattern has higher priority than decreaseIndent when inheriting` (4527)
- [ ] `type honors users indentation adjustment` (4547)
- [ ] `bug 29972: if a line is line comment, open bracket should not indent next line` (4568)
- [ ] `issue #122714: tabSize=1 prevent typing a string matching decreaseIndentPattern in an empty file` (4747)
- [ ] `issue #338693: undo after multi-character autoclose with auto-indentation first removes only the auto-closed characters` (5463)
- [ ] Not here: the `Auto Indent On Type - …` suites of `contrib/indentation/test/browser/indentation.test.ts` exercise
      this code too, but they need the bundled language configurations and the `Indentation` feature helpers: step E
      ports them.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `cursorTypeEditOperations.ts` (+ auto-indent on type); tests table `cursor.test.ts`
      (`Editor Controller` +8)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Electric characters (P5-46). Auto-indent on paste and the `editor.action.reindentlines` commands (step E).

## Suggested commit message

`feat(view): re-indent the line while typing`
