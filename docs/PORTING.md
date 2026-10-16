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
| `WordCharacterClassifier` | `wordSegmenterLocales` is ignored: `FindPrevIntlWordBeforeOrAtOffset` / `FindNextIntlWordAtOrAfterOffset` always return `null` | No Native AOT-friendly equivalent of `Intl.Segmenter` |
| Tabs in rendered lines | Drawn with DirectWrite tab stops (`IncrementalTabStop = tabSize × space width`) instead of being expanded to spaces by `viewLineRenderer.ts`; a tab after a full-width character ends on a pixel tab stop, not a visible-column one | One UTF-16 index is one layout index, so no `CharacterMapping` is needed (ADR 0005, decision 5) |
| `CommandRegistry` | One per editor instead of a global registry | Two editors on a page never share state |
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
| `editor/common/model/pieceTreeTextBuffer/*` | `LoomEditor.Core/Model/PieceTree/` | ✅ ported (phase 1), except search (phase 5) |
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
| `editor/common/cursor/oneCursor.ts`, `cursorCollection.ts`, `cursorContext.ts` | `LoomEditor.View/Cursors/` (`Cursor`, `CursorCollection`, `CursorContext`) | ✅ ported (P3-16); `CursorCollection.lastAddedCursorIndex` follows the code (a full-array index), not Monaco comment |
| `editor/common/cursor/cursor.ts` (`CursorsController`, `CursorModelState`, `AutoClosedAction`) | `LoomEditor.View/Cursors/CursorsController*.cs` | ✅ ported (P3-18, P3-19) except typing, paste, cut and composition (later cards) and the auto-closing and snippet cut points. `ModelInjectedTextChangedEvent` is not ported (no injected text) |
| `editor/common/viewModelEventDispatcher.ts`, `editor/common/viewEvents.ts` | `LoomEditor.View/Events/` | 🔶 subset (P3-17): the dispatcher, the collector, `ViewCursorStateChangedEvent`, `ViewRevealRangeRequestEvent`, `CursorStateChangedEvent`, `ReadOnlyEditAttemptEvent`; no `Disposable` base, no duplicate-handler warning |
| `editor/common/cursor/*` | `LoomEditor.View/Cursors/` | ⏳ phase 3 |
| `editor/common/cursorCommon.ts` | `LoomEditor.View/Cursors/` | 🔶 types (P3-12) and `CursorConfiguration` (P3-13): `SingleCursorState`, `CursorState` and partial forms, `EditOperationType`, `SelectionStartKind`, `ColumnSelectData`, `EditOperationResult`, `ICursorSimpleModel` (+ `TextModelCursorSimpleModel`), `IsQuote`; the rest comes later; `CursorConfiguration` (P3-13) without the language parts and with the editor options it reads (`EditorCursorOptions`) |
| `editor/common/cursorEvents.ts` | `LoomEditor.View/Cursors/CursorChangeReason.cs` | 🔶 `CursorChangeReason` only (P3-12) |
| `editor/common/coordinatesConverter.ts` | `LoomEditor.View/Layout/` | 🔶 `ICoordinatesConverter` and `IdentityCoordinatesConverter` only (P3-12); the word-wrap converter comes with word wrap |
| `base/common/scrollable.ts` | `LoomEditor.View/Layout/` (`ScrollState`, `ScrollEvent`, `Scrollable`, `SmoothScrollingOperation`, scroll value types) | ✅ ported (P2-01, P2-02); `TimeProvider` replaces `Date.now()`; `ISmoothScrollPosition` extends `IScrollPosition` and `ScrollState` implements it (TypeScript structural typing) |
| `editor/common/viewLayout/lineHeights.ts` | `LoomEditor.View/Layout/` (`LineHeightsManager` (+ `.Changes.cs`), `CustomLine`, `CustomLineHeightData`) | ✅ ported (P2-03) without `CustomLineHeightData.fromDecorations` (no decorations yet); heights are `int`; `PendingChange` is an abstract record; `ArrayMap` is a private nested class |
| `editor/common/viewLayout/linesLayout.ts` | `LoomEditor.View/Layout/` (`LinesLayout` (+ `.Whitespaces.cs`, `.Changes.cs`, `.Viewport.cs`), `EditorWhitespace`, `IEditorWhitespace`, `IWhitespaceChangeAccessor`, `ILineHeightChangeAccessor`) | ✅ ported (P2-04, P2-05); heights are `int`; `PendingChanges` is a private nested class; `PartialViewLinesViewportData`, `ViewWhitespaceViewportData` and `Viewport` (from `viewModel.ts`) live next to it |
| `base/browser/ui/scrollbar/scrollableElement.ts` | `LoomEditor.View/Layout/` (`MouseWheelClassifier`, `MouseWheelScrolling`, `MouseWheelInput`, `MouseWheelOptions`) | ✅ adapted (P2-09), classifier and wheel arithmetic only; scrollbar drawing is XAML (ADR 0005) |
| `editor/common/viewLayout/viewLineRenderer.ts` | `LoomEditor.Rendering/` (`OverflowLabel`; the `stopRenderingLineAfter` constant in `TextViewRenderer`) | 🔶 partly ported (P2-13): only `stopRenderingLineAfter` and the "Show more" overflow label; the HTML renderer is not ported (ADR 0005) |
| `editor/browser/viewParts/viewLines/viewLines.ts` | `LoomEditor.Rendering/` (`TextViewRenderer`, `LineLayoutCache`, `ITextMeasurer`, `ILineLayout`, `Win2D/`), `LoomEditor.WinUI/Controls/` (`LoomEditor.Font.cs`) | 🔶 adapted (P2-11 to P2-14, P2-20): draws only the visible lines at the scroll position; line layouts cached by text; `_maxLineWidth` and its reset on flush, layout and font changes (`OnFontChanged`); no `ViewLine` DOM, no `CharacterMapping` (ADR 0005) |
| `editor/browser/viewParts/lineNumbers/lineNumbers.ts`, `editor/browser/viewParts/currentLineHighlight/currentLineHighlight.ts` | `LoomEditor.Rendering/` (`TextViewRenderer.Gutter.cs`, `RenderLineHighlight`) | 🔶 adapted (P2-15): drawn on the canvas; only `RenderLineNumbersType.On`, `renderFinalNewline = on`, `renderLineHighlight` (`none`, `gutter`, `line`, `all`), `renderLineHighlightOnlyWhenFocus = false`; no relative/interval line numbers or line-number decorations |
| `editor/browser/viewParts/viewCursors/viewCursors.ts`, `editor/browser/viewParts/viewCursors/viewCursor.ts` | `LoomEditor.Rendering/` (`CursorBlinking`, `TextEditorCursorBlinkingStyle`, caret drawing in `TextViewRenderer`) | 🔶 adapted (P2-16): blinking state machine without a timer (the control ticks it with a `DispatcherQueueTimer`, P2-19) and `Line` cursor style only. Behavior difference: the CSS-animated styles `Smooth`, `Phase` and `Expand` behave like `Blink`; no secondary cursors, block/underline styles or IME |
| `editor/common/viewLayout/viewLayout.ts` | `LoomEditor.View/Layout/` (`ViewLayout` (+ `.Scrollable.cs`: `EditorScrollDimensions`, `EditorScrollable`), `ViewLayoutOptions`, `ViewLayoutState`, `ScrollType`, `ContentSizeChangedEventArgs`) | ✅ ported (P2-08); no word-wrap branch in the content width, explicit options instead of `IEditorConfiguration` (`onConfigurationChanged` → `OnOptionsChanged`, `OnLayoutInfoChanged`, `OnFontInfoChanged`); `ContentSizeChangedEvent.attemptToMerge` not ported |
| `editor/common/config/fontInfo.ts` | `LoomEditor.View/Layout/` (`BareFontInfo`, `FontInfo`) | ✅ ported (P2-06), subset: no font feature/variation settings, no editor zoom, no serialization; Windows constants; records |
| `editor/browser/config/fontMeasurements.ts` | `LoomEditor.View/Layout/` (`FontMeasurements`, `ICharacterWidthReader`) | ✅ adapted (P2-06), measurement logic only; no cache, so the "re-measure when width <= 2" retry is not ported (clamp to 5 only); DirectWrite reader in P2-12 |
| `editor/common/config/editorOptions.ts` | `LoomEditor.View/Layout/` (`EditorLayoutInfo`, `OverviewRulerPosition`, `EditorLayoutInfoComputer`, `EditorLayoutInfoComputerEnv`, `EditorLayoutOptions`) | ✅ ported (P2-07), `computeLayout` subset: no minimap, word wrap or accessibility parts; rest of the file (options system) not ported |
| `editor/common/viewModel/viewModelImpl.ts` | `LoomEditor.View/Layout/` (`ViewModel`) | 🔶 subset: layout part (P2-10) and the cursor facade (P3-20: `ViewModel.Cursors.cs`, owns the `CursorsController`, event dispatcher and `CursorConfiguration`); identity line mapping (view lines = model lines); typing, paste, cut and composition facade methods come with their cards; projections in phase 6 |
| `editor/common/viewLayout/viewLinesViewportData.ts` | `LoomEditor.View/Layout/` (`ViewportData`) | 🔶 phase 2 subset (P2-10): fields and constructor only; no rendering data, wrapped-line or decoration queries |
| `editor/common/viewModel/*` (the rest) | `LoomEditor.View/Layout/` | ⏳ phase 6 |
| `microsoft/vscode-textmate` | `LoomEditor.TextMate/Grammar/` | ⏳ phase 4 |
| `editor/contrib/<feature>/` | `LoomEditor.View/Features/<Feature>/` | ⏳ phase 5 |

## Monaco tests ported in phase 1

Counted at the pinned commit by test name (`test(...)` / `it(...)` in the Monaco file; a `// Monaco: '<name>'` comment
marks each ported test). "Ported" includes tests adapted to C# types.

| Monaco test file (suite) | Ported / total | Not ported, and why |
|---|---|---|
| `core/range.test.ts` | 9 / 9 | |
| `core/cursorColumns.test.ts` | 6 / 6 | P3-03 |
| `core/characterClassifier.test.ts` | 1 / 1 | P3-04 |
| `model/intervalTree.test.ts` (`IntervalTree 1`, `IntervalTree 2`) | 25 / 25 | P3-06, P3-07 |
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
