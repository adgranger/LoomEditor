# Indentation

**Role:** converts the indentation of the document to spaces or tabs, changes the tab size, detects the indentation,
re-indents lines with the indentation rules of the language, and re-indents pasted text (off by default).

**Monaco sources:** `src/vs/editor/contrib/indentation/` (`common/indentUtils.ts`, `common/indentation.ts`, `browser/indentation.ts`).

**Files:**
- `IndentUtils.cs` — `GetSpaceCnt`, `GenerateIndent`.
- `ReindentOperations.cs` — `GetReindentEditOperations`: the edits that re-indent lines.
- `IndentationToSpacesCommand.cs`, `IndentationToTabsCommand.cs` — the conversion commands.
- `AutoIndentOnPaste.cs`, `AutoIndentOnPasteCommand.cs`, `EditorIndentationOptions.cs` — the paste re-indentation (`editor.autoIndentOnPaste`, default off; it listens to `ViewModel.DidPaste`).
- `IndentationActions.cs` — what the eight commands do to a `ViewModel`.
- `IndentationFeature.cs`, `IndentationCommands.cs` — the feature (registers the eight commands, owns the `AutoIndentOnPaste`) and the command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/Indentation/`.

**Known limits:** no quick pick. `IndentUsingTabs`, `IndentUsingSpaces` and `ChangeTabDisplaySize` take no argument and apply
the item the quick pick pre-selects, `min(tabSize - 1, 7) + 1`; `IndentationActions.ChangeIndentationSize` takes the size.
The `detectIndentation` editor option is not wired: only the command exists.
