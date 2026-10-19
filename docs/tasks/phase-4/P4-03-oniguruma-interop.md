# P4-03 — Oniguruma interop: `[LibraryImport]`, safe handles, one compiled regex

- **Status:** done
- **Depends on:** P4-02
- **Size:** M

## Goal

`LoomEditor.TextMate/Oniguruma/` can compile an Oniguruma regex (UTF-16 LE, vscode-oniguruma's options) and search a
.NET string from a position, returning the capture offsets in UTF-16 code units. The convention test accepts the
headers of the three upstream repositories.

## Sources

| File | Lines | What to port |
|---|---|---|
| oniguruma: `src/onig.cc` | 25–35, 56–70, 74–92, 119–133 | `OnigRegExp` (the regex part, not the cache yet), `encodeOnigRegion`, `hasGAnchor`, the compile call (`onig_new` with `ONIG_OPTION_CAPTURE_GROUP`, `ONIG_SYNTAX_DEFAULT`), `_searchOnigRegExp` |
| Oniguruma 6.9.10 `src/oniguruma.h` (from the archive `build.ps1` downloads, `src/native/oniguruma/.build/onig-6.9.10/src/oniguruma.h`) | — | signatures, option values, `OnigRegion`, `OnigErrorInfo`, `ONIG_MAX_ERROR_MESSAGE_LEN`, the retry-limit default (ADR 0007, decision 4) |

## Files to create or change

- `src/LoomEditor.TextMate/Oniguruma/OnigNative.cs` — create (`[LibraryImport("onig")]` declarations only)
- `src/LoomEditor.TextMate/Oniguruma/OnigRegexHandle.cs`, `OnigRegionHandle.cs` — create (`SafeHandle`s)
- `src/LoomEditor.TextMate/Oniguruma/OnigRegex.cs` — create (one compiled regex: `Search(string, int start, OnigSearchOptions)` → `OnigMatch?`)
- `src/LoomEditor.TextMate/Oniguruma/OnigException.cs` — create
- `src/LoomEditor.TextMate/LoomEditor.TextMate.csproj` — `AllowUnsafeBlocks`, `InternalsVisibleTo` for `LoomEditor.TextMate.Tests` and `LoomEditor.View`
- `tests/LoomEditor.Architecture.Tests/ConventionTests.cs` — change: a ported header may name `microsoft/vscode`,
  `microsoft/vscode-textmate` or `microsoft/vscode-oniguruma`
- `tests/LoomEditor.TextMate.Tests/Oniguruma/OnigRegexTests.cs` — create

## Design decisions (already made — do not change)

- Encoding: the address of the exported data symbol `OnigEncodingUTF16_LE` (`NativeLibrary.GetExport`). Syntax: the
  value of the exported pointer variable `OnigDefaultSyntax`. Check both names with `dumpbin /exports onig.dll` and
  write them as constants.
- `onig_initialize` is called once per process (a `static readonly` lazy initializer is allowed here: it is not
  mutable state), with the UTF-16 LE encoding.
- Patterns and subjects are passed as `char*` from `fixed`; `end = start + length * 2` bytes. Offsets returned by
  Oniguruma are bytes: divide by 2. `-1` (unmatched group) stays `-1`.
- `onig_error_code_to_str` is variadic in C: declare it with one `OnigErrorInfo*` argument (the Windows x64 and ARM64
  ABIs pass it like a fixed argument) and say so in a comment.
- A search error other than `ONIG_MISMATCH` (including the retry-limit codes) returns "no match" and writes one
  `Debug.WriteLine`; a compile error throws `OnigException` with Oniguruma's message (vscode-oniguruma throws too).
- `OnigRegex` remembers `HasGAnchor` (the naive scan of `hasGAnchor`, onig.cc 74–84).
- `IRegexEngine.Version` returns `onig_version()`.

## Tests

- [x] New: `Compile_InvalidPattern_ThrowsWithOnigurumaMessage`
- [x] New: `Search_FromPosition_ReturnsUtf16Offsets` (subject with a surrogate pair before the match)
- [x] New: `Search_UnmatchedGroup_ReturnsMinusOne`
- [x] New: `Search_OnigurumaOnlySyntax_Works` (`\h+`, `[[:alpha:]]+`, `a++`, `(?<n>a|\(\g<n>\))`)
- [x] New: `HasGAnchor_DetectsBackslashG`
- [x] New: `Version_Is6910`
- [x] New (architecture): a file whose header names `microsoft/vscode-textmate:` without a second line fails the convention test

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map updated (vscode-oniguruma section created)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The scanner and its cache (P4-04), `onig_regset_*` (never used, ADR 0007 decision 3).

## Suggested commit message

`feat(textmate): call Oniguruma through LibraryImport`
