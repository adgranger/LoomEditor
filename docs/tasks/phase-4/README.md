# Phase 4 — read this before any card

Phase 4 brings syntax highlighting: TextMate grammars (a port of `vscode-textmate`), the native Oniguruma regex engine,
Monaco's tokens in the text model, a background tokenizer thread, and VS Code color themes. The decisions are in
[ADR 0007](../../adr/0007-textmate-regex-engine.md) (regex engine) and
[ADR 0008](../../adr/0008-syntax-highlighting.md) (everything else). This page lists the conventions that **every**
phase 4 card relies on. A card never repeats them. The conventions of [phase-3/README.md](../phase-3/README.md)
(sections 1, 3, 4 and 6) still apply.

## 1. Three upstream repositories

| Repository | Pinned commit | Read a file at |
|---|---|---|
| `microsoft/vscode` | `0c9c0a4295a9222e12a605537738719455ae3056` | `https://raw.githubusercontent.com/microsoft/vscode/0c9c0a4295a9222e12a605537738719455ae3056/<path>` |
| `microsoft/vscode-textmate` v9.3.2 | `25b68dad91920b1ed79d357534b8c4582f62d80d` | `https://raw.githubusercontent.com/microsoft/vscode-textmate/25b68dad91920b1ed79d357534b8c4582f62d80d/<path>` |
| `microsoft/vscode-oniguruma` v1.7.0 | `716aeaa229e4ae2e3b0057377b55743e9a3e995b` | `https://raw.githubusercontent.com/microsoft/vscode-oniguruma/716aeaa229e4ae2e3b0057377b55743e9a3e995b/<path>` |

A card says which repository each file comes from: **`vscode:`**, **`textmate:`** or **`oniguruma:`** before the path.
Headers of ported files name the repository and its commit:

```csharp
// Ported from microsoft/vscode-textmate: src/grammar/tokenizeString.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.
```

Test names: `// Monaco: '<name>'` for vscode tests, `// vscode-textmate: '<name>'` and `// vscode-oniguruma: '<name>'`
for the others.

## 2. Where things go

The table is in ADR 0008, decision 2. Namespaces follow the folders: `LoomEditor.TextMate.Oniguruma`,
`LoomEditor.TextMate.Grammar`, `LoomEditor.TextMate.Theme`, `LoomEditor.TextMate.Parsing`,
`LoomEditor.TextMate.Tokenization`, `LoomEditor.Core.Tokens`, `LoomEditor.Core.Model.Tokens`, `LoomEditor.Themes`,
`LoomEditor.View.Highlighting`. Tests mirror `src/`. Everything is `internal` unless the card says **public**.
`LoomEditor.TextMate` and `LoomEditor.Themes` share their internals with `LoomEditor.View` and with their test project
(`InternalsVisibleTo`, added by the first card that needs it).

## 3. Names: vscode-textmate vs Monaco

The two code bases have types with the same name. Keep them apart:

| Name | vscode-textmate (`LoomEditor.TextMate`) | Monaco (`LoomEditor.Core`) |
|---|---|---|
| `EncodedTokenAttributes` / `TokenMetadata` | `Grammar/EncodedTokenAttributes.cs` (`set`, used by the grammar) | `Tokens/TokenMetadata.cs` (getters used by the view) |
| `LineTokens` | `Grammar/LineTokens.cs` (the accumulator while tokenizing one line) | `Tokens/LineTokens.cs` (the tokens of a model line) |
| `StandardTokenType`, `FontStyle`, `ColorMap` | in `LoomEditor.TextMate` | in `LoomEditor.Core.Tokens` (Monaco's) |
| `StateStack` | the grammar's rule stack (`StateStackImpl`) | — (Monaco's `IState`, implemented by a TextMate adapter) |

The bit layout of the 32-bit metadata is the same in both (documented in Monaco's `encodedTokenAttributes.ts` 45–66).

## 4. JavaScript traps specific to phase 4 (in addition to phase-3/README.md section 4)

1. **Token arrays are `uint[]`**, never `int[]`: the metadata uses bit 31 (background color). `Uint32Array` → `uint[]`,
   `x >>> 0` → a `uint` cast, `>>>` → `>>>` on `uint` (C# 11) or `>>` on `uint`.
2. **Object key order.** `for (const k in obj)` visits **integer-like keys first, in increasing order**, then the other
   keys in insertion order. Captures (`"0"`, `"1"`, …) rely on it. `JsonDocument.EnumerateObject` gives document order:
   sort integer-like keys when the card says so.
3. **`Map` and `Set` keep insertion order.** Use `Dictionary` only for lookups; keep a `List` beside it when the order
   is iterated (or `OrderedDictionary<TKey, TValue>`).
4. **Regexes inside the port** (not grammar regexes): JavaScript `\d` and `\w` are ASCII; .NET's are Unicode. Use
   `[GeneratedRegex]` with `[0-9]` / `[A-Za-z0-9_]`, or `RegexOptions.ECMAScript`.
5. **`str.split(sep)` keeps empty parts** (same as `string.Split` without options). **`toLowerCase()`** → `ToLowerInvariant()`.
6. **`Promise` / `async`** in vscode-textmate become synchronous calls (ADR 0008, decision 3).
7. **Raw grammars are mutated** by vscode-textmate (it stores rule ids in them): `Grammar` deep-clones the raw grammar
   it receives, exactly where upstream calls `clone`. Never share a raw grammar object between two `Grammar`s.
8. **`number` limits.** Rule ids, line positions and colors fit in `int`. Time limits use `Stopwatch`, never `DateTime`.

## 5. Native code and threads

- Only `LoomEditor.TextMate/Oniguruma/` calls native code (`[LibraryImport]`, `SafeHandle`). The project sets
  `AllowUnsafeBlocks` for it (P4-03).
- A scanner, a grammar and a registry are **single-threaded**: they are used by the thread that created them (ADR 0008,
  decision 5). The class remarks say so (`Threading: …`).
- Raw grammars and parsed themes are read-only after loading and may be shared between threads.

## 6. Test data and helpers shared by many cards

| Helper or data | Created by | Port of / source |
|---|---|---|
| `onig.dll` next to the test assemblies | P4-02 | `src/native/oniguruma/build.ps1` |
| `tests/LoomEditor.TextMate.Tests/TestHelpers/OnigTestLib.cs` (creates the engine for tests) | P4-04 | textmate: `src/tests/onigLibs.ts` |
| `tests/LoomEditor.TextMate.Tests/TestCases/` (copied verbatim: `first-mate/`, `suite1/`) | P4-06 | textmate: `test-cases/` |
| `tests/LoomEditor.TextMate.Tests/TestHelpers/TestCaseFiles.cs` (finds `TestCases/` from the test assembly) | P4-06 | — |
| `tests/LoomEditor.TextMate.Tests/TestHelpers/TestGrammars.cs` (builds a grammar from inline JSON) | P4-17 | — |
| `tests/LoomEditor.Core.Tests/Tokens/TestHelpers/TestLineToken.cs` | P4-24 | vscode: `src/vs/editor/test/common/core/testLineToken.ts` |
| `tests/LoomEditor.Core.Tests/Model/Tokens/TestHelpers/ManualTokenizationSupport.cs` | P4-31 | vscode: `model.line.test.ts` 103–151 |
| `tests/LoomEditor.TextMate.Tests/TestHelpers/ManualUiDispatcher.cs` | P4-38 | — |

The `TestCases/` files are copied **byte for byte** (MIT, vscode-textmate), with the `.gitattributes` rule
`tests/LoomEditor.TextMate.Tests/TestCases/** -text` so that Git never changes their line endings.

## 7. Commands and commit

Same as every card (see `CLAUDE.md`). The TextMate tests need `onig.dll` (P4-02 builds it on the first build; it needs
CMake and MSVC, which come with Visual Studio). Node is needed only by P4-47 to regenerate golden files.
