# Porting Monaco to C#

LoomEditor ports `microsoft/vscode` → `src/vs/editor` (MIT license). This page explains **how** to port a file,
and keeps the **map** from Monaco files to LoomEditor files.

## Reference commit

All ports currently target **`0c9c0a4295a9222e12a605537738719455ae3056`** (vscode `main`, 2026-10-13).
When you port a file, read it at that commit:
`https://github.com/microsoft/vscode/blob/0c9c0a4295a9222e12a605537738719455ae3056/<path>`.
If you deliberately port from a newer commit, write that commit in the file header.

## Method, step by step

1. **Read the Monaco file and its tests first.** Tests are in `src/vs/editor/test/...` with the same path.
2. **Create the C# file in the matching LoomEditor folder** (see the map below) with this header:
   ```csharp
   // Ported from microsoft/vscode: src/vs/editor/common/core/range.ts
   // Upstream commit: <sha>  —  Copyright (c) Microsoft Corporation. MIT License.
   ```
   Use `// Inspired by microsoft/vscode: ...` when the design is adapted rather than translated.
   (`ConventionTests.PortedFiles_StateTheUpstreamCommit` checks the header.)
3. **Port the tests first, then the code.** Keep each Monaco test name in a comment:
   ```csharp
   // Monaco: 'swap start and stop'
   [Fact]
   public void Constructor_EndLineBeforeStartLine_SwapsPositions() { ... }
   ```
4. **Keep Monaco's concepts and behavior; use C# conventions.**

   | TypeScript (Monaco) | C# (LoomEditor) |
   |---|---|
   | `class Position { constructor(public readonly lineNumber, ...) }` | `readonly record struct Position` with properties |
   | `range.containsPosition(p)` | `range.Contains(p)` (Monaco name in `<remarks>`) |
   | `static Range.plusRange(a, b)` | instance method `a.Plus(b)` when it reads better |
   | `IDisposable`, `Disposable`, `DisposableStore` | `IDisposable`, `List<IDisposable>` |
   | `Emitter<T>` / `onDidChange` | .NET `event EventHandler<T>? Changed` |
   | `number` used as an int | `int` (check for overflow: JS numbers are 53-bit) |
   | `string.charCodeAt(i)` | `string[i]` (UTF-16 code unit — identical semantics) |
   | `Uint32Array`, typed arrays | `int[]`, `List<int>`, `Span<int>` |
   | `undefined` / `null` | nullable (`T?`) — enable nullable reasoning, don't use `!` to silence it |
   | regular expressions | `[GeneratedRegex]` (never `new Regex(...)` in hot paths) |
5. **Don't port what .NET already has** (`strings.ts` helpers, `arrays.ts`…): use the BCL, and say so in a comment.
6. **Don't optimize while porting.** First a faithful port with green tests; then benchmarks; then optimizations,
   each with a comment explaining *why* and the benchmark that proves it.
7. **Update the map below** in the same pull request.

## Behavior differences we accept

Any deliberate difference from Monaco must be documented in the XML doc of the member *and* listed here.

| Where | Difference | Why |
|---|---|---|
| `Position`, `Range`, `Selection` constructors | Throw `ArgumentOutOfRangeException` for values < 1 (Monaco accepts anything) | Fail fast; Monaco's own model validates positions anyway |
| `Selection` | Not a subclass of `Range` (exposes a `Range` property) | C# record structs cannot inherit |
| IME underlines (P3-53) | Composition underlines are view model state (`ViewModel.CompositionFormats`), not model decorations; `Windows.UI.Text.UnderlineType` has no thin value, so `Thick` is the converted segment and any other underline is thin | No decorations API in phase 3 (ADR 0006) |
| IME (`ImeInput`, P3-51) | IME needs Windows 11: on Windows 10 `CoreTextServicesManager.GetForCurrentView()` fails, one debug line is written and the editor works without IME | Raising the minimum Windows version is not needed (ADR 0006, decision 11) |
| Text input (`LoomEditor.Ime.cs`, P3-51) | Plain typing comes from `CharacterReceived`, composition from `CoreTextEditContext` (Monaco gets both from the hidden `EditContext`) | WinUI delivers them on two separate paths; keys and characters are ignored while a composition is active |
| `ViewModel.GetHiddenAreas` (folded-line moves) | Always empty: the folded-line moves behave like line moves | No folding before phase 5 |
| `ViewModel.GetTextDirection` (`_moveLeft`, `_moveRight`) | Always `TextDirection.LTR`: the RTL swap is kept but never taken | Text direction is always LTR in phase 3 |
| `WordCharacterClassifier` | `wordSegmenterLocales` is ignored: `FindPrevIntlWordBeforeOrAtOffset` / `FindNextIntlWordAtOrAfterOffset` always return `null` | No Native AOT-friendly equivalent of `Intl.Segmenter` |
| Tabs in rendered lines | Drawn with DirectWrite tab stops (`IncrementalTabStop = tabSize × space width`) instead of being expanded to spaces by `viewLineRenderer.ts`; a tab after a full-width character ends on a pixel tab stop, not a visible-column one | One UTF-16 index is one layout index, so no `CharacterMapping` is needed (ADR 0005, decision 5) |
| `CommandRegistry` | One per editor instead of a global registry | Two editors on a page never share state |
| Keybindings | `when` clauses are C# predicates (`WhenClauses`) instead of context-key expressions; no chords; Windows table only | No expression parser (ADR 0006, decision 8); chords are only used by Find-based commands |
| Reveal (`viewLines.ts`) | No sticky scroll (`_stickyScrollEnabled` is false) and no RTL (`hasRTL` is false); `_computeScrollTopToRevealRange` returns a whole number of DIPs (Monaco keeps the fraction) | No sticky scroll before phase 6; scroll positions are whole DIPs |
| Mouse hit testing | Geometric on the canvas (no DOM, no `ElementPath`): `x < contentLeft` is the gutter, the column comes from the DirectWrite layout of the line (`ILineLayout.HitTest`) | Monaco's `viewLinesGpu` path; there are no DOM nodes to ask |
| Clipboard | The metadata of a copy (empty-selection line, block, one text per cursor) are kept in memory only (no custom clipboard format); the empty-selection check uses the primary selection, like Monaco | Native AOT and no JSON; Monaco does the same on the web |
| Mouse down | No drag-and-drop of the selection (ADR 0006, decision 12); no view zones; the click count is made by `MouseDownState` because WinUI reports none | Cut for phase 3 |
| `ViewController.dispatchMouse` | Double-click never selects bracket or string content in phase 3 (`_trySelectBracketContent` and `_trySelectStringContent` return nothing); no Linux selection clipboard, no `scrollOnMiddleClick` | Needs bracket pairs and tokens (phases 4–5) |
| `ClipboardUtils` | No rich (HTML) copy: `copyWithSyntaxHighlighting` and `getRichTextToCopy` are not ported, the stored `mode` is always `null` | ADR 0006, decision 12 |
| `ShiftCommand` | No `cachedStringRepeat` cache: the indent is built each time | Monaco's global mutable cache is a performance trick only; same result |
| `StringBuffer` | Growable `char[]` instead of a `string` | Monaco's `buffer += text` on the change buffer would be quadratic in .NET |
| `CursorsController` | Exceptions thrown inside a cursor edit are not swallowed (Monaco: `onUnexpectedError`); the cursors still resume tracking their selections | A bug in a port must fail the tests |
| `CursorsController` | Auto-closed actions are not tracked in phase 3: `AutoClosedAction.IsValid` is always false and nothing adds actions | They need model decorations (phases 4–5) |
| `RedBlackTree` | One sentinel per tree instead of a global mutable `SENTINEL`; `TreeNode.IsDetached` replaces `parent === null`; `next`/`prev` are tree methods | Monaco's `resetSentinel` writes to the global sentinel: two trees on two threads would corrupt each other |
| `IntervalTree` | One sentinel per tree instead of a global `SENTINEL` (`IntervalTree.Sentinel`); `resetSentinel` resets the tree's own | Same reason as `RedBlackTree` |
| `IntervalNode`, `IntervalTree` | Offsets (`Start`, `End`, `Delta`, `MaxEnd`, cached offsets) are `long` | Monaco uses 53-bit JavaScript numbers; the ported `force delta overflow` test goes above 2^31 |
| `PieceTreeBase.NodeAt`, `NodeAt2` | Throw `ArgumentOutOfRangeException` for an offset or position outside the document (Monaco returns `null` and fails later) | Fail fast with a clear message |
| `PieceTreeBase.GetCharCode`, `GetLineCharCode` | Return `char` (`'\0'` at the end of the document) instead of a number (`0` or `NaN`) | Same UTF-16 unit; no `NaN` in C# |
| `PieceTreeBase.Insert` | Inserting an empty string is a no-op (Monaco splits the piece; inside a CRLF pair this made `GetLineCount` count one line too many) | Found by the P1-15 property test; Monaco callers never insert empty text |
| `PieceTreeTextBufferFactory.Create` | Returns the `PieceTreeTextBuffer` directly, not a `{ textBuffer, disposable }` pair; `PieceTreeTextBuffer` is not `IDisposable` and has no `onDidChangeContent` | Nothing to dispose in this port |
| `ITextBuffer` | `IReadonlyTextBuffer` and `ITextBuffer` are one interface; no `onDidChangeContent`, not `IDisposable`, no `findMatchesLineByLine` yet (phase 5) | Simpler contract; search is deferred |
| `TextModel.GetLineMinColumn` | Throws `ArgumentOutOfRangeException` for an illegal line number (Monaco ignores it and returns 1) | Consistent with the other line methods; avoids a static-able public instance method |
| `ModelRawLineChanged` | Has `LineNumber` and `LineNumberPostEdit` only, as at the pinned commit (the card listed a `Detail` field that does not exist upstream) | Faithful to the pinned Monaco source |
| `TextChange` | Monaco's binary serialization (`write`, `read`, `writeSize`) is not ported | Nothing in LoomEditor persists undo stacks |
| `TextModel` decorations | One decorations tree in phase 3 (no overview-ruler or injected-text tree) and no public decorations API: only tracked ranges (`SetTrackedRange`, `GetTrackedRange`) | Phase 3 needs nothing else; the other trees and `deltaDecorations` come with decorations |
| `TextModel` options | No indentation guessing yet (`detectIndentation` is false; phases 4–5); `indentSize: 'tabSize'` is a `null` indent size (`TextModelUpdateOptions.IndentSizeIsTabSize`); `NormalizePosition` is `static` | Guessing needs tokens; C# has no union type; CA1822 |
| `CursorConfiguration` | No language configuration in phase 3 (no auto-closing pairs, surrounding pairs, electric characters, comment tokens); `inputMode` is always insert; the editor options come from `EditorCursorOptions` instead of `IEditorConfiguration`; `shouldRecreate` is not ported | Language configuration comes in phases 4–5; overtype is out of phase 3; the view model recreates the configuration |
| `ICommand` | Named `IEditCommand` in C#; `CommandExecutor` has no `editReason` parameter, builds a new list of selections instead of mutating the input, and keys the loser cursors by a `HashSet<int>` of majors | The editor also has commands in the sense of named actions (ADR 0006, decision 1); C# idiom |
| `UndoRedoStack` | Replaces Monaco's `IUndoRedoService` (workspace-wide undo, groups, snapshots, prompts) with a small per-model stack; `SingleModelEditStackElement` keeps its data as is when closed instead of serializing it | Multi-model undo is out of scope; nothing persists undo stacks |
| `TextModel.Undo` / `Redo` | Return the cursor state to restore (Monaco returns nothing) and also set it as `ModelRawContentChangedEventArgs.ResultingSelection` of the event, like Monaco (P3-10), only on the first of the two events when the EOL changes too; undoing a step that changed text and EOL raises two events, not one merged event; an exception thrown by a `cursorStateComputer` is not caught | Simpler API; no deferred event emitter yet |
| `MouseWheelClassifier` | One instance per editor instead of Monaco's global `INSTANCE`; no `acceptStandardWheelEvent` (DOM event) | Two editors must not share their wheel history |
| `MouseWheelScrolling` | The wheel arithmetic of `_onMouseWheel` is a pure function that does not validate the position; no inertial scrolling, `scrollYToX` or `flipAxes` | Testable without a `ViewLayout`; the caller validates and compares with the future position |
| `EditorLayoutOptions` | `GlyphMargin` and `Folding` default to `false` (Monaco: `true`) | No feature draws a glyph or a folding arrow yet; an empty 16-19 DIP strip would look like a bug. Phases adding breakpoints or folding turn them on |
| `Scrollable`, `SmoothScrollingOperation` | A `TimeProvider` replaces `Date.now()`; `ISmoothScrollPosition` extends `IScrollPosition` | Testable time; TypeScript structural typing has no C# equivalent |
| `LineHeightsManager`, `LinesLayout` | Heights are `int`; no `CustomLineHeightData.fromDecorations`; `PendingChanges` are private nested types | No decorations yet; line heights are whole DIPs |
| `ViewLayout` | No word-wrap branch in the content width; explicit options (`ViewLayoutOptions`, `OnOptionsChanged`, `OnLayoutInfoChanged`, `OnFontInfoChanged`) instead of `IEditorConfiguration`; `ContentSizeChangedEvent.attemptToMerge` not ported | Word wrap is phase 6; no options system yet |
| `ViewModel`, `ViewportData` | Phase 2 subset: view lines are model lines (identity mapping), no view events, no rendering data, no decoration or wrapped-line queries | Projections (word wrap, folding, injected text) come in phase 6 |
| `FontInfo`, `FontMeasurements` | No font feature or variation settings, no editor zoom, no serialization, no measurement cache (so the "re-measure when width ≤ 2" retry is not ported); widths are read through `ICharacterWidthReader` (DirectWrite in `Win2DTextMeasurer`) | Windows only; nothing to cache or zoom yet |
| `EditorLayoutInfoComputer` | Subset of `computeLayout`: no minimap, word wrap or accessibility parts | Phases 6–7 |
| Scrollbars | XAML `ScrollBar` primitives driven by the `Scrollable`; Monaco's `scrollbarState.ts` and `abstractScrollbar.ts` are not ported | Windows look and accessibility (ADR 0005, decision 3) |
| Line rendering | `TextViewRenderer` draws on a canvas the size of the viewport; there is no `viewLineRenderer.ts` HTML output, no `CharacterMapping`; a cache of line layouts keyed by the text of the line replaces `ViewLine` invalidation; only `stopRenderingLineAfter` (10,000) and its "Show more" label are ported | ADR 0005 (decisions 1, 4 and 5) |
| `CursorBlinking` | The CSS-animated styles `Smooth`, `Phase` and `Expand` behave like `Blink`; only the `Line` cursor style is drawn; no timer inside (the control ticks it with a `DispatcherQueueTimer`); no secondary cursors | No CSS animations on a canvas; multi-cursor is phase 3 |
| Line numbers and line highlight | Drawn on the canvas; only `RenderLineNumbersType.On`, `renderFinalNewline = on`, and `renderLineHighlight` `none` / `gutter` / `line` / `all` with `renderLineHighlightOnlyWhenFocus = false` | No relative or interval numbers or decorations yet |
| Oniguruma encoding (vscode-oniguruma) | Patterns and subjects are UTF-16 LE, not UTF-8: no UTF-8 copy and no offset maps; offsets are UTF-16 code units directly. An unmatched group is `(-1, -1)` (vscode-oniguruma gives the string length or `4294967295`; the length is 0 in all cases). A start position outside the string is clamped (vscode-oniguruma clamps only non-ASCII strings). Byte escapes `\xHH` are compiled as code points `\x{HH}` (Oniguruma rejects them in UTF-16), and the UTF-8 BOM bytes `\xEF\xBB\xBF` (CSS grammar) as `\x{FEFF}` (P4-35) | ADR 0007, decision 2 |
| Tokenization worker (P4-37) | A `Thread` and a `Channel<WorkerMessage>` replace the web worker RPC; `RunOnceScheduler(10)` is a 10 ms wait for more messages and `setTimeout0` is a read of the pending messages between two slices; end states cross threads as `StateStack` references (`null` = unchanged) instead of `StackDiff`s; a batch is at most 200 lines (Monaco: the check `tokenizedLines > 200` lets 201 through); the grammar is created synchronously on the worker thread; `TokenizationSupportWithLineLimit.MaxTokenizationLineLength` is settable (Monaco: an observable); no font tokens, no tokenization time report | Native AOT, no serialization, native scanners belong to one thread (ADR 0008, decision 5) |
| Theme loading (P4-40) | No `.tmTheme` files and no `tokenColors` given as a path (both are plist: ADR 0008, decision 8): a `FormatException`; no `semanticTokenColors` / `semanticHighlighting`, no custom colors, no font size, family or line height; an include cycle is cut at depth 32; a rule without scope or settings is dropped on load (it has no effect upstream either); a color text that is not a color is red and an invalid hex digit is 0 (`Color.fromHex`) | Native AOT and a smaller surface; the rest follows `colorThemeData.ts` |
| `OnigScanner` (vscode-oniguruma) | Always the per-regex search with its cache; the `onig_regset_search` branch (strings under 1000 UTF-8 bytes) is not used. It returns the same match | ADR 0007, decision 3 |
| Raw grammars (vscode-textmate) | JSON grammars are read with `System.Text.Json` (comments and trailing commas allowed), not with `json.ts`; no location metadata (`$vscodeTextmateLocation`), so debug messages cannot point to a grammar line. Properties of the wrong JSON type are ignored; `applyEndPatternLast` is `true` for `true` or a non-zero number | ADR 0008, decision 3 |
| TextMate themes (vscode-textmate) | No font attributes: `fontFamily`, `fontSize` and `lineHeight` of theme settings are ignored; `StyleAttributes` and `ParsedThemeRule` have no font fields; `FontAttribute`, `FontInfo`, `LineFonts` are not ported, nor the tests `'Fonts are correctly set'` and `'… 2'` | ADR 0008, decision 3 |
| Tokenization registry (`tokenizationRegistry.ts`) | An instance per editor (ADR 0008, decision 4), not generic, no lazy factories (`registerFactory`, `getOrCreate`, `isResolved`); the color map is `IReadOnlyList<uint>` (ARGB) instead of `Color[]`; `onDidChange` is the `Changed` event; no CSS class names or inline styles from metadata (`getClassNameFromMetadata`, `getInlineStyleFromMetadata`) | Core does not know the theme types; WinUI draws, it has no CSS |
| `tabSize` | `LoomEditor.TabSize` is a dependency property (default 4, Monaco's `EDITOR_MODEL_DEFAULTS.tabSize`); a value < 1 throws `ArgumentOutOfRangeException` and the old value is kept | WinUI property system; fail fast |

## Map: Monaco → LoomEditor

| Monaco file | LoomEditor file | Status |
|---|---|---|
| `editor/common/core/position.ts` | `LoomEditor.Core/Text/Position.cs` | ✅ ported |
| `editor/common/core/range.ts` | `LoomEditor.Core/Text/Range.cs` | ✅ ported |
| `editor/common/core/selection.ts` | `LoomEditor.Core/Text/Selection.cs` | ✅ ported |
| `editor/common/core/editOperation.ts` | `LoomEditor.Core/Text/TextEdit.cs` | ✅ ported (`ISingleEditOperation` + `EditOperation`) |
| `editor/common/core/cursorColumns.ts` | `LoomEditor.Core/Text/CursorColumns.cs` | ✅ ported (P3-03) |
| `editor/common/core/misc/indentation.ts` | `LoomEditor.Core/Text/Indentation.cs` | ✅ `normalizeIndentation` only (P3-03) |
| `editor/common/core/characterClassifier.ts` | `LoomEditor.Core/Text/CharacterClassifier.cs`, `CharacterSet.cs` | ✅ ported (P3-04) |
| `editor/common/core/wordCharacterClassifier.ts` | `LoomEditor.Core/Text/WordCharacterClassifier.cs` | ✅ ported (P3-04); Intl segmentation is a stub |
| `editor/common/core/wordHelper.ts` | `LoomEditor.Core/Text/WordHelper.cs` | ✅ constants only (`USUAL_WORD_SEPARATORS`, `IWordAtPosition`) (P3-04) |
| `editor/common/model/intervalTree.ts` | `LoomEditor.Core/Decorations/` | ✅ ported (P3-05 … P3-07) |
| `editor/test/common/core/range.test.ts` | `tests/LoomEditor.Core.Tests/Text/RangeTests.cs` | ✅ all tests ported |
| `platform/commands/common/commands.ts` | `LoomEditor.View/Commands/CommandRegistry.cs` | ✅ adapted (per editor) |
| `editor/common/editorCommon.ts` (`IEditorContribution`) | `LoomEditor.View/Features/IEditorFeature.cs` | ✅ adapted |
| `editor/common/cursor/cursorMoveCommands.ts` (`selectAll`) | `LoomEditor.View/Features/SelectAll/` | ✅ ported |
| `editor/common/cursor/cursorMoveCommands.ts` | `LoomEditor.View/Cursors/CursorMoveCommands*.cs`, `CursorMove.cs` | ✅ ported (P3-24, P3-25) except `word` (P3-30); `CursorMove.metadata` is not ported |
| `editor/common/model/pieceTreeTextBuffer/*` | `LoomEditor.Core/Model/PieceTree/` | ✅ ported (phase 1), except search (phase 5) |
| `editor/common/encodedTokenAttributes.ts` | `LoomEditor.Core/Tokens/` (`TokenMetadata`, `MetadataConsts`, `LanguageId`, `ColorId`, `FontStyle`, `StandardTokenType`, `TokenPresentation`) | ✅ P4-23; without the CSS helpers; ids are `int` constants, metadata is `uint` |
| `editor/common/languages.ts` (tokens), `languages/nullTokenize.ts`, `tokenizationRegistry.ts`, `services/languagesRegistry.ts` (`LanguageIdCodec`) | `LoomEditor.Core/Tokens/` (`IState`, `ITokenizationSupport`, `EncodedTokenizationResult`, `NullTokenize` with `NullState`, `LanguageIdCodec`, `TokenizationRegistry`, `TokenizationSupportChangedEventArgs`) | 🔶 P4-23; `IBackgroundTokenizer` and `IBackgroundTokenizationStore` come with P4-28, `createBackgroundTokenizer` with P4-29 (it needs `TextModel`); no string `tokenize`, no `fontInfo` |
| `editor/common/tokens/lineTokens.ts`, `test/common/core/lineTokens.test.ts` | `LoomEditor.Core/Tokens/` (`IViewLineTokens`, `LineTokens`, `LineTokens.Slice.cs`) | 🔶 P4-24; no `getClassName` / `getInlineStyle` (CSS; `GetMetadata`, `GetForeground`, `GetPresentation` instead), no `getTokensInRange`, `sliceZeroCopy`, `TokenArray*`, `toString` (later phases); `getStandardTokenTypeAtPosition` comes with P4-29; length mismatch is a Debug `Guard.Assert`, not `onUnexpectedError`; 7 tests ported (`LineTokensTests`) |
| `editor/common/tokens/contiguousTokensEditing.ts`, `contiguousTokensStore.ts` | `LoomEditor.Core/Tokens/` (`ContiguousTokensEditing`, `ContiguousTokensStore`) | 🔶 P4-25; lines are `uint[]?` (no `ArrayBuffer`, `toUint32Array` dropped, `EmptyLineTokens` compared by reference); `acceptEdit` has Monaco's 3 parameters; `setMultilineTokens` is ported in P4-29; 4 new tests, no upstream test file |
| `editor/common/tokens/contiguousMultilineTokens.ts`, `contiguousMultilineTokensBuilder.ts` | `LoomEditor.Core/Tokens/` (`ContiguousMultilineTokens`, `ContiguousMultilineTokensBuilder`) | 🔶 P4-26; no serialization (`deserialize`, `serialize`, ADR 0008 decision 5), no `getLineRange`; `_insertLines` clamps the insert index (JS `arrayInsert` appends past the end); 5 new tests, no upstream test file |
| `editor/common/model/fixedArray.ts`, `model/textModelTokens.ts` (1/2: `TokenizationStateStore`, `TrackingTokenizationStateStore`, `RangePriorityQueueImpl`), `core/ranges/offsetRange.ts` (partial), `test/common/model/textModelTokens.test.ts` | `LoomEditor.Core/Model/Tokens/` (`FixedArray`, `TokenizationStateStore`, `TrackingTokenizationStateStore`, `RangePriorityQueue`), `LoomEditor.Core/Text/OffsetRange.cs` | 🔶 P4-27; `OffsetRange` has only the members used so far and stands in for `LineRange`; `BugIndicatingError` is `ArgumentException` / `InvalidOperationException`; the tokenizers of the same file come with P4-28; tests `addRange`, `addRangeAndResize` ported + 3 new |
| `editor/common/model/textModelTokens.ts` (2/2: `TokenizerWithStateStore`, `TokenizerWithStateStoreAndTextModel`, `findLikelyRelevantLines`, `safeTokenize`, `DefaultBackgroundTokenizer`) | `LoomEditor.Core/Model/Tokens/` (`TokenizerWithStateStore`, `TokenizerWithStateStoreAndTextModel`, `DefaultBackgroundTokenizer`, `ITokenizationScheduler`, `ITokenizationModelInfo`), `LoomEditor.Core/Tokens/` (`IBackgroundTokenizer`, `IBackgroundTokenizationStore`) | 🔶 P4-28; non-generic over `IState`; behavior differences: scheduler abstraction instead of `runWhenGlobalIdle` / `setTimeout0`, `ITokenizationModelInfo` instead of `ITextModel.getLanguageId` / `isAttachedToEditor` (until P4-29 / P4-30), a throwing support is traced with `Debug.WriteLine` instead of `onUnexpectedError`, `Dispose` cancels the pending idle callback, no `setFontInfo` / `reportMismatchingTokens`; 4 new tests, no upstream test file |
| `editor/common/tokenizationTextModelPart.ts` (`BackgroundTokenizationState`), `model/tokens/tokenizationTextModelPart.ts`, `abstractSyntaxTokenBackend.ts`, `tokenizerSyntaxTokenBackend.ts`, `textModelEvents.ts` (`IModelTokensChangedEvent`, `IModelLanguageChangedEvent`) | `LoomEditor.Core/Model/TextModel.Tokenization.cs`, `LoomEditor.Core/Model/Tokens/` (`TokenizationTextModelPart`, `TokenizerSyntaxTokenBackend`, `BackgroundTokenizationState`), `LoomEditor.Core/Model/Events/` (`ModelTokensChangedEventArgs`, `ModelLanguageChangedEventArgs`) | 🔶 P4-29, all internal; no semantic tokens, tree-sitter, font tokens, word helpers, bracket pairs; `AbstractSyntaxTokenBackend` merged into the one backend; the registry `Changed` event resets only for the model language; `SetLanguageId` resets tokenization itself; public constructors use an empty registry (plain text); without a scheduler the default background tokenizer is not created; the registry subscription is never removed (no model disposal yet); 5 new tests |
| `editor/common/model/tokens/abstractSyntaxTokenBackend.ts` (`AttachedViews`, `AttachedViewState`, `AttachedViewHandler`), `tokens/common.ts` (`RateLimiter`), `model.ts` (`IAttachedView`), `textModel.ts` (size limits, `onBeforeAttached` / `onBeforeDetached`), `test/common/model/model.modes.test.ts` | `LoomEditor.Core/Model/Tokens/` (`AttachedViews.cs`, `IAttachedView`, `RateLimiter`), `TextModel.Tokenization.cs` (`AttachView`, `DetachView`, `IsTooLargeForTokenization`) | 🔶 P4-30, internal; no `visibleLineRanges` observable (tree-sitter); the 50 ms refresh delay is `ITokenizationScheduler.Post`; `largeFileOptimizations` is always on; all 14 `model.modes` tests ported (`ModelModesTests`) + 3 new |
| `test/common/model/model.line.test.ts` (`ModelLinesTokens`, 1/2: deletes, inserts, insertion updates), `testLineToken.ts` helpers | `tests/LoomEditor.Core.Tests/Model/Tokens/ModelLinesTokensTests.cs`, `TestHelpers/ManualTokenizationSupport.cs` | ✅ P4-31; 29 tests ported (`single delete 1-5`, `multi delete 6-9`, `single insert 1-5`, `multi insert 6-9`, `insertion on empty line`, `updates tokens on insertion 1-10`); no bug found; compares the foreground instead of `getClassName`; the `getIndentLevel` suite is not about tokens |
| `test/common/model/model.line.test.ts` (2/2: deletes inside tokens, replace, split, append), `textModelWithTokens.test.ts` (`TextModelWithTokens regression tests`) | `tests/LoomEditor.Core.Tests/Model/Tokens/ModelLinesTokensTests.cs`, `TextModelWithTokensRegressionTests.cs` | ✅ P4-32; 25 + 2 tests ported (`microsoft/monaco-editor#122`, `issue #63822`); left for phase 5: `microsoft/monaco-editor#133` and `issue #11856` (bracket matching); no bug found |
| `editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts` | `LoomEditor.Core/Model/PieceTree/RedBlackTree*.cs`, `TreeNode.cs`, `NodeColor.cs` | ✅ ported (P1-03; `rbInsertLeft/Right` and `NodePosition` from `pieceTreeBase.ts`) |
| `editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` | `LoomEditor.Core/Model/PieceTree/LineStarts.cs`, `PieceTreeBase*.cs` (+ more) | ✅ ported (P1-01, P1-05 to P1-08, P1-14, tests P1-10 to P1-15) except search (`findMatchesInNode`…), which comes in phase 5 |
| `editor/common/model/pieceTreeTextBuffer/pieceTreeTextBufferBuilder.ts` | `LoomEditor.Core/Model/PieceTree/PieceTreeTextBufferBuilder.cs`, `PieceTreeTextBufferFactory.cs` | ✅ ported (P1-09) |
| `editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts` | `LoomEditor.Core/Model/PieceTree/PieceTreeTextBuffer*.cs` | ✅ ported (P1-09, P1-16, P1-17, P1-18 `PieceTreeTextBuffer.ApplyEdits.cs`) except `findMatchesLineByLine` (phase 5); reverse edits carry a `TextChange` (P1-27) |
| `editor/common/model.ts` (`EndOfLinePreference`, `DefaultEndOfLine`, `EndOfLineSequence`, `IReadonlyTextBuffer` + `ITextBuffer` merged, without search) | `LoomEditor.Core/Model/` | ✅ ported (P1-09, P1-16); `applyEdits` added in P1-18 |
| `editor/common/core/misc/eolCounter.ts` (`countEOL`, without the EOL kind) and the edit types of `model.ts` / `mirrorTextModel.ts` | `LoomEditor.Core/Model/Edits/`, `Model/ModelContentChange.cs` | ✅ ported (P1-17) |
| `editor/test/common/model/linesTextBuffer/linesTextBuffer.test.ts` (`_getInverseEdits`, `_toSingleEditOperation`) | `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeTextBufferInverseEditsTests.cs`, `PieceTreeTextBufferToSingleEditOperationTests.cs` | ✅ all tests ported |
| `base/common/strings.ts` (`containsRTL`, `isBasicASCII`, `containsUnusualLineTerminators`, BOM, `firstNonWhitespaceIndex`, `lastNonWhitespaceIndex`) | `LoomEditor.Core/Model/StringClassification.cs` | ✅ ported (P1-09, P1-16) |
| `base/common/strings.ts` (partly: code points, surrogates, emoji, full-width, leading whitespace, `splitLines`, `CodePointIterator`, `getLeftDeleteOffset` — P3-01; graphemes: `GraphemeIterator`, `nextCharLength`, `prevCharLength`, `getCharContainingOffset`, `charCount`, break table — P3-02) | `LoomEditor.Core/Text/Strings.cs`, `Strings.Emoji.cs`, `Strings.Graphemes.cs`, `CodePointIterator.cs`, `GraphemeIterator.cs`, `GraphemeBreakTree.cs`, `GraphemeBreakType.cs`, `GraphemeBreakData.cs` (generated) | ✅ ported (P3-01, P3-02) |
| `editor/test/common/model/linesTextBuffer/linesTextBufferBuilder.test.ts` | `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeTextBufferBuilderTests.cs` | ✅ all tests ported |
| `editor/common/textModelEvents.ts` (`ModelRaw*`, `RawContentChangedType`, `ModelRawContentChangedEvent`) | `LoomEditor.Core/Model/Events/` | ✅ ported (P1-23; no injected text, `resultingSelection`, `merge`); undo, redo, `PushEditOperations`, `PushEol` (P1-27) |
| `editor/test/common/model/model.test.ts` (suites `Model`, `Model Line Separators`) | `tests/LoomEditor.Core.Tests/Model/ModelTests.cs`, `ModelLineSeparatorsTests.cs` | ✅ ported (suite `Words`: phase 5) |
| `editor/common/model/textModel.ts` | `LoomEditor.Core/Model/TextModel.cs` | ✅ ported (phase 1): read API (P1-20), position/range validation (P1-21), edits, `SetValue`, `SetEol` and `ContentChanged` (P1-22), raw line-level events `RawContentChanged` (P1-23), snapshots `CreateSnapshot` and `FromSnapshot` (P1-28), tracked ranges (P3-08), options (P3-09), trim auto whitespace (P3-11); no other decorations, no edit sources; `GetLineMinColumn` validates its line number (Monaco does not); `ValidatePosition`/`ValidateRange` also have `int` overloads (Monaco passes 0 or negative values) |
| `editor/common/core/textChange.ts` | `LoomEditor.Core/Undo/TextChange.cs`, `TextChangeCompressor.cs` | ✅ ported (P1-26) except binary serialization |
| `editor/common/model/editStack.ts` | `LoomEditor.Core/Undo/` (`SingleModelEditStackData`, `SingleModelEditStackElement`, `EditStack`, `UndoRedoStack`) | ✅ ported (P1-27), data form only: no `serialize`/`deserialize`, no `MultiModelEditStackElement`; `editStack.test.ts` (binary serialization) not applicable |
| `editor/common/model/intervalTree.ts` | `LoomEditor.Core/Decorations/` | ⏳ phase 2+ |
| `editor/common/editorCommon.ts` (`ICommand`, `IEditOperationBuilder`, `ICursorStateComputerData`) | `LoomEditor.View/Cursors/Commands/` | ✅ ported (P3-14): `ICommand` is `IEditCommand` |
| `editor/common/cursor/cursor.ts` (`CommandExecutor`) | `LoomEditor.View/Cursors/CommandExecutor*.cs` | ✅ ported (P3-14); `Cursor` and `CursorCollection` are ported (P3-16), `CursorsController` is in `CursorsController*.cs` |
| `editor/common/commands/replaceCommand.ts` | `LoomEditor.View/Cursors/Commands/ReplaceCommand.cs` | ✅ ported (P3-15); the overtype commands are not reachable in phase 3 |
| `editor/common/commands/shiftCommand.ts` | `LoomEditor.View/Cursors/Commands/ShiftCommand.cs` | ✅ ported (P3-31) except the enter-action re-alignment (cut point, lines 144–176: phases 4–5) |
| `editor/common/commands/columnSelectionPasteCommand.ts` | `LoomEditor.View/Cursors/Commands/ColumnSelectionPasteCommand.cs` | ✅ ported (P3-35) |
| `editor/contrib/multicursor/browser/multicursor.ts` | `LoomEditor.View/Features/MultiCursor/` | 🔶 without Find (P3-38): add cursor above/below, cursors at line ends, to top/bottom, focus next/previous cursor. `addSelectionToNextFindMatch`, `selectHighlights`, `changeAll`, `MultiCursorSession`, `SelectionHighlighter` are phase 5 |
| `editor/browser/controller/editContext/native/nativeEditContext.ts` | `LoomEditor.View/Controller/EditContextState.cs`, `EditContextModel.cs` | 🔶 text, selection and type data (P3-50): `_getNewEditContextState`, `_updateEditContext`, the surrogate buffering of `textupdate`, `_emitTypeEvent`, `_onType`. Only the primary selection is exposed to the IME, as in Monaco (`CompositionType` still applies to all cursors). The WinUI side is `LoomEditor.WinUI/Input/ImeInput.cs` (P3-51): `CoreTextEditContext` text, selection, update and composition events, `Notify*` calls queued on the dispatcher; `EditContextModel.OnSelectionUpdate` answers `SelectionUpdating`. `LayoutRequested` gives the screen-pixel bounds of the primary selection (P3-52, `ScreenCoordinates`, `NativeMethods.ClientToScreen`), refreshed with `NotifyLayoutChanged` on scroll, size, DPI and window moves. `FormatUpdating` gives the underlines (P3-53): `EditContextModel.ToCompositionFormats` → `ViewModel.CompositionFormats`, drawn by `TextViewRenderer.Composition.cs` with `EditorTheme.CompositionBorder` |
| `editor/browser/controller/editContext/clipboardUtils.ts` | `LoomEditor.View/Controller/ClipboardData.cs` | 🔶 copy data, clipboard metadata and `InMemoryClipboardMetadataManager` (P3-36); the HTML (rich) part and the DataTransfer functions are not ported |
| `editor/common/cursor/cursorDeleteOperations.ts` | `LoomEditor.View/Cursors/DeleteOperations.cs` | 🔶 `deleteLeft`, `deleteRight` (P3-32), `cut` (P3-36) without auto-closing pair delete (phases 4–5) |
| `editor/common/cursor/cursorTypeOperations.ts` | `LoomEditor.View/Cursors/TypeOperations.cs` | 🔶 `typeWithInterceptors` (Enter and plain character), `typeWithoutInterceptors`, `shiftIndent`, `unshiftIndent` (P3-33), `indent`, `outdent`, `tab` (P3-34), `paste` (P3-35), `compositionType` and `compositionEndWithInterceptors` (P3-49: the surround and auto-closing branches are phase 4–5 cut points); the other interceptors are phase 4–5 cut points |
| `editor/common/cursor/cursorTypeEditOperations.ts` | `LoomEditor.View/Cursors/TypeEditOperations*.cs` | 🔶 plain typing and Enter (P3-33): `SimpleCharacterTypeOperation`, `EnterOperation` (without `getEnterAction` / `getIndentForEnter`), `TypeWithoutInterceptorsOperation`; Tab (P3-34): `TabOperation` (`_goodIndentForLine` is a cut point: no good indent without a language); paste (P3-35): `PasteOperation`; composition (P3-49): `CompositionOperation`, `CompositionEndOvertypeOperation`; auto-indent, auto-closing, surround and electric characters are phase 4–5 |
| `editor/common/cursor/oneCursor.ts`, `cursorCollection.ts`, `cursorContext.ts` | `LoomEditor.View/Cursors/` (`Cursor`, `CursorCollection`, `CursorContext`) | ✅ ported (P3-16); `CursorCollection.lastAddedCursorIndex` follows the code (a full-array index), not Monaco comment |
| `editor/common/cursor/cursor.ts` (`CursorsController`, `CursorModelState`, `AutoClosedAction`) | `LoomEditor.View/Cursors/CursorsController*.cs` | ✅ ported (P3-18, P3-19) except the auto-closing and snippet cut points. `ModelInjectedTextChangedEvent` is not ported (no injected text) |
| `editor/common/viewModelEventDispatcher.ts`, `editor/common/viewEvents.ts` | `LoomEditor.View/Events/` | 🔶 subset (P3-17): the dispatcher, the collector, `ViewCursorStateChangedEvent`, `ViewRevealRangeRequestEvent`, `CursorStateChangedEvent`, `ReadOnlyEditAttemptEvent`; no `Disposable` base, no duplicate-handler warning |
| `editor/common/cursor/cursorAtomicMoveOperations.ts` | `LoomEditor.View/Cursors/AtomicTabMoveOperations.cs` | ✅ ported (P3-22) |
| `editor/common/cursor/cursorColumnSelection.ts` | `LoomEditor.View/Cursors/ColumnSelection.cs` | ✅ ported (P3-28); the `columnSelect` and `cursorColumnSelect…` commands are in `CoreNavigationCommands.ColumnSelect.cs` |
| `editor/common/cursor/cursorWordOperations.ts` | `LoomEditor.View/Cursors/WordOperations*.cs`, `WordNavigationType.cs`, `WordType.cs`, `FindWordResult.cs`, `DeleteWordContext.cs` | 🔶 moves (P3-29): word scanning, `moveWordLeft/Right`, `moveWordPartLeft/Right`; the deletes, `word` and `deleteInsideWord` are P3-30. `DeleteWordContext` has no `autoClosingPairs` (phase 4–5) |
| `editor/contrib/wordOperations/browser/wordOperations.ts` | `LoomEditor.View/Features/WordOperations/` | 🔶 moves (P3-29): the 16 `cursorWord…` commands; the deletes are P3-30 |
| `editor/contrib/wordPartOperations/browser/wordPartOperations.ts` | `LoomEditor.View/Features/WordPartOperations/` | ✅ ported (P3-30), with the legacy aliases `cursorWordPartStartLeft(Select)` |
| `editor/browser/coreCommands.ts` | `LoomEditor.View/Commands/CoreEditorCommand.cs`, `CoreNavigationCommands*.cs`, `CoreNavigationCommands.Scroll.cs`, `EditorScroll.cs`, `CoreEditingCommands.cs`, `CoreCommandRegistration.cs` | 🔶 scroll commands `editorScroll`, `scrollLineUp`…`scrollRight` (P3-38); editing commands `deleteLeft`, `deleteRight` (P3-32), `lineBreakInsert` (P3-33), `tab`, `outdent` (P3-34), `undo`, `redo` (P3-37); navigation commands (P3-26): move, page, home, end, top, bottom, cancel selection, remove secondary cursors, set selection; `precondition` and `metadata` are not ported; `kbOpts` are in `DefaultKeybindings.cs` (P3-39) |
| `editor/browser/coreCommands.ts` + `editorExtensions.ts` (`kbOpts`), `platform/keybinding/common/keybindingResolver.ts` (subset) | `LoomEditor.View/Commands/DefaultKeybindings.cs`, `KeybindingResolver.cs`, `KeybindingContext.cs`, `WhenClauses.cs` | 🔶 keybindings, Windows only (P3-39) |
| `editor/common/cursor/cursorMoveOperations.ts` | `LoomEditor.View/Cursors/MoveOperations*.cs`, `CursorPosition.cs` | ✅ ported (P3-23) |
| `editor/common/cursor/*` | `LoomEditor.View/Cursors/` | ⏳ phase 3 |
| `editor/common/cursorCommon.ts` | `LoomEditor.View/Cursors/` | 🔶 types (P3-12) and `CursorConfiguration` (P3-13): `SingleCursorState`, `CursorState` and partial forms, `EditOperationType`, `SelectionStartKind`, `ColumnSelectData`, `EditOperationResult`, `ICursorSimpleModel` (+ `TextModelCursorSimpleModel`), `IsQuote`; the rest comes later; `CursorConfiguration` (P3-13) without the language parts and with the editor options it reads (`EditorCursorOptions`) |
| `editor/common/cursorEvents.ts` | `LoomEditor.View/Cursors/CursorChangeReason.cs` | 🔶 `CursorChangeReason` only (P3-12) |
| `editor/common/coordinatesConverter.ts` | `LoomEditor.View/Layout/` | 🔶 `ICoordinatesConverter` and `IdentityCoordinatesConverter` only (P3-12); the word-wrap converter comes with word wrap |
| `base/common/scrollable.ts` | `LoomEditor.View/Layout/` (`ScrollState`, `ScrollEvent`, `Scrollable`, `SmoothScrollingOperation`, scroll value types) | ✅ ported (P2-01, P2-02); `TimeProvider` replaces `Date.now()`; `ISmoothScrollPosition` extends `IScrollPosition` and `ScrollState` implements it (TypeScript structural typing) |
| `editor/common/viewLayout/lineHeights.ts` | `LoomEditor.View/Layout/` (`LineHeightsManager` (+ `.Changes.cs`), `CustomLine`, `CustomLineHeightData`) | ✅ ported (P2-03) without `CustomLineHeightData.fromDecorations` (no decorations yet); heights are `int`; `PendingChange` is an abstract record; `ArrayMap` is a private nested class |
| `editor/common/viewLayout/linesLayout.ts` | `LoomEditor.View/Layout/` (`LinesLayout` (+ `.Whitespaces.cs`, `.Changes.cs`, `.Viewport.cs`), `EditorWhitespace`, `IEditorWhitespace`, `IWhitespaceChangeAccessor`, `ILineHeightChangeAccessor`) | ✅ ported (P2-04, P2-05); heights are `int`; `PendingChanges` is a private nested class; `PartialViewLinesViewportData`, `ViewWhitespaceViewportData` and `Viewport` (from `viewModel.ts`) live next to it |
| `base/browser/ui/scrollbar/scrollableElement.ts` | `LoomEditor.View/Layout/` (`MouseWheelClassifier`, `MouseWheelScrolling`, `MouseWheelInput`, `MouseWheelOptions`) | ✅ adapted (P2-09), classifier and wheel arithmetic only; scrollbar drawing is XAML (ADR 0005) |
| `editor/common/viewLayout/viewLineRenderer.ts` | `LoomEditor.Rendering/` (`OverflowLabel`; the `stopRenderingLineAfter` constant in `TextViewRenderer`) | 🔶 partly ported (P2-13): only `stopRenderingLineAfter` and the "Show more" overflow label; the HTML renderer is not ported (ADR 0005) |
| (no upstream: WinUI wiring) | `LoomEditor.WinUI/Controls/LoomEditor.Highlighting.cs` (`LanguageId`, `Theme`), `Threading/DispatcherQueueUiDispatcher.cs`, public `BuiltInLanguages.Ids` | ✅ P4-45; the property is `LanguageId`, not `Language` (that would hide `FrameworkElement.Language`); the worker is stopped on `Unloaded` and restarted on `Loaded`; `EditorTheme` has no `required` members (the XAML compiler constructs it) |
| `workbench/.../textMate/browser/backgroundTokenization/textMateWorkerTokenizerController.ts` (creation) | `LoomEditor.TextMate/Tokenization/TextMateWorkerTokenizerController.cs` | 🔶 P4-46 fix: ignores the `ContentChanged` events whose version is not newer than the model snapshot sent to the worker (the model resets its tokenization, and so creates the controller, before it raises the event that would otherwise be applied twice by the worker mirror) |
| `editor/browser/viewParts/viewLines/viewLines.ts` | `LoomEditor.Rendering/` (`TextViewRenderer`, `LineLayoutCache`, `ITextMeasurer`, `ILineLayout`, `Win2D/`), `LoomEditor.WinUI/Controls/` (`LoomEditor.Font.cs`) | 🔶 adapted (P2-11 to P2-14, P2-20): draws only the visible lines at the scroll position; line layouts cached by text; `_maxLineWidth` and its reset on flush, layout and font changes (`OnFontChanged`); no `ViewLine` DOM, no `CharacterMapping` (ADR 0005); reveal (P3-40): `RevealCalculator`, `HorizontalRevealRequest` in `LoomEditor.View/Layout/`, `TextViewRenderer.Reveal.cs`, `LoomEditor.Reveal.cs` |
| `editor/browser/viewParts/lineNumbers/lineNumbers.ts`, `editor/browser/viewParts/currentLineHighlight/currentLineHighlight.ts` | `LoomEditor.Rendering/` (`TextViewRenderer.Gutter.cs`, `RenderLineHighlight`) | 🔶 adapted (P2-15): drawn on the canvas; only `RenderLineNumbersType.On`, `renderFinalNewline = on`, `renderLineHighlight` (`none`, `gutter`, `line`, `all`), `renderLineHighlightOnlyWhenFocus = false`; no relative/interval line numbers or line-number decorations; several cursors (P3-42): highlight on every distinct cursor line when all selections are empty, active number = primary selection |
| `editor/browser/viewParts/viewCursors/viewCursors.ts`, `editor/browser/viewParts/viewCursors/viewCursor.ts` | `LoomEditor.Rendering/` (`CursorBlinking`, `TextEditorCursorBlinkingStyle`, caret drawing in `TextViewRenderer`) | 🔶 adapted (P2-16): blinking state machine without a timer (the control ticks it with a `DispatcherQueueTimer`, P2-19) and `Line` cursor style only. Behavior difference: the CSS-animated styles `Smooth`, `Phase` and `Expand` behave like `Blink`; no secondary cursors, block/underline styles or IME; several cursors (P3-42): one caret per selection, same style |
| `editor/browser/controller/mouseTarget.ts` | `LoomEditor.View/Controller/MouseTarget.cs`, `MouseTargetFactory.cs`, `ILineHitTester.cs`; `LoomEditor.Rendering/TextViewRenderer.HitTest.cs` | 🔶 canvas subset (P3-43): gutter, content text, empty content, `_getMouseColumn`, soft tab snap; no widgets, view zones, minimap, scrollbars, RTL |
| `editor/contrib/clipboard/browser/clipboard.ts` | `LoomEditor.View/Controller/ClipboardCommands.cs`, `IClipboardService.cs`; `LoomEditor.WinUI/Input/WindowsClipboardService.cs` | 🔶 cut, copy and paste (P3-47): the web path over the system clipboard; no paste providers or rich copy |
| `editor/browser/view/viewController.ts` | `LoomEditor.View/Controller/ViewController.cs`, `MouseDispatchData.cs` | 🔶 `dispatchMouse` and the mouse helpers (P3-44); the mouse commands `createCursor`, `_wordSelect`, `_lineSelect`... are in `Commands/CoreNavigationCommands.Mouse.cs` |
| `editor/browser/controller/mouseHandler.ts` | `LoomEditor.View/Controller/MouseDownState.cs`, `MouseDownOperation.cs`, `EditorMouseEvent.cs` | 🔶 `MouseDownState` and `MouseDownOperation` without drag-and-drop of the selection (P3-45); pointer events: `LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs`, `Input/PointerInput.cs` (P3-46) |
| `editor/browser/controller/dragScrolling.ts` | `LoomEditor.View/Controller/DragScrolling.cs` | ✅ ported (P3-45) |
| `editor/common/viewLayout/viewLayout.ts` | `LoomEditor.View/Layout/` (`ViewLayout` (+ `.Scrollable.cs`: `EditorScrollDimensions`, `EditorScrollable`), `ViewLayoutOptions`, `ViewLayoutState`, `ScrollType`, `ContentSizeChangedEventArgs`) | ✅ ported (P2-08); no word-wrap branch in the content width, explicit options instead of `IEditorConfiguration` (`onConfigurationChanged` → `OnOptionsChanged`, `OnLayoutInfoChanged`, `OnFontInfoChanged`); `ContentSizeChangedEvent.attemptToMerge` not ported |
| `editor/common/config/fontInfo.ts` | `LoomEditor.View/Layout/` (`BareFontInfo`, `FontInfo`) | ✅ ported (P2-06), subset: no font feature/variation settings, no editor zoom, no serialization; Windows constants; records |
| `editor/browser/config/fontMeasurements.ts` | `LoomEditor.View/Layout/` (`FontMeasurements`, `ICharacterWidthReader`) | ✅ adapted (P2-06), measurement logic only; no cache, so the "re-measure when width <= 2" retry is not ported (clamp to 5 only); DirectWrite reader in P2-12 |
| `editor/common/config/editorOptions.ts` | `LoomEditor.View/Layout/` (`EditorLayoutInfo`, `OverviewRulerPosition`, `EditorLayoutInfoComputer`, `EditorLayoutInfoComputerEnv`, `EditorLayoutOptions`) | ✅ ported (P2-07), `computeLayout` subset: no minimap, word wrap or accessibility parts; rest of the file (options system) not ported |
| `editor/common/viewModel/viewModelImpl.ts` | `LoomEditor.View/Layout/` (`ViewModel`) | 🔶 subset: layout part (P2-10) and the cursor facade (P3-20: `ViewModel.Cursors.cs`, owns the `CursorsController`, event dispatcher and `CursorConfiguration`); identity line mapping (view lines = model lines); typing, paste, cut and composition facade methods come with their cards; projections in phase 6 |
| `editor/common/viewLayout/viewLinesViewportData.ts` | `LoomEditor.View/Layout/` (`ViewportData`) | 🔶 phase 2 subset (P2-10): fields and constructor only; no rendering data, wrapped-line or decoration queries |
| `editor/common/viewModel/*` (the rest) | `LoomEditor.View/Layout/` | ⏳ phase 6 |
| `microsoft/vscode-textmate` | `LoomEditor.TextMate/Grammar/` | ⏳ phase 4 |
| `editor/contrib/<feature>/` | `LoomEditor.View/Features/<Feature>/` | ⏳ phase 5 |

### vscode-oniguruma (`716aeaa229e4ae2e3b0057377b55743e9a3e995b`, v1.7.0)

| vscode-oniguruma file | LoomEditor file | Status |
|---|---|---|
| `src/onig.cc` (`OnigRegExp`: `createOnigRegExp`, `hasGAnchor`, `_searchOnigRegExp`, `encodeOnigRegion`, `getLastOnigError`) | `LoomEditor.TextMate/Oniguruma/` (`OnigRegex`, `OnigSearchOptions`, `OnigException`; native side: `OnigNative`, `OnigLibrary`, `OnigRegexHandle`, `OnigRegionHandle`) | ✅ P4-03, UTF-16 LE instead of UTF-8 (ADR 0007, decision 2) |
| `src/onig.cc` (`searchOnigRegExp`, `createOnigScanner`, `freeOnigScanner`, `toOnigOption`, `findNextOnigScannerMatch`) | `LoomEditor.TextMate/Oniguruma/OnigScanner.cs` | ✅ P4-04, per-regex branch only (no `onig_regset_*`) |
| `src/index.ts` (`FindOption`, `OnigString`, `OnigScanner`) | `LoomEditor.TextMate/Oniguruma/` (`OnigString`, `OnigScanner`) | ✅ P4-04, no UTF-8 buffers or offset maps; `loadWASM` not ported |
| `src/test/index.test.ts` | `LoomEditor.TextMate.Tests/Oniguruma/OnigScannerTests.cs` | ✅ 12 / 12 |

### vscode-textmate (`25b68dad91920b1ed79d357534b8c4582f62d80d`, v9.3.2)

| vscode-textmate file | LoomEditor file | Status |
|---|---|---|
| `src/onigLib.ts` | `LoomEditor.TextMate/Oniguruma/` (`IOnigLib`, `IOnigScanner`, `IOnigString`, `OnigMatch.cs`, `FindOption`) | ✅ P4-04; `disposeOnigString` is `IDisposable.Dispose` |
| `src/tests/onigLibs.ts` | `LoomEditor.TextMate.Tests/TestHelpers/OnigTestLib.cs` | ✅ P4-04 (synchronous) |
| `src/utils.ts` | `LoomEditor.TextMate/Grammar/` (`RegexSource`, `Strings`, `CachedFn`) | 🔶 P4-05: `clone`/`mergeObjects` (methods of the raw types, P4-06), `performanceNow` not ported (`containsRTL` is `Strings.ContainsRtl`, P4-16); `OrMask` is a `[Flags]` enum |
| `src/matcher.ts` | `LoomEditor.TextMate/Grammar/Matcher.cs` | ✅ P4-05 |
| `src/rule.ts` | `LoomEditor.TextMate/Grammar/` (`RuleId`, `RegExpSource`, `RegExpSourceList`, `CompiledRule`; `IRuleRegistry.cs` with `IGrammarRegistry`, `IRuleFactoryHelper` and `IRuleRegistryAndOnigLib` for `IRuleRegistry & IOnigLib`; `Rule`, `CaptureRule`, `MatchRule`, `IncludeOnlyRule`, `BeginEndRule`, `BeginWhileRule`, `RuleFactory`) | ✅ P4-11, P4-12, without `$location` (no location metadata) |
| `src/grammar/grammarDependencies.ts` | `LoomEditor.TextMate/Grammar/` (`IncludeReference.cs` with `parseInclude`; `GrammarDependencies.cs` + `.Collect.cs`: reference classes, `ExternalReferenceCollector`, `ScopeDependencyProcessor`) | ✅ P4-12, P4-13; the processor takes `lookup` / `injections` functions instead of `IGrammarRepository` (ADR 0008, decision 3) |
| `src/grammar/basicScopesAttributeProvider.ts` | `LoomEditor.TextMate/Grammar/` (`BasicScopeAttributesProvider.cs` with `BasicScopeAttributes`, `ScopeMatcher.cs`) | ✅ P4-13; `ScopeMatcher` ends its regex with `\z` (not `$`) as .NET's `$` also matches before a final newline |
| `src/debug.ts` | `LoomEditor.TextMate/Grammar/DebugFlags.cs` | ✅ P4-11, debug mode always off |
| `src/encodedTokenAttributes.ts` | `LoomEditor.TextMate/Grammar/` (`EncodedTokenAttributes`, `StandardTokenType.cs` with `OptionalStandardTokenType`) | ✅ P4-10, metadata as `uint`; `FontAttribute` not ported (ADR 0008) |
| `src/tests/grammar.test.ts` (metadata tests, 34–107) | `LoomEditor.TextMate.Tests/Grammar/EncodedTokenAttributesTests.cs` | ✅ 9 / 9 (the rest of the file comes with the grammar cards) |
| `src/grammar/grammar.ts` (1/6: `IThemeProvider`, `IGrammarRepository`, `AttributedScopeStack`, `BalancedBracketSelectors`, `nameMatcher`) | `LoomEditor.TextMate/Grammar/` (`IGrammarRepository.cs` with both interfaces, `AttributedScopeStack.cs`, `BalancedBracketSelectors.cs`, `NameMatcher.cs`) | 🔶 P4-14; no font attributes (ADR 0008); `AttributedScopeStack` takes an `IScopeAttributesContext` (`ThemeProvider`, `GetMetadataForScope`) that `Grammar` will implement (P4-17) instead of the `Grammar` class; instance `equals` is `EqualsStack` |
| `src/grammar/grammar.ts` (2/6: `StateStackImpl`, `StateStackFrame`) + `src/main.ts` (`StateStack`, `INITIAL`) | `LoomEditor.TextMate/Grammar/` (`StateStack.cs` with `StateStack.Initial`, `StateStackImpl.cs`, `StateStackFrame.cs`) | 🔶 P4-15; `StateStackImpl.Null` is `INITIAL`; instance `equals` is `Equals(StateStack?)`, `GetHashCode` not overridden; `GetRule` returns a non-null `Rule` |
| `src/grammar/grammar.ts` (3/6: `LineTokens`, `TokenTypeMatcher`) + `src/main.ts` (`IToken`, `ITokenizeLineResult`, `ITokenizeLineResult2`) | `LoomEditor.TextMate/Grammar/` (`LineTokens.cs`, `TokenizeLineResult.cs` with `Token`, `TokenizeLineResult`, `TokenizeLineResult2`, `TokenTypeMatcher`) | 🔶 P4-16; no `fonts` (`FontInfo` / `LineFonts` not ported, ADR 0008); binary tokens are `uint[]`; `containsRTL` is now ported in `Strings.ContainsRtl` (same regex as Core's `StringClassification`, which is internal to Core) |
| `src/grammar/grammar.ts` (4/6: `createGrammar`, `Grammar` (tokenization in the row below), `Injection`, `collectInjections`, `initGrammar`) + `src/main.ts` (`IGrammarConfiguration`) | `LoomEditor.TextMate/Grammar/` (`Grammar.cs`, `Grammar.Injections.cs`, `GrammarConfiguration.cs`; `IGrammarRepository.cs` also has `IGrammarRepositoryAndThemeProvider`) | 🔶 P4-17; `createGrammar` is `Grammar.Create`; an injection grammar is compiled as a `RawRule` with the grammar's `name`, `patterns` and `repository` (upstream passes the `IRawGrammar` as a rule); no `fonts`; `tokenizeLine*` come with P4-19 |
| `src/grammar/tokenizeString.ts` (1/2: `TokenizeStringResult`, `matchRuleOrInjections`, `matchRule`, `matchInjections`, `prepareRuleSearch`, `prepareRuleWhileSearch`, `getFindOptions`) | `LoomEditor.TextMate/Grammar/TokenizeString.Match.cs` (`static partial class TokenizeString`, `MatchResult`, `MatchInjectionsResult`) | 🔶 P4-18; the `UseOnigurumaFindOptions` branches are not compiled (the flag is a constant false); `GetFindOptions` is kept unused; no debug logging; the loop and captures come with P4-19 |
| `src/grammar/tokenizeString.ts` (2/2: `_tokenizeString`, `_checkWhileConditions`, `handleCaptures`, `LocalStackElement`) + `src/grammar/grammar.ts` (`tokenizeLine`, `tokenizeLine2`, `_tokenize`) + `src/main.ts` (`IGrammar`) | `LoomEditor.TextMate/Grammar/` (`TokenizeString.cs`, `TokenizeString.While.cs`, `TokenizeString.Captures.cs`, `Grammar.Tokenize.cs`, `IGrammar.cs`) | ✅ P4-19; time limit in milliseconds with `Stopwatch` (0 = none); no `lineFonts`; no debug logging; `scanNext`'s shared variables live in a private `ScanState`; a `StateStack` that is not a `StateStackImpl` is rejected with `ArgumentException` |
| `src/registry.ts` (`SyncRegistry`) + `src/main.ts` (`RegistryOptions`, `Registry`, `IGrammarConfiguration`, `parseRawGrammar`) + `src/diffStateStacks.ts` | `LoomEditor.TextMate/Grammar/` (`SyncRegistry.cs`, `Registry.cs`, `RegistryOptions.cs`, `DiffStateStacks.cs`; `GrammarConfiguration.cs` from P4-17) | ✅ P4-20; **synchronous** (ADR 0008, decision 3): `loadGrammar*` return `IGrammar?`, the loader is a `Func<string, RawGrammar?>`, the loader is called once per scope name; loading a scope name without grammar throws `InvalidOperationException` like upstream rejects; `SyncRegistry.Injections` returns an empty list instead of `undefined`; `parseRawGrammar` is `Registry.ParseRawGrammar` |
| `src/tests/tokenization.test.ts` (runner + `test-cases/first-mate/tests.json`), `src/tests/resolver.ts` | `LoomEditor.TextMate.Tests/Grammar/` (`TokenizationSuiteRunner.cs`, `FirstMateTests.cs`) | ✅ P4-21: 64 / 64 cases pass, no bug found; raw grammars are parsed once per file and shared between cases (the grammar clones them); `resolver.ts` is not ported (the runner builds its options inline, as upstream does); the suite1 files come with P4-22 |
| `test-cases/suite1/tests.json`, `test-cases/suite1/whileTests.json`; `src/tests/grammar.test.ts` (`Shadowed rules are resolved correctly`) | `LoomEditor.TextMate.Tests/Grammar/` (`Suite1Tests.cs`, `WhileTests.cs`, `GrammarRegressionTests.cs`) | ✅ P4-22: 20 / 20 suite1 and 9 / 9 while cases pass (plist grammars included), no bug found; the shadowed-rules test is skipped as upstream; `'Fonts are correctly set'` and `'… 2'` are not ported (ADR 0008, decision 3) |
| `src/rawGrammar.ts` | `LoomEditor.TextMate/Parsing/` (`RawGrammar`, `RawRule`, `RawRepository`, `RawCaptures`) | ✅ P4-06, without `ILocation`/`ILocatable`; `clone` is `DeepClone()` |
| `src/parseRawGrammar.ts` | `LoomEditor.TextMate/Parsing/RawGrammarReader.cs` | ✅ P4-06 (JSON with `System.Text.Json`), P4-07 (plist) |
| `src/plist.ts` | `LoomEditor.TextMate/Parsing/` (`PlistParser`, `PlistParser.Values`, `JsNumbers` for `parseInt`/`parseFloat`) | ✅ P4-07: builds a `JsonNode` tree; `parseWithLocation` not ported; `<date>` kept as text |
| `src/json.ts`, `src/tests/json.test.ts` | — | ❌ not ported (ADR 0008, decision 3) |
| `src/theme.ts` | `LoomEditor.TextMate/Theme/` (`Theme`, `ThemeTrieElement`, `ThemeTrieElementRule`, `ScopeStack.cs` with `ThemeScopes`, `StyleAttributes`, `RawTheme`, `ThemeParser` with `resolveParsedThemeRules`, `ParsedThemeRule`, `FontStyle`, `ColorMap`) | ✅ P4-08, P4-09, without font attributes; sorts are stable as in JavaScript |
| `src/tests/themes.test.ts` | `LoomEditor.TextMate.Tests/Theme/` (`ThemeParsingTests`, `ThemeMatchingTests`, `ThemeResolvingTests`) | 🔶 every matching, parsing and resolving test. Not ported: the data-driven theme tests (73–123) and the timing tests (125, 129) — 11 MB of test data, covered by the golden tests (P4-48) — and the font tests (ADR 0008). `assertThemeEqual` compares the color map, the defaults and the trie (upstream compares only the color map, by mistake) |
| `test-cases/first-mate/`, `test-cases/suite1/` | `LoomEditor.TextMate.Tests/TestCases/` | copied byte for byte (P4-06) |
| `src/tests/matcher.test.ts` | `LoomEditor.TextMate.Tests/Grammar/MatcherTests.cs` | ✅ 27 / 27 |


### VS Code workbench (`0c9c0a4295a9222e12a605537738719455ae3056`, `src/vs/workbench/services/textMate/browser`)

| Upstream | LoomEditor | Status |
|---|---|---|
| `tokenizationSupport/textMateTokenizationSupport.ts` | `LoomEditor.TextMate/Tokenization/TextMateTokenizationSupport.cs` | 🔶 P4-33; no tokenization time telemetry (`_reportTokenizationTime`, `_reportSlowTokenization`), no string `tokenize`; `onDidEncounterLanguage` is the `EncounteredLanguage` event; the 500 ms limit is a constant; `StateStack` implements Core's `IState`; a stopped-early line is traced with `Debug.WriteLine` |
| `tokenizationSupport/tokenizationSupportWithLineLimit.ts` | `LoomEditor.TextMate/Tokenization/TokenizationSupportWithLineLimit.cs` | 🔶 P4-33; the limit (`editor.maxTokenizationLineLength`, default 20 000) is a constructor argument, not an observable; no disposable parameter; 5 new tests (`TextMateTokenizationSupportTests`) |
| `common/TMScopeRegistry.ts`, `common/TMGrammarFactory.ts`, `common/TMGrammars.ts` (`ITMSyntaxExtensionPoint`) | `LoomEditor.TextMate/Tokenization/` (`TMScopeRegistry`, `TMGrammarFactory`, `LanguageDefinition`, `LanguageDefinitionReader`) | 🔶 P4-34; synchronous factory (`Func` file reader instead of `host.readFile`); `language.json` instead of extension manifests (ADR 0008 decision 6); no `sourceExtensionId`; `createGrammar` merges the injected embedded languages into a copy; `Registry`'s `loadGrammar` goes through a thread-safe `RawGrammarCache` (each file parsed once) |
| `browser/textMateTokenizationFeatureImpl.ts` (`_validateGrammarDefinition`, `_createTokenizationSupport`) | `LoomEditor.TextMate/Tokenization/` (`LanguageRegistry`, `TextMateTokenizationFactory`) | 🔶 P4-34; the language service is `LanguageRegistry` (ids via Core's `LanguageIdCodec`, extension / file name lookup, `plaintext` always present); no `onDidEncounterLanguage` hook, no telemetry; 6 new tests |
| `extensions/{csharp,typescript-basics,javascript,json,html,css,xml,markdown-basics,python,yaml}/syntaxes/*` | `LoomEditor.TextMate/Grammars/<language>/` (embedded, with `language.json`) and `BuiltInLanguages.cs` | ✅ P4-35; copied unchanged, ten languages (+ `jsonc`), licenses in `NOTICE.md` |
| `editor/common/model/mirrorTextModel.ts`, `model/prefixSumComputer.ts` (not `ConstantTimePrefixSumComputer`), `core/edits/lengthEdit.ts` and the used members of `edits/edit.ts`, `model/textModelStringEdit.ts` (`linesLengthEditFromModelContentChange`), `workbench/.../textMate/browser/indexTransformer.ts`, `test/.../arrayOperation.test.ts`, `test/common/viewModel/prefixSumComputer.test.ts` | `LoomEditor.Core/Model/` (`MirrorTextModel`, `PrefixSumComputer`), `LoomEditor.Core/Text/` (`LengthEdit`, `LengthReplacement`, `OffsetRange` +3 members), `LoomEditor.Core/Model/Tokens/` (`IndexTransformer`, `ModelContentChangeEdits`) | 🔶 P4-36; `BaseEdit`/`BaseReplacement` are folded into the non-generic `LengthEdit`/`LengthReplacement` (only the members used); `MirrorTextModel` has no `URI` and takes a `MirrorModelChangedEvent` copy instead of `IModelChangedEvent`; `LengthReplacement.Slice` is static |
| `workbench/.../backgroundTokenization/worker/textMateWorkerTokenizer.ts` (not `_getFontTokensUpdate`), `textMateTokenizationWorker.worker.ts` (not `$init`), `textMateWorkerHost.ts` (not `$readFile`, `$reportTokenizationTime`) | `LoomEditor.TextMate/Tokenization/Worker/` (`TextMateWorkerTokenizer`, `TokenizationWorker`, `WorkerMessage`, `StateDeltas`, `ITextMateWorkerHost`) | 🔶 P4-37; see "Behavior differences" (thread + channel instead of a web worker) |
| `workbench/.../backgroundTokenization/textMateWorkerTokenizerController.ts` (not `_stringEditFromChanges`, logging, font tokens), `threadedBackgroundTokenizerFactory.ts` (not the WASM / web-worker plumbing, telemetry, mismatch notification), `textModel.ts` (`_MODEL_SYNC_LIMIT`, `isTooLargeForSyncing`, `onDidChangeAttached`) | `LoomEditor.TextMate/Tokenization/` (`TextMateWorkerTokenizerController` + `.Rebase`, `ThreadedBackgroundTokenizerFactory`), `Worker/ITokenizationWorker`, `LoomEditor.Core/Threading/IUiDispatcher`, `TextModel.Tokenization.cs` | 🔶 P4-38; states arrive as `StateStack` references (no `applyStateStackDiff`); the worker is created by a function and synchronously; the controller and factory take `maxTokenizationLineLength` as a number (no observable); `TextModel.ModelSyncLimit` is a settable static, like Monaco's, so tests can lower it. `TextMateTokenizationSupport` needed no change: it already takes the factory's `CreateBackgroundTokenizer` as a function (wired by P4-42) |
| `platform/theme/common/colorUtils.ts` (`resolveDefaultColor`, `resolveColorValue`), `colors/editorColors.ts`, `editorColorRegistry.ts`, `colors/baseColors.ts` (the ids `EditorTheme` uses and those they refer to), `workbench/.../themes/common/colorThemeData.ts` (`themeTypeSelector`, `type`) | `LoomEditor.Themes/` (`ThemeType` (public), `ThemeTypes`, `EditorColorDefaults`) | ✅ P4-39; a static table instead of the color registry; no color transforms (no listed id uses one) |
| `workbench/.../themes/common/colorThemeData.ts` (`tokenColors`, `getTokenColorIndex`, `tokenColorMap`, `_loadColorTheme` JSON branch, `defaultThemeColors`, `TokenColorIndex`, `normalizeColor`), `themeCompatibility.ts` (`convertSettings`), `base/common/color.ts` (`Color.fromHex`) | `LoomEditor.Themes/` (`VsCodeThemeLoader` + `.Compatibility`, `VsCodeTheme`, `TokenColorIndex`, `ColorNormalization`, `TokenColorRule` (public), `ThemeColor.Parse` accepts `#RGBA`) | 🔶 P4-40; see "Behavior differences" |
| `extensions/theme-defaults/themes/*.json` + `package.json`, `package.nls.json` (the ten default themes, their labels and `uiTheme`) | `LoomEditor.Themes/BuiltIn/*.json` (verbatim, embedded), `BuiltInThemes.cs`, `EditorTheme` (`Type`, `TokenColors`, `Load`, `BuiltIn`, `Dark` = Dark Modern, `Light` = Light Modern; public) | ✅ P4-41; the English labels are the display names; `EditorTheme.Dark` and `Light` are now loaded from the real files (their old hand-written values are gone) |
| `workbench/.../textMate/browser/textMateTokenizationFeatureImpl.ts` (`_getOrCreateGrammarFactory`, `_updateTheme`, `toColorMap`) | `LoomEditor.View/Highlighting/` (`SyntaxHighlighter`, `ThemeToTextMate`, `NoopTokenizationScheduler`), `EditorContext` (owns the highlighter; now `IDisposable`) | 🔶 P4-42; no extension points: a language gets its support when `SetLanguage` first needs it, unknown ids are plain text; without a dispatcher there is no worker and the model's scheduler runs nothing; the scheduler is also a no-op with a worker (only used by the fallback tokenizer for models too large to sync) |
| `editor/common/viewModel/viewModelImpl.ts` (`visibleLinesStabilized`, `_handleVisibleLinesChanged`, `onDidChangeTokens`, `getViewLineData`), `viewEvents.ts` (`ViewTokensChangedEvent`) | `LoomEditor.View/Layout/ViewModel.Tokens.cs`, `ViewportData.GetTokens`, `Events/ViewEvent.cs` | 🔶 P4-43; the view model is the one attached view of the model; view lines are model lines (no coordinate conversion, no `ModelTokensChangedEvent` outgoing event); the stabilized call is scheduled on the animation-frame scheduler after each viewport change; tokens are inflated per line on demand |
| `editor/common/viewLayout/viewLineRenderer.ts` (`transformAndRemoveOverflowing`) | `LoomEditor.Rendering/LineStyle.cs` (`StyleRun`, `LineStyle`), `Win2D/Win2DTextMeasurer.cs`, `TextViewRenderer.cs` | 🔶 P4-44, inspiration only: tokens become merged color/font-style runs cut at the rendered length (`StopRenderingLineAfter`); no HTML spans, no whitespace or bidi handling; a line of the default foreground and no style is `LineStyle.Plain`; token backgrounds and selection foreground are not drawn |

## Monaco tests ported in phases 1 and 3

The rows for phase 3 (`cursor*`, `wordOperations`, `shiftCommand`, `columnSelectionPaste`, `multicursor`, `intervalTree`, `cursorColumns`, `characterClassifier` …) are in this table, with the card that ported them. Phase 3 total: see the "Ported / total" column; every gap says why.

Counted at the pinned commit by test name (`test(...)` / `it(...)` in the Monaco file; a `// Monaco: '<name>'` comment
marks each ported test). "Ported" includes tests adapted to C# types.

| Monaco test file (suite) | Ported / total | Not ported, and why |
|---|---|---|
| `core/range.test.ts` | 9 / 9 | |
| `core/cursorColumns.test.ts` | 6 / 6 | P3-03 |
| `core/characterClassifier.test.ts` | 1 / 1 | P3-04 |
| `model/intervalTree.test.ts` (`IntervalTree 1`, `IntervalTree 2`) | 25 / 25 | P3-06, P3-07 |
| `cursorAtomicMoveOperations.test.ts` | 2 / 2 | P3-22 |
| `cursorMoveCommand.test.ts` | 47 / 55 | suite `Cursor move command - foldedLine unit` (8): needs `setHiddenAreas` (folding, phase 5) |
| `cursor.test.ts` (suite `Editor Controller - Cursor`, movement and state) | 73 / 75 | `issue #144041`, `issue #140195`: word wrap and injected text (phase 6). The 6 column selection tests are ported in P3-28 (`ColumnSelectionTests`). `select all` runs `CursorMoveCommands.SelectAll` (`editor.action.selectAll` is owned by `SelectAllFeature`); `setSelection / setPosition with source` checks the `CursorStateChangedEvent` source (no editor API, no tokenization registration) |
| `wordOperations.test.ts` | 53 / 56 | `cursorWordLeft - Recognize words` and `cursorWordRight - Recognize words` (2): need `Intl.Segmenter` (not available); `deleteWordLeft - issue #91855` (1): needs auto-closing pairs (phase 4–5) |
| `shiftCommand.test.ts` | 23 / 25 | `issue #348`, `issue #1609`: need a language with doc-block on-enter rules (phases 4–5) |
| `wordPartOperations.test.ts` | 15 / 15 | |
| `cursor.test.ts` (`issue #832: word right`) | 1 / 1 | runs `CursorMoveCommands.Word` directly (the `WordSelect` mouse commands are P3-44) |
| `cursor.test.ts` (mouse commands: `issue #33788`, `issue #12887`, `Double-click on punctuation...`, `issue #7100`, `issue #112039`, `issue #158236`) | 6 / 6 | `MouseCommandTests` (P3-44) |
| `cursor.test.ts` (Backspace and Delete: `issue #1140`, `issue #84897`, `issue #122914`, two `issue #99629`, `UseTabStops is off`, `Backspace removes whitespaces with tab size`) | 7 / 7 | `Backspace removes whitespaces with tab size` calls `TextModel.Undo` (`CoreEditingCommands.Undo` is P3-37) |
| `cursor.test.ts` (`issue #9675: Undo/Redo adds a stop in between CHN Characters`) | 1 / 1 | `CompositionTests` (P3-49) |
| `cursor.test.ts` (`Editor Controller`: typing, Enter, `removeAutoWhitespace`, undo) | 17 / 17 | The two `removeAutoWhitespace` tests that use the Tab command are in `TabTests` (P3-34). Undo goes through `TextModel.Undo` (core command: P3-37) |
| `cursor.test.ts` (suite `Undo stops`) | 11 / 11 | Undo goes through `TextModel.Undo` (core command: P3-37) |
| `cursor.test.ts` (Tab and Shift+Tab: `monaco-editor#443`, `Bug 9121`, `Bug #18293`, `issue #95591`, `Bug #16657`, `issue #3071`, `issue #3463`, `issue #4312`, `Cursor honors insertSpaces configuration on tab`, `PR #5423`) | 10 / 10 | Undo goes through `TextModel.Undo` (core command: P3-37) |
| `cursor.test.ts` (paste: `issue #10212`, `#74722`, `#4996`, `#16155`, `#43722`, two `#46440`, `#256039`, `#85712`, `#15118`, `#40695`) | 11 / 11 | |
| `columnSelectionPaste.test.ts` (`Column selection paste`) | 44 / 57 | `uses successive model lines when word wrap is enabled` (phase 6); `reports pasted ranges for cursors and multiline destination selections` (needs the `onDidPaste` editor event); `respects overtype and pads short rows` (overtype mode); the 10 tests of the nested suite `clipboard metadata` (browser clipboard events: P3-47 has its own). `responds to spread, full, and spread configuration transitions` does not check `EditorOptions.multiCursorPaste.validate` |
| `cursor.test.ts` (cut: `bug #16740`, `issue #128602`) | 2 / 2 | |
| `cursor.test.ts` (`issue #44805: Should not be able to undo in readonly editor`) | 1 / 1 | |
| `multicursor.test.ts` | 2 / 76 | `issue #26393: Multiple cursors + Word wrap` (word wrap, phase 6); the 73 tests of `Multicursor selection` and below need Find (phase 5) |
| `pieceTreeTextBuffer.test.ts` | 84 / 87 | suite `chunk based search` (3): search is deferred to phase 5 (Find) |
| `linesTextBuffer.test.ts` | 29 / 29 | |
| `linesTextBufferBuilder.test.ts` | 12 / 12 | |
| `model.test.ts` (`Model`, `Model Line Separators`) | 29 / 29 | |
| `model.test.ts` (`Words`) | 0 / 3 | word helpers: phases 4–5 |
| `editableTextModel.test.ts` | 62 / 62 | |
| `editableTextModelAuto.test.ts` | 4 / 4 | the disabled `GENERATE_TESTS` loop is ported as a helper |
| `textModel.test.ts` (`TextModel.mightContainRTL`, `createSnapshot`, `TextModelData.fromString`) | 14 / 14 | |
| `textModel.test.ts` (`Editor Model - TextModel`) | 14 / 26 | indentation guessing and `normalizeIndentation` (10), `issue #168836` indent size option (1), `does not use events internally` (1): phases 4–5. The 3 NaN / float `validatePosition` / `validateRange` tests are adapted to `int` inside the validation tests |
| `model.line.test.ts` | 0 / 53 | `ModelLine` tokens and `getIndentLevel`: phase 4 (tokenization) |
| `viewModel/prefixSumComputer.test.ts` (`Editor ViewModel - PrefixSumComputer`) | 48 / 48 | P4-36: run on `PrefixSumComputer` only; the same tests ran on `ConstantTimePrefixSumComputer` (not ported) too |
| `textMate/test/browser/arrayOperation.test.ts` | 1 / 1 | P4-36 |
| `textChange.test.ts` | 0 / 10 | **gap**: card P1-26 wrongly said Monaco has no test file for `textChange.ts`. LoomEditor has its own `TextChange*Tests` instead; porting these 10 tests (9 `TextChangeCompressor`, 1 `issue #118041`) is a follow-up |
| `editStack.test.ts` | 0 / 1 | tests binary serialization, which is not ported |

## Monaco tests ported in phase 2

| Monaco test file (suite) | Ported / total | Not ported, and why |
|---|---|---|
| `base/test/common/scrollable.test.ts` (`SmoothScrollingOperation`) | 5 / 5 | |
| `editor/test/common/viewLayout/lineHeights.test.ts` (`LineHeightsManager`, `LineHeightsManager (auto-commit on read)`) | 38 / 38 | |
| `editor/test/common/viewLayout/linesLayout.test.ts` (`Editor ViewLayout - LinesLayout`) | 12 / 12 | |
| `base/test/browser/ui/scrollbar/scrollableElement.test.ts` (`MouseWheelClassifier`) | 9 / 9 | |
| `editor/test/browser/config/editorLayoutProvider.test.ts` (`Editor ViewLayout - EditorLayoutProvider`) | 10 / 20 | the 8 minimap tests (`EditorLayoutProvider 9` … `14`) and the 2 word-wrap tests (`issue #31312`, `leaves the rightmost column…`): minimap and word wrap are phase 6. The 10 ported ones are `EditorLayoutProvider 1` … `8 - rounds floats` |

## Cut points for phases 4–5

Where phase 3 stops because Monaco needs a language configuration, tokens or bracket matching. Without a language
configuration Monaco itself behaves like this for a plain-text model, so phase 3 is faithful for plain text. Phases 4–5 start
from this list (every place is marked `// Phase 4–5 cut point` in the code).

| Where (LoomEditor) | Monaco (pinned commit) | What is missing |
|---|---|---|
| `Cursors/TypeOperations.cs` | `cursorTypeEditOperations.ts` 29–113, 115–152, 154–404, 421–478, 480–546; 591–656 | auto-indent on type, auto-closing overtype and open characters, surround selection, electric characters; the composition-as-surround check |
| `Cursors/TypeEditOperations.Enter.cs` | `cursorTypeEditOperations.ts` 591–615 (`getEnterAction`), 620–656 (`getIndentForEnter`) | on-enter rules, indent after Enter |
| `Cursors/TypeEditOperations.Tab.cs` | `cursorTypeEditOperations.ts` 877–916 (`_goodIndentForLine`) | smart Tab (`getInheritIndentForLine`, `getEnterAction`) |
| `Cursors/DeleteOperations.cs`, `DeleteWordContext.cs`, `WordOperations.Delete.cs`, `Features/WordOperations/DeleteWordCommand.cs` | `cursorDeleteOperations.ts` 167–169; `cursorWordOperations.ts` | auto-closing pair delete, `autoClosingPairs` for word deletes |
| `Cursors/CursorConfiguration.cs`, `AutoClosedAction.cs`, `CursorsController.Edits.cs` | `cursorCommon.ts`; `cursor.ts` 682–748 | `autoClosingPairs`, `surroundingPairs`, comment tokens, auto-closed characters (need model decorations) and the `"snippet"` source |
| `Cursors/Commands/ShiftCommand.cs` | `shiftCommand.ts` 144–176 | enter action of the previous line (doc blocks) |
| `Controller/ViewController.cs` | `viewController.ts` (`_trySelectBracketContent`, string content) | double-click on a bracket or string selects its content (bracket matching, tokens) |
| `Layout/RevealCalculator.cs` | `viewLines.ts` reveal | sticky scroll lines |

## Done in phase 3 (editing)

The step-by-step cards are in **[docs/tasks/phase-3/](tasks/phase-3/)** (P3-01 … P3-55); the design is
[ADR 0006](adr/0006-editing-and-text-input.md). Main design decisions:

- Monaco's cursor (`editor/common/cursor/*`) is ported into `LoomEditor.View/Cursors/` as is: `CursorsController`,
  `CommandExecutor`, move, type, delete, word, shift and paste operations, with Monaco's tests.
- Ranges that follow edits are tracked ranges on a ported interval tree; there is no public decorations API yet.
- Commands and keybindings: Monaco's core commands and VS Code's default Windows shortcuts; `when` clauses are C# predicates.
- The mouse is hit-tested on our own layout; the click logic (`MouseDownOperation`, `ViewController`, drag scrolling) is pure.
- The clipboard keeps the multi-cursor metadata in memory, like Monaco; text goes to Windows with CRLF.
- Plain typing comes from `CharacterReceived`, composition from `CoreTextEditContext` (Windows 11 only).
- The "done when" criterion is met: Japanese (Microsoft IME) and Chinese (Microsoft Pinyin) input work in the Native AOT
  Gallery (`docs/testing/phase-3-manual-tests.md`, 2026-10-19). Not tested: Korean IME and Windows 10.

## Done in phase 2 (rendering)

The step-by-step cards are in **[docs/tasks/phase-2/](tasks/phase-2/)** (P2-01 … P2-23); the design is
[ADR 0005](adr/0005-viewport-rendering.md). Main design decisions:

- Scrolling and layout are Monaco's, ported to `LoomEditor.View/Layout/` with integer arithmetic (`Scrollable`,
  `LinesLayout`, `ViewLayout`, `ViewModel` subset), so a 100 MB document keeps exact offsets (`bigNumbersDelta`).
- The control draws only the visible lines on a viewport-sized `CanvasControl`: no `CanvasVirtualControl`, no
  `ScrollViewer`. Scrollbars are XAML `ScrollBar` primitives mirroring the `Scrollable`.
- Text is measured and laid out by DirectWrite through `Win2DTextMeasurer`; tabs are DirectWrite tab stops, so
  proportional fonts, wide characters and emoji need no special case. Line layouts are cached by text.
- The caret blinking state machine (`CursorBlinking`) has no timer; the control drives it with a `DispatcherQueueTimer`.
- Font, tab size and DPI changes go through one method, `RecreateTextMeasurer`.
- The "done when" criterion is met: the Gallery scroll test on a 100 MB document, in a Native AOT build, runs at
  60 fps with p95 17 ms (`docs/performance.md`, "Scrolling (card P2-22)").

## Done in phase 1 (piece tree and text model)

The step-by-step cards are in **[docs/tasks/](tasks/README.md)** (P1-01 … P1-32). Main design decisions:

- `PieceTreeBase` is split into `partial` files by concern (`.Lines`, `.Positions`, `.Insert`, `.Delete`, `.Crlf`,
  `.Snapshot`, `.Eol`) and **owns** a `RedBlackTree` (composition) instead of mixing tree and text code.
- **One sentinel node per tree** instead of Monaco's global mutable `SENTINEL` (thread safety, parallel tests).
- `StringBuffer` is a growable `char[]`, not a `string` (appending to the change buffer must not be quadratic).
- `PieceTreeTextBuffer` / `ITextBuffer` stay `internal`; the public API is `TextModel`, which uses `TextEdit` for edits.
- Search in the piece tree is deferred to phase 5 (Find); decorations and injected text to phase 2+.
- Monaco's workspace-wide `IUndoRedoService` is replaced by a per-model `UndoRedoStack`.
