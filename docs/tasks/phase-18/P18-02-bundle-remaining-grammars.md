# P18-02 — Bundle the remaining built-in grammars of VS Code

- **Status:** todo
- **Depends on:** P18-01
- **Size:** M (data files, one list, tests; almost no code). Not a Monaco port: no `// Ported from` header on JSON files.

## Goal

Every language that has a TextMate grammar in the built-in extensions of VS Code at the pinned commit is bundled, except the
ones listed in "Out of scope". Same method, same rules and same checks as P18-01: **read that card first and follow it** (copy
byte for byte, `language.json` by hand from `package.json`, one `Sources` line per language file, golden test per language id,
`NOTICE.md`, Gallery sample, `CHANGELOG.md`).

## Sources

`microsoft/vscode` at `0c9c0a4295a9222e12a605537738719455ae3056`, `extensions/<ext>/…`. Check every file name against the
folder listing; licenses in each `cgmanifest.json`. Where a manifest says a language shares a grammar or a configuration, copy
the file once.

| Folder (`Grammars/…`) | Language ids | Files (`syntaxes/`) | scopeName | Notes |
|---|---|---|---|---|
| `objective-c` | `objective-c`, `objective-cpp` | `objective-c.tmLanguage.json`, `objective-c++.tmLanguage.json` | `source.objc`, `source.objcpp` | `.m`; `.mm`. One shared `language-configuration.json` |
| `cpp` (existing folder) | `cuda-cpp` | `cuda-cpp.tmLanguage.json` | `source.cuda-cpp` | `.cu .cuh`; new `cuda.language.json` in the folder of P18-01; configuration: the folder's own |
| `r` | `r` | `r.tmLanguage.json` | `source.r` | `.R .Rhistory .Rprofile .rt` |
| `perl` | `perl`, `raku` | `perl.tmLanguage.json`, `perl6.tmLanguage.json` | `source.perl`, `source.perl.6` | two configurations: `perl.language-configuration.json`, `perl6.language-configuration.json` |
| `fsharp` | `fsharp` | `fsharp.tmLanguage.json` | `source.fsharp` | `.fs .fsi .fsx .fsscript` |
| `vb` | `vb` | `asp-vb-net.tmLanguage.json` | `source.asp.vb.net` | `.vb .brs .vbs .bas .vba` |
| `razor` | `razor` | `cshtml.tmLanguage.json` | `text.html.cshtml` | `.cshtml .razor`; `embeddedLanguages` limited to bundled languages (csharp, html, css, javascript) |
| `clojure` | `clojure` | `clojure.tmLanguage.json` | `source.clojure` | `.clj .cljs .cljc .cljx .clojure .edn` |
| `coffeescript` | `coffeescript` | `coffeescript.tmLanguage.json` | `source.coffee` | `.coffee .cson .iced` |
| `groovy` | `groovy` | `groovy.tmLanguage.json` | `source.groovy` | `.groovy .gvy .gradle .jenkinsfile .nf`, file name `Jenkinsfile` |
| `julia` | `julia` | `julia.tmLanguage.json` | `source.julia` | `.jl` only (`juliamarkdown` has no grammar: skip) |
| `handlebars` | `handlebars` | `Handlebars.tmLanguage.json` | `text.html.handlebars` | `.handlebars .hbs .hjs` |
| `pug` | `jade` | `pug.tmLanguage.json` | `text.pug` | VS Code's id is `jade` (aliases `Pug`, `Jade`): keep `jade`; `.pug .jade` |
| `hlsl` | `hlsl` | `hlsl.tmLanguage.json` | `source.hlsl` | `.hlsl .hlsli .fx .fxh .vsh .psh .cginc .compute` |
| `shaderlab` | `shaderlab` | `shaderlab.tmLanguage.json` | `source.shaderlab` | `.shader`; its manifest embeds HLSL: keep the scope mapping |
| `latex` | `tex`, `latex`, `bibtex` | `TeX.tmLanguage.json`, `LaTeX.tmLanguage.json`, `Bibtex.tmLanguage.json` | `text.tex`, `text.tex.latex`, `text.bibtex` | `latex-language-configuration.json` shared by `tex` and `latex`; `bibtex` has none in the manifest: write a minimal one (comments `%`? no: copy nothing, use `{ }` brackets and `{ }` auto-closing pairs) and say so in the result. **No** `cpp_embedded_latex` and **no** `markdown_latex_combined` (not user-facing languages) |
| `restructuredtext` | `restructuredtext` | `rst.tmLanguage.json` | `source.rst` | `.rst` |
| `make` | `makefile` | `make.tmLanguage.json` | `source.makefile` | `.mak .mk`, file names `Makefile makefile GNUmakefile OCamlMakefile` |
| `bat` | `bat` | `batchfile.tmLanguage.json` | `source.batchfile` | `.bat .cmd` |
| `dotenv` | `dotenv` | `dotenv.tmLanguage.json` | `source.dotenv` | `.env`, file names `.env .flaskenv user-dirs.dirs` |
| `ini` (existing folder) | `properties` | the `ini.tmLanguage.json` already there (scope `source.ini`) | `source.ini` | new `properties.language.json` + `properties.language-configuration.json`; a second language may use the same grammar file: if the loader or the "no orphan resource" test cannot express that, **stop and ask** |

`firstLine` fields: same rule as P18-01 (only if `LanguageDefinitionReader` already reads them).

## Files to create or change

Same list as P18-01 (grammar folders, `BuiltInLanguages.cs`, `markdown/language.json` fenced-code scopes for the new ids that the
VS Code manifest maps, `NOTICE.md`, golden samples and `GoldenFile.Languages`, `BuiltInLanguagesTests.cs` rows and count,
Gallery samples, `docs/PORTING.md`, `CHANGELOG.md` under `## [Unreleased]` → `### Added`).

## Design decisions (already made — do not change)

- Same as P18-01: no reader change, no grammar fix, language ids are VS Code's, only bundled languages in `embeddedLanguages`,
  the Oniguruma nested-negated-class rewrite only when a golden test needs it and listed in the result.
- If a grammar makes the golden generator or a test take unreasonably long (a single sample over a few seconds), stop and report.

## Tests

Same four kinds as P18-01 for the new ids: golden tests (one sample per id: 20–40 lines), the `Samples` theory rows, a
`GetLanguageIdByFileName` theory (`a.m`, `a.mm`, `a.rs`-style new extensions: `a.pl`, `a.fs`, `a.vb`, `a.cshtml`, `a.hbs`,
`a.pug`, `a.tex`, `a.bib`, `Makefile`, `a.bat`, `.env`, `a.properties`, …) and one fenced-code check in Markdown
(`Markdown_FencedR_IsTokenizedAsR`), plus `Razor_HtmlAndCsharp_AreTokenizedAsEmbeddedLanguages`. `EveryEmbeddedResource_IsListedByALanguage` stays green.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` and `dotnet test --solution LoomEditor.Portable.slnf -c Release` — green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings
- [ ] Size added to `LoomEditor.TextMate.dll` written in the result
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`git-base`, `log`, `search-result`, `prompt-basics`, `markdown-math`, `juliamarkdown`, `cpp_embedded_latex`,
`markdown_latex_combined` (not languages a user opens as files or without a grammar of their own); Monaco's Monarch-only
languages (Kotlin, Scala, GraphQL, Protobuf, …); first-line detection; any grammar fix.

## Suggested commit message

`feat(textmate): bundle the remaining built-in grammars of VS Code`
