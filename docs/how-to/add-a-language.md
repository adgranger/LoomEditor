# How to add a language (syntax highlighting)

> **Status: phase 4 — not available yet.** This page describes the planned workflow so that the design stays simple.

LoomEditor highlights code with **TextMate grammars**, the same format as VS Code. Adding a language should never
require writing C#.

## Workflow

1. **Get the grammar** (`.tmLanguage.json`) — usually from the VS Code repository (`extensions/<language>/syntaxes/`)
   or the language's official extension. Check its license and add it to `NOTICE.md`.
2. **Add it** to `src/LoomEditor.TextMate/Grammars/<language>/`, files copied byte for byte, with a `language.json`
   written from the extension's `package.json`: `id`, `aliases`, `extensions`, `filenames`, `scopeName`, `path`,
   `embeddedLanguages` (bundled languages only), `tokenTypes`, `unbalancedBracketScopes`, and `grammars` for the other
   files of the folder (injections, grammars only included by others, each with its `scopeName` and `path`).
   Grammar files are embedded automatically (`Grammars/**/*.json` and `*.tmLanguage`, resource name
   `Grammars/<folder>/<file>`); comment tokens and brackets are phase 5.
3. **Register it** in the single list of built-in languages: one line in `Grammars/BuiltInLanguages.cs` (`Sources`:
   folder and `language.json` name).
4. **Add a golden test**: a sample file in `tests/LoomEditor.TextMate.Tests/Golden/<language>/sample.<ext>` and run
   the regeneration script to produce `sample.<ext>.tokens.json` (the expected tokens, made by the real vscode-textmate):

   ```powershell
   cd eng/golden
   npm ci
   node generate.mjs
   ```

   (Node 20+; see ng/golden/README.md.)
   Review the generated file — it is the specification.
5. Run `dotnet test`. The golden test fails if tokenization changes.

## Rules

- Grammars are read with `System.Text.Json.Nodes.JsonNode` (no serializer, no reflection; Native AOT) — ADR 0008, decision 3.
- Regexes in grammars use the Oniguruma syntax and run through `IRegexEngine` (native Oniguruma 6.9.10, ADR 0007).
- The plan for phase 4 is in `docs/tasks/README.md` (cards P4-34, P4-35, P4-47, P4-48 make this workflow real).
