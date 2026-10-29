# Phase 13 — Avalonia backend (macOS and Linux)

Later, not scheduled. `LoomEditor.Core`, `TextMate`, `Themes` and `View` already run on any .NET platform
(enforced by the architecture tests); only the two Windows projects are specific to Windows.

- [ ] ADR: rendering backend for Avalonia (Skia through `DrawingContext`, or a custom `ICustomDrawOperation`), and how text is measured without DirectWrite
- [ ] `LoomEditor.Avalonia` project (`WinUI → Rendering → View` becomes `Avalonia → View`): control, keyboard, pointer, clipboard
- [ ] IME through Avalonia's `TextInputMethodClient` (the Windows version uses `CoreTextEditContext`)
- [ ] Tab stops and font metrics without DirectWrite's `IncrementalTabStop` (see "Behavior differences" in docs/PORTING.md)
- [ ] Gallery for Avalonia; CI on macOS and Linux; Native AOT check on both
