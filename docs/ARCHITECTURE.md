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
| 7 | **Minimal, tracked public API.** Everything is `internal` unless needed; public members are listed in `PublicAPI.*.txt`. `LoomEditor.View` and `LoomEditor.Rendering` share internals with the assemblies above them (ADR 0005). | PublicApiAnalyzers (RS0016/RS0017) |
| 8 | **Values are immutable**: `Position`, `Range`, `Selection`, `TextEdit` are `readonly record struct`. | code review |
| 9 | **Threading is documented.** The model is changed on the UI thread only. Tokenization runs in the background, incrementally, cancellably, and publishes results on the UI thread. Each class states its thread contract in its XML doc. | code review |

## How a key press becomes pixels (phase 0, select all)

```
KeyDown (WinUI)            LoomEditor.OnKeyDown
  → KeyboardInput          Ctrl+A → "editor.action.selectAll"
  → CommandRegistry        runs the command registered by SelectAllFeature
  → EditorContext          Selection = whole document → SelectionChanged
  → CanvasControl          Invalidate() → Draw
  → TextViewRenderer       draws selection, line numbers, text on an IRenderSurface
  → Win2DRenderSurface     Direct2D / DirectWrite
```

## How a key press becomes an edit (phase 3)

```
KeyDown (WinUI)              LoomEditor.OnKeyDown            ignored while an IME composition is active
  → KeyboardInput            key + modifiers → Keybinding
  → KeybindingResolver       Keybinding + KeybindingContext (the `when` clauses) → command id
  → CommandRegistry          runs the command (CoreNavigationCommands / CoreEditingCommands, or a feature's)
  → ViewModel                CursorMoveCommands, TypeOperations, DeleteOperations… → CursorsController
  → CommandExecutor          IEditCommands → TextModel.PushEditOperations (one undo step) → ResultingSelection
  → events                   ContentChanged, CursorStateChangedEvent (outgoing), view events
  → reveal + redraw          RevealCalculator scrolls to keep the cursor visible, CanvasControl.Invalidate()
```

Plain typing does not go through commands: `CharacterReceived` → `TextInputBuffer` → `ViewModel.Type`. IME, emoji panel
and other text services use `CoreTextEditContext` (Windows 11): `ImeInput` → `EditContextModel` → `ViewModel.Type` /
`CompositionType`; its underlines and candidate window position come from the same `ImeInput`.

The mouse: pointer events → `PointerInput` → `MouseTargetFactory` (hit test) → `MouseDownOperation` → `ViewController`
→ cursor commands. The clipboard: `ClipboardCommands` (copy, cut, paste) with `WindowsClipboardService`.

## How a frame is drawn (phase 2)

```
Wheel / scrollbar / ScrollTop   LoomEditor (WinUI)      OnPointerWheelChanged, ScrollBar.ValueChanged, ScrollTop setter
  → MouseWheelScrolling         wheel delta → new scroll position (pure arithmetic, MouseWheelClassifier for smooth)
  → ViewLayout                  validates and stores the position in its Scrollable (smooth scrolling animates it)
  → Scroll event                the control updates the scrollbars and calls CanvasControl.Invalidate()
  → TextViewRenderer.Render     ViewModel.GetViewportData: visible lines, offsets relative to the scroll position
                                line layouts from the LineLayoutCache (keyed by line text), widest line → ViewLayout
  → Win2DRenderSurface          Direct2D / DirectWrite draw: selection, line highlight, text, caret, gutter
```

The caret blinks on its own: a `DispatcherQueueTimer` ticks `CursorBlinking` only while the editor has the focus, and
every tick invalidates the canvas. A font, tab size or DPI change goes through `RecreateTextMeasurer`: new measurer,
`ViewModel.SetFontInfo`, `TextViewRenderer.OnFontChanged` (cache cleared), first visible line kept.

## How a line gets its colors (phase 4)

```
LanguageId / Theme (control)     LoomEditor (WinUI)      → SyntaxHighlighter.SetLanguage / SetTheme (View/Highlighting)
  → TMGrammarFactory             grammar of the language (embedded .tmLanguage.json, regexes on native Oniguruma)
  → worker thread                one per editor: TextMateWorkerTokenizer keeps a mirror of the text and its own
                                 state store, tokenizes in slices of 20 ms, batches of at most 200 lines
  → UI thread                    ThreadedBackgroundTokenizerFactory: batches arrive through the dispatcher
  → token store                  TextModel.Tokenization (Core/Model/Tokens): tokens + one state per line,
                                 TokensChanged event, BackgroundTokenizationState
  → ViewModel                    OnModelTokensChanged → redraw; GetViewLineTokens(line) for the renderer
  → TextViewRenderer             token foreground / font style from the registry's color map (the theme)
```

The visible lines come first: when the viewport changes, the view model calls `SetVisibleLines` on its attached view,
and the model tokenizes the lines it does not know yet **on the UI thread** with Monaco's heuristics (a few lines
before the viewport, then `TokenizeLine2`), so scrolling never waits for the worker. An edit tells the worker (the model
change is mirrored) and invalidates the states after it. Without a dispatcher there is no worker and only the visible
lines are tokenized. Benchmarks: [performance.md](performance.md). To add a language: [how-to/add-a-language.md](how-to/add-a-language.md).

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

## How a feature reacts to the editor (phase 5)

```
TextModel events (ContentChanged, DecorationsChanged, tokens)   ViewModel events (cursor, hidden areas, focus)
  → the feature                  subscribes in Attach, owns its state; pure logic stays in a model class
  → IEditorScheduler             RunOnceScheduler / Delayer delay the work (a DispatcherQueueTimer in the control,
                                 ManualEditorScheduler in tests); a feature never owns a timer
  → model decorations            TextModel.DeltaDecorations / EditorDecorationsCollection (interval tree, ranges follow edits)
  → view-model decorations       the ViewModel maps them to view lines (InlineModelDecorationsComputer)
  → DecorationStyles             decoration class name → colors, borders, gutter icons, after-content text
  → TextViewRenderer             draws backgrounds, inline styles, gutter icons, after-content
```

Folding is the longest example:

```
text change / language          FoldingFeature (debounced: 300 ms, then 200 ms)
  → IndentRangeProvider         ranges from the indentation (and #region markers) → FoldingRanges.SanitizeAndMerge
  → FoldingModel                regions + collapse state, decorations from FoldingDecorationProvider (chevrons, "⋯")
  → HiddenRangeModel            the ranges of the collapsed regions, merged
  → ViewModel.SetHiddenAreas    the model lines to hide
  → ViewModelLinesFromProjectedModel   view lines (hidden model lines are not view lines); CoordinatesConverter
  → LinesLayout / TextViewRenderer     only view lines have a height and are drawn; the cursor moves out of folded code
```

The same view lines will carry word wrap in phase 6 (cut points: [PORTING.md](PORTING.md#cut-points-for-phase-6)).
Clicks on the chevrons and on the `⋯` come back through the mouse hit test (`MouseTargetFactory`) to `FoldingFeature.Mouse.cs`.
Design: [ADR 0009](adr/0009-editor-features.md).

## Where do I find…?

| I want to… | Go to |
|---|---|
| change how text is stored or edited | `src/LoomEditor.Core/Model/` |
| change the text model (lines, EOL, edits, events) | `src/LoomEditor.Core/Model/` (`TextModel*.cs`) |
| change undo / redo | `src/LoomEditor.Core/Undo/` |
| change the piece tree (text storage) | `src/LoomEditor.Core/Model/PieceTree/` |
| change a value type (`Range`…) | `src/LoomEditor.Core/Text/` |
| add or fix an editor feature | `src/LoomEditor.View/Features/<Name>/` |
| add a command or shortcut | the feature's `<Name>Commands.cs`; shortcuts in `src/LoomEditor.WinUI/Input/` (phase 3: `View/Commands/DefaultKeybindings.cs`) |
| change cursors, selections, how typing / deleting / indenting / paste behave | `src/LoomEditor.View/Cursors/` (`CursorsController`, `TypeOperations`, `DeleteOperations`, `CursorMoveCommands`…) |
| change a command or a default shortcut | `src/LoomEditor.View/Commands/` (`CoreNavigationCommands`, `CoreEditingCommands`, `DefaultKeybindings.cs`, `KeybindingResolver`) |
| change mouse selection or hit testing | `src/LoomEditor.View/Controller/` (`MouseTargetFactory`, `MouseDownOperation`, `ViewController`), `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs` |
| change the clipboard | `src/LoomEditor.View/Controller/ClipboardData.cs`, `ClipboardCommands.cs`, `src/LoomEditor.WinUI/Input/WindowsClipboardService.cs` |
| change IME input (composition, candidate window, underlines) | `src/LoomEditor.View/Controller/EditContext*.cs`, `src/LoomEditor.WinUI/Input/ImeInput.cs`, `src/LoomEditor.WinUI/Controls/LoomEditor.Ime.cs` |
| change tracked ranges (ranges that follow edits) | `src/LoomEditor.Core/Decorations/` (interval tree), `src/LoomEditor.Core/Model/TextModel.TrackedRanges.cs` |
| change how things are drawn | `src/LoomEditor.Rendering/TextViewRenderer.cs` |
| change the Win2D backend | `src/LoomEditor.Rendering/Win2D/` |
| change scrolling, wheel or the scroll position | `src/LoomEditor.View/Layout/` (`Scrollable`, `ViewLayout`, `MouseWheelScrolling`); the control side is `src/LoomEditor.WinUI/Controls/LoomEditor.Scrolling.cs` |
| change the layout (line heights, content size, visible lines) | `src/LoomEditor.View/Layout/` (`LinesLayout`, `LineHeightsManager`, `ViewLayout`, `ViewModel`) |
| change the tokens of a model (token store, states, `TokensChanged`, viewport-first tokenization) | `src/LoomEditor.Core/Model/Tokens/`, `src/LoomEditor.Core/Tokens/`, `src/LoomEditor.Core/Model/TextModel.Tokenization.cs` |
| change how a grammar is loaded or a line is tokenized | `src/LoomEditor.TextMate/Grammar/` (vscode-textmate), `Parsing/`, `Tokenization/` (`TMGrammarFactory`, `TextMateTokenizationSupport`) |
| change the background tokenization thread | `src/LoomEditor.TextMate/Tokenization/Worker/`, `ThreadedBackgroundTokenizerFactory.cs`, `TextMateWorkerTokenizerController*.cs` |
| add or fix a grammar / a language | `src/LoomEditor.TextMate/Grammars/<language>/` and `BuiltInLanguages.cs` ([how-to](how-to/add-a-language.md)) |
| change the regex engine (Oniguruma) | `src/LoomEditor.TextMate/Oniguruma/`, `src/native/oniguruma/` ([ADR 0007](adr/0007-textmate-regex-engine.md)) |
| change a theme or how themes are loaded | `src/LoomEditor.Themes/` (`VsCodeThemeLoader`, `EditorTheme`, `BuiltIn/*.json`), `src/LoomEditor.View/Highlighting/ThemeToTextMate.cs` |
| wire highlighting into the control (`LanguageId`, `Theme`) | `src/LoomEditor.View/Highlighting/SyntaxHighlighter.cs`, `src/LoomEditor.WinUI/Controls/LoomEditor.Highlighting.cs` |
| regenerate the golden token files | `eng/golden/` (`generate.mjs`), tests in `tests/LoomEditor.TextMate.Tests/Golden/` |
| change the caret or its blinking | `src/LoomEditor.Rendering/CursorBlinking.cs` (state machine), `TextViewRenderer.cs` (drawing), `src/LoomEditor.WinUI/Controls/LoomEditor.Caret.cs` (timer and focus) |
| change font measurement, tab size or DPI handling | `src/LoomEditor.View/Layout/FontMeasurements.cs`, `src/LoomEditor.Rendering/Win2D/Win2DTextMeasurer.cs`, `src/LoomEditor.WinUI/Controls/LoomEditor.Font.cs` |
| change the control's XAML template | `src/LoomEditor.WinUI/Themes/Generic.xaml` |
| add syntax highlighting for a language | `src/LoomEditor.TextMate/` (phase 4), see [how-to/add-a-language.md](how-to/add-a-language.md) |
| change decorations (model side: ranges, options, change events) | `src/LoomEditor.Core/Decorations/`, `TextModel.Decorations*.cs`; the per-feature handle is `src/LoomEditor.View/Features/EditorDecorationsCollection.cs` |
| change how a decoration looks (class name → colors, gutter icon, after-content) | `src/LoomEditor.View/Layout/DecorationStyles.cs`, drawing in `src/LoomEditor.Rendering/TextViewRenderer.Decorations.cs`, `.GutterDecorations.cs` |
| change the language configuration (brackets, comments, auto-closing, on-enter and indent rules) | `src/LoomEditor.Core/Languages/` (`LanguageConfigurationRegistry`, `AutoIndent*.cs`), the JSON files in `src/LoomEditor.TextMate/Grammars/<language>/` |
| change bracket pairs (matching, colorization, the bracket tree) | `src/LoomEditor.Core/Model/BracketPairs/` (`Tree/` is the parser), `src/LoomEditor.View/Features/BracketMatching/` |
| change find / replace (logic) | `src/LoomEditor.Core/Model/Search/` (`TextModelSearch`), `src/LoomEditor.View/Features/Find/` (`FindModel`, `FindController`, `ReplacePattern`) |
| change the find widget | `src/LoomEditor.WinUI/Controls/Find/` (`FindWidget*.cs`), `LoomEditor.Find.cs` |
| change folding | `src/LoomEditor.View/Features/Folding/` ([README](../src/LoomEditor.View/Features/Folding/README.md)) |
| change hidden areas / view lines (what is a drawn line) | `src/LoomEditor.View/Layout/` (`ViewModel`, `ViewModelLinesFromProjectedModel*.cs`) |
| change word highlight (F7) | `src/LoomEditor.View/Features/WordHighlighter/` |
| delay work in a feature | `IEditorContext.Scheduler`, `src/LoomEditor.View/Features/Scheduling/` |
| add a shortcut or a chord (Ctrl+K Ctrl+…) | `src/LoomEditor.View/Commands/DefaultKeybindings.cs`, `KeybindingResolver`, `KeybindingDispatcher` |
| understand a word | [GLOSSARY.md](GLOSSARY.md) |
| know why something is the way it is | [adr/](adr/) |
