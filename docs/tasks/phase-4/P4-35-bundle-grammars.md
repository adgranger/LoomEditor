# P4-35 — Bundle the ten built-in grammars

- **Status:** done
- **Depends on:** P4-34
- **Size:** M (data files + one list + tests; little code)

## Goal

LoomEditor ships the grammars of C#, TypeScript, JavaScript, JSON (and JSON with comments), HTML, CSS, XML, Markdown,
Python and YAML as embedded resources, each with its `language.json`, listed in `BuiltInLanguages.cs`, with their
licenses in `NOTICE.md`.

## Sources

Copy **verbatim** from `microsoft/vscode` at the pinned commit (raw URL, phase-4/README.md section 1). Check every name
in the folder listing before copying (`https://api.github.com/repos/microsoft/vscode/contents/extensions/<ext>/syntaxes?ref=0c9c0a4295a9222e12a605537738719455ae3056`).

| Language id | Files (`extensions/…/syntaxes/`) | scopeName | Notes from `extensions/<ext>/package.json` |
|---|---|---|---|
| `csharp` | `csharp/…/csharp.tmLanguage.json` | `source.cs` | extensions `.cs .csx .cake`; `tokenTypes` `meta.interpolation: other`; `unbalancedBracketScopes` (5 scopes) |
| `typescript` | `typescript-basics/…/TypeScript.tmLanguage.json`, the JSDoc injection for TS | `source.ts` | `.ts .cts .mts`; `tokenTypes` and `unbalancedBracketScopes` from the manifest; injection `injectTo [source.ts, source.tsx]` |
| `javascript` | `javascript/…/JavaScript.tmLanguage.json`, `javascript/…/Regular Expressions (JavaScript).tmLanguage` (plist), the JSDoc injection for JS (in `typescript-basics`) | `source.js`, `source.js.regexp` | `.js .es6 .mjs .cjs .pac`, file name `jakefile`; `embeddedLanguages` only for the JSX tag scopes (keep them) |
| `json`, `jsonc` | `json/…/JSON.tmLanguage.json`, `JSONC.tmLanguage.json` | `source.json`, `source.json.comments` | the extensions and file names of both languages (`tsconfig.json`, `jsconfig.json` are `jsonc`) |
| `html` | `html/…/html.tmLanguage.json`, `html-derivative.tmLanguage.json` | `text.html.basic`; language `html` uses **`text.html.derivative`** | `embeddedLanguages` {`text.html`: html, `source.css`: css, `source.js`: javascript, `source.python`: python}; drop `source.smarty` (not bundled); `tokenTypes` |
| `css` | `css/…/css.tmLanguage.json` | `source.css` | `.css`; `tokenTypes` |
| `xml` | `xml/…/xml.tmLanguage.json` | `text.xml` | the extensions of the manifest (`.xml .xsd .csproj .props .targets .xaml .svg .slnx` …) |
| `markdown` | `markdown-basics/…/markdown.tmLanguage.json` | `text.html.markdown` | `embeddedLanguages` limited to the bundled languages; no `markdown-math` |
| `python` | `python/…/MagicPython.tmLanguage.json`, `MagicRegExp.tmLanguage.json` | `source.python`, `source.regexp.python` | the extensions and file names of the manifest |
| `yaml` | `yaml/…/yaml.tmLanguage.json`, `yaml-1.0`, `yaml-1.1`, `yaml-1.2`, `yaml-1.3`, `yaml-embedded` | `source.yaml` (+ the others) | `.yaml .yml …`; `unbalancedBracketScopes` |

Licenses: `extensions/<ext>/cgmanifest.json` (repository, commit, license). HTML and the JavaScript regex grammar
come from TextMate bundles under the **TextMate Bundle License**: copy its full text into `NOTICE.md`.

## Files to create or change

- `src/LoomEditor.TextMate/Grammars/<language>/` — the grammar files + `language.json` (one folder per row)
- `src/LoomEditor.TextMate/Grammars/BuiltInLanguages.cs` — create (the list, and the embedded-resource `readFile`)
- `src/LoomEditor.TextMate/LoomEditor.TextMate.csproj` — `EmbeddedResource` items with `LogicalName`s (P4-01 findings)
- `.gitattributes` — `src/LoomEditor.TextMate/Grammars/** -text` (files stay byte for byte)
- `NOTICE.md` — one entry per upstream grammar repository
- `tests/LoomEditor.TextMate.Tests/Grammars/BuiltInLanguagesTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0008 decisions 6 and 7. The `language.json` files are written by hand from the manifests; they list only
  bundled languages in `embeddedLanguages`.
- `BuiltInLanguages.All` is an `IReadOnlyList<LanguageDefinition>` built once (`static readonly`, immutable).
- Language ids are VS Code's.

## Tests

- [x] New: `EveryBuiltInGrammar_LoadsAndTokenizesAShortSample` (theory over the ten language ids; a two-line sample in the test)
- [x] New: `Html_ScriptTag_IsTokenizedAsJavaScript` (embedded language id of the token inside `<script>`)
- [x] New: `Markdown_FencedCsharp_IsTokenizedAsCsharp`
- [x] New: `EveryEmbeddedResource_IsListedByALanguage` (no orphan file)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings
- [x] `docs/how-to/add-a-language.md` updated to the real steps (remove "Status: phase 4 — not available yet" only in P4-50)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Golden tests (P4-47, P4-48), language configuration (phase 5).

## Suggested commit message

`feat(textmate): bundle grammars for ten languages`
