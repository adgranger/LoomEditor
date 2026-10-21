# P5-54 — Auto indent on paste

- **Status:** done
- **Depends on:** P5-53
- **Size:** M

## Goal

Port `AutoIndentOnPaste`: after a keyboard paste, the first pasted line gets the indentation the language rules
want, and the following lines move by the same amount. It is off by default, as in VS Code
(`editor.autoIndentOnPaste: false`). Port the `Auto Indent On Paste - TypeScript/JavaScript` suite.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/indentation/browser/indentation.ts` | 322–367 | `AutoIndentOnPasteCommand` |
| same | 369–568 | `AutoIndentOnPaste`: `update`, `trigger`, `rangeContainsOnlyWhitespaceCharacters`, `dispose` |
| same | 570–576 | `isStartOrEndInString` |
| `common/tokens/lineTokens.ts` | 413–423 | `getStandardTokenTypeAtPosition` |
| `src/vs/editor/browser/widget/codeEditor/codeEditorWidget.ts` | 1208–1222 | `_paste`: the paste range and `onDidPaste` for `source === 'keyboard'` |
| `common/config/editorOptions.ts` | 6174–6181 | `autoIndentOnPaste` (default `false`), `autoIndentOnPasteWithinString` (default `true`) |
| `contrib/indentation/test/browser/indentation.test.ts` | 396–821 | suite `Auto Indent On Paste - TypeScript/JavaScript`: setup and its 6 tests (not the 5 `test.skip`) |

## Files to create or change

- `src/LoomEditor.View/Features/Indentation/AutoIndentOnPaste.cs`, `AutoIndentOnPasteCommand.cs` — create
- `src/LoomEditor.View/Features/Indentation/EditorIndentationOptions.cs` — create
- `src/LoomEditor.View/Features/Indentation/IndentationFeature.cs` — change: create and dispose an `AutoIndentOnPaste`
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `internal event EventHandler<Range>? DidPaste` and `internal void NotifyDidPaste(Range range)`
- `src/LoomEditor.View/Controller/ClipboardCommands.cs` — change: `PasteAsync` raises it (port of `_paste` 1208–1222)
- `src/LoomEditor.Core/Tokens/LineTokens.cs` — change: `internal static StandardTokenType? GetStandardTokenTypeAtPosition(TextModel model, Position position)` (Monaco's module function; `undefined` → `null`)
- `tests/LoomEditor.View.Tests/Features/Indentation/AutoIndentOnPasteTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed record EditorIndentationOptions(bool AutoIndentOnPaste = false, bool AutoIndentOnPasteWithinString = true)`
  with `Default`; `IndentationFeature.Options` is `internal` and settable, like `CommentFeature.Options`.
- `internal sealed class AutoIndentOnPaste(ViewModel viewModel, Func<EditorIndentationOptions> options, LanguageConfigurationRegistry languageConfigurations) : IDisposable`
  subscribes to `ViewModel.DidPaste` once. Monaco's `update()` (re-subscribe on configuration / model / language
  change) becomes a check at the start of the handler: do nothing unless `options().AutoIndentOnPaste` and
  `viewModel.CursorConfig.AutoIndent >= EditorAutoIndentStrategy.Full`. `Trigger(Range range)` is `internal` and does
  not check them (the tests call it directly, as Monaco's do).
- The paste event is raised **outside** `ViewModel.Paste`, by `ClipboardCommands.PasteAsync` (Monaco raises it in the
  widget, not in the view model). Tests that call `viewModel.Paste(…, "keyboard")` therefore never trigger it twice.
- From step B: `AutoIndent.GetGoodIndentForLine(autoIndent, IVirtualModel, languageId, lineNumber, IIndentConverter, registry)`
  → `string?` (P5-17), `AutoIndent.GetIndentMetadata(model, lineNumber, registry)` → `int?` (P5-18),
  `IndentConsts` (P5-12), `IIndentConverter` (P5-17: `ShiftIndent`, `UnshiftIndent`), the flat `IVirtualModel`
  (P5-16: `GetLineTokens`, `GetLanguageId`, `GetLanguageIdAtPosition`, `ForceTokenization`, `GetLineContent`).
  The virtual model of lines 469–488 is a private nested class of `AutoIndentOnPaste` that delegates to the model and
  overrides `GetLineContent(firstLineNumber)`; `ForceTokenization` delegates too. The indent converter is a private
  nested class over `ShiftCommand.ShiftIndent` / `UnshiftIndent`.
- The registry is the model's (`viewModel.Model.LanguageConfigurationRegistry`, as in P5-51).
- `/\S/.test(text)` → "contains a char that is not JavaScript whitespace": `!Strings.IsJavaScriptWhitespaceOnly(text)`
  (exists, P3-31). `content.trim().length === 0` → the same helper.
- `editor.executeCommand('autoIndentOnPaste', cmd)` → `viewModel.ExecuteCommand(cmd, "autoIndentOnPaste")` between two
  `PushStackElement` calls.
- **Test helpers already exist**: the helpers of `indentation.test.ts` lines 30–141 are P5-16's `TestLanguages`
  (`TestLanguages.TypeScript`, `RegisterLanguageConfiguration(registry, language)`, `StandardTokenTypeData`,
  `RegisterTokenizationSupport(tokenizationRegistry, codec, tokens, languageId)` with Monaco's shared line counter),
  linked into `LoomEditor.View.Tests` by P5-37. Do not create another copy. The suite `setup` becomes a
  `TestEditorServices` (P5-37) with `RegisterLanguage(TestLanguages.TypeScript)` and the language configuration;
  each test creates its model with `services.CreateTextModel(text, TestLanguages.TypeScript)` and runs
  `TestCodeEditor.WithTestCodeEditor(model, …, new TestCodeEditorOptions(CursorOptions: EditorCursorOptions.Default with { AutoIndent = EditorAutoIndentStrategy.Full }))`.
  Register the tokenization support **after** the model is created, as Monaco does.
- `editor.registerAndInstantiateContribution(AutoIndentOnPaste.ID, AutoIndentOnPaste)` → `using var controller = new AutoIndentOnPaste(editor.ViewModel, () => EditorIndentationOptions.Default, registry)`.

## Tests

Port, suite `Auto Indent On Paste - TypeScript/JavaScript` (6):

- [x] `issue #119225: Do not add extra leading space when pasting JSDoc`
- [x] `issue #167299: Blank line removes indent`
- [x] `issue #29803: do not indent when pasting text with only one line`
- [x] `issue #29753: incorrect indentation after comment`
- [x] `issue #29753: incorrect indentation of header comment`
- [x] `issue #209859: do not do change indentation when pasted inside of a string`
- [x] **Not ported** (`test.skip` upstream, failing in Monaco too): `issue #181065`, `issue #86301`, `issue #85781`,
      `issue #29589`, `issue #201420` — listed in the PORTING tests table
- [x] New: `DidPaste_OptionOff_DoesNotChangeTheText`; `DidPaste_OptionOn_ReindentsThePastedBlock` (through `ClipboardCommands.PasteAsync` with a fake clipboard)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `contrib/indentation/browser/indentation.ts` (✅ except the quick pick, P5-54);
      `codeEditorWidget.ts` `_paste` (`onDidPaste`); tests table `indentation.test.ts` 14 / 85
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`editor.formatOnPaste` (needs a formatter: not ported), the typing suites (P5-61).

## Suggested commit message

`feat(view): auto indent on paste`
