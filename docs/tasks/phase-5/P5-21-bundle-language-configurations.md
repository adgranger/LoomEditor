# P5-21 — Bundle the ten language configurations and register them

- **Status:** todo
- **Depends on:** P5-20
- **Size:** M (data files, a few lines of code, tests)

## Goal

The ten built-in languages ship VS Code's `language-configuration.json` unchanged, next to their grammar;
`language.json` names the file; `SyntaxHighlighter.SetLanguage` registers the configuration of the language (and of
the languages its grammar embeds) in the editor's `LanguageConfigurationRegistry`. A test compiles every regex of
every bundled file.

## Sources

Copy **byte for byte** from `microsoft/vscode` at the pinned commit (raw URL, phase-5/README.md section 1):

| Language ids | Upstream file | Copied to (`src/LoomEditor.TextMate/Grammars/…`) |
|---|---|---|
| `csharp` | `extensions/csharp/language-configuration.json` | `csharp/language-configuration.json` |
| `typescript` | `extensions/typescript-basics/language-configuration.json` | `typescript/language-configuration.json` |
| `javascript` | `extensions/javascript/javascript-language-configuration.json` | `javascript/javascript-language-configuration.json` |
| `json`, `jsonc` | `extensions/json/language-configuration.json` (both ids, `extensions/json/package.json` 48, 76) | `json/language-configuration.json` |
| `html` | `extensions/html/language-configuration.json` | `html/language-configuration.json` |
| `css` | `extensions/css/language-configuration.json` | `css/language-configuration.json` |
| `xml` | `extensions/xml/xml.language-configuration.json` | `xml/xml.language-configuration.json` |
| `markdown` | `extensions/markdown-basics/language-configuration.json` | `markdown/language-configuration.json` |
| `python` | `extensions/python/language-configuration.json` | `python/language-configuration.json` |
| `yaml` | `extensions/yaml/language-configuration.json` | `yaml/language-configuration.json` |

| File | Lines | What to port |
|---|---|---|
| `src/vs/workbench/contrib/codeEditor/common/languageConfigurationExtensionPoint.ts` | 104–131, 438–441 | the idea only: a language gets its configuration when it is first used; `_handleConfig` registers with priority **50** |

## Files to create or change

- `src/LoomEditor.TextMate/Grammars/<folder>/…language-configuration.json` — the eleven-id table above (ten files)
- `src/LoomEditor.TextMate/Grammars/*/language.json`, `json/jsonc.language.json` — add `"configuration": "<file name>"`
- `src/LoomEditor.TextMate/Tokenization/LanguageDefinition.cs` — `LanguageDefinition` gets `string? ConfigurationLocation` (resource name: folder + `configuration`, like `path`)
- `src/LoomEditor.TextMate/Tokenization/LanguageDefinitionReader.cs` — read `configuration`
- `src/LoomEditor.TextMate/Tokenization/LanguageRegistry.cs` — add `LanguageDefinition? GetLanguageDefinition(string languageId)` if missing
- `src/LoomEditor.View/Highlighting/SyntaxHighlighter.cs` — the constructor takes the `LanguageConfigurationRegistry`; `SetLanguage` registers configurations
- `src/LoomEditor.View/Features/EditorContext.cs` — pass the model's registry to the highlighter
- `NOTICE.md` — under "Monaco Editor / Visual Studio Code": the `language-configuration.json` files of the ten
  languages come unchanged from `extensions/<language>/` of `microsoft/vscode` (MIT)
- `docs/how-to/add-a-language.md` — step 2: copy the extension's language configuration file and name it in `language.json` (`configuration`)
- `tests/LoomEditor.TextMate.Tests/Grammars/BuiltInLanguagesTests.cs` — `EveryEmbeddedResource_IsListedByALanguage` also lists the configuration files; new tests
- `tests/LoomEditor.View.Tests/Highlighting/SyntaxHighlighterTests.cs` — new tests

## Design decisions (already made — do not change)

- ADR 0009, decisions 1, 5 and 6. The files are embedded by the existing `Grammars\**\*.json` item (resource
  `Grammars/<folder>/<file>`); `.gitattributes` already keeps them byte for byte. No file is edited.
- `language.json` field `configuration`: a path relative to the language folder (VS Code's
  `"configuration": "./language-configuration.json"` without `./`).
- `SetLanguage(languageId)`, after the fallback to `plaintext`: for `languageId` and for every language id that its
  grammar's `EmbeddedLanguages` maps to (HTML → css, javascript, python; Markdown → its fenced languages), if that
  language is a built-in one with a `ConfigurationLocation` and the highlighter has not registered it yet:
  `LanguageConfigurationReader.Read(BuiltInLanguages.ReadFile(location), id)` and
  `registry.Register(id, configuration, 50)`. The registrations are disposed with the highlighter. Embedded ids
  without a bundled configuration (`jsx-tags`) keep the empty configuration.
- The configuration is parsed per editor (no static cache): ten small files.

## Tests

- [ ] New (TextMate): `EveryBundledRegex_Compiles` (theory over the eleven language ids: `Read` with a warning
  collector gives no warning — an invalid or untranslated regex is a warning, P5-20)
- [ ] New (TextMate): `EveryLanguageWithAGrammar_HasAConfiguration`
- [ ] New (TextMate): `Markdown_WordPattern_MatchesUnicodeWords` (`héllo_wörld` is one word)
- [ ] New (TextMate): `Csharp_Configuration_HasCommentsBracketsAndRegionMarkers`
- [ ] Update: `EveryEmbeddedResource_IsListedByALanguage`
- [ ] New (View): `SetLanguage_RegistersTheLanguageConfiguration` (`csharp`: line comment `//`)
- [ ] New (View): `SetLanguage_Html_RegistersTheEmbeddedLanguagesConfigurations` (`css`, `javascript`)
- [ ] New (View): `SetLanguage_Twice_RegistersOnce`
- [ ] New (View): `Dispose_UnregistersTheConfigurations`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings (Native AOT: JSON and regexes)
- [ ] `docs/PORTING.md`: the "Cut points for phases 4–5" section says the language configuration now exists (`TextModel.LanguageConfigurationRegistry`); behavior difference: `jsx-tags` and the other non-bundled languages have no configuration
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Using the configuration while typing (step D), brackets (step C), comments and indentation commands (step E),
folding (step G).

## Suggested commit message

`feat(textmate): bundle the language configurations of the built-in languages`
