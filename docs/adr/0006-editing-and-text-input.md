# ADR 0006 — Editing: Monaco's cursor in View, text input through `CharacterReceived` and `CoreTextEditContext`

- **Status:** accepted
- **Date:** 2026-10-15

## Context

Phase 3 makes the editor editable. The user must be able to select, use several cursors, type, delete, indent, copy,
paste, use the mouse, and type Japanese and Chinese through an IME. Monaco does all of this in
`src/vs/editor/common/cursor/` (the cursor and its operations) and `src/vs/editor/browser/` (commands, mouse,
clipboard, the browser `EditContext`).

The C# port has a text model with undo/redo (phase 1) and a view model with layout and scrolling (phase 2). It has
one `Selection` in `EditorContext`, no decorations, no editor options for indentation, and no grapheme support.

Three problems need a decision:

1. Monaco's cursor follows its selections across edits through **tracked ranges**. These are decorations stored in an
   interval tree (`intervalTree.ts`), which is not ported yet.
2. The browser gives Monaco one text-input path. A WinUI 3 **desktop** app has two. Plain typing (including dead keys
   and AltGr) arrives as `CharacterReceived` (`WM_CHAR`). IME composition arrives through `CoreTextEditContext`.
   On a desktop app, `CoreTextServicesManager.GetForCurrentView()` works only on **Windows 11** (microsoft-ui-xaml
   issue #4239). The repository supports Windows 10 2004 (`LoomWindowsMinVersion` = 10.0.19041.0).
3. Monaco's cursor needs editor options (`CursorConfiguration`), model options (`tabSize`, `insertSpaces`,
   `trimAutoWhitespace`), and the language configuration (auto-closing pairs, indentation rules). The language
   configuration comes only in phases 4–5.

## Decisions

1. **The cursor is Monaco's, ported to `LoomEditor.View/Cursors/`** with its tests: `CursorConfiguration`,
   `SingleCursorState`, `CursorState`, `Cursor`, `CursorCollection`, `CursorsController`, the move, word, type and
   delete operations, and the edit commands of `src/vs/editor/common/commands/`. Monaco's `ICommand` (an edit
   command) is named **`IEditCommand`** in C#. This avoids confusion with `EditorCommand` (a command run by id) and
   with `System.Windows.Input.ICommand`.
2. **The view model owns the cursors, as in Monaco.** `ViewModel` gets the cursor API of `viewModelImpl.ts` (`Type`,
   `Paste`, `Cut`, `ExecuteEdits`, `ExecuteCommands`, `SetCursorStates`, `RevealPrimaryCursor`…). View lines are still
   model lines (ADR 0005, decision 6), so the coordinates converter is Monaco's `IdentityCoordinatesConverter`.
3. **`EditorContext` creates the `ViewModel`.** Until phase 2 the control created it when the canvas was ready. The
   cursor must exist before that (the `Text` property is set before the control is loaded). `EditorContext` now
   creates the `ViewModel` in its constructor with `FontInfo.Default` (a placeholder font). The control replaces the
   font with `ViewModel.SetFontInfo` when the canvas is ready. `IEditorContext` gains `Selections` and
   `SetSelections`. `Selection` stays and means the **primary** selection.
4. **Tracked ranges use a faithful port of `intervalTree.ts`** in `LoomEditor.Core/Decorations/`, with its tests.
   `TextModel` gets Monaco's internal `_setTrackedRange` / `_getTrackedRange`. The public decorations API
   (`deltaDecorations`) is **not** part of phase 3. As in the piece tree (phase 1), there is one sentinel node per tree,
   not a global one.
5. **`TextModel` gets Monaco's model options** (`tabSize`, `indentSize`, `insertSpaces`, `trimAutoWhitespace`) with
   `UpdateOptions`. The edit path gets Monaco's identified edit operations (`forceMoveMarkers`,
   `isAutoWhitespaceEdit`), deferred event emission, auto-whitespace trimming and `ResultingSelection` on the raw
   change event. The control's `TabSize` property (P2-20) now writes the model option.
6. **No language configuration in phase 3.** Every branch of Monaco that needs a language is cut at a named point and
   behaves as Monaco does when the language has no configuration (no auto-closing, no auto-indent rules, no electric
   characters, no surround). Phases 4–5 fill these points. Each card names the cut.
7. **Graphemes are Monaco's.** `base/common/strings.ts` (code points, `GraphemeIterator`, the grapheme break table,
   emoji helpers) is ported to `LoomEditor.Core/Text/`. .NET `StringInfo` is **not** used, because it follows full
   UAX #29 and Monaco does not. The break table is a generated `ReadOnlySpan<int>` literal (no JSON at run time).
8. **Core commands and keybindings.** Monaco's `coreCommands.ts` becomes `LoomEditor.View/Commands/CoreNavigationCommands`
   and `CoreEditingCommands` (internal). They are registered in the editor's `CommandRegistry` under Monaco's ids
   (`cursorLeft`, `deleteLeft`…). The Windows key table of Monaco is in `LoomEditor.View/Commands/DefaultKeybindings.cs`
   with a small resolver. `when` clauses are C# predicates on a `KeybindingContext`; there is no expression parser.
9. **Mouse.** Hit testing is done on our own layout: the line is found with `ViewLayout`, the column with
   `ILineLayout.HitTest`, in the spirit of Monaco's GPU path (`viewLinesGpu.ts`). The click logic (`MouseDownState`,
   `ViewController.dispatchMouse`, drag scrolling) is ported as pure classes in `LoomEditor.View/Controller/`.
   Selection drag-and-drop is not part of phase 3.
10. **Clipboard.** Text goes to the Windows clipboard with CRLF line breaks (Monaco's `forceCRLF` on Windows). The
    multi-cursor metadata (`isFromEmptySelection`, `multicursorText`, `isBlock`) is kept **in memory** for the last
    copy, like Monaco's `InMemoryClipboardMetadataManager`. No custom clipboard format (no JSON, Native AOT).
11. **Text input has two paths.**
    - `CharacterReceived` → `ViewModel.Type(text, "keyboard")` for plain typing. Characters below U+0020 and U+007F are
      ignored, except `'\r'`, which types `"\n"`. Surrogate pairs are joined before typing.
    - `CoreTextEditContext` (Windows 11) → `ViewModel.CompositionType` / `Type` for IME composition, the emoji panel and
      other text services. It exposes the lines of the primary selection, like Monaco's `nativeEditContext.ts`.
    - While a composition is active, `CharacterReceived` and key bindings are ignored.
    - On Windows 10, `GetForCurrentView()` fails. The control catches the failure, writes one debug trace and works
      without IME. This is documented in `docs/PORTING.md`. Raising the minimum Windows version is not needed.
12. **Out of phase 3:** overtype mode, selection drag-and-drop, Find-based multi-cursor commands (`Ctrl+D`, phase 5),
    double-click on brackets or strings selecting their content (needs tokens), rich copy (HTML), touch input,
    Intl word segmentation (`wordSegmenterLocales`).

## Consequences

- `LoomEditor.View` gets three new folders: `Cursors/` (with `Cursors/Commands/`), `Controller/` (mouse logic) and
  `Events/` (view model events). `LoomEditor.Core` gets `Text/` additions (strings, cursor columns, word classifiers)
  and `Decorations/` (interval tree).
- `LoomEditor.Core` gives `InternalsVisibleTo` to `LoomEditor.View` and `LoomEditor.View.Tests` (the cursor uses the
  internal string, column, word and tracked-range helpers of Core), as `LoomEditor.View` already does for
  `LoomEditor.Rendering` (ADR 0005, decision 8).
- The public API grows: `IEditorContext.Selections` / `SetSelections`, the control's `IsReadOnly`, `Selections`,
  `SetSelections` and `SelectionChanged`, `ModelRawContentChangedEventArgs.ResultingSelection`, the `TextModel` options
  API, and the new features (`WordOperationsFeature`, `WordPartOperationsFeature`, `MultiCursorFeature` with their
  command id constants), like `SelectAllFeature` today. Everything else stays `internal`.
- The IME works on Windows 11 only until a TSF (`ITextStoreACP2`) implementation is written, if ever needed.
- Phases 4–5 plug the language configuration into the cut points listed in the phase 3 cards.
