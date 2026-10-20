# ADR 0009 — Editor features: decorations, language configuration, bracket pairs, find, folding

- **Status:** accepted
- **Date:** 2026-10-20

## Context

Phase 5 delivers the editor features of the roadmap: find / replace, folding, bracket matching and auto-closing
pairs, auto-indentation, toggle comment and word highlight. Phases 3 and 4 left cut points for them
(`docs/PORTING.md`, "Cut points for phases 4–5"): every one needs Monaco's **language configuration**, and most
features need **model decorations**, which do not exist yet (phase 3 only has tracked ranges).

The maintainer chose on 2026-10-20:

- bracket matching on Monaco's **full bracket pair AST** (`bracketPairsTree/*`), not the old regex search;
- folding from **indentation and `#region` markers** (syntax folding needs a language server: phase 8);
- a **XAML find widget** over the canvas, driven by the ported find model;
- three extras: **Find-based multi-cursor** (`Ctrl+D`, the 73 deferred `multicursor.test.ts` tests), **line
  operations** (move / copy / delete lines…) and the 3 deferred **chunk based search** tests of the piece tree.

## Decisions

1. **Pinned commit.** `microsoft/vscode` `0c9c0a4295a9222e12a605537738719455ae3056`, as in phases 1–4. The
   `language-configuration.json` files come from the same commit (`extensions/<language>/`).

2. **Where things go.**

   | Monaco | LoomEditor | Namespace |
   |---|---|---|
   | `common/model.ts` (`IModelDecorationOptions`…, subset), `common/model/textModel.ts` (decorations part), `decorationProvider.ts` | `LoomEditor.Core/Decorations/`, `LoomEditor.Core/Model/TextModel.Decorations.cs` | `LoomEditor.Core.Decorations` |
   | `common/languages/languageConfiguration.ts`, `languageConfigurationRegistry.ts`, `enterAction.ts`, `autoIndent.ts`, `supports.ts`, `supports/*` (not `tokenization.ts`, `inplaceReplaceSupport.ts`) | `LoomEditor.Core/Languages/` (+ `Supports/`) | `LoomEditor.Core.Languages` |
   | `common/model/bracketPairsTextModelPart/**`, `common/textModelBracketPairs.ts` | `LoomEditor.Core/Model/BracketPairs/` (+ `Tree/`), `TextModel.BracketPairs.cs` | `LoomEditor.Core.Model.BracketPairs` |
   | `common/model/textModelSearch.ts`, `pieceTreeBase.ts` search | `LoomEditor.Core/Model/Search/`, `TextModel.Search.cs`, `PieceTree/PieceTreeBase.Search.cs` | `LoomEditor.Core.Model.Search` |
   | `common/model/indentationGuesser.ts` | `LoomEditor.Core/Model/IndentationGuesser.cs` | `LoomEditor.Core.Model` |
   | `common/viewModel/viewModelLines.ts`, `modelLineProjection.ts`, `viewModelDecorations.ts`, `inlineDecorations.ts` | `LoomEditor.View/Layout/` | `LoomEditor.View.Layout` |
   | `common/cursor/*`, `common/commands/*` additions (auto-closing, surround, electric characters, on-enter…) | `LoomEditor.View/Cursors/` (existing files) | `LoomEditor.View.Cursors` |
   | `contrib/<name>/browser/*` (logic) | `LoomEditor.View/Features/<Name>/` | `LoomEditor.View.Features.<Name>` |
   | `base/common/async.ts` (`RunOnceScheduler`, `Delayer`, `TimeoutTimer`, subset) | `LoomEditor.View/Features/Scheduling/` (not a feature: the convention test skips it) | `LoomEditor.View.Features.Scheduling` |
   | workbench `languageConfigurationExtensionPoint.ts` (the JSON reader) | `LoomEditor.TextMate/Tokenization/LanguageConfigurationReader.cs` | `LoomEditor.TextMate.Tokenization` |
   | `extensions/<language>/language-configuration.json` | `LoomEditor.TextMate/Grammars/<language>/` (embedded, next to the grammar) | — |
   | drawing decorations, folding controls, bracket colors | `LoomEditor.Rendering/` | `LoomEditor.Rendering` |
   | find widget (`findWidget.ts` behavior only, not its DOM) | `LoomEditor.WinUI/Controls/Find/` | `LoomEditor.WinUI.Controls.Find` |

   The dependency table of `ARCHITECTURE.md` does not change.

3. **Model decorations are Monaco's.** `TextModel` gets `DeltaDecorations(ownerId, oldIds, newDecorations)`,
   `ChangeDecorations`, `GetDecorationsInRange`, `GetLineDecorations`, `GetDecorationRange`, `GetDecorationOptions`
   and the `DecorationsChanged` event, on the interval trees of phase 3 (`DecorationsTrees`). The view side is the
   port of `editor.createDecorationsCollection` (`EditorDecorationsCollection`, internal) and `viewModelDecorations.ts`.
   Decorations stay **internal** in phase 5 (no public decorations API).

4. **Decoration styles replace CSS.** `ModelDecorationOptions` keeps Monaco's names (`ClassName`, `InlineClassName`,
   `IsWholeLine`, `GlyphMarginClassName`, `LinesDecorationsClassName`, `AfterContentClassName`, `ZIndex`,
   `Stickiness`, `Description`…). A class name is a **key** into `DecorationStyles` (`LoomEditor.View/Layout/`): a
   table, written by hand from Monaco's CSS files, that says what a class draws (background color id, border color id,
   foreground color id, font style, after-content text like the folded `…`). Color ids are VS Code theme color ids,
   resolved with the current `EditorTheme` (defaults in `EditorColorDefaults`, with Monaco's `transparent` and
   `lessProminent` color transforms). Unknown class names draw nothing. Two rows need more than a fixed color: the
   bracket pair colors (a computed foreground that cycles over the non-transparent `editorBracketHighlight.*` colors)
   and `bracket-match-inline` (an `Important` flag, Monaco's CSS `!important`).
   Injected text (`before` / `after` with content in the model) is **not** ported.

5. **Language configuration is Monaco's, per editor.** `LanguageConfigurationRegistry` is an **instance** (like the
   tokenization registry, ADR 0008 decision 4) given to the `TextModel` and to the cursor configuration. The ten
   built-in languages ship VS Code's `language-configuration.json` **unchanged**; the reader is the port of
   `languageConfigurationExtensionPoint.ts` (`JsonDocument` with comments and trailing commas, as in phase 4).
   `SyntaxHighlighter.SetLanguage` also registers the language configuration.

6. **Regexes are .NET regexes.** Language configuration rules and the find widget use
   `System.Text.RegularExpressions.Regex` (`RegexOptions.CultureInvariant`, plus `IgnoreCase` / `Multiline` from the
   JavaScript flags), never Oniguruma (Core cannot reference TextMate) and never `RegexOptions.ECMAScript` (it forbids
   lookbehind and Unicode classes that the configurations use). The configuration reader translates the few
   JavaScript-only constructs the bundled files use (Markdown's `wordPattern`: `\p{Alphabetic}` →
   `[\p{L}\p{Nl}\p{Mn}\p{Mc}]`, an approximation; `\p{Number}` → `\p{N}`; `\p{Nonspacing_Mark}` → `\p{Mn}`) to .NET
   classes. .NET `\w`, `\d` and `\b` are Unicode where JavaScript's are ASCII: regexes that Monaco writes in code use
   explicit ASCII classes; the bundled files are compiled as they are (a documented behavior difference). JavaScript-only syntax typed in a find query (`\u{…}`, Unicode property names) behaves as .NET does:
   a documented behavior difference. A unit test compiles every regex of every bundled `language-configuration.json`.
   Whole-word search uses Monaco's `WordCharacterClassifier`, not `\b`.

7. **Bracket pairs: the full AST.** `bracketPairsTree/*` (length, AST, tokenizer, parser, `concat23Trees`,
   `beforeEditPositionMapper`, `combineTextEditInfos`, `smallImmutableSet`, `nodeReader`) is ported with its tests. The
   model's `BracketPairs` part (`bracketPairsImpl.ts`) answers `MatchBracket`, `FindEnclosingBrackets`,
   `FindPrevBracket` / `FindNextBracket`, `GetBracketPairsInRange`. As in Monaco, the tree is built lazily on the first
   request for documents up to 5 000 000 characters, whether colorization is on or not; bigger documents use Monaco's
   legacy regex search (ported too). **Bracket pair colorization** is ported (`colorizedBracketPairsDecorationProvider.ts`, default on,
   as in VS Code). Bracket pair **guides** (`guidesTextModelPart.ts`) are phase 6.

8. **Hidden areas without word wrap.** Folding needs view lines ≠ model lines. `viewModelLines.ts`
   (`ViewModelLinesFromProjectedModel`) is ported with only the **identity and hidden projections** of
   `modelLineProjection.ts`: the projection with line breaks (word wrap) is phase 6 and will plug into the same class.
   `IdentityCoordinatesConverter` is replaced by the real converter. Every view-line consumer (layout, viewport,
   tokens, cursors, hit testing, reveal) already goes through `ICoordinatesConverter` or the view model (ADR 0006); the
   cards that change one say which.

9. **Folding ranges come from indentation and markers.** `indentRangeProvider.ts` (with `folding.markers` and
   `offSide` from the language configuration) is the only provider. `syntaxRangeProvider.ts` and the folding range
   providers of language features are phase 8. Folding controls are drawn in the gutter by the renderer
   (`showFoldingControls`: `mouseover`, the VS Code default) and clicked through the existing hit testing.

10. **Timers.** Monaco runs several features after a delay (`RunOnceScheduler`, `Delayer`: find decorations,
    bracket matching 50 ms, word highlight 50 ms, folding 200–300 ms). `IEditorContext` gets an `IEditorScheduler`
    (`Schedule(TimeSpan delay, Action action) → IDisposable`, UI thread; public, like `IEditorContext`). The control
    implements it with a `DispatcherQueueTimer`; the public `EditorContext()` uses a scheduler that never runs (like
    its animation frames); tests use `ManualEditorScheduler` (runs on demand, fake clock). The delays are Monaco's;
    folding's adaptive delay is simplified to 300 ms the first time, then 200 ms. Nothing in a feature uses `Task.Delay`, threads or `DateTime`.

11. **Find widget in XAML.** The find logic is Monaco's (`findState.ts`, `findModel.ts`, `findDecorations.ts`,
    `replacePattern.ts`, `replaceAllCommand.ts`, the commands of `findController.ts`) in
    `LoomEditor.View/Features/Find/`, with its tests. The widget is a WinUI `UserControl` in the control's template,
    top right of the text area, that reads and writes the `FindReplaceState`: find box, replace box, the four toggles
    (match case, whole word, regex, preserve case), find in selection, "n of m", previous / next / close, replace /
    replace all. Its keyboard shortcuts are VS Code's. Not ported: search history, resizing the widget by dragging,
    the overview ruler marks, the accessibility help.

12. **Options.** The new editor options (`autoClosingBrackets`, `autoClosingQuotes`, `autoClosingDelete`,
    `autoClosingOvertype`, `autoSurround`, `autoIndent`, `matchBrackets`, `bracketPairColorization.enabled`,
    `folding`, `showFoldingControls`, `occurrencesHighlight`, `selectionHighlight`, `find.*`, `detectIndentation`,
    `comments.*`) are internal records with VS Code's defaults, like `EditorCursorOptions`. No new public property on
    the control in phase 5 except what a card says.

13. **Two-key chords.** VS Code's `Ctrl+K …` shortcuts (comments, folding, Find-based multi-cursor) need chords,
    which phase 3 does not have: the keybinding resolver gets them first (P5-47).

14. **One feature = one folder** (architecture rule 5): `Find`, `Folding`, `BracketMatching`, `Comment`,
    `Indentation`, `LinesOperations`, `WordHighlighter`, and the existing `MultiCursor` gets the Find-based commands.
    Each has a `README.md` with its Monaco sources and its limits, and is listed in `EditorFeatures.cs`.

## Consequences

- Every phase 3 cut point (`// Phase 4–5 cut point`) is resolved or explicitly moved to a later phase by the end of
  phase 5; `docs/PORTING.md` lists them.
- Phase 6 (word wrap, guides, minimap, sticky scroll) builds on the projections, decorations and bracket pairs of
  this phase.
- The decorations API, the language configuration API and the find widget can become public later without changing
  their shape.
