# Phase 15 — Avalonia backend (macOS and Linux)

**Done when:** a host using only the NuGet packages can put `LoomEditor` and `LoomDiffEditor` in an Avalonia app. The app runs on Windows, macOS and Linux and is
published with Native AOT on each. It has the same public API as the WinUI controls, highlighting (native Oniguruma on every desktop runtime), editing, IME,
clipboard, widgets, comment threads and the diff editor with its line and block actions. The WinUI control is unchanged for its hosts.

(Renumbered from 13 on 2026-10-30, then from 14 on 2026-11-02.) Planned 2026-11-03. Decision: [ADR 0016](../adr/0016-avalonia-backend.md). Design and rules:
[docs/tasks/phase-15/README.md](../tasks/phase-15/README.md). Task cards: [docs/tasks/phase-15/](../tasks/phase-15/README.md) (34 cards).

`LoomEditor.Core`, `TextMate`, `Themes`, `View` and `Lsp` already run on any .NET platform (enforced by the architecture tests). Phase 15 makes `Rendering` portable too and adds
the Avalonia control on top.

## Checklist

- [ ] ADR 0016: Avalonia's `DrawingContext` + `TextLayout`, the same public API, one-to-one translation of the WinUI files
- [ ] `LoomEditor.Rendering` is `net11.0`; Win2D moves into `LoomEditor.WinUI` (P15-01, P15-02)
- [ ] Oniguruma for `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`; CI on Linux and macOS (P15-03, P15-04)
- [ ] `LoomEditor.Avalonia` project, headless test project, API parity test (P15-05)
- [ ] Text measurement and drawing without DirectWrite: tab stops through `DefaultIncrementalTab` (P15-06, P15-07)
- [ ] The `LoomEditor` control: drawing, scrolling, options, keyboard, clipboard, pointer, IME (`TextInputMethodClient`), accessibility, host API (P15-08 … P15-16)
- [ ] Widgets: find, hover, suggest, comment threads (P15-17 … P15-23)
- [ ] `LoomDiffEditor`, with hidden unchanged regions, the accessible viewer and the phase 14 actions (P15-24 … P15-30)
- [ ] Avalonia Gallery; Native AOT on Windows, macOS and Linux in CI; `LoomEditor.Avalonia` package; docs (P15-31 … P15-34)
