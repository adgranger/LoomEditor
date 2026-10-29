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
| `LoomEditor.Lsp` | LSP client: JSON-RPC over stdio, document sync, mapping to the language features | Core |

`LoomEditor.Lsp` (language server client) references Core only and sits beside View: the Gallery wires it to the control (ADR 0011).

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

Word wrap uses the same view lines (next section).
Clicks on the chevrons and on the `⋯` come back through the mouse hit test (`MouseTargetFactory`) to `FoldingFeature.Mouse.cs`.
Design: [ADR 0009](adr/0009-editor-features.md).

## View lines with word wrap and injected text (phase 6)

```
model line + injected text      TextModel.GetLineInjectedText → LineInjectedText (from decorations with `after` / `before` options)
  → ILineBreaksComputer         MonospaceLineBreaksComputer: break offsets from a fixed-width approximation (wrap column,
                                wrapping indent, injected text widths); incremental when only a few characters changed
  → ModelLineProjectionData     break offsets, injection offsets and the offset ↔ view position math
  → WrappedModelLineProjection  one model line as N view lines (indent, content, injected text → inline decorations)
  → ViewModelLinesFromProjectedModel   all projections + hidden areas; the CoordinatesConverter (model ↔ view positions)
  → ViewModel                   word wrap settings, view events, cursor movement over view lines
  → LinesLayout / TextViewRenderer     view lines are drawn; mouse hit tests and reveal use view positions
```

The wrapping column comes from `EditorLayoutInfoComputer` (viewport width, minimap, scrollbars) and the control's `WordWrap*`
properties. The renderer measures wrapped lines with the text measurer, so the drawn width can differ slightly from the
fixed-width approximation (see "Behavior differences" in [PORTING.md](PORTING.md)).

The other view extras sit on the same view model:

- **Guides:** the model computes them (`GuidesTextModelPart`: indent and bracket pair guides, active guide); `ViewModelLinesFromProjectedModel.Guides.cs`
  maps them to view lines, `IndentGuidesComputer` and `GuideColors` give the data, `TextViewRenderer.Guides.cs` draws them.
- **Overview ruler:** decoration options (`overviewRuler`) → `ViewModel.GetAllOverviewRulerDecorations` (groups per color,
  `OverviewRulerDecorations*.cs`) → `TextViewRenderer.OverviewRuler.cs`. Find, highlights and bracket matching add their marks.
- **Minimap:** `LoomEditor.View/Minimap/` is the logic (`MinimapModel` over the view model, `MinimapLayout`,
  `MinimapSamplingState`, char renderer and sheet, `MinimapLinesRenderer`, `MinimapDecorationsRenderer`, `MinimapPointerController`);
  `TextViewRenderer.Minimap.cs` draws it with Win2D (pixel buffer, slider, shadow) and `LoomEditor.Minimap.cs` holds the control properties.
- **Sticky scroll:** the `StickyScroll` feature (indentation-based model → candidates → widget state, publishes `ViewModel.StickyScroll`);
  `TextViewRenderer.StickyScroll.cs` draws the lines, `StickyScrollFeature.Mouse.cs` handles clicks.

## Accessibility (phase 7)

```
TextModel + ViewModel           the text and the view lines (word wrap, folding)
  → AutomationTextDocument      the model text as `
`-joined lines (LoomEditor.View/Accessibility/, no WinUI types)
  → AutomationTextRange         a range: expand / move by the seven units (`*UnitStarts`), find, selection
  → AutomationTextGeometry      bounding rectangles and points, through IAutomationLineMetrics (RendererLineMetrics in WinUI)
  → LoomEditorTextRangeProvider ITextRangeProvider over an AutomationTextRange   (LoomEditor.WinUI/Accessibility/)
  → LoomEditorAutomationPeer    Text, Text2 and Value patterns, returned to the UIA clients (Narrator, NVDA)
```

Events path: `EditorContext.SelectionChanged` / `Model.ContentChanged` → `AutomationEventCoalescer` (one event per burst) →
`IAutomationEventSink` → `AutomationEventSink` → `RaiseAutomationEvent`.
Announcements path: features call `IEditorAnnouncer` (find results, word highlight navigation, tab focus mode; the message
texts are `AccessibilityMessages`) → `AutomationAnnouncer` → `RaiseNotificationEvent`.

Four more pieces sit beside this chain:

- **Screen reader mode:** `EditorAccessibilityOptions` (`Auto` / `On` / `Off`, `ScreenReaderDetector` for `Auto`) forces wrapping indent `None`
  and wraps long-line models (`TextModel.IsDominatedByLongLines`) at the viewport (`EditorLayoutOptions.GetEffectiveWrapping`).
- **Tab focus mode:** the `TabFocus` feature (`Ctrl+M`) sets `EditorContext.TabMovesFocus`, the control property `TabMovesFocus` lets Tab leave the editor.
- **High contrast:** `SystemHighContrastThemeFactory` builds an `EditorTheme` from the Windows system colors; the control follows
  them (`UseSystemHighContrast`); `editor.selectionForeground` colors the selected text in `TextViewRenderer.Selection.cs`.
- **Self-check:** the Gallery runs `UiaSelfCheck` (an in-process UIA client) and has the accessibility switches.

Design: [ADR 0010](adr/0010-accessibility.md). Manual checks with Narrator and NVDA: [testing/phase-7-manual-tests.md](testing/phase-7-manual-tests.md).

## Language features and the language server client (phase 8)

Three layers, then a separate project that speaks LSP ([ADR 0011](adr/0011-language-server-client.md)):

```
LoomEditor.Core    provider contracts (ICompletionItemProvider, IHoverProvider, IDefinitionProvider), LanguageFeatureRegistry<T>,
                   LanguageFeaturesService, MarkerService, MarkerDecorations, the fuzzy filter       (public API: applications register providers)
  → LoomEditor.View    SuggestModel / CompletionModel / SuggestController, ContentHoverComputer / HoverController,
                       GotoDefinitionController, squiggle styles (DecorationStyles), the Markdown subset parser
  → LoomEditor.WinUI   OverlayHost (the overlay layer in the control template), HoverWidget, SuggestWidget

LoomEditor.Lsp (beside View, references Core only):
  LspMessageReader/Writer (Content-Length framing) → JsonRpcConnection → LspClient (child process, initialize)
    → LspDocumentSync, LspCompletionProvider / LspHoverProvider / LspDefinitionProvider, LspDiagnosticsBinder
    → LspEditorSession (one call connects an editor to a server)
```

Typing a letter until the completion list shows:

```
ViewModel.Type  → DidType
  → SuggestModel                    quick suggest timer (IEditorScheduler), trigger characters
  → SuggestItems.ProvideAsync       asks the registered providers (UI thread), returns a Task
  → LspCompletionProvider           builds `textDocument/completion`
  → JsonRpcConnection               write queue → server stdout/stdin ← read loop (background threads)
  ← server answer                   the Task completes on any thread
  ← UiContinuation.Then             hops back to the UI thread through IUiDispatcher, re-checks the request is current
  → CompletionModel                 filter (FuzzyScore) and sort
  → SuggestController               list state, accept, additional edits
  → ISuggestPresenter → SuggestWidget   rows drawn by hand in the overlay layer
```

**Threading** (ADR 0011 decision 4): the editor (model, decorations, markers, features, widgets) is UI thread only. The LSP client has one
read loop and one ordered write queue; a `didChange` sent before a request always reaches the server first. Providers are called on the UI thread
and their `Task` may complete anywhere: View code never uses `await` on it, only `UiContinuation.Then`. Server notifications reach the editor through the
`post` delegate of `LspEditorSession` (the Gallery passes `DispatcherQueue.TryEnqueue`). Conversions in a provider continuation never touch the `TextModel`.

**The fake language server** (`tests/LoomEditor.Lsp.FakeServer`) is a deterministic server (completion, resolve, hover, definition, diagnostics). Tests run it in
process over in-memory pipes (`tests/LoomEditor.Lsp.Tests`, and the end-to-end tests of the real features in `tests/LoomEditor.View.Tests/Lsp`); a few start it as a child process; the Gallery self-check runs it published with Native AOT. No automated test needs a real server.

Deliberately not delivered in phase 8: snippets (delivered in phase 10), signature help, references, rename, code actions, formatting, semantic tokens, inlay hints, folding ranges from the server, peek definition.

## Diff editor (phase 9)

Four layers ([ADR 0012](adr/0012-diff-editor.md)); every class is `internal` except `LoomDiffEditor` and its few enums and properties:

```
LoomEditor.Core    Diff/: DefaultLinesDiffComputer (Myers, dynamic programming, heuristics, moved lines), the range mappings,
                   DocumentDiffProvider; Text/: LineRange, OffsetRange                           (pure: string[] in, mappings out)
  → LoomEditor.View    DiffEditor/: DiffEditorOptions, DiffState, DiffEditorViewModel, UnchangedRegion, line range alignment,
                       decorations, view zone specs, DiffNavigator, DiffOverviewZones, AccessibleDiffViewerModel
                       Layout/: ViewZones (gaps with a purpose), OverviewZoneManager, ScrollbarState
                       Features/DiffNavigation/ (Alt+F5, F7), read-only mode in EditorContext
  → LoomEditor.Rendering   view zones drawn by hand (diagonal fill, deleted lines, collapsed bar), OverviewRulerPainter
  → LoomEditor.WinUI       LoomDiffEditor: two LoomEditor controls, the sash, the zone controllers, the overview strip,
                           the accessible viewer (Controls/DiffEditor/)
```

From text to pixels:

```
TextModel (original) ┐
TextModel (modified) ┴→ DiffEditorViewModel         debounce 200 ms (IEditorScheduler), cancels the previous request
  → DocumentDiffProvider.ComputeDiffAsync          Task.Run over copies of the lines; result back on the UI thread (IUiDispatcher)
  → DiffState (mappings, moves, identical)  +  UnchangedRegions (when HideUnchangedRegions is on)
  ├→ DiffDecorationsComputer → line and character decorations in both editors
  ├→ LineRangeAlignments + DiffEditorViewZones       hatched gaps (side by side) / deleted-lines zones (inline); equal ScrollTop
  ├→ HideUnchangedRegionsController                  hidden areas + "N hidden lines" bars, pointer handling
  ├→ DiffOverviewZones → OverviewZoneManager → OverviewRulerPainter   the strip and the viewport slider (ScrollbarState)
  └→ AccessibleDiffViewerModel → AccessibleDiffViewer (list of rows, UIA names from AccessibilityMessages)
```

**Threading**: everything is UI thread only except the diff computation itself (a pool thread working on copies of the lines). While a new diff
is computed the old one stays (`IsDiffUpToDate` is false): Monaco patches it through the edit, phase 9 does not.

**Not delivered in phase 9**: moved blocks drawing (the computation is ported), the gutter menu and revert buttons, `diffCodeLens`, the embedded diff editor,
`compactMode`, true inline view, the legacy diff computer, `serializeState`, accessibility sounds, mapping the old diff through edits, `setModel`, breadcrumbs.
How to use the control: [how-to/use-the-diff-editor.md](how-to/use-the-diff-editor.md).

## Text rendering options and snippets (phase 10)

Decisions in [ADR 0013](adr/0013-text-options-and-snippets.md). Three flows; each editor has its own state, nothing is a singleton:

```
Fonts:      control properties (FontLigatures, FontFeatureSettings, FontVariationSettings, LetterSpacing, FontWeight, ZoomLevel)
  → EditorFontOptions + EditorZoomState → BareFontInfo.Create (zoom factor) → RecreateTextMeasurer
  → FontInfo (width table: word wrap, minimap)  +  CanvasTypography / SetCharacterSpacing on each CanvasTextLayout (ligatures, spacing, wght)
  positions always come from ILineLayout.GetCharacterX / HitTest

Whitespace: RenderWhitespaceComputer (pure: the ranges of a line) → overlay drawn over the text layout (dots, arrows)
            control characters are substituted 1:1 before the layout is built

Snippets:   LoomEditor.Core/Snippets (scanner, SnippetParser, TextmateSnippet, variable resolvers)
  → LoomEditor.View/Features/Snippet (OneSnippet, SnippetSession, SnippetController: tab stops tracked by model decorations)
  ← CompletionItem.InsertTextRules (InsertAsSnippet) from the suggest widget and from LspConvert
```

The input mode (insert / overtype) is `EditorContext.InputMode`, one per editor.

Editing features added in phase 10, each a folder of `LoomEditor.View/Features/` (feature, commands, README): `SelectionAnchor`, `LineSelection`, `CursorUndo`, `CaretOperations`
(move selected text), `SmartSelect`, `DragAndDrop` (the drag state lives in the mouse operation of `LoomEditor.View/Controller/`), `Overtype`, `FontZoom`, `Snippet` and `GotoError`.
A key that depends on a state of the editor reads a field of `KeybindingContext` (`InSnippetMode`, `HasNextTabstop`, `MarkersNavigationVisible`…), filled by the control.

```
Problems:   MarkerService → MarkerList (sorted, wraps around) → MarkerController (F8: caret, reveal, announcement; Escape ends the session)
Snippet mode: SnippetController (keys Tab / Shift+Tab / Escape, undo and caret tracking) → SnippetSession (every cursor) → OneSnippet (decorations)
```

**Not delivered in phase 10**: the "Later" table of [tasks/phase-10/README.md](tasks/phase-10/README.md) (unicode highlight, font tokens of themes, semantic highlighting, rich copy, touch, linked editing,
formatting and the other provider based features), the choice list and snippet text edits, the marker widget of `F8`. The Native AOT text self-check passes (10 of 10, P10-30, 2026-10-29,
[roadmap](roadmap/phase-10-editor-completeness.md)). **Not verified**: the manual plan has not been run.
How to use it: [how-to/use-snippets.md](how-to/use-snippets.md), [how-to/font-and-whitespace-options.md](how-to/font-and-whitespace-options.md).

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
| change word wrap (line breaks, wrapped view lines, wrapping indent) | `src/LoomEditor.View/Layout/` (`MonospaceLineBreaksComputer*.cs`, `ModelLineProjection*.cs`, `WrappedModelLineProjection*.cs`, `ViewModelLinesFromProjectedModel.Wrapping.cs`, `ViewModel.Wrapping.cs`), control properties in `src/LoomEditor.WinUI/Controls/LoomEditor.WordWrap.cs` |
| change injected text (`after` / `before` decoration text) | `src/LoomEditor.Core/Model/Events/LineInjectedText.cs`, `src/LoomEditor.View/Layout/InjectedTextInlineDecorationsComputer.cs`, drawing in `TextViewRenderer.Decorations.cs` |
| change indent or bracket pair guides | `src/LoomEditor.Core/Model/Guides/` (model), `src/LoomEditor.View/Layout/` (`IndentGuidesComputer`, `GuideColors`), `src/LoomEditor.Rendering/TextViewRenderer.Guides.cs` |
| change the overview ruler | `src/LoomEditor.View/Layout/OverviewRulerDecorations*.cs`, `src/LoomEditor.Rendering/TextViewRenderer.OverviewRuler.cs`, `OverviewRulerSettings.cs` |
| change the minimap | `src/LoomEditor.View/Minimap/` (logic), `src/LoomEditor.Rendering/TextViewRenderer.Minimap.cs` (drawing), `src/LoomEditor.WinUI/Controls/LoomEditor.Minimap.cs` |
| change sticky scroll | `src/LoomEditor.View/Features/StickyScroll/` ([README](../src/LoomEditor.View/Features/StickyScroll/README.md)), `src/LoomEditor.Rendering/TextViewRenderer.StickyScroll.cs` |
| delay work in a feature | `IEditorContext.Scheduler`, `src/LoomEditor.View/Features/Scheduling/` |
| add a shortcut or a chord (Ctrl+K Ctrl+…) | `src/LoomEditor.View/Commands/DefaultKeybindings.cs`, `KeybindingResolver`, `KeybindingDispatcher` |
| understand a word | [GLOSSARY.md](GLOSSARY.md) |
| know why something is the way it is | [adr/](adr/) |
| change the UIA text pattern, ranges or units (what a screen reader reads and how it moves) | `src/LoomEditor.View/Accessibility/` (`AutomationText*.cs`, `*UnitStarts.cs`), `src/LoomEditor.WinUI/Accessibility/` (`LoomEditorAutomationPeer.cs`, `LoomEditorTextRangeProvider.cs`); spec in `docs/tasks/phase-7/README.md` §4 |
| change announcements or UIA events | `src/LoomEditor.View/Accessibility/` (`AccessibilityMessages.cs`, `IEditorAnnouncer.cs`, `AutomationEventCoalescer.cs`), `src/LoomEditor.WinUI/Accessibility/` (`AutomationAnnouncer.cs`, `AutomationEventSink.cs`) |
| change the accessibility options or the screen reader wrapping | `src/LoomEditor.View/Layout/EditorAccessibilityOptions.cs`, `EditorLayoutOptions.cs`; control property in `src/LoomEditor.WinUI/Controls/LoomEditor.Accessibility.cs` |
| change the tab focus mode | `src/LoomEditor.View/Features/TabFocus/` |
| change the high contrast themes | `src/LoomEditor.Themes/HighContrast/`, `src/LoomEditor.WinUI/Controls/LoomEditor.HighContrast.cs`, `src/LoomEditor.WinUI/Accessibility/SystemHighContrast.cs` |
| run the UIA self-check | `samples/LoomEditor.Gallery/UiaSelfCheck.cs` (the Gallery accessibility switches) |
| add a completion, hover or definition provider, or read the provider contracts (phase 8) | `src/LoomEditor.Core/Languages/Features/` ([README](../src/LoomEditor.Core/Languages/Features/README.md)), [how-to/add-a-language-server.md](how-to/add-a-language-server.md) |
| change markers, squiggles or the ruler marks of markers | `src/LoomEditor.Core/Markers/` (`MarkerService`, `MarkerDecorations`), `src/LoomEditor.View/Layout/DecorationStyles.cs` (`squiggly-*`), `src/LoomEditor.Rendering/UnderlineGeometry.cs` |
| change hover (computation, delays, popup) | `src/LoomEditor.View/Hover/`, `src/LoomEditor.View/Features/Hover/`, `src/LoomEditor.View/Markdown/`, `src/LoomEditor.WinUI/Controls/Hover/` |
| change completion (filter, session, list, accept) | `src/LoomEditor.Core/Filters/` (fuzzy), `src/LoomEditor.View/Suggest/`, `src/LoomEditor.View/Features/Suggest/`, `src/LoomEditor.WinUI/Controls/Suggest/` |
| change go to definition (`F12`, `Ctrl+click`) | `src/LoomEditor.View/Features/GotoDefinition/` (`ILocationOpener`) |
| change the overlay layer (hover and completion hosts) | `src/LoomEditor.WinUI/Controls/Overlay/` (`OverlayHost`), `src/LoomEditor.View/Layout/OverlayPlacement.cs`, `LoomEditor.Overlay.cs` |
| change the LSP client or the editor session | `src/LoomEditor.Lsp/` ([README](../src/LoomEditor.Lsp/README.md)); wire format in `Protocol/`, framing and JSON-RPC in `JsonRpc/` |
| change the fake language server or write an LSP test | `tests/LoomEditor.Lsp.FakeServer/` ([README](../tests/LoomEditor.Lsp.FakeServer/README.md)), helpers in `tests/LoomEditor.Lsp.Tests/TestHelpers/` |
| run the language server demo or the LSP self-check | `samples/LoomEditor.Gallery/Lsp/` (`LspSelfCheck.cs`, `--lsp-selfcheck=<file>`), [testing/phase-8-manual-tests.md](testing/phase-8-manual-tests.md) |
| change the diff algorithm or the document diff provider | `src/LoomEditor.Core/Diff/` (`DefaultLinesDiffComputer`, `Algorithms/`, `DocumentDiffProvider`); Monaco's fixtures in `tests/LoomEditor.Core.Tests/Diff/Fixtures/` |
| change the diff model (options, state, unchanged regions, alignment, decorations, navigation) | `src/LoomEditor.View/DiffEditor/` (`DiffEditorViewModel`, `UnchangedRegion`, `LineRangeAlignment`, `DiffNavigator`), `src/LoomEditor.View/Features/DiffNavigation/` |
| change view zones (gaps, deleted lines, collapsed bars) | model `src/LoomEditor.View/Layout/ViewZones.cs`, drawing `src/LoomEditor.Rendering/TextViewRenderer.ViewZones.cs`, `.CollapsedZones.cs` |
| change the diff control (sash, side by side, inline, overview strip, accessible viewer) | `src/LoomEditor.WinUI/Controls/DiffEditor/` ([how-to](how-to/use-the-diff-editor.md)) |
| change the overview ruler | `src/LoomEditor.View/Layout/OverviewZoneManager.cs`, `ScrollbarState.cs`, `src/LoomEditor.Rendering/OverviewRulerPainter.cs` |
| run the diff demo, the diff self-check or the diff benchmarks | `samples/LoomEditor.Gallery/DiffDemoWindow.xaml`, `DiffSelfCheck.cs` (`--diff-selfcheck=<file>`), `tests/LoomEditor.Benchmarks/DiffBenchmarks.cs`, [testing/phase-9-manual-tests.md](testing/phase-9-manual-tests.md) |
| change font options, ligatures, letter spacing or zoom | `src/LoomEditor.View/Layout/` (`EditorFontOptions`, `EditorZoomState`, `BareFontInfo`), DirectWrite side in `src/LoomEditor.Rendering/Win2D/` ([ADR 0013](adr/0013-text-options-and-snippets.md)) |
| change whitespace or control character rendering | `src/LoomEditor.View/Layout/` (`RenderWhitespaceComputer`), drawing in `src/LoomEditor.Rendering/TextViewRenderer*.cs` |
| change snippets (syntax, variables, session, tab stops) | `src/LoomEditor.Core/Snippets/` (parser), `src/LoomEditor.View/Features/Snippet/` (`OneSnippet`, `SnippetSession`, `SnippetController`; [how-to](how-to/use-snippets.md)) |
| change go to next / previous problem | `src/LoomEditor.View/Features/GotoError/` (`MarkerList`, `MarkerController`), the message in `AccessibilityMessages.MarkerNavigation` |
| change selection commands (anchor, cursor undo, smart select, move selected text) | `src/LoomEditor.View/Features/` (`SelectionAnchor`, `LineSelection`, `CursorUndo`, `SmartSelect`, `CaretOperations`) |
| change drag and drop of the selection | `src/LoomEditor.View/Features/DragAndDrop/`, the drag state in `src/LoomEditor.View/Controller/` |
| run the text self-check (Native AOT) | `samples/LoomEditor.Gallery/TextSelfCheck.cs` (`--text-selfcheck=<file>`), [testing/phase-10-manual-tests.md](testing/phase-10-manual-tests.md) |
| change the input mode (overtype) or the cursor style | `EditorContext.InputMode`, `src/LoomEditor.View/Cursors/`, caret drawing in `src/LoomEditor.Rendering/` |
