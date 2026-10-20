// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/tokenizer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>Reads a document as a stream of bracket and text <see cref="Token"/>s (Monaco: <c>Tokenizer</c>).</summary>
/// <remarks>Threading: single-threaded (UI thread), like the tree that owns it.</remarks>
internal interface ITokenizer
{
    /// <summary>The offset of the next token to read.</summary>
    Length Offset { get; }

    /// <summary>The length of the whole text.</summary>
    Length Length { get; }

    /// <summary>Reads the next token, or returns <see langword="null"/> at the end of the text.</summary>
    Token? Read();

    /// <summary>Returns the next token without consuming it, or <see langword="null"/> at the end of the text.</summary>
    Token? Peek();

    /// <summary>Moves the offset forward by <paramref name="length"/>.</summary>
    void Skip(Length length);

    /// <summary>The whole text.</summary>
    string GetText();
}

/// <summary>
/// What a <see cref="TextBufferTokenizer"/> needs from a model (Monaco: <c>ITokenizerSource</c>, whose
/// <c>tokenization.getLineTokens</c> is flattened into <see cref="GetLineTokens"/>).
/// </summary>
/// <remarks>Threading: single-threaded (UI thread).</remarks>
internal interface ITokenizerSource
{
    /// <summary><c>getValue</c>.</summary>
    string GetValue();

    /// <summary><c>getLineCount</c>.</summary>
    int GetLineCount();

    /// <summary><c>getLineLength</c>.</summary>
    int GetLineLength(int lineNumber);

    /// <summary><c>tokenization.getLineTokens</c>.</summary>
    IViewLineTokens GetLineTokens(int lineNumber);
}
