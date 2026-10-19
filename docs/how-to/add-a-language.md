# How to add a language (syntax highlighting)

> **Status: phase 4 — not available yet.** This page describes the planned workflow so that the design stays simple.

LoomEditor highlights code with **TextMate grammars**, the same format as VS Code. Adding a language should never
require writing C#.

## Planned workflow

1. **Get the grammar** (`.tmLanguage.json`) — usually from the VS Code repository (`extensions/<language>/syntaxes/`)
   or the language's official extension. Check its license and add it to `NOTICE.md`.
2. **Add it** to `src/LoomEditor.TextMate/Grammars/<language>/` with a small `language.json`
   (id, file extensions, scope name, comment tokens, brackets).
3. **Register it** in the single list of built-in languages (one line, like `EditorFeatures.cs`).
4. **Add a golden test**: a sample file in `tests/LoomEditor.TextMate.Tests/Golden/<language>/sample.<ext>` and run
   the regeneration script to produce `sample.<ext>.tokens.json` (the expected tokens).
   Review the generated file — it is the specification.
5. Run `dotnet test`. The golden test fails if tokenization changes.

## Rules

- Grammars are read with `System.Text.Json.Nodes.JsonNode` (no serializer, no reflection; Native AOT) — ADR 0008, decision 3.
- Regexes in grammars use the Oniguruma syntax and run through `IRegexEngine` (native Oniguruma 6.9.10, ADR 0007).
- The plan for phase 4 is in `docs/tasks/README.md` (cards P4-34, P4-35, P4-47, P4-48 make this workflow real).
