# P13-01 — `BuiltInLanguages.GetLanguageIdByFileName`

- **Status:** todo
- **Depends on:** P12-15
- **Size:** S

**Recommended model:** Sonnet

## Goal

A host finds the language of a file with `BuiltInLanguages.GetLanguageIdByFileName("src/Program.cs")` → `"csharp"` and sets it as `editor.LanguageId`, without access to the
internal `LanguageRegistry`.

## Sources

Sources: none (specified in this card; [phase-13/README.md](README.md) table of section 2). Read first: `src/LoomEditor.TextMate/Grammars/BuiltInLanguages.cs`,
`src/LoomEditor.TextMate/Tokenization/LanguageRegistry.cs` (`GetLanguageIdByFileName`, `GetLanguageIdByExtension`, the `plaintext` registration in the constructor),
`tests/LoomEditor.TextMate.Tests/Grammars/BuiltInLanguagesTests.cs`, the `extensions` / `filenames` of the `language.json` files under `src/LoomEditor.TextMate/Grammars/`.

## Files to create or change

- `src/LoomEditor.TextMate/Grammars/BuiltInLanguages.cs` — change
- `src/LoomEditor.TextMate/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.TextMate`)
- `tests/LoomEditor.TextMate.Tests/Grammars/BuiltInLanguagesTests.cs` — change

## Design decisions (already made — do not change)

- New member, XML doc with `<param>`, `<returns>`, `<exception>`:
  ```csharp
  public static string? GetLanguageIdByFileName(string fileName)
  ```
  `ArgumentNullException` for `null`. It accepts a bare file name **or a path**: it applies `Path.GetFileName` first (so `src/tsconfig.json` matches the declared file name
  `tsconfig.json` → `jsonc`, not the `.json` extension), then delegates to `LanguageRegistry.GetLanguageIdByFileName` (declared file names first, case insensitive, then the
  extension). Returns `null` when nothing matches; the XML doc says "use `"plaintext"` then". An empty string returns `null`.
- The lookup uses a private static registry holding `plaintext` and every bundled language: `private static readonly LanguageRegistry Registry = CreateRegistry();`, declared
  **after** `All` (static field initializers run in textual order; `CreateRegistry` calls `RegisterAll`). The registry is only read after the type initializer: thread-safe.
  The class `<remarks>` already says "immutable after the type initializer"; keep it true.
- Known limit, written in the XML doc `<remarks>`: only the last extension is used (`Path.GetExtension`), so multi-dot extensions declared by a language (`.csproj.user`,
  `.js.map`) are matched by their last part only (`.user` matches nothing, `.map` matches nothing). Same rule as `LanguageRegistry`; do not change it.
- `PublicAPI.Unshipped.txt` gains exactly one line: `static LoomEditor.TextMate.Grammars.BuiltInLanguages.GetLanguageIdByFileName(string! fileName) -> string?`.

## Tests

- [ ] New (`BuiltInLanguagesTests`): `Program.cs` → `csharp`; `C:\repo\src\App.TS` → `typescript` (path and case); `README.md` → `markdown`; `notes.txt` → `plaintext`;
  `tsconfig.json` and `src/tsconfig.json` → `jsonc` (declared file name wins over `.json`); `package.json` → `json`; `SConstruct` → `python` (file name without extension);
  `Makefile` → `null`; `""` → `null`; `null` → `ArgumentNullException`.
- [ ] New: every non-null result is in `BuiltInLanguages.Ids`.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `PublicAPI.Unshipped.txt` (TextMate) gains the one line above, nothing else
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Registering languages from the host, `GetLanguageIdByExtension` as a public member, first-line detection (`#!/usr/bin/env python`), making `LanguageRegistry` public.

## Suggested commit message

`feat(textmate): find the language of a file name from the host`
