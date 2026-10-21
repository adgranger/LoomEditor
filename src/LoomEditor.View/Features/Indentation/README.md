# Indentation

**Role:** converts the indentation of the document to spaces or tabs, re-indents lines with the indentation rules of the
language, and (later) detects the indentation and re-indents pasted text.

**Monaco sources:** `src/vs/editor/contrib/indentation/` (`common/indentUtils.ts`, `common/indentation.ts`, `browser/indentation.ts`).

**Files:**
- `IndentUtils.cs` — `GetSpaceCnt`, `GenerateIndent`.
- `ReindentOperations.cs` — `GetReindentEditOperations`: the edits that re-indent lines.
- `IndentationToSpacesCommand.cs`, `IndentationToTabsCommand.cs` — the conversion commands.
- `IndentationFeature.cs`, `IndentationCommands.cs` — the feature (it registers nothing yet: the actions come in P5-53, paste in P5-54) and the command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/Indentation/`.

**Known limits:** no actions yet.
