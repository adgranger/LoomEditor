// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetVariables.ts (KnownSnippetVariableNames)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>The names of the variables the resolvers know (Monaco: <c>KnownSnippetVariableNames</c>).</summary>
/// <remarks>Threading: immutable after the static initialization.</remarks>
internal static class SnippetVariables
{
    /// <summary>The 39 known variable names, case-sensitive.</summary>
    public static readonly HashSet<string> KnownNames = new(StringComparer.Ordinal)
    {
        "CURRENT_YEAR", "CURRENT_YEAR_SHORT", "CURRENT_MONTH", "CURRENT_DATE", "CURRENT_HOUR", "CURRENT_MINUTE", "CURRENT_SECOND",
        "CURRENT_MILLISECOND", "CURRENT_DAY_NAME", "CURRENT_DAY_NAME_SHORT", "CURRENT_MONTH_NAME", "CURRENT_MONTH_NAME_SHORT",
        "CURRENT_SECONDS_UNIX", "CURRENT_MILLISECONDS_UNIX", "CURRENT_TIMEZONE_OFFSET", "CURRENT_TIMEZONE_NAME",
        "SELECTION", "CLIPBOARD", "TM_SELECTED_TEXT", "TM_CURRENT_LINE", "TM_CURRENT_WORD", "TM_LINE_INDEX", "TM_LINE_NUMBER",
        "TM_FILENAME", "TM_FILENAME_BASE", "TM_DIRECTORY", "TM_DIRECTORY_BASE", "TM_FILEPATH",
        "CURSOR_INDEX", "CURSOR_NUMBER", "RELATIVE_FILEPATH",
        "BLOCK_COMMENT_START", "BLOCK_COMMENT_END", "LINE_COMMENT",
        "WORKSPACE_NAME", "WORKSPACE_FOLDER", "RANDOM", "RANDOM_HEX", "UUID",
    };
}
