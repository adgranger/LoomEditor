# Golden token files

`generate.mjs` regenerates the expected tokens of the golden tests (`tests/LoomEditor.TextMate.Tests/Golden/<language>/sample.<ext>.tokens.json`)
with the **real** [vscode-textmate](https://github.com/microsoft/vscode-textmate) 9.3.2 and
[vscode-oniguruma](https://github.com/microsoft/vscode-oniguruma) 1.7.0, from LoomEditor's own grammar files
(`src/LoomEditor.TextMate/Grammars/*/language.json`) and themes (`src/LoomEditor.Themes/BuiltIn/`: Dark Modern and
Light Modern).

```powershell
cd eng/golden
npm ci
node generate.mjs
```

It needs Node 20+, and is never run by the build or by the tests. Running it twice gives no diff. It prints one line per
sample and exits with a non-zero code on any error.

Each line of a file has `text`, `tokens` (from `tokenizeLine`: start, end and the scopes joined by spaces) and, per theme
(`darkModern`, `lightModern`), the colors (from `tokenizeLine2` and the color map: start, foreground, font style). The theme
pipeline is VS Code's (`colorThemeData.ts`): includes, the default rule, `normalizeColor`, `TokenColorIndex`. The samples are the
fixtures of `extensions/vscode-colorize-tests` of VS Code (MIT), copied unchanged.