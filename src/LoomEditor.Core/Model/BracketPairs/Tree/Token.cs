// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/tokenizer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>The kind of a <see cref="Token"/> (Monaco: <c>TokenKind</c>).</summary>
internal enum TokenKind
{
    /// <summary>Plain text.</summary>
    Text = 0,

    /// <summary>An opening bracket.</summary>
    OpeningBracket = 1,

    /// <summary>A closing bracket.</summary>
    ClosingBracket = 2,
}

/// <summary>
/// A token of the bracket pair tokenizer: plain text or a bracket. Token instances are shared between reads
/// (Monaco: <c>Token</c>).
/// </summary>
/// <remarks>Threading: immutable, read on the UI thread.</remarks>
internal sealed class Token
{
    /// <summary>Creates a token.</summary>
    /// <param name="length">The length of the token text.</param>
    /// <param name="kind">The kind of the token.</param>
    /// <param name="bracketId">See <see cref="BracketId"/>.</param>
    /// <param name="bracketIds">See <see cref="BracketIds"/>.</param>
    /// <param name="astNode">See <see cref="AstNode"/>.</param>
    public Token(Length length, TokenKind kind, int bracketId, SmallImmutableSet<int> bracketIds, AstNode? astNode)
    {
        ArgumentNullException.ThrowIfNull(bracketIds);

        Length = length;
        Kind = kind;
        BracketId = bracketId;
        BracketIds = bracketIds;
        AstNode = astNode;
    }

    /// <summary>The length of the token text.</summary>
    public Length Length { get; }

    /// <summary>The kind of the token.</summary>
    public TokenKind Kind { get; }

    /// <summary>
    /// If this token is an opening bracket, this is the id of the opening bracket.
    /// If this token is a closing bracket, this is the id of the first opening bracket that is closed by this bracket.
    /// Otherwise, it is -1.
    /// </summary>
    public int BracketId { get; }

    /// <summary>
    /// If this token is an opening bracket, this just contains <see cref="BracketId"/>.
    /// If this token is a closing bracket, this lists all opening bracket ids that it closes.
    /// Otherwise, it is empty.
    /// </summary>
    public SmallImmutableSet<int> BracketIds { get; }

    /// <summary>A <see cref="BracketAstNode"/> or a <see cref="TextAstNode"/>, if any (Monaco: <c>astNode</c>).</summary>
    public AstNode? AstNode { get; }
}
