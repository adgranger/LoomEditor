# P4-42 — `SyntaxHighlighter`: grammars, worker and theme for one editor

- **Status:** todo
- **Depends on:** P4-41
- **Size:** M

## Goal

Each `EditorContext` owns a `SyntaxHighlighter` that joins everything for its model: the built-in languages, the
editor's `TokenizationRegistry`, the UI-thread grammar factory, the background worker, and the theme (token rules →
vscode-textmate theme + color map). Setting a language or a theme on it re-tokenizes the model.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/workbench/services/textMate/browser/textMateTokenizationFeatureImpl.ts` | 260–285, 342–362, 465–471 | `_getOrCreateGrammarFactory`, `_updateTheme` (raw theme `{ name, settings: tokenColors }`, `setTheme(theme, tokenColorMap)` on the factory **and** the worker, `TokenizationRegistry.setColorMap`), `toColorMap` |
| vscode: same file | 115–144 | `_handleGrammarsExtPoint` — only the idea: a language gets its support when it is first needed (no extension points) |

## Files to create or change

- `src/LoomEditor.View/Highlighting/SyntaxHighlighter.cs` — create (internal)
- `src/LoomEditor.View/Highlighting/ThemeToTextMate.cs` — create (`EditorTheme` → `RawTheme` + color maps)
- `src/LoomEditor.View/Features/EditorContext.cs` — change: the internal constructor takes an `IUiDispatcher?`; the
  model is created with the editor's `TokenizationRegistry`; `Highlighter` property (internal); dispose it
- `tests/LoomEditor.View.Tests/Highlighting/TestHelpers/ManualUiDispatcher.cs` — create (same as the TextMate tests' one)
- `tests/LoomEditor.View.Tests/Highlighting/SyntaxHighlighterTests.cs` — create
- `tests/LoomEditor.View.Tests/LoomEditor.View.Tests.csproj` — the tests need `onig.dll` (P4-02 already copies it through the project reference; check)

## Design decisions (already made — do not change)

- ADR 0008 decisions 5 and 10. `SetLanguage(string languageId)`: if the registry has no support for that language,
  create it with the UI-thread `TMGrammarFactory` and `Register` it, then `Model.Tokenization.SetLanguageId`.
  Unknown ids fall back to `plaintext`.
- `SetTheme(EditorTheme theme)`: build the raw theme from `theme.TokenColors` (rule 0 included, P4-40) and the color
  map; call `setTheme` on the UI factory and send the theme to the worker; then `TokenizationRegistry.SetColorMap`
  (Core's ARGB `uint` list), which resets the model's tokenization (P4-29).
- With a dispatcher: the worker is used (`ThreadedBackgroundTokenizerFactory`, P4-38). Without one (`null`, the public
  `EditorContext()` constructor and unit tests): no worker; the model uses `DefaultBackgroundTokenizer` with a scheduler
  that does nothing, so only the visible lines are tokenized (heuristically) — say so in the XML doc.
- `SyntaxHighlighter` is `IDisposable`: it stops the worker and disposes the grammar factory.

## Tests

New:

- [ ] `SetLanguage_Csharp_ModelGetsCsharpTokens` (no dispatcher, `ForceTokenization`)
- [ ] `SetLanguage_Unknown_FallsBackToPlainText`
- [ ] `SetTheme_ChangesTokenColors` (same text, Dark Modern vs Light Modern: different foreground ids/colors)
- [ ] `WithDispatcher_WorkerTokensArriveOnDispatch` (`ManualUiDispatcher`, wait for the worker with a timeout, then `RunAll`)
- [ ] `Dispose_StopsTheWorker`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The view model and the renderer (P4-43, P4-44), the control's properties (P4-45).

## Suggested commit message

`feat(view): add the syntax highlighter of an editor`
