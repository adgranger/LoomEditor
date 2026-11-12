# P18-01 — Bundle 17 more grammars (C, C++, Java, Go, Rust, PHP, Ruby, SQL, shell, PowerShell, SCSS, Less, Swift, Dart, Dockerfile, ini, diff)

- **Status:** done
- **Depends on:** — (Lua, PR #29, is the model: read `src/LoomEditor.TextMate/Grammars/lua/` and `docs/how-to/add-a-language.md` first)
- **Size:** M (data files, one list, tests; almost no code). Not a Monaco port: no `// Ported from` header on JSON files.

## Goal

LoomEditor ships the grammars of the languages below as embedded resources, each with its `language.json` and its language
configuration, listed in `BuiltInLanguages.cs`, with a golden test each and their licenses in `NOTICE.md`. This is the
"common languages" batch of the built-in extensions of VS Code that LoomEditor did not bundle (phase 4 + Lua).

## Sources

Copy **verbatim** from `microsoft/vscode` at the pinned commit
(`https://raw.githubusercontent.com/microsoft/vscode/0c9c0a4295a9222e12a605537738719455ae3056/extensions/<ext>/…`).
`language.json` is written by hand from `extensions/<ext>/package.json` (ids, aliases, extensions, filenames, `scopeName`,
`configuration`, `path`, `grammars`, `tokenTypes`, `embeddedLanguages` limited to bundled languages). Licenses:
`extensions/<ext>/cgmanifest.json`. Check every file name against the folder listing before copying.

| Folder (`Grammars/…`) | Language ids | Files (`extensions/<ext>/syntaxes/`) | scopeName | Notes |
|---|---|---|---|---|
| `cpp` | `c`, `cpp` | `c.tmLanguage.json`, `cpp.tmLanguage.json`, `cpp.embedded.macro.tmLanguage.json`, `platform.tmLanguage.json` | `source.c`, `source.cpp`, `source.cpp.embedded.macro`, `source.c.platform` | two language files in one folder (like `json/` and `typescript/`): `language.json` (cpp, the other grammars in `grammars`) and `c.language.json`. `c`: `.c .i`; `cpp`: the long list of the manifest (`.h` is C++). **No** `cuda-cpp`. Includes of `source.asm`, `source.x86`, `source.glsl`… are not bundled and resolve to nothing: expected |
| `java` | `java` | `java.tmLanguage.json` | `source.java` | `.java .jav` |
| `go` | `go` | `go.tmLanguage.json` | `source.go` | `.go` |
| `rust` | `rust` | `rust.tmLanguage.json` | `source.rust` | `.rs` |
| `php` | `php` | `html.tmLanguage.json` (the language's grammar), `php.tmLanguage.json` | `text.html.php`, `source.php` | the manifest declares both for `php`: the language `path` is `html.tmLanguage.json`, `php.tmLanguage.json` goes in `grammars`. `embeddedLanguages`: `text.html` html, `source.php` php, `source.sql` sql, `text.xml` xml, `source.js` javascript, `source.json` json, `source.css` css (all bundled once this card is done) |
| `ruby` | `ruby` | `ruby.tmLanguage.json` | `source.ruby` | extensions and file names of the manifest (`Rakefile`, `Gemfile`, …) |
| `sql` | `sql` | `sql.tmLanguage.json` | `source.sql` | `.sql .dsql` |
| `shellscript` | `shellscript` | `shell-unix-bash.tmLanguage.json` | `source.shell` | extensions and file names of the manifest; aliases `shell`, `sh`, `bash`… |
| `powershell` | `powershell` | `powershell.tmLanguage.json` | `source.powershell` | `.ps1 .psm1 .psd1 .pssc .psrc` |
| `scss` | `scss` | `scss.tmLanguage.json`, `sassdoc.tmLanguage.json` | `source.css.scss`, `source.sassdoc` | sassdoc goes in `grammars`; `source.css` is the bundled CSS |
| `less` | `less` | `less.tmLanguage.json` | `source.css.less` | `.less` |
| `swift` | `swift` | `swift.tmLanguage.json` | `source.swift` | `.swift` |
| `dart` | `dart` | `dart.tmLanguage.json` | `source.dart` | `.dart` |
| `docker` | `dockerfile` | `docker.tmLanguage.json` | `source.dockerfile` | `.dockerfile .containerfile`, file names `Dockerfile`, `Containerfile` |
| `ini` | `ini` | `ini.tmLanguage.json` | `source.ini` | `.ini` only (the `properties` language of the same extension is out of scope). The configuration is `ini.language-configuration.json`, copied unchanged |
| `diff` | `diff` | `diff.tmLanguage.json` | `source.diff` | `.diff .patch .rej` |

If a manifest has a `firstLine` (php, ruby, shell, powershell), put it in `language.json` only if `LanguageDefinitionReader`
already reads it; otherwise leave it out and say so in the result (no reader change in this card).

## Files to create or change

- `src/LoomEditor.TextMate/Grammars/<folder>/` — the files above + `language-configuration.json` (unchanged copy) + `language.json`
- `src/LoomEditor.TextMate/Grammars/BuiltInLanguages.cs` — one `Sources` line per language file (update the comment about folders that hold several languages)
- `src/LoomEditor.TextMate/Grammars/markdown/language.json` — add to `embeddedLanguages` the fenced-code scopes of the new languages (`meta.embedded.block.<id>` → id, as the VS Code manifest does for those ids)
- `NOTICE.md` — one row per upstream grammar repository (see each `cgmanifest.json`); add a license text only if it is not already there
- `tests/LoomEditor.TextMate.Tests/Golden/<id>/sample.<ext>` — one sample per language id (20–40 lines: comments, strings, numbers, keywords, one language-specific construct); run `eng/golden` to generate `sample.<ext>.tokens.json` and **review them**
- `tests/LoomEditor.TextMate.Tests/Golden/GoldenFile.cs` — the `Languages` list
- `tests/LoomEditor.TextMate.Tests/Grammars/BuiltInLanguagesTests.cs` — the `Samples` rows and the language count
- `samples/LoomEditor.Gallery/HighlightingSamples.cs` — a short sample per new language (the Gallery language picker lists `BuiltInLanguages.Ids`)
- `docs/PORTING.md` (the row of `extensions/…/syntaxes`) and `docs/how-to/add-a-language.md` if a step changed
- `CHANGELOG.md` — a line under `## [Unreleased]` → `### Added`

## Design decisions (already made — do not change)

- Same rules as phase 4 and the Lua grammar: grammars copied byte for byte (`.gitattributes` marks them `-text`), language ids are VS Code's, only bundled languages in `embeddedLanguages`, no reader change.
- Not in this batch: `cuda-cpp`, `properties`, `objective-c`, `r`, `perl`, `fsharp`, `vb`, `razor`, `clojure`, `coffeescript`, `groovy`, `julia`, `handlebars`, `pug`, `hlsl`, `shaderlab`, `latex`, `restructuredtext`, `make`, `bat`, `dotenv`.
- The benchmark set (`GrammarBenchmarks`) is not changed.
- Oniguruma is 6.9.10 and vscode-oniguruma 6.9.8: if a golden test fails on a nested negated class `[a[^b]]`, apply the rewrite documented in `docs/PORTING.md` ("Behavior differences") for that rule only, and list it in the result. Any other divergence between the golden file and our tokens: **stop and ask**, do not tune the grammar.
- Embedded resources are picked up by the existing globs; do not touch the `.csproj`.

## Tests

- [x] New: golden tests, one per language id (`c`, `cpp`, `java`, `go`, `rust`, `php`, `ruby`, `sql`, `shellscript`, `powershell`, `scss`, `less`, `swift`, `dart`, `dockerfile`, `ini`, `diff`), tokens made by the real vscode-textmate
- [x] New: `EveryBuiltInGrammar_LoadsAndTokenizesAShortSample` rows for the 17 new ids; the language count in `EveryLanguageWithAGrammar_HasAConfiguration` updated
- [x] New: `Markdown_FencedCpp_IsTokenizedAsCpp` (like the existing fenced C# test) and `Php_HtmlOutsideTheTags_IsTokenizedAsHtml`
- [x] New: `GetLanguageIdByFileName_NewExtensions_ReturnsTheLanguage` (theory: `a.cpp` and `a.h` → `cpp`, `a.c` → `c`, `Dockerfile`, `Rakefile`, `a.sh`, `a.ps1`, `a.patch`, …)
- [x] Existing `EveryEmbeddedResource_IsListedByALanguage` still green (no orphan file)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` and `dotnet test --solution LoomEditor.Portable.slnf -c Release` — green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings
- [x] The size added to `LoomEditor.TextMate.dll` (embedded grammars) is written in the card's result
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Languages not listed above, Monaco's Monarch-only languages (Kotlin, Scala, GraphQL, Protobuf, …: no VS Code grammar),
language detection by first line, any grammar fix.

## Suggested commit message

`feat(textmate): bundle grammars for C, C++, Java, Go, Rust, PHP, Ruby, SQL, shell, PowerShell, SCSS, Less, Swift, Dart, Dockerfile, ini and diff`
