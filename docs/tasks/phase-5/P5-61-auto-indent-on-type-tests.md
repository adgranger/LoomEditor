# P5-61 — Auto indent on type: the language suites of `indentation.test.ts`

- **Status:** done
- **Depends on:** P5-60
- **Size:** M (tests only)

## Goal

Port the "Auto Indent On Type" suites of `indentation.test.ts` (38 tests: TypeScript, Ruby, PHP, Visual Basic). They
type text and Enter in an editor whose language has indentation and on-enter rules. They exercise **step D's typing
code** (on-enter actions, electric characters, auto-indent on type in `Cursors/`), not the Indentation feature: they
live in this file upstream, so they are ported here, after both. No production code is expected.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/indentation/test/browser/indentation.test.ts` | 823–1406 | suite `Auto Indent On Type - TypeScript/JavaScript`: setup and its 11 tests (not the 11 `test.skip`) |
| same | 1408–1475 | suite `Auto Indent On Type - Ruby` (1 test, 1 skip) |
| same | 1477–1527 | suite `Auto Indent On Type - PHP` (1 test) |
| same | 1736–2378 | suite `Auto Indent On Type - Visual Basic` (25 tests + the placeholder) |

## Files to create or change

- `tests/LoomEditor.View.Tests/Features/Indentation/AutoIndentOnTypeTests.TypeScript.cs`, `.Ruby.cs`, `.Php.cs`,
  `.VisualBasic.cs` — create (one partial class per suite, ~400 lines max per file)
- `src/LoomEditor.View/Cursors/**` — change **only** to fix a porting bug a test exposes; if a test fails because
  step D's behavior differs from Monaco and the fix is not a small porting bug, stop and report it

## Design decisions (already made — do not change)

- Setup of every suite, as in P5-54: a `TestEditorServices` (P5-37) with `RegisterLanguage` and P5-16's
  `TestLanguages.RegisterLanguageConfiguration(registry, TestLanguages.X)`; models from `services.CreateTextModel(text, TestLanguages.X)`;
  `TestCodeEditor.WithTestCodeEditor(model, …, new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { AutoIndent = … }))`
  for `autoIndent: 'full'` / `'advanced'`. PHP's test uses `TestLanguages.RegisterTokenizationSupport` after creating the model.
- `viewModel.type(text)` → `editor.ViewModel.Type(text)`; `viewModel.type('\n', 'keyboard')` → `Type("\n", "keyboard")`;
  `editor.setSelection` → `editor.ViewModel.SetSelections("test", [selection])`.
- **Not ported** (reasons in the PORTING tests table): the 26 `test.skip` of the file (they fail in Monaco too), and the
  7 placeholder tests `temp issue because there should be at least one passing test in a suite` (`assert.ok(true)`)
  of the suites `Indent With Tab`, `Auto Indent On Paste - Go`, `Auto Indent On Type - CPP`, `- HTML`,
  `- Visual Basic`, `- Latex`, `- Lua`. The suites with only skipped tests and a placeholder (`Indent With Tab`, `Go`,
  `CPP`, `HTML`, `Latex`, `Lua`) are not created.

## Tests

Port, `Auto Indent On Type - TypeScript/JavaScript` (11):

- [x] `issue #208215: indent after arrow function`, `issue #208215: indent after arrow function 2`, `issue #116843: indent after arrow function`
- [x] `issue #29755: do not add indentation on enter if indentation is already valid`, `issue #36090`, `issue #115304: indent block comment onEnter`
- [x] `issue #43244: indent when lambda arrow function is detected, outdent when end is reached`, `issue #43244: incorrect indentation after if/for/while without braces`
- [x] `issue #208232: incorrect indentation inside of comments`, `issue #209802: allman style braces in JavaScript`, `issue #225916: surrounding with brackets works after arrow function`

Port, `Auto Indent On Type - Ruby` (1) and `- PHP` (1):

- [x] `issue #198350: in or when incorrectly match non keywords for Ruby`
- [x] `issue #199050: should not indent after { detected in a string`

Port, `Auto Indent On Type - Visual Basic` (25), every name prefixed by `issue #118932: `:

- [x] `no indentation in visual basic files`, `indent after Module declaration`, `indent after Sub declaration`, `dedent on End Sub`, `dedent on End Module`
- [x] `indent after Function declaration`, `dedent on End Function`, `indent after If Then`, `indent after ElseIf Then`, `dedent and indent on Else`
- [x] `indent after While`, `dedent on End While`, `indent after For`, `dedent on Next`, `indent after Do`, `dedent on Loop`
- [x] `indent after Select Case`, `dedent on End Select`, `indent after Try`, `dedent and indent on Catch`, `dedent and indent on Finally`, `dedent on End Try`
- [x] `indent after Class`, `dedent on End Class`, `full program indentation flow`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: tests table `indentation.test.ts` 52 / 85 (not ported: 26 `test.skip`, 7 placeholders)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The skipped upstream tests; any new typing behavior.

## Suggested commit message

`test(view): port the auto indent on type tests`
