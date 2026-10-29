// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (TokenType, Token)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>The kinds of token of the snippet syntax (Monaco: <c>TokenType</c>).</summary>
internal enum TokenType
{
    Dollar,
    Colon,
    Comma,
    CurlyOpen,
    CurlyClose,
    Backslash,
    Forwardslash,
    Pipe,
    Int,
    VariableName,
    Format,
    Plus,
    Dash,
    QuestionMark,
    EOF,
}

/// <summary>One token: its kind, and where it is in the scanned text (Monaco: <c>Token</c>).</summary>
/// <param name="Type">The kind.</param>
/// <param name="Pos">The index of its first character.</param>
/// <param name="Len">Its length in UTF-16 code units.</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct SnippetToken(TokenType Type, int Pos, int Len);
