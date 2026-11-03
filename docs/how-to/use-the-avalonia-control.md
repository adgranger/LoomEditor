# How to use LoomEditor in an Avalonia app

`LoomEditor.Avalonia` is the same editor as `LoomEditor.WinUI`, for Avalonia 12 on Windows, macOS and Linux. It has the same public API (the architecture test
`AvaloniaApiParityTests` checks it member by member), so the other how-to guides ([host API](use-the-host-api.md), [diff editor](use-the-diff-editor.md),
[comment threads](use-comment-threads.md), [snippets](use-snippets.md), [fonts](font-and-whitespace-options.md)) apply, with the differences listed below.

What this page says was proved by tests or CI. Anything else is marked **not verified**; the manual plan is
[docs/testing/phase-15-manual-tests.md](../testing/phase-15-manual-tests.md), and agents did not run it.

## Install

```powershell
dotnet add package LoomEditor.Avalonia --prerelease
```

It brings `LoomEditor.Rendering`, `LoomEditor.View`, `LoomEditor.TextMate`, `LoomEditor.Themes` and `LoomEditor.Core`. Your app also references Avalonia itself (`Avalonia.Desktop`
and a theme, for example `Avalonia.Themes.Fluent`). If your project uses `Range`, add the alias of the [README](../../README.md).

## A minimal window

`App.axaml`: add `LoomEditorStyles` after your theme (it carries the control templates and the icons).

```xml
<Application
    xmlns="https://github.com/avaloniaui"
    xmlns:loom="using:LoomEditor.Avalonia">
    <Application.Styles>
        <FluentTheme />
        <loom:LoomEditorStyles />
    </Application.Styles>
</Application>
```

`MainWindow.axaml`:

```xml
<Window
    xmlns="https://github.com/avaloniaui"
    xmlns:loom="using:LoomEditor.Avalonia.Controls">
    <loom:LoomEditor x:Name="Editor" LanguageId="csharp" Text="class Program { }" />
</Window>
```

```csharp
using LoomEditor.Core.Text;

Editor.SetSelections([Selection.Collapsed(new Position(1, 1))]);
Editor.SelectionChanged += (_, _) => UpdateStatusBar(Editor.Selections);
```

`LoomDiffEditor` is in the same namespace (`<loom:LoomDiffEditor OriginalText="…" ModifiedText="…" />`). A complete app is the Gallery:
`samples/LoomEditor.Gallery.Avalonia` (main window, diff window, host API window, language server demo).

Proved by: `eng/package-test/LoomEditor.PackageTest.Avalonia`, a headless app that references the packages only, uses the editor and the diff editor and is published with Native AOT
and run on Windows and Linux in CI (job `package-avalonia`); and the Avalonia headless tests in `tests/LoomEditor.Avalonia.Tests`.

## Differences from the WinUI control

The public API is translated one to one. These are the substitutions:

| WinUI | Avalonia |
|---|---|
| `DependencyProperty` (read-write) | `StyledProperty<T>` (`LoomEditor.FontSizeProperty`…) |
| `DependencyProperty` (read-only, set by the control) | `DirectProperty<TOwner, T>` |
| `VirtualKey` (`LineActionKey = VirtualKey.S`) | `Avalonia.Input.Key` (`Key.S`) |
| `UIElement` / `FrameworkElement` (a view zone, a widget) | `Control` |
| `Windows.UI.Color` | `Avalonia.Media.Color` |
| `ElementTheme` | `Avalonia.Styling.ThemeVariant` |
| `TypedEventHandler<TSender, TArgs>` | `EventHandler<TArgs>` |

Behavior that is not the same (each is in "Behavior differences" of [docs/PORTING.md](../PORTING.md)):

- **Icon glyphs of the diff block and line actions.** `PrimaryBlockActionGlyph`, `SecondaryBlockActionGlyph` and `LineActionGlyph` are characters of an icon font. WinUI draws them with the
  system symbol font; Avalonia uses the property `LoomDiffEditor.GlyphFontFamily`, whose default (`Segoe Fluent Icons, Segoe MDL2 Assets`) exists on Windows only. On macOS and Linux
  set `GlyphFontFamily` to a font you ship, or use plain characters such as `"+"` (the Gallery does).
- **macOS keys.** The Command key works as Ctrl and the Control key as Meta, so every default keybinding works with Cmd (Cmd+C, Cmd+Z…). Monaco's macOS-only keybindings
  (for example Cmd+Left for Home) are **not** ported.
- **Accessibility.** The editor is an `Edit` automation peer whose value is the text; there is no text pattern (Avalonia 12.1.5 has no public text provider), so a screen reader
  does not read lines, words or the caret range. A screen reader is detected on Windows only: on macOS and Linux set `AccessibilitySupport = EditorAccessibilitySupport.On`.
  **Not verified** with Narrator, NVDA, VoiceOver or Orca.
- **IME.** Composition arrives as preedit text; there are no composition underline styles (one thin underline). **Not verified** with a real IME on any system.
- **View zones** follow a scroll one dispatcher turn late (Avalonia refuses a layout change while a frame is drawn).
- **Fonts.** The default font is the monospace font of the operating system (Cascadia Mono / Consolas, Menlo, DejaVu Sans Mono). Font variation axes other than `wght` are not supported.
- **Comment zone diagnostics** of the WinUI Gallery have no counterpart; the Gallery of this control does not show them.

## Native AOT

The control is trimming and Native AOT safe. CI publishes the Gallery with `PublishAot` for `win-x64`, `linux-x64` and `osx-arm64` with 0 warnings and runs its text, diff,
comments and host API self-checks on each published binary (job `aot-avalonia`), then starts it for 10 seconds. Notes for your app:

- Use compiled bindings (`AvaloniaUseCompiledBindingsByDefault`), as the Gallery does, and no reflection.
- Build first, then publish with `--no-build`: a publish that recompiles drops the compiled XAML of an Avalonia app (`dotnet build -r <rid>` then `dotnet publish -r <rid> --no-build`).
- Publish with the runtime identifier of the machine you target; `osx-x64` and `linux-arm64` are packed but CI does not AOT-publish them (**not verified**).

## Oniguruma on every system

Syntax highlighting uses the native Oniguruma library, packed by `LoomEditor.TextMate` as `runtimes/<rid>/native/`: `win-x64` and `win-arm64` (`onig.dll`), `linux-x64` and
`linux-arm64` (`libonig.so`), `osx-x64` and `osx-arm64` (`libonig.dylib`, one universal file). A publish for one of these runtime identifiers copies the right file next to the
executable; CI checks that it is there (`aot-avalonia`) and that the package carries all six (`eng/Pack.ps1 -RequireAllNatives`).

## What was not verified

Everything in [docs/testing/phase-15-manual-tests.md](../testing/phase-15-manual-tests.md): the control on macOS and Ubuntu seen by a person (fonts, high DPI, scrolling feel,
trackpad), the IME on each system, screen readers, high contrast, and the language server demo on a real server. The Avalonia headless tests and the CI jobs above are the only
automatic proof.
