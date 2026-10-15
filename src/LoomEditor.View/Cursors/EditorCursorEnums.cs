// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>When to close brackets, quotes or comments automatically (Monaco: <c>autoClosingBrackets</c>, <c>autoClosingComments</c>, <c>autoClosingQuotes</c> strings).</summary>
internal enum EditorAutoClosingStrategy
{
    Always,
    LanguageDefined,
    BeforeWhitespace,
    Never,
}

/// <summary>When to delete or overtype an auto-closed character (Monaco: <c>autoClosingDelete</c>, <c>autoClosingOvertype</c> strings).</summary>
internal enum EditorAutoClosingEditStrategy
{
    Always,
    Auto,
    Never,
}

/// <summary>What typing a bracket or a quote does with a selection (Monaco: <c>autoSurround</c> strings).</summary>
internal enum EditorAutoSurroundStrategy
{
    LanguageDefined,
    Quotes,
    Brackets,
    Never,
}

/// <summary>How much the editor adjusts the indentation of new lines (Monaco: <c>EditorAutoIndentStrategy</c>).</summary>
internal enum EditorAutoIndentStrategy
{
    None = 0,
    Keep = 1,
    Brackets = 2,
    Advanced = 3,
    Full = 4,
}

/// <summary>How a paste with as many lines as cursors is distributed (Monaco: <c>multiCursorPaste</c> strings).</summary>
internal enum MultiCursorPasteMode
{
    Spread,
    Full,
}

/// <summary>Whether typing inserts or overwrites (Monaco: <c>InputMode</c>'s <c>'insert' | 'overtype'</c>).</summary>
internal enum InputMode
{
    Insert,
    Overtype,
}
