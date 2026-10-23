# ADR 0001 — Technical baseline (.NET, Windows App SDK, Native AOT, Oniguruma)

- **Status:** accepted
- **Date:** 2026-10-13
- **Verified on:** Windows 11 Pro 10.0.26200 (x64), Visual Studio 18 (2026) Enterprise

All facts below were checked on the date above (NuGet API, GitHub API, local builds). They are **not** from memory.
Re-check them when you upgrade anything listed here.

## 1. .NET 11 SDK

| Item | Value |
|---|---|
| SDK pinned in `global.json` | `11.0.100-rc.2.26504.105` (.NET 11 **RC 2**, "go-live" license) |
| Roll-forward | `latestFeature` + `allowPrerelease` → GA (expected November 2026) is picked up automatically |
| Plain projects | `net11.0` (property `LoomNetTargetFramework`) |
| Windows projects | `net11.0-windows10.0.26100.0` (property `LoomWindowsTargetFramework`), minimum `10.0.19041.0` |
| C# | `LangVersion=latest` (C# 15 with the .NET 11 SDK) |
| Test runner | `dotnet test` in **Microsoft.Testing.Platform** mode (`global.json` → `"test": { "runner": ... }`) |

**Decision:** target .NET 11 RC 2 now. Move `global.json` to the GA SDK as soon as it ships; nothing else should change.

## 2. Windows App SDK and Native AOT

| Item | Value |
|---|---|
| Windows App SDK | **2.5.1** (latest stable; 2.5.4 is "experimental") — brings WinUI component 2.3.9, Win2D-compatible |
| C#/WinRT | the projection shipped by the SDK; `CsWinRTAotOptimizerEnabled=true` by default |
| ILCompiler | `Microsoft.DotNet.ILCompiler 11.0.0-rc.2.26504.105` (x64 and ARM64 runtime packs) |

**Native AOT support level — verified, not assumed:**

- A WinUI 3 app (`samples/LoomEditor.Gallery`) referencing LoomEditor.WinUI, Rendering, View, Core, TextMate, Themes
  and Win2D publishes with `PublishAot=true` and **zero** IL/trim/AOT warnings, and starts (window displayed, text rendered).
- XAML constraints under AOT:
  - use `x:Bind` (compiled), not `{Binding}`; if `{Binding}` is ever needed, the bound type must be marked
    `[Microsoft.UI.Xaml.Data.GeneratedBindableCustomProperty]`;
  - WinRT-exposed classes must be `partial` (the C#/WinRT AOT generator adds code to them) — e.g. `LoomEditor`, `App`;
  - no `Type.GetType(string)`, no reflection-based activation (banned in `src/BannedSymbols.txt`).
- All `src/` projects set `IsAotCompatible=true` (which enables the trim, AOT and single-file analyzers).

### Known issues (and their workarounds)

1. **The app PRI file is not published with `PublishAot=true`.** With Windows App SDK 2.5.1 + .NET 11 RC 1, the
   compiled XAML of the app *and of its libraries* is embedded in `<App>.pri`, but that file is missing from the publish
   folder. Symptom: the AOT app exits immediately (`0xC000027B`), with
   `XamlParseException: Cannot locate resource from 'ms-appx:///MainWindow.xaml'`.
   **Workaround:** `src/LoomEditor.WinUI/buildTransitive/LoomEditor.WinUI.targets` adds the PRI to the publish list.
   It ships in the NuGet package, so apps using LoomEditor get the fix automatically. CI checks that the PRI is
   present and that the app starts. *Remove the workaround once fixed upstream.*
2. **Embedded resources were not found at run time in the AOT Gallery** (`GetManifestResourceStream` returned null).
   Not investigated further: the sample text is now a C# constant. If LoomEditor ever needs embedded resources
   (themes, grammars), add an AOT test for them first.
3. **Win2D warns (`WIN2D0001`) in AnyCPU libraries.** Harmless for libraries — the *app* copies the native DLL for its
   platform — so `Win2DWarnNoPlatform=false` is set in Rendering and WinUI, with a comment.
4. **DirectWrite takes one font family name**, not a XAML-style list (`"Cascadia Mono, Consolas"` silently falls
   back to a proportional font). `Win2DFont` picks the first installed family itself.

## 3. Win2D vs. direct DirectWrite/Direct2D interop

See [ADR 0002](0002-rendering-backend.md). Summary: Win2D 1.4.0 is AOT-clean in practice (verified above).

## 4. Oniguruma

| Item | Value |
|---|---|
| License | **BSD-2-Clause** (`COPYING`, © K.Kosako) — compatible with MIT; attribution in `NOTICE.md` |
| Version | **6.9.10** (2025-01-01), SHA-256 `2A5CFC5A…D769FC05` (matches the `.sha256` published with the release) |
| Build | `src/native/oniguruma/build.ps1`: CMake + MSVC, `onig.dll`, static CRT (no VC++ redistributable needed) |
| x64 | built and checked locally (no `VCRUNTIME140.dll` dependency) |
| ARM64 | same script with `-Architecture ARM64`; needs the "MSVC ARM64 build tools" component — **built in CI only** (not installed locally) |

> ⚠️ **Upstream status: the Oniguruma project ended on 2025-04-24 and its GitHub repository is archived.**
> No more fixes, including security fixes, will come from upstream.

**Consequences and decision:**

- We pin 6.9.10 and keep Oniguruma strictly behind `IRegexEngine` (`src/LoomEditor.TextMate/Oniguruma/`), so it can be
  replaced without touching the tokenizer.
- Before phase 4, write ADR "TextMate regex engine" comparing: (a) Oniguruma 6.9.10 frozen, (b) a maintained fork,
  (c) .NET `Regex` + a translation layer for the Oniguruma syntax subset used by VS Code grammars (no native code at all).
  VS Code itself still uses Oniguruma through `vscode-oniguruma`, which is the reference behavior for golden tests.

## 5. Other choices made while verifying

- **Solution format:** `LoomEditor.slnx` (XML solution, supported by the .NET 11 SDK and Visual Studio 2026).
- **Packages:** Central Package Management (`Directory.Packages.props`).
- **SourceLink:** built into the .NET SDK (no package needed).
- **GitHub Actions:** `actions/checkout@v7`, `setup-dotnet@v6`, `upload-artifact@v7`, `cache@v6` (latest majors on the date above).
