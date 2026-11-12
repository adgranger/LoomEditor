# P18-03 — Bundle the last grammars of VS Code (JSX, JSON Lines, code snippets, XSL, Docker Compose, Git, ignore)

- **Status:** done
- **Depends on:** P18-02
- **Size:** S (data files, one list, tests; no code). Not a Monaco port: no `// Ported from` header on JSON files.

## Goal

The languages with a TextMate grammar in the built-in extensions of VS Code that P18-01 and P18-02 left out, except the ones
listed in "Out of scope". Same method as P18-01 and P18-02: copy byte for byte, `language.json` by hand from `package.json`,
one `Sources` line per language file, a golden test per language id, `NOTICE.md`, Gallery sample, `CHANGELOG.md`.

## Sources

`microsoft/vscode` at `0c9c0a4295a9222e12a605537738719455ae3056`, `extensions/<ext>/…`.

| Folder (`Grammars/…`) | Language ids | Files | scopeName | Notes |
|---|---|---|---|---|
| `javascript` | `javascriptreact` | `JavaScriptReact.tmLanguage.json` | `source.js.jsx` | `.jsx` moves from `javascript` to this id, as upstream (new `jsx.language.json`); same configuration as `javascript` |
| `json` | `jsonl`, `snippets` | `JSONL.tmLanguage.json`, `snippets.tmLanguage.json` | `source.json.lines`, `source.json.comments.snippets` | `.jsonl .ndjson`; `.code-snippets` |
| `xml` | `xsl` | `xsl.tmLanguage.json` | `text.xml.xsl` | `.xsl .xslt`; `xsl.language-configuration.json` |
| `yaml` | `dockercompose` | the `yaml.tmLanguage.json` already there | `source.yaml` | upstream selects it by file name patterns only, which are not read: reachable by id only |
| `git-base` | `git-commit`, `git-rebase`, `ignore` | `git-commit`, `git-rebase`, `ignore` grammars | `text.git-commit`, `text.git-rebase`, `source.ignore` | `COMMIT_EDITMSG MERGE_MSG`; `git-rebase-todo`; `.gitignore .gitignore_global .git-blame-ignore-revs` |

## Out of scope

`log`, `search-result`, `prompt-basics`, `markdown-math`, `markdown_latex_combined`, `cpp_embedded_latex`; file name patterns;
first-line detection; any grammar fix.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` and `dotnet test --solution LoomEditor.Portable.slnf -c Release` — green
- [x] Card status and `docs/tasks/README.md` updated

## Suggested commit message

`feat(textmate): bundle the last built-in grammars of VS Code`
