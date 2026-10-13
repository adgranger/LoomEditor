# Oniguruma (native)

[Oniguruma](https://github.com/kkos/oniguruma) is the regular-expression engine used by TextMate grammars
(and by VS Code, through `vscode-oniguruma`). License: BSD-2-Clause (see NOTICE.md).

We do **not** vendor its sources: `build.ps1` downloads a pinned release, checks its SHA-256 and builds `onig.dll`.

```powershell
./src/native/oniguruma/build.ps1 -Architecture x64     # -> artifacts/native/win-x64/onig.dll
./src/native/oniguruma/build.ps1 -Architecture ARM64   # -> artifacts/native/win-arm64/onig.dll
```

> ⚠️ Upstream ended the project on 2025-04-24 (the repository is archived). We pin the last release (6.9.10).
> See docs/adr/0001-baseline.md for the consequences and the fallback plan.

The managed side is in `src/LoomEditor.TextMate/Oniguruma/` (`[LibraryImport]` only, behind `IRegexEngine`).
