// Ported from microsoft/vscode: src/vs/editor/common/languages/languageConfiguration.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages;

/// <summary>Describes what to do with the indentation when pressing Enter (Monaco: <c>IndentAction</c>).</summary>
internal enum IndentAction
{
    /// <summary>Insert new line and copy the previous line's indentation.</summary>
    None = 0,

    /// <summary>Insert new line and indent once (relative to the previous line's indentation).</summary>
    Indent = 1,

    /// <summary>
    /// Insert two new lines: the first one indented which will hold the cursor, the second one at the same
    /// indentation level.
    /// </summary>
    IndentOutdent = 2,

    /// <summary>Insert new line and outdent once (relative to the previous line's indentation).</summary>
    Outdent = 3,
}

/// <summary>Describes what to do when pressing Enter (Monaco: <c>EnterAction</c>).</summary>
/// <param name="IndentAction">Describe what to do with the indentation.</param>
/// <param name="AppendText">Describes text to be appended after the new line and after the indentation.</param>
/// <param name="RemoveText">
/// Describes the number of characters to remove from the new line's indentation. Monaco's optional <c>removeText</c>
/// is only read as <c>removeText || 0</c> or as a truth value: 0 here.
/// </param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct EnterAction(IndentAction IndentAction, string? AppendText = null, int RemoveText = 0);

/// <summary>An <see cref="EnterAction"/> with every value set (Monaco: <c>CompleteEnterAction</c>).</summary>
/// <param name="IndentAction">Describe what to do with the indentation.</param>
/// <param name="AppendText">Describes text to be appended after the new line and after the indentation.</param>
/// <param name="RemoveText">Describes the number of characters to remove from the new line's indentation.</param>
/// <param name="Indentation">The line's indentation minus removeText.</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct CompleteEnterAction(IndentAction IndentAction, string AppendText, int RemoveText, string Indentation);
