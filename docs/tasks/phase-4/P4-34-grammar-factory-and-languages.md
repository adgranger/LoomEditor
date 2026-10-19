# P4-34 — Languages, grammar definitions, `TMScopeRegistry` and `TMGrammarFactory`

- **Status:** todo
- **Depends on:** P4-33
- **Size:** L — split: `LanguageDefinition*.cs`, `LanguageRegistry.cs`, `TMScopeRegistry.cs`, `TMGrammarFactory.cs`, `RawGrammarCache.cs`

## Goal

From a list of language definitions (`language.json` files), LoomEditor knows each language (id, extensions, file
names) and can create the `ITokenizationSupport` of a language: grammar with its embedded languages, injections,
token types and bracket selectors, as VS Code's TextMate service does.

## Sources

| File | Lines | What to port |
|---|---|---|
| vscode: `src/vs/workbench/services/textMate/common/TMScopeRegistry.ts` | 10–59 | `IValidGrammarDefinition`, `IValidTokenTypeMap`, `IValidEmbeddedLanguagesMap`, `TMScopeRegistry` |
| vscode: `src/vs/workbench/services/textMate/common/TMGrammarFactory.ts` | 11–169 | `ITMGrammarFactoryHost`, `ICreateGrammarResult`, `missingTMGrammarErrorMessage`, `TMGrammarFactory` (constructor with `loadGrammar` and `getInjections`, `has`, `setTheme`, `getColorMap`, `createGrammar`) |
| vscode: `src/vs/workbench/services/textMate/common/TMGrammars.ts` | 18–27, 56–85 | `ITMSyntaxExtensionPoint` (the fields of `language.json`), the `tokenTypes` values, the bracket-scope defaults |
| vscode: `src/vs/workbench/services/textMate/browser/textMateTokenizationFeatureImpl.ts` | 146–214, 287–340 | `_validateGrammarDefinition` (embedded languages → encoded ids, token types switch, bracket defaults `['*']` / `[]`), `_createTokenizationSupport` (the chain: grammar factory → `TextMateTokenizationSupport` → `TokenizationSupportWithLineLimit`) |

## Files to create or change

- `src/LoomEditor.TextMate/Tokenization/LanguageDefinition.cs` (+ `LanguageDefinitionReader.cs`, `JsonNode` reader of `language.json`) — create
- `src/LoomEditor.TextMate/Tokenization/LanguageRegistry.cs` — create (ids ↔ `LanguageIdCodec`, extension / file name lookup, `plaintext` always present)
- `src/LoomEditor.TextMate/Tokenization/TMScopeRegistry.cs`, `TMGrammarFactory.cs`, `RawGrammarCache.cs`, `TextMateTokenizationFactory.cs` (the `_createTokenizationSupport` chain) — create
- `tests/LoomEditor.TextMate.Tests/Tokenization/LanguageRegistryTests.cs`, `TMGrammarFactoryTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0008 decision 6: `language.json` fields are `id`, `aliases`, `extensions`, `filenames`, `scopeName`, `path`
  (relative to the language folder), `embeddedLanguages`, `tokenTypes`, `injectTo`, `balancedBracketScopes`,
  `unbalancedBracketScopes`. A language folder may also declare extra grammars (injections, grammars only included by
  others) in an array `grammars` with the same fields (no `id`).
- Files are read through a `Func<string, string?> readFile` (resource name → text), so tests use in-memory files and
  P4-35 uses embedded resources (P4-01 findings).
- `RawGrammarCache` parses each grammar file **once** and is thread-safe (`ConcurrentDictionary<string, Lazy<RawGrammar?>>`):
  raw grammars are shared read-only between the UI thread and the worker (ADR 0008 decision 5); every `Grammar` clones
  what it gets (trap 7).
- `TMGrammarFactory` is single-threaded (one per thread) and `IDisposable` (disposes its `Registry`).
- Errors (`missingTMGrammarErrorMessage`, a broken grammar) are `Debug.WriteLine`d and give no support (plain text),
  like VS Code logs them.

## Tests

No upstream unit tests. New:

- [ ] `LanguageRegistry_ByExtensionAndFileName`
- [ ] `ReadLanguageJson_AllFields`
- [ ] `CreateGrammar_InjectionIsAppliedToTargetScope` (two in-memory grammars, `injectTo`)
- [ ] `CreateGrammar_EmbeddedLanguagesAreEncoded` (an HTML-like grammar embedding a JS-like one)
- [ ] `CreateTokenizationSupport_UnknownLanguage_ReturnsNull`
- [ ] `RawGrammarCache_ParsesOnce_FromTwoThreads`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated; "Behavior differences": synchronous factory, `language.json` instead of extension manifests
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The real grammar files (P4-35), the worker (P4-37).

## Suggested commit message

`feat(textmate): register languages and create grammars from definitions`
