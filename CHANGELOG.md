# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html). Versions are published when a `vX.Y.Z` tag is pushed, see
[docs/RELEASING.md](docs/RELEASING.md).

## [Unreleased]

### Added

- `LoomEditor.Core`: port of the Monaco text model (piece tree, `TextModel`, positions, ranges, selections, edits, undo/redo, decorations, markers, tokens, snippets, diff algorithms, comment threads), with no UI dependency.
- `LoomEditor.TextMate`: TextMate grammar tokenizer (port of vscode-textmate) backed by a native Oniguruma, ten built-in languages; the package carries `onig.dll` for `win-x64` and `win-arm64`.
- `LoomEditor.Themes`: VS Code compatible color themes and the ten built-in default themes.
- `LoomEditor.View`: cursors and commands, view layout, and the editor features: multi-cursor, line and word operations, comments, indentation, find, folding, bracket matching, word highlighter, sticky scroll, hover, suggest, go to definition and error, smart select, snippets, drag and drop, overtype, font zoom, whitespace rendering, diff navigation, inline comment threads on lines or on exact selected text.
- `LoomEditor.Rendering`: Win2D (DirectWrite) rendering of the viewport, gutter, minimap and decorations.
- `LoomEditor.WinUI`: the native WinUI 3 `LoomEditor` control and the `LoomDiffEditor` (side by side or inline), with IME input, UI Automation accessibility and high contrast support.
- All six packages are Native AOT compatible, checked by CI.
- Windows only for now (Windows 10 build 19041 or later, x64 or ARM64).

[Unreleased]: https://github.com/adgranger/LoomEditor/commits/main
