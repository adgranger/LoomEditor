# ADR 0007 — TextMate regex engine: native Oniguruma 6.9.10, frozen, behind `IRegexEngine`

- **Status:** accepted
- **Date:** 2026-10-19
- **Replaces:** the open question of ADR 0001, section 4

## Context

TextMate grammars (the `.tmLanguage.json` files of VS Code) are written in the **Oniguruma** regex dialect. VS Code runs
them with `vscode-oniguruma` 1.7.0, a WebAssembly build of Oniguruma **6.9.8** (submodule `deps/oniguruma` at
`08d36110c5670c815ad6d6f969e578049d209080`). That build is the reference behavior for our golden tests.

ADR 0001 pinned Oniguruma **6.9.10** (built by `src/native/oniguruma/build.ps1`) and noted that upstream ended the
project on 2025-04-24 (the repository is archived: no more fixes, including security fixes). It asked to compare three
options before phase 4.

| Option | For | Against |
|---|---|---|
| (a) Oniguruma 6.9.10, native, frozen | Same engine family as VS Code: every construct of every grammar works (`\G`, `\h`, `[[:alpha:]]`, possessive quantifiers, `\g<name>` subexpression calls, `&&` class intersection, `(?~…)` absent operator). The build script exists. `ONIG_ENCODING_UTF16_LE` works directly on .NET strings (no UTF-8 conversion). | Native DLL per architecture (x64, ARM64). No more upstream fixes. |
| (b) A maintained fork | Same as (a), with fixes | Not evaluated for phase 4 (no fork was chosen by the maintainer); it stays possible later behind the same interface, since it is the same C API |
| (c) .NET `Regex` + a translation layer | No native code | No subexpression calls (used by several grammars), different POSIX brackets, `\h`, intersection and absent operator; possessive quantifiers must be rewritten; `RegexOptions.Compiled` does nothing under Native AOT (interpreter only). Every difference becomes a golden-test failure to explain. |

## Decision

1. **Option (a).** LoomEditor uses the native Oniguruma **6.9.10** library, frozen, through `[LibraryImport]` only.
   All of it is inside `src/LoomEditor.TextMate/Oniguruma/`. Nothing else knows Oniguruma exists: the tokenizer uses
   `IRegexEngine` → `IOnigScanner` / `IOnigString` (the shape of vscode-textmate's `IOnigLib`).
2. **Compile options are vscode-oniguruma's:** `ONIG_OPTION_CAPTURE_GROUP`, `ONIG_SYNTAX_DEFAULT`, but the encoding
   is **`ONIG_ENCODING_UTF16_LE`** instead of UTF-8. Offsets are UTF-16 code units, which is what vscode-oniguruma
   returns after its UTF-8 → UTF-16 mapping. A lone surrogate is one code unit (vscode-oniguruma encodes it as a 3-byte
   value; the visible offsets are the same).
3. **The scanner algorithm is vscode-oniguruma's per-regex path** (`src/onig.cc`, `findNextOnigScannerMatch`, the
   branch used for strings of 1000 UTF-8 bytes or more, lines 250–271): search every regex from the start position, keep the
   earliest match, the lowest pattern index wins a tie, stop early when a match starts at the start position, and cache
   the last result of each regex per string (never for a regex containing `\G`). The `onig_regset_search` branch
   (short strings) is **not** used: it returns the same match (earliest start, lowest index) and one path is easier
   to test. This is listed in `docs/PORTING.md` (behavior differences).
4. **Safety.** Only grammars that ship with LoomEditor or that the host application gives on purpose are compiled.
   Oniguruma's own retry limit is kept at its compiled-in default (`ONIG_DEFAULT_RETRY_LIMIT_IN_MATCH` in
   `oniguruma.h`; P4-03 checks the name and value in the 6.9.10 header): a catastrophic pattern ends the search with a
   negative "retry limit over" code, which the scanner treats as "no match" for that regex. The time limit per line is vscode-textmate's (500 ms, ADR 0008).
5. **Packaging.** `onig.dll` is built by `build.ps1` (x64 locally, x64 and ARM64 in CI) and shipped as a native asset
   of `LoomEditor.TextMate` (`runtimes/win-<arch>/native/onig.dll`). The build of `LoomEditor.TextMate` runs
   `build.ps1` when the DLL for the build architecture is missing (P4-02).
6. **Reference differences.** VS Code uses Oniguruma 6.9.8; we use 6.9.10. A golden test that differs only because of
   the engine version is fixed by regenerating nothing: the difference is written in `docs/PORTING.md` and the test
   lists it as a known difference (P4-48 says how).

## Consequences

- Grammars run exactly as in VS Code, apart from the documented engine-version differences.
- Replacing the engine (a fork, or a managed engine one day) means a new `IRegexEngine` implementation and a green
  golden-test run; the tokenizer does not change.
- The NuGet package and the Gallery carry one native DLL per architecture.
- The vscode-oniguruma test suite (`src/test/index.test.ts`, 12 tests) is ported against our scanner (P4-04).
