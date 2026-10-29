// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (CompletionItemInsertTextRule)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Features;

/// <summary>How the insert text of a completion item is interpreted.</summary>
/// <remarks>Monaco: <c>CompletionItemInsertTextRule</c> (the same bit values). Threading: enum, safe to share between threads.</remarks>
[Flags]
public enum InsertTextRules
{
    /// <summary>No rule: the text is inserted as it is, re-indented to the line.</summary>
    None = 0,

    /// <summary>Keep the whitespace of the text: do not adjust the indentation of its lines.</summary>
    KeepWhitespace = 1,

    /// <summary>The text is a snippet template (tab stops, placeholders, variables).</summary>
    InsertAsSnippet = 4,
}
