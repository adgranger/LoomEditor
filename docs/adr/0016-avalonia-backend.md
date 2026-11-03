# ADR 0016 — Avalonia backend: a second control on Avalonia's own text stack, next to WinUI

- **Status:** accepted (2026-11-03, with the decisions of [phase-15/README.md](../tasks/phase-15/README.md))
- **Date:** 2026-11-03
- **Amended by:** [ADR 0017](0017-shared-host-logic.md) (decisions 4 and 5: shared code and public UI-free types are compiled from one source)
- **Extends:** [ADR 0002](0002-rendering-backend.md) (rendering behind `IRenderSurface`), [ADR 0007](0007-textmate-regex-engine.md) (native Oniguruma)

## Context

LoomEditor runs on Windows only. Core, TextMate, Themes, View and Lsp are already plain `net11.0` and free of Windows APIs (enforced by
`DependencyRulesTests` and `PlatformNeutralSourceTests`). What ties the editor to Windows:

| Piece | Today | Size |
|---|---|---|
| `LoomEditor.Rendering` | `net11.0-windows` only because its `Win2D/` folder uses Win2D; the other 31 files use `IRenderSurface`, `ITextMeasurer`, `ILineLayout` only | 2 665 portable lines + 423 Win2D lines |
| `LoomEditor.WinUI` | the `LoomEditor` and `LoomDiffEditor` controls, input, IME (`CoreTextEditContext`), UI Automation, widgets | 117 files, ~10 000 lines |
| Oniguruma | `onig.dll` for `win-x64` and `win-arm64` only (`src/native/oniguruma/build.ps1`) | — |
| CI | `windows-latest` only | — |

Avalonia (MIT) runs on Windows, macOS and Linux, supports Native AOT with compiled XAML and compiled bindings, and draws with Skia.

Ways to draw text on Avalonia:

| Option | For | Against |
|---|---|---|
| **A. Avalonia `DrawingContext` + `Avalonia.Media.TextFormatting.TextLayout`** | No direct Skia code; Avalonia does font fallback, HarfBuzz shaping, bidi, hit testing; `TextParagraphProperties.DefaultIncrementalTab` gives the same tab model as DirectWrite's `IncrementalTabStop`; works with every Avalonia renderer, including the headless one used by tests | Less control over glyph runs; variation axes other than weight are not exposed (same limit as Win2D, ADR 0013 decision 5) |
| B. `ICustomDrawOperation` + SkiaSharp + HarfBuzzSharp | Full control, can batch glyph runs | Shaping, fallback, bidi and hit testing by hand; ties the backend to Skia; more code to keep correct |

## Decision

1. **A second control project, `LoomEditor.Avalonia`** (`net11.0`), next to `LoomEditor.WinUI`. WinUI stays the Windows-native control; nothing in it is
   removed or renamed. Dependency rule: `Avalonia → Rendering → View → (TextMate, Themes) → Core`.
2. **`LoomEditor.Rendering` becomes portable** (`net11.0`): the `Win2D/` folder moves into `LoomEditor.WinUI/Rendering/Win2D/`, its only consumer.
   `FontFeatureTags` (pure tag packing) stays in Rendering. The `LoomEditor.Rendering` package keeps its name and loses its Win2D dependency.
3. **Option A**: `AvaloniaRenderSurface`, `AvaloniaTextMeasurer`, `AvaloniaLineLayout`, `AvaloniaBitmapCache`, `AvaloniaMinimapFontSampler` in
   `LoomEditor.Avalonia/Rendering/`, the twins of the Win2D files. Option B stays possible later behind the same interfaces if profiling asks for it.
4. **Same public API, translated.** Every public member of `LoomEditor.WinUI` has a counterpart with the same name in the namespace
   `LoomEditor.Avalonia.Controls`. A fixed list of type substitutions applies (`DependencyProperty` → `StyledProperty<T>` / `DirectProperty<,>`, `VirtualKey` →
   `Avalonia.Input.Key`, `UIElement` → `Control`, `Windows.UI.Color` → `Avalonia.Media.Color`). The UI-free public types (enums, event args, `DiffLine`, `DiffSide`, …)
   are **copied** into the Avalonia namespace: moving them would break the published WinUI API. An architecture test compares the two `PublicAPI.Unshipped.txt` files
   through that substitution list, so no member is forgotten.
5. **One-to-one port of the WinUI files.** Each WinUI file gets a twin with the same relative path and name under `LoomEditor.Avalonia` (`Controls/LoomEditor.Pointer.cs`).
   UI-free internal helpers of WinUI move first to View or Rendering (P15-02), so they are shared, not copied.
6. **Input:** keyboard through `KeyDown` / `TextInput`; IME through Avalonia's `TextInputMethodClient`; clipboard through `TopLevel.Clipboard` (already async in
   `IClipboardService.ReadTextAsync`). On macOS the input layer maps **Cmd → `KeyModifiers.Ctrl`** and **Control → `KeyModifiers.Meta`** (Monaco's `CtrlCmd` /
   `WinCtrl`), so every existing keybinding works with Cmd. Monaco's mac-only keybindings (`mac:` entries, e.g. Cmd+Left = Home) are not ported in phase 15
   (listed in "Behavior differences").
7. **Accessibility:** Avalonia `AutomationPeer`s with the providers that Avalonia exposes at the pinned version. What UI Automation's text pattern does on Windows and Avalonia
   cannot express is written down as a gap, not built by hand.
8. **Oniguruma on every desktop RID:** `build.sh` (CMake) builds `libonig.so` for `linux-x64` / `linux-arm64` and a universal `libonig.dylib` for `osx-x64` /
   `osx-arm64`. It is the same 6.9.10 source and options. `[LibraryImport("onig")]` already resolves those names. They are packed as `runtimes/<rid>/native/`.
9. **Tests:** a new `tests/LoomEditor.Avalonia.Tests` runs on **Avalonia.Headless with the Skia renderer** (draws to a bitmap, no window). It covers what the WinUI control
   never could: pixels of the surface, text widths, typing, IME composition, clipboard, pointer selection. CI runs the portable projects and these tests on
   `ubuntu-latest`, `macos-latest` and `windows-latest`.
10. **Versions:** the latest stable Avalonia release on nuget.org on the day P15-05 runs, pinned in `Directory.Packages.props` and written in this ADR's
    "Consequences" by that card. Compiled bindings by default (`AvaloniaUseCompiledBindingsByDefault`), no reflection binding (Native AOT).

## Why

- **The architecture was built for this.** ADR 0002 put drawing behind `IRenderSurface`; five projects already pass the platform-neutral tests. Phase 15 only replaces the
  two top layers.
- **Option A is the least code that is correct on hard text**: fallback fonts, CJK, emoji, bidi and ligatures come from Avalonia's HarfBuzz path. Win2D gave the same
  for free on Windows.
- **Copying the public types, not moving them,** costs ~15 small files and keeps every existing WinUI host (VIGO) building unchanged.

## Consequences

- Two controls with the same API to maintain. A new public member on one gets its twin on the other in the same card (the parity test fails otherwise).
- New packages: `LoomEditor.Avalonia`; `LoomEditor.TextMate` gains `runtimes/linux-*` and `runtimes/osx-*`.
- The Avalonia Gallery (`samples/LoomEditor.Gallery.Avalonia`) is the manual test bed on macOS and Linux and is published with Native AOT on the three OSes in CI.
- Known gaps at the end of phase 15: mac-specific keybindings, the UI Automation text-pattern details that Avalonia lacks, font variation axes other than `wght`.
- Pinned versions (P15-05, latest stable on nuget.org on 2026-11-03): `Avalonia`, `Avalonia.Skia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Headless` all 12.1.5, in `Directory.Packages.props` (group `Avalonia`). `Avalonia.Headless.XUnit` is not used (xUnit v2 only): the tests start a `HeadlessUnitTestSession` themselves.
- **Verified at the end of phase 15** (P15-31 … P15-34): the Avalonia headless tests; `AvaloniaApiParityTests` with no pending member (the only exceptions are the WinUI XAML compiler's `Connect` / `GetBindingConnector` of the find widget); the Avalonia Gallery
  self-checks on Windows (text 10 of 10, diff 9 of 9, comments 17 of 17, host API 6 of 6); CI run [41654238868](https://github.com/adgranger/LoomEditor/actions/runs/41654238868): the Gallery published with Native AOT on `win-x64`, `linux-x64` and `osx-arm64`
  with 0 warnings, the same self-check counts on each published binary, the app alive after 10 s; CI run [41656687785](https://github.com/adgranger/LoomEditor/actions/runs/41656687785): the `LoomEditor.Avalonia` package built with the others and a headless
  package test app published with Native AOT and run on Linux (and on Windows locally). The TextMate package carries the six `runtimes/*/native` entries (`Pack.ps1 -RequireAllNatives`).
- **Not verified:** the manual plan [phase-15-manual-tests.md](../testing/phase-15-manual-tests.md) (the control seen by a person on macOS and Ubuntu, real IMEs, screen readers, high DPI, the language server demo), `osx-x64` and `linux-arm64` Native AOT publishes (packed, not published in CI), and the package test on macOS.
