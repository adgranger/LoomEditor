# LoomEditor

**A native code editor control for WinUI 3 — a C# port of the Monaco editor (the editor of VS Code).**

- **Native**: no WebView2, no JavaScript. C# all the way, plus DirectWrite (through Win2D) and Oniguruma.
- **Native AOT compatible**: no run-time reflection; CI publishes the demo app with `PublishAot=true` and zero warnings.
- **Monaco, ported faithfully**: same concepts (`TextModel`, `Position`, `Range`, `Selection`, piece tree…),
  Monaco's tests ported, every ported file linked to its source.
- **Easy to maintain**: one feature = one folder, explicit registration, architecture rules checked by tests.
- **MIT licensed.**

> **Status: pre-alpha, first release `0.1.0-alpha.1`.** Phases 1 to 12 are built and tested: text model, rendering,
> editing and IME, TextMate highlighting with VS Code themes, editor features (find, folding, brackets, suggest,
> snippets…), accessibility, the diff editor and inline comment threads. The API is not stable yet and the manual
> test plans are not all run. The LSP client (`LoomEditor.Lsp`) is not packaged yet. See the roadmap below.

## Usage (target API)

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:loom="using:LoomEditor.WinUI.Controls">

    <loom:LoomEditor Text="{x:Bind ViewModel.Code, Mode=OneWay}" />
</Window>
```

```csharp
using LoomEditor.Core.Text;
using LoomEditor.View.Features.SelectAll;

editor.Text = File.ReadAllText("Program.cs");
editor.IsReadOnly = false;                       // the user can type, paste, undo (Ctrl+Z)…
editor.SetSelections([Selection.Collapsed(new Position(1, 1))]);
editor.SelectionChanged += (_, _) => UpdateStatusBar(editor.Selections);
editor.Commands.Execute(SelectAllCommands.SelectAll);
```

See the [how-to guides](docs/how-to/) for languages, themes, snippets, the diff editor and comment threads.

> `LoomEditor.Core.Text.Range` has the same name as `System.Range`. In projects that use it, add
> `<Using Include="LoomEditor.Core.Text.Range" Alias="Range" />` to your `.csproj`.

## Requirements

- Windows 10 version 2004 (build 19041) or later, x64 or ARM64 (IME input needs Windows 11)
- .NET 11 SDK (currently RC 2, see `global.json`)
- Windows App SDK 2.5
- To build Oniguruma from source (the NuGet package ships `onig.dll`): Visual Studio 2022+ with the C++ workload

## Install

```powershell
dotnet add package LoomEditor.WinUI --prerelease
```

Packages are published to nuget.org when a `vX.Y.Z` tag is pushed (first version: `0.1.0-alpha.1`); see
[docs/RELEASING.md](docs/RELEASING.md) and [CHANGELOG.md](CHANGELOG.md).

## Build, test, run

```powershell
dotnet build LoomEditor.slnx -c Release                          # warnings are errors in Release
dotnet test --solution LoomEditor.slnx -c Release                # unit + architecture tests
dotnet run --project samples/LoomEditor.Gallery -p:Platform=x64  # the demo app
dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64  # Native AOT build of the demo app
```

## Roadmap

| Phase | Content | Done when |
|---|---|---|
| 0 | Skeleton, docs, CI, AOT smoke test | CI green, AOT Gallery starts ✅ |
| 1 | Text model: piece tree, positions, EOL, undo/redo | Monaco model tests ported and green ✅ |
| 2 | Rendering: plain text, virtualized viewport, gutter, caret, scrolling | A 100 MB file is fluid ✅ |
| 3 | Editing: selection, multi-cursor, keyboard, mouse, clipboard, IME | Japanese/Chinese IME input works ✅ |
| 4 | TextMate grammars + VS Code themes | Golden tests green for 10 languages ✅ |
| 5 | Features: find/replace, folding, brackets, auto-indent, comment | One feature = one documented folder ✅ |
| 6 | Minimap, sticky scroll, word wrap, whitespace, guides | — ✅ |
| 7 | Full UI Automation accessibility | Narrator reads and navigates the content ✅ |
| 8 | LSP client (completion, hover, diagnostics, go-to-definition) | Demo with a real language server ✅ (not packaged yet) |
| 9 | Diff editor (side by side, inline, overview ruler, accessible viewer) | Gallery diff demo (built and tested; manual plan pending) ✅ |
| 10 | Editor completeness: fonts, zoom, whitespace, overtype, snippets, drag and drop | Built and tested ✅ |
| 11 | Inline comment threads (GitHub review style) | Built and tested ✅ |
| 12 | Comment threads on line ranges or exact selected text | Built and tested ✅ |

Details: [docs/roadmap/](docs/roadmap/).

## Documentation

- [Architecture](docs/ARCHITECTURE.md) — layers, rules, where to find things
- [Porting guide](docs/PORTING.md) — how Monaco files are ported, and the Monaco → LoomEditor map
- [Glossary](docs/GLOSSARY.md) — piece tree, token, decoration, view line… in plain words
- [How to add a feature](docs/how-to/add-a-feature.md) · [a command](docs/how-to/add-a-command.md) · [a language](docs/how-to/add-a-language.md)
- [Architecture decisions (ADRs)](docs/adr/)
- [Contributing](CONTRIBUTING.md)

## Packages

| Package | Content |
|---|---|
| **`LoomEditor.WinUI`** | the `LoomEditor` control — the package you install; it brings the others |
| `LoomEditor.Rendering` | drawing (Win2D) |
| `LoomEditor.View` | cursors, commands, features (UI-independent) |
| `LoomEditor.TextMate`, `LoomEditor.Themes` | syntax highlighting and themes (UI-independent) |
| `LoomEditor.Core` | text model and value types (UI-independent) |

## License

MIT — see [LICENSE](LICENSE). LoomEditor contains code ported from Monaco / VS Code (MIT) and uses
vscode-textmate (MIT) and Oniguruma (BSD-2-Clause): see [NOTICE.md](NOTICE.md).
