# Architecture

LoomEditor is a port of the Monaco editor (`microsoft/vscode`, folder `src/vs/editor`) to C# and WinUI 3.
This page tells you **where things are** and **which rules keep the code base easy to change**.

## Layers

```
 ┌──────────────────────────────────────────────────────────────┐
 │ LoomEditor.WinUI        the LoomEditor control: XAML, input,  │  net11.0-windows
 │                         IME, accessibility                    │
 ├──────────────────────────────────────────────────────────────┤
 │ LoomEditor.Rendering    pixels: IRenderSurface + Win2D        │  net11.0-windows
 ├──────────────────────────────────────────────────────────────┤
 │ LoomEditor.View         cursors, commands, layout, features   │  net11.0
 ├───────────────────────────────┬──────────────────────────────┤
 │ LoomEditor.TextMate           │ LoomEditor.Themes             │  net11.0
 │ grammars + Oniguruma          │ VS Code color themes          │
 ├───────────────────────────────┴──────────────────────────────┤
 │ LoomEditor.Core         Position, Range, Selection, TextEdit, │  net11.0
 │                         TextModel, piece tree, undo           │
 └──────────────────────────────────────────────────────────────┘
```

| Project | Contains | May reference (directly) |
|---|---|---|
| `LoomEditor.Core` | value types, text model, piece tree, undo, decorations | nothing |
| `LoomEditor.TextMate` | TextMate grammars, Oniguruma interop | Core |
| `LoomEditor.Themes` | VS Code themes → `EditorTheme`, `ThemeColor` | Core |
| `LoomEditor.View` | cursors, commands, view layout, **features** | Core, TextMate, Themes |
| `LoomEditor.Rendering` | `IRenderSurface`, `TextViewRenderer`, Win2D backend | View |
| `LoomEditor.WinUI` | `LoomEditor` control, input, accessibility | Rendering |

Monaco equivalents: Core ≈ `editor/common/core` + `editor/common/model`; View ≈ `editor/common/cursor`,
`editor/common/viewModel`, `editor/contrib`; Rendering + WinUI ≈ `editor/browser`.

## The rules (checked automatically)

| # | Rule | Checked by |
|---|---|---|
| 1 | **The core does not know WinUI.** Core, TextMate, Themes and View target plain `net11.0`, reference no UI package, and their assemblies reference only `System.*` and `LoomEditor.*`. | `tests/LoomEditor.Architecture.Tests/DependencyRulesTests.cs` |
| 2 | **Dependencies go one way** (table above). Changing the table = changing the architecture: write an ADR. | same |
| 3 | **No run-time reflection.** No `dynamic`, no `Activator`, no `Type.GetType(string)`, no reflection-based JSON. Use source generators: `JsonSerializerContext`, `[GeneratedRegex]`, `[LibraryImport]`, `x:Bind`. | AOT/trim analyzers (`IsAotCompatible`), `src/BannedSymbols.txt`, CI AOT publish |
| 4 | **Explicit composition.** No DI container, no assembly scanning. Enabled features are listed in `EditorFeatures.cs`. | code review, `ConventionTests` |
| 5 | **One feature = one folder** under `LoomEditor.View/Features/`, with `XxxFeature.cs`, `XxxCommands.cs`, `README.md`. | `ConventionTests.cs` |
| 6 | **Ported files say where they come from** (header with Monaco path and upstream commit). | `ConventionTests.cs` |
| 7 | **Minimal, tracked public API.** Everything is `internal` unless needed; public members are listed in `PublicAPI.*.txt`. | PublicApiAnalyzers (RS0016/RS0017) |
| 8 | **Values are immutable**: `Position`, `Range`, `Selection`, `TextEdit` are `readonly record struct`. | code review |
| 9 | **Threading is documented.** The model is changed on the UI thread only. Tokenization runs in the background, incrementally, cancellably, and publishes results on the UI thread. Each class states its thread contract in its XML doc. | code review |

## How a key press becomes pixels (phase 0)

```
KeyDown (WinUI)            LoomEditor.OnKeyDown
  → KeyboardInput          Ctrl+A → "editor.action.selectAll"
  → CommandRegistry        runs the command registered by SelectAllFeature
  → EditorContext          Selection = whole document → SelectionChanged
  → CanvasControl          Invalidate() → Draw
  → TextViewRenderer       draws selection, line numbers, text on an IRenderSurface
  → Win2DRenderSurface     Direct2D / DirectWrite
```

## Extension model: features

A feature (Find, Folding, BracketMatching…) implements `IEditorFeature`:

```csharp
public interface IEditorFeature : IDisposable
{
    string Id { get; }                       // "loom.folding"
    void Attach(IEditorContext context);     // subscribe to events, register commands
}
```

`EditorFeatureHost` creates the features listed in `EditorFeatures.Default` (or the list given to the control's
`Features` property), attaches them, and disposes them when the control is unloaded.
Step-by-step guide: [how-to/add-a-feature.md](how-to/add-a-feature.md).

## Where do I find…?

| I want to… | Go to |
|---|---|
| change how text is stored or edited | `src/LoomEditor.Core/Model/` |
| change a value type (`Range`…) | `src/LoomEditor.Core/Text/` |
| add or fix an editor feature | `src/LoomEditor.View/Features/<Name>/` |
| add a command or shortcut | the feature's `<Name>Commands.cs`; shortcuts in `src/LoomEditor.WinUI/Input/` (phase 3: `View/Commands/DefaultKeybindings.cs`) |
| change how things are drawn | `src/LoomEditor.Rendering/TextViewRenderer.cs` |
| change the Win2D backend | `src/LoomEditor.Rendering/Win2D/` |
| change the control's XAML template | `src/LoomEditor.WinUI/Themes/Generic.xaml` |
| add syntax highlighting for a language | `src/LoomEditor.TextMate/` (phase 4), see [how-to/add-a-language.md](how-to/add-a-language.md) |
| understand a word | [GLOSSARY.md](GLOSSARY.md) |
| know why something is the way it is | [adr/](adr/) |
