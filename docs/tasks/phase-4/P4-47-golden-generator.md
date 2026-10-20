# P4-47 — Golden tests (1/2): the reference generator (real vscode-textmate) and the samples

- **Status:** done
- **Depends on:** P4-46
- **Size:** M (a Node script + data files)

## Goal

`eng/golden/` regenerates the expected tokens of each sample with the **real** vscode-textmate 9.3.2 and
vscode-oniguruma 1.7.0, from **our** grammar and theme files, for the ten languages.

## Sources

| File | Lines | What to use |
|---|---|---|
| vscode: `extensions/vscode-colorize-tests/test/colorize-fixtures/` | — | the samples: `test.cs`, `test.ts`, `test.js`, `test.json`, `test.html`, `test.css`, `test.xml`, `test.md`, `test.py`, `test.yaml` (copy verbatim; MIT) |
| vscode: `src/vs/workbench/contrib/themes/browser/themes.test.contribution.ts` | 110–138, 159–195 | how VS Code captures tokens (`tokenizeLine` with the state carried, `tokenizeLine2` + color map) — the script does the same |
| vscode: `src/vs/workbench/services/themes/common/colorThemeData.ts` | 104–150, 774–833, 993–1106 | the theme pipeline the script must reproduce in JavaScript (include, rule 0, `normalizeColor`, `TokenColorIndex`) |
| vscode: `src/vs/workbench/services/textMate/common/TMGrammarFactory.ts` | 37–100 | how the registry gets grammars and injections |

## Files to create or change

- `eng/golden/package.json` (exact versions `"vscode-textmate": "9.3.2"`, `"vscode-oniguruma": "1.7.0"`), `package-lock.json` (committed), `generate.mjs`, `README.md` — create; `eng/golden/node_modules/` in `.gitignore`
- `tests/LoomEditor.TextMate.Tests/Golden/<language>/sample.<ext>` — the fixtures; `…/sample.<ext>.tokens.json` — generated
- `.gitattributes` — `tests/LoomEditor.TextMate.Tests/Golden/** -text`
- `docs/how-to/add-a-language.md` — the regeneration command

## Design decisions (already made — do not change)

- ADR 0008 decision 11. The script reads `src/LoomEditor.TextMate/Grammars/*/language.json` (same fields as P4-34),
  builds a vscode-textmate `Registry` with `loadGrammar` from those files and `getInjections` from `injectTo`, and loads
  Dark Modern and Light Modern from `src/LoomEditor.Themes/BuiltIn/` with the VS Code pipeline above.
- Output format (one file per sample, UTF-8, `\n`, two-space indentation; the values below only show the shape):
  ```json
  { "language": "csharp", "lines": [
      { "text": "using System;",
        "tokens": [ { "start": 0, "end": 5, "scopes": "source.cs keyword.other.directive.using.cs" } ],
        "darkModern": [ { "start": 0, "foreground": "#C586C0", "fontStyle": "" } ],
        "lightModern": [ { "start": 0, "foreground": "#AF00DB", "fontStyle": "" } ] } ] }
  ```
  `tokens` comes from `tokenizeLine`; the theme arrays come from `tokenizeLine2` (metadata decoded with the color map).
- The script is never run by the build or the tests; it needs Node 20+ (`node generate.mjs` from `eng/golden/`).
- Lines are split like VS Code (`\r\n`, `\r`, `\n`).

## Tests

None in C# here (P4-48). The script prints one line per sample and exits non-zero on any error.

## Acceptance criteria

- [x] `npm ci && node generate.mjs` in `eng/golden/` produces the ten `.tokens.json` files; running it twice gives no diff
- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings; `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `NOTICE.md`: the colorize fixtures (MIT, VS Code)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The C# comparison (P4-48).

## Suggested commit message

`test(textmate): generate golden tokens with the real vscode-textmate`
