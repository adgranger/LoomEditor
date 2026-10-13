# LoomEditor

**A native code editor control for WinUI 3 — a C# port of the Monaco editor (the editor of VS Code).**

- **Native**: no WebView2, no JavaScript. C# all the way, plus DirectWrite (through Win2D) and Oniguruma.
- **Native AOT compatible**: no run-time reflection; CI publishes the demo app with `PublishAot=true` and zero warnings.
- **Monaco, ported faithfully**: same concepts (`TextModel`, `Position`, `Range`, `Selection`, piece tree…),
  Monaco's tests ported, every ported file linked to its source.
- **Easy to maintain**: one feature = one folder, explicit registration, architecture rules checked by tests.
- **MIT licensed.**

> **Status: phase 0 (pre-alpha).** The control displays read-only text with line numbers, and Ctrl+A selects all.
> Not usable for real editing yet. See the roadmap below.

## Usage (target API)

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:loom="using:LoomEditor.WinUI.Controls">

    <loom:LoomEditor Text="{x:Bind ViewModel.Code, Mode=OneWay}" />
</Window>
```

```csharp
using LoomEditor.View.Features.SelectAll;

editor.Text = File.ReadAllText("Program.cs");
editor.Commands.Execute(SelectAllCommands.SelectAll);
```

Planned for later phases: `Language="csharp"`, `Theme="Dark Modern"`, two-way editing with a `TextModel`,
decorations, LSP integration, diff editor.

> `LoomEditor.Core.Text.Range` has the same name as `System.Range`. In projects that use it, add
> `<Using Include="LoomEditor.Core.Text.Range" Alias="Range" />` to your `.csproj`.

## Requirements

- Windows 10 version 2004 (build 19041) or later, x64 or ARM64
- .NET 11 SDK (currently RC 1, see `global.json`)
- Windows App SDK 2.5
- To build Oniguruma (phase 4): Visual Studio 2022+ with the C++ workload

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
| **0** | Skeleton, docs, CI, AOT smoke test | CI green, AOT Gallery starts ← **we are here** |
| 1 | Text model: piece tree, positions, EOL, undo/redo | Monaco model tests ported and green |
| 2 | Rendering: plain text, virtualized viewport, gutter, caret, scrolling | A 100 MB file is fluid |
| 3 | Editing: selection, multi-cursor, keyboard, mouse, clipboard, IME | Japanese/Chinese IME input works |
| 4 | TextMate grammars + VS Code themes | Golden tests green for 10 languages |
| 5 | Features: find/replace, folding, brackets, auto-indent, comment | One feature = one documented folder |
| 6 | Minimap, sticky scroll, word wrap, whitespace, guides | — |
| 7 | Full UI Automation accessibility | Narrator reads and navigates the content |
| 8 | LSP client (completion, hover, diagnostics, go-to-definition) | Demo with a real language server |
| 9 | Diff editor | — |

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
