# How to use snippets

A snippet is a text template with tab stops. `LoomEditor.InsertSnippet(template)` inserts it at **every cursor**, selects the first tab stop and enters *snippet mode*:
`Tab` / `Shift+Tab` move between the tab stops, `Escape` leaves, and the mode ends by itself at the last tab stop, when a caret leaves the placeholders, or on undo.
The syntax is Monaco's (TextMate snippets); the design is in [ADR 0013](../adr/0013-text-options-and-snippets.md).

```csharp
editor.InsertSnippet("for (let ${1:i} = 0; $1 < ${2:array}.length; $1++) {\n\t$0\n}");
editor.InsertSnippet("console.log('${TM_SELECTED_TEXT}', ${TM_SELECTED_TEXT});$0");   // wraps the selection
editor.InsertSnippet("foo", overwriteBefore: 3, overwriteAfter: 0);                   // replaces the 3 characters before each caret
```

| Syntax | Example | Meaning |
|---|---|---|
| Tab stop | `$1`, `${1}` | a place to jump to; the same number twice is a *mirror*: typing in one updates the others |
| Final tab stop | `$0` | where the caret ends (the end of the snippet when absent) |
| Placeholder | `${1:default}` | a tab stop with a default text, selected when reached; placeholders can nest (`${1:a ${2:b}}`) |
| Choice | `${1\|one,two\|}` | the **first** option is inserted; there is no list (not supported) |
| Variable | `$TM_FILENAME`, `${TM_SELECTED_TEXT:none}` | resolved on insert; an unknown or empty variable keeps its default text, or its name |
| Transform | `${TM_FILENAME/(.*)\..+$/$1/}` | a regular expression applied to a variable or to the text of a placeholder when you leave it |
| Escape | `\$`, `\}`, `\\` | a literal character |

Variables: `SELECTION` / `TM_SELECTED_TEXT`, `TM_CURRENT_LINE`, `TM_CURRENT_WORD`, `TM_LINE_INDEX`, `TM_LINE_NUMBER`, `CURSOR_INDEX`, `CURSOR_NUMBER`, `CLIPBOARD`,
`LINE_COMMENT`, `BLOCK_COMMENT_START`, `BLOCK_COMMENT_END`, the `CURRENT_*` date and time ones, `RANDOM`, `RANDOM_HEX`, `UUID`, and the ones that need the application:
`TM_FILENAME`, `TM_FILENAME_BASE`, `TM_DIRECTORY`, `TM_DIRECTORY_BASE`, `TM_FILEPATH`, `RELATIVE_FILEPATH`, `WORKSPACE_NAME`, `WORKSPACE_FOLDER`. The file and workspace variables
are unknown unless the application gives them (the control gives none: `ISnippetVariableHost` is internal today).

**Whitespace.** The template is re-indented to the line of each cursor and its line breaks follow the model, as in Monaco. A nested `InsertSnippet` while a snippet is active
is merged into the active placeholder.

**Completion items.** An item with `InsertTextRules.InsertAsSnippet` (and every LSP item with `insertTextFormat: 2`) is inserted as a snippet when it is accepted,
at every cursor, with the item's replace range and additional edits; `InsertTextRules.KeepWhitespace` skips the re-indentation. Plain items still edit the primary cursor only.

**Not supported:** the choice list (`inSnippetChoice`), snippet text edits from a workspace edit (`apply(edits)`), the overtype capture of `TM_SELECTED_TEXT`, a library of user snippets
(an application concern), `InsertSnippet` in a read-only editor (it does nothing).

Try it in the Gallery: the "Snippets" sample and its two buttons. Tests: `tests/LoomEditor.View.Tests/Features/Snippet/`.
