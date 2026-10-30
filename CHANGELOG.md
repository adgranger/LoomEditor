# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html). Versions are published when a `vX.Y.Z` tag is pushed, see
[docs/RELEASING.md](docs/RELEASING.md).

## [Unreleased]

## [0.2.0-alpha.4] - 2026-10-30

### Added

- `LoomEditor.WinUI`: `LineHeight` is settable on `LoomEditor` and `LoomDiffEditor` (Monaco `lineHeight`): 0 is automatic, below 8 a multiplier of the font size, else a height in DIPs. Reading it still gives the height in use.

### Fixed

- `LoomEditor.Rendering`: the diagonal hatching of the diff editor's empty view zones now matches Monaco (2 px stripes every 8 px of `x + y`; it had twice as many stripes, half as thick).

## [0.2.0-alpha.3] - 2026-10-30

### Added

- `LoomEditor.WinUI`: `LoomEditor.FoldingEnabled` (Monaco `folding`) and `LoomEditor.LineDecorationsWidth` (Monaco `lineDecorationsWidth`). With folding off, no line numbers and a width of 0, the margin disappears.

## [0.2.0-alpha.2] - 2026-10-30

### Fixed

- `LoomEditor.WinUI`: `LoomDiffEditor.WaitForDiffAsync` and `RevealFirstChangeAsync` no longer throw when called while the control is unloaded; they wait for the next load.

## [0.2.0-alpha.1] - 2026-10-30

### Added

- `LoomEditor.TextMate`: `BuiltInLanguages.GetLanguageIdByFileName` (the language of a file name, `null` when unknown).
- `LoomEditor.View`: public host decorations (`EditorDecoration`, `EditorDecorationOptions`, `DecorationsCollection`) and host decoration styles (`CustomDecorationStyle`, `CustomDecorationStyles.Register` / `Unregister`).
- `LoomEditor.WinUI`: public host API on `LoomEditor`: `CreateDecorationsCollection`, `ScrollChanged` / `EditorScrollChangedEventArgs`, `LayoutChanged`, `LineHeight`, `GetTopForLineNumber`, `LineNumbersEnabled`, `ResetText`, `SetHiddenAreas`, `GutterClicked` / `GutterClickedEventArgs` / `GutterArea`, `AddViewZone` and `RemoveViewZone` (host WinUI content in a gap between lines). See [how-to/use-the-host-api.md](docs/how-to/use-the-host-api.md).

## [0.1.0-alpha.1] - 2026-10-29

### Added

- `LoomEditor.Core`: port of the Monaco text model (piece tree, `TextModel`, positions, ranges, selections, edits, undo/redo, decorations, markers, tokens, snippets, diff algorithms, comment threads), with no UI dependency.
- `LoomEditor.TextMate`: TextMate grammar tokenizer (port of vscode-textmate) backed by a native Oniguruma, ten built-in languages; the package carries `onig.dll` for `win-x64` and `win-arm64`.
- `LoomEditor.Themes`: VS Code compatible color themes and the ten built-in default themes.
- `LoomEditor.View`: cursors and commands, view layout, and the editor features: multi-cursor, line and word operations, comments, indentation, find, folding, bracket matching, word highlighter, sticky scroll, hover, suggest, go to definition and error, smart select, snippets, drag and drop, overtype, font zoom, whitespace rendering, diff navigation, inline comment threads on lines or on exact selected text.
- `LoomEditor.Rendering`: Win2D (DirectWrite) rendering of the viewport, gutter, minimap and decorations.
- `LoomEditor.WinUI`: the native WinUI 3 `LoomEditor` control and the `LoomDiffEditor` (side by side or inline), with IME input, UI Automation accessibility and high contrast support.
- All six packages are Native AOT compatible, checked by CI.
- Windows only for now (Windows 10 build 19041 or later, x64 or ARM64).

[Unreleased]: https://github.com/adgranger/LoomEditor/compare/v0.2.0-alpha.4...HEAD
[0.2.0-alpha.4]: https://github.com/adgranger/LoomEditor/compare/v0.2.0-alpha.3...v0.2.0-alpha.4
[0.2.0-alpha.3]: https://github.com/adgranger/LoomEditor/compare/v0.2.0-alpha.2...v0.2.0-alpha.3
[0.2.0-alpha.2]: https://github.com/adgranger/LoomEditor/compare/v0.2.0-alpha.1...v0.2.0-alpha.2
[0.2.0-alpha.1]: https://github.com/adgranger/LoomEditor/compare/v0.1.0-alpha.1...v0.2.0-alpha.1
[0.1.0-alpha.1]: https://github.com/adgranger/LoomEditor/releases/tag/v0.1.0-alpha.1
